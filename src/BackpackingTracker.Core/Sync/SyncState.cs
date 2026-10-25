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
    /// <see cref="BaseKey"/>. A side whose UpdatedAt differs from the base has changed since then.
    /// </summary>
    public IReadOnlyDictionary<string, DateTimeOffset> Bases { get; init; } = new Dictionary<string, DateTimeOffset>();

    public static string BaseKey(string table, string id) => $"{table}/{id}";
}
