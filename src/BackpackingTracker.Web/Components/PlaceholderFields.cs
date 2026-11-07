using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Trips;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Web.Components;

/// <summary>
/// What's typed in for a Placeholder: an optional label, and optional estimated calories and weight for one, the weight
/// in a chosen unit. Editing an existing Placeholder keeps its stored grams while the weight text is untouched.
/// </summary>
public sealed class PlaceholderFields
{
    private readonly decimal? _originalGrams;

    public PlaceholderFields()
    {
    }

    public PlaceholderFields(UnitSystem unitSystem, PlaceholderFood? existing = null)
    {
        WeightUnit = WeightInput.DefaultUnit(unitSystem);
        Label = existing?.Label;
        CaloriesText = existing?.Calories is { } calories ? NutritionInput.TextForEditing(calories) : null;
        _originalGrams = existing?.WeightGrams;
        WeightText = _originalGrams is { } grams ? WeightInput.TextForEditing(grams, WeightUnit) : null;
    }

    public string? Label { get; set; }

    public string? CaloriesText { get; set; }

    public string? WeightText { get; set; }

    public WeightUnit WeightUnit { get; private set; }

    /// <summary>Validates a typed weight, for the field's own message.</summary>
    public string? ValidateWeight(string? text) => ParseWeight(text).Error;

    /// <summary>Switching unit on an untouched weight shows the same weight in the new unit.</summary>
    public void ChangeWeightUnit(WeightUnit unit)
    {
        if (_originalGrams is { } grams && WeightInput.IsUnchanged(WeightText, WeightUnit, grams))
        {
            WeightText = WeightInput.TextForEditing(grams, unit);
        }

        WeightUnit = unit;
    }

    /// <summary>The Placeholder as typed, or null while a figure doesn't read.</summary>
    public PlaceholderFood? Read()
    {
        var calories = NutritionInput.ParseEstimatedCalories(CaloriesText);
        var weight = ParseWeight(WeightText);
        return calories.Error is null && weight.Error is null
            ? new PlaceholderFood(Label, calories.Amount, weight.Grams)
            : null;
    }

    /// <summary>Blank is no estimate; otherwise the typed weight, keeping the stored grams while the text is untouched.</summary>
    private WeightParseResult ParseWeight(string? text) =>
        string.IsNullOrWhiteSpace(text) ? new WeightParseResult(null, null)
        : _originalGrams is { } grams ? WeightInput.ParseEdited(text, WeightUnit, grams)
        : WeightInput.Parse(text, WeightUnit);
}
