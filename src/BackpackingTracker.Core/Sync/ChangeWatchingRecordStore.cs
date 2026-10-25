using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Sync;

/// <summary>
/// The record store as the app's areas use it: each save also reports a local change, so automatic sync knows
/// there's something to send. Sync itself uses the plain store, so saving what a sync brought in doesn't set
/// off another sync.
/// </summary>
internal sealed class ChangeWatchingRecordStore : IRecordStore
{
    private readonly IRecordStore _store;
    private readonly Action _changed;

    public ChangeWatchingRecordStore(IRecordStore store, Action changed)
    {
        _store = store;
        _changed = changed;
    }

    public async Task SaveAsync<T>(string table, T record) where T : StoredRecord
    {
        await _store.SaveAsync(table, record);
        _changed();
    }

    public async Task SaveAllAsync(IReadOnlyList<RecordToSave> records)
    {
        await _store.SaveAllAsync(records);
        _changed();
    }

    public Task<T?> LoadAsync<T>(string table, string id) where T : StoredRecord => _store.LoadAsync<T>(table, id);

    public Task<IReadOnlyList<T>> ListAsync<T>(string table) where T : StoredRecord => _store.ListAsync<T>(table);
}
