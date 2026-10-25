using System.Text.Json;
using System.Text.Json.Nodes;
using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Preferences;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Sync;

/// <summary>
/// One table of stored records, with the record type it holds, so whole-database operations (export, import
/// and sync) can read and write every table without knowing each type.
/// </summary>
internal abstract class RecordTable
{
    // The same JSON shape the record store uses, so a record looks the same in the file as on the device.
    internal static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Every table, in the order they appear in the file and in import summaries.</summary>
    public static readonly IReadOnlyList<RecordTable> All =
    [
        new RecordTable<GearItem>(GearCatalogue.Table, "Gear Item", "Gear Items"),
        new RecordTable<DefaultCategory>(GearCatalogue.CategoryTable, "Default Category", "Default Categories"),
        new RecordTable<GearList>(GearLists.Table, "Gear List", "Gear Lists"),
        new RecordTable<FoodItem>(FoodCatalogue.Table, "Food Item", "Food Items"),
        new RecordTable<Meal>(MealCatalogue.Table, "Meal", "Meals"),
        new RecordTable<Trip>(TripPlanner.Table, "Trip", "Trips"),
        new RecordTable<Settings>(UserSettings.Table, "Settings", "Settings"),
    ];

    protected RecordTable(string name, string recordName, string recordTypeName)
    {
        Name = name;
        RecordName = recordName;
        RecordTypeName = recordTypeName;
    }

    public static RecordTable Named(string name) =>
        All.FirstOrDefault(table => table.Name == name) ?? throw new InvalidOperationException($"No table named {name}.");

    /// <summary>The table's name in the record store and the file, such as "gearItems".</summary>
    public string Name { get; }

    /// <summary>What the backpacker calls one of these records, such as "Gear Item".</summary>
    public string RecordName { get; }

    /// <summary>What the backpacker calls these records, such as "Gear Items".</summary>
    public string RecordTypeName { get; }

    /// <summary>Every record in the table, tombstones included.</summary>
    public abstract Task<IReadOnlyList<StoredRecord>> ListAsync(IRecordStore store);

    /// <summary>One record, tombstone or not; null if the table has no record with that id.</summary>
    public abstract Task<StoredRecord?> LoadAsync(IRecordStore store, string id);

    /// <summary>Reads one record of this table's type; throws <see cref="JsonException"/> if it isn't one.</summary>
    public abstract StoredRecord Read(JsonNode node);

    public JsonNode Write(StoredRecord record) => JsonSerializer.SerializeToNode(record, record.GetType(), JsonOptions)!;

    /// <summary>
    /// Whether two versions of a record hold the same contents, ignoring when they were changed. Compared as
    /// JSON because a record's own equality compares its lists (such as Tags) by reference.
    /// </summary>
    public bool SameContents(StoredRecord first, StoredRecord second)
    {
        var (a, b) = ((JsonObject)Write(first), (JsonObject)Write(second));
        a.Remove("updatedAt");
        b.Remove("updatedAt");
        return JsonNode.DeepEquals(a, b);
    }
}

internal sealed class RecordTable<T> : RecordTable where T : StoredRecord
{
    public RecordTable(string name, string recordName, string recordTypeName) : base(name, recordName, recordTypeName)
    {
    }

    public override async Task<IReadOnlyList<StoredRecord>> ListAsync(IRecordStore store) => await store.ListAsync<T>(Name);

    public override async Task<StoredRecord?> LoadAsync(IRecordStore store, string id) => await store.LoadAsync<T>(Name, id);

    public override StoredRecord Read(JsonNode node) =>
        node.Deserialize<T>(JsonOptions) is { Id: not null } record
            ? record
            : throw new JsonException($"A record in {Name} has no id.");
}
