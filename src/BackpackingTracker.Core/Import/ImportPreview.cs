using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Import;

/// <summary>
/// What importing a file will do, worked out without saving anything: what happens to each row, and the
/// Categories and entries of the Gear List the import can create. <see cref="GearImport.ImportAsync"/> saves it.
/// </summary>
public sealed record ImportPreview(
    IReadOnlyList<CsvRowResult> Rows,
    IReadOnlyList<ImportSection> Sections,
    IReadOnlyList<string> NewDefaultCategoryNames)
{
    public IReadOnlyList<CsvRowResult> Created => RowsWith(CsvRowOutcome.Created);

    public IReadOnlyList<CsvRowResult> Reused => RowsWith(CsvRowOutcome.Reused);

    public IReadOnlyList<CsvRowResult> Flagged => RowsWith(CsvRowOutcome.Flagged);

    public IReadOnlyList<CsvRowResult> Rejected => RowsWith(CsvRowOutcome.Rejected);

    /// <summary>False when no row has gear, so there's nothing to make a Gear List from.</summary>
    public bool CanCreateGearList => Sections.Count > 0;

    private IReadOnlyList<CsvRowResult> RowsWith(CsvRowOutcome outcome) => Rows.Where(row => row.Outcome == outcome).ToList();
}

/// <summary>
/// One of the sheet's sections, in sheet order: a Category of the Gear List the import can create, with its
/// entries. A section whose rows were all rejected has no entries but is still a Category.
/// </summary>
public sealed record ImportSection(string Name, IReadOnlyList<ImportEntry> Entries);

/// <summary>A List Entry the import will create: every row for this Gear Item in the section, added up.</summary>
public sealed record ImportEntry(GearItem GearItem, int Quantity, CarryMode CarryMode);

/// <summary>
/// The choices beside the preview. GearListName is the name of the Gear List to create, or null for none;
/// AddSectionsToDefaultCategories adds the sections the Default Categories don't have yet.
/// </summary>
public sealed record ImportOptions(string? GearListName = null, bool AddSectionsToDefaultCategories = false);
