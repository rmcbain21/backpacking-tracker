using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Completing a Trip freezes its Meal Plan's food figures; Reopen clears them. And the food stats.</summary>
public class FoodFreezingTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static readonly DateOnly July2 = new(2026, 7, 2);
    private static readonly DateOnly July3 = new(2026, 7, 3);

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public FoodFreezingTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private record Setup(Trip Trip, FoodItem Bar, FoodItem Ramen, FoodItem Potatoes, Meal Bomb);

    /// <summary>
    /// A 3-day Trip (July 1 to 3) with a Ramen Bomb (127 g, 530 cal, 13 g protein, $0.50) and two Clif Bars
    /// (68 g, 260 cal, 9 g protein, $1.80 each) on Day 1, and one Clif Bar on Day 2.
    /// </summary>
    private async Task<Setup> PlannedTripAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July1, July3);
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m, 9m, 1.80m));
        var ramen = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Ramen", 85m, 380m, 10m, 0.50m));
        var potatoes = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Instant potatoes", 28m, 100m, 2m));
        var bomb = await _app.Meals.CreateMealAsync(new MealDetails("Ramen Bomb", [new(ramen.Id, 1m), new(potatoes.Id, 1.5m)]));
        await _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id, 2m);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July2, MealSlot.Snacks, bar.Id);
        return new Setup(trip, bar, ramen, potatoes, bomb);
    }

    private async Task<IReadOnlyList<SlotEntry>> StoredSlotEntriesAsync(string tripId) =>
        (await _store.LoadAsync<Trip>("trips", tripId))!.Days.SelectMany(day => day.SlotEntries).ToList();

    // ----- Freezing -----

    [Fact]
    public async Task Completing_a_trip_freezes_each_slot_entrys_figures_per_meal_or_serving()
    {
        var s = await PlannedTripAsync();

        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        var entries = await StoredSlotEntriesAsync(s.Trip.Id);
        Assert.Equal(new FoodFigures(127m, 530m, 13m, 0.50m), entries.Single(e => e.MealId == s.Bomb.Id).Frozen);
        Assert.All(entries.Where(e => e.FoodItemId == s.Bar.Id), e => Assert.Equal(new FoodFigures(68m, 260m, 9m, 1.80m), e.Frozen));
    }

    [Fact]
    public async Task A_planned_trip_has_no_frozen_figures()
    {
        var s = await PlannedTripAsync();

        Assert.All(await StoredSlotEntriesAsync(s.Trip.Id), e => Assert.Null(e.Frozen));
    }

    [Fact]
    public async Task Editing_food_after_completion_changes_planned_trips_but_not_completed_ones()
    {
        var s = await PlannedTripAsync();
        var planned = await _app.Trips.CreateTripAsync("Alps", July1, July1);
        await _app.Trips.AddFoodItemToSlotAsync(planned.Id, July1, MealSlot.Snacks, s.Bar.Id);
        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        await _app.Food.UpdateFoodItemAsync(s.Bar.Id, new FoodItemDetails("Clif Bar", 70m, 300m, 10m, 2.00m));
        await _app.Food.UpdateFoodItemAsync(s.Ramen.Id, new FoodItemDetails("Ramen", 100m, 450m, 12m, 0.75m));

        var completed = (await ViewAsync(s.Trip.Id)).MealPlan;
        Assert.Equal((1310m, 331m), (completed.Calories, completed.FoodGrams));
        Assert.Equal((300m, 70m), ((await ViewAsync(planned.Id)).MealPlan.Calories, (await ViewAsync(planned.Id)).MealPlan.FoodGrams));
    }

    [Fact]
    public async Task Changing_a_meals_food_items_after_completion_leaves_the_completed_trip_alone()
    {
        var s = await PlannedTripAsync();
        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        await _app.Meals.UpdateMealAsync(s.Bomb.Id, new MealDetails("Ramen Bomb", [new(s.Ramen.Id, 2m)]));

        var dinner = (await ViewAsync(s.Trip.Id)).MealPlan.Days[0].Slots.Single(slot => slot.Slot == MealSlot.Dinner).Entries[0];
        Assert.Equal((127m, 530m, 13m, 0.50m), (dinner.WeightGrams, dinner.Calories, dinner.ProteinGrams, dinner.Cost));
    }

    [Fact]
    public async Task Frozen_food_still_counts_in_the_completed_trips_consumable_weight()
    {
        var s = await PlannedTripAsync();
        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        await _app.Food.UpdateFoodItemAsync(s.Bar.Id, new FoodItemDetails("Clif Bar", 100m, 260m));

        Assert.Equal(331m, (await ViewAsync(s.Trip.Id)).Weight.ConsumableGrams);
    }

    [Fact]
    public async Task Reopen_clears_the_frozen_figures_and_uses_current_values_again()
    {
        var s = await PlannedTripAsync();
        await _app.Trips.CompleteTripAsync(s.Trip.Id);
        await _app.Food.UpdateFoodItemAsync(s.Bar.Id, new FoodItemDetails("Clif Bar", 70m, 300m, 10m, 2.00m));

        await _app.Trips.ReopenTripAsync(s.Trip.Id);

        Assert.All(await StoredSlotEntriesAsync(s.Trip.Id), e => Assert.Null(e.Frozen));
        // Ramen Bomb 530 cal, plus three Clif Bars now at 300 cal.
        Assert.Equal(1430m, (await ViewAsync(s.Trip.Id)).MealPlan.Calories);
    }

    [Fact]
    public async Task Completing_again_takes_a_fresh_freeze()
    {
        var s = await PlannedTripAsync();
        await _app.Trips.CompleteTripAsync(s.Trip.Id);
        await _app.Trips.ReopenTripAsync(s.Trip.Id);
        await _app.Food.UpdateFoodItemAsync(s.Bar.Id, new FoodItemDetails("Clif Bar", 70m, 300m, 10m, 2.00m));

        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        var bar = (await StoredSlotEntriesAsync(s.Trip.Id)).First(e => e.FoodItemId == s.Bar.Id);
        Assert.Equal(new FoodFigures(70m, 300m, 10m, 2.00m), bar.Frozen);
    }

    [Fact]
    public async Task Food_already_gone_when_completing_stays_unfrozen_and_counts_for_nothing()
    {
        var s = await PlannedTripAsync();
        await _store.SaveAsync("foodItems", s.Bar with { Deleted = true });

        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        Assert.All((await StoredSlotEntriesAsync(s.Trip.Id)).Where(e => e.FoodItemId == s.Bar.Id), e => Assert.Null(e.Frozen));
        Assert.Equal(530m, (await ViewAsync(s.Trip.Id)).MealPlan.Calories);
    }

    [Fact]
    public async Task Frozen_food_deleted_on_another_device_afterwards_still_counts()
    {
        var s = await PlannedTripAsync();
        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        await _store.SaveAsync("foodItems", s.Bar with { Deleted = true });

        var snacks = (await ViewAsync(s.Trip.Id)).MealPlan.Days[1].Slots.Single(slot => slot.Slot == MealSlot.Snacks).Entries[0];
        Assert.Equal((null, 260m), (snacks.Name, snacks.Calories));
    }

    // ----- Food stats -----

    [Fact]
    public async Task Calories_per_day_is_the_meal_plans_calories_over_every_trip_day()
    {
        var s = await PlannedTripAsync();

        var stats = (await ViewAsync(s.Trip.Id)).Stats;

        // 530 + 2 × 260 on Day 1, 260 on Day 2, nothing on Day 3: 1,310 cal over 3 days.
        Assert.Equal(436.67m, Math.Round(stats.CaloriesPerDay!.Value, 2));
    }

    [Fact]
    public async Task Food_cost_adds_up_the_slot_entries_that_have_a_cost()
    {
        var s = await PlannedTripAsync();

        // Ramen Bomb $0.50 (its potatoes have no cost) and three Clif Bars at $1.80.
        Assert.Equal(5.90m, (await ViewAsync(s.Trip.Id)).Stats.FoodCost);
    }

    [Fact]
    public async Task Food_stats_are_empty_without_a_meal_plan_or_costs()
    {
        var trip = await _app.Trips.CreateTripAsync("Empty", July1, July2);
        var potatoes = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Instant potatoes", 28m, 100m));
        var noCost = await _app.Trips.CreateTripAsync("No cost", July1, July2);
        await _app.Trips.AddFoodItemToSlotAsync(noCost.Id, July1, MealSlot.Dinner, potatoes.Id, 2m);

        var empty = (await ViewAsync(trip.Id)).Stats;
        var costless = (await ViewAsync(noCost.Id)).Stats;

        Assert.Equal((null, null), (empty.CaloriesPerDay, empty.FoodCost));
        Assert.Equal((100m, null), (costless.CaloriesPerDay, costless.FoodCost));
    }

    [Fact]
    public async Task A_completed_trips_food_stats_use_the_frozen_figures()
    {
        var s = await PlannedTripAsync();
        await _app.Trips.CompleteTripAsync(s.Trip.Id);

        await _app.Food.UpdateFoodItemAsync(s.Bar.Id, new FoodItemDetails("Clif Bar", 68m, 1000m, 9m, 10m));

        var stats = (await ViewAsync(s.Trip.Id)).Stats;
        Assert.Equal((436.67m, 5.90m), (Math.Round(stats.CaloriesPerDay!.Value, 2), stats.FoodCost));
    }
}
