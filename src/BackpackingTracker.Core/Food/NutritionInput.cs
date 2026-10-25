using System.Globalization;

namespace BackpackingTracker.Core.Food;

/// <summary>The outcome of reading a typed nutrition figure: the amount (null for none), or a message to show.</summary>
public sealed record NutritionParseResult(decimal? Amount, string? Error);

/// <summary>Reads and shows calories and protein per serving.</summary>
public static class NutritionInput
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private const NumberStyles AllowedStyles =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands;

    /// <summary>Calories are required, to the nearest whole calorie.</summary>
    public static NutritionParseResult ParseCalories(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new NutritionParseResult(null, "Enter the calories in a serving.");
        }

        return ParseAmount(text, "Enter a number, like 250.", "Calories can't be negative.", decimals: 0);
    }

    /// <summary>Protein is optional, in grams to 0.1 g; blank means not recorded.</summary>
    public static NutritionParseResult ParseProtein(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? new NutritionParseResult(null, null)
            : ParseAmount(text, "Enter a number, like 9.5.", "Protein can't be negative.", decimals: 1);

    private static NutritionParseResult ParseAmount(string text, string notANumber, string negative, int decimals)
    {
        if (!decimal.TryParse(text, AllowedStyles, Culture, out var amount))
        {
            return new NutritionParseResult(null, notANumber);
        }

        return amount < 0
            ? new NutritionParseResult(null, negative)
            : new NutritionParseResult(Math.Round(amount, decimals, MidpointRounding.AwayFromZero), null);
    }

    public static string Calories(decimal calories) => $"{calories.ToString("#,0", Culture)} cal";

    public static string Protein(decimal grams) => $"{grams.ToString("0.#", Culture)} g protein";

    public static string TextForEditing(decimal? amount) => amount?.ToString("0.#", Culture) ?? "";
}
