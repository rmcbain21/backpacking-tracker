using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Trip's calculated stats: its length, its route totals (sums of what's typed in on its Trip Days), its pack
/// weights, and its food (weights and food frozen on a Completed Trip). Distances and elevation are in metres,
/// weights in grams. A route total is null when no Trip Day has that figure filled in, and the food figures are
/// null when nothing in the Meal Plan provides them, so "not recorded" isn't shown as zero.
/// </summary>
public sealed record TripStats(
    int Days,
    int Nights,
    decimal? PlannedDistanceMetres,
    decimal? ActualDistanceMetres,
    decimal? ElevationGainMetres,
    decimal StartingPackGrams,
    decimal BaseGrams,
    decimal? CaloriesPerDay,
    decimal? FoodCost)
{
    /// <param name="weight">The Trip's pack weight, from frozen weights on a Completed Trip.</param>
    /// <param name="mealPlan">The Trip's Meal Plan, from frozen food figures on a Completed Trip.</param>
    internal static TripStats For(Trip trip, PackWeight weight, MealPlanView mealPlan) =>
        new(trip.Days.Count,
            Math.Max(trip.Days.Count - 1, 0),
            Sum(trip.Days.Select(day => day.PlannedDistanceMetres)),
            Sum(trip.Days.Select(day => day.ActualDistanceMetres)),
            Sum(trip.Days.Select(day => day.ElevationGainMetres)),
            weight.StartingPackGrams,
            weight.BaseGrams,
            // Over every Trip Day, planned or not: a partial first or last day lowers the average, as it does on the trail.
            mealPlan.IsEmpty || trip.Days.Count == 0 ? null : mealPlan.Calories / trip.Days.Count,
            mealPlan.Cost);

    /// <summary>The sum of the figures filled in, or null if none is.</summary>
    private static decimal? Sum(IEnumerable<decimal?> figures)
    {
        var filledIn = figures.OfType<decimal>().ToList();
        return filledIn.Count == 0 ? null : filledIn.Sum();
    }
}
