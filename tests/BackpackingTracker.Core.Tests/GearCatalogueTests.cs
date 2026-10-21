using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearCatalogueTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    [Fact]
    public async Task An_added_gear_item_appears_when_gear_items_are_listed()
    {
        var app = CreateApp();

        await app.Gear.AddGearItemAsync("Tent", "Durston X-Mid 1", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal("Tent", item.Name);
        Assert.Equal("Durston X-Mid 1", item.BrandModel);
        Assert.Equal(765m, item.WeightGrams);
    }

    [Fact]
    public async Task An_added_gear_item_gets_a_guid_id_and_is_stamped_with_the_current_time()
    {
        var app = CreateApp();

        await app.Gear.AddGearItemAsync("Tent", "Durston X-Mid 1", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.True(Guid.TryParse(item.Id, out _));
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero), item.UpdatedAt);
        Assert.False(item.Deleted);
    }

    [Fact]
    public async Task Each_gear_item_gets_its_own_id()
    {
        var app = CreateApp();

        var tent = await app.Gear.AddGearItemAsync("Tent", "Durston X-Mid 1", 765m);
        var quilt = await app.Gear.AddGearItemAsync("Quilt", "EE Revelation 20", 590m);

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

        await app.Gear.AddGearItemAsync("Spork", "Toaks Titanium", entered);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal(stored, item.WeightGrams);
    }

    [Fact]
    public async Task Gear_items_are_still_there_after_the_app_is_reopened()
    {
        var firstSession = CreateApp();
        await firstSession.Gear.AddGearItemAsync("Tent", "Durston X-Mid 1", 765m);

        var secondSession = CreateApp();

        var item = Assert.Single(await secondSession.Gear.ListGearItemsAsync());
        Assert.Equal("Tent", item.Name);
        Assert.Equal("Durston X-Mid 1", item.BrandModel);
        Assert.Equal(765m, item.WeightGrams);
    }

    [Fact]
    public async Task Gear_items_are_listed_by_name_ignoring_case()
    {
        var app = CreateApp();
        await app.Gear.AddGearItemAsync("tent", "Durston X-Mid 1", 765m);
        await app.Gear.AddGearItemAsync("Quilt", "EE Revelation 20", 590m);
        await app.Gear.AddGearItemAsync("Backpack", "Gossamer Gear Mariposa", 870m);

        var names = (await app.Gear.ListGearItemsAsync()).Select(item => item.Name);

        Assert.Equal(["Backpack", "Quilt", "tent"], names);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_gear_item_needs_a_name(string name)
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<ArgumentException>(
            () => app.Gear.AddGearItemAsync(name, "Durston X-Mid 1", 765m));

        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task A_gear_item_cannot_weigh_less_than_nothing()
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => app.Gear.AddGearItemAsync("Tent", "Durston X-Mid 1", -1m));

        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task Names_and_brand_models_are_trimmed()
    {
        var app = CreateApp();

        await app.Gear.AddGearItemAsync("  Tent ", " Durston X-Mid 1  ", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal("Tent", item.Name);
        Assert.Equal("Durston X-Mid 1", item.BrandModel);
    }
}
