namespace BackpackingTracker.Core.Gear;

/// <summary>
/// One of the Default Categories: the backpacker's usual set, copied onto each new Gear List as its
/// starting Categories (ADR 0003). Changing it later doesn't change existing lists.
/// </summary>
public sealed record DefaultCategory : StoredRecord
{
    public required string Name { get; init; }

    /// <summary>Position in the backpacker's chosen order, starting at 0.</summary>
    public required int SortOrder { get; init; }
}
