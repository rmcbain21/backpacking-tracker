using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Copying an earlier Trip's Meal Plan by day position.</summary>
public class MealPlanCopyTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public MealPlanCopyTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private static DateOnly August(int day) => new(2026, 8, day);

    private async Task<Trip> TripAsync(string tripId) =>
        (await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.")).Trip;

    /// <summary>What's in each day's slots, such as ["Breakfast: Oatmeal ×1", "Snacks: Clif Bar ×2"], day by day.</summary>
    private async Task<string[][]> PlanAsync(string tripId) =>
        (await _app.Trips.GetTripAsync(tripId))!.MealPlan.Days
            .Select(day => day.Slots.SelectMany(slot => slot.Entries.Select(e => $"{slot.Slot}: {e.Name} ×{e.Entry.Quantity:0.##}")).ToArray())
            .ToArray();

    private Task<FoodItem> FoodAsync(string name) => _app.Food.AddFoodItemAsync(new FoodItemDetails(name, 50m, 200m));

    /// <summary>A 3-day source Trip, July 1 to 3: Oatmeal for breakfast each day, plus two Clif Bars as Day 2's snacks.</summary>
    private async Task<(Trip Source, FoodItem Oatmeal, FoodItem Bar)> SourceAsync()
    {
        var source = await _app.Trips.CreateTripAsync("Wind Rivers", July(1), July(3));
        var oatmeal = await FoodAsync("Oatmeal");
        var bar = await FoodAsync("Clif Bar");
        for (var day = 1; day <= 3; day++)
        {
            await _app.Trips.AddFoodItemToSlotAsync(source.Id, July(day), MealSlot.Breakfast, oatmeal.Id);
        }

        await _app.Trips.AddFoodItemToSlotAsync(source.Id, July(2), MealSlot.Snacks, bar.Id, 2m);
        return (source, oatmeal, bar);
    }

    [Fact]
    public async Task Days_are_copied_by_position_onto_a_trip_of_the_same_length()
    {
        var (source, _, _) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(12));

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        Assert.Equal(await PlanAsync(source.Id), await PlanAsync(target.Id));
        Assert.Equal(August(11), (await TripAsync(target.Id)).Days[1].Date);
    }

    [Fact]
    public async Task A_shorter_target_gets_only_the_first_days()
    {
        var (source, _, _) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Overnighter", August(10), August(11));

        var plan = await _app.Trips.PlanMealPlanCopyAsync(target.Id, source.Id);
        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        Assert.Equal(2, plan.DayCount);
        Assert.Equal(
            [["Breakfast: Oatmeal ×1"], ["Breakfast: Oatmeal ×1", "Snacks: Clif Bar ×2"]],
            await PlanAsync(target.Id));
    }

    [Fact]
    public async Task A_longer_target_keeps_its_later_days_as_they_were()
    {
        var (source, oatmeal, bar) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Long", August(10), August(14));
        await _app.Trips.AddFoodItemToSlotAsync(target.Id, August(14), MealSlot.Dinner, bar.Id);

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        var plan = await PlanAsync(target.Id);
        Assert.Equal(5, plan.Length);
        Assert.Equal(["Breakfast: Oatmeal ×1"], plan[2]);
        Assert.Empty(plan[3]);
        Assert.Equal(["Dinner: Clif Bar ×1"], plan[4]);
    }

    [Fact]
    public async Task Copies_refer_to_the_same_food_with_new_ids_and_leave_the_source_alone()
    {
        var (source, oatmeal, _) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(12));

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        var copied = (await TripAsync(target.Id)).Days[0].SlotEntries[0];
        var original = (await TripAsync(source.Id)).Days[0].SlotEntries[0];
        Assert.Equal((oatmeal.Id, 1m), (copied.FoodItemId, copied.Quantity));
        Assert.NotEqual(original.Id, copied.Id);

        await _app.Trips.RemoveSlotEntryAsync(target.Id, August(10), copied.Id);
        Assert.Single((await TripAsync(source.Id)).Days[0].SlotEntries);
    }

    [Fact]
    public async Task Meals_are_copied_as_meals()
    {
        var (source, oatmeal, _) = await SourceAsync();
        var meal = await _app.Meals.CreateMealAsync(new MealDetails("Oat bowl", [new(oatmeal.Id, 2m)]));
        await _app.Trips.AddMealToSlotAsync(source.Id, July(1), MealSlot.Dinner, meal.Id, 1.5m);
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(10));

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        var dinner = (await TripAsync(target.Id)).Days[0].SlotEntries.Single(e => e.Slot == MealSlot.Dinner);
        Assert.Equal((meal.Id, (string?)null, 1.5m), (dinner.MealId, dinner.FoodItemId, dinner.Quantity));
    }

    [Fact]
    public async Task Copying_from_a_completed_trip_drops_its_frozen_figures()
    {
        var (source, oatmeal, _) = await SourceAsync();
        await _app.Trips.CompleteTripAsync(source.Id);
        await _app.Food.UpdateFoodItemAsync(oatmeal.Id, new FoodItemDetails("Oatmeal", 50m, 300m));
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(10));

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        var copied = (await TripAsync(target.Id)).Days[0].SlotEntries[0];
        Assert.Null(copied.Frozen);
        Assert.Equal(300m, (await _app.Trips.GetTripAsync(target.Id))!.MealPlan.Calories);
    }

    // ----- Confirmation -----

    [Fact]
    public async Task Replacing_days_with_food_planned_needs_confirmation()
    {
        var (source, _, bar) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(13));
        await _app.Trips.AddFoodItemToSlotAsync(target.Id, August(10), MealSlot.Lunch, bar.Id);
        await _app.Trips.AddFoodItemToSlotAsync(target.Id, August(12), MealSlot.Lunch, bar.Id);
        await _app.Trips.AddFoodItemToSlotAsync(target.Id, August(13), MealSlot.Lunch, bar.Id);

        var plan = await _app.Trips.PlanMealPlanCopyAsync(target.Id, source.Id);
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.CopyMealPlanAsync(target.Id, source.Id));

        // Day 4 has food too, but the 3-day source doesn't reach it.
        Assert.Equal([1, 3], plan.ReplacedDays);
        Assert.Equal(("Days 1 and 3", true), (plan.ReplacedDaysText, plan.NeedsConfirmation));
        Assert.Equal("Days 1 and 3 already have food planned. Confirm to replace it.", refused.Message);
        Assert.Equal(["Lunch: Clif Bar ×1"], (await PlanAsync(target.Id))[0]);
    }

    [Fact]
    public async Task Confirming_replaces_each_copied_days_whole_meal_plan()
    {
        var (source, _, bar) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(12));
        await _app.Trips.AddFoodItemToSlotAsync(target.Id, August(10), MealSlot.Lunch, bar.Id);

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id, replaceExisting: true);

        Assert.Equal(["Breakfast: Oatmeal ×1"], (await PlanAsync(target.Id))[0]);
    }

    [Fact]
    public async Task One_replaced_day_is_named_on_its_own()
    {
        var (source, _, bar) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(12));
        await _app.Trips.AddFoodItemToSlotAsync(target.Id, August(11), MealSlot.Lunch, bar.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.CopyMealPlanAsync(target.Id, source.Id));

        Assert.Equal("Day 2 already has food planned. Confirm to replace it.", refused.Message);
    }

    [Fact]
    public async Task An_empty_target_needs_no_confirmation()
    {
        var (source, _, _) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(12));

        var plan = await _app.Trips.PlanMealPlanCopyAsync(target.Id, source.Id);

        Assert.Equal(("Wind Rivers", 3, false, ""), (plan.SourceName, plan.DayCount, plan.NeedsConfirmation, plan.ReplacedDaysText));
    }

    // ----- Retired food and refusals -----

    [Fact]
    public async Task Retired_meals_and_food_items_are_left_out_and_named()
    {
        var (source, _, bar) = await SourceAsync();
        await _app.Food.RetireFoodItemAsync(bar.Id);
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(12));

        var plan = await _app.Trips.PlanMealPlanCopyAsync(target.Id, source.Id);
        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);

        Assert.Equal(["Clif Bar"], plan.Skipped);
        Assert.Equal(["Breakfast: Oatmeal ×1"], (await PlanAsync(target.Id))[1]);
    }

    [Fact]
    public async Task Copying_onto_a_completed_trip_is_refused()
    {
        var (source, _, _) = await SourceAsync();
        var target = await _app.Trips.CreateTripAsync("Alps", August(10), August(12));
        await _app.Trips.CompleteTripAsync(target.Id);

        var plan = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.PlanMealPlanCopyAsync(target.Id, source.Id));
        var copy = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.CopyMealPlanAsync(target.Id, source.Id, true));

        Assert.Equal("This Trip is Completed. Reopen it to change its Meal Plan.", plan.Message);
        Assert.Equal(plan.Message, copy.Message);
    }

    [Fact]
    public async Task Copying_from_itself_a_missing_trip_or_one_without_a_meal_plan_is_refused()
    {
        var (source, _, _) = await SourceAsync();
        var empty = await _app.Trips.CreateTripAsync("Empty", August(1), August(2));

        async Task<string> RefusalAsync(string from) =>
            (await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.CopyMealPlanAsync(source.Id, from))).Message;

        Assert.Equal("Choose another Trip to copy from.", await RefusalAsync(source.Id));
        Assert.Equal("That Trip no longer exists.", await RefusalAsync("gone"));
        Assert.Equal("Empty has no Meal Plan to copy.", await RefusalAsync(empty.Id));
    }
}
