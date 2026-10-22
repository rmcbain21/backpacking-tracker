using System.Globalization;

namespace BackpackingTracker.Core.Units;

/// <summary>
/// Formats stored metric values (grams, metres) for display in the chosen unit system (ADR 0002).
/// </summary>
public static class UnitFormat
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    public static string Weight(decimal grams, UnitSystem system)
    {
        return system == UnitSystem.Metric ? MetricWeight(grams) : ImperialWeight(grams);
    }

    public static string Distance(decimal metres, UnitSystem system)
    {
        return system == UnitSystem.Metric
            ? $"{(metres / 1000m).ToString("0.0", Culture)} km"
            : $"{(metres / UnitConversion.MetresPerMile).ToString("0.0", Culture)} mi";
    }

    public static string Elevation(decimal metres, UnitSystem system)
    {
        return system == UnitSystem.Metric
            ? $"{metres.ToString("#,0", Culture)} m"
            : $"{(metres / UnitConversion.MetresPerFoot).ToString("#,0", Culture)} ft";
    }

    private static string MetricWeight(decimal grams)
    {
        if (grams < 1000m)
        {
            return $"{grams.ToString("0.#", Culture)} g";
        }

        var kilograms = Math.Round(grams / 1000m, 2, MidpointRounding.AwayFromZero);
        return $"{kilograms.ToString("0.00", Culture)} kg";
    }

    private static string ImperialWeight(decimal grams)
    {
        // Round to the displayed precision first, so 15.97 oz shows as "1 lb 0.0 oz", not "16.0 oz".
        var ounces = Math.Round(grams / UnitConversion.GramsPerOunce, 1, MidpointRounding.AwayFromZero);
        if (ounces < UnitConversion.OuncesPerPound)
        {
            return $"{ounces.ToString("0.0", Culture)} oz";
        }

        var pounds = Math.Floor(ounces / UnitConversion.OuncesPerPound);
        var remainder = ounces - pounds * UnitConversion.OuncesPerPound;
        return $"{pounds.ToString("0", Culture)} lb {remainder.ToString("0.0", Culture)} oz";
    }
}
