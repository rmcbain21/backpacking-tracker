namespace BackpackingTracker.Core.Import;

public enum CsvRowOutcome
{
    /// <summary>A new Gear Item will be created.</summary>
    Created,

    /// <summary>An existing Gear Item matches and will be used as it is.</summary>
    Reused,

    /// <summary>An existing Gear Item matches, but the CSV gives a different weight. It won't be changed.</summary>
    Flagged,

    /// <summary>The row can't be imported; Reason says why.</summary>
    Rejected,
}
