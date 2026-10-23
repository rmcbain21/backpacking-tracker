using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Lists;

/// <summary>One line on a Gear List: a Gear Item, how many of it, and how it's carried on this list.</summary>
public sealed record ListEntry
{
    /// <summary>A GUID string, so one entry can be changed or removed even if the Gear Item is on the list twice.</summary>
    public required string Id { get; init; }

    public required string GearItemId { get; init; }

    public int Quantity { get; init; } = 1;

    public CarryMode CarryMode { get; init; } = CarryMode.Base;
}
