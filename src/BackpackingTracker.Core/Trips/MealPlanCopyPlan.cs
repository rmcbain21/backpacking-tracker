namespace BackpackingTracker.Core.Trips;

/// <summary>
/// What copying another Trip's Meal Plan would do. Days are matched by position (the source's Day 1 to this Trip's
/// Day 1) up to the shorter Trip's length; each copied day's Meal Plan replaces this Trip's for that day, and later days
/// are left alone. Retired Meals and Food Items aren't copied. Replacing any day that already has food planned needs
/// confirmation.
/// </summary>
/// <param name="SourceName">The Trip copied from.</param>
/// <param name="DayCount">How many days are copied.</param>
/// <param name="ReplacedDays">The Day numbers within those that already have food planned here, which would be replaced.</param>
/// <param name="Skipped">The names of Retired Meals and Food Items left out, each once.</param>
public sealed record MealPlanCopyPlan(string SourceName, int DayCount, IReadOnlyList<int> ReplacedDays, IReadOnlyList<string> Skipped)
{
    public bool NeedsConfirmation => ReplacedDays.Count > 0;

    /// <summary>The days that would be replaced, as "Day 2" or "Days 1 and 2" or "Days 1, 2 and 3"; empty if none.</summary>
    public string ReplacedDaysText => ReplacedDays.Count switch
    {
        0 => "",
        1 => $"Day {ReplacedDays[0]}",
        _ => $"Days {string.Join(", ", ReplacedDays.Take(ReplacedDays.Count - 1))} and {ReplacedDays[^1]}",
    };
}
