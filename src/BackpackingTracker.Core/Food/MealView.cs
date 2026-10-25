namespace BackpackingTracker.Core.Food;

/// <summary>A Meal component with its Food Item looked up (null if it was deleted on another device).</summary>
public sealed record MealComponentView(MealComponent Component, FoodItem? FoodItem);

/// <summary>A Meal with its Food Items looked up and its totals from their current values.</summary>
public sealed record MealView(Meal Meal, IReadOnlyList<MealComponentView> Components, MealTotals Totals);
