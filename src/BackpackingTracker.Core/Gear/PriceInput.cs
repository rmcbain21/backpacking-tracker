using System.Globalization;

namespace BackpackingTracker.Core.Gear;

/// <summary>The outcome of reading a typed price: the price (null for none), or a message to show.</summary>
public sealed record PriceParseResult(decimal? Price, string? Error);

/// <summary>Reads and shows prices, in dollars and cents.</summary>
public static class PriceInput
{
    private static readonly CultureInfo Culture = CultureInfo.InvariantCulture;

    private const NumberStyles AllowedStyles =
        NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite |
        NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint;

    /// <summary>A blank price means none. A leading "$" is allowed.</summary>
    public static PriceParseResult Parse(string? text)
    {
        var trimmed = text?.Trim().TrimStart('$') ?? "";
        if (trimmed.Length == 0)
        {
            return new PriceParseResult(null, null);
        }

        if (!decimal.TryParse(trimmed, AllowedStyles, Culture, out var price))
        {
            return new PriceParseResult(null, "Enter a price, like 12.99.");
        }

        if (price < 0)
        {
            return new PriceParseResult(null, "A price can't be negative.");
        }

        return new PriceParseResult(Math.Round(price, 2, MidpointRounding.AwayFromZero), null);
    }

    public static string Format(decimal price) => "$" + price.ToString("#,0.00", Culture);

    public static string TextForEditing(decimal? price) => price?.ToString("0.00", Culture) ?? "";
}
