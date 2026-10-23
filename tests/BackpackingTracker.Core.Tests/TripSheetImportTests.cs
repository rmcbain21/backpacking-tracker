using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Import;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>
/// Importing a trip sheet as the backpacker keeps it in Excel: Category as section-header rows,
/// Item (the Gear Item's Gear Type), Name, and weights in g, lb and oz.
/// </summary>
public class TripSheetImportTests
{
    private const string Header = "Category,Item,Name,g,Lb.,oz";

    private readonly AppCore _app = new(new InMemoryRecordStore(), new FakeClock(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero)));

    private static string Sheet(params string[] rows) => string.Join("\r\n", [Header, .. rows]);

    /// <summary>Each imported row as (Gear Type, Name, grams, Category).</summary>
    private static (string GearType, string Name, decimal Grams, string Category)[] Imported(CsvImportSummary summary) =>
        summary.Rows
            .Where(row => row.GearItem is not null)
            .Select(row => (row.GearItem!.GearType, row.GearItem.Name, row.GearItem.WeightGrams, row.Category))
            .ToArray();

    [Fact]
    public async Task Section_headers_set_the_category_and_item_and_name_become_gear_type_and_name()
    {
        var summary = await _app.Import.ImportCsvAsync(Sheet(
            "Big Three,,,,,",
            ",Tent,Nemo Dragonfly,907,2,32",
            ",Quilt,EE Revelation 20,590,1.3,20.8",
            "Water,,,,,",
            ",Filter,Sawyer Squeeze,108,0.238,3.81"));

        Assert.Equal(
            [("Tent", "Nemo Dragonfly", 907m, "Big Three"), ("Quilt", "EE Revelation 20", 590m, "Big Three"), ("Filter", "Sawyer Squeeze", 108m, "Water")],
            Imported(summary));
        Assert.Equal(3, (await _app.Gear.ListGearItemsAsync()).Count);
    }

    [Fact]
    public async Task Total_rows_and_spacer_rows_are_skipped()
    {
        var summary = await _app.Import.ImportCsvAsync(Sheet(
            "Shelter,,,,,",
            ",Tent,Nemo Dragonfly,907,2,32",
            ",,Total:,907,2,32",
            ",,,,,",
            ",,,0,0,0",
            ",,,,0,0",
            ",,TOTAL: ,907,2,32",
            ",,total,907,2,32"));

        Assert.Single(summary.Rows);
        Assert.Single(summary.Created);
    }

    [Fact]
    public async Task A_row_with_no_item_has_no_gear_type()
    {
        var summary = await _app.Import.ImportCsvAsync(Sheet("Electronics,,,,,", ",,Electronics bag,24,0.053,0.847"));

        Assert.Equal([("", "Electronics bag", 24m, "Electronics")], Imported(summary));
    }

    [Theory]
    [InlineData(",Lamp,Biolite Alpenglow Mini,,0,0", "Weight (g) is missing.")]
    [InlineData(",Radio,Small Weather Radio,0,0,0", "Weight (g) is 0.")]
    [InlineData(",Socks,,,0,0", "Weight (g) is missing.")]
    public async Task Rows_without_a_weight_are_rejected(string row, string reason)
    {
        var summary = await _app.Import.ImportCsvAsync(Sheet("Electronics,,,,,", row, ",Headlamp,Nitecore NU25,45,0.099,1.587"));

        var rejected = Assert.Single(summary.Rejected);
        Assert.Equal(3, rejected.RowNumber);
        Assert.Equal(reason, rejected.Reason);
        Assert.Single(summary.Created);
    }

    [Fact]
    public async Task Gear_before_any_section_header_has_no_category()
    {
        var summary = await _app.Import.ImportCsvAsync(Sheet(",Bandana,Buff,28,0.062,0.988"));

        Assert.Equal([("Bandana", "Buff", 28m, "")], Imported(summary));
    }

    [Fact]
    public async Task Columns_are_found_by_name_in_any_order_and_qty_and_type_are_used_when_present()
    {
        var summary = await _app.Import.ImportCsvAsync(
            "oz,G,name,ITEM,Qty,Type,category\n" +
            ",,,,,,Clothing\n" +
            "9.3,264,OV Vario,Insulated Jacket,2,worn,");

        var row = Assert.Single(summary.Created);
        Assert.Equal(2, row.Quantity);
        Assert.Equal(CarryMode.Worn, row.CarryMode);
        Assert.Equal([("Insulated Jacket", "OV Vario", 264m, "Clothing")], Imported(summary));
    }

    [Fact]
    public async Task A_second_trip_sheet_reuses_gear_already_imported()
    {
        await _app.Import.ImportCsvAsync(Sheet("Shelter,,,,,", ",Tent,Nemo Dragonfly,907,2,32"));

        var summary = await _app.Import.ImportCsvAsync(Sheet("Big Three,,,,,", ",tent, nemo dragonfly ,907,2,32"));

        Assert.Single(summary.Reused);
        Assert.Equal("Big Three", summary.Reused[0].Category);
        Assert.Single(await _app.Gear.ListGearItemsAsync());
    }
}
