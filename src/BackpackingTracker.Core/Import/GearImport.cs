using System.Globalization;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;

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

    /// <summary>The Gear List Category for rows that come before any section (or have a blank Category).</summary>
    public const string NoSectionName = "Other";

    private readonly IRecordStore _store;
    private readonly GearCatalogue _gear;
    private readonly GearLists _lists;

    public GearImport(IRecordStore store, GearCatalogue gear, GearLists lists)
    {
        _store = store;
        _gear = gear;
        _lists = lists;
    }

    /// <summary>
    /// Works out what importing the file will do, saving nothing. Each row becomes a Gear Item: a row whose
    /// Gear Type and Name match an existing Gear Item (ignoring case and spaces) reuses it unchanged; a row
    /// that can't be imported is rejected with a reason and the rest still import.
    /// </summary>
    public async Task<ImportPreview> PreviewCsvAsync(string csvText)
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

            var created = await _gear.NewGearItemAsync(new GearItemDetails(row.Name, row.GearType, values.WeightGrams));

            // Not saved yet, so give it the Gear Type spelling of an earlier new Gear Item in this file.
            var spelling = knownItems.Select(item => item.GearType).FirstOrDefault(t => TagRules.Comparer.Equals(t, created.GearType));
            created = created with { GearType = spelling ?? created.GearType };
            knownItems.Add(created);
            results.Add(new CsvRowResult(row.Number, CsvRowOutcome.Created, created.Name, row.Category, created, values.Quantity, values.CarryMode, values.WeightGrams));
        }

        var sections = Sections(results);
        var newDefaults = await _gear.NewDefaultCategoriesAsync(SectionNames(results));
        return new ImportPreview(results, sections, newDefaults.Select(category => category.Name).ToList());
    }

    /// <summary>
    /// Saves what the preview showed, all at once (if anything fails, nothing is saved): the new Gear Items,
    /// and as the options say, a Gear List with the sheet's sections as its Categories and the sections
    /// added to the Default Categories. Returns the created Gear List, or null if none was asked for.
    /// </summary>
    public async Task<GearList?> ImportAsync(ImportPreview preview, ImportOptions options)
    {
        // A preview imported twice, or gear added since, would otherwise create the same Gear Item twice.
        var knownItems = (await _gear.ListGearItemsAsync()).Concat(await _gear.ListRetiredGearItemsAsync()).ToList();
        if (preview.Created.Any(row => knownItems.Any(item => Matches(item, row.GearItem!.GearType, row.GearItem.Name))))
        {
            throw new RuleViolationException("Your gear has changed since this preview. Choose the file again.");
        }

        var records = preview.Created
            .Select(row => new RecordToSave(GearCatalogue.Table, row.GearItem!))
            .ToList();

        GearList? list = null;
        if (options.GearListName is { } name)
        {
            if (!preview.CanCreateGearList)
            {
                throw new RuleViolationException("There's no gear in this file to make a Gear List from.");
            }

            var categories = preview.Sections.Select(section => new Category { Id = Guid.NewGuid().ToString(), Name = section.Name }).ToList();
            var entries = preview.Sections
                .Zip(categories, (section, category) => section.Entries.Select(entry => new ListEntry
                {
                    Id = Guid.NewGuid().ToString(),
                    GearItemId = entry.GearItem.Id,
                    CategoryId = category.Id,
                    Quantity = entry.Quantity,
                    CarryMode = entry.CarryMode,
                }))
                .SelectMany(sectionEntries => sectionEntries)
                .ToList();
            list = _lists.NewGearList(name, categories, entries);
            records.Add(new RecordToSave(GearLists.Table, list));
        }

        if (options.AddSectionsToDefaultCategories)
        {
            var newDefaults = await _gear.NewDefaultCategoriesAsync(SectionNames(preview.Rows));
            records.AddRange(newDefaults.Select(category => new RecordToSave(GearCatalogue.CategoryTable, category)));
        }

        await _store.SaveAllAsync(records);
        return list;
    }

    /// <summary>The sheet's section names in sheet order, each once; rows with no section aren't one.</summary>
    private static IEnumerable<string> SectionNames(IEnumerable<CsvRowResult> rows) =>
        rows.Select(row => row.Category).Where(name => name.Length > 0).Distinct(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Each section in sheet order (rows with no section under <see cref="NoSectionName"/>), with one entry per
    /// Gear Item: its rows' quantities added up, and the first row's Carry Mode.
    /// </summary>
    private static IReadOnlyList<ImportSection> Sections(IReadOnlyList<CsvRowResult> rows) =>
        rows
            .GroupBy(row => row.Category.Length > 0 ? row.Category : NoSectionName, StringComparer.OrdinalIgnoreCase)
            .Select(section => new ImportSection(
                section.First().Category.Length > 0 ? section.First().Category : NoSectionName,
                section
                    .Where(row => row.GearItem is not null)
                    .GroupBy(row => row.GearItem!.Id)
                    .Select(item => new ImportEntry(item.First().GearItem!, item.Sum(row => row.Quantity), item.First().CarryMode))
                    .ToList()))
            .ToList();

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
    private static bool Matches(GearItem item, GearRow row) => Matches(item, row.GearType, row.Name);

    private static bool Matches(GearItem item, string gearType, string name) =>
        string.Equals(item.GearType, gearType.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(item.Name, name.Trim(), StringComparison.OrdinalIgnoreCase);

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
