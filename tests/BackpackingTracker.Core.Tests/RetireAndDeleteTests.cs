using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class RetireAndDeleteTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<GearItem> AddAsync(AppCore app, string name, params string[] tags)
    {
        var shelter = (await app.Gear.ListCategoriesAsync()).Single(category => category.Name == "Shelter");
        return await app.Gear.AddGearItemAsync(new GearItemDetails(name, "", 100m, shelter.Id, tags));
    }

    private static async Task<string[]> CatalogueNamesAsync(AppCore app) =>
        (await app.Gear.ListGearItemsAsync()).Select(item => item.Name).ToArray();

    [Fact]
    public async Task A_retired_gear_item_leaves_the_catalogue()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        await AddAsync(app, "Stakes");

        await app.Gear.RetireGearItemAsync(tent.Id);

        Assert.Equal(["Stakes"], await CatalogueNamesAsync(CreateApp()));
        Assert.Equal(["Stakes"], (await app.Gear.ListGearItemsByCategoryAsync())
            .SelectMany(section => section.GearItems).Select(item => item.Name));
        Assert.Empty((await app.Gear.ListGearItemsByCategoryAsync(new GearFilter("tent"))));
    }

    [Fact]
    public async Task Retired_gear_items_are_listed_separately()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        var tarp = await AddAsync(app, "Tarp");
        await AddAsync(app, "Stakes");

        await app.Gear.RetireGearItemAsync(tent.Id);
        await app.Gear.RetireGearItemAsync(tarp.Id);

        var retired = await app.Gear.ListRetiredGearItemsAsync();
        Assert.Equal(["Tarp", "Tent"], retired.Select(item => item.Name));
        Assert.All(retired, item => Assert.True(item.Retired));
    }

    [Fact]
    public async Task A_retired_gear_item_can_be_un_retired()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        await app.Gear.RetireGearItemAsync(tent.Id);

        await app.Gear.UnretireGearItemAsync(tent.Id);

        Assert.Equal(["Tent"], await CatalogueNamesAsync(CreateApp()));
        Assert.Empty(await app.Gear.ListRetiredGearItemsAsync());
    }

    [Fact]
    public async Task Retiring_stamps_the_gear_item_with_the_current_time()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        var later = _clock.UtcNow.AddDays(3);
        _clock.UtcNow = later;

        await app.Gear.RetireGearItemAsync(tent.Id);

        Assert.Equal(later, Assert.Single(await app.Gear.ListRetiredGearItemsAsync()).UpdatedAt);
    }

    [Fact]
    public async Task A_retired_gear_item_can_still_be_looked_up_by_id()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        await app.Gear.RetireGearItemAsync(tent.Id);

        var found = await app.Gear.GetGearItemAsync(tent.Id);

        Assert.NotNull(found);
        Assert.Equal("Tent", found.Name);
        Assert.True(found.Retired);
    }

    [Fact]
    public async Task Tags_used_only_by_retired_gear_are_not_suggested()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent", "winter", "solo");
        await AddAsync(app, "Stakes", "solo");

        await app.Gear.RetireGearItemAsync(tent.Id);

        Assert.Equal(["solo"], await app.Gear.ListTagsAsync());
    }

    /// <summary>Stands in for Gear Lists and Trips, which will report the Gear Items they use.</summary>
    private sealed class FakeReferences(string gearItemId, params string[] uses) : IGearItemReferences
    {
        public Task<IReadOnlyList<string>> FindUsesAsync(string id) =>
            Task.FromResult<IReadOnlyList<string>>(id == gearItemId ? uses : []);
    }

    [Fact]
    public async Task An_unused_gear_item_can_be_deleted()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        await AddAsync(app, "Stakes");

        var plan = await app.Gear.PlanGearItemDeletionAsync(tent.Id);
        await app.Gear.DeleteGearItemAsync(tent.Id);

        Assert.True(plan.CanDelete);
        Assert.Equal("Delete Tent? This can't be undone.", plan.Message);
        Assert.Equal(["Stakes"], await CatalogueNamesAsync(CreateApp()));
        Assert.Null(await app.Gear.GetGearItemAsync(tent.Id));
    }

    [Fact]
    public async Task A_retired_gear_item_can_be_deleted()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        await app.Gear.RetireGearItemAsync(tent.Id);

        await app.Gear.DeleteGearItemAsync(tent.Id);

        Assert.Empty(await app.Gear.ListRetiredGearItemsAsync());
    }

    [Fact]
    public async Task Deleting_keeps_a_tombstone_so_the_deletion_syncs()
    {
        // The tombstone is what sync (ticket 24) sends to other devices; until sync exists,
        // the store is the only place to see it.
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        var later = _clock.UtcNow.AddHours(2);
        _clock.UtcNow = later;

        await app.Gear.DeleteGearItemAsync(tent.Id);

        var stored = await _store.LoadAsync<GearItem>("gearItems", tent.Id);
        Assert.NotNull(stored);
        Assert.True(stored.Deleted);
        Assert.Equal(later, stored.UpdatedAt);
    }

    [Fact]
    public async Task A_gear_item_that_something_uses_cannot_be_deleted_and_retire_is_offered()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        app.Gear.AddReferences(new FakeReferences(tent.Id, "Gear List \"Summer kit\"", "Trip \"Sierra 2026\""));

        var plan = await app.Gear.PlanGearItemDeletionAsync(tent.Id);
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.DeleteGearItemAsync(tent.Id));

        Assert.False(plan.CanDelete);
        Assert.Equal(["Gear List \"Summer kit\"", "Trip \"Sierra 2026\""], plan.UsedBy);
        Assert.Equal(
            "Tent can't be deleted because Gear List \"Summer kit\" and Trip \"Sierra 2026\" use it. Retire it instead?",
            plan.Message);
        Assert.Equal(plan.Message, refused.Message);
        Assert.Equal(["Tent"], await CatalogueNamesAsync(app));
    }

    [Fact]
    public async Task Retire_is_not_offered_for_used_gear_that_is_already_retired()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        await app.Gear.RetireGearItemAsync(tent.Id);
        app.Gear.AddReferences(new FakeReferences(tent.Id, "Trip \"Sierra 2026\""));

        var plan = await app.Gear.PlanGearItemDeletionAsync(tent.Id);

        Assert.Equal("Tent can't be deleted because Trip \"Sierra 2026\" uses it.", plan.Message);
    }

    [Fact]
    public async Task The_deletion_plan_says_how_to_ask_the_backpacker()
    {
        var app = CreateApp();
        var unused = await AddAsync(app, "Stakes");
        var used = await AddAsync(app, "Tent");
        var usedAndRetired = await AddAsync(app, "Tarp");
        await app.Gear.RetireGearItemAsync(usedAndRetired.Id);
        app.Gear.AddReferences(new FakeReferences(used.Id, "Trip \"Sierra 2026\""));
        app.Gear.AddReferences(new FakeReferences(usedAndRetired.Id, "Trip \"Sierra 2026\""));

        var canDelete = await app.Gear.PlanGearItemDeletionAsync(unused.Id);
        var offersRetire = await app.Gear.PlanGearItemDeletionAsync(used.Id);
        var cannotDelete = await app.Gear.PlanGearItemDeletionAsync(usedAndRetired.Id);

        Assert.Equal(("Delete Gear Item", "Delete"), (canDelete.Title, canDelete.ConfirmButton));
        Assert.Equal(("Can't delete", "Retire"), (offersRetire.Title, offersRetire.ConfirmButton));
        Assert.Equal(("Can't delete", null), (cannotDelete.Title, cannotDelete.ConfirmButton));
    }

    [Fact]
    public async Task Uses_reported_by_every_area_are_combined()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        app.Gear.AddReferences(new FakeReferences(tent.Id, "Gear List \"Summer kit\""));
        app.Gear.AddReferences(new FakeReferences(tent.Id, "Trip \"Sierra 2026\""));
        app.Gear.AddReferences(new FakeReferences("another-gear-item", "Gear List \"Winter kit\""));

        var plan = await app.Gear.PlanGearItemDeletionAsync(tent.Id);

        Assert.Equal(["Gear List \"Summer kit\"", "Trip \"Sierra 2026\""], plan.UsedBy);
    }

    [Fact]
    public async Task Deleting_a_gear_item_that_no_longer_exists_is_refused()
    {
        var app = CreateApp();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.DeleteGearItemAsync("gone"));

        Assert.Equal("That Gear Item no longer exists.", refused.Message);
    }

    [Fact]
    public async Task Retired_gear_items_still_need_moving_when_their_category_is_removed()
    {
        var app = CreateApp();
        var tent = await AddAsync(app, "Tent");
        await app.Gear.RetireGearItemAsync(tent.Id);
        var shelter = (await app.Gear.ListCategoriesAsync()).Single(category => category.Name == "Shelter");

        var plan = await app.Gear.PlanCategoryRemovalAsync(shelter.Id);

        Assert.True(plan.NeedsMoveTarget);
        Assert.Equal(1, plan.GearItemCount);
    }
}
