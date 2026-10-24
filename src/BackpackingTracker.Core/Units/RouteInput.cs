using System.Globalization;

namespace BackpackingTracker.Core.Units;

/// <summary>The outcome of parsing a typed distance or elevation: metres (null when left blank), otherwise a message to show.</summary>
public sealed record LengthParseResult(decimal? Metres, string? Error);

/// <summary>
/// Turns a distance typed in miles or kilometres, or an elevation typed in feet or metres, into stored
/// metres (to 0.1 m, ADR 0002). Both are optional, so blank text is no value rather than an error.
/// </summary>
public static class RouteInput
{
    private const NumberStyles AllowedStyles =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands;

    public static string DistanceLabel(UnitSystem system) => system == UnitSystem.Metric ? "km" : "mi";

    public static string ElevationLabel(UnitSystem system) => system == UnitSystem.Metric ? "m" : "ft";

    /// <summary>A stored distance as a number to edit: miles or kilometres to 0.1; blank when there's none.</summary>
    public static string DistanceText(decimal? metres, UnitSystem system) =>
        metres is null ? "" : Math.Round(metres.Value / MetresPerDistanceUnit(system), 1, MidpointRounding.AwayFromZero)
            .ToString("0.0", CultureInfo.InvariantCulture);

    /// <summary>A stored elevation as a number to edit: whole feet or metres; blank when there's none.</summary>
    public static string ElevationText(decimal? metres, UnitSystem system) =>
        metres is null ? "" : Math.Round(metres.Value / MetresPerElevationUnit(system), 0, MidpointRounding.AwayFromZero)
            .ToString("0", CultureInfo.InvariantCulture);

    /// <summary>
    /// Parses a typed distance. If the text is still what DistanceText showed for the original, the original
    /// metres are kept, so saving an untouched distance doesn't round it.
    /// </summary>
    public static LengthParseResult ParseDistance(string? text, UnitSystem system, decimal? originalMetres = null) =>
        originalMetres is not null && text?.Trim() == DistanceText(originalMetres, system)
            ? new LengthParseResult(originalMetres, null)
            : Parse(text, MetresPerDistanceUnit(system), "A distance");

    /// <summary>Parses a typed elevation, keeping the original metres while the text is unchanged.</summary>
    public static LengthParseResult ParseElevation(string? text, UnitSystem system, decimal? originalMetres = null) =>
        originalMetres is not null && text?.Trim() == ElevationText(originalMetres, system)
            ? new LengthParseResult(originalMetres, null)
            : Parse(text, MetresPerElevationUnit(system), "An elevation");

    private static decimal MetresPerDistanceUnit(UnitSystem system) =>
        system == UnitSystem.Metric ? 1000m : UnitConversion.MetresPerMile;

    private static decimal MetresPerElevationUnit(UnitSystem system) =>
        system == UnitSystem.Metric ? 1m : UnitConversion.MetresPerFoot;

    private static LengthParseResult Parse(string? text, decimal metresPerUnit, string what)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new LengthParseResult(null, null);
        }

        if (!decimal.TryParse(text, AllowedStyles, CultureInfo.InvariantCulture, out var amount))
        {
            return new LengthParseResult(null, "Enter a number, like 8.5.");
        }

        if (amount < 0)
        {
            return new LengthParseResult(null, $"{what} can't be negative.");
        }

        return new LengthParseResult(Math.Round(amount * metresPerUnit, 1, MidpointRounding.AwayFromZero), null);
    }
}
