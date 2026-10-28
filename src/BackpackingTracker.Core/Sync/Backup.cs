using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Sync;

/// <summary>
/// Exports the whole database as a JSON file, and imports such a file back combined with the device's data,
/// so the backpacker has a backup that doesn't depend on Google.
/// </summary>
public sealed class Backup
{
    private readonly IRecordStore _store;

    public Backup(IRecordStore store)
    {
        _store = store;
    }

    /// <summary>Every record on the device, tombstones included, as a <see cref="SyncFile"/>.</summary>
    public async Task<string> ExportAsync()
    {
        var records = new Dictionary<RecordTable, IReadOnlyList<StoredRecord>>();
        foreach (var table in RecordTable.All)
        {
            records[table] = await table.ListAsync(_store);
        }

        return SyncFile.Write(records);
    }

    /// <summary>
    /// Combines an exported file into the device's records (see <see cref="RecordCombine.IncomingWinners"/>) and
    /// says what changed. Refuses a file that isn't an export or is in an unknown format; then, or if saving
    /// fails, nothing changes.
    /// </summary>
    public async Task<ChangeSummary> ImportAsync(string fileText)
    {
        var incoming = Read(fileText);

        var toSave = new List<RecordToSave>();
        var changes = new List<RecordTypeChanges>();
        foreach (var table in RecordTable.All)
        {
            var winners = RecordCombine.IncomingWinners(await table.ListAsync(_store), incoming[table]);
            toSave.AddRange(winners.Select(winner => new RecordToSave(table.Name, winner.Record)));
            changes.AddRange(RecordTypeChanges.Count(table, winners));
        }

        if (toSave.Count > 0)
        {
            await _store.SaveAllAsync(toSave);
        }

        return new ChangeSummary(changes);
    }

    private static IReadOnlyDictionary<RecordTable, IReadOnlyList<StoredRecord>> Read(string fileText)
    {
        try
        {
            return SyncFile.Read(fileText);
        }
        catch (UnreadableSyncFileException unreadable) when (unreadable.UnknownVersion is { } version)
        {
            throw new RuleViolationException(
                $"This export is in format version {version}, which this version of the app can't read, so nothing was imported.");
        }
        catch (UnreadableSyncFileException)
        {
            throw new RuleViolationException("This file isn't a Backpacking Tracker export, so nothing was imported.");
        }
    }
}

/// <summary>What an import or a sync changed on this device, per record type; only the types with changes are listed.</summary>
/// <param name="ConflictsChanged">A sync found new Sync Conflicts, or ones waiting went away by themselves.</param>
public sealed record ChangeSummary(IReadOnlyList<RecordTypeChanges> Changes, bool ConflictsChanged = false)
{
    public bool NothingChanged => Changes.Count == 0 && !ConflictsChanged;
}

/// <summary>How many records of one type, such as "Gear Items", were added, updated and deleted on this device.</summary>
public sealed record RecordTypeChanges(string RecordType, int Added, int Updated, int Deleted)
{
    /// <summary>The counts for one table's combined records; none when nothing visible changed.</summary>
    internal static IEnumerable<RecordTypeChanges> Count(RecordTable table, IReadOnlyList<CombinedRecord> combined)
    {
        var counts = new RecordTypeChanges(
            table.RecordTypeName,
            Added: combined.Count(record => record.Change == CombineChange.Added),
            Updated: combined.Count(record => record.Change == CombineChange.Updated),
            Deleted: combined.Count(record => record.Change == CombineChange.Deleted));
        return counts.Added + counts.Updated + counts.Deleted > 0 ? [counts] : [];
    }
}
