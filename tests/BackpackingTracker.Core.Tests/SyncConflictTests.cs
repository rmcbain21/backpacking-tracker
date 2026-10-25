using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Tests;

/// <summary>A record changed on two devices since they last synced: kept on both until the backpacker chooses.</summary>
public class SyncConflictTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly InMemoryCloudFileStore _drive = new();
    private readonly InMemoryRecordStore _laptopStore = new();
    private readonly AppCore _phone;
    private readonly AppCore _laptop;

    public SyncConflictTests()
    {
        _phone = new AppCore(new InMemoryRecordStore(), _clock, _drive);
        _laptop = new AppCore(_laptopStore, _clock, _drive);
    }

    private void Later() => _clock.UtcNow = _clock.UtcNow.AddMinutes(5);

    private static Task UpdateWeightAsync(AppCore app, GearItem item, decimal grams) =>
        app.Gear.UpdateGearItemAsync(item.Id, new GearItemDetails(item.Name, item.GearType, grams));

    private static async Task<decimal?> WeightAsync(AppCore app, GearItem item) =>
        (await app.Gear.GetGearItemAsync(item.Id))?.WeightGrams;

    private static async Task<int> PendingAsync(AppCore app) => (await app.Sync.GetStatusAsync()).PendingConflicts;

    private static async Task SyncAllAsync(params AppCore[] devices)
    {
        foreach (var device in devices)
        {
            await device.Sync.SyncNowAsync();
        }
    }

    /// <summary>A tent known to both devices, synced, then weighed differently on each: 950 g on the phone, 900 g on the laptop.</summary>
    private async Task<GearItem> ConflictingTentAsync()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var tent = await _phone.Gear.AddGearItemAsync(new GearItemDetails("Duplex", "Tent", 1000m));
        await SyncAllAsync(_phone, _laptop);
        Later();
        await UpdateWeightAsync(_phone, tent, 950m);
        Later();
        await UpdateWeightAsync(_laptop, tent, 900m);
        await SyncAllAsync(_phone, _laptop);
        return tent;
    }

    [Fact]
    public async Task Different_edits_on_two_devices_become_a_sync_conflict_and_neither_is_overwritten()
    {
        var tent = await ConflictingTentAsync();

        await _phone.Sync.SyncNowAsync();

        Assert.Equal(1, await PendingAsync(_laptop));
        Assert.Equal(900m, await WeightAsync(_laptop, tent));
        Assert.Equal(950m, await WeightAsync(_phone, tent));
    }

    [Fact]
    public async Task The_same_edit_on_two_devices_is_not_a_conflict()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var tent = await _phone.Gear.AddGearItemAsync(new GearItemDetails("Duplex", "Tent", 1000m));
        await SyncAllAsync(_phone, _laptop);
        Later();
        await UpdateWeightAsync(_phone, tent, 950m);
        Later();
        await UpdateWeightAsync(_laptop, tent, 950m);

        await SyncAllAsync(_phone, _laptop, _phone);

        Assert.Equal(0, await PendingAsync(_laptop));
        Assert.Equal(0, await PendingAsync(_phone));
    }

    [Fact]
    public async Task A_conflict_is_still_waiting_after_the_app_is_reopened()
    {
        await ConflictingTentAsync();

        var reopened = new AppCore(_laptopStore, _clock, _drive);

        Assert.Equal(1, await PendingAsync(reopened));
        Assert.Single(await reopened.Sync.ListConflictsAsync());
    }

    [Fact]
    public async Task Syncing_again_before_settling_keeps_one_conflict()
    {
        await ConflictingTentAsync();

        await SyncAllAsync(_laptop, _phone, _laptop);

        Assert.Equal(1, await PendingAsync(_laptop));
    }

    [Fact]
    public async Task Other_changes_keep_syncing_while_a_conflict_waits()
    {
        await ConflictingTentAsync();
        await _phone.Gear.AddGearItemAsync(new GearItemDetails("Stove", "Stove", 80m));

        await SyncAllAsync(_phone, _laptop);

        Assert.Contains("Stove", (await _laptop.Gear.ListGearItemsAsync()).Select(item => item.Name));
        Assert.Equal(1, await PendingAsync(_laptop));
    }

    [Fact]
    public async Task The_conflict_shows_both_versions_in_display_units_with_the_difference_marked()
    {
        await ConflictingTentAsync();

        var conflict = Assert.Single(await _laptop.Sync.ListConflictsAsync());

        Assert.Equal(("Gear Item", "Duplex"), (conflict.RecordType, conflict.Name));
        var weight = conflict.Lines.Single(line => line.Label == "Weight");
        Assert.Equal(
            (UnitFormat.Weight(900m, UnitSystem.Imperial), UnitFormat.Weight(950m, UnitSystem.Imperial), true),
            (weight.ThisDevice, weight.OtherDevice, weight.Differs));
        Assert.False(conflict.Lines.Single(line => line.Label == "Name").Differs);
    }

    [Fact]
    public async Task Keeping_this_devices_version_sends_it_to_the_other_device()
    {
        var tent = await ConflictingTentAsync();
        var conflict = Assert.Single(await _laptop.Sync.ListConflictsAsync());
        Later();

        await _laptop.Sync.SettleAsync(conflict.Id, SyncConflictChoice.ThisDevice);
        await SyncAllAsync(_laptop, _phone);

        Assert.Equal(0, await PendingAsync(_laptop));
        Assert.Equal(900m, await WeightAsync(_phone, tent));
        Assert.Equal(900m, await WeightAsync(_laptop, tent));
    }

    [Fact]
    public async Task Keeping_the_other_devices_version_takes_it_here_and_it_stays_settled()
    {
        var tent = await ConflictingTentAsync();
        var conflict = Assert.Single(await _laptop.Sync.ListConflictsAsync());
        Later();

        await _laptop.Sync.SettleAsync(conflict.Id, SyncConflictChoice.OtherDevice);

        Assert.Equal(950m, await WeightAsync(_laptop, tent));
        await SyncAllAsync(_laptop, _phone, _laptop);
        Assert.Equal(0, await PendingAsync(_laptop));
        Assert.Equal(0, await PendingAsync(_phone));
        Assert.Equal(950m, await WeightAsync(_phone, tent));
    }

    [Fact]
    public async Task Settling_a_conflict_that_is_no_longer_waiting_is_refused()
    {
        await ConflictingTentAsync();
        var conflict = Assert.Single(await _laptop.Sync.ListConflictsAsync());
        await _laptop.Sync.SettleAsync(conflict.Id, SyncConflictChoice.ThisDevice);

        await Assert.ThrowsAsync<RuleViolationException>(() => _laptop.Sync.SettleAsync(conflict.Id, SyncConflictChoice.OtherDevice));
    }

    [Fact]
    public async Task A_deletion_on_one_device_and_an_edit_on_the_other_is_a_conflict()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var tarp = await _phone.Gear.AddGearItemAsync(new GearItemDetails("Old tarp", "Tarp", 300m));
        await SyncAllAsync(_phone, _laptop);
        Later();
        await _phone.Gear.DeleteGearItemAsync(tarp.Id);
        Later();
        await UpdateWeightAsync(_laptop, tarp, 280m);

        await SyncAllAsync(_phone, _laptop);

        var conflict = Assert.Single(await _laptop.Sync.ListConflictsAsync());
        Assert.Equal(("No", "Yes"), conflict.Lines.Where(line => line.Label == "Deleted").Select(line => (line.ThisDevice, line.OtherDevice)).Single());
        await _laptop.Sync.SettleAsync(conflict.Id, SyncConflictChoice.OtherDevice);
        Assert.Null(await _laptop.Gear.GetGearItemAsync(tarp.Id));
        await SyncAllAsync(_laptop, _phone);
        Assert.Null(await _phone.Gear.GetGearItemAsync(tarp.Id));
        Assert.Equal(0, await PendingAsync(_laptop));
    }

    [Fact]
    public async Task A_trip_conflict_lists_only_the_lines_that_differ()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var trip = await _phone.Trips.CreateTripAsync("Wind Rivers", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3));
        await SyncAllAsync(_phone, _laptop);
        Later();
        await _phone.Trips.UpdateDayAsync(trip.Id, new DateOnly(2026, 7, 2), new TripDayDetails(Campsite: "Island Lake"));
        Later();
        await _laptop.Trips.UpdateDayAsync(trip.Id, new DateOnly(2026, 7, 2), new TripDayDetails(Campsite: "Titcomb Basin"));
        await _laptop.Trips.SetWaterCarriedAsync(trip.Id, 1000m);

        await SyncAllAsync(_phone, _laptop);

        var conflict = Assert.Single(await _laptop.Sync.ListConflictsAsync());
        Assert.Equal(("Trip", "Wind Rivers"), (conflict.RecordType, conflict.Name));
        Assert.Equal(
            [("Water carried", "1.0 L", "—"), ("Day 2 campsite", "Titcomb Basin", "Island Lake")],
            conflict.Lines.Select(line => (line.Label, line.ThisDevice, line.OtherDevice)));
        Assert.True(conflict.SameCount > 0);
    }

    [Fact]
    public async Task A_record_that_differs_on_a_devices_first_sync_is_a_conflict()
    {
        // The laptop got the phone's tent from a backup, so it has the record but no sync history for it.
        var tent = await _phone.Gear.AddGearItemAsync(new GearItemDetails("Duplex", "Tent", 1000m));
        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());
        await _phone.Sync.ConnectAsync();
        Later();
        await UpdateWeightAsync(_phone, tent, 950m);
        await _phone.Sync.SyncNowAsync();
        Later();
        await UpdateWeightAsync(_laptop, tent, 900m);

        await _laptop.Sync.ConnectAsync();
        await _laptop.Sync.SyncNowAsync();

        Assert.Equal(1, await PendingAsync(_laptop));
    }

    [Fact]
    public async Task Reconnecting_keeps_the_sync_history_so_an_edit_made_while_disconnected_is_not_a_conflict()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var tent = await _phone.Gear.AddGearItemAsync(new GearItemDetails("Duplex", "Tent", 1000m));
        await SyncAllAsync(_phone, _laptop);
        await _laptop.Sync.DisconnectAsync();
        Later();
        await UpdateWeightAsync(_laptop, tent, 900m);

        await _laptop.Sync.ConnectAsync();
        await SyncAllAsync(_laptop, _phone);

        Assert.Equal(0, await PendingAsync(_laptop));
        Assert.Equal(900m, await WeightAsync(_phone, tent));
    }
}
