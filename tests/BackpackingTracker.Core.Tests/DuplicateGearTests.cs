using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>Finding duplicate Gear Items, such as the same sheet imported on two devices, and Merging them.</summary>
public class DuplicateGearTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly InMemoryCloudFileStore _drive = new();
    private readonly AppCore _phone;
    private readonly AppCore _laptop;

    public DuplicateGearTests()
    {
        // Two devices with their own data and one Drive, sharing a clock so tests control which edit is newer.
        _phone = new AppCore(new InMemoryRecordStore(), _clock, _drive);
        _laptop = new AppCore(new InMemoryRecordStore(), _clock, _drive);
    }

    private void Later() => _clock.UtcNow = _clock.UtcNow.AddMinutes(5);

    private static Task<GearItem> AddGearAsync(AppCore app, string name, string gearType = "Tent", decimal grams = 1000m) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, gearType, grams));

    private async Task SyncAllAsync()
    {
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();
    }

    private static async Task<DuplicateGroup> OnlyGroupAsync(AppCore app) => Assert.Single(await app.Duplicates.ListAsync());

    private static async Task<IReadOnlyList<string>> GearItemIdsOnListAsync(AppCore app, string gearListId) =>
        (await app.Lists.GetGearListAsync(gearListId))!.GearList.Entries.Select(entry => entry.GearItemId).ToList();

    private static async Task<IReadOnlyList<ListEntry>> PackingListAsync(AppCore app, string tripId) =>
        (await app.Trips.GetTripAsync(tripId))!.Trip.PackingList.Entries;

    private static async Task<string> FirstCategoryAsync(AppCore app, string tripId) =>
        (await app.Trips.GetTripAsync(tripId))!.Trip.PackingList.Categories[0].Id;

    // ----- Finding duplicates -----

    [Fact]
    public async Task Gear_with_the_same_gear_type_and_name_ignoring_case_and_spaces_is_a_duplicate()
    {
        var a = await AddGearAsync(_phone, "Duplex", "Tent");
        var b = await AddGearAsync(_phone, " duplex", "tent ");
        await AddGearAsync(_phone, "Duplex", "Shelter");
        await AddGearAsync(_phone, "Stove");

        var group = await OnlyGroupAsync(_phone);

        Assert.Equal(new[] { a.Id, b.Id }.Order(), group.GearItemIds.Order());
    }

    [Fact]
    public async Task Retired_gear_counts_as_a_duplicate()
    {
        var a = await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_phone, "Duplex");
        await _phone.Gear.RetireGearItemAsync(a.Id);

        Assert.Equal(2, (await OnlyGroupAsync(_phone)).Items.Count);
    }

    [Fact]
    public async Task The_one_used_by_more_lists_and_trips_is_suggested_to_keep()
    {
        await AddGearAsync(_phone, "Duplex");
        var used = await AddGearAsync(_phone, "Duplex");
        var list = await _phone.Lists.CreateGearListAsync("Summer");
        await _phone.Lists.AddEntryAsync(list.Id, used.Id);

        var group = await OnlyGroupAsync(_phone);

        Assert.Equal(used.Id, group.SuggestedKeepId);
        Assert.Equal(used.Id, group.Items[0].GearItem.Id);
        Assert.Equal(["Gear List \"Summer\""], group.Items[0].UsedBy);
    }

    [Fact]
    public async Task The_same_sheet_imported_on_two_devices_shows_up_as_duplicates_after_syncing()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_laptop, "Duplex");

        await SyncAllAsync();

        Assert.Equal(2, (await OnlyGroupAsync(_phone)).Items.Count);
        Assert.Equal(2, (await OnlyGroupAsync(_laptop)).Items.Count);
    }

    // ----- The notice after a sync -----

    [Fact]
    public async Task A_sync_that_brings_in_a_duplicate_suggests_a_review_until_the_notice_is_closed()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_laptop, "Duplex");
        Assert.Equal(0, await _laptop.Duplicates.CountToReviewAsync());

        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();

        Assert.Equal(1, await _laptop.Duplicates.CountToReviewAsync());
        _laptop.Duplicates.CloseReviewNotice();
        Assert.Equal(0, await _laptop.Duplicates.CountToReviewAsync());
    }

    [Fact]
    public async Task Duplicates_made_on_this_device_alone_dont_raise_the_notice()
    {
        await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_phone, "Duplex");

        Assert.Equal(0, await _phone.Duplicates.CountToReviewAsync());
        Assert.Single(await _phone.Duplicates.ListAsync());
    }

    [Fact]
    public async Task Importing_an_export_with_a_duplicate_suggests_a_review()
    {
        await AddGearAsync(_laptop, "Duplex");
        await AddGearAsync(_phone, "Duplex");

        await _phone.Backup.ImportAsync(await _laptop.Backup.ExportAsync());

        Assert.Equal(1, await _phone.Duplicates.CountToReviewAsync());
    }

    // ----- Merging -----

    [Fact]
    public async Task Merging_keeps_the_chosen_gear_as_it_is_and_deletes_the_others()
    {
        var a = await AddGearAsync(_phone, "Duplex", grams: 1100m);
        var b = await AddGearAsync(_phone, "Duplex", grams: 1090m);
        var group = await OnlyGroupAsync(_phone);

        await _phone.Duplicates.MergeAsync(group.GearItemIds, b.Id);

        var left = Assert.Single(await _phone.Gear.ListGearItemsAsync());
        Assert.Equal(b.Id, left.Id);
        Assert.Equal(1090m, left.WeightGrams);
        Assert.Null(await _phone.Gear.GetGearItemAsync(a.Id));
        Assert.Empty(await _phone.Duplicates.ListAsync());
    }

    [Fact]
    public async Task Merging_moves_gear_lists_and_trips_including_completed_ones_onto_the_kept_gear()
    {
        var a = await AddGearAsync(_phone, "Duplex", grams: 1100m);
        var b = await AddGearAsync(_phone, "Duplex", grams: 1090m);
        var list = await _phone.Lists.CreateGearListAsync("Summer");
        await _phone.Lists.AddEntryAsync(list.Id, a.Id);
        var planned = await _phone.Trips.CreateTripAsync("JMT", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3), list.Id);
        var completed = await _phone.Trips.CreateTripAsync("Test", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 2), list.Id);
        await _phone.Trips.CompleteTripAsync(completed.Id);

        await _phone.Duplicates.MergeAsync((await OnlyGroupAsync(_phone)).GearItemIds, b.Id);

        Assert.Equal([b.Id], await GearItemIdsOnListAsync(_phone, list.Id));
        Assert.Equal([b.Id], (await PackingListAsync(_phone, planned.Id)).Select(entry => entry.GearItemId));
        var frozen = Assert.Single(await PackingListAsync(_phone, completed.Id));
        Assert.Equal(b.Id, frozen.GearItemId);
        Assert.Equal(1100m, frozen.FrozenWeightGrams);
    }

    [Fact]
    public async Task A_list_with_both_keeps_one_entry_the_kept_gears_own_and_quantities_arent_added()
    {
        var a = await AddGearAsync(_phone, "Duplex");
        var b = await AddGearAsync(_phone, "Duplex");
        var trip = await _phone.Trips.CreateTripAsync("JMT", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3));
        var category = await FirstCategoryAsync(_phone, trip.Id);
        var entryA = await _phone.Trips.AddEntryAsync(trip.Id, a.Id, category);
        var entryB = await _phone.Trips.AddEntryAsync(trip.Id, b.Id, category);
        await _phone.Trips.UpdateEntryAsync(trip.Id, entryB.Id, 2, CarryMode.Worn);
        await _phone.Trips.SetPackedAsync(trip.Id, entryA.Id, packed: true);

        await _phone.Duplicates.MergeAsync((await OnlyGroupAsync(_phone)).GearItemIds, b.Id);

        var entry = Assert.Single(await PackingListAsync(_phone, trip.Id));
        Assert.Equal(entryB.Id, entry.Id);
        Assert.Equal(b.Id, entry.GearItemId);
        Assert.Equal(2, entry.Quantity);
        Assert.Equal(CarryMode.Worn, entry.CarryMode);
        Assert.True(entry.Packed);
    }

    [Fact]
    public async Task The_plan_says_what_the_merge_will_change()
    {
        var a = await AddGearAsync(_phone, "Duplex");
        var b = await AddGearAsync(_phone, "Duplex");
        var list = await _phone.Lists.CreateGearListAsync("Summer");
        await _phone.Lists.AddEntryAsync(list.Id, a.Id);
        await _phone.Trips.CreateTripAsync("JMT", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3), list.Id);
        await _phone.Trips.CreateTripAsync("Test", new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 2), list.Id);

        var plan = await _phone.Duplicates.PlanMergeAsync((await OnlyGroupAsync(_phone)).GearItemIds, b.Id);

        Assert.Equal(
            "Keep the chosen Duplex? The other one will be deleted, and 1 Gear List and 2 Trips will use the kept one instead.",
            plan.Message);
    }

    [Fact]
    public async Task Merging_is_refused_once_the_set_has_changed()
    {
        var a = await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_phone, "Duplex");
        var ids = (await OnlyGroupAsync(_phone)).GearItemIds;
        await AddGearAsync(_phone, "Duplex");

        await Assert.ThrowsAsync<RuleViolationException>(() => _phone.Duplicates.MergeAsync(ids, a.Id));
    }

    [Fact]
    public async Task The_merge_reaches_the_other_device()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var a = await AddGearAsync(_phone, "Duplex");
        var b = await AddGearAsync(_laptop, "Duplex");
        var list = await _laptop.Lists.CreateGearListAsync("Summer");
        await _laptop.Lists.AddEntryAsync(list.Id, b.Id);
        await SyncAllAsync();

        Later();
        await _laptop.Duplicates.MergeAsync((await OnlyGroupAsync(_laptop)).GearItemIds, a.Id);
        await SyncAllAsync();

        Assert.Equal([a.Id], (await _phone.Gear.ListGearItemsAsync()).Select(item => item.Id));
        Assert.Equal([a.Id], await GearItemIdsOnListAsync(_phone, list.Id));
    }

    [Fact]
    public async Task A_list_changed_on_another_device_before_it_had_the_merge_is_moved_onto_the_kept_gear()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var a = await AddGearAsync(_phone, "Duplex");
        var b = await AddGearAsync(_laptop, "Duplex");
        await SyncAllAsync();

        // The laptop merges A into B while the phone, not yet synced, puts A on a new list.
        Later();
        await _laptop.Duplicates.MergeAsync((await OnlyGroupAsync(_laptop)).GearItemIds, b.Id);
        var list = await _phone.Lists.CreateGearListAsync("Summer");
        await _phone.Lists.AddEntryAsync(list.Id, a.Id);
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();
        await SyncAllAsync();

        Assert.Equal([b.Id], await GearItemIdsOnListAsync(_phone, list.Id));
        Assert.Equal([b.Id], await GearItemIdsOnListAsync(_laptop, list.Id));
    }

    [Fact]
    public async Task Merging_is_blocked_while_one_of_them_is_in_a_sync_conflict()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var a = await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_phone, "Duplex");
        await SyncAllAsync();
        Later();
        await _phone.Gear.UpdateGearItemAsync(a.Id, new GearItemDetails("Duplex", "Tent", 900m));
        Later();
        await _laptop.Gear.UpdateGearItemAsync(a.Id, new GearItemDetails("Duplex", "Tent", 950m));
        await SyncAllAsync();

        // The laptop synced second, so the conflict waits there.
        Assert.Equal(1, (await _laptop.Sync.GetStatusAsync()).PendingConflicts);
        var group = await OnlyGroupAsync(_laptop);

        Assert.False(group.CanMerge);
        await Assert.ThrowsAsync<RuleViolationException>(() => _laptop.Duplicates.MergeAsync(group.GearItemIds, a.Id));
    }

    // ----- Not duplicates -----

    [Fact]
    public async Task Not_duplicates_stops_the_set_being_offered_on_every_device()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        await AddGearAsync(_phone, "Stuff sack", "Storage");
        await AddGearAsync(_phone, "Stuff sack", "Storage");
        await SyncAllAsync();

        await _phone.Duplicates.MarkNotDuplicatesAsync((await OnlyGroupAsync(_phone)).GearItemIds);
        await SyncAllAsync();

        Assert.Empty(await _phone.Duplicates.ListAsync());
        Assert.Empty(await _laptop.Duplicates.ListAsync());
    }

    [Fact]
    public async Task A_new_matching_gear_item_offers_the_set_again()
    {
        await AddGearAsync(_phone, "Stuff sack", "Storage");
        await AddGearAsync(_phone, "Stuff sack", "Storage");
        await _phone.Duplicates.MarkNotDuplicatesAsync((await OnlyGroupAsync(_phone)).GearItemIds);

        await AddGearAsync(_phone, "Stuff sack", "Storage");

        Assert.Equal(3, (await OnlyGroupAsync(_phone)).Items.Count);
    }
}
