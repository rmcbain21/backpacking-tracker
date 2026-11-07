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

    /// <summary>
    /// Servings per package is optional: blank means not recorded; otherwise a number more than zero, to 0.01.
    /// </summary>
    public static ServingsParseResult ParsePerPackage(string? text) =>
        string.IsNullOrWhiteSpace(text) ? new ServingsParseResult(null, null) : Parse(text);

    /// <summary>"6 servings", "1 serving", or with servings per package "3 packages (6 servings)" or "1.5 packages (3 servings)".</summary>
    public static string Amount(decimal servings, decimal? perPackage) =>
        perPackage is { } size
            ? $"{Packages(Round(servings / size))} ({Servings(servings)})"
            : Servings(servings);

    /// <summary>
    /// What to buy for the servings: with servings per package, whole packages rounded up, "2 packages (3 of 4 servings)";
    /// otherwise "3 servings".
    /// </summary>
    public static string ToBuy(decimal servings, decimal? perPackage)
    {
        if (perPackage is not { } size)
        {
            return Servings(servings);
        }

        var packages = WholePackages(servings, size);
        var bought = packages * size;
        return bought == servings
            ? $"{Packages(packages)} ({Servings(servings)})"
            : $"{Packages(packages)} ({Format(servings)} of {Servings(bought)})";
    }

    /// <summary>The whole packages needed for the servings, rounded up.</summary>
    public static decimal WholePackages(decimal servings, decimal perPackage) => Math.Ceiling(Round(servings / perPackage));

    public static string Servings(decimal servings) => $"{Format(servings)} serving{(servings == 1m ? "" : "s")}";

    public static string Packages(decimal packages) => $"{Format(packages)} package{(packages == 1m ? "" : "s")}";
}
