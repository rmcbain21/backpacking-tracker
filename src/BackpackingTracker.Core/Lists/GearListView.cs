using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Lists;

/// <summary>
/// A List Entry with its Gear Item looked up (null if it was deleted on another device) and the
/// entry's weight: the Gear Item's current weight times the quantity.
/// </summary>
public sealed record ListEntryView(ListEntry Entry, GearItem? GearItem, decimal EntryWeightGrams);

/// <summary>
/// One of the list's Categories, with its Position on the list (-1 for "No Category"), its entries sorted by
/// Name, and their subtotal (worn entries included: it's everything listed under the heading).
/// </summary>
public sealed record CategorySection(Category Category, int Position, IReadOnlyList<ListEntryView> Entries, decimal SubtotalGrams)
{
    /// <summary>False for the "No Category" section, which isn't one of the list's Categories.</summary>
    public bool IsOnList => Position >= 0;
}

/// <summary>
/// A Gear List with its entries' current weights and its totals. Sections lists every Category on
/// the list, in order, even empty ones, plus "No Category" last if any entry's Category is gone.
/// </summary>
public sealed record GearListView(
    GearList GearList, IReadOnlyList<ListEntryView> Entries, IReadOnlyList<CategorySection> Sections, PackWeight Weight);
