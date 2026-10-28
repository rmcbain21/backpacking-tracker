namespace BackpackingTracker.Core.Gear;

/// <summary>
/// A set of matching Gear Items the backpacker said aren't duplicates, so they aren't offered for a Merge again.
/// Synced, so other devices don't ask either. Its id comes from the Gear Item ids, so two devices dismissing the
/// same set make the same record.
/// </summary>
public sealed record NotDuplicates : StoredRecord
{
    /// <summary>The Gear Items' ids, in order. A group with any other Gear Item in it is offered again.</summary>
    public IReadOnlyList<string> GearItemIds { get; init; } = [];

    internal static string IdFor(IEnumerable<string> gearItemIds) => string.Join("+", gearItemIds.Order(StringComparer.Ordinal));
}
