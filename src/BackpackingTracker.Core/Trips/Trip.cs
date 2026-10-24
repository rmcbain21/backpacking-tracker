using BackpackingTracker.Core.Lists;

namespace BackpackingTracker.Core.Trips;

/// <summary>One specific outing, with its own Packing List and Trip Days. Later changes to a Gear List never rewrite it.</summary>
public sealed record Trip : StoredRecord
{
    public required string Name { get; init; }

    public required DateOnly StartDate { get; init; }

    /// <summary>On or after the start date; the same day for a one-day trip.</summary>
    public required DateOnly EndDate { get; init; }

    public TripStatus Status { get; init; } = TripStatus.Planned;

    /// <summary>
    /// The name of the Gear List the Trip was created from, for display only; null for a Trip created empty.
    /// Not a link: the Gear List can be renamed or deleted without changing the Trip.
    /// </summary>
    public string? CreatedFrom { get; init; }

    /// <summary>
    /// The Trip's own Categories and List Entries: a copy of a Gear List's (or the Default Categories) when the
    /// Trip was created, then edited on this Trip only. Kept inside the Trip record, so it syncs as part of it.
    /// </summary>
    public ListContents PackingList { get; init; } = new();

    /// <summary>The water carried from the trailhead, in grams; null when none is recorded. Counted as Consumable Weight.</summary>
    public decimal? WaterCarriedGrams { get; init; }

    /// <summary>One Trip Day per calendar day from the start date to the end date, in order.</summary>
    public IReadOnlyList<TripDay> Days { get; init; } = [];

    /// <summary>The number of calendar days from the start date to the end date, both included.</summary>
    public static int DayCount(DateOnly startDate, DateOnly endDate) => endDate.DayNumber - startDate.DayNumber + 1;

    /// <summary>
    /// One day per date from the start to the end: the Trip's own day for each date it already has, and an
    /// empty day for each date it doesn't. Days outside the dates are left out.
    /// </summary>
    internal IReadOnlyList<TripDay> DaysFor(DateOnly startDate, DateOnly endDate) =>
        Dates(startDate, endDate)
            .Select(date => Days.FirstOrDefault(day => day.Date == date) ?? new TripDay { Date = date })
            .ToList();

    /// <summary>The Trip's days in order, re-dated from a new start: day 1 on the start date, day 2 the next, and so on.</summary>
    internal IReadOnlyList<TripDay> DaysMovedTo(DateOnly startDate) =>
        Days.Select((day, i) => day with { Date = startDate.AddDays(i) }).ToList();

    internal static IEnumerable<DateOnly> Dates(DateOnly startDate, DateOnly endDate) =>
        Enumerable.Range(0, DayCount(startDate, endDate)).Select(startDate.AddDays);
}
