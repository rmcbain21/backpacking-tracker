using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Lists;

/// <summary>
/// One line on a Gear List: a Gear Item, how many of it, how it's carried on this list, and which of
/// the list's Categories it's under.
/// </summary>
public sealed record ListEntry
{
    /// <summary>A GUID string, so one entry can be changed or removed.</summary>
    public required string Id { get; init; }

    public required string GearItemId { get; init; }

    /// <summary>The id of one of the list's Categories; "" only on lists saved before list Categories.</summary>
    public string CategoryId { get; init; } = "";

    public int Quantity { get; init; } = 1;

    public CarryMode CarryMode { get; init; } = CarryMode.Base;
}
