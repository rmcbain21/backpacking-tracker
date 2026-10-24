using BackpackingTracker.Core.Lists;

namespace BackpackingTracker.Core.Trips;

/// <summary>One specific outing, with its own Packing List. Later changes to a Gear List never rewrite it.</summary>
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
}
