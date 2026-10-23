using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearCatalogueTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static Task<GearItem> AddGearItemAsync(AppCore app, string name, string? gearType, decimal weightGrams) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, gearType, weightGrams));

    [Fact]
    public async Task An_added_gear_item_appears_when_gear_items_are_listed()
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "Durston X-Mid 1", "Tent", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal("Durston X-Mid 1", item.Name);
        Assert.Equal("Tent", item.GearType);
        Assert.Equal(765m, item.WeightGrams);
    }

    [Fact]
    public async Task An_added_gear_item_gets_a_guid_id_and_is_stamped_with_the_current_time()
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "Durston X-Mid 1", "Tent", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.True(Guid.TryParse(item.Id, out _));
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero), item.UpdatedAt);
        Assert.False(item.Deleted);
    }

    [Fact]
    public async Task Each_gear_item_gets_its_own_id()
    {
        var app = CreateApp();

        var tent = await AddGearItemAsync(app, "Durston X-Mid 1", "Tent", 765m);
        var quilt = await AddGearItemAsync(app, "EE Revelation 20", "Quilt", 590m);

        Assert.NotEqual(tent.Id, quilt.Id);
    }

    [Theory]
    [InlineData(28.349, 28.3)]
    [InlineData(28.35, 28.4)]
    [InlineData(453.592, 453.6)]
    [InlineData(12.5, 12.5)]
    public async Task Gear_item_weights_are_stored_to_a_tenth_of_a_gram(decimal entered, decimal stored)
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "Toaks Titanium", "Spork", entered);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal(stored, item.WeightGrams);
    }

    [Fact]
    public async Task Gear_items_are_still_there_after_the_app_is_reopened()
    {
        await AddGearItemAsync(CreateApp(), "Durston X-Mid 1", "Tent", 765m);

        var item = Assert.Single(await CreateApp().Gear.ListGearItemsAsync());

        Assert.Equal(("Durston X-Mid 1", "Tent", 765m), (item.Name, item.GearType, item.WeightGrams));
    }

    [Fact]
    public async Task Gear_items_are_listed_by_gear_type_then_name_ignoring_case_with_no_gear_type_last()
    {
        var app = CreateApp();
        await AddGearItemAsync(app, "Mazama M!GO 1L", "Water Bottle", 120m);
        await AddGearItemAsync(app, "Electronics bag", null, 24m);
        await AddGearItemAsync(app, "SWD Long Haul 50", "backpack", 1361m);
        await AddGearItemAsync(app, "Igneous 1L", "Water Bottle", 70m);

        var names = (await app.Gear.ListGearItemsAsync()).Select(item => item.Name);

        Assert.Equal(["SWD Long Haul 50", "Igneous 1L", "Mazama M!GO 1L", "Electronics bag"], names);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_gear_item_needs_a_name(string name)
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<RuleViolationException>(() => AddGearItemAsync(app, name, "Tent", 765m));

        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task A_gear_item_cannot_weigh_less_than_nothing()
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<RuleViolationException>(() => AddGearItemAsync(app, "Durston X-Mid 1", "Tent", -1m));

        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Gear_type_is_optional(string? gearType)
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "Bandana", gearType, 28m);

        Assert.Equal("", Assert.Single(await app.Gear.ListGearItemsAsync()).GearType);
    }

    [Fact]
    public async Task Names_and_gear_types_are_trimmed()
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "  Durston X-Mid 1 ", " Tent  ", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal(("Durston X-Mid 1", "Tent"), (item.Name, item.GearType));
    }
}
