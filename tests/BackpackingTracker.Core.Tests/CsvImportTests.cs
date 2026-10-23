using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>Importing the gear layout: one row per Gear Item, with fixed columns.</summary>
public class CsvImportTests
{
    private const string Header = "Category,Gear Type,Name,Weight (g),Qty,Carry Mode";

    private readonly AppCore _app = new(new InMemoryRecordStore(), new FakeClock(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero)));

    private static string Csv(params string[] rows) => string.Join("\n", [Header, .. rows]);

    private Task<GearItem> AddExistingTentAsync() =>
        _app.Gear.AddGearItemAsync(new GearItemDetails("Durston X-Mid 1", "Tent", 765.4m, ["solo"]));

    [Fact]
    public async Task Each_row_becomes_a_gear_item_with_its_gear_type_and_name()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv(
            "Shelter,Tent,Durston X-Mid 1,765.4,,",
            "Sleep,Quilt,EE Revelation 20,590,,"));

        var items = await _app.Gear.ListGearItemsAsync();
        Assert.Equal(
            [("Quilt", "EE Revelation 20", 590m), ("Tent", "Durston X-Mid 1", 765.4m)],
            items.Select(item => (item.GearType, item.Name, item.WeightGrams)));
        Assert.Equal([2, 3], summary.Created.Select(row => row.RowNumber));
    }

    [Fact]
    public async Task Each_row_keeps_its_category_for_a_gear_list()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv(
            "Big Three,Tent,Durston X-Mid 1,765,,",
            ",Bandana,,28,,"));

        Assert.Equal(["Big Three", ""], summary.Rows.Select(row => row.Category));
    }

    [Fact]
    public async Task Importing_does_not_change_the_default_categories()
    {
        var before = (await _app.Gear.ListCategoriesAsync()).Select(category => category.Name).ToList();

        await _app.Import.ImportCsvAsync(Csv("Big Three,Tent,Durston X-Mid 1,765,,"));

        Assert.Equal(before, (await _app.Gear.ListCategoriesAsync()).Select(category => category.Name));
    }

    [Theory]
    [InlineData("Category,Name,Brand/Model,Weight (g),Qty,Carry Mode")]
    [InlineData("Gear Type,Category,Name,Weight (g),Qty,Carry Mode")]
    [InlineData("Category,Gear Type,Name,Weight,Qty,Carry Mode")]
    [InlineData("")]
    public async Task A_file_without_the_expected_header_is_refused(string header)
    {
        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportCsvAsync(header + "\nShelter,Tent,,765,,"));

        Assert.Equal(
            "The first row must be the column names: Category, Gear Type, Name, Weight (g), Qty, Carry Mode (or, for a trip sheet: Category, Item, Name, g).",
            refused.Message);
        Assert.Empty(await _app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task A_row_matching_an_existing_gear_item_reuses_it_unchanged()
    {
        var existing = await AddExistingTentAsync();

        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,  TENT , durston x-mid 1 ,765.4,,"));

        var reused = Assert.Single(summary.Reused);
        Assert.Equal(existing.Id, reused.GearItem!.Id);
        var tent = Assert.Single(await _app.Gear.ListGearItemsAsync());
        Assert.Equal(("Durston X-Mid 1", "Tent", 765.4m), (tent.Name, tent.GearType, tent.WeightGrams));
        Assert.Equal(["solo"], tent.Tags);
        Assert.Equal(existing.UpdatedAt, tent.UpdatedAt);
    }

    [Fact]
    public async Task A_matching_row_with_a_different_weight_is_flagged_with_both_weights()
    {
        var existing = await AddExistingTentAsync();

        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,Tent,Durston X-Mid 1,794,,"));

        var flagged = Assert.Single(summary.Flagged);
        Assert.Empty(summary.Reused);
        Assert.Equal(794m, flagged.CsvWeightGrams);
        Assert.Equal(765.4m, flagged.GearItem!.WeightGrams);
        Assert.Equal(765.4m, Assert.Single(await _app.Gear.ListGearItemsAsync()).WeightGrams);
        Assert.Equal(existing.Id, flagged.GearItem.Id);
    }

    [Theory]
    [InlineData("Shelter,Tent,Tarptent Notch,765.4,,")]
    [InlineData("Shelter,Tarp,Durston X-Mid 1,765.4,,")]
    public async Task Gear_type_and_name_must_both_match(string row)
    {
        await AddExistingTentAsync();

        var summary = await _app.Import.ImportCsvAsync(Csv(row));

        Assert.Single(summary.Created);
        Assert.Equal(2, (await _app.Gear.ListGearItemsAsync()).Count);
    }

    [Fact]
    public async Task A_row_matching_retired_gear_reuses_it_and_leaves_it_retired()
    {
        var existing = await AddExistingTentAsync();
        await _app.Gear.RetireGearItemAsync(existing.Id);

        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,Tent,Durston X-Mid 1,765.4,,"));

        Assert.Equal(existing.Id, Assert.Single(summary.Reused).GearItem!.Id);
        Assert.Empty(await _app.Gear.ListGearItemsAsync());
        Assert.Single(await _app.Gear.ListRetiredGearItemsAsync());
    }

    [Fact]
    public async Task A_gear_item_listed_twice_in_one_file_is_created_once()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv(
            "Shelter,Stakes,MSR Groundhog,10,6,",
            "Shelter,stakes,msr groundhog,10,2,"));

        Assert.Equal([2], summary.Created.Select(row => row.RowNumber));
        Assert.Equal([3], summary.Reused.Select(row => row.RowNumber));
        Assert.Single(await _app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task A_row_with_a_gear_type_but_no_name_uses_the_gear_type_as_its_name()
    {
        await _app.Import.ImportCsvAsync(Csv("Misc,Mirror,,16,,"));

        var mirror = Assert.Single(await _app.Gear.ListGearItemsAsync());
        Assert.Equal(("Mirror", "Mirror"), (mirror.Name, mirror.GearType));
    }

    [Fact]
    public async Task Blank_qty_means_one_and_blank_carry_mode_means_base()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,Tent,Durston X-Mid 1,765,,"));

        var row = Assert.Single(summary.Created);
        Assert.Equal(1, row.Quantity);
        Assert.Equal(CarryMode.Base, row.CarryMode);
    }

    [Theory]
    [InlineData("worn", CarryMode.Worn)]
    [InlineData(" Consumable ", CarryMode.Consumable)]
    [InlineData("BASE", CarryMode.Base)]
    public async Task Carry_mode_is_read_ignoring_case(string text, CarryMode expected)
    {
        var summary = await _app.Import.ImportCsvAsync(Csv($"Clothing,Puffy,OV Vario,283,2,{text}"));

        var row = Assert.Single(summary.Created);
        Assert.Equal(2, row.Quantity);
        Assert.Equal(expected, row.CarryMode);
    }

    [Theory]
    [InlineData("Shelter,,,765,,", "Name is missing.")]
    [InlineData("Shelter,Tent,Durston,,,", "Weight (g) is missing.")]
    [InlineData("Shelter,Tent,Durston,abc,,", "Weight (g) \"abc\" isn't a number.")]
    [InlineData("Shelter,Tent,Durston,-5,,", "Weight (g) can't be negative.")]
    [InlineData("Shelter,Tent,Durston,0,,", "Weight (g) is 0.")]
    [InlineData("Shelter,Tent,Durston,765,0,", "Qty \"0\" isn't a whole number of 1 or more.")]
    [InlineData("Shelter,Tent,Durston,765,1.5,", "Qty \"1.5\" isn't a whole number of 1 or more.")]
    [InlineData("Shelter,Tent,Durston,765,,packed", "Carry Mode \"packed\" isn't base, worn or consumable.")]
    [InlineData("Shelter,Tent,Durston,765,,1", "Carry Mode \"1\" isn't base, worn or consumable.")]
    public async Task An_invalid_row_is_rejected_with_its_row_number_and_reason(string row, string reason)
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Sleep,Quilt,EE Revelation,590,,", row, "Cook,Stove,BRS,25,,"));

        var rejected = Assert.Single(summary.Rejected);
        Assert.Equal(3, rejected.RowNumber);
        Assert.Equal(reason, rejected.Reason);
        Assert.Equal(["EE Revelation", "BRS"], (await _app.Gear.ListGearItemsAsync()).Select(item => item.Name));
    }

    [Fact]
    public async Task Blank_rows_are_skipped_but_still_count_for_row_numbers()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Sleep,Quilt,EE,590,,", ",,,,,", "", "Cook,Stove,BRS,abc,,"));

        Assert.Single(summary.Created);
        Assert.Equal(5, Assert.Single(summary.Rejected).RowNumber);
        Assert.Equal(2, summary.Rows.Count);
    }

    [Fact]
    public async Task Quoted_fields_can_hold_commas_and_quotes()
    {
        await _app.Import.ImportCsvAsync(Csv("Shelter,\"Tent, 1-person\",\"Durston \"\"X-Mid\"\" 1\",765,,"));

        var tent = Assert.Single(await _app.Gear.ListGearItemsAsync());
        Assert.Equal("Tent, 1-person", tent.GearType);
        Assert.Equal("Durston \"X-Mid\" 1", tent.Name);
    }

    [Fact]
    public async Task A_file_saved_by_excel_as_csv_utf8_imports()
    {
        // Excel's "CSV UTF-8" starts with a byte-order mark and ends lines with \r\n.
        var csv = (char)0xFEFF + Header + "\r\nShelter,Tent,Durston,765,,\r\nSleep,Quilt,EE,590,,\r\n";

        var summary = await _app.Import.ImportCsvAsync(csv);

        Assert.Equal(2, summary.Created.Count);
        Assert.Empty(summary.Rejected);
    }

    [Fact]
    public async Task Rows_missing_their_last_empty_columns_still_import()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,Tent,Durston,765"));

        var row = Assert.Single(summary.Created);
        Assert.Equal(1, row.Quantity);
        Assert.Equal(CarryMode.Base, row.CarryMode);
    }

    [Fact]
    public async Task A_header_with_no_rows_imports_nothing()
    {
        var summary = await _app.Import.ImportCsvAsync(Header + "\n");

        Assert.Empty(summary.Rows);
    }

    [Fact]
    public async Task A_weight_with_a_thousands_separator_is_read()
    {
        // Excel saves a cell formatted as 1,234 that way, quoted.
        await _app.Import.ImportCsvAsync(Csv("Pack,Backpack,Gossamer Gear,\"1,234.5\",,"));

        Assert.Equal(1234.5m, Assert.Single(await _app.Gear.ListGearItemsAsync()).WeightGrams);
    }

    [Fact]
    public async Task A_file_saved_in_the_wrong_text_encoding_is_refused()
    {
        // Excel's plain "CSV" isn't UTF-8; read as UTF-8, letters like é become U+FFFD.
        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportCsvAsync(Csv($"Cook,Caf{(char)0xFFFD} press,Bodum,200,,")));

        Assert.Equal(
            "Some letters in this file can't be read. In Excel, save it with Save As, \"CSV UTF-8\", and import that.",
            refused.Message);
        Assert.Empty(await _app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task The_header_may_have_empty_columns_after_the_six()
    {
        var summary = await _app.Import.ImportCsvAsync(Header + ",,\nShelter,Tent,Durston,765,,,,");

        Assert.Single(summary.Created);
    }

    [Fact]
    public async Task A_header_with_extra_named_columns_is_refused()
    {
        await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportCsvAsync(Header + ",Price\nShelter,Tent,Durston,765,,,299"));
    }

    [Fact]
    public async Task The_header_is_matched_ignoring_case_and_spaces()
    {
        var summary = await _app.Import.ImportCsvAsync(
            " category , GEARTYPE ,name,weight (G), qty ,Carry  mode\nShelter,Tent,Durston,765,,");

        Assert.Single(summary.Created);
    }
}
