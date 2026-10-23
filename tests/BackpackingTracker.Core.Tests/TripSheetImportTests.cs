using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>
/// Importing a trip sheet as the backpacker keeps it in Excel: Category as section-header rows,
/// Item (the Gear Item's name), Name (its brand/model), and weights in g, lb and oz.
/// </summary>
public class TripSheetImportTests
{
    private const string Header = "Category,Item,Name,g,Lb.,oz";

    private readonly AppCore _app = new(new InMemoryRecordStore(), new FakeClock(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero)));

    private static string Sheet(params string[] rows) => string.Join("\r\n", [Header, .. rows]);

    private async Task<(string Name, string BrandModel, decimal Grams, string Category)[]> ImportedGearAsync()
    {
        var categories = await _app.Gear.ListCategoriesAsync();
        return (await _app.Gear.ListGearItemsAsync())
            .Select(item => (item.Name, item.BrandModel, item.WeightGrams, categories.Single(c => c.Id == item.CategoryId).Name))
            .ToArray();
    }

    [Fact]
    public async Task Section_headers_set_the_category_and_item_and_name_become_name_and_brand_model()
    {
        var summary = await _app.Import.ImportCsvAsync(Sheet(
            "Big Three,,,,,",
            ",Tent,Nemo Dragonfly,907,2,32",
            ",Quilt,EE Revelation 20,590,1.3,20.8",
            "Water,,,,,",
            ",Filter,Sawyer Squeeze,108,0.238,3.81"));

        Assert.Equal(3, summary.Created.Count);
        Assert.Equal(["Big Three"], summary.CreatedCategories);
        Assert.Equal(
            [("Filter", "Sawyer Squeeze", 108m, "Water"), ("Quilt", "EE Revelation 20", 590m, "Big Three"), ("Tent", "Nemo Dragonfly", 907m, "Big Three")],
            await ImportedGearAsync());
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
    public async Task A_row_with_no_item_uses_its_name_as_the_gear_item_name()
    {
        await _app.Import.ImportCsvAsync(Sheet("Electronics,,,,,", ",,Electronics bag,24,0.053,0.847"));

        Assert.Equal([("Electronics bag", "", 24m, "Electronics")], await ImportedGearAsync());
    }

    [Theory]
    [InlineData(",Lamp,Biolite Alpenglow Mini,,0,0", "Weight (g) is missing.")]
    [InlineData(",Radio,Small Weather Radio,0,0,0", "Weight (g) is 0.")]
    public async Task Rows_without_a_weight_are_rejected(string row, string reason)
    {
        var summary = await _app.Import.ImportCsvAsync(Sheet("Electronics,,,,,", row, ",Headlamp,Nitecore NU25,45,0.099,1.587"));

        var rejected = Assert.Single(summary.Rejected);
        Assert.Equal(3, rejected.RowNumber);
        Assert.Equal(reason, rejected.Reason);
        Assert.Single(summary.Created);
    }

    [Fact]
    public async Task Gear_before_any_section_header_goes_in_misc()
    {
        await _app.Import.ImportCsvAsync(Sheet(",Bandana,,28,0.062,0.988"));

        Assert.Equal([("Bandana", "", 28m, "Misc")], await ImportedGearAsync());
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
        Assert.Equal([("Insulated Jacket", "OV Vario", 264m, "Clothing")], await ImportedGearAsync());
    }

    [Fact]
    public async Task A_second_trip_sheet_reuses_gear_already_imported()
    {
        await _app.Import.ImportCsvAsync(Sheet("Shelter,,,,,", ",Tent,Nemo Dragonfly,907,2,32"));

        var summary = await _app.Import.ImportCsvAsync(Sheet("Big Three,,,,,", ",tent, nemo dragonfly ,907,2,32"));

        Assert.Single(summary.Reused);
        Assert.Empty(summary.CreatedCategories);
        Assert.Single(await _app.Gear.ListGearItemsAsync());
    }
}
