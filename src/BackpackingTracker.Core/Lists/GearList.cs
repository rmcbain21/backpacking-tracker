namespace BackpackingTracker.Core.Lists;

/// <summary>A reusable, named set of List Entries; the starting point for a Trip's Packing List.</summary>
public sealed record GearList : StoredRecord
{
    public required string Name { get; init; }

    /// <summary>Kept inside the Gear List record, so they sync as part of it.</summary>
    public IReadOnlyList<ListEntry> Entries { get; init; } = [];
}
