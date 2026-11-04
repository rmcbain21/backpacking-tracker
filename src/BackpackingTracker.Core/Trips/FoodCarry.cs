using BackpackingTracker.Core.Food;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// The run of Trip Days whose food is bought at one place: from the start of the Trip ("from home"), or from a
/// Resupply, up to the day before the next Resupply. Its food weight and calories are its days' (frozen ones on a
/// Completed Trip), and its shopping list totals their Food Items.
/// </summary>
/// <param name="Number">1 for the Food Carry from home, then 2, 3… for each Resupply in order.</param>
/// <param name="Place">The Resupply's place name, or its day's start when blank; null from home or when both are blank.</param>
/// <param name="Days">Its days' Meal Plans, in order; never empty.</param>
/// <param name="Lines">Its shopping list: one line per Food Item, by name.</param>
public sealed record FoodCarry(int Number, string? Place, IReadOnlyList<DayMealPlan> Days, IReadOnlyList<ShoppingLine> Lines)
{
    public bool FromHome => Number == 1;

    /// <summary>The day the food is bought for: day 1, or the Resupply day. Its bought ticks are kept on it.</summary>
    public TripDay FirstDay => Days[0].Day;

    public int FirstDayNumber => Days[0].DayNumber;

    public int LastDayNumber => Days[^1].DayNumber;

    public DateOnly FirstDate => Days[0].Day.Date;

    public DateOnly LastDate => Days[^1].Day.Date;

    public decimal FoodGrams => Days.Sum(day => day.FoodGrams);

    public decimal Calories => Days.Sum(day => day.Calories);

    public int BoughtCount => Lines.Count(line => line.Bought);

    /// <summary>
    /// Splits the Trip's days into Food Carries at each Resupply day (one Food Carry when there's none), each with its
    /// shopping list. A Resupply on day 1 is ignored.
    /// </summary>
    internal static IReadOnlyList<FoodCarry> For(
        IReadOnlyList<DayMealPlan> days, IReadOnlyDictionary<string, MealView> meals, IReadOnlyDictionary<string, FoodItem> foodItems)
    {
        var runs = new List<List<DayMealPlan>>();
        foreach (var day in days)
        {
            if (runs.Count == 0 || day.Day.Resupply)
            {
                runs.Add([]);
            }

            runs[^1].Add(day);
        }

        return runs.Select((run, i) => new FoodCarry(
                i + 1,
                i == 0 ? null : run[0].Day.ResupplyPlace ?? run[0].Day.Start,
                run,
                ShoppingLinesFor(run, meals, foodItems)))
            .ToList();
    }

    /// <summary>
    /// The days' slot entries totalled per Food Item: a Food Item's servings × quantity, and each Meal broken into its
    /// Food Items (a component's servings × the Meal's quantity). A Meal deleted on another device can't be broken down,
    /// so it's left out; a deleted Food Item shows without a name.
    /// </summary>
    private static IReadOnlyList<ShoppingLine> ShoppingLinesFor(
        IReadOnlyList<DayMealPlan> days, IReadOnlyDictionary<string, MealView> meals, IReadOnlyDictionary<string, FoodItem> foodItems)
    {
        var totals = new Dictionary<string, (string? Name, decimal Servings, decimal Grams)>();

        void Add(string foodItemId, string? name, decimal servings, decimal grams)
        {
            var (_, servingsSoFar, gramsSoFar) = totals.GetValueOrDefault(foodItemId);
            totals[foodItemId] = (name, servingsSoFar + servings, gramsSoFar + grams);
        }

        foreach (var entryView in days.SelectMany(day => day.Slots).SelectMany(slot => slot.Entries))
        {
            var entry = entryView.Entry;
            if (entry.FoodItemId is { } foodItemId)
            {
                Add(foodItemId, entryView.Name, entry.Quantity, entryView.WeightGrams);
            }
            else if (entry.MealId is { } mealId && meals.TryGetValue(mealId, out var meal))
            {
                foreach (var component in meal.Components)
                {
                    var servings = component.Component.Servings * entry.Quantity;
                    Add(component.Component.FoodItemId, component.FoodItem?.Name, servings, (component.FoodItem?.ServingWeightGrams ?? 0m) * servings);
                }
            }
        }

        var bought = days[0].Day.Bought;
        return totals
            .Select(total => new ShoppingLine(
                total.Key,
                total.Value.Name,
                total.Value.Servings,
                total.Value.Grams,
                bought.Any(tick => tick.FoodItemId == total.Key && tick.Servings >= total.Value.Servings)))
            .OrderBy(line => line.Name is null)
            .ThenBy(line => line.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}

/// <summary>
/// One Food Item to buy for a Food Carry: its servings and weight across the Food Carry's days. Bought when it was
/// ticked with at least as many servings as it has now, so a line whose servings go up is unticked.
/// </summary>
/// <param name="Name">Null if the Food Item was deleted on another device.</param>
public sealed record ShoppingLine(string FoodItemId, string? Name, decimal Servings, decimal WeightGrams, bool Bought);
