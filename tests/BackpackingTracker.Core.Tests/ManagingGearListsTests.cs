using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>Renaming, duplicating and deleting Gear Lists.</summary>
public class ManagingGearListsTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public ManagingGearListsTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<GearListView> ViewAsync(string gearListId) =>
        await _app.Lists.GetGearListAsync(gearListId) ?? throw new InvalidOperationException("No such Gear List.");

    /// <summary>Each entry as (Category, Gear Item id, quantity, Carry Mode), in list order.</summary>
    private static (string Category, string GearItemId, int Quantity, CarryMode CarryMode)[] Entries(GearListView view) =>
        view.Sections
            .SelectMany(s => s.Entries.Select(e => (s.Category.Name, e.Entry.GearItemId, e.Entry.Quantity, e.Entry.CarryMode)))
            .ToArray();

    /// <summary>A "Summer" list with a tent in Shelter (quantity 1) and stakes in Shelter (quantity 6, worn).</summary>
    private async Task<(GearList List, GearItem Tent, GearItem Stakes)> SummerListAsync()
    {
        var tent = await _app.Gear.AddGearItemAsync(new GearItemDetails("Durston X-Mid 1", "Tent", 765m));
        var stakes = await _app.Gear.AddGearItemAsync(new GearItemDetails("MSR Groundhog", "Stakes", 10m));
        var list = await _app.Lists.CreateGearListAsync("Summer");
        var shelter = list.Categories[0].Id;
        await _app.Lists.AddEntryAsync(list.Id, tent.Id, shelter);
        var stakesEntry = await _app.Lists.AddEntryAsync(list.Id, stakes.Id, shelter);
        await _app.Lists.UpdateEntryAsync(list.Id, stakesEntry.Id, 6, CarryMode.Worn);
        return ((await ViewAsync(list.Id)).GearList, tent, stakes);
    }

    [Fact]
    public async Task A_gear_list_can_be_renamed()
    {
        var list = await _app.Lists.CreateGearListAsync("Summer");

        await _app.Lists.RenameGearListAsync(list.Id, "  Summer 2026 ");

        Assert.Equal("Summer 2026", (await ViewAsync(list.Id)).GearList.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_gear_list_cant_be_renamed_to_a_blank_name(string name)
    {
        var list = await _app.Lists.CreateGearListAsync("Summer");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Lists.RenameGearListAsync(list.Id, name));

        Assert.Equal("Give the Gear List a name.", refused.Message);
        Assert.Equal("Summer", (await ViewAsync(list.Id)).GearList.Name);
    }

    [Fact]
    public async Task Duplicating_copies_the_categories_and_entries_under_the_new_name()
    {
        var (summer, _, _) = await SummerListAsync();
        await _app.Lists.RenameCategoryAsync(summer.Id, summer.Categories[0].Id, "Big Three");

        var winter = await _app.Lists.DuplicateGearListAsync(summer.Id, " Winter ");

        var original = await ViewAsync(summer.Id);
        var copy = await ViewAsync(winter.Id);
        Assert.NotEqual(summer.Id, winter.Id);
        Assert.Equal("Winter", copy.GearList.Name);
        Assert.Equal(original.GearList.Categories.Select(c => c.Name), copy.GearList.Categories.Select(c => c.Name));
        Assert.Equal(Entries(original), Entries(copy));
        Assert.Equal(
            (original.Weight.BaseGrams, original.Weight.WornGrams, original.Weight.StartingPackGrams),
            (copy.Weight.BaseGrams, copy.Weight.WornGrams, copy.Weight.StartingPackGrams));
        Assert.Empty(copy.GearList.Entries.Select(e => e.Id).Intersect(original.GearList.Entries.Select(e => e.Id)));
    }

    [Fact]
    public async Task Changing_the_copy_doesnt_change_the_original()
    {
        var (summer, tent, _) = await SummerListAsync();
        var winter = await _app.Lists.DuplicateGearListAsync(summer.Id, "Winter");
        var tentEntry = winter.Entries.Single(e => e.GearItemId == tent.Id);

        await _app.Lists.UpdateEntryAsync(winter.Id, tentEntry.Id, 2, CarryMode.Base);
        await _app.Lists.RenameCategoryAsync(winter.Id, winter.Categories[0].Id, "Big Three");
        await _app.Lists.AddCategoryAsync(winter.Id, "Snow");

        var original = (await ViewAsync(summer.Id)).GearList;
        Assert.Equal(summer.Categories.Select(c => c.Name), original.Categories.Select(c => c.Name));
        Assert.Equal(1, original.Entries.Single(e => e.GearItemId == tent.Id).Quantity);
    }

    [Fact]
    public async Task A_duplicate_needs_a_name()
    {
        var list = await _app.Lists.CreateGearListAsync("Summer");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Lists.DuplicateGearListAsync(list.Id, " "));

        Assert.Equal("Give the Gear List a name.", refused.Message);
        Assert.Single(await _app.Lists.ListGearListsAsync());
    }

    [Fact]
    public async Task A_deleted_gear_list_is_gone_but_its_gear_stays()
    {
        var (summer, _, _) = await SummerListAsync();

        await _app.Lists.DeleteGearListAsync(summer.Id);

        Assert.Empty(await _app.Lists.ListGearListsAsync());
        Assert.Null(await _app.Lists.GetGearListAsync(summer.Id));
        Assert.Equal(2, (await _app.Gear.ListGearItemsAsync()).Count);
    }

    [Fact]
    public async Task Deleting_a_gear_list_keeps_a_tombstone_so_the_deletion_syncs()
    {
        // The tombstone is what sync (ticket 24) sends to other devices; until sync exists,
        // the store is the only place to see it.
        var list = await _app.Lists.CreateGearListAsync("Summer");
        var later = _clock.UtcNow.AddHours(2);
        _clock.UtcNow = later;

        await _app.Lists.DeleteGearListAsync(list.Id);

        var stored = await _store.LoadAsync<GearList>("gearLists", list.Id);
        Assert.NotNull(stored);
        Assert.True(stored.Deleted);
        Assert.Equal(later, stored.UpdatedAt);
    }

    [Fact]
    public async Task Gear_only_on_a_deleted_gear_list_can_be_deleted()
    {
        var (summer, tent, _) = await SummerListAsync();
        Assert.False((await _app.Gear.PlanGearItemDeletionAsync(tent.Id)).CanDelete);

        await _app.Lists.DeleteGearListAsync(summer.Id);
        await _app.Gear.DeleteGearItemAsync(tent.Id);

        Assert.Null(await _app.Gear.GetGearItemAsync(tent.Id));
    }

    [Fact]
    public async Task Gear_on_a_duplicate_cant_be_deleted_after_the_original_is()
    {
        var (summer, tent, _) = await SummerListAsync();
        await _app.Lists.DuplicateGearListAsync(summer.Id, "Winter");

        await _app.Lists.DeleteGearListAsync(summer.Id);

        var plan = await _app.Gear.PlanGearItemDeletionAsync(tent.Id);
        Assert.False(plan.CanDelete);
        Assert.Equal(["Gear List \"Winter\""], plan.UsedBy);
    }

    [Fact]
    public async Task A_deleted_gear_list_cant_be_changed()
    {
        var list = await _app.Lists.CreateGearListAsync("Summer");
        await _app.Lists.DeleteGearListAsync(list.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Lists.RenameGearListAsync(list.Id, "Winter"));

        Assert.Equal("That Gear List no longer exists.", refused.Message);
    }
}
