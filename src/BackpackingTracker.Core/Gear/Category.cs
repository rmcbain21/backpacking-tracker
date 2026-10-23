namespace BackpackingTracker.Core.Gear;

/// <summary>The one group a Gear Item belongs to, used to break down pack weight.</summary>
public sealed record Category : StoredRecord
{
    /// <summary>
    /// Stands in for a Category that no longer exists (removed on another device before a sync),
    /// so gear pointing at it is still shown. Never stored.
    /// </summary>
    public static Category None { get; } =
        new() { Id = "", UpdatedAt = DateTimeOffset.UnixEpoch, Name = "No Category", SortOrder = int.MaxValue };

    public required string Name { get; init; }

    /// <summary>Position in the backpacker's chosen order, starting at 0.</summary>
    public required int SortOrder { get; init; }
}
