using BackpackingTracker.Core.Food;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A slot entry with its Meal or Food Item looked up, and what it weighs and provides: one Meal's or serving's
/// figures (frozen ones on a Completed Trip, otherwise current ones) times the quantity. Name is null if the Meal or
/// Food Item was deleted on another device; it then counts for nothing unless its figures were frozen first.
/// </summary>
public sealed record SlotEntryView(SlotEntry Entry, string? Name, decimal WeightGrams, decimal Calories, decimal? ProteinGrams, decimal? Cost)
{
    public bool IsMeal => Entry.MealId is not null;
}

/// <summary>One of a Trip Day's four meal slots, with its entries in the order added; empty slots are allowed.</summary>
public sealed record MealSlotView(MealSlot Slot, IReadOnlyList<SlotEntryView> Entries);

/// <summary>A Trip Day's Meal Plan: all four slots in order, and the day's calories and food weight.</summary>
public sealed record DayMealPlan(TripDay Day, int DayNumber, IReadOnlyList<MealSlotView> Slots, decimal Calories, decimal FoodGrams);

/// <summary>
/// A Trip's Meal Plan: every Trip Day's slots together, with calories and food weight per day, from each Meal's and
/// Food Item's current values (frozen ones on a Completed Trip). Its total food weight counts in the Trip's
/// Consumable Weight.
/// </summary>
public sealed record MealPlanView(IReadOnlyList<DayMealPlan> Days)
{
    public decimal FoodGrams => Days.Sum(day => day.FoodGrams);

    public decimal Calories => Days.Sum(day => day.Calories);

    /// <summary>The cost of the slot entries that have one; null when none does.</summary>
    public decimal? Cost
    {
        get
        {
            var costs = Days.SelectMany(day => day.Slots).SelectMany(slot => slot.Entries).Select(e => e.Cost).OfType<decimal>().ToList();
            return costs.Count == 0 ? null : costs.Sum();
        }
    }

    public bool IsEmpty => Days.All(day => day.Day.SlotEntries.Count == 0);

    /// <param name="meals">The Meals the Trip's slot entries use, by id; a missing one was deleted on another device.</param>
    /// <param name="foodItems">The Food Items they use, likewise.</param>
    internal static MealPlanView For(Trip trip, IReadOnlyDictionary<string, MealView> meals, IReadOnlyDictionary<string, FoodItem> foodItems)
    {
        SlotEntryView ViewOf(SlotEntry entry)
        {
            var name = entry.MealId is { } mealId
                ? meals.GetValueOrDefault(mealId)?.Meal.Name
                : foodItems.GetValueOrDefault(entry.FoodItemId ?? "")?.Name;
            var figures = entry.Frozen ?? CurrentFigures(entry, meals, foodItems);
            return figures is null
                ? new SlotEntryView(entry, name, 0m, 0m, null, null)
                : new SlotEntryView(entry, name,
                    figures.WeightGrams * entry.Quantity, figures.Calories * entry.Quantity,
                    figures.ProteinGrams * entry.Quantity, figures.Cost * entry.Quantity);
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

    /// <summary>One Meal's totals or one serving of the Food Item, as they are now; null if it was deleted.</summary>
    internal static FoodFigures? CurrentFigures(SlotEntry entry, IReadOnlyDictionary<string, MealView> meals, IReadOnlyDictionary<string, FoodItem> foodItems)
    {
        if (entry.MealId is { } mealId)
        {
            return meals.TryGetValue(mealId, out var meal)
                ? new FoodFigures(meal.Totals.WeightGrams, meal.Totals.Calories, meal.Totals.ProteinGrams, meal.Totals.Cost)
                : null;
        }

        return foodItems.TryGetValue(entry.FoodItemId ?? "", out var item)
            ? new FoodFigures(item.ServingWeightGrams, item.CaloriesPerServing, item.ProteinGramsPerServing, item.CostPerServing)
            : null;
    }
}
