using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearEditingTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<string> CategoryIdAsync(AppCore app, string name) =>
        (await app.Gear.ListCategoriesAsync()).Single(category => category.Name == name).Id;

    private static async Task<GearItem> AddTentAsync(AppCore app) =>
        await app.Gear.AddGearItemAsync(new GearItemDetails("Tent", "Durston X-Mid 1", 765m, await CategoryIdAsync(app, "Shelter")));

    [Fact]
    public async Task Price_and_notes_are_optional_and_kept_when_given()
    {
        var app = CreateApp();
        var shelter = await CategoryIdAsync(app, "Shelter");

        var tent = await app.Gear.AddGearItemAsync(new GearItemDetails(
            "Tent", "Durston X-Mid 1", 765m, shelter, Price: 299.999m, Notes: "  Maker says 794 g  "));
        var stakes = await app.Gear.AddGearItemAsync(new GearItemDetails("Stakes", "", 60m, shelter));

        Assert.Equal(300.00m, tent.Price);
        Assert.Equal("Maker says 794 g", tent.Notes);
        Assert.Null(stakes.Price);
        Assert.Equal("", stakes.Notes);
    }

    [Fact]
    public async Task A_price_cannot_be_negative()
    {
        var app = CreateApp();
        var shelter = await CategoryIdAsync(app, "Shelter");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.AddGearItemAsync(
            new GearItemDetails("Tent", "", 765m, shelter, Price: -1m)));

        Assert.Equal("A price can't be negative.", refused.Message);
    }

    [Fact]
    public async Task Every_field_of_a_gear_item_can_be_edited()
    {
        var app = CreateApp();
        var tent = await AddTentAsync(app);
        var sleep = await CategoryIdAsync(app, "Sleep");
        var later = _clock.UtcNow.AddDays(1);
        _clock.UtcNow = later;

        await app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(
            " Bivy ", " MLD Superlight ", 200.04m, sleep, [" winter ", "Winter"], Price: 150m, Notes: "Used"));

        var edited = Assert.Single(await CreateApp().Gear.ListGearItemsAsync());
        Assert.Equal(tent.Id, edited.Id);
        Assert.Equal("Bivy", edited.Name);
        Assert.Equal("MLD Superlight", edited.BrandModel);
        Assert.Equal(200.0m, edited.WeightGrams);
        Assert.Equal(sleep, edited.CategoryId);
        Assert.Equal(["winter"], edited.Tags);
        Assert.Equal(150m, edited.Price);
        Assert.Equal("Used", edited.Notes);
        Assert.Equal(later, edited.UpdatedAt);
    }

    [Fact]
    public async Task Editing_applies_the_same_rules_as_adding()
    {
        var app = CreateApp();
        var tent = await AddTentAsync(app);

        var noName = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(" ", "", 765m, tent.CategoryId)));
        var noCategory = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Tent", "", 765m, "")));

        Assert.Equal("Give the Gear Item a name.", noName.Message);
        Assert.Equal("Choose a Category.", noCategory.Message);
        Assert.Equal("Tent", Assert.Single(await app.Gear.ListGearItemsAsync()).Name);
    }

    [Fact]
    public async Task Editing_a_gear_item_that_no_longer_exists_is_refused()
    {
        var app = CreateApp();

        var shelter = await CategoryIdAsync(app, "Shelter");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.UpdateGearItemAsync(
            "not-a-gear-item", new GearItemDetails("Tent", "", 765m, shelter)));

        Assert.Equal("That Gear Item no longer exists.", refused.Message);
        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }
}
