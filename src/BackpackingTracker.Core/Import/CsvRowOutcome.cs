namespace BackpackingTracker.Core.Import;

public enum CsvRowOutcome
{
    /// <summary>A new Gear Item was created.</summary>
    Created,

    /// <summary>An existing Gear Item matched and was used as it is.</summary>
    Reused,

    /// <summary>An existing Gear Item matched, but the CSV gives a different weight. Nothing was changed.</summary>
    Flagged,

    /// <summary>The row couldn't be imported; Reason says why.</summary>
    Rejected,
}
