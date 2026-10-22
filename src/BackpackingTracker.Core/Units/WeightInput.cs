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

    /// <summary>A stored weight as a number to edit in the given unit: oz to 0.1, lb to 0.01, g to 0.1.</summary>
    public static string TextForEditing(decimal grams, WeightUnit unit) => unit switch
    {
        WeightUnit.Ounces => Math.Round(grams / UnitConversion.GramsPerOunce, 1, MidpointRounding.AwayFromZero)
            .ToString("0.0", CultureInfo.InvariantCulture),
        WeightUnit.Pounds => Math.Round(grams / (UnitConversion.GramsPerOunce * UnitConversion.OuncesPerPound), 2, MidpointRounding.AwayFromZero)
            .ToString("0.00", CultureInfo.InvariantCulture),
        _ => grams.ToString("0.#", CultureInfo.InvariantCulture),
    };

    /// <summary>
    /// Like Parse, but if the text is still what TextForEditing showed, the original grams are kept:
    /// otherwise every save of an untouched weight shown in oz or lb would round it a little.
    /// </summary>
    public static WeightParseResult ParseEdited(string? text, WeightUnit unit, decimal originalGrams) =>
        IsUnchanged(text, unit, originalGrams)
            ? new WeightParseResult(originalGrams, null)
            : Parse(text, unit);

    /// <summary>True while the text still reads as TextForEditing showed the stored weight.</summary>
    public static bool IsUnchanged(string? text, WeightUnit unit, decimal originalGrams) =>
        text?.Trim() == TextForEditing(originalGrams, unit);

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
