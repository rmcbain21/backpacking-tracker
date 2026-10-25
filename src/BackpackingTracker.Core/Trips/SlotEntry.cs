namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Meal or a single Food Item in one of a Trip Day's meal slots, with a quantity (Meals, or servings of a Food
/// Item), which may be fractional. Exactly one of MealId and FoodItemId is set. Kept inside its Trip Day.
/// </summary>
public sealed record SlotEntry
{
    public required string Id { get; init; }

    public required MealSlot Slot { get; init; }

    public string? MealId { get; init; }

    public string? FoodItemId { get; init; }

    public required decimal Quantity { get; init; }
}
