namespace BackpackingTracker.Core.Gear;

/// <summary>The one group a Gear Item belongs to, used to break down pack weight.</summary>
public sealed record Category : StoredRecord
{
    public required string Name { get; init; }

    /// <summary>Position in the backpacker's chosen order, starting at 0.</summary>
    public required int SortOrder { get; init; }
}
