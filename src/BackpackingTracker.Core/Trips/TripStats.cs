using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Trip's calculated stats: its length, its route totals (sums of what's typed in on its Trip Days), its pack
/// weights, and its food (weights and food frozen on a Completed Trip). Distances and elevation are in metres,
/// weights in grams. A route total is null when no Trip Day has that figure filled in, and the food figures are
/// null when nothing in the Meal Plan provides them, so "not recorded" isn't shown as zero.
/// Skipped Trip Days are left out of what was hiked (days, nights, actual distance, elevation gain) but kept in
/// what was planned (planned days and nights, planned distance) and in the food, which was still carried.
/// </summary>
/// <param name="Days">Days hiked: every Trip Day not Skipped.</param>
/// <param name="Nights">Nights out over the days hiked: one fewer than the days, never below zero.</param>
/// <param name="PlannedDays">Every Trip Day, Skipped or not; the same as <paramref name="Days"/> when none is Skipped.</param>
public sealed record TripStats(
    int Days,
    int Nights,
    int PlannedDays,
    int PlannedNights,
    decimal? PlannedDistanceMetres,
    decimal? ActualDistanceMetres,
    decimal? ElevationGainMetres,
    decimal StartingPackGrams,
    decimal BaseGrams,
    decimal? CaloriesPerDay,
    decimal? FoodCost)
{
    /// <summary>True when any day was Skipped, so the days hiked differ from the days planned.</summary>
    public bool AnySkipped => Days != PlannedDays;

    /// <param name="weight">The Trip's pack weight, from frozen weights on a Completed Trip.</param>
    /// <param name="mealPlan">The Trip's Meal Plan, from frozen food figures on a Completed Trip.</param>
    internal static TripStats For(Trip trip, PackWeight weight, MealPlanView mealPlan)
    {
        var hiked = trip.Days.Where(day => !day.Skipped).ToList();
        return new(
            hiked.Count,
            NightsFor(hiked.Count),
            trip.Days.Count,
            NightsFor(trip.Days.Count),
            Sum(trip.Days.Select(day => day.PlannedDistanceMetres)),
            Sum(hiked.Select(day => day.ActualDistanceMetres)),
            Sum(hiked.Select(day => day.ElevationGainMetres)),
            weight.StartingPackGrams,
            weight.BaseGrams,
            // Over every Trip Day, planned or not, Skipped or not: a partial first or last day lowers the average, as
            // it does on the trail, and skipping a day doesn't change how well each day's food was planned.
            mealPlan.IsEmpty || trip.Days.Count == 0 ? null : mealPlan.Calories / trip.Days.Count,
            mealPlan.Cost);
    }

    private static int NightsFor(int days) => Math.Max(days - 1, 0);

    /// <summary>The sum of the figures filled in, or null if none is.</summary>
    private static decimal? Sum(IEnumerable<decimal?> figures)
    {
        var filledIn = figures.OfType<decimal>().ToList();
        return filledIn.Count == 0 ? null : filledIn.Sum();
    }
}
