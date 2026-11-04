using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Resupply days, the Food Carries they split a Trip into, their shopping lists, and their weights (ADR 0007).</summary>
public class ResupplyTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on 10 July.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public ResupplyTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private sealed record Food(FoodItem Bar, FoodItem Ramen, FoodItem Potatoes, Meal Bomb);

    /// <summary>
    /// A Trip over the given days in July with, every day, a Clif Bar (68 g, 260 cal) ×2 in Snacks and a Ramen Bomb
    /// (Ramen 85 g ×1 + Instant potatoes 28 g ×1.5 = 127 g, 530 cal) for Dinner: 263 g of food a day.
    /// </summary>
    private async Task<(Trip Trip, Food Food)> SetUpAsync(int startDay, int endDay)
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(startDay), July(endDay));
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));
        var ramen = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Ramen", 85m, 380m));
        var potatoes = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Instant potatoes", 28m, 100m));
        var bomb = await _app.Meals.CreateMealAsync(new MealDetails("Ramen Bomb", [new(ramen.Id, 1m), new(potatoes.Id, 1.5m)]));
        for (var day = startDay; day <= endDay; day++)
        {
            await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(day), MealSlot.Snacks, bar.Id, 2m);
            await _app.Trips.AddMealToSlotAsync(trip.Id, July(day), MealSlot.Dinner, bomb.Id);
        }

        return (trip, new Food(bar, ramen, potatoes, bomb));
    }

    private const decimal DayOfFood = 263m;

    // ----- Marking a Resupply -----

    [Fact]
    public async Task Without_a_resupply_the_trip_is_one_food_carry_from_home()
    {
        var (trip, _) = await SetUpAsync(8, 12);

        var carry = Assert.Single((await ViewAsync(trip.Id)).MealPlan.FoodCarries);

        Assert.True(carry.FromHome);
        Assert.Equal((1, 5, 5 * DayOfFood), (carry.FirstDayNumber, carry.LastDayNumber, carry.FoodGrams));
    }

    [Fact]
    public async Task Resupply_days_split_the_trip_into_food_carries()
    {
        var (trip, _) = await SetUpAsync(1, 9);

        await _app.Trips.SetResupplyAsync(trip.Id, July(5), true, "  Kennedy Meadows ");
        await _app.Trips.SetResupplyAsync(trip.Id, July(8), true);
        await _app.Trips.UpdateDayAsync(trip.Id, July(8), new TripDayDetails(Start: "Lone Pine"));

        var carries = (await new AppCore(_store, _clock).Trips.GetTripAsync(trip.Id))!.MealPlan.FoodCarries;
        Assert.Equal(
            [(1, (string?)null, 1, 4), (2, "Kennedy Meadows", 5, 7), (3, "Lone Pine", 8, 9)],
            carries.Select(c => (c.Number, c.Place, c.FirstDayNumber, c.LastDayNumber)));
    }

    [Fact]
    public async Task Day_one_cant_be_a_resupply()
    {
        var (trip, _) = await SetUpAsync(8, 12);

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetResupplyAsync(trip.Id, July(8), true));
    }

    [Fact]
    public async Task A_resupply_moved_onto_day_one_by_a_date_change_is_ignored()
    {
        var (trip, _) = await SetUpAsync(8, 12);
        await _app.Trips.SetResupplyAsync(trip.Id, July(10), true, "Town");

        await _app.Trips.ChangeDatesAsync(trip.Id, July(10), July(12), removeDaysWithContent: true);

        Assert.Single((await ViewAsync(trip.Id)).MealPlan.FoodCarries);
    }

    [Fact]
    public async Task A_resupply_counts_as_content_when_removing_days()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(8), July(12));
        await _app.Trips.SetResupplyAsync(trip.Id, July(12), true);

        var plan = await _app.Trips.PlanDateChangeAsync(trip.Id, July(8), July(11));

        Assert.True(plan.NeedsConfirmation);
    }

    [Fact]
    public async Task Resupplies_lock_on_a_completed_trip()
    {
        var (trip, _) = await SetUpAsync(8, 12);
        await _app.Trips.CompleteTripAsync(trip.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetResupplyAsync(trip.Id, July(10), true));
    }

    [Fact]
    public async Task Adding_a_day_never_copies_the_resupply()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(8), July(8), openEnded: true);
        await _app.Trips.AddDayAsync(trip.Id);
        await _app.Trips.SetResupplyAsync(trip.Id, July(9), true, "Town");

        var added = await _app.Trips.AddDayAsync(trip.Id, copyMealsFromPreviousDay: true);

        Assert.False(added.Resupply);
    }

    // ----- Weights (ADR 0007) -----

    [Fact]
    public async Task Only_the_first_food_carry_counts_in_starting_pack_weight()
    {
        var (trip, _) = await SetUpAsync(1, 9);
        var before = await ViewAsync(trip.Id);
        Assert.Equal(9 * DayOfFood, before.Weight.ConsumableGrams);

        await _app.Trips.SetResupplyAsync(trip.Id, July(5), true);

        var view = await ViewAsync(trip.Id);
        Assert.Equal(4 * DayOfFood, view.Weight.ConsumableGrams);
        Assert.Equal(4 * DayOfFood, view.Weight.StartingPackGrams);
        Assert.Equal(4 * DayOfFood, view.Weight.ByCategory.Single(line => line.Category.Name == "Food").Grams);
        Assert.Equal(9 * DayOfFood, view.MealPlan.FoodGrams);
    }

    // ----- Shopping lists -----

    [Fact]
    public async Task A_shopping_list_totals_food_items_with_meals_broken_down()
    {
        var (trip, _) = await SetUpAsync(1, 9);
        await _app.Trips.SetResupplyAsync(trip.Id, July(5), true);

        var lines = (await ViewAsync(trip.Id)).MealPlan.FoodCarries[0].Lines;

        Assert.Equal(
            [("Clif Bar", 8m, 544m), ("Instant potatoes", 6m, 168m), ("Ramen", 4m, 340m)],
            lines.Select(l => (l.Name!, l.Servings, l.WeightGrams)));
    }

    [Fact]
    public async Task Skipped_days_food_is_on_the_shopping_list()
    {
        var (trip, _) = await SetUpAsync(8, 9);
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(8), skipped: true);

        var bar = (await ViewAsync(trip.Id)).MealPlan.FoodCarries[0].Lines.Single(l => l.Name == "Clif Bar");

        Assert.Equal(4m, bar.Servings);
    }

    [Fact]
    public async Task Lines_can_be_ticked_as_bought_and_reset()
    {
        var (trip, food) = await SetUpAsync(1, 9);
        await _app.Trips.SetResupplyAsync(trip.Id, July(5), true);

        await _app.Trips.SetBoughtAsync(trip.Id, July(5), food.Bar.Id, true);
        await _app.Trips.SetBoughtAsync(trip.Id, July(5), food.Ramen.Id, true);
        await _app.Trips.SetBoughtAsync(trip.Id, July(5), food.Ramen.Id, false);

        var carries = (await ViewAsync(trip.Id)).MealPlan.FoodCarries;
        Assert.Equal(0, carries[0].BoughtCount);
        Assert.Equal(["Clif Bar"], carries[1].Lines.Where(l => l.Bought).Select(l => l.Name));

        await _app.Trips.ResetBoughtAsync(trip.Id, July(5));
        Assert.Equal(0, (await ViewAsync(trip.Id)).MealPlan.FoodCarries[1].BoughtCount);
    }

    [Fact]
    public async Task A_line_whose_servings_go_up_is_unticked_and_one_that_goes_down_keeps_its_tick()
    {
        var (trip, food) = await SetUpAsync(8, 9);
        await _app.Trips.SetBoughtAsync(trip.Id, July(8), food.Bar.Id, true);
        await _app.Trips.SetBoughtAsync(trip.Id, July(8), food.Ramen.Id, true);

        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(9), MealSlot.Snacks, food.Bar.Id);
        var dinner = (await ViewAsync(trip.Id)).Trip.Days[1].SlotEntries.Single(e => e.Slot == MealSlot.Dinner);
        await _app.Trips.RemoveSlotEntryAsync(trip.Id, July(9), dinner.Id);

        var lines = (await ViewAsync(trip.Id)).MealPlan.FoodCarries[0].Lines;
        Assert.False(lines.Single(l => l.Name == "Clif Bar").Bought);
        Assert.True(lines.Single(l => l.Name == "Ramen").Bought);
    }

    [Fact]
    public async Task Unmarking_a_resupply_drops_its_bought_ticks()
    {
        var (trip, food) = await SetUpAsync(1, 9);
        await _app.Trips.SetResupplyAsync(trip.Id, July(5), true, "Town");
        await _app.Trips.SetBoughtAsync(trip.Id, July(5), food.Bar.Id, true);

        await _app.Trips.SetResupplyAsync(trip.Id, July(5), false);
        await _app.Trips.SetResupplyAsync(trip.Id, July(5), true);

        var carry = (await ViewAsync(trip.Id)).MealPlan.FoodCarries[1];
        Assert.Equal((null, 0), (carry.FirstDay.ResupplyPlace, carry.BoughtCount));
    }

    [Fact]
    public async Task Bought_ticks_lock_on_a_completed_trip()
    {
        var (trip, food) = await SetUpAsync(8, 9);
        await _app.Trips.CompleteTripAsync(trip.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetBoughtAsync(trip.Id, July(8), food.Bar.Id, true));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ResetBoughtAsync(trip.Id, July(8)));
    }

    [Fact]
    public async Task Food_carry_totals_use_frozen_figures_on_a_completed_trip()
    {
        var (trip, food) = await SetUpAsync(8, 9);
        await _app.Trips.CompleteTripAsync(trip.Id);

        await _app.Food.UpdateFoodItemAsync(food.Bar.Id, new FoodItemDetails("Clif Bar", 100m, 999m));

        Assert.Equal(2 * DayOfFood, (await ViewAsync(trip.Id)).MealPlan.FoodCarries[0].FoodGrams);
    }

    // ----- On trail and stats -----

    [Fact]
    public async Task Food_left_is_the_rest_of_the_current_food_carry_less_what_was_eaten()
    {
        var (trip, _) = await SetUpAsync(8, 14);
        await _app.Trips.SetResupplyAsync(trip.Id, July(12), true, "Kennedy Meadows");
        var snacks = (await ViewAsync(trip.Id)).Trip.Days[2].SlotEntries.Single(e => e.Slot == MealSlot.Snacks);
        await _app.Trips.SetSlotEntryEatenAsync(trip.Id, July(10), snacks.Id, true);

        var view = await ViewAsync(trip.Id);

        // 10 and 11 July, less today's eaten bars.
        Assert.Equal(2 * DayOfFood - 136m, view.FoodLeftGrams);
        Assert.Equal(1, view.CurrentFoodCarry!.Number);
        Assert.Equal(("Kennedy Meadows", July(12)), (view.NextResupply!.Place, view.NextResupply.FirstDate));
    }

    [Fact]
    public async Task The_next_resupply_is_today_on_the_day_and_none_after_the_last()
    {
        var (trip, _) = await SetUpAsync(8, 12);
        await _app.Trips.SetResupplyAsync(trip.Id, July(10), true);

        Assert.Equal(July(10), (await ViewAsync(trip.Id)).NextResupply!.FirstDate);

        _clock.UtcNow = _clock.UtcNow.AddDays(1);
        Assert.Null((await ViewAsync(trip.Id)).NextResupply);
    }

    [Fact]
    public async Task Stats_show_resupplies_and_the_longest_food_carry()
    {
        var (trip, _) = await SetUpAsync(1, 9);
        Assert.Null((await ViewAsync(trip.Id)).Stats.LongestFoodCarry);

        await _app.Trips.SetResupplyAsync(trip.Id, July(3), true);
        await _app.Trips.SetResupplyAsync(trip.Id, July(7), true);

        Assert.Equal(new LongestFoodCarry(2, 4, 4 * DayOfFood), (await ViewAsync(trip.Id)).Stats.LongestFoodCarry);
    }
}
