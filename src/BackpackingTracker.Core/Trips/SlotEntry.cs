using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Meal, a single Food Item or a Placeholder in one of a Trip Day's meal slots, with a quantity (Meals, servings of a
/// Food Item, or Placeholders), which may be fractional. Exactly one of MealId, FoodItemId and Placeholder is set. Kept
/// inside its Trip Day.
/// </summary>
public sealed record SlotEntry
{
    public required string Id { get; init; }

    public required MealSlot Slot { get; init; }

    public string? MealId { get; init; }

    public string? FoodItemId { get; init; }

    /// <summary>Set on a Placeholder: food not chosen yet, with its label and its estimates for one.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public PlaceholderFood? Placeholder { get; init; }

    public required decimal Quantity { get; init; }

    /// <summary>
    /// One Meal's or serving's figures as they were when the Trip was Completed; null on a Planned Trip, whose food
    /// uses each Meal's and Food Item's current values. Always null on a Placeholder, whose estimates are already fixed.
    /// </summary>
    public FoodFigures? Frozen { get; init; }

    /// <summary>
    /// Ticked as eaten, covering the whole quantity: once the Trip has started, on any of its days. Only shows
    /// progress; no weight changes. Never copied with the entry.
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Eaten { get; init; }

    [JsonIgnore]
    public bool IsPlaceholder => Placeholder is not null;
}

/// <summary>
/// What a Placeholder holds: an optional label ("pizza in town") and optional estimates of the calories and weight of
/// one. A blank estimate counts as zero, and is flagged wherever food is totalled.
/// </summary>
public sealed record PlaceholderFood(string? Label = null, decimal? Calories = null, decimal? WeightGrams = null)
{
    /// <summary>The name a Placeholder shows: its label, or "Any breakfast" from its slot when unlabelled.</summary>
    public string NameIn(MealSlot slot) => Label ?? $"Any {AnyName(slot)}";

    private static string AnyName(MealSlot slot) => slot switch
    {
        MealSlot.Snacks => "snack",
        _ => slot.ToString().ToLowerInvariant(),
    };
}
