using System.Globalization;
using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Import;

/// <summary>
/// Brings gear in from a spreadsheet saved as CSV, in one of two layouts:
/// <list type="bullet">
/// <item>The gear layout: one row per Gear Item, with the fixed <see cref="Columns"/>.</item>
/// <item>A trip sheet, as kept in Excel: Category as section-header rows, then rows with Item (the
/// Gear Item's Gear Type), Name and a g column, plus subtotal and spacer rows to skip. Columns are
/// found by name; others, such as lb and oz, are ignored.</item>
/// </list>
/// </summary>
public sealed class GearImport
{
    /// <summary>The columns the gear layout's first row must name, in this order.</summary>
    public static readonly IReadOnlyList<string> Columns = ["Category", "Gear Type", "Name", "Weight (g)", "Qty", "Carry Mode"];

    /// <summary>The columns a trip sheet's first row must include, in any order.</summary>
    public static readonly IReadOnlyList<string> TripSheetColumns = ["Category", "Item", "Name", "g"];

    // Text that wasn't UTF-8 (Excel's plain "CSV") arrives with this in place of letters like é.
    private const char UnreadableCharacter = (char)0xFFFD;

    private readonly GearCatalogue _gear;

    public GearImport(GearCatalogue gear)
    {
        _gear = gear;
    }

    /// <summary>
    /// Imports each row as a Gear Item. A row whose Gear Type and Name match an existing Gear Item
    /// (ignoring case and spaces) reuses it unchanged; a row that can't be imported is rejected with
    /// a reason and the rest still import.
    /// </summary>
    public async Task<CsvImportSummary> ImportCsvAsync(string csvText)
    {
        // Importing garbled letters would create Gear Items that never match a correctly saved sheet.
        if (csvText.Contains(UnreadableCharacter))
        {
            throw new RuleViolationException(
                "Some letters in this file can't be read. In Excel, save it with Save As, \"CSV UTF-8\", and import that.");
        }

        var records = CsvReader.ReadRecords(csvText);
        var layout = records.Count == 0 ? null : SheetLayout.FromHeader(records[0]);
        if (layout is null)
        {
            throw new RuleViolationException(
                $"The first row must be the column names: {string.Join(", ", Columns)} " +
                $"(or, for a trip sheet: {string.Join(", ", TripSheetColumns)}).");
        }

        var results = new List<CsvRowResult>();

        // Retired gear matches too, so re-importing an old trip sheet doesn't recreate it.
        var knownItems = (await _gear.ListGearItemsAsync()).Concat(await _gear.ListRetiredGearItemsAsync()).ToList();

        foreach (var row in layout.ReadGearRows(records))
        {
            var (values, problem) = ReadValues(row);
            if (values is null)
            {
                results.Add(new CsvRowResult(row.Number, CsvRowOutcome.Rejected, row.Name, row.Category, Reason: problem));
                continue;
            }

            var match = knownItems.FirstOrDefault(item => Matches(item, row));
            if (match is not null)
            {
                var outcome = match.WeightGrams == values.WeightGrams ? CsvRowOutcome.Reused : CsvRowOutcome.Flagged;
                results.Add(new CsvRowResult(row.Number, outcome, match.Name, row.Category, match, values.Quantity, values.CarryMode, values.WeightGrams));
                continue;
            }

            var created = await _gear.AddGearItemAsync(new GearItemDetails(row.Name, row.GearType, values.WeightGrams));
            knownItems.Add(created);
            results.Add(new CsvRowResult(row.Number, CsvRowOutcome.Created, created.Name, row.Category, created, values.Quantity, values.CarryMode, values.WeightGrams));
        }

        return new CsvImportSummary(results);
    }

    /// <summary>The weight, Qty and Carry Mode a valid row gives.</summary>
    private sealed record RowValues(decimal WeightGrams, int Quantity, CarryMode CarryMode);

    /// <summary>Reads the row's values, or says why the row can't be imported.</summary>
    private static (RowValues? Values, string? Problem) ReadValues(GearRow row)
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

