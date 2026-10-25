using System.Text.Json.Nodes;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Preferences;

namespace BackpackingTracker.Core.Sync;

/// <summary>Whether this device is connected to a cloud drive, when it last synced, and how many Sync Conflicts are waiting.</summary>
public sealed record SyncStatus(bool Connected, DateTimeOffset? LastSyncedAt, int PendingConflicts);

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
    private readonly UserSettings _settings;
    private readonly Action _settled;

    /// <param name="settled">Called after a Sync Conflict is settled, a local change the next sync must send.</param>
    public CloudSync(IRecordStore store, IClock clock, ICloudFileStore? cloud, UserSettings settings, Action settled)
    {
        _store = store;
        _clock = clock;
        _cloud = cloud;
        _settings = settings;
        _settled = settled;
    }

    public async Task<SyncStatus> GetStatusAsync()
    {
        var state = await LoadStateAsync();
        return new SyncStatus(state.Connected, state.LastSyncedAt, state.Conflicts.Count);
    }

    /// <summary>Remembers that the backpacker connected their cloud drive on this device (signing in is the app's job).</summary>
    public async Task ConnectAsync() => await SaveStateAsync(await LoadStateAsync() with { Connected = true });

    /// <summary>
    /// Stops syncing this device. The records and the sync history are kept, so connecting again carries on
    /// where the last sync left off.
    /// </summary>
    public async Task DisconnectAsync() => await SaveStateAsync(await LoadStateAsync() with { Connected = false });

    /// <summary>
    /// Reads the sync file (none yet counts as empty), merges it with the device's records record by record
    /// (see <see cref="RecordMerge.WithBase"/>), saves the merged records here, writes them to the file, then
    /// records the sync. If writing fails the bookkeeping is left as it was, so the next sync tries again.
    /// A record both devices changed differently is left as it is on both sides and becomes a Sync Conflict.
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
            var bases = new Dictionary<string, DateTimeOffset>();
            var conflicts = new List<PendingConflict>();
            var fileChanged = fileModified is null;
            foreach (var table in RecordTable.All)
            {
                var result = RecordMerge.WithBase(
                    table, await table.ListAsync(_store), remote[table], id => BaseOf(state, table, id));
                toSave.AddRange(result.ToSaveLocally.Select(record => new RecordToSave(table.Name, record.Record)));
                merged[table] = result.Merged;
                fileChanged |= result.FileChanged;
                changes.AddRange(RecordTypeChanges.Count(table, result.ToSaveLocally));

                var conflicted = result.Conflicts.Select(conflict => conflict.Remote.Id).ToHashSet();
                foreach (var record in result.Merged.Where(record => !conflicted.Contains(record.Id)))
                {
                    bases[SyncState.Key(table.Name, record.Id)] = record.UpdatedAt;
                }

                foreach (var conflict in result.Conflicts)
                {
                    // Keeps its old base (if any), so the next sync finds the conflict again until it's settled.
                    if (BaseOf(state, table, conflict.Local.Id) is { } oldBase)
                    {
                        bases[SyncState.Key(table.Name, conflict.Local.Id)] = oldBase;
                    }

                    conflicts.Add(new PendingConflict(table.Name, conflict.Local.Id, table.Write(conflict.Remote).ToJsonString()));
                }
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

            await SaveStateAsync(state with { LastSyncedAt = _clock.UtcNow, Bases = bases, Conflicts = conflicts });
            return new ChangeSummary(changes);
        }

        throw new RuleViolationException("Another device kept syncing at the same time. Try again in a moment.");
    }

    /// <summary>The Sync Conflicts waiting on this device, each with its two versions side by side.</summary>
    public async Task<IReadOnlyList<SyncConflictView>> ListConflictsAsync()
    {
        var state = await LoadStateAsync();
        var describer = await ConflictDescriber.CreateAsync(_store, (await _settings.GetAsync()).UnitSystem);
        var views = new List<SyncConflictView>();
        foreach (var conflict in state.Conflicts)
        {
            var (table, mine, theirs) = await VersionsAsync(conflict);
            views.Add(describer.Describe(conflict.Id, table, mine, theirs));
        }

        return views;
    }

    /// <summary>
    /// Settles a Sync Conflict with the chosen version: it's saved on this device as a new edit, so the next
    /// sync sends it to the file and every other device takes it.
    /// </summary>
    public async Task SettleAsync(string conflictId, SyncConflictChoice choice)
    {
        var state = await LoadStateAsync();
        var conflict = state.Conflicts.FirstOrDefault(pending => pending.Id == conflictId)
            ?? throw new RuleViolationException("That Sync Conflict has already been settled.");
        var (table, mine, theirs) = await VersionsAsync(conflict);
        var chosen = (choice == SyncConflictChoice.ThisDevice ? mine : theirs) with { UpdatedAt = _clock.UtcNow };

        // The other device's version becomes the base: next sync, the file still holds it (unchanged since),
        // and this device's new edit wins over it. If the other device changes it again first, that's a new conflict.
        var bases = new Dictionary<string, DateTimeOffset>(state.Bases) { [conflict.Id] = theirs.UpdatedAt };
        var settled = state with
        {
            UpdatedAt = _clock.UtcNow,
            Bases = bases,
            Conflicts = state.Conflicts.Where(pending => pending.Id != conflictId).ToList(),
        };
        await _store.SaveAllAsync([new RecordToSave(table.Name, chosen), new RecordToSave(SyncState.Table, settled)]);
        _settled();
    }

    private async Task<(RecordTable Table, StoredRecord Mine, StoredRecord Theirs)> VersionsAsync(PendingConflict conflict)
    {
        var table = RecordTable.Named(conflict.Table);
        var mine = await table.LoadAsync(_store, conflict.RecordId)
            ?? throw new InvalidOperationException($"The record {conflict.Id} in a Sync Conflict is missing.");
        return (table, mine, table.Read(JsonNode.Parse(conflict.OtherDeviceJson)!));
    }

    private static DateTimeOffset? BaseOf(SyncState state, RecordTable table, string id) =>
        state.Bases.TryGetValue(SyncState.Key(table.Name, id), out var @base) ? @base : null;

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
        await _store.LoadAsync<SyncState>(SyncState.Table, SyncState.SingleId)
            ?? new SyncState { Id = SyncState.SingleId, UpdatedAt = _clock.UtcNow };

    private Task SaveStateAsync(SyncState state) => _store.SaveAsync(SyncState.Table, state with { UpdatedAt = _clock.UtcNow });
}
