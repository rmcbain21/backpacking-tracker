using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Trip with its Packing List entries' current weights and its totals (water carried included). Sections
/// lists every Category on the Packing List, in order, even empty ones, plus "No Category" last if any
/// entry's Category is gone.
/// </summary>
public sealed record TripView(
    Trip Trip, IReadOnlyList<ListEntryView> Entries, IReadOnlyList<CategorySection> Sections, PackWeight Weight)
{
    /// <summary>How many Packing List entries are ticked on the packing checklist.</summary>
    public int PackedCount => Trip.PackingList.Entries.Count(entry => entry.Packed);

    /// <summary>How many entries the packing checklist has: one per Packing List entry, whatever its quantity.</summary>
    public int ChecklistCount => Trip.PackingList.Entries.Count;

    /// <summary>Days and nights, route totals and pack weights, to review and compare Trips.</summary>
    public TripStats Stats => TripStats.For(Trip, Weight);
}

/// <summary>
/// Every Trip: upcoming (Planned) ones soonest first, and past (Completed) ones most recent first.
/// </summary>
public sealed record TripListing(IReadOnlyList<TripView> Upcoming, IReadOnlyList<TripView> Past);
