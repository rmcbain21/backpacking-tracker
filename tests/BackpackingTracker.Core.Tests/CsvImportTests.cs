using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class CsvImportTests
{
    private const string Header = "Category,Name,Brand/Model,Weight (g),Qty,Carry Mode";

    private readonly AppCore _app = new(new InMemoryRecordStore(), new FakeClock(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero)));

    private static string Csv(params string[] rows) => string.Join("\n", [Header, .. rows]);

    private async Task<string> CategoryNameOfAsync(GearItem item) =>
        (await _app.Gear.ListCategoriesAsync()).Single(category => category.Id == item.CategoryId).Name;

    private async Task<GearItem> AddExistingTentAsync()
    {
        var sleep = (await _app.Gear.ListCategoriesAsync()).Single(category => category.Name == "Sleep");
        return await _app.Gear.AddGearItemAsync(new GearItemDetails("Tent", "Durston X-Mid 1", 765.4m, sleep.Id, ["solo"]));
    }

    [Fact]
    public async Task Each_row_becomes_a_gear_item_in_its_category()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv(
            "Shelter,Tent,Durston X-Mid 1,765.4,,",
            "Sleep,Quilt,EE Revelation 20,590,,"));

        var items = await _app.Gear.ListGearItemsAsync();
        Assert.Equal(["Quilt", "Tent"], items.Select(item => item.Name));
        var tent = items.Single(item => item.Name == "Tent");
        Assert.Equal("Durston X-Mid 1", tent.BrandModel);
        Assert.Equal(765.4m, tent.WeightGrams);
        Assert.Equal("Shelter", await CategoryNameOfAsync(tent));
        Assert.Equal([2, 3], summary.Created.Select(row => row.RowNumber));
    }

    [Theory]
    [InlineData("Name,Category,Brand/Model,Weight (g),Qty,Carry Mode")]
    [InlineData("Category,Name,Brand/Model,Weight,Qty,Carry Mode")]
    [InlineData("")]
    public async Task A_file_without_the_expected_header_is_refused(string header)
    {
        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportCsvAsync(header + "\nShelter,Tent,,765,,"));

        Assert.Equal(
            "The first row must be the column names: Category, Name, Brand/Model, Weight (g), Qty, Carry Mode.",
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
        Assert.Equal("Tent", tent.Name);
        Assert.Equal("Durston X-Mid 1", tent.BrandModel);
        Assert.Equal(765.4m, tent.WeightGrams);
        Assert.Equal(["solo"], tent.Tags);
        Assert.Equal("Sleep", await CategoryNameOfAsync(tent));
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

    [Fact]
    public async Task Brand_model_is_part_of_the_match()
    {
        await AddExistingTentAsync();

        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,Tent,Tarptent Notch,765.4,,"));

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
    public async Task Unknown_categories_are_created_and_known_ones_matched_ignoring_case()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv(
            "Big Three,Tent,,765,,",
            "big three,Quilt,,590,,",
            "SLEEP,Pad,,400,,"));

        Assert.Equal(["Big Three"], summary.CreatedCategories);
        var items = await _app.Gear.ListGearItemsAsync();
        Assert.Equal("Big Three", await CategoryNameOfAsync(items.Single(item => item.Name == "Quilt")));
        Assert.Equal("Sleep", await CategoryNameOfAsync(items.Single(item => item.Name == "Pad")));
        Assert.Equal("Big Three", (await _app.Gear.ListCategoriesAsync())[^1].Name);
    }

    [Fact]
    public async Task A_row_with_no_category_goes_in_misc()
    {
        await _app.Import.ImportCsvAsync(Csv(",Bandana,,28,,"));

        Assert.Equal("Misc", await CategoryNameOfAsync(Assert.Single(await _app.Gear.ListGearItemsAsync())));
    }

    [Fact]
    public async Task Blank_qty_means_one_and_blank_carry_mode_means_base()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,Tent,,765,,"));

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
        var summary = await _app.Import.ImportCsvAsync(Csv($"Clothing,Puffy,,283,2,{text}"));

        var row = Assert.Single(summary.Created);
        Assert.Equal(2, row.Quantity);
        Assert.Equal(expected, row.CarryMode);
    }

    [Theory]
    [InlineData("Shelter,,Durston,765,,", "Name is missing.")]
    [InlineData("Shelter,Tent,,,,", "Weight (g) is missing.")]
    [InlineData("Shelter,Tent,,abc,,", "Weight (g) \"abc\" isn't a number.")]
    [InlineData("Shelter,Tent,,-5,,", "Weight (g) can't be negative.")]
    [InlineData("Shelter,Tent,,765,0,", "Qty \"0\" isn't a whole number of 1 or more.")]
    [InlineData("Shelter,Tent,,765,1.5,", "Qty \"1.5\" isn't a whole number of 1 or more.")]
    [InlineData("Shelter,Tent,,765,,packed", "Carry Mode \"packed\" isn't base, worn or consumable.")]
    [InlineData("Shelter,Tent,,765,,1", "Carry Mode \"1\" isn't base, worn or consumable.")]
    public async Task An_invalid_row_is_rejected_with_its_row_number_and_reason(string row, string reason)
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Sleep,Quilt,,590,,", row, "Cook,Stove,,25,,"));

        var rejected = Assert.Single(summary.Rejected);
        Assert.Equal(3, rejected.RowNumber);
        Assert.Equal(reason, rejected.Reason);
        Assert.Equal(["Quilt", "Stove"], (await _app.Gear.ListGearItemsAsync()).Select(item => item.Name));
    }

    [Fact]
    public async Task A_rejected_row_does_not_create_its_category()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Kayaking,Paddle,,abc,,"));

        Assert.Empty(summary.CreatedCategories);
        Assert.DoesNotContain("Kayaking", (await _app.Gear.ListCategoriesAsync()).Select(category => category.Name));
    }

    [Fact]
    public async Task Blank_rows_are_skipped_but_still_count_for_row_numbers()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Sleep,Quilt,,590,,", ",,,,,", "", "Cook,Stove,,abc,,"));

        Assert.Single(summary.Created);
        Assert.Equal(5, Assert.Single(summary.Rejected).RowNumber);
        Assert.Equal(2, summary.Rows.Count);
    }

    [Fact]
    public async Task Quoted_fields_can_hold_commas_and_quotes()
    {
        await _app.Import.ImportCsvAsync(Csv("Shelter,\"Tent, 1-person\",\"Durston \"\"X-Mid\"\" 1\",765,,"));

        var tent = Assert.Single(await _app.Gear.ListGearItemsAsync());
        Assert.Equal("Tent, 1-person", tent.Name);
        Assert.Equal("Durston \"X-Mid\" 1", tent.BrandModel);
    }

    [Fact]
    public async Task A_file_saved_by_excel_as_csv_utf8_imports()
    {
        // Excel's "CSV UTF-8" starts with a byte-order mark and ends lines with \r\n.
        var csv = "﻿" + Header + "\r\nShelter,Tent,,765,,\r\nSleep,Quilt,,590,,\r\n";

        var summary = await _app.Import.ImportCsvAsync(csv);

        Assert.Equal(2, summary.Created.Count);
        Assert.Empty(summary.Rejected);
    }

    [Fact]
    public async Task Rows_missing_their_last_empty_columns_still_import()
    {
        var summary = await _app.Import.ImportCsvAsync(Csv("Shelter,Tent,,765"));

        var row = Assert.Single(summary.Created);
        Assert.Equal(1, row.Quantity);
        Assert.Equal(CarryMode.Base, row.CarryMode);
    }

    [Fact]
    public async Task A_header_with_no_rows_imports_nothing()
    {
        var summary = await _app.Import.ImportCsvAsync(Header + "\n");

        Assert.Empty(summary.Rows);
        Assert.Empty(summary.CreatedCategories);
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
            () => _app.Import.ImportCsvAsync(Csv("Cook,Caf� press,,200,,")));

        Assert.Equal(
            "Some letters in this file can't be read. In Excel, save it with Save As, \"CSV UTF-8\", and import that.",
            refused.Message);
        Assert.Empty(await _app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task A_row_with_no_category_goes_in_misc_even_after_misc_is_renamed()
    {
        var misc = (await _app.Gear.ListCategoriesAsync()).Single(category => category.Name == "Misc");
        await _app.Gear.RenameCategoryAsync(misc.Id, "Other");

        var summary = await _app.Import.ImportCsvAsync(Csv(",Bandana,,28,,"));

        Assert.Equal("Other", await CategoryNameOfAsync(Assert.Single(await _app.Gear.ListGearItemsAsync())));
        Assert.Empty(summary.CreatedCategories);
    }

    [Fact]
    public async Task The_header_may_have_empty_columns_after_the_six()
    {
        var summary = await _app.Import.ImportCsvAsync(Header + ",,\nShelter,Tent,,765,,,,");

        Assert.Single(summary.Created);
    }

    [Fact]
    public async Task A_header_with_extra_named_columns_is_refused()
    {
        await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportCsvAsync(Header + ",Price\nShelter,Tent,,765,,,299"));
    }

    [Fact]
    public async Task The_header_ignores_spaces_inside_column_names()
    {
        var summary = await _app.Import.ImportCsvAsync(
            "Category,Name,Brand / Model,Weight(g),Qty,CarryMode\nShelter,Tent,,765,,");

        Assert.Single(summary.Created);
    }

    [Fact]
    public async Task The_header_is_matched_ignoring_case_and_spaces()
    {
        var summary = await _app.Import.ImportCsvAsync(
            " category , NAME ,brand/model,weight (G), qty ,CARRY MODE\nShelter,Tent,,765,,");

        Assert.Single(summary.Created);
    }
}
