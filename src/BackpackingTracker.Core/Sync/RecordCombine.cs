namespace BackpackingTracker.Core.Sync;

/// <summary>What taking an incoming record does to the device, as the backpacker sees it.</summary>
internal enum CombineChange
{
    /// <summary>A record the backpacker couldn't see before (new, or brought back after a deletion).</summary>
    Added,

    Updated,

    Deleted,

    /// <summary>A tombstone the device didn't have, or a newer tombstone over one it did: kept so the deletion syncs, but nothing visible changes.</summary>
    Unseen,
}

/// <summary>An incoming record that wins over the device's copy, and what taking it does.</summary>
internal sealed record CombinedRecord(StoredRecord Record, CombineChange Change);

/// <summary>A record both devices changed differently since they last synced: a Sync Conflict.</summary>
internal sealed record ConflictedRecord(StoredRecord Local, StoredRecord Remote);

/// <summary>The outcome of combining one table with the sync file.</summary>
/// <param name="ToSaveLocally">The file's records that win over (or are missing from) the device.</param>
/// <param name="Combined">Every record as the file should hold it: for a Sync Conflict, the file's own version.</param>
/// <param name="FileChanged">Whether <paramref name="Combined"/> differs from what the file holds.</param>
/// <param name="Conflicts">Records left as they are on both sides until the backpacker chooses a version.</param>
internal sealed record SyncCombineResult(
    IReadOnlyList<CombinedRecord> ToSaveLocally,
    IReadOnlyList<StoredRecord> Combined,
    bool FileChanged,
    IReadOnlyList<ConflictedRecord> Conflicts);

/// <summary>
/// Combines records from elsewhere (an imported file, or the sync file) into the device's records, record by
/// record. A record only on one side is taken from that side. Tombstones are records like any other, so a
/// deletion is combined the same way as an edit.
/// </summary>
internal static class RecordCombine
{
    /// <summary>
    /// For an import, which has no record of earlier syncs: when both sides have a record, the one with the
    /// later <see cref="StoredRecord.UpdatedAt"/> wins. On a tie the device's copy stays, so importing twice
    /// changes nothing. Returns the incoming records to save; records where the device's copy wins are left out.
    /// </summary>
    public static IReadOnlyList<CombinedRecord> IncomingWinners(IEnumerable<StoredRecord> local, IEnumerable<StoredRecord> incoming)
    {
        var localById = local.ToDictionary(record => record.Id);
        return incoming
            .Select(record => (Record: record, Existing: localById.GetValueOrDefault(record.Id)))
            .Where(pair => pair.Existing is null || pair.Record.UpdatedAt > pair.Existing.UpdatedAt)
            .Select(pair => new CombinedRecord(pair.Record, Classify(pair.Existing, pair.Record)))
            .ToList();
    }

    /// <summary>
    /// For sync, comparing each side with its base (its UpdatedAt at the last sync, by record id). When only
    /// one side has changed since then, that side wins, whatever the clocks say. When both have changed, it's
    /// a Sync Conflict, unless they hold the same contents (or are both deleted), when the later one is taken.
    /// With no base (this device's first sync of the record) a side counts as changed unless it was never
    /// edited (UpdatedAt is the Unix epoch, as for the first-run Default Categories).
    /// </summary>
    public static SyncCombineResult WithBase(
        RecordTable table, IEnumerable<StoredRecord> local, IEnumerable<StoredRecord> remote, Func<string, DateTimeOffset?> baseOf)
    {
        var localById = local.ToDictionary(record => record.Id);
        var remoteById = remote.ToDictionary(record => record.Id);
        var toSave = new List<CombinedRecord>();
        var combined = new List<StoredRecord>();
        var conflicts = new List<ConflictedRecord>();
        var fileChanged = false;

        foreach (var id in localById.Keys.Union(remoteById.Keys))
        {
            var mine = localById.GetValueOrDefault(id);
            var theirs = remoteById.GetValueOrDefault(id);
            var outcome = mine is null ? Outcome.Theirs
                : theirs is null ? Outcome.Mine
                : Decide(table, mine, theirs, baseOf(id));

            switch (outcome)
            {
                case Outcome.Theirs:
                    toSave.Add(new CombinedRecord(theirs!, Classify(mine, theirs!)));
                    combined.Add(theirs!);
                    break;
                case Outcome.Mine:
                    fileChanged |= theirs is null || theirs.UpdatedAt != mine!.UpdatedAt;
                    combined.Add(mine!);
                    break;
                case Outcome.Conflict:
                    conflicts.Add(new ConflictedRecord(mine!, theirs!));
                    combined.Add(theirs!);
                    break;
            }
        }

        return new SyncCombineResult(toSave, combined, fileChanged, conflicts);
    }

    private enum Outcome
    {
        Mine,
        Theirs,
        Conflict,
    }

    private static Outcome Decide(RecordTable table, StoredRecord mine, StoredRecord theirs, DateTimeOffset? @base)
    {
        if (theirs.UpdatedAt == mine.UpdatedAt)
        {
            return Outcome.Mine;
        }

        var mineChanged = ChangedSince(mine, @base);
        var theirsChanged = ChangedSince(theirs, @base);
        if (mineChanged != theirsChanged)
        {
            return theirsChanged ? Outcome.Theirs : Outcome.Mine;
        }

        // Both changed (neither can be unchanged, as their times differ): a conflict unless the changes agree.
        var agree = (mine.Deleted && theirs.Deleted) || table.SameContents(mine, theirs);
        return !agree ? Outcome.Conflict
            : theirs.UpdatedAt > mine.UpdatedAt ? Outcome.Theirs
            : Outcome.Mine;
    }

    private static bool ChangedSince(StoredRecord record, DateTimeOffset? @base) =>
        @base is { } known ? record.UpdatedAt != known : record.UpdatedAt != DateTimeOffset.UnixEpoch;

    private static CombineChange Classify(StoredRecord? existing, StoredRecord incoming) =>
        (existing is { Deleted: false }, incoming.Deleted) switch
        {
            (false, false) => CombineChange.Added,
            (true, false) => CombineChange.Updated,
            (true, true) => CombineChange.Deleted,
            (false, true) => CombineChange.Unseen,
        };
}
