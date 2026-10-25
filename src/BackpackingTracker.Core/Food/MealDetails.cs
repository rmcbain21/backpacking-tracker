namespace BackpackingTracker.Core.Food;

/// <summary>What the backpacker fills in to create or edit a Meal.</summary>
public sealed record MealDetails(string Name, IReadOnlyList<MealComponent> Components, IReadOnlyList<string>? Tags = null);
