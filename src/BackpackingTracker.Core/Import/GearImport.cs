using System.Globalization;
using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Import;

/// <summary>Brings gear in from a spreadsheet saved as CSV.</summary>
public sealed class GearImport
{
    /// <summary>The columns the CSV's first row must name, in this order.</summary>
    public static readonly IReadOnlyList<string> Columns = ["Category", "Name", "Brand/Model", "Weight (g)", "Qty", "Carry Mode"];

    private readonly GearCatalogue _gear;

    public GearImport(GearCatalogue gear)
    {
        _gear = gear;
    }

    /// <summary>
    /// Imports each row as a Gear Item. A row whose name and brand/model match an existing Gear Item
    /// (ignoring case and spaces) reuses it unchanged; unknown Categories are created; a row that
    /// can't be imported is rejected with a reason and the rest still import.
    /// </summary>
    public async Task<CsvImportSummary> ImportCsvAsync(string csvText)
    {
        // Text that wasn't UTF-8 (Excel's plain "CSV") arrives with U+FFFD in place of letters like é.
        // Importing it would create Gear Items that never match a correctly saved sheet.
        if (csvText.Contains('�'))
        {
            throw new RuleViolationException(
                "Some letters in this file can't be read. In Excel, save it with Save As, \"CSV UTF-8\", and import that.");
        }

        var records = CsvReader.ReadRecords(csvText);
        if (records.Count == 0 || !IsHeader(records[0]))
        {
            throw new RuleViolationException($"The first row must be the column names: {string.Join(", ", Columns)}.");
        }

        var results = new List<CsvRowResult>();
        var createdCategories = new List<string>();
        var categories = (await _gear.ListCategoriesAsync()).ToList();

        // Retired gear matches too, so re-importing an old trip sheet doesn't recreate it.
        var knownItems = (await _gear.ListGearItemsAsync()).Concat(await _gear.ListRetiredGearItemsAsync()).ToList();

        for (var index = 1; index < records.Count; index++)
        {
            var row = new CsvRow(index + 1, records[index]);
            if (row.IsBlank)
            {
                continue;
            }

            var (values, problem) = ReadValues(row);
            if (values is null)
            {
                results.Add(new CsvRowResult(row.Number, CsvRowOutcome.Rejected, row.Name, Reason: problem));
                continue;
            }

            var match = knownItems.FirstOrDefault(item => Matches(item, row));
            if (match is not null)
            {
                var outcome = match.WeightGrams == values.WeightGrams ? CsvRowOutcome.Reused : CsvRowOutcome.Flagged;
                results.Add(new CsvRowResult(row.Number, outcome, match.Name, match, values.Quantity, values.CarryMode, values.WeightGrams));
                continue;
            }

            var category = FindCategory(categories, row.Category);
            if (category is null)
            {
                category = await _gear.AddCategoryAsync(row.Category);
                categories.Add(category);
                createdCategories.Add(category.Name);
            }

            var created = await _gear.AddGearItemAsync(new GearItemDetails(row.Name, row.BrandModel, values.WeightGrams, category.Id));
            knownItems.Add(created);
            results.Add(new CsvRowResult(row.Number, CsvRowOutcome.Created, created.Name, created, values.Quantity, values.CarryMode, values.WeightGrams));
        }

        return new CsvImportSummary(results, createdCategories);
    }

    /// <summary>The six column names, ignoring case and spaces; any further columns must be empty.</summary>
    private static bool IsHeader(IReadOnlyList<string> fields)
    {
        if (fields.Count < Columns.Count)
        {
            return false;
        }

        for (var i = 0; i < fields.Count; i++)
        {
            var expected = i < Columns.Count ? WithoutSpaces(Columns[i]) : "";
            if (!string.Equals(WithoutSpaces(fields[i]), expected, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string WithoutSpaces(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c)));

    /// <summary>
    /// The row's Category by name, ignoring case (with two of the same name, possible after a sync, the
    /// first in order). A blank Category means Misc, found by its fixed id so a renamed Misc still works.
    /// </summary>
    private static Category? FindCategory(IReadOnlyList<Category> categories, string name)
    {
        if (name.Length == 0)
        {
            return categories.FirstOrDefault(c => c.Id == DefaultCategories.MiscId)
                ?? categories.FirstOrDefault(c => string.Equals(c.Name, "Misc", StringComparison.OrdinalIgnoreCase));
        }

        return categories.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>The weight, Qty and Carry Mode a valid row gives.</summary>
    private sealed record RowValues(decimal WeightGrams, int Quantity, CarryMode CarryMode);

    /// <summary>Reads the row's values, or says why the row can't be imported.</summary>
    private static (RowValues? Values, string? Problem) ReadValues(CsvRow row)
    {
        if (row.Name.Length == 0)
        {
            return (null, "Name is missing.");
        }

        if (row.Weight.Length == 0)
        {
            return (null, "Weight (g) is missing.");
        }

        // Thousands separators are allowed: Excel saves a cell formatted as 1,234 that way.
        const NumberStyles weightStyles = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands;
        if (!decimal.TryParse(row.Weight, weightStyles, CultureInfo.InvariantCulture, out var weight))
        {
            return (null, $"Weight (g) \"{row.Weight}\" isn't a number.");
        }

        if (weight < 0)
        {
            return (null, "Weight (g) can't be negative.");
        }

        var quantity = 1;
        if (row.Qty.Length > 0 && (!int.TryParse(row.Qty, NumberStyles.None, CultureInfo.InvariantCulture, out quantity) || quantity < 1))
        {
            return (null, $"Qty \"{row.Qty}\" isn't a whole number of 1 or more.");
        }

        var carryMode = CarryMode.Base;
        if (row.CarryMode.Length > 0)
        {
            var named = Enum.GetValues<CarryMode>()
                .Where(mode => string.Equals(mode.ToString(), row.CarryMode, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (named.Count == 0)
            {
                return (null, $"Carry Mode \"{row.CarryMode}\" isn't base, worn or consumable.");
            }

            carryMode = named[0];
        }

        return (new RowValues(Math.Round(weight, 1, MidpointRounding.AwayFromZero), quantity, carryMode), null);
    }

    /// <summary>Same name and brand/model, ignoring case and surrounding spaces.</summary>
    private static bool Matches(GearItem item, CsvRow row) =>
        string.Equals(item.Name, row.Name, StringComparison.OrdinalIgnoreCase)
        && string.Equals(item.BrandModel, row.BrandModel, StringComparison.OrdinalIgnoreCase);

    /// <summary>One data row, with each column trimmed; missing trailing columns read as blank.</summary>
    private sealed record CsvRow(int Number, IReadOnlyList<string> Fields)
    {
        public string Category => Field(0);
        public string Name => Field(1);
        public string BrandModel => Field(2);
        public string Weight => Field(3);
        public string Qty => Field(4);
        public string CarryMode => Field(5);

        public bool IsBlank => Fields.All(string.IsNullOrWhiteSpace);

        private string Field(int index) => index < Fields.Count ? Fields[index].Trim() : "";
    }
}
