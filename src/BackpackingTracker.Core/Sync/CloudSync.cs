using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Sync;

/// <summary>Whether this device is connected to a cloud drive, and when it last synced.</summary>
public sealed record SyncStatus(bool Connected, DateTimeOffset? LastSyncedAt);

/// <summary>
/// Backs up and syncs the device's records through one sync file in the backpacker's cloud drive, so changes
/// made on one device show up on the others (ADR 0001).
/// </summary>
public sealed class CloudSync
{
    // A sync starts again when another device wrote the file while it ran; this many tries, then it gives up.
    private const int MaxAttempts = 3;

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly ICloudFileStore? _cloud;

    public CloudSync(IRecordStore store, IClock clock, ICloudFileStore? cloud)
    {
        _store = store;
        _clock = clock;
        _cloud = cloud;
    }

    public async Task<SyncStatus> GetStatusAsync()
    {
        var state = await LoadStateAsync();
        return new SyncStatus(state.Connected, state.LastSyncedAt);
    }

    /// <summary>Remembers that the backpacker connected their cloud drive on this device (signing in is the app's job).</summary>
    public async Task ConnectAsync() => await SaveStateAsync(await LoadStateAsync() with { Connected = true });

    /// <summary>
    /// Stops syncing this device and forgets its sync bookkeeping. The records on the device are kept, and
    /// connecting again merges them with the file like a first sync.
    /// </summary>
    public Task DisconnectAsync() => SaveStateAsync(NewState());

    /// <summary>
    /// Reads the sync file (none yet counts as empty), merges it with the device's records record by record
    /// (see <see cref="RecordMerge.WithBase"/>), saves the merged records here, writes them to the file, then
    /// records the sync. If writing fails the bookkeeping is left as it was, so the next sync tries again.
    /// Returns what changed on this device.
    /// </summary>
    public async Task<ChangeSummary> SyncNowAsync()
    {
        var state = await LoadStateAsync();
        if (!state.Connected || _cloud is null)
        {
            throw new RuleViolationException("Connect Google Drive before syncing.");
        }

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            var fileModified = await _cloud.GetLastModifiedAsync();
            var remote = ReadSyncFile(await _cloud.ReadAsync());

            var toSave = new List<RecordToSave>();
            var merged = new Dictionary<RecordTable, IReadOnlyList<StoredRecord>>();
            var changes = new List<RecordTypeChanges>();
            var fileChanged = fileModified is null;
            foreach (var table in RecordTable.All)
            {
                var result = RecordMerge.WithBase(
                    await table.ListAsync(_store), remote[table], id => BaseOf(state, table, id));
                toSave.AddRange(result.ToSaveLocally.Select(record => new RecordToSave(table.Name, record.Record)));
                merged[table] = result.Merged;
                fileChanged |= result.FileChanged;
                changes.AddRange(RecordTypeChanges.Count(table, result.ToSaveLocally));
            }

            if (toSave.Count > 0)
            {
                await _store.SaveAllAsync(toSave);
            }

            if (fileChanged)
            {
                // Another device wrote the file since it was read: writing now would lose its changes. The
                // merge so far is saved here, so starting again only has to merge what that device added.
                // (A write landing between this check and ours can still be lost from the file, but the
                // device that made it re-sends its changes on its next sync.)
                if (await _cloud.GetLastModifiedAsync() != fileModified)
                {
                    continue;
                }

                await _cloud.WriteAsync(SyncFile.Write(merged));
            }

            await SaveStateAsync(state with
            {
                LastSyncedAt = _clock.UtcNow,
                Bases = merged
                    .SelectMany(pair => pair.Value.Select(record => (Key: SyncState.BaseKey(pair.Key.Name, record.Id), record.UpdatedAt)))
                    .ToDictionary(pair => pair.Key, pair => pair.UpdatedAt),
            });
            return new ChangeSummary(changes);
        }

        throw new RuleViolationException("Another device kept syncing at the same time. Try again in a moment.");
    }

    private static DateTimeOffset? BaseOf(SyncState state, RecordTable table, string id) =>
        state.Bases.TryGetValue(SyncState.BaseKey(table.Name, id), out var @base) ? @base : null;

    private static IReadOnlyDictionary<RecordTable, IReadOnlyList<StoredRecord>> ReadSyncFile(string? text)
    {
        try
        {
            return SyncFile.ReadOrEmpty(text);
        }
        catch (UnreadableSyncFileException unreadable) when (unreadable.UnknownVersion is { } version)
        {
            throw new RuleViolationException(
                $"The sync file was written by a newer version of the app (format {version}). Update the app on this device, then sync again.");
        }
        catch (UnreadableSyncFileException)
        {
            throw new RuleViolationException("The sync file in Google Drive can't be read, so nothing was synced.");
        }
    }

    private async Task<SyncState> LoadStateAsync() =>
        await _store.LoadAsync<SyncState>(SyncState.Table, SyncState.SingleId) ?? NewState();

    private SyncState NewState() => new() { Id = SyncState.SingleId, UpdatedAt = _clock.UtcNow };

    private Task SaveStateAsync(SyncState state) => _store.SaveAsync(SyncState.Table, state with { UpdatedAt = _clock.UtcNow });
}
