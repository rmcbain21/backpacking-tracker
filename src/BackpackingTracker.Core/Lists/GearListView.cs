using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Lists;

/// <summary>
/// A List Entry with its Gear Item looked up (null if it was deleted on another device) and the
/// entry's weight: the Gear Item's current weight times the quantity.
/// </summary>
public sealed record ListEntryView(ListEntry Entry, GearItem? GearItem, decimal EntryWeightGrams);

/// <summary>A Gear List with its entries' current weights and its totals.</summary>
public sealed record GearListView(GearList GearList, IReadOnlyList<ListEntryView> Entries, PackWeight Weight);
