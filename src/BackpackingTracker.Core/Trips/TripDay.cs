namespace BackpackingTracker.Core.Trips;

/// <summary>
/// One calendar day of a Trip: its route and journal notes, all optional. Distances and elevation gain are
/// stored in metres (ADR 0002). Kept inside the Trip record, in date order, one per day of the Trip.
/// </summary>
public sealed record TripDay
{
    public required DateOnly Date { get; init; }

    /// <summary>Where the day starts, such as a trailhead.</summary>
    public string? Start { get; init; }

    /// <summary>Where the day ends.</summary>
    public string? End { get; init; }

    public string? Campsite { get; init; }

    public decimal? PlannedDistanceMetres { get; init; }

    public decimal? ActualDistanceMetres { get; init; }

    public decimal? ElevationGainMetres { get; init; }

    public string? JournalNotes { get; init; }

    /// <summary>True when anything has been filled in, so removing the day would lose it.</summary>
    public bool HasContent() =>
        Start is not null || End is not null || Campsite is not null ||
        PlannedDistanceMetres is not null || ActualDistanceMetres is not null || ElevationGainMetres is not null ||
        JournalNotes is not null;
}

/// <summary>What can be typed in on a Trip Day. Blank text and missing numbers leave a field empty.</summary>
public sealed record TripDayDetails(
    string? Start = null,
    string? End = null,
    string? Campsite = null,
    decimal? PlannedDistanceMetres = null,
    decimal? ActualDistanceMetres = null,
    decimal? ElevationGainMetres = null,
    string? JournalNotes = null);
