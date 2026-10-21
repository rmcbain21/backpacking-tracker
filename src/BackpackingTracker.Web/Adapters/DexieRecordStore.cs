using BackpackingTracker.Core;
using BackpackingTracker.Core.Ports;
using Microsoft.JSInterop;

namespace BackpackingTracker.Web.Adapters;

/// <summary>
/// Stores records in the browser's IndexedDB through Dexie (wwwroot/js/recordStore.js).
/// </summary>
public sealed class DexieRecordStore : IRecordStore, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module;

    public DexieRecordStore(IJSRuntime js)
    {
        _module = new Lazy<Task<IJSObjectReference>>(
            () => js.InvokeAsync<IJSObjectReference>("import", "./js/recordStore.js").AsTask());
    }

    public async Task SaveAsync<T>(string table, T record) where T : StoredRecord
    {
        var module = await _module.Value;
        await module.InvokeVoidAsync("save", table, record);
    }

    public async Task<T?> LoadAsync<T>(string table, string id) where T : StoredRecord
    {
        var module = await _module.Value;
        return await module.InvokeAsync<T?>("load", table, id);
    }

    public async Task<IReadOnlyList<T>> ListAsync<T>(string table) where T : StoredRecord
    {
        var module = await _module.Value;
        return await module.InvokeAsync<List<T>>("list", table);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
    }
}
