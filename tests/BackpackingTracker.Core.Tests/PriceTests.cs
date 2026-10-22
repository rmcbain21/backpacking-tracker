using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Tests;

public class PriceTests
{
    [Theory]
    [InlineData("12.5", 12.50)]
    [InlineData(" $12.99 ", 12.99)]
    [InlineData("0", 0)]
    [InlineData("4.999", 5.00)]
    public void A_typed_price_is_read_to_the_cent(string text, decimal expected)
    {
        var result = PriceInput.Parse(text);

        Assert.Null(result.Error);
        Assert.Equal(expected, result.Price);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_price_means_no_price(string? text)
    {
        var result = PriceInput.Parse(text);

        Assert.Null(result.Error);
        Assert.Null(result.Price);
    }

    [Theory]
    [InlineData("abc", "Enter a price, like 12.99.")]
    [InlineData("1,299", "Enter a price, like 12.99.")]
    [InlineData("-3", "A price can't be negative.")]
    public void An_invalid_price_is_rejected_with_a_message(string text, string expected)
    {
        Assert.Equal(expected, PriceInput.Parse(text).Error);
    }

    [Theory]
    [InlineData(12.5, "$12.50")]
    [InlineData(1299, "$1,299.00")]
    [InlineData(0, "$0.00")]
    public void Prices_are_shown_in_dollars_and_cents(decimal price, string expected)
    {
        Assert.Equal(expected, PriceInput.Format(price));
    }

    [Fact]
    public void A_price_is_shown_for_editing_without_the_dollar_sign()
    {
        Assert.Equal("12.50", PriceInput.TextForEditing(12.5m));
        Assert.Equal("", PriceInput.TextForEditing(null));
    }
}
