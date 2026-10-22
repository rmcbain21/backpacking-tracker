using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearCatalogueTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<GearItem> AddGearItemAsync(AppCore app, string name, string? brandModel, decimal weightGrams)
    {
        var shelter = (await app.Gear.ListCategoriesAsync()).First(category => category.Name == "Shelter");
        return await app.Gear.AddGearItemAsync(new GearItemDetails(name, brandModel, weightGrams, shelter.Id));
    }

    [Fact]
    public async Task An_added_gear_item_appears_when_gear_items_are_listed()
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "Tent", "Durston X-Mid 1", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal("Tent", item.Name);
        Assert.Equal("Durston X-Mid 1", item.BrandModel);
        Assert.Equal(765m, item.WeightGrams);
    }

    [Fact]
    public async Task An_added_gear_item_gets_a_guid_id_and_is_stamped_with_the_current_time()
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "Tent", "Durston X-Mid 1", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.True(Guid.TryParse(item.Id, out _));
        Assert.Equal(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero), item.UpdatedAt);
        Assert.False(item.Deleted);
    }

    [Fact]
    public async Task Each_gear_item_gets_its_own_id()
    {
        var app = CreateApp();

        var tent = await AddGearItemAsync(app, "Tent", "Durston X-Mid 1", 765m);
        var quilt = await AddGearItemAsync(app, "Quilt", "EE Revelation 20", 590m);

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

        await AddGearItemAsync(app, "Spork", "Toaks Titanium", entered);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal(stored, item.WeightGrams);
    }

    [Fact]
    public async Task Gear_items_are_still_there_after_the_app_is_reopened()
    {
        var firstSession = CreateApp();
        await AddGearItemAsync(firstSession, "Tent", "Durston X-Mid 1", 765m);

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
        await AddGearItemAsync(app, "tent", "Durston X-Mid 1", 765m);
        await AddGearItemAsync(app, "Quilt", "EE Revelation 20", 590m);
        await AddGearItemAsync(app, "Backpack", "Gossamer Gear Mariposa", 870m);

        var names = (await app.Gear.ListGearItemsAsync()).Select(item => item.Name);

        Assert.Equal(["Backpack", "Quilt", "tent"], names);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_gear_item_needs_a_name(string name)
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<RuleViolationException>(
            () => AddGearItemAsync(app, name, "Durston X-Mid 1", 765m));

        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task A_gear_item_cannot_weigh_less_than_nothing()
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<RuleViolationException>(
            () => AddGearItemAsync(app, "Tent", "Durston X-Mid 1", -1m));

        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Brand_model_is_optional(string? brandModel)
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "Bandana", brandModel, 28m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal("", item.BrandModel);
    }

    [Fact]
    public async Task A_gear_item_belongs_to_the_category_chosen_for_it()
    {
        var app = CreateApp();
        var cook = (await app.Gear.ListCategoriesAsync()).Single(category => category.Name == "Cook");

        await app.Gear.AddGearItemAsync(new GearItemDetails("Stove", "BRS-3000T", 25m, cook.Id));

        Assert.Equal(cook.Id, Assert.Single(await app.Gear.ListGearItemsAsync()).CategoryId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-category")]
    public async Task A_gear_item_needs_a_category_that_exists(string categoryId)
    {
        var app = CreateApp();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.AddGearItemAsync(new GearItemDetails("Stove", "BRS-3000T", 25m, categoryId)));

        Assert.Equal("Choose a Category.", refused.Message);
        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task Gear_items_saved_before_categories_existed_are_in_misc()
    {
        // Tickets 01-03 saved Gear Items with no Category, and the API can no longer create one,
        // so this test writes such a record to the store directly.
        await _store.SaveAsync("gearItems", new GearItem
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = "Tent",
            WeightGrams = 765m,
        });
        var app = CreateApp();
        var misc = (await app.Gear.ListCategoriesAsync()).Single(category => category.Name == "Misc");

        Assert.Equal(misc.Id, Assert.Single(await app.Gear.ListGearItemsAsync()).CategoryId);
    }

    [Fact]
    public async Task Names_and_brand_models_are_trimmed()
    {
        var app = CreateApp();

        await AddGearItemAsync(app, "  Tent ", " Durston X-Mid 1  ", 765m);

        var item = Assert.Single(await app.Gear.ListGearItemsAsync());
        Assert.Equal("Tent", item.Name);
        Assert.Equal("Durston X-Mid 1", item.BrandModel);
    }
}
