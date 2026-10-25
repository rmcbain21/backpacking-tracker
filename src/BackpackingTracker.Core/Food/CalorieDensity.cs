using System.Globalization;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Food;

/// <summary>
/// Calories per unit of weight, the figure backpackers use to judge how efficient food is to carry:
/// calories per ounce in imperial, per 100 g in metric.
/// </summary>
public static class CalorieDensity
{
    /// <summary>Calories per gram, or null when there is no weight to divide by.</summary>
    public static decimal? PerGram(decimal calories, decimal grams) => grams > 0 ? calories / grams : null;

    public static decimal? PerGram(FoodItem item) => PerGram(item.CaloriesPerServing, item.ServingWeightGrams);

    /// <summary>Such as "120 cal/oz" or "424 cal/100 g", to the nearest whole calorie; a dash when there's no weight.</summary>
    public static string Format(decimal? caloriesPerGram, UnitSystem system)
    {
        if (caloriesPerGram is not { } perGram)
        {
            return "—";
        }

        return system == UnitSystem.Metric
            ? $"{Whole(perGram * 100m)} cal/100 g"
            : $"{Whole(perGram * UnitConversion.GramsPerOunce)} cal/oz";
    }

    private static string Whole(decimal calories) =>
        Math.Round(calories, 0, MidpointRounding.AwayFromZero).ToString("#,0", CultureInfo.InvariantCulture);
}
