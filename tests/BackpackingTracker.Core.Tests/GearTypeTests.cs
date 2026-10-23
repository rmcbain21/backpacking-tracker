using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearTypeTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static Task<GearItem> AddAsync(AppCore app, string name, string gearType) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, gearType, 100m));

    [Fact]
    public async Task A_new_gear_type_takes_the_spelling_of_one_already_in_use()
    {
        var app = CreateApp();
        await AddAsync(app, "USB C Cord", "Power Cord");

        var cord = await AddAsync(app, "USB-C cord w/adapter", " power cord ");

        Assert.Equal("Power Cord", cord.GearType);
    }

    [Fact]
    public async Task Gear_types_in_use_are_suggested_once_each_in_alphabetical_order()
    {
        var app = CreateApp();
        await AddAsync(app, "Igneous 1L", "Water Bottle");
        await AddAsync(app, "Mazama M!GO 1L", "water bottle");
        await AddAsync(app, "SWD Long Haul 50", "Backpack");
        await app.Gear.AddGearItemAsync(new GearItemDetails("Electronics bag", null, 24m));

        Assert.Equal(["Backpack", "Water Bottle"], await app.Gear.ListGearTypesAsync());
    }

    [Fact]
    public async Task Gear_types_used_only_by_retired_gear_are_not_suggested()
    {
        var app = CreateApp();
        var tarp = await AddAsync(app, "HG Hex Tarp", "Tarp");
        await AddAsync(app, "Nemo Dragonfly", "Tent");

        await app.Gear.RetireGearItemAsync(tarp.Id);

        Assert.Equal(["Tent"], await app.Gear.ListGearTypesAsync());
    }

    // Gear Items saved before Gear Types (tickets 01-26) had a name, a brand/model and a Category, and the API
    // can no longer create one, so these tests write such records to the store directly.
    private Task SaveOldRecordAsync(string id, string name, string brandModel, string? categoryId = null) =>
        _store.SaveAsync("gearItems", new GearItem
        {
            Id = id,
            UpdatedAt = _clock.UtcNow,
            Name = name,
            WeightGrams = 765m,
            LegacyBrandModel = brandModel,
            LegacyCategoryId = categoryId,
        });

    [Fact]
    public async Task An_old_gear_item_name_becomes_its_gear_type_and_its_brand_model_its_name()
    {
        await SaveOldRecordAsync("old-tent", "Tent", "Durston X-Mid 1");

        var tent = Assert.Single(await CreateApp().Gear.ListGearItemsAsync());

        Assert.Equal(("Durston X-Mid 1", "Tent"), (tent.Name, tent.GearType));
        Assert.Equal(765m, tent.WeightGrams);
    }

    [Fact]
    public async Task An_old_gear_item_with_no_brand_model_keeps_its_name_as_both()
    {
        await SaveOldRecordAsync("old-bandana", "Bandana", "");

        var bandana = Assert.Single(await CreateApp().Gear.ListGearItemsAsync());

        Assert.Equal(("Bandana", "Bandana"), (bandana.Name, bandana.GearType));
    }

    [Fact]
    public async Task An_old_gear_item_is_converted_only_once_after_it_is_edited()
    {
        await SaveOldRecordAsync("old-tent", "Tent", "Durston X-Mid 1");
        var app = CreateApp();
        var tent = Assert.Single(await app.Gear.ListGearItemsAsync());

        await app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(tent.Name, tent.GearType, 760m));

        var edited = Assert.Single(await CreateApp().Gear.ListGearItemsAsync());
        Assert.Equal(("Durston X-Mid 1", "Tent", 760m), (edited.Name, edited.GearType, edited.WeightGrams));
    }

    [Fact]
    public async Task Old_gear_items_sort_and_search_by_their_converted_gear_type()
    {
        await SaveOldRecordAsync("old-tent", "Tent", "Durston X-Mid 1");
        await SaveOldRecordAsync("old-quilt", "Quilt", "EE Revelation 20");
        var app = CreateApp();

        Assert.Equal(["EE Revelation 20", "Durston X-Mid 1"], (await app.Gear.ListGearItemsAsync()).Select(item => item.Name));
        Assert.Equal(["Durston X-Mid 1"], (await app.Gear.ListGearItemsAsync(new GearFilter(GearType: "tent"))).Select(item => item.Name));
    }
}
