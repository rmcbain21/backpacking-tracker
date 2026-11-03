using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Trip with its Packing List entries' current weights, its Meal Plan, and its totals (Meal Plan food and water
/// carried included). Sections lists every Category on the Packing List, in order, even empty ones, plus
/// "No Category" last if any entry's Category is gone.
/// </summary>
/// <param name="Today">The date it was looked at, for whether the Trip has started, is On Trail or is overdue.</param>
public sealed record TripView(
    Trip Trip,
    IReadOnlyList<ListEntryView> Entries,
    IReadOnlyList<CategorySection> Sections,
    PackWeight Weight,
    MealPlanView MealPlan,
    DateOnly Today)
{
    /// <summary>Today is on or after the start date, so its days can be marked Skipped.</summary>
    public bool HasStarted => Today >= Trip.StartDate;

    /// <summary>
    /// Planned, and today falls within its dates. An Open-ended Trip is On Trail from its start date until it's
    /// Completed, since its end date is only its last day so far. Worked out from the dates, not a Trip Status.
    /// </summary>
    public bool OnTrail => Trip.Status == TripStatus.Planned && HasStarted && (Trip.OpenEnded || Today <= Trip.EndDate);

    /// <summary>Still Planned, though its end date has passed: most likely finished but not yet marked Completed.</summary>
    public bool Overdue => Trip.Status == TripStatus.Planned && !Trip.OpenEnded && Trip.EndDate < Today;

    /// <summary>Which day of the Trip today is, counting the start date as day 1, whether or not that Trip Day exists yet.</summary>
    public int TodayNumber => Today.DayNumber - Trip.StartDate.DayNumber + 1;

    /// <summary>Today's Trip Day, or null when today isn't one (such as an Open-ended Trip's day not yet added).</summary>
    public TripDay? TodayDay => Trip.Days.FirstOrDefault(day => day.Date == Today);

    /// <summary>Days from today until the start date: 1 when it starts tomorrow.</summary>
    public int DaysToStart => Trip.StartDate.DayNumber - Today.DayNumber;

    /// <summary>How many Packing List entries are ticked on the packing checklist.</summary>
    public int PackedCount => Trip.PackingList.Entries.Count(entry => entry.Packed);

    /// <summary>How many entries the packing checklist has: one per Packing List entry, whatever its quantity.</summary>
    public int ChecklistCount => Trip.PackingList.Entries.Count;

    /// <summary>Days and nights, route totals, pack weights and food, to review and compare Trips.</summary>
    public TripStats Stats => TripStats.For(Trip, Weight, MealPlan);
}

/// <summary>
/// Every Trip: On Trail ones earliest started first, then the other Planned ones (upcoming, and overdue ones that
/// ended without being Completed) soonest first, then Completed ones (past) most recent first.
/// </summary>
public sealed record TripListing(IReadOnlyList<TripView> OnTrail, IReadOnlyList<TripView> Upcoming, IReadOnlyList<TripView> Past)
{
    public bool IsEmpty => OnTrail.Count == 0 && Upcoming.Count == 0 && Past.Count == 0;

    /// <summary>
    /// The Trip for the card at the top of the Trips page: an overdue Trip first (the one that ended earliest), so it
    /// gets Completed; otherwise the On Trail Trip that started earliest; otherwise the next one to start; otherwise none.
    /// </summary>
    public TripView? Featured =>
        Upcoming.Where(view => view.Overdue).OrderBy(view => view.Trip.EndDate).FirstOrDefault()
        ?? OnTrail.FirstOrDefault()
        ?? Upcoming.FirstOrDefault(view => view.DaysToStart > 0);
}
