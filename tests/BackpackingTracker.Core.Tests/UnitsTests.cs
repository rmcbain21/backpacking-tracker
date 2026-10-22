using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Tests;

public class UnitsTests
{
    [Theory]
    [InlineData(0, "0.0 oz")]
    [InlineData(28.3, "1.0 oz")]
    [InlineData(121.9, "4.3 oz")]
    [InlineData(450.0, "15.9 oz")]
    [InlineData(453.6, "1 lb 0.0 oz")]
    [InlineData(765.4, "1 lb 11.0 oz")]
    [InlineData(1134.0, "2 lb 8.0 oz")]
    public void Imperial_weights_show_ounces_under_a_pound_and_pounds_and_ounces_above(decimal grams, string expected)
    {
        Assert.Equal(expected, UnitFormat.Weight(grams, UnitSystem.Imperial));
    }

    [Fact]
    public void A_weight_that_rounds_up_to_16_ounces_shows_as_a_pound()
    {
        // 453.5 g is 15.997 oz, which would read as "16.0 oz".
        Assert.Equal("1 lb 0.0 oz", UnitFormat.Weight(453.5m, UnitSystem.Imperial));
    }

    [Theory]
    [InlineData(0, "0 g")]
    [InlineData(12.5, "12.5 g")]
    [InlineData(765, "765 g")]
    [InlineData(999.9, "999.9 g")]
    [InlineData(1000, "1.00 kg")]
    [InlineData(1234.5, "1.23 kg")]
    [InlineData(10875, "10.88 kg")]
    public void Metric_weights_show_grams_under_a_kilogram_and_kilograms_above(decimal grams, string expected)
    {
        Assert.Equal(expected, UnitFormat.Weight(grams, UnitSystem.Metric));
    }

    [Theory]
    [InlineData(0, UnitSystem.Imperial, "0.0 mi")]
    [InlineData(16093.44, UnitSystem.Imperial, "10.0 mi")]
    [InlineData(20000, UnitSystem.Imperial, "12.4 mi")]
    [InlineData(20000, UnitSystem.Metric, "20.0 km")]
    [InlineData(8450, UnitSystem.Metric, "8.5 km")]
    public void Distances_show_in_miles_or_kilometres_to_a_tenth(decimal metres, UnitSystem system, string expected)
    {
        Assert.Equal(expected, UnitFormat.Distance(metres, system));
    }

    [Theory]
    [InlineData(0, UnitSystem.Imperial, "0 ft")]
    [InlineData(716, UnitSystem.Imperial, "2,349 ft")]
    [InlineData(716, UnitSystem.Metric, "716 m")]
    [InlineData(1500.4, UnitSystem.Metric, "1,500 m")]
    public void Elevations_show_in_whole_feet_or_metres(decimal metres, UnitSystem system, string expected)
    {
        Assert.Equal(expected, UnitFormat.Elevation(metres, system));
    }

    [Theory]
    [InlineData("4.3", WeightUnit.Ounces, 121.9)]
    [InlineData("1.5", WeightUnit.Pounds, 680.4)]
    [InlineData("765.4", WeightUnit.Grams, 765.4)]
    [InlineData("765.44", WeightUnit.Grams, 765.4)]
    [InlineData(" 12 ", WeightUnit.Grams, 12)]
    [InlineData(".5", WeightUnit.Ounces, 14.2)]
    [InlineData("0", WeightUnit.Ounces, 0)]
    public void A_typed_weight_is_converted_to_grams_to_a_tenth(string text, WeightUnit unit, decimal expectedGrams)
    {
        var result = WeightInput.Parse(text, unit);

        Assert.Null(result.Error);
        Assert.Equal(expectedGrams, result.Grams);
    }

    [Theory]
    [InlineData("", "Enter a weight.")]
    [InlineData("   ", "Enter a weight.")]
    [InlineData("abc", "Enter a number, like 4.3.")]
    [InlineData("4.3 oz", "Enter a number, like 4.3.")]
    [InlineData("1,5", "Enter a number, like 4.3.")]
    [InlineData("-2", "A weight can't be negative.")]
    public void Typed_weights_that_are_not_a_valid_number_are_rejected_with_a_message(string text, string expectedError)
    {
        var result = WeightInput.Parse(text, WeightUnit.Ounces);

        Assert.Null(result.Grams);
        Assert.Equal(expectedError, result.Error);
    }

    [Theory]
    [InlineData(UnitSystem.Imperial, WeightUnit.Ounces)]
    [InlineData(UnitSystem.Metric, WeightUnit.Grams)]
    public void Weights_are_typed_in_ounces_or_grams_by_default(UnitSystem system, WeightUnit expected)
    {
        Assert.Equal(expected, WeightInput.DefaultUnit(system));
    }

    [Theory]
    [InlineData(WeightUnit.Ounces, "oz")]
    [InlineData(WeightUnit.Pounds, "lb")]
    [InlineData(WeightUnit.Grams, "g")]
    public void Weight_units_have_short_labels(WeightUnit unit, string expected)
    {
        Assert.Equal(expected, WeightInput.Label(unit));
    }

    public static TheoryData<int> TenthsOfAnOunceUpTo80Ounces() => new(Enumerable.Range(0, 801));

    [Theory]
    [MemberData(nameof(TenthsOfAnOunceUpTo80Ounces))]
    public void Typed_ounces_display_as_typed(int tenths)
    {
        var typed = $"{tenths / 10}.{tenths % 10}";
        var expected = tenths < 160
            ? $"{typed} oz"
            : $"{tenths / 160} lb {tenths % 160 / 10}.{tenths % 10} oz";

        var grams = WeightInput.Parse(typed, WeightUnit.Ounces).Grams!.Value;

        Assert.Equal(expected, UnitFormat.Weight(grams, UnitSystem.Imperial));
    }

    [Theory]
    [InlineData("2.5", WeightUnit.Pounds, UnitSystem.Imperial, "2 lb 8.0 oz")]
    [InlineData("765.4", WeightUnit.Grams, UnitSystem.Metric, "765.4 g")]
    [InlineData("1.2", WeightUnit.Pounds, UnitSystem.Metric, "544.3 g")]
    public void Typed_pounds_and_grams_round_trip(string typed, WeightUnit unit, UnitSystem system, string expected)
    {
        var grams = WeightInput.Parse(typed, unit).Grams!.Value;

        Assert.Equal(expected, UnitFormat.Weight(grams, system));
    }
}
