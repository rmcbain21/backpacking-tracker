using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Sync;

/// <summary>
/// Exports the whole database as a JSON file, and imports such a file back merged with the device's data,
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
    /// Merges an exported file into the device's records (see <see cref="RecordMerge"/>) and says what changed.
    /// Refuses a file that isn't an export or is in an unknown format; then, or if saving fails, nothing changes.
    /// </summary>
    public async Task<ImportSummary> ImportAsync(string fileText)
    {
        var incoming = SyncFile.Read(fileText);

        var toSave = new List<RecordToSave>();
        var changes = new List<RecordTypeChanges>();
        foreach (var table in RecordTable.All)
        {
            var winners = RecordMerge.IncomingWinners(await table.ListAsync(_store), incoming[table]);
            toSave.AddRange(winners.Select(winner => new RecordToSave(table.Name, winner.Record)));

            var counts = new RecordTypeChanges(
                table.RecordTypeName,
                Added: winners.Count(winner => winner.Change == MergeChange.Added),
                Updated: winners.Count(winner => winner.Change == MergeChange.Updated),
                Deleted: winners.Count(winner => winner.Change == MergeChange.Deleted));
            if (counts.Added + counts.Updated + counts.Deleted > 0)
            {
                changes.Add(counts);
            }
        }

        if (toSave.Count > 0)
        {
            await _store.SaveAllAsync(toSave);
        }

        return new ImportSummary(changes);
    }
}

/// <summary>What an import changed, per record type; only the types with changes are listed.</summary>
public sealed record ImportSummary(IReadOnlyList<RecordTypeChanges> Changes)
{
    public bool NothingChanged => Changes.Count == 0;
}

/// <summary>How many records of one type, such as "Gear Items", an import added, updated and deleted.</summary>
public sealed record RecordTypeChanges(string RecordType, int Added, int Updated, int Deleted);
