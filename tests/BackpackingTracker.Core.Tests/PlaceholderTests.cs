using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>
/// Placeholders: slot entries standing in for food not chosen yet, how they count, how they show on the shopping list,
/// and Replacing them with a Meal or Food Item.
/// </summary>
public class PlaceholderTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on 10 July.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public PlaceholderTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private async Task<TripView> ViewAsync(string tripId) =>
        await new AppCore(_store, _clock).Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private static DayMealPlan Day(TripView view, int day) => view.MealPlan.Days.Single(d => d.Day.Date == July(day));

    private static SlotEntryView Entry(TripView view, int day, MealSlot slot) =>
        Assert.Single(Day(view, day).Slots.Single(s => s.Slot == slot).Entries);

    private static readonly PlaceholderFood Breakfast = new(Calories: 400m, WeightGrams: 113m);

    private static readonly PlaceholderFood Dinner = new(Calories: 700m, WeightGrams: 198m);

    /// <summary>
    /// The running example: a Trip from 4 to 11 July with a Resupply at Kennedy Meadows on day 6 (9 July), and
    /// Placeholder breakfasts on days 6–8 and dinners on days 6–7.
    /// </summary>
    private async Task<Trip> KennedyMeadowsAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(4), July(11));
        await _app.Trips.SetResupplyAsync(trip.Id, July(9), true, "Kennedy Meadows");
        for (var day = 9; day <= 11; day++)
        {
            await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(day), MealSlot.Breakfast, Breakfast);
        }

        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, Dinner);
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(10), MealSlot.Dinner, Dinner);
        return trip;
    }

    // ----- Adding -----

    [Fact]
    public async Task A_placeholder_is_named_by_its_label_or_its_slot()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Breakfast, new PlaceholderFood());
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Snacks, new PlaceholderFood("   "));
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, new PlaceholderFood("  pizza in town "));

        var view = await ViewAsync(trip.Id);

        Assert.Equal("Any breakfast", Entry(view, 9, MealSlot.Breakfast).Name);
        Assert.Equal("Any snack", Entry(view, 9, MealSlot.Snacks).Name);
        Assert.Equal("pizza in town", Entry(view, 9, MealSlot.Dinner).Name);
        Assert.True(Entry(view, 9, MealSlot.Dinner).IsPlaceholder);
    }

    [Fact]
    public async Task Adding_the_same_label_again_adds_to_its_quantity()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, new PlaceholderFood("Pizza", 700m));
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, new PlaceholderFood("pizza", 900m), 2m);
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, new PlaceholderFood());

        var dinner = Day(await ViewAsync(trip.Id), 9).Slots.Single(s => s.Slot == MealSlot.Dinner).Entries;

        Assert.Equal([("Pizza", 3m, 2100m), ("Any dinner", 1m, 0m)], dinner.Select(e => (e.Name, e.Entry.Quantity, e.Calories)));
    }

    [Fact]
    public async Task Adding_to_an_eaten_placeholder_unticks_it()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(10));
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Lunch, new PlaceholderFood());
        await _app.Trips.SetDayEatenAsync(trip.Id, July(9));

        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Lunch, new PlaceholderFood());

        var lunch = Entry(await ViewAsync(trip.Id), 9, MealSlot.Lunch);
        Assert.Equal((2m, false), (lunch.Entry.Quantity, lunch.Entry.Eaten));
    }

    [Fact]
    public async Task Negative_estimates_are_refused()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));

        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Lunch, new PlaceholderFood(Calories: -1m)));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Lunch, new PlaceholderFood(WeightGrams: -1m)));
    }

    [Fact]
    public async Task A_placeholder_can_be_relabelled_and_reestimated()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));
        var added = await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, new PlaceholderFood(), 2m);

        await _app.Trips.UpdatePlaceholderAsync(trip.Id, July(9), added.Id, new PlaceholderFood(" Burrito ", 800m, 300m));

        var dinner = Entry(await ViewAsync(trip.Id), 9, MealSlot.Dinner);
        Assert.Equal(("Burrito", 2m, 1600m, 600m), (dinner.Name, dinner.Entry.Quantity, dinner.Calories, dinner.WeightGrams));
    }

    // ----- How it counts -----

    [Fact]
    public async Task Estimates_count_in_the_days_food_and_a_blank_one_is_flagged()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Breakfast, Breakfast, 2m);
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, new PlaceholderFood("Pizza", Calories: 900m));

        var view = await ViewAsync(trip.Id);
        var day = Day(view, 9);

        Assert.Equal((1700m, 226m), (day.Calories, day.FoodGrams));
        Assert.Equal((1, 0), (day.WithoutWeight, day.WithoutCalories));
        Assert.Equal(1, view.MealPlan.WithoutWeight);
        Assert.Equal(1, Assert.Single(view.MealPlan.FoodCarries).WithoutWeight);
    }

    [Fact]
    public async Task Only_placeholders_in_the_first_food_carry_count_in_starting_pack_weight()
    {
        var trip = await KennedyMeadowsAsync();
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(4), MealSlot.Lunch, new PlaceholderFood(WeightGrams: 150m));

        var view = await ViewAsync(trip.Id);

        Assert.Equal(150m, view.Weight.StartingPackGrams);
        Assert.Equal(3 * 113m + 2 * 198m, view.MealPlan.FoodCarries[1].FoodGrams);
    }

    [Fact]
    public async Task Eaten_placeholders_count_in_the_eaten_stats_and_food_left()
    {
        var trip = await KennedyMeadowsAsync();

        await _app.Trips.SetDayEatenAsync(trip.Id, July(9));
        var breakfast = Entry(await ViewAsync(trip.Id), 10, MealSlot.Breakfast);
        await _app.Trips.SetSlotEntryEatenAsync(trip.Id, July(10), breakfast.Entry.Id, true);

        var view = await ViewAsync(trip.Id);
        // Planned up to today: 9 and 10 July's breakfast and dinner.
        Assert.Equal((1500m, 2200m), (view.Stats.Eaten!.EatenCalories, view.Stats.Eaten.PlannedCalories));
        // Today (10 July) leaves its dinner and 11 July's breakfast.
        Assert.Equal(198m + 113m, view.FoodLeftGrams);
    }

    [Fact]
    public async Task Completing_and_reopening_leave_a_placeholder_as_it_is()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, Dinner);

        await _app.Trips.CompleteTripAsync(trip.Id);
        var completed = Entry(await ViewAsync(trip.Id), 9, MealSlot.Dinner);
        await _app.Trips.ReopenTripAsync(trip.Id);
        var reopened = Entry(await ViewAsync(trip.Id), 9, MealSlot.Dinner);

        Assert.Null(completed.Entry.Frozen);
        Assert.Equal((700m, 198m), (completed.Calories, completed.WeightGrams));
        Assert.Equal(completed.Entry, reopened.Entry);
    }

    [Fact]
    public async Task Placeholders_are_copied_unticked_with_a_meal_plan_and_onto_a_new_day()
    {
        var source = await _app.Trips.CreateTripAsync("PCT", July(9), July(10), openEnded: true);
        await _app.Trips.AddPlaceholderToSlotAsync(source.Id, July(9), MealSlot.Dinner, new PlaceholderFood("Pizza", 900m));
        await _app.Trips.SetDayEatenAsync(source.Id, July(9));
        var target = await _app.Trips.CreateTripAsync("Sierra", July(20), July(21));

        await _app.Trips.CopyMealPlanAsync(target.Id, source.Id);
        var added = await _app.Trips.AddDayAsync(source.Id, copyMealsFromPreviousDay: true);

        var copied = Assert.Single((await ViewAsync(target.Id)).Trip.Days[0].SlotEntries);
        Assert.Equal((new PlaceholderFood("Pizza", 900m), false), (copied.Placeholder, copied.Eaten));
        Assert.Equal(new PlaceholderFood("Pizza", 900m), Assert.Single(added.SlotEntries).Placeholder);
    }

    // ----- The shopping list -----

    [Fact]
    public async Task Placeholders_are_listed_per_slot_and_label_to_choose_at_the_store()
    {
        var trip = await KennedyMeadowsAsync();
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(11), MealSlot.Dinner, new PlaceholderFood("pizza in town"));
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(10), MealSlot.Snacks, bar.Id, 2m);

        var carry = (await ViewAsync(trip.Id)).MealPlan.FoodCarries[1];

        Assert.Equal("Clif Bar", Assert.Single(carry.Lines).Name);
        Assert.Equal(
            [(MealSlot.Breakfast, (string?)null, 3m, 339m, 0), (MealSlot.Dinner, null, 2m, 396m, 0), (MealSlot.Dinner, "pizza in town", 1m, 0m, 1)],
            carry.PlaceholderLines.Select(l => (l.Slot, l.Label, l.Quantity, l.WeightGrams, l.WithoutWeight)));
        Assert.Equal(4, carry.LineCount);
    }

    [Fact]
    public async Task A_placeholder_line_is_ticked_as_bought_until_its_quantity_goes_up()
    {
        var trip = await KennedyMeadowsAsync();

        await _app.Trips.SetPlaceholderBoughtAsync(trip.Id, July(9), MealSlot.Breakfast, null, true);
        await _app.Trips.SetPlaceholderBoughtAsync(trip.Id, July(9), MealSlot.Dinner, null, true);
        var ticked = (await ViewAsync(trip.Id)).MealPlan.FoodCarries[1];
        await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(11), MealSlot.Dinner, Dinner);
        var afterAdding = (await ViewAsync(trip.Id)).MealPlan.FoodCarries[1];

        Assert.Equal((2, 2), (ticked.BoughtCount, ticked.LineCount));
        Assert.Equal([true, false], afterAdding.PlaceholderLines.Select(l => l.Bought));
    }

    [Fact]
    public async Task Resetting_the_shopping_list_unticks_placeholder_lines_too()
    {
        var trip = await KennedyMeadowsAsync();
        await _app.Trips.SetPlaceholderBoughtAsync(trip.Id, July(9), MealSlot.Breakfast, null, true);

        await _app.Trips.ResetBoughtAsync(trip.Id, July(9));

        Assert.Equal(0, (await ViewAsync(trip.Id)).MealPlan.FoodCarries[1].BoughtCount);
    }

    [Fact]
    public async Task Ticking_a_placeholder_line_not_on_the_list_is_refused()
    {
        var trip = await KennedyMeadowsAsync();

        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.SetPlaceholderBoughtAsync(trip.Id, July(9), MealSlot.Lunch, null, true));
    }

    // ----- Replacing -----

    [Fact]
    public async Task Replacing_keeps_the_slot_quantity_and_eaten_tick()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(10));
        var snack = await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Snacks, new PlaceholderFood(), 2m);
        await _app.Trips.SetSlotEntryEatenAsync(trip.Id, July(9), snack.Id, true);
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));

        await _app.Trips.ReplacePlaceholderWithFoodItemAsync(trip.Id, July(9), snack.Id, bar.Id);

        var replaced = Entry(await ViewAsync(trip.Id), 9, MealSlot.Snacks);
        Assert.Equal(("Clif Bar", 2m, true, false), (replaced.Name, replaced.Entry.Quantity, replaced.Entry.Eaten, replaced.IsPlaceholder));
        Assert.Equal(520m, replaced.Calories);
    }

    [Fact]
    public async Task Replacing_with_what_the_slot_already_has_merges_them()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(10));
        var ramen = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Ramen", 85m, 380m));
        var bomb = await _app.Meals.CreateMealAsync(new MealDetails("Ramen Bomb", [new(ramen.Id, 1m)]));
        var meal = await _app.Trips.AddMealToSlotAsync(trip.Id, July(9), MealSlot.Dinner, bomb.Id);
        var placeholder = await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Dinner, Dinner, 2m);
        await _app.Trips.SetSlotEntryEatenAsync(trip.Id, July(9), placeholder.Id, true);

        await _app.Trips.ReplacePlaceholderWithMealAsync(trip.Id, July(9), placeholder.Id, bomb.Id);

        var merged = Entry(await ViewAsync(trip.Id), 9, MealSlot.Dinner);
        Assert.Equal((meal.Id, 3m, false), (merged.Entry.Id, merged.Entry.Quantity, merged.Entry.Eaten));
    }

    [Fact]
    public async Task Only_a_placeholder_can_be_replaced_and_not_with_retired_food()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(9), July(9));
        var bar = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Clif Bar", 68m, 260m));
        var old = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Old bar", 68m, 260m));
        await _app.Food.RetireFoodItemAsync(old.Id);
        var entry = await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(9), MealSlot.Snacks, bar.Id);
        var placeholder = await _app.Trips.AddPlaceholderToSlotAsync(trip.Id, July(9), MealSlot.Snacks, new PlaceholderFood());

        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.ReplacePlaceholderWithFoodItemAsync(trip.Id, July(9), entry.Id, bar.Id));
        await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.ReplacePlaceholderWithFoodItemAsync(trip.Id, July(9), placeholder.Id, old.Id));
    }
}
