using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Trip's calculated stats: its length, its route totals (sums of what's typed in on its Trip Days) and its
/// pack weights (frozen on a Completed Trip). Distances and elevation are in metres, weights in grams. A route
/// total is null when no Trip Day has that figure filled in, so "not recorded" isn't shown as zero.
/// </summary>
public sealed record TripStats(
    int Days,
    int Nights,
    decimal? PlannedDistanceMetres,
    decimal? ActualDistanceMetres,
    decimal? ElevationGainMetres,
    decimal StartingPackGrams,
    decimal BaseGrams)
{
    /// <param name="weight">The Trip's pack weight, from frozen weights on a Completed Trip.</param>
    internal static TripStats For(Trip trip, PackWeight weight) =>
        new(trip.Days.Count,
            Math.Max(trip.Days.Count - 1, 0),
            Sum(trip.Days.Select(day => day.PlannedDistanceMetres)),
            Sum(trip.Days.Select(day => day.ActualDistanceMetres)),
            Sum(trip.Days.Select(day => day.ElevationGainMetres)),
            weight.StartingPackGrams,
            weight.BaseGrams);

    /// <summary>The sum of the figures filled in, or null if none is.</summary>
    private static decimal? Sum(IEnumerable<decimal?> figures)
    {
        var filledIn = figures.OfType<decimal>().ToList();
        return filledIn.Count == 0 ? null : filledIn.Sum();
    }
}
