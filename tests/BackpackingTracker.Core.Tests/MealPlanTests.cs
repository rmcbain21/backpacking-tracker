using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>A Trip's Meal Plan: Meals and Food Items in each Trip Day's slots, their totals, and food in pack weight.</summary>
public class MealPlanTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static readonly DateOnly July2 = new(2026, 7, 2);
    private static readonly DateOnly July3 = new(2026, 7, 3);

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public MealPlanTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    /// <summary>A 3-day Trip (July 1 to 3), a Clif Bar (68 g, 260 cal) and a Ramen Bomb (127 g, 530 cal).</summary>
    private async Task<(Trip Trip, FoodItem Bar, Meal Bomb)> SetUpAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July1, July3);
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));
        var ramen = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Ramen", 85m, 380m));
        var potatoes = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Instant potatoes", 28m, 100m));
        var bomb = await _app.Meals.CreateMealAsync(new MealDetails("Ramen Bomb", [new(ramen.Id, 1m), new(potatoes.Id, 1.5m)]));
        return (trip, bar, bomb);
    }

    private static IReadOnlyList<SlotEntryView> Slot(TripView view, int dayNumber, MealSlot slot) =>
        view.MealPlan.Days[dayNumber - 1].Slots.Single(s => s.Slot == slot).Entries;

    // ----- Slots and slot entries -----

    [Fact]
    public async Task Every_trip_day_has_four_empty_slots_to_start()
    {
        var (trip, _, _) = await SetUpAsync();

        var plan = (await ViewAsync(trip.Id)).MealPlan;

        Assert.Equal(3, plan.Days.Count);
        Assert.All(plan.Days, day =>
        {
            Assert.Equal([MealSlot.Breakfast, MealSlot.Lunch, MealSlot.Dinner, MealSlot.Snacks], day.Slots.Select(s => s.Slot));
            Assert.All(day.Slots, slot => Assert.Empty(slot.Entries));
            Assert.Equal((0m, 0m), (day.Calories, day.FoodGrams));
        });
    }

    [Fact]
    public async Task Meals_and_food_items_go_in_slots_with_a_quantity_and_are_remembered()
    {
        var (trip, bar, bomb) = await SetUpAsync();

        await _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id, 2m);

        var view = await new AppCore(_store, _clock).Trips.GetTripAsync(trip.Id);
        var dinner = Assert.Single(Slot(view!, 1, MealSlot.Dinner));
        var snacks = Assert.Single(Slot(view!, 1, MealSlot.Snacks));
        Assert.Equal(("Ramen Bomb", true, 1m), (dinner.Name, dinner.IsMeal, dinner.Entry.Quantity));
        Assert.Equal(("Clif Bar", false, 2m), (snacks.Name, snacks.IsMeal, snacks.Entry.Quantity));
        Assert.Empty(Slot(view!, 1, MealSlot.Breakfast));
        Assert.Empty(Slot(view!, 2, MealSlot.Dinner));
    }

    [Fact]
    public async Task A_slot_holds_several_entries_in_the_order_added()
    {
        var (trip, bar, bomb) = await SetUpAsync();

        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July2, MealSlot.Lunch, bar.Id);
        await _app.Trips.AddMealToSlotAsync(trip.Id, July2, MealSlot.Lunch, bomb.Id);

        Assert.Equal(["Clif Bar", "Ramen Bomb"], Slot(await ViewAsync(trip.Id), 2, MealSlot.Lunch).Select(e => e.Name));
    }

    [Fact]
    public async Task Adding_the_same_thing_to_a_slot_again_adds_to_its_quantity()
    {
        var (trip, bar, _) = await SetUpAsync();
        var first = await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);

        var again = await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id, 0.5m);

        Assert.Equal((first.Id, 1.5m), (again.Id, again.Quantity));
        Assert.Equal(1.5m, Assert.Single(Slot(await ViewAsync(trip.Id), 1, MealSlot.Snacks)).Entry.Quantity);
    }

    [Fact]
    public async Task The_same_food_in_another_slot_is_a_separate_entry()
    {
        var (trip, bar, _) = await SetUpAsync();

        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Breakfast, bar.Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);

        var view = await ViewAsync(trip.Id);
        Assert.Single(Slot(view, 1, MealSlot.Breakfast));
        Assert.Single(Slot(view, 1, MealSlot.Snacks));
    }

    [Fact]
    public async Task A_slot_entry_quantity_can_be_changed_and_the_entry_removed()
    {
        var (trip, bar, bomb) = await SetUpAsync();
        var entry = await _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Dinner, bar.Id);

        await _app.Trips.UpdateSlotEntryAsync(trip.Id, July1, entry.Id, 0.5m);
        Assert.Equal(0.5m, Slot(await ViewAsync(trip.Id), 1, MealSlot.Dinner)[0].Entry.Quantity);

        await _app.Trips.RemoveSlotEntryAsync(trip.Id, July1, entry.Id);
        Assert.Equal(["Clif Bar"], Slot(await ViewAsync(trip.Id), 1, MealSlot.Dinner).Select(e => e.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(0.004)]
    public async Task A_quantity_must_be_more_than_zero(double quantity)
    {
        var (trip, bar, _) = await SetUpAsync();
        var entry = await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);

        var add = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Lunch, bar.Id, (decimal)quantity));
        var update = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Trips.UpdateSlotEntryAsync(trip.Id, July1, entry.Id, (decimal)quantity));

        Assert.Equal("A quantity must be more than zero.", add.Message);
        Assert.Equal("A quantity must be more than zero.", update.Message);
    }

    [Fact]
    public async Task Retired_meals_and_food_items_cannot_be_added()
    {
        var (trip, bar, bomb) = await SetUpAsync();
        await _app.Food.RetireFoodItemAsync(bar.Id);
        await _app.Meals.RetireMealAsync(bomb.Id);

        var food = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id));
        var meal = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id));

        Assert.Equal("Clif Bar is Retired, so it can't be added to the Meal Plan.", food.Message);
        Assert.Equal("Ramen Bomb is Retired, so it can't be added to the Meal Plan.", meal.Message);
    }

    [Fact]
    public async Task Missing_things_and_days_outside_the_trip_are_refused()
    {
        var (trip, bar, _) = await SetUpAsync();

        async Task<string> RefusalAsync(Func<Task> change) => (await Assert.ThrowsAsync<RuleViolationException>(change)).Message;

        Assert.Equal("That Meal no longer exists.",
            await RefusalAsync(() => _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, "gone")));
        Assert.Equal("That Food Item no longer exists.",
            await RefusalAsync(() => _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Dinner, "gone")));
        Assert.Equal("That day is no longer part of the Trip.",
            await RefusalAsync(() => _app.Trips.AddFoodItemToSlotAsync(trip.Id, new DateOnly(2026, 7, 9), MealSlot.Dinner, bar.Id)));
        Assert.Equal("That is no longer in the Meal Plan.",
            await RefusalAsync(() => _app.Trips.RemoveSlotEntryAsync(trip.Id, July1, "gone")));
        Assert.Equal("Choose Breakfast, Lunch, Dinner or Snacks.",
            await RefusalAsync(() => _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, (MealSlot)9, bar.Id)));
    }

    [Fact]
    public async Task The_meal_plan_is_locked_on_a_completed_trip()
    {
        var (trip, bar, _) = await SetUpAsync();
        var entry = await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);
        await _app.Trips.CompleteTripAsync(trip.Id);

        async Task<string> RefusalAsync(Func<Task> change) => (await Assert.ThrowsAsync<RuleViolationException>(change)).Message;

        const string locked = "This Trip is Completed. Reopen it to change its Meal Plan.";
        Assert.Equal(locked, await RefusalAsync(() => _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Lunch, bar.Id)));
        Assert.Equal(locked, await RefusalAsync(() => _app.Trips.UpdateSlotEntryAsync(trip.Id, July1, entry.Id, 3m)));
        Assert.Equal(locked, await RefusalAsync(() => _app.Trips.RemoveSlotEntryAsync(trip.Id, July1, entry.Id)));

        await _app.Trips.ReopenTripAsync(trip.Id);
        await _app.Trips.RemoveSlotEntryAsync(trip.Id, July1, entry.Id);
        Assert.Empty(Slot(await ViewAsync(trip.Id), 1, MealSlot.Snacks));
    }

    // ----- Totals -----

    [Fact]
    public async Task Each_day_totals_its_calories_and_food_weight()
    {
        var (trip, bar, bomb) = await SetUpAsync();
        await _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id, 2m);
        await _app.Trips.AddMealToSlotAsync(trip.Id, July2, MealSlot.Dinner, bomb.Id, 0.5m);

        var view = await ViewAsync(trip.Id);
        var plan = view.MealPlan;

        // Day 1: Ramen Bomb 127 g / 530 cal, two Clif Bars 136 g / 520 cal. Day 2: half a Ramen Bomb.
        var bars = Slot(view, 1, MealSlot.Snacks)[0];
        Assert.Equal((136m, 520m), (bars.WeightGrams, bars.Calories));
        Assert.Equal((1050m, 263m), (plan.Days[0].Calories, plan.Days[0].FoodGrams));
        Assert.Equal((265m, 63.5m), (plan.Days[1].Calories, plan.Days[1].FoodGrams));
        Assert.Equal((0m, 0m), (plan.Days[2].Calories, plan.Days[2].FoodGrams));
        Assert.Equal((1315m, 326.5m), (plan.Calories, plan.FoodGrams));
    }

    [Fact]
    public async Task A_planned_trip_uses_the_current_food_values()
    {
        var (trip, bar, _) = await SetUpAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);

        await _app.Food.UpdateFoodItemAsync(bar.Id, new FoodItemDetails("Clif Bar", 70m, 270m));

        var day = (await ViewAsync(trip.Id)).MealPlan.Days[0];
        Assert.Equal((270m, 70m), (day.Calories, day.FoodGrams));
    }

    [Fact]
    public async Task Retired_food_already_in_the_plan_still_counts()
    {
        var (trip, bar, _) = await SetUpAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);

        await _app.Food.RetireFoodItemAsync(bar.Id);

        Assert.Equal(68m, (await ViewAsync(trip.Id)).MealPlan.FoodGrams);
    }

    [Fact]
    public async Task Food_deleted_on_another_device_is_shown_as_missing_and_weighs_nothing()
    {
        var (trip, bar, _) = await SetUpAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);

        // As if a sync brought in the deletion from a device where the Clif Bar wasn't in this plan.
        await _store.SaveAsync("foodItems", bar with { Deleted = true });

        var entry = Assert.Single(Slot(await ViewAsync(trip.Id), 1, MealSlot.Snacks));
        Assert.Equal((null, 0m, 0m), (entry.Name, entry.WeightGrams, entry.Calories));
    }

    // ----- Food in pack weight -----

    [Fact]
    public async Task Meal_plan_food_counts_as_consumable_and_starting_pack_weight()
    {
        var (trip, bar, bomb) = await SetUpAsync();
        var tent = await _app.Gear.AddGearItemAsync(new GearItemDetails("Tent", null, 1000m));
        var view = await ViewAsync(trip.Id);
        await _app.Trips.AddEntryAsync(trip.Id, tent.Id, view.Trip.PackingList.Categories[0].Id);
        await _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July2, MealSlot.Snacks, bar.Id);
        await _app.Trips.SetWaterCarriedAsync(trip.Id, 1000m);

        var weight = (await ViewAsync(trip.Id)).Weight;

        Assert.Equal((1000m, 195m + 1000m, 2195m), (weight.BaseGrams, weight.ConsumableGrams, weight.StartingPackGrams));
        Assert.Equal(2195m, (await ViewAsync(trip.Id)).Stats.StartingPackGrams);
    }

    [Fact]
    public async Task Food_is_its_own_breakdown_line_after_the_categories_and_before_water()
    {
        var (trip, bar, _) = await SetUpAsync();
        var tent = await _app.Gear.AddGearItemAsync(new GearItemDetails("Tent", null, 932m));
        var view = await ViewAsync(trip.Id);
        await _app.Trips.AddEntryAsync(trip.Id, tent.Id, view.Trip.PackingList.Categories[0].Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);
        await _app.Trips.SetWaterCarriedAsync(trip.Id, 1000m);

        var lines = (await ViewAsync(trip.Id)).Weight.ByCategory;

        var first = view.Trip.PackingList.Categories[0].Name;
        Assert.Equal([(first, 932m), ("Food", 68m), ("Water", 1000m)], lines.Select(l => (l.Category.Name, l.Grams)));
        Assert.Equal(0.034m, Math.Round(lines[1].Share, 3));
    }

    [Fact]
    public async Task There_is_no_food_line_without_food()
    {
        var (trip, _, _) = await SetUpAsync();
        await _app.Trips.SetWaterCarriedAsync(trip.Id, 1000m);

        Assert.Equal(["Water"], (await ViewAsync(trip.Id)).Weight.ByCategory.Select(l => l.Category.Name));
    }

    [Fact]
    public async Task Trips_are_listed_with_their_food_weights()
    {
        var (trip, bar, _) = await SetUpAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id, 3m);

        var listed = Assert.Single((await _app.Trips.ListTripsAsync()).Upcoming);

        Assert.Equal(204m, listed.Weight.ConsumableGrams);
    }

    // ----- Delete guards -----

    [Fact]
    public async Task A_food_item_in_a_meal_plan_cannot_be_deleted()
    {
        var (trip, bar, _) = await SetUpAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Snacks, bar.Id);

        var plan = await _app.Food.PlanFoodItemDeletionAsync(bar.Id);

        Assert.Equal(["Trip \"Wind Rivers\""], plan.UsedBy);
        Assert.Equal("Clif Bar can't be deleted because Trip \"Wind Rivers\" uses it. Retire it instead?", plan.Message);
    }

    [Fact]
    public async Task A_food_item_in_both_a_meal_and_a_meal_plan_names_both()
    {
        var (trip, _, bomb) = await SetUpAsync();
        var ramenId = bomb.Components[0].FoodItemId;
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Lunch, ramenId);

        var plan = await _app.Food.PlanFoodItemDeletionAsync(ramenId);

        Assert.Equal(["Meal \"Ramen Bomb\"", "Trip \"Wind Rivers\""], plan.UsedBy);
    }

    [Fact]
    public async Task A_meal_in_a_meal_plan_cannot_be_deleted_and_each_trip_is_named_once()
    {
        var (trip, _, bomb) = await SetUpAsync();
        var other = await _app.Trips.CreateTripAsync("Alps", July1, July1);
        await _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id);
        await _app.Trips.AddMealToSlotAsync(trip.Id, July2, MealSlot.Dinner, bomb.Id);
        await _app.Trips.AddMealToSlotAsync(other.Id, July1, MealSlot.Lunch, bomb.Id);

        var plan = await _app.Meals.PlanMealDeletionAsync(bomb.Id);
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Meals.DeleteMealAsync(bomb.Id));

        Assert.Equal(["Trip \"Alps\"", "Trip \"Wind Rivers\""], plan.UsedBy);
        Assert.Equal("Ramen Bomb can't be deleted because Trip \"Alps\" and Trip \"Wind Rivers\" use it. Retire it instead?", refused.Message);
    }

    [Fact]
    public async Task A_meal_can_be_deleted_once_no_meal_plan_uses_it()
    {
        var (trip, _, bomb) = await SetUpAsync();
        var entry = await _app.Trips.AddMealToSlotAsync(trip.Id, July1, MealSlot.Dinner, bomb.Id);

        await _app.Trips.RemoveSlotEntryAsync(trip.Id, July1, entry.Id);

        Assert.True((await _app.Meals.PlanMealDeletionAsync(bomb.Id)).CanDelete);
    }

    // ----- Date changes -----

    [Fact]
    public async Task Removing_a_day_with_only_meals_needs_confirmation()
    {
        var (trip, bar, _) = await SetUpAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July3, MealSlot.Breakfast, bar.Id);

        var plan = await _app.Trips.PlanDateChangeAsync(trip.Id, July1, July2);
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ChangeDatesAsync(trip.Id, July1, July2));

        Assert.True(plan.NeedsConfirmation);
        Assert.Equal("Day 3 would be removed, with what's filled in. Confirm to remove.", refused.Message);
    }

    [Fact]
    public async Task Moving_a_trip_takes_each_days_meals_with_it()
    {
        var (trip, bar, _) = await SetUpAsync();
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July1, MealSlot.Breakfast, bar.Id);

        await _app.Trips.MoveTripAsync(trip.Id, new DateOnly(2026, 8, 1));

        var day1 = (await ViewAsync(trip.Id)).MealPlan.Days[0];
        Assert.Equal(new DateOnly(2026, 8, 1), day1.Day.Date);
        Assert.Equal(["Clif Bar"], day1.Slots[0].Entries.Select(e => e.Name));
    }
}
