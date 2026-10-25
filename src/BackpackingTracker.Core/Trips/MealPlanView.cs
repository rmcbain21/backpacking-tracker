using BackpackingTracker.Core.Food;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A slot entry with its Meal or Food Item looked up, and what it weighs and its calories: the Meal's totals or the
/// Food Item's serving values, times the quantity. Name is null, and it counts for nothing, if the Meal or Food Item
/// was deleted on another device.
/// </summary>
public sealed record SlotEntryView(SlotEntry Entry, string? Name, decimal WeightGrams, decimal Calories)
{
    public bool IsMeal => Entry.MealId is not null;
}

/// <summary>One of a Trip Day's four meal slots, with its entries in the order added; empty slots are allowed.</summary>
public sealed record MealSlotView(MealSlot Slot, IReadOnlyList<SlotEntryView> Entries);

/// <summary>A Trip Day's Meal Plan: all four slots in order, and the day's calories and food weight.</summary>
public sealed record DayMealPlan(TripDay Day, int DayNumber, IReadOnlyList<MealSlotView> Slots, decimal Calories, decimal FoodGrams);

/// <summary>
/// A Trip's Meal Plan: every Trip Day's slots together, with calories and food weight per day, from each Meal's and
/// Food Item's current values. Its total food weight counts in the Trip's Consumable Weight.
/// </summary>
public sealed record MealPlanView(IReadOnlyList<DayMealPlan> Days)
{
    public decimal FoodGrams => Days.Sum(day => day.FoodGrams);

    public decimal Calories => Days.Sum(day => day.Calories);

    /// <param name="meals">The Meals the Trip's slot entries use, by id; a missing one was deleted on another device.</param>
    /// <param name="foodItems">The Food Items they use, likewise.</param>
    internal static MealPlanView For(Trip trip, IReadOnlyDictionary<string, MealView> meals, IReadOnlyDictionary<string, FoodItem> foodItems)
    {
        SlotEntryView ViewOf(SlotEntry entry)
        {
            if (entry.MealId is { } mealId)
            {
                return meals.TryGetValue(mealId, out var meal)
                    ? new SlotEntryView(entry, meal.Meal.Name, meal.Totals.WeightGrams * entry.Quantity, meal.Totals.Calories * entry.Quantity)
                    : new SlotEntryView(entry, null, 0m, 0m);
            }

            return foodItems.TryGetValue(entry.FoodItemId ?? "", out var item)
                ? new SlotEntryView(entry, item.Name, item.ServingWeightGrams * entry.Quantity, item.CaloriesPerServing * entry.Quantity)
                : new SlotEntryView(entry, null, 0m, 0m);
        }

        var days = trip.Days.Select((day, i) =>
        {
            var entries = day.SlotEntries.Select(ViewOf).ToList();
            var slots = Enum.GetValues<MealSlot>()
                .Select(slot => new MealSlotView(slot, entries.Where(e => e.Entry.Slot == slot).ToList()))
                .ToList();
            return new DayMealPlan(day, i + 1, slots, entries.Sum(e => e.Calories), entries.Sum(e => e.WeightGrams));
        });
        return new MealPlanView(days.ToList());
    }
}
