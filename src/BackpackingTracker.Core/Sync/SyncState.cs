using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Sync;

/// <summary>
/// This device's sync bookkeeping. Local only: never in the sync file or an export, because it describes
/// this device's view of the file.
/// </summary>
internal sealed record SyncState : StoredRecord
{
    public const string Table = "syncState";

    public const string SingleId = "00000000-0000-0000-0000-000000000002";

    /// <summary>Whether the backpacker has connected a cloud drive on this device.</summary>
    public bool Connected { get; init; }

    public DateTimeOffset? LastSyncedAt { get; init; }

    /// <summary>
    /// Each record's <see cref="StoredRecord.UpdatedAt"/> at the last successful sync (its base), keyed by
    /// <see cref="Key"/>. A side whose UpdatedAt differs from the base has changed since then. A record in a
    /// Sync Conflict keeps its old base, so the conflict is found again on each sync until it's settled.
    /// </summary>
    public IReadOnlyDictionary<string, DateTimeOffset> Bases { get; init; } = new Dictionary<string, DateTimeOffset>();

    /// <summary>The Sync Conflicts waiting for the backpacker, as found by the last successful sync.</summary>
    public IReadOnlyList<PendingConflict> Conflicts { get; init; } = [];

    /// <summary>Identifies a record across tables, such as "gearItems/0b6c…".</summary>
    public static string Key(string table, string id) => $"{table}/{id}";
}

/// <summary>
/// A Sync Conflict waiting on this device. This device's version is the record in the store; the other
/// device's version is kept here as JSON, so the conflict can be shown and settled without a connection.
/// </summary>
internal sealed record PendingConflict(string Table, string RecordId, string OtherDeviceJson)
{
    [JsonIgnore]
    public string Id => SyncState.Key(Table, RecordId);
}
