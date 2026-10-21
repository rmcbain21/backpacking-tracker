using System.Text.Json;
using BackpackingTracker.Core;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Tests.Fakes;

/// <summary>
/// Keeps records as JSON, like the real store, so a caller can't change a stored record
/// by holding on to the object it saved.
/// </summary>
public sealed class InMemoryRecordStore : IRecordStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly Dictionary<string, Dictionary<string, string>> _tables = new();

    public Task SaveAsync<T>(string table, T record) where T : StoredRecord
    {
        if (!_tables.TryGetValue(table, out var rows))
        {
            rows = new Dictionary<string, string>();
            _tables[table] = rows;
        }

        rows[record.Id] = JsonSerializer.Serialize(record, JsonOptions);
        return Task.CompletedTask;
    }

    public Task<T?> LoadAsync<T>(string table, string id) where T : StoredRecord
    {
        if (_tables.TryGetValue(table, out var rows) && rows.TryGetValue(id, out var json))
        {
            return Task.FromResult(JsonSerializer.Deserialize<T>(json, JsonOptions));
        }

        return Task.FromResult<T?>(null);
    }

    public Task<IReadOnlyList<T>> ListAsync<T>(string table) where T : StoredRecord
    {
        if (!_tables.TryGetValue(table, out var rows))
        {
            return Task.FromResult<IReadOnlyList<T>>([]);
        }

        IReadOnlyList<T> records = rows.Values
            .Select(json => JsonSerializer.Deserialize<T>(json, JsonOptions)!)
            .ToList();
        return Task.FromResult(records);
    }
}
