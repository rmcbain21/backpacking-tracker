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

    [Theory]
    [InlineData(765.0, WeightUnit.Ounces, "27.0")]
    [InlineData(765.0, WeightUnit.Pounds, "1.69")]
    [InlineData(765.0, WeightUnit.Grams, "765")]
    [InlineData(765.4, WeightUnit.Grams, "765.4")]
    public void A_stored_weight_is_shown_for_editing_in_the_chosen_unit(decimal grams, WeightUnit unit, string expected)
    {
        Assert.Equal(expected, WeightInput.TextForEditing(grams, unit));
    }

    [Theory]
    [InlineData(WeightUnit.Ounces)]
    [InlineData(WeightUnit.Pounds)]
    [InlineData(WeightUnit.Grams)]
    public void An_unchanged_weight_keeps_the_exact_stored_grams(WeightUnit unit)
    {
        // 765.0 g shows as "27.0" oz, which would parse back as 765.4 g.
        var text = WeightInput.TextForEditing(765.0m, unit);

        Assert.Equal(765.0m, WeightInput.ParseEdited($" {text} ", unit, originalGrams: 765.0m).Grams);
    }

    [Theory]
    [InlineData("27.0", true)]
    [InlineData(" 27.0 ", true)]
    [InlineData("27", false)]
    [InlineData("27.1", false)]
    public void A_weight_is_unchanged_while_it_reads_as_it_was_shown(string text, bool expected)
    {
        Assert.Equal(expected, WeightInput.IsUnchanged(text, WeightUnit.Ounces, originalGrams: 765.0m));
    }

    [Fact]
    public void A_changed_weight_is_parsed_as_typed()
    {
        Assert.Equal(768.3m, WeightInput.ParseEdited("27.1", WeightUnit.Ounces, originalGrams: 765.0m).Grams);
    }

    [Fact]
    public void An_invalid_edited_weight_is_rejected()
    {
        Assert.Equal("Enter a number, like 4.3.", WeightInput.ParseEdited("abc", WeightUnit.Ounces, 765.0m).Error);
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
    [InlineData("2", WeightUnit.Litres, UnitSystem.Metric, "2.00 kg")]
    [InlineData("1.5", WeightUnit.Litres, UnitSystem.Imperial, "3 lb 4.9 oz")]
    public void Typed_pounds_grams_and_litres_round_trip(string typed, WeightUnit unit, UnitSystem system, string expected)
    {
        var grams = WeightInput.Parse(typed, unit).Grams!.Value;

        Assert.Equal(expected, UnitFormat.Weight(grams, system));
    }
}

public class WaterUnitsTests
{
    [Theory]
    [InlineData("2", 2000)]
    [InlineData("1.75", 1750)]
    [InlineData("0.5", 500)]
    public void Litres_of_water_become_grams(string typed, decimal expected)
    {
        Assert.Equal(new WeightParseResult(expected, null), WeightInput.Parse(typed, WeightUnit.Litres));
    }

    [Theory]
    [InlineData(2000, "2.0", "2.0 L")]
    [InlineData(1750, "1.75", "1.75 L")]
    [InlineData(1999.4, "2.0", "2.0 L")]
    public void Water_shows_in_litres(decimal grams, string forEditing, string shown)
    {
        Assert.Equal(forEditing, WeightInput.TextForEditing(grams, WeightUnit.Litres));
        Assert.Equal(shown, UnitFormat.WaterVolume(grams));
    }

    [Fact]
    public void Litres_are_offered_for_water_first_but_not_for_gear()
    {
        Assert.Equal(WeightUnit.Litres, WeightInput.WaterUnits[0]);
        Assert.DoesNotContain(WeightUnit.Litres, WeightInput.GearUnits);
        Assert.Equal("L", WeightInput.Label(WeightUnit.Litres));
    }
}

public class RouteInputTests
{
    [Theory]
    [InlineData("8.5", UnitSystem.Imperial, 13679.4)]
    [InlineData("8.5", UnitSystem.Metric, 8500)]
    [InlineData("1,000", UnitSystem.Imperial, 1609344)]
    [InlineData(" 0 ", UnitSystem.Metric, 0)]
    public void Typed_distances_become_metres(string typed, UnitSystem system, decimal expected)
    {
        Assert.Equal(new LengthParseResult(expected, null), RouteInput.ParseDistance(typed, system));
    }

    [Theory]
    [InlineData("2349", UnitSystem.Imperial, 716)]
    [InlineData("1000", UnitSystem.Imperial, 304.8)]
    [InlineData("716", UnitSystem.Metric, 716)]
    public void Typed_elevations_become_metres(string typed, UnitSystem system, decimal expected)
    {
        Assert.Equal(new LengthParseResult(expected, null), RouteInput.ParseElevation(typed, system));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void A_blank_distance_or_elevation_is_no_value_not_an_error(string? typed)
    {
        Assert.Equal(new LengthParseResult(null, null), RouteInput.ParseDistance(typed, UnitSystem.Imperial));
        Assert.Equal(new LengthParseResult(null, null), RouteInput.ParseElevation(typed, UnitSystem.Imperial));
    }

    [Fact]
    public void Text_and_negative_numbers_are_refused()
    {
        Assert.Equal("Enter a number, like 8.5.", RouteInput.ParseDistance("far", UnitSystem.Imperial).Error);
        Assert.Equal("A distance can't be negative.", RouteInput.ParseDistance("-1", UnitSystem.Imperial).Error);
        Assert.Equal("An elevation can't be negative.", RouteInput.ParseElevation("-1", UnitSystem.Metric).Error);
    }

    [Theory]
    [InlineData(13679.4, UnitSystem.Imperial, "8.5")]
    [InlineData(8450, UnitSystem.Metric, "8.5")]
    public void Stored_distances_show_to_a_tenth_for_editing(decimal metres, UnitSystem system, string expected)
    {
        Assert.Equal(expected, RouteInput.DistanceText(metres, system));
        Assert.Equal("", RouteInput.DistanceText(null, system));
    }

    [Theory]
    [InlineData(716, UnitSystem.Imperial, "2349")]
    [InlineData(716.4, UnitSystem.Metric, "716")]
    public void Stored_elevations_show_in_whole_units_for_editing(decimal metres, UnitSystem system, string expected)
    {
        Assert.Equal(expected, RouteInput.ElevationText(metres, system));
        Assert.Equal("", RouteInput.ElevationText(null, system));
    }

    [Fact]
    public void An_untouched_distance_or_elevation_keeps_its_stored_metres()
    {
        // 14,000 m shows as "8.7" mi, which on its own would parse back to 14001.3 m.
        Assert.Equal(14000m, RouteInput.ParseDistance("8.7", UnitSystem.Imperial, 14000m).Metres);
        Assert.Equal(14001.3m, RouteInput.ParseDistance("8.7", UnitSystem.Imperial).Metres);
        Assert.Equal(716m, RouteInput.ParseElevation("2349", UnitSystem.Imperial, 716m).Metres);
        Assert.Equal(16093.4m, RouteInput.ParseDistance("10", UnitSystem.Imperial, 14000m).Metres);
    }
}
