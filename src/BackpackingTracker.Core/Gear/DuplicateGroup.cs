namespace BackpackingTracker.Core.Gear;

/// <summary>
/// Gear Items with the same Gear Type and Name, offered for a Merge: most often the same sheet imported on two
/// devices before syncing. Pass <see cref="GearItemIds"/> back to merge them or mark them as not duplicates.
/// </summary>
/// <param name="Items">The Gear Items, the suggested one to keep first.</param>
/// <param name="SuggestedKeepId">The one used by the most Gear Lists and Trips (not Retired, on a tie).</param>
/// <param name="BlockedReason">Why it can't be merged yet, or null if it can.</param>
public sealed record DuplicateGroup(IReadOnlyList<DuplicateCandidate> Items, string SuggestedKeepId, string? BlockedReason)
{
    public IReadOnlyList<string> GearItemIds => Items.Select(candidate => candidate.GearItem.Id).ToList();

    public bool CanMerge => BlockedReason is null;
}

/// <summary>One Gear Item in a <see cref="DuplicateGroup"/>, and what uses it, such as Gear List "Summer kit".</summary>
public sealed record DuplicateCandidate(GearItem GearItem, IReadOnlyList<string> UsedBy);

/// <summary>What a Merge will do, for the backpacker to confirm.</summary>
/// <param name="GearListCount">Gear Lists that will use the kept Gear Item instead of one merged away.</param>
/// <param name="TripCount">Trips whose Packing Lists will, likewise.</param>
public sealed record GearMergePlan(GearItem Kept, int MergedCount, int GearListCount, int TripCount)
{
    /// <summary>Such as: "Keep the chosen Duplex? The other one will be deleted, and 1 Gear List and 2 Trips will use the kept one instead."</summary>
    public string Message
    {
        get
        {
            var others = MergedCount == 1 ? "The other one" : $"The other {MergedCount}";
            var uses = new[] { Count(GearListCount, "Gear List"), Count(TripCount, "Trip") }.OfType<string>().ToList();
            var after = uses.Count == 0 ? "." : $", and {string.Join(" and ", uses)} will use the kept one instead.";
            return $"Keep the chosen {Kept.Name}? {others} will be deleted{after}";
        }
    }

    private static string? Count(int count, string what) => count switch
    {
        0 => null,
        1 => $"1 {what}",
        _ => $"{count} {what}s",
    };
}
