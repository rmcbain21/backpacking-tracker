using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// A Trip with its Packing List entries' current weights and its totals. Sections lists every Category on
/// the Packing List, in order, even empty ones, plus "No Category" last if any entry's Category is gone.
/// </summary>
public sealed record TripView(
    Trip Trip, IReadOnlyList<ListEntryView> Entries, IReadOnlyList<CategorySection> Sections, PackWeight Weight);

/// <summary>
/// Every Trip: upcoming (Planned) ones soonest first, and past (Completed) ones most recent first.
/// </summary>
public sealed record TripListing(IReadOnlyList<TripView> Upcoming, IReadOnlyList<TripView> Past);
