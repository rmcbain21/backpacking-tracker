using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Weight;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearListTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<GearItem> AddGearAsync(AppCore app, string name, decimal grams)
    {
        var misc = (await app.Gear.ListCategoriesAsync()).Single(category => category.Name == "Misc");
        return await app.Gear.AddGearItemAsync(new GearItemDetails(name, "", grams, misc.Id));
    }

    private static async Task<GearListView> ViewAsync(AppCore app, GearList list) =>
        (await app.Lists.GetGearListAsync(list.Id))!;

    [Fact]
    public async Task A_new_gear_list_has_its_name_and_no_entries()
    {
        var app = CreateApp();

        var list = await app.Lists.CreateGearListAsync("  Summer kit ");

        var view = await ViewAsync(CreateApp(), list);
        Assert.Equal("Summer kit", view.GearList.Name);
        Assert.Empty(view.Entries);
        Assert.Equal(0m, view.Weight.StartingPackGrams);
        Assert.True(Guid.TryParse(list.Id, out _));
        Assert.Equal(_clock.UtcNow, list.UpdatedAt);
    }

    [Fact]
    public async Task A_gear_list_needs_a_name()
    {
        var app = CreateApp();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Lists.CreateGearListAsync("  "));

        Assert.Equal("Give the Gear List a name.", refused.Message);
        Assert.Empty(await app.Lists.ListGearListsAsync());
    }

    [Fact]
    public async Task Gear_lists_are_listed_by_name_with_their_weights()
    {
        var app = CreateApp();
        var winter = await app.Lists.CreateGearListAsync("winter kit");
        await app.Lists.CreateGearListAsync("Summer kit");
        await app.Lists.AddEntryAsync(winter.Id, (await AddGearAsync(app, "Tent", 907m)).Id);

        var lists = await app.Lists.ListGearListsAsync();

        Assert.Equal(["Summer kit", "winter kit"], lists.Select(view => view.GearList.Name));
        Assert.Equal(907m, lists[1].Weight.StartingPackGrams);
    }

    [Fact]
    public async Task An_added_entry_has_quantity_one_and_carry_mode_base()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var tent = await AddGearAsync(app, "Tent", 907m);

        await app.Lists.AddEntryAsync(list.Id, tent.Id);

        var entry = Assert.Single((await ViewAsync(CreateApp(), list)).Entries);
        Assert.Equal(tent.Id, entry.GearItem!.Id);
        Assert.Equal(1, entry.Entry.Quantity);
        Assert.Equal(CarryMode.Base, entry.Entry.CarryMode);
        Assert.Equal(907m, entry.EntryWeightGrams);
    }

    [Fact]
    public async Task Retired_gear_cannot_be_added()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var tarp = await AddGearAsync(app, "Tarp", 340m);
        await app.Gear.RetireGearItemAsync(tarp.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Lists.AddEntryAsync(list.Id, tarp.Id));

        Assert.Equal("Tarp is Retired. Un-retire it to add it to a Gear List.", refused.Message);
        Assert.Empty((await ViewAsync(app, list)).Entries);
    }

    [Fact]
    public async Task Gear_that_does_not_exist_cannot_be_added()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Lists.AddEntryAsync(list.Id, "gone"));

        Assert.Equal("That Gear Item no longer exists.", refused.Message);
    }

    [Fact]
    public async Task An_entry_quantity_and_carry_mode_can_be_changed()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var socks = await AddGearAsync(app, "Socks", 76m);
        var entry = await app.Lists.AddEntryAsync(list.Id, socks.Id);
        var later = _clock.UtcNow.AddHours(1);
        _clock.UtcNow = later;

        await app.Lists.UpdateEntryAsync(list.Id, entry.Id, quantity: 2, CarryMode.Worn);

        var view = await ViewAsync(CreateApp(), list);
        var changed = Assert.Single(view.Entries);
        Assert.Equal(2, changed.Entry.Quantity);
        Assert.Equal(CarryMode.Worn, changed.Entry.CarryMode);
        Assert.Equal(152m, changed.EntryWeightGrams);
        Assert.Equal(152m, view.Weight.WornGrams);
        Assert.Equal(later, view.GearList.UpdatedAt);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task An_entry_quantity_must_be_at_least_one(int quantity)
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var entry = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Socks", 76m)).Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Lists.UpdateEntryAsync(list.Id, entry.Id, quantity, CarryMode.Base));

        Assert.Equal("Quantity must be 1 or more.", refused.Message);
        Assert.Equal(1, Assert.Single((await ViewAsync(app, list)).Entries).Entry.Quantity);
    }

    [Fact]
    public async Task An_entry_can_be_removed()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var tent = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Tent", 907m)).Id);
        await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Quilt", 590m)).Id);

        await app.Lists.RemoveEntryAsync(list.Id, tent.Id);

        var view = await ViewAsync(CreateApp(), list);
        Assert.Equal(["Quilt"], view.Entries.Select(entry => entry.GearItem!.Name));
        Assert.Equal(590m, view.Weight.BaseGrams);
    }

    [Fact]
    public async Task The_same_gear_item_can_have_a_different_carry_mode_on_another_list()
    {
        var app = CreateApp();
        var puffy = await AddGearAsync(app, "Puffy", 283m);
        var summer = await app.Lists.CreateGearListAsync("Summer kit");
        var winter = await app.Lists.CreateGearListAsync("Winter kit");
        await app.Lists.AddEntryAsync(summer.Id, puffy.Id);
        var worn = await app.Lists.AddEntryAsync(winter.Id, puffy.Id);

        await app.Lists.UpdateEntryAsync(winter.Id, worn.Id, 1, CarryMode.Worn);

        Assert.Equal(283m, (await ViewAsync(app, summer)).Weight.BaseGrams);
        Assert.Equal(283m, (await ViewAsync(app, winter)).Weight.WornGrams);
        Assert.Equal(0m, (await ViewAsync(app, winter)).Weight.BaseGrams);
    }

    [Fact]
    public async Task Adding_gear_already_on_the_list_adds_one_to_its_quantity()
    {
        var app = CreateApp();
        var socks = await AddGearAsync(app, "Socks", 76m);
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var first = await app.Lists.AddEntryAsync(list.Id, socks.Id);
        await app.Lists.UpdateEntryAsync(list.Id, first.Id, 1, CarryMode.Worn);

        var second = await app.Lists.AddEntryAsync(list.Id, socks.Id);

        var entry = Assert.Single((await ViewAsync(app, list)).Entries);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal(2, entry.Entry.Quantity);
        Assert.Equal(CarryMode.Worn, entry.Entry.CarryMode);
        Assert.Equal(152m, entry.EntryWeightGrams);
    }

    [Fact]
    public async Task An_unknown_carry_mode_is_refused()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var entry = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Socks", 76m)).Id);

        await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Lists.UpdateEntryAsync(list.Id, entry.Id, 1, (CarryMode)7));

        Assert.Equal(CarryMode.Base, Assert.Single((await ViewAsync(app, list)).Entries).Entry.CarryMode);
    }

    [Fact]
    public async Task Re_weighing_a_gear_item_updates_every_gear_list()
    {
        var app = CreateApp();
        var tent = await AddGearAsync(app, "Tent", 907m);
        var summer = await app.Lists.CreateGearListAsync("Summer kit");
        var winter = await app.Lists.CreateGearListAsync("Winter kit");
        await app.Lists.AddEntryAsync(summer.Id, tent.Id);
        await app.Lists.AddEntryAsync(winter.Id, tent.Id);

        await app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Tent", "", 880m, tent.CategoryId));

        Assert.Equal(880m, (await ViewAsync(app, summer)).Weight.BaseGrams);
        Assert.Equal(880m, (await ViewAsync(app, winter)).Weight.BaseGrams);
    }

    [Fact]
    public async Task A_retired_gear_item_already_on_a_list_still_counts()
    {
        var app = CreateApp();
        var tent = await AddGearAsync(app, "Tent", 907m);
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        await app.Lists.AddEntryAsync(list.Id, tent.Id);

        await app.Gear.RetireGearItemAsync(tent.Id);

        var entry = Assert.Single((await ViewAsync(app, list)).Entries);
        Assert.True(entry.GearItem!.Retired);
        Assert.Equal(907m, (await ViewAsync(app, list)).Weight.BaseGrams);
    }

    [Fact]
    public async Task An_entry_whose_gear_item_was_deleted_on_another_device_weighs_nothing()
    {
        // Sync can bring in another device's deletion of a Gear Item this list uses; the API
        // refuses that delete, so this test writes the tombstone to the store directly.
        var app = CreateApp();
        var tent = await AddGearAsync(app, "Tent", 907m);
        var quilt = await AddGearAsync(app, "Quilt", 590m);
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        await app.Lists.AddEntryAsync(list.Id, tent.Id);
        await app.Lists.AddEntryAsync(list.Id, quilt.Id);
        await _store.SaveAsync("gearItems", tent with { Deleted = true });

        var view = await ViewAsync(app, list);

        Assert.Null(view.Entries[0].GearItem);
        Assert.Equal(0m, view.Entries[0].EntryWeightGrams);
        Assert.Equal(590m, view.Weight.BaseGrams);
    }

    [Fact]
    public async Task The_breakdown_follows_each_gear_item_current_category()
    {
        var app = CreateApp();
        var categories = await app.Gear.ListCategoriesAsync();
        var shelter = categories.Single(category => category.Name == "Shelter");
        var sleep = categories.Single(category => category.Name == "Sleep");
        var bigThree = await app.Gear.AddCategoryAsync("Big Three");
        var tent = await app.Gear.AddGearItemAsync(new GearItemDetails("Tent", "", 900m, shelter.Id));
        var quilt = await app.Gear.AddGearItemAsync(new GearItemDetails("Quilt", "", 600m, sleep.Id));
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        await app.Lists.AddEntryAsync(list.Id, tent.Id);
        await app.Lists.AddEntryAsync(list.Id, quilt.Id);

        await app.Gear.UpdateGearItemAsync(quilt.Id, new GearItemDetails("Quilt", "", 600m, bigThree.Id));

        var lines = (await ViewAsync(app, list)).Weight.ByCategory;
        Assert.Equal(["Shelter", "Big Three"], lines.Select(line => line.Category.Name));
        Assert.Equal([0.6m, 0.4m], lines.Select(line => line.Share));
    }

    [Fact]
    public async Task An_unknown_gear_list_is_not_found()
    {
        Assert.Null(await CreateApp().Lists.GetGearListAsync("not-a-list"));
    }
}
