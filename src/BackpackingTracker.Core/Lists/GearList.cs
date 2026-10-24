namespace BackpackingTracker.Core.Lists;

/// <summary>A reusable, named set of List Entries; the starting point for a Trip's Packing List.</summary>
public sealed record GearList : StoredRecord
{
    public required string Name { get; init; }

    /// <summary>
    /// The list's own Categories, in order: a copy of the Default Categories when the list was made, then
    /// edited on this list only (ADR 0003). Kept inside the Gear List record, so they sync as part of it.
    /// </summary>
    public IReadOnlyList<Category> Categories { get; init; } = [];

    /// <summary>Kept inside the Gear List record, so they sync as part of it.</summary>
    public IReadOnlyList<ListEntry> Entries { get; init; } = [];

    /// <summary>The Categories and entries together, for the rules Gear Lists share with Packing Lists.</summary>
    internal ListContents Contents => new() { Categories = Categories, Entries = Entries };

    internal GearList With(ListContents contents) => this with { Categories = contents.Categories, Entries = contents.Entries };
}
