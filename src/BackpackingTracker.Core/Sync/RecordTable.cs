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
        new RecordTable<GearItem>(GearCatalogue.Table, "Gear Items"),
        new RecordTable<DefaultCategory>(GearCatalogue.CategoryTable, "Default Categories"),
        new RecordTable<GearList>(GearLists.Table, "Gear Lists"),
        new RecordTable<FoodItem>(FoodCatalogue.Table, "Food Items"),
        new RecordTable<Meal>(MealCatalogue.Table, "Meals"),
        new RecordTable<Trip>(TripPlanner.Table, "Trips"),
        new RecordTable<Settings>(UserSettings.Table, "Settings"),
    ];

    protected RecordTable(string name, string recordTypeName)
    {
        Name = name;
        RecordTypeName = recordTypeName;
    }

    /// <summary>The table's name in the record store and the file, such as "gearItems".</summary>
    public string Name { get; }

    /// <summary>What the backpacker calls these records, such as "Gear Items".</summary>
    public string RecordTypeName { get; }

    /// <summary>Every record in the table, tombstones included.</summary>
    public abstract Task<IReadOnlyList<StoredRecord>> ListAsync(IRecordStore store);

    /// <summary>Reads one record of this table's type; throws <see cref="JsonException"/> if it isn't one.</summary>
    public abstract StoredRecord Read(JsonNode node);

    public JsonNode Write(StoredRecord record) => JsonSerializer.SerializeToNode(record, record.GetType(), JsonOptions)!;
}

internal sealed class RecordTable<T> : RecordTable where T : StoredRecord
{
    public RecordTable(string name, string recordTypeName) : base(name, recordTypeName)
    {
    }

    public override async Task<IReadOnlyList<StoredRecord>> ListAsync(IRecordStore store) => await store.ListAsync<T>(Name);

    public override StoredRecord Read(JsonNode node) =>
        node.Deserialize<T>(JsonOptions) is { Id: not null } record
            ? record
            : throw new JsonException($"A record in {Name} has no id.");
}
