using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Servings per package on a Food Item: entered optionally, shown as packages, and rounded up on the shopping list.</summary>
public class ServingsPerPackageTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public ServingsPerPackageTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private Task<FoodItem> PouchAsync(decimal? perPackage = 2m) =>
        _app.Food.AddFoodItemAsync(new FoodItemDetails("Peak Refuel Chili Mac", 98m, 500m, ServingsPerPackage: perPackage));

    // ----- On the Food Item -----

    [Fact]
    public async Task Servings_per_package_is_optional_and_kept()
    {
        var pouch = await PouchAsync(2.004m);
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));

        var reloaded = await new AppCore(_store, _clock).Food.GetFoodItemAsync(pouch.Id);

        Assert.Equal(2m, reloaded?.ServingsPerPackage);
        Assert.Null(bar.ServingsPerPackage);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Servings_per_package_must_be_more_than_zero(decimal perPackage)
    {
        await Assert.ThrowsAsync<RuleViolationException>(() => PouchAsync(perPackage));
    }

    // ----- Showing amounts -----

    [Theory]
    [InlineData(6, 2, "3 packages (6 servings)")]
    [InlineData(3, 2, "1.5 packages (3 servings)")]
    [InlineData(2, 2, "1 package (2 servings)")]
    [InlineData(1, null, "1 serving")]
    [InlineData(2.5, null, "2.5 servings")]
    public void An_amount_shows_packages_when_known(decimal servings, int? perPackage, string expected)
    {
        Assert.Equal(expected, ServingsInput.Amount(servings, perPackage));
    }

    [Theory]
    [InlineData(3, 2, "2 packages (3 of 4 servings)")]
    [InlineData(4, 2, "2 packages (4 servings)")]
    [InlineData(0.5, 2, "1 package (0.5 of 2 servings)")]
    [InlineData(3, null, "3 servings")]
    public void What_to_buy_rounds_up_to_whole_packages(decimal servings, int? perPackage, string expected)
    {
        Assert.Equal(expected, ServingsInput.ToBuy(servings, perPackage));
    }

    [Fact]
    public void Blank_servings_per_package_reads_as_none()
    {
        Assert.Equal(new ServingsParseResult(null, null), ServingsInput.ParsePerPackage("  "));
        Assert.Equal(2m, ServingsInput.ParsePerPackage("2").Servings);
        Assert.NotNull(ServingsInput.ParsePerPackage("0").Error);
    }

    // ----- In a Trip -----

    [Fact]
    public async Task The_shopping_list_rounds_up_to_whole_packages_including_meal_components()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(11));
        var pouch = await PouchAsync();
        var oats = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Oats", 40m, 150m, ServingsPerPackage: 8m));
        var breakfast = await _app.Meals.CreateMealAsync(new MealDetails("Oats bowl", [new(oats.Id, 2m)]));
        for (var day = 9; day <= 11; day++)
        {
            await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(day), MealSlot.Dinner, pouch.Id);
            await _app.Trips.AddMealToSlotAsync(trip.Id, July(day), MealSlot.Breakfast, breakfast.Id);
        }

        var lines = Assert.Single((await _app.Trips.GetTripAsync(trip.Id))!.MealPlan.FoodCarries).Lines;

        Assert.Equal(
            [("Oats", 6m, (decimal?)1m), ("Peak Refuel Chili Mac", 3m, 2m)],
            lines.Select(line => (line.Name!, line.Servings, line.Packages)));
    }

    [Fact]
    public async Task A_slot_entry_knows_its_food_items_servings_per_package()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));
        var pouch = await PouchAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(9), MealSlot.Dinner, pouch.Id, 2m);

        var entry = (await _app.Trips.GetTripAsync(trip.Id))!.MealPlan.Days[0].Entries.Single();

        Assert.Equal(2m, entry.ServingsPerPackage);
    }
}
