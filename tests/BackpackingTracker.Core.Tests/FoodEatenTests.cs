using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Ticking a Trip Day's slot entries as eaten, and the food eaten against what was planned.</summary>
public class FoodEatenTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on 10 July.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public FoodEatenTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    /// <summary>
    /// A Trip from the given days in July, with a Clif Bar (260 cal) ×2 in Snacks and a Ramen Bomb (530 cal) for Dinner
    /// on every day: 1,050 cal a day.
    /// </summary>
    private async Task<(Trip Trip, FoodItem Bar, Meal Bomb)> SetUpAsync(int startDay, int endDay)
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July(startDay), July(endDay));
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));
        var ramen = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Ramen", 85m, 380m));
        var potatoes = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Instant potatoes", 28m, 100m));
        var bomb = await _app.Meals.CreateMealAsync(new MealDetails("Ramen Bomb", [new(ramen.Id, 1m), new(potatoes.Id, 1.5m)]));
        for (var day = startDay; day <= endDay; day++)
        {
            await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(day), MealSlot.Snacks, bar.Id, 2m);
            await _app.Trips.AddMealToSlotAsync(trip.Id, July(day), MealSlot.Dinner, bomb.Id);
        }

        return (trip, bar, bomb);
    }

    private static SlotEntry Entry(TripView view, DateOnly date, MealSlot slot) =>
        view.Trip.Days.Single(day => day.Date == date).SlotEntries.Single(entry => entry.Slot == slot);

    private static DayMealPlan Day(TripView view, DateOnly date) => view.MealPlan.Days.Single(day => day.Day.Date == date);

    private async Task TickAsync(string tripId, DateOnly date, MealSlot slot, bool eaten = true) =>
        await _app.Trips.SetSlotEntryEatenAsync(tripId, date, Entry(await ViewAsync(tripId), date, slot).Id, eaten);

    [Fact]
    public async Task A_tick_covers_the_whole_entry_and_is_remembered_with_the_trip()
    {
        var (trip, _, _) = await SetUpAsync(9, 11);

        await TickAsync(trip.Id, July(10), MealSlot.Snacks);

        var view = await new AppCore(_store, _clock).Trips.GetTripAsync(trip.Id);
        Assert.True(Entry(view!, July(10), MealSlot.Snacks).Eaten);
        Assert.False(Entry(view!, July(10), MealSlot.Dinner).Eaten);
        Assert.Equal((520m, 1_050m), (Day(view!, July(10)).EatenCalories, Day(view!, July(10)).Calories));
    }

    [Fact]
    public async Task Tapping_again_unticks()
    {
        var (trip, _, _) = await SetUpAsync(9, 11);
        await TickAsync(trip.Id, July(10), MealSlot.Snacks);

        await TickAsync(trip.Id, July(10), MealSlot.Snacks, eaten: false);

        Assert.False(Entry(await ViewAsync(trip.Id), July(10), MealSlot.Snacks).Eaten);
    }

    [Fact]
    public async Task Ticking_never_changes_any_weight()
    {
        var (trip, _, _) = await SetUpAsync(9, 11);
        static (decimal, decimal, decimal, decimal) Figures(TripView view) =>
            (view.Weight.BaseGrams, view.Weight.ConsumableGrams, view.Weight.StartingPackGrams, view.MealPlan.FoodGrams);
        var before = Figures(await ViewAsync(trip.Id));

        await _app.Trips.SetDayEatenAsync(trip.Id, July(9));

        Assert.Equal(before, Figures(await ViewAsync(trip.Id)));
    }

    [Fact]
    public async Task Food_cant_be_ticked_before_the_trip_has_started()
    {
        var (trip, _, _) = await SetUpAsync(11, 13);

        await Assert.ThrowsAsync<RuleViolationException>(() => TickAsync(trip.Id, July(11), MealSlot.Snacks));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetDayEatenAsync(trip.Id, July(11)));
    }

    [Fact]
    public async Task Any_day_can_be_ticked_once_started_including_past_and_skipped_ones()
    {
        var (trip, _, _) = await SetUpAsync(8, 12);
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(9), skipped: true);

        await TickAsync(trip.Id, July(8), MealSlot.Dinner);
        await TickAsync(trip.Id, July(9), MealSlot.Dinner);
        await TickAsync(trip.Id, July(12), MealSlot.Dinner);

        var view = await ViewAsync(trip.Id);
        Assert.All(new[] { 8, 9, 12 }, day => Assert.True(Entry(view, July(day), MealSlot.Dinner).Eaten));
    }

    [Fact]
    public async Task All_eaten_ticks_every_entry_on_that_day_only()
    {
        var (trip, _, _) = await SetUpAsync(9, 11);

        await _app.Trips.SetDayEatenAsync(trip.Id, July(10));

        var view = await ViewAsync(trip.Id);
        Assert.True(Day(view, July(10)).AllEaten);
        Assert.Equal(1_050m, Day(view, July(10)).EatenCalories);
        Assert.False(Day(view, July(9)).AllEaten);
        Assert.False(Day(view, July(11)).AllEaten);
    }

    [Fact]
    public async Task A_day_with_nothing_planned_isnt_all_eaten()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July(9), July(11));

        Assert.False(Day(await ViewAsync(trip.Id), July(10)).AllEaten);
    }

    [Fact]
    public async Task Adding_more_to_a_ticked_entry_unticks_it()
    {
        var (trip, bar, _) = await SetUpAsync(9, 11);
        await TickAsync(trip.Id, July(10), MealSlot.Snacks);

        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(10), MealSlot.Snacks, bar.Id);

        var snacks = Entry(await ViewAsync(trip.Id), July(10), MealSlot.Snacks);
        Assert.Equal((3m, false), (snacks.Quantity, snacks.Eaten));
    }

    [Fact]
    public async Task Changing_the_quantity_keeps_the_tick()
    {
        var (trip, _, _) = await SetUpAsync(9, 11);
        var snacks = Entry(await ViewAsync(trip.Id), July(10), MealSlot.Snacks);
        await TickAsync(trip.Id, July(10), MealSlot.Snacks);

        await _app.Trips.UpdateSlotEntryAsync(trip.Id, July(10), snacks.Id, 1m);

        Assert.True(Entry(await ViewAsync(trip.Id), July(10), MealSlot.Snacks).Eaten);
    }

    [Fact]
    public async Task Ticks_are_locked_on_a_completed_trip_and_kept_when_reopened()
    {
        var (trip, _, _) = await SetUpAsync(9, 11);
        await TickAsync(trip.Id, July(10), MealSlot.Snacks);
        await _app.Trips.CompleteTripAsync(trip.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() => TickAsync(trip.Id, July(10), MealSlot.Dinner));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetDayEatenAsync(trip.Id, July(11)));

        await _app.Trips.ReopenTripAsync(trip.Id);
        Assert.True(Entry(await ViewAsync(trip.Id), July(10), MealSlot.Snacks).Eaten);
    }

    [Fact]
    public async Task Copying_meals_onto_a_new_day_never_copies_ticks()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(10), July(10), openEnded: true);
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(10), MealSlot.Snacks, bar.Id);
        await _app.Trips.SetDayEatenAsync(trip.Id, July(10));

        var added = await _app.Trips.AddDayAsync(trip.Id, copyMealsFromPreviousDay: true);

        Assert.False(Assert.Single(added.SlotEntries).Eaten);
    }

    [Fact]
    public async Task Copying_a_meal_plan_from_another_trip_never_copies_ticks()
    {
        var (source, _, _) = await SetUpAsync(9, 10);
        await _app.Trips.SetDayEatenAsync(source.Id, July(9));
        var target = await _app.Trips.CreateTripAsync("Sierra", July(20), July(21));

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        var view = await ViewAsync(target.Id);
        Assert.NotEmpty(view.Trip.Days[0].SlotEntries);
        Assert.All(view.Trip.Days.SelectMany(day => day.SlotEntries), entry => Assert.False(entry.Eaten));
    }

    // ----- Stats -----

    [Fact]
    public async Task There_are_no_eaten_stats_until_something_is_ticked()
    {
        var (trip, _, _) = await SetUpAsync(9, 11);

        Assert.Null((await ViewAsync(trip.Id)).Stats.Eaten);
    }

    [Fact]
    public async Task On_a_planned_trip_eaten_stats_count_the_days_up_to_today()
    {
        var (trip, _, _) = await SetUpAsync(8, 12);
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(9), skipped: true);
        await _app.Trips.SetDayEatenAsync(trip.Id, July(8));
        await _app.Trips.SetDayEatenAsync(trip.Id, July(9));
        await TickAsync(trip.Id, July(10), MealSlot.Dinner);
        await _app.Trips.SetDayEatenAsync(trip.Id, July(12));

        var eaten = (await ViewAsync(trip.Id)).Stats.Eaten!;

        // 8, 9 (Skipped, still counted) and 10 July: 1,050 + 1,050 + 530 of 3 × 1,050. Tomorrow's tick isn't counted yet.
        Assert.Equal(new FoodEaten(2_630m, 3_150m, 3), eaten);
        Assert.Equal(83, eaten.Percent);
        Assert.Equal(2_630m / 3, eaten.CaloriesPerDay);
    }

    [Fact]
    public async Task On_a_completed_trip_eaten_stats_count_every_day_with_frozen_calories()
    {
        var (trip, bar, _) = await SetUpAsync(8, 12);
        await _app.Trips.SetDayEatenAsync(trip.Id, July(8));
        await _app.Trips.SetDayEatenAsync(trip.Id, July(12));
        await _app.Trips.CompleteTripAsync(trip.Id);
        await _app.Food.UpdateFoodItemAsync(bar.Id, new FoodItemDetails("Clif Bar", 68m, 999m));

        var eaten = (await ViewAsync(trip.Id)).Stats.Eaten!;

        Assert.Equal(new FoodEaten(2_100m, 5_250m, 5), eaten);
        Assert.Equal(40, eaten.Percent);
    }
}
