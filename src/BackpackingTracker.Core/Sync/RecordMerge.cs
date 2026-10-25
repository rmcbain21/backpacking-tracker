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

/// <summary>The outcome of merging one table with the sync file.</summary>
/// <param name="ToSaveLocally">The file's records that win over (or are missing from) the device.</param>
/// <param name="Merged">Every record after the merge: what the device and the file should both hold.</param>
/// <param name="FileChanged">Whether <paramref name="Merged"/> differs from what the file holds.</param>
public sealed record SyncMergeResult(IReadOnlyList<MergedRecord> ToSaveLocally, IReadOnlyList<StoredRecord> Merged, bool FileChanged);

/// <summary>
/// Merges records from elsewhere (an imported file, or the sync file) into the device's records, record by
/// record. A record only on one side is taken from that side. Tombstones are records like any other, so a
/// deletion is merged the same way as an edit.
/// </summary>
public static class RecordMerge
{
    /// <summary>
    /// For an import, which has no record of earlier syncs: when both sides have a record, the one with the
    /// later <see cref="StoredRecord.UpdatedAt"/> wins. On a tie the device's copy stays, so importing twice
    /// changes nothing. Returns the incoming records to save; records where the device's copy wins are left out.
    /// </summary>
    public static IReadOnlyList<MergedRecord> IncomingWinners(IEnumerable<StoredRecord> local, IEnumerable<StoredRecord> incoming)
    {
        var localById = local.ToDictionary(record => record.Id);
        return incoming
            .Select(record => (Record: record, Existing: localById.GetValueOrDefault(record.Id)))
            .Where(pair => pair.Existing is null || pair.Record.UpdatedAt > pair.Existing.UpdatedAt)
            .Select(pair => new MergedRecord(pair.Record, Classify(pair.Existing, pair.Record)))
            .ToList();
    }

    /// <summary>
    /// For sync, comparing each side with its base (its UpdatedAt at the last sync, by record id): when only
    /// one side has changed since then, that side wins, whatever the clocks say. When both have changed, or
    /// there is no base yet, the later edit wins, and on a tie the device's copy stays.
    /// </summary>
    public static SyncMergeResult WithBase(
        IEnumerable<StoredRecord> local, IEnumerable<StoredRecord> remote, Func<string, DateTimeOffset?> baseOf)
    {
        var localById = local.ToDictionary(record => record.Id);
        var remoteById = remote.ToDictionary(record => record.Id);
        var toSave = new List<MergedRecord>();
        var merged = new List<StoredRecord>();
        var fileChanged = false;

        foreach (var id in localById.Keys.Union(remoteById.Keys))
        {
            var mine = localById.GetValueOrDefault(id);
            var theirs = remoteById.GetValueOrDefault(id);
            var takeTheirs = mine is null || (theirs is not null && RemoteWins(mine, theirs, baseOf(id)));
            if (takeTheirs)
            {
                toSave.Add(new MergedRecord(theirs!, Classify(mine, theirs!)));
                merged.Add(theirs!);
            }
            else
            {
                fileChanged |= theirs is null || theirs.UpdatedAt != mine!.UpdatedAt;
                merged.Add(mine!);
            }
        }

        return new SyncMergeResult(toSave, merged, fileChanged);
    }

    private static bool RemoteWins(StoredRecord mine, StoredRecord theirs, DateTimeOffset? @base)
    {
        if (theirs.UpdatedAt == mine.UpdatedAt)
        {
            return false;
        }

        var mineChanged = @base is null || mine.UpdatedAt != @base;
        var theirsChanged = @base is null || theirs.UpdatedAt != @base;
        if (mineChanged != theirsChanged)
        {
            return theirsChanged;
        }

        // Both changed: until Sync Conflicts (ticket 25) let the backpacker choose, the later edit wins.
        return theirs.UpdatedAt > mine.UpdatedAt;
    }

    private static MergeChange Classify(StoredRecord? existing, StoredRecord incoming) =>
        (existing is { Deleted: false }, incoming.Deleted) switch
        {
            (false, false) => MergeChange.Added,
            (true, false) => MergeChange.Updated,
            (true, true) => MergeChange.Deleted,
            (false, true) => MergeChange.Unseen,
        };
}
