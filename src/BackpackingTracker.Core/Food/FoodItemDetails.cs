namespace BackpackingTracker.Core.Food;

/// <summary>What the backpacker fills in to add or edit a Food Item.</summary>
public sealed record FoodItemDetails(
    string Name,
    decimal ServingWeightGrams,
    decimal CaloriesPerServing,
    decimal? ProteinGramsPerServing = null,
    decimal? CostPerServing = null,
    IReadOnlyList<string>? Tags = null);