        // In a spreadsheet, 0 means the item hasn't been weighed yet.
        if (weight == 0)
        {
            return (null, "Weight (g) is 0.");
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

    /// <summary>Same Gear Type and Name, ignoring case and surrounding spaces.</summary>
    private static bool Matches(GearItem item, GearRow row) =>
        string.Equals(item.GearType, row.GearType, StringComparison.OrdinalIgnoreCase)
        && string.Equals(item.Name, row.Name, StringComparison.OrdinalIgnoreCase);

    /// <summary>A row that describes a Gear Item, whichever layout it came from. Every value is trimmed.</summary>
    private sealed record GearRow(
        int Number, string Category, string GearType, string Name, string Weight, string Qty, string CarryMode);

    /// <summary>Where a layout's values are, by column position (-1 for a column it doesn't have).</summary>
    private sealed record SheetLayout(
        bool IsTripSheet, int Category, int GearType, int Name, int Weight, int Qty, int CarryMode)
    {
        /// <summary>The layout the header row describes, or null if it's neither.</summary>
        public static SheetLayout? FromHeader(IReadOnlyList<string> header)
        {
            var names = header.Select(WithoutSpaces).ToList();

            // The gear layout: exactly the six columns in order; any further columns must be empty.
            var isGearLayout = names.Count >= Columns.Count
                && names.Select((name, i) => i < Columns.Count ? Same(name, Columns[i]) : name.Length == 0).All(ok => ok);
            if (isGearLayout)
            {
                return new SheetLayout(IsTripSheet: false, Category: 0, GearType: 1, Name: 2, Weight: 3, Qty: 4, CarryMode: 5);
            }

            int IndexOf(string column) => names.FindIndex(name => Same(name, column));

            // A trip sheet: Item is the Gear Item's Gear Type and Name is its Name.
            var tripSheet = new SheetLayout(
                IsTripSheet: true, Category: IndexOf("Category"), GearType: IndexOf("Item"), Name: IndexOf("Name"),
                Weight: IndexOf("g"), Qty: IndexOf("Qty"), CarryMode: IndexOf("Type"));
            var hasRequiredColumns = tripSheet.Category >= 0 && tripSheet.GearType >= 0 && tripSheet.Name >= 0 && tripSheet.Weight >= 0;
            return hasRequiredColumns ? tripSheet : null;
        }

        /// <summary>
        /// The rows after the header that describe Gear Items, skipping blank, section-header and subtotal
        /// rows. A row with a Gear Type but no Name uses the Gear Type as its Name.
        /// </summary>
        public IEnumerable<GearRow> ReadGearRows(IReadOnlyList<IReadOnlyList<string>> records)
        {
            var section = "";
            for (var index = 1; index < records.Count; index++)
            {
                var fields = records[index];
                string Field(int column) => column >= 0 && column < fields.Count ? fields[column].Trim() : "";

                var row = new GearRow(
                    Number: index + 1, Field(Category), Field(GearType), Field(Name), Field(Weight), Field(Qty), Field(CarryMode));

                if (IsTripSheet)
                {
                    // A trip sheet's Category is a section-header row that applies to the rows below it.
                    if (row.Category.Length > 0)
                    {
                        section = row.Category;
                    }

                    // Rows with neither Item nor Name are section headers or spacers; "Total:" rows are subtotals.
                    if (row.GearType.Length == 0 && (row.Name.Length == 0 || IsSubtotal(row.Name)))
                    {
                        continue;
                    }

                    row = row with { Category = section };
                }
                else if (fields.All(string.IsNullOrWhiteSpace))
                {
                    continue;
                }

                yield return row.Name.Length == 0 ? row with { Name = row.GearType } : row;
            }
        }

        private static bool IsSubtotal(string text) =>
            text.TrimEnd(':', ' ').Equals("Total", StringComparison.OrdinalIgnoreCase);

        private static bool Same(string name, string column) =>
            string.Equals(name, WithoutSpaces(column), StringComparison.OrdinalIgnoreCase);
    }

    private static string WithoutSpaces(string text) => string.Concat(text.Where(c => !char.IsWhiteSpace(c)));
}
