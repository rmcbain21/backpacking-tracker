namespace BackpackingTracker.Core.Food;

/// <summary>
/// What a Meal adds up to, from its Food Items' current values times their servings. Protein and cost
/// count only the Food Items that record them, and are null when none do.
/// </summary>
public sealed record MealTotals(decimal WeightGrams, decimal Calories, decimal? ProteinGrams, decimal? Cost)
{
    public decimal? CaloriesPerGram => CalorieDensity.PerGram(Calories, WeightGrams);

    public static MealTotals Of(IEnumerable<(FoodItem Item, decimal Servings)> components)
    {
        var list = components.ToList();
        return new MealTotals(
            list.Sum(c => c.Item.ServingWeightGrams * c.Servings),
            list.Sum(c => c.Item.CaloriesPerServing * c.Servings),
            SumKnown(list, c => c.Item.ProteinGramsPerServing * c.Servings),
            SumKnown(list, c => c.Item.CostPerServing * c.Servings));
    }

    private static decimal? SumKnown(IReadOnlyList<(FoodItem Item, decimal Servings)> components, Func<(FoodItem Item, decimal Servings), decimal?> amount)
    {
        var known = components.Select(amount).OfType<decimal>().ToList();
        return known.Count == 0 ? null : known.Sum();
    }
}
