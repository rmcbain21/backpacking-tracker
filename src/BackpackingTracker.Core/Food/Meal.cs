namespace BackpackingTracker.Core.Food;

/// <summary>A reusable, named combination of Food Items eaten together, such as a "Ramen Bomb".</summary>
public sealed record Meal : StoredRecord
{
    public required string Name { get; init; }

    /// <summary>The Food Items in the Meal, each once, in the order added.</summary>
    public IReadOnlyList<MealComponent> Components { get; init; } = [];

    /// <summary>Free-form labels used to find Meals, such as "dinner".</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>No longer used: hidden from the Food page and pickers, but still found by id.</summary>
    public bool Retired { get; init; }
}

/// <summary>A Food Item in a Meal and how many servings of it, which may be fractional (such as 0.5).</summary>
public sealed record MealComponent(string FoodItemId, decimal Servings);
