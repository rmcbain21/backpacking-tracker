using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Trip's calculated stats: its length, its route totals (sums of what's typed in on its Trip Days), its pack
/// weights, and its food (weights and food frozen on a Completed Trip). Distances and elevation are in metres,
/// weights in grams. A route total is null when no Trip Day has that figure filled in, and the food figures are
/// null when nothing in the Meal Plan provides them, so "not recorded" isn't shown as zero.
/// Skipped Trip Days are left out of what was hiked (days, nights, actual distance, elevation gain) but kept in
/// what was planned (planned days and nights, planned distance) and in the food, which was still carried.
/// With an Expected Distance there is progress too, and on an Open-ended Trip, once enough is hiked, a finish estimate.
/// Once anything is ticked as eaten, the food eaten against what was planned.
/// </summary>
/// <param name="HeaviestPack">With any Resupply, the Food Carry setting out with the heaviest pack (the first on a tie).</param>
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
    decimal? FoodCost,
    TripProgress? Progress,
    FinishEstimate? FinishEstimate,
    FoodEaten? Eaten,
    LongestFoodCarry? LongestFoodCarry,
    FoodCarry? HeaviestPack)
{
    /// <summary>The hiked days with an actual distance needed before an Open-ended Trip's finish is estimated.</summary>
    internal const int DaysForFinishEstimate = 3;

    /// <summary>True when any day was Skipped, so the days hiked differ from the days planned.</summary>
    public bool AnySkipped => Days != PlannedDays;

    /// <param name="weight">The Trip's pack weight, from frozen weights on a Completed Trip.</param>
    /// <param name="mealPlan">The Trip's Meal Plan, from frozen food figures on a Completed Trip.</param>
    /// <param name="today">The date it's looked at: a Planned Trip's food eaten counts the days up to it.</param>
    internal static TripStats For(Trip trip, PackWeight weight, MealPlanView mealPlan, DateOnly today)
    {
        var hiked = trip.Days.Where(day => !day.Skipped).ToList();
        var actualDistance = Sum(hiked.Select(day => day.ActualDistanceMetres));
        var progress = trip.ExpectedDistanceMetres is { } expected ? new TripProgress(actualDistance ?? 0m, expected) : null;
        return new(
            hiked.Count,
            NightsFor(hiked.Count),
            trip.Days.Count,
            NightsFor(trip.Days.Count),
            Sum(trip.Days.Select(day => day.PlannedDistanceMetres)),
            actualDistance,
            Sum(hiked.Select(day => day.ElevationGainMetres)),
            weight.StartingPackGrams,
            weight.BaseGrams,
            // Over every Trip Day, planned or not, Skipped or not: a partial first or last day lowers the average, as
            // it does on the trail, and skipping a day doesn't change how well each day's food was planned.
            mealPlan.IsEmpty || trip.Days.Count == 0 ? null : mealPlan.Calories / trip.Days.Count,
            mealPlan.Cost,
            progress,
            trip.OpenEnded && progress is not null ? EstimateFinish(trip, hiked, progress) : null,
            EatenFor(trip, mealPlan, today),
            LongestFoodCarryOf(mealPlan),
            mealPlan.Resupplies == 0 ? null : mealPlan.FoodCarries.MaxBy(carry => carry.PackOutGrams));
    }

    /// <summary>With any Resupply, the Food Carry with the most days (the heavier one on a tie); otherwise null.</summary>
    private static LongestFoodCarry? LongestFoodCarryOf(MealPlanView mealPlan) =>
        mealPlan.Resupplies == 0
            ? null
            : mealPlan.FoodCarries
                .OrderByDescending(carry => carry.Days.Count)
                .ThenByDescending(carry => carry.FoodGrams)
                .Select(carry => new LongestFoodCarry(mealPlan.Resupplies, carry.Days.Count, carry.FoodGrams))
                .First();

    /// <summary>
    /// The calories eaten against those planned, over the days up to and including today on a Planned Trip, or every
    /// day once Completed; Skipped days count, since their food was still carried. Null until anything is ticked.
    /// </summary>
    private static FoodEaten? EatenFor(Trip trip, MealPlanView mealPlan, DateOnly today)
    {
        var days = mealPlan.Days.Where(day => trip.Status == TripStatus.Completed || day.Day.Date <= today).ToList();
        if (!days.Any(day => day.Day.SlotEntries.Any(entry => entry.Eaten)))
        {
            return null;
        }

        return new FoodEaten(days.Sum(day => day.EatenCalories), days.Sum(day => day.Calories), days.Count);
    }

    /// <summary>
    /// When an Open-ended Trip would reach its Expected Distance at the pace so far: the actual distance over every day
    /// hiked, zero days included. Null until enough days have an actual distance, or once the distance is covered.
    /// </summary>
    private static FinishEstimate? EstimateFinish(Trip trip, IReadOnlyList<TripDay> hiked, TripProgress progress)
    {
        if (hiked.Count(day => day.ActualDistanceMetres is not null) < DaysForFinishEstimate || progress.RemainingMetres == 0)
        {
            return null;
        }

        var pace = progress.ActualMetres / hiked.Count;
        if (pace == 0)
        {
            return null;
        }

        var daysToGo = (int)Math.Ceiling(progress.RemainingMetres / pace);
        return new FinishEstimate(trip.EndDate.AddDays(daysToGo), daysToGo);
    }

    private static int NightsFor(int days) => Math.Max(days - 1, 0);

    /// <summary>The sum of the figures filled in, or null if none is.</summary>
    private static decimal? Sum(IEnumerable<decimal?> figures)
    {
        var filledIn = figures.OfType<decimal>().ToList();
        return filledIn.Count == 0 ? null : filledIn.Sum();
    }
}

/// <summary>How far along a Trip is against its Expected Distance, counting only the actual distance hiked. In metres.</summary>
public sealed record TripProgress(decimal ActualMetres, decimal ExpectedMetres)
{
    /// <summary>What's left to hike, never below zero.</summary>
    public decimal RemainingMetres => Math.Max(ExpectedMetres - ActualMetres, 0m);

    /// <summary>The share hiked, as a whole percentage; over 100 when the Trip went further than expected.</summary>
    public int Percent => (int)Math.Round(ActualMetres / ExpectedMetres * 100m, 0, MidpointRounding.AwayFromZero);
}

/// <summary>A Trip's Resupplies, and its longest Food Carry: "3 resupplies · longest Food Carry 6 days (8.1 lb)".</summary>
public sealed record LongestFoodCarry(int Resupplies, int Days, decimal FoodGrams);

/// <summary>The calories ticked as eaten and those planned, over the days counted.</summary>
public sealed record FoodEaten(decimal EatenCalories, decimal PlannedCalories, int Days)
{
    /// <summary>The share of the planned calories eaten, as a whole percentage; null when nothing was planned.</summary>
    public int? Percent => PlannedCalories == 0 ? null : (int)Math.Round(EatenCalories / PlannedCalories * 100m, 0, MidpointRounding.AwayFromZero);

    /// <summary>The calories eaten per day counted.</summary>
    public decimal CaloriesPerDay => Days == 0 ? 0m : EatenCalories / Days;
}

/// <summary>When an Open-ended Trip would finish at the pace so far, and how many days after its last day that is.</summary>
public sealed record FinishEstimate(DateOnly Date, int DaysToGo);
