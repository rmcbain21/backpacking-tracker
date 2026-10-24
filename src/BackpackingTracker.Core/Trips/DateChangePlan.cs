namespace BackpackingTracker.Core.Trips;

/// <summary>A Trip Day a date change would remove, by its Day number (1 is the first) and its date.</summary>
public sealed record RemovedTripDay(int DayNumber, TripDay Day)
{
    public bool HasContent => Day.HasContent();
}

/// <summary>
/// What changing a Trip's start or end date would do. Each Trip Day stays on its date: days outside the
/// new dates are removed (from the start when the Trip starts later, from the end when it ends sooner),
/// and new dates get empty days. Removing any day with content needs confirmation.
/// </summary>
public sealed record DateChangePlan(
    DateOnly StartDate, DateOnly EndDate, IReadOnlyList<DateOnly> AddedDates, IReadOnlyList<RemovedTripDay> RemovedDays)
{
    public bool NeedsConfirmation => RemovedDays.Any(day => day.HasContent);

    /// <summary>The removed days that have content, as "Day 4" or "Days 3 and 4" or "Days 2, 3 and 4".</summary>
    internal string DaysWithContent()
    {
        var numbers = RemovedDays.Where(day => day.HasContent).Select(day => day.DayNumber.ToString()).ToList();
        return numbers.Count == 1
            ? $"Day {numbers[0]}"
            : $"Days {string.Join(", ", numbers.Take(numbers.Count - 1))} and {numbers[^1]}";
    }
}
