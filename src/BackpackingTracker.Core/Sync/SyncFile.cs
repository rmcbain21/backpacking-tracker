using System.Text.Json;
using System.Text.Json.Nodes;

namespace BackpackingTracker.Core.Sync;

/// <summary>
/// The JSON document holding a whole database: a format version and every record of every type, tombstones
/// included. It is both the JSON export and the sync file.
/// </summary>
public static class SyncFile
{
    /// <summary>The format this app writes, and the only one it reads.</summary>
    public const int FormatVersion = 1;

    private const string NotAnExport = "This file isn't a Backpacking Tracker export, so nothing was imported.";

    private static readonly JsonSerializerOptions WriteOptions = new(RecordTable.JsonOptions) { WriteIndented = true };

    /// <summary>The records of each table as a file, each table's records in id order so the same data always gives the same file.</summary>
    internal static string Write(IReadOnlyDictionary<RecordTable, IReadOnlyList<StoredRecord>> records)
    {
        var tables = new JsonObject();
        foreach (var table in RecordTable.All)
        {
            var rows = records.GetValueOrDefault(table, []);
            tables[table.Name] = new JsonArray(rows.OrderBy(record => record.Id, StringComparer.Ordinal).Select(table.Write).ToArray());
        }

        var file = new JsonObject { ["formatVersion"] = FormatVersion, ["records"] = tables };
        return file.ToJsonString(WriteOptions);
    }

    /// <summary>
    /// The records of each table in the file. Refuses a file that isn't an export, or is in a format this
    /// app can't read, with a message for the backpacker.
    /// </summary>
    internal static IReadOnlyDictionary<RecordTable, IReadOnlyList<StoredRecord>> Read(string text)
    {
        try
        {
            if (JsonNode.Parse(text) is not JsonObject file
                || file["formatVersion"] is not JsonValue versionValue
                || !versionValue.TryGetValue(out int version))
            {
                throw new RuleViolationException(NotAnExport);
            }

            if (version != FormatVersion)
            {
                throw new RuleViolationException(
                    $"This export is in format version {version}, which this version of the app can't read, so nothing was imported.");
            }

            var tables = file["records"] as JsonObject ?? throw new RuleViolationException(NotAnExport);
            return RecordTable.All.ToDictionary(
                table => table,
                table => (IReadOnlyList<StoredRecord>)(tables[table.Name]?.AsArray() ?? [])
                    .Select(node => table.Read(node ?? throw new JsonException("An empty record.")))
                    .ToList());
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            // Malformed JSON, or records that aren't what their table holds.
            throw new RuleViolationException(NotAnExport);
        }
    }
}
