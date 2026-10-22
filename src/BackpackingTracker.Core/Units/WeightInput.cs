using System.Globalization;

namespace BackpackingTracker.Core.Units;

/// <summary>The outcome of parsing a typed weight: grams on success, otherwise a message to show.</summary>
public sealed record WeightParseResult(decimal? Grams, string? Error);

/// <summary>Turns a weight typed in oz, lb or g into stored grams (to 0.1 g, ADR 0002).</summary>
public static class WeightInput
{
    private const NumberStyles AllowedStyles =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    /// <summary>The unit a weight is typed in unless the backpacker picks another.</summary>
    public static WeightUnit DefaultUnit(UnitSystem system) =>
        system == UnitSystem.Metric ? WeightUnit.Grams : WeightUnit.Ounces;

    public static string Label(WeightUnit unit) => unit switch
    {
        WeightUnit.Ounces => "oz",
        WeightUnit.Pounds => "lb",
        _ => "g",
    };

    public static WeightParseResult Parse(string? text, WeightUnit unit)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new WeightParseResult(null, "Enter a weight.");
        }

        if (!decimal.TryParse(text, AllowedStyles, CultureInfo.InvariantCulture, out var amount))
        {
            return new WeightParseResult(null, "Enter a number, like 4.3.");
        }

        if (amount < 0)
        {
            return new WeightParseResult(null, "A weight can't be negative.");
        }

        var grams = unit switch
        {
            WeightUnit.Ounces => amount * UnitConversion.GramsPerOunce,
            WeightUnit.Pounds => amount * UnitConversion.OuncesPerPound * UnitConversion.GramsPerOunce,
            _ => amount,
        };

        return new WeightParseResult(Math.Round(grams, 1, MidpointRounding.AwayFromZero), null);
    }
}
