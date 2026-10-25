namespace BackpackingTracker.Core.Trips;

/// <summary>
/// What one Meal, or one serving of a Food Item, weighs and provides: copied onto a slot entry when its Trip is
/// Completed, so later edits to the Meal or Food Item never change a past Trip. Protein and cost are null when
/// not recorded.
/// </summary>
public sealed record FoodFigures(decimal WeightGrams, decimal Calories, decimal? ProteinGrams, decimal? Cost);
