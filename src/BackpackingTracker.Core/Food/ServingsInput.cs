using System.Globalization;

namespace BackpackingTracker.Core.Food;

/// <summary>The outcome of reading a typed number of servings: the servings, or a message to show.</summary>
public sealed record ServingsParseResult(decimal? Servings, string? Error);

/// <summary>Reads and shows numbers of servings, which may be fractional, to 0.01.</summary>
public static class ServingsInput
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private const NumberStyles AllowedStyles =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    public static ServingsParseResult Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new ServingsParseResult(null, "Enter the servings.");
        }

        if (!decimal.TryParse(text, AllowedStyles, Culture, out var servings))
        {
            return new ServingsParseResult(null, "Enter a number, like 1.5.");
        }

        var rounded = Round(servings);
        return rounded <= 0
            ? new ServingsParseResult(null, "Servings must be more than zero.")
            : new ServingsParseResult(rounded, null);
    }

    internal static decimal Round(decimal servings) => Math.Round(servings, 2, MidpointRounding.AwayFromZero);

    /// <summary>Such as "1", "0.5" or "1.25".</summary>
    public static string Format(decimal servings) => servings.ToString("0.##", Culture);
}
