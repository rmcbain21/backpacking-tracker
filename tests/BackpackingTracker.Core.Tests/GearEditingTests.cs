using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearEditingTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static Task<GearItem> AddTentAsync(AppCore app) =>
        app.Gear.AddGearItemAsync(new GearItemDetails("Durston X-Mid 1", "Tent", 765m));

    [Fact]
    public async Task Price_and_notes_are_optional_and_kept_when_given()
    {
        var app = CreateApp();

        var tent = await app.Gear.AddGearItemAsync(new GearItemDetails(
            "Durston X-Mid 1", "Tent", 765m, Price: 299.999m, Notes: "  Maker says 794 g  "));
        var stakes = await app.Gear.AddGearItemAsync(new GearItemDetails("MSR Groundhog", "Stakes", 60m));

        Assert.Equal(300.00m, tent.Price);
        Assert.Equal("Maker says 794 g", tent.Notes);
        Assert.Null(stakes.Price);
        Assert.Equal("", stakes.Notes);
    }

    [Fact]
    public async Task A_price_cannot_be_negative()
    {
        var app = CreateApp();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.AddGearItemAsync(
            new GearItemDetails("Durston X-Mid 1", "Tent", 765m, Price: -1m)));

        Assert.Equal("A price can't be negative.", refused.Message);
    }

    [Fact]
    public async Task Every_field_of_a_gear_item_can_be_edited()
    {
        var app = CreateApp();
        var tent = await AddTentAsync(app);
        var later = _clock.UtcNow.AddDays(1);
        _clock.UtcNow = later;

        await app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(
            " MLD Superlight ", " Bivy ", 200.04m, [" winter ", "Winter"], Price: 150m, Notes: "Used"));

        var edited = Assert.Single(await CreateApp().Gear.ListGearItemsAsync());
        Assert.Equal(tent.Id, edited.Id);
        Assert.Equal("MLD Superlight", edited.Name);
        Assert.Equal("Bivy", edited.GearType);
        Assert.Equal(200.0m, edited.WeightGrams);
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
            () => app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(" ", "Tent", 765m)));
        var negative = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Durston X-Mid 1", "Tent", -5m)));

        Assert.Equal("Give the Gear Item a name.", noName.Message);
        Assert.Equal("A weight can't be negative.", negative.Message);
        Assert.Equal("Durston X-Mid 1", Assert.Single(await app.Gear.ListGearItemsAsync()).Name);
    }

    [Fact]
    public async Task Editing_a_gear_item_that_no_longer_exists_is_refused()
    {
        var app = CreateApp();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.UpdateGearItemAsync(
            "not-a-gear-item", new GearItemDetails("Durston X-Mid 1", "Tent", 765m)));

        Assert.Equal("That Gear Item no longer exists.", refused.Message);
        Assert.Empty(await app.Gear.ListGearItemsAsync());
    }
}
