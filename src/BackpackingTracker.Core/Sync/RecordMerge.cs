namespace BackpackingTracker.Core.Sync;

/// <summary>What taking an incoming record does to the device, as the backpacker sees it.</summary>
public enum MergeChange
{
    /// <summary>A record the backpacker couldn't see before (new, or brought back after a deletion).</summary>
    Added,

    Updated,

    Deleted,

    /// <summary>A tombstone the device didn't have, or a newer tombstone over one it did: kept so the deletion syncs, but nothing visible changes.</summary>
    Unseen,
}

/// <summary>An incoming record that wins over the device's copy, and what taking it does.</summary>
public sealed record MergedRecord(StoredRecord Record, MergeChange Change);

/// <summary>
/// Merges incoming records (from an imported file, or the sync file) into the device's records, record by
/// record: a record only on one side is taken from that side, and when both sides have it the one with the
/// later <see cref="StoredRecord.UpdatedAt"/> wins. Tombstones are records like any other, so a deletion wins
/// over an older edit and loses to a newer one. On a tie the device's copy stays, so merging twice changes nothing.
/// </summary>
public static class RecordMerge
{
    /// <summary>The incoming records to save on the device; records where the device's copy wins are left out.</summary>
    public static IReadOnlyList<MergedRecord> IncomingWinners(IEnumerable<StoredRecord> local, IEnumerable<StoredRecord> incoming)
    {
        var localById = local.ToDictionary(record => record.Id);
        var winners = new List<MergedRecord>();
        foreach (var record in incoming)
        {
            var existing = localById.GetValueOrDefault(record.Id);
            if (existing is not null && record.UpdatedAt <= existing.UpdatedAt)
            {
                continue;
            }

            var wasVisible = existing is { Deleted: false };
            var change = (wasVisible, record.Deleted) switch
            {
                (false, false) => MergeChange.Added,
                (true, false) => MergeChange.Updated,
                (true, true) => MergeChange.Deleted,
                (false, true) => MergeChange.Unseen,
            };
            winners.Add(new MergedRecord(record, change));
        }

        return winners;
    }
}
