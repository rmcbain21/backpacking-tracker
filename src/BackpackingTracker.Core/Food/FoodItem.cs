namespace BackpackingTracker.Core.Food;

public sealed record FoodItem : StoredRecord
{
    /// <summary>What the backpacker calls it, such as "Clif Bar, Chocolate Chip".</summary>
    public required string Name { get; init; }

    /// <summary>One serving's weight in grams, to 0.1 g (ADR 0002). Always more than zero.</summary>
    public required decimal ServingWeightGrams { get; init; }

    /// <summary>Calories (kcal) in one serving, to the nearest whole calorie.</summary>
    public required decimal CaloriesPerServing { get; init; }

    /// <summary>Grams of protein in one serving, to 0.1 g, if recorded.</summary>
    public decimal? ProteinGramsPerServing { get; init; }

    /// <summary>What one serving costs, if recorded.</summary>
    public decimal? CostPerServing { get; init; }

    /// <summary>Free-form labels used to find food, such as "no-cook" or "breakfast".</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>No longer used: hidden from the Food page and pickers, but still found by id.</summary>
    public bool Retired { get; init; }
}
