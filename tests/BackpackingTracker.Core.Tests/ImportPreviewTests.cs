using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Import;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>
/// The import's preview, the Gear List it can create from the sheet's sections, adding the sections to the
/// Default Categories, and saving all at once.
/// </summary>
public class ImportPreviewTests
{
    private const string SheetHeader = "Category,Item,Name,g,Lb.,oz";
    private const string GearHeader = "Category,Gear Type,Name,Weight (g),Qty,Carry Mode";

    private readonly InMemoryRecordStore _store = new();
    private readonly AppCore _app;

    public ImportPreviewTests()
    {
        _app = new AppCore(_store, new FakeClock(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero)));
    }

    private static string Sheet(params string[] rows) => string.Join("\r\n", [SheetHeader, .. rows]);

    private static string GearCsv(params string[] rows) => string.Join("\n", [GearHeader, .. rows]);

    private static readonly string TripSheet = Sheet(
        "Big Three,,,,,",
        ",Backpack,SWD Long Haul 50,1361,3,48",
        ",Tent,Durston X-Mid 1,765,1.7,27",
        ",,Total:,2126,4.7,75",
        "Water,,,,,",
        ",Filter,Sawyer Squeeze,108,0.2,3.8",
        ",Bottle,Smartwater 1L,38,0.1,1.3",
        ",Bottle,Smartwater 1L,38,0.1,1.3");

    private async Task<GearList> ImportWithGearListAsync(string csv, string name = "Summer")
    {
        var preview = await _app.Import.PreviewCsvAsync(csv);
        return (await _app.Import.ImportAsync(preview, new ImportOptions(GearListName: name)))!;
    }

    /// <summary>The list's Categories, each with its entries as (Name, Quantity, Carry Mode) by Name.</summary>
    private async Task<(string Category, (string Name, int Quantity, CarryMode CarryMode)[] Entries)[]> SectionsOfAsync(string gearListId)
    {
        var view = await _app.Lists.GetGearListAsync(gearListId) ?? throw new InvalidOperationException("No such Gear List.");
        return view.Sections
            .Select(section => (section.Category.Name,
                section.Entries.Select(e => (e.GearItem!.Name, e.Entry.Quantity, e.Entry.CarryMode)).ToArray()))
            .ToArray();
    }

    private async Task<IReadOnlyList<string>> DefaultCategoryNamesAsync() =>
        (await _app.Gear.ListDefaultCategoriesAsync()).Select(category => category.Name).ToList();

    [Fact]
    public async Task The_preview_saves_nothing()
    {
        var defaults = await DefaultCategoryNamesAsync();

        var preview = await _app.Import.PreviewCsvAsync(TripSheet);

        Assert.Equal(4, preview.Created.Count);
        Assert.Empty(await _app.Gear.ListGearItemsAsync());
        Assert.Empty(await _app.Lists.ListGearListsAsync());
        Assert.Equal(defaults, await DefaultCategoryNamesAsync());
    }

    [Fact]
    public async Task The_preview_shows_the_gear_list_sections_and_their_entries()
    {
        var preview = await _app.Import.PreviewCsvAsync(TripSheet);

        Assert.Equal(
            [("Big Three", new[] { ("SWD Long Haul 50", 1), ("Durston X-Mid 1", 1) }), ("Water", new[] { ("Sawyer Squeeze", 1), ("Smartwater 1L", 2) })],
            preview.Sections.Select(s => (s.Name, s.Entries.Select(e => (e.GearItem.Name, e.Quantity)).ToArray())));
        Assert.True(preview.CanCreateGearList);
    }

    [Fact]
    public async Task Importing_saves_what_the_preview_showed()
    {
        var preview = await _app.Import.PreviewCsvAsync(TripSheet);

        await _app.Import.ImportAsync(preview, new ImportOptions());

        Assert.Equal(
            preview.Created.Select(row => row.GearItem!.Id).Order(),
            (await _app.Gear.ListGearItemsAsync()).Select(item => item.Id).Order());
    }

    [Fact]
    public async Task The_gear_list_has_the_sheet_sections_as_its_categories_in_sheet_order()
    {
        var list = await ImportWithGearListAsync(TripSheet);

        Assert.Equal(
            [
                ("Big Three", [("Durston X-Mid 1", 1, CarryMode.Base), ("SWD Long Haul 50", 1, CarryMode.Base)]),
                ("Water", [("Sawyer Squeeze", 1, CarryMode.Base), ("Smartwater 1L", 2, CarryMode.Base)]),
            ],
            await SectionsOfAsync(list.Id));
        Assert.Equal("Summer", Assert.Single(await _app.Lists.ListGearListsAsync()).GearList.Name);
    }

    [Fact]
    public async Task The_gear_list_name_is_trimmed_and_must_not_be_blank()
    {
        var preview = await _app.Import.PreviewCsvAsync(TripSheet);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportAsync(preview, new ImportOptions(GearListName: "  ")));
        var list = await _app.Import.ImportAsync(preview, new ImportOptions(GearListName: " Summer 2026 "));

        Assert.Equal("Give the Gear List a name.", refused.Message);
        Assert.Equal("Summer 2026", list!.Name);
    }

    [Fact]
    public async Task Repeated_rows_in_a_section_add_up_and_keep_the_first_rows_carry_mode()
    {
        var list = await ImportWithGearListAsync(GearCsv(
            "Shelter,Stakes,MSR Groundhog,10,4,",
            "Shelter,stakes,msr groundhog,10,2,worn"));

        Assert.Equal([("Shelter", new[] { ("MSR Groundhog", 6, CarryMode.Base) })], await SectionsOfAsync(list.Id));
    }

    [Fact]
    public async Task The_same_gear_item_in_two_sections_is_an_entry_in_each()
    {
        var list = await ImportWithGearListAsync(Sheet(
            "Clothing,,,,,",
            ",Bandana,Buff,28,0,1",
            "Misc,,,,,",
            ",Bandana,Buff,28,0,1"));

        Assert.Equal(
            [("Clothing", new[] { ("Buff", 1, CarryMode.Base) }), ("Misc", new[] { ("Buff", 1, CarryMode.Base) })],
            await SectionsOfAsync(list.Id));
        Assert.Single(await _app.Gear.ListGearItemsAsync());
    }

    [Fact]
    public async Task Qty_and_carry_mode_come_from_the_row_when_the_layout_has_them()
    {
        var list = await ImportWithGearListAsync(GearCsv(
            "Clothing,Puffy,OV Vario,283,1,worn",
            "Food,Bar,Clif Bar,68,6,consumable"));

        Assert.Equal(
            [("Clothing", new[] { ("OV Vario", 1, CarryMode.Worn) }), ("Food", new[] { ("Clif Bar", 6, CarryMode.Consumable) })],
            await SectionsOfAsync(list.Id));
    }

    [Fact]
    public async Task Reused_and_flagged_rows_put_the_existing_gear_item_on_the_list()
    {
        var tent = await _app.Gear.AddGearItemAsync(new GearItemDetails("Durston X-Mid 1", "Tent", 790m));
        var filter = await _app.Gear.AddGearItemAsync(new GearItemDetails("Sawyer Squeeze", "Filter", 108m));

        var list = await ImportWithGearListAsync(TripSheet);

        var entryItems = list.Entries.Select(entry => entry.GearItemId).ToList();
        Assert.Contains(tent.Id, entryItems);
        Assert.Contains(filter.Id, entryItems);
        Assert.Equal(790m, (await _app.Gear.GetGearItemAsync(tent.Id))!.WeightGrams);
    }

    [Fact]
    public async Task Rows_with_no_section_go_under_other()
    {
        var list = await ImportWithGearListAsync(GearCsv(
            ",Bandana,Buff,28,,",
            "Shelter,Tent,Durston X-Mid 1,765,,"));

        Assert.Equal(
            [(GearImport.NoSectionName, new[] { ("Buff", 1, CarryMode.Base) }), ("Shelter", new[] { ("Durston X-Mid 1", 1, CarryMode.Base) })],
            await SectionsOfAsync(list.Id));
    }

    [Fact]
    public async Task A_section_whose_rows_were_all_rejected_is_still_a_category()
    {
        var list = await ImportWithGearListAsync(Sheet(
            "Shelter,,,,,",
            ",Tent,Durston X-Mid 1,765,1.7,27",
            "Electronics,,,,,",
            ",Lamp,Biolite Alpenglow,,0,0"));

        Assert.Equal(
            [("Shelter", new[] { ("Durston X-Mid 1", 1, CarryMode.Base) }), ("Electronics", Array.Empty<(string, int, CarryMode)>())],
            await SectionsOfAsync(list.Id));
    }

    [Fact]
    public async Task A_file_with_no_gear_cant_make_a_gear_list()
    {
        var preview = await _app.Import.PreviewCsvAsync(Sheet());

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportAsync(preview, new ImportOptions(GearListName: "Summer")));

        Assert.False(preview.CanCreateGearList);
        Assert.Equal("There's no gear in this file to make a Gear List from.", refused.Message);
        Assert.Empty(await _app.Lists.ListGearListsAsync());
    }

    [Fact]
    public async Task Sections_can_be_added_to_the_default_categories_after_the_existing_ones()
    {
        var defaults = await DefaultCategoryNamesAsync();
        var preview = await _app.Import.PreviewCsvAsync(Sheet(
            "Big Three,,,,,",
            ",Tent,Durston X-Mid 1,765,1.7,27",
            "water,,,,,",
            ",Filter,Sawyer Squeeze,108,0.2,3.8",
            "Toiletry,,,,,",
            ",Toothbrush,Travel toothbrush,12,0,0.4"));

        await _app.Import.ImportAsync(preview, new ImportOptions(AddSectionsToDefaultCategories: true));

        Assert.Equal(["Big Three", "Toiletry"], preview.NewDefaultCategoryNames);
        Assert.Equal([.. defaults, "Big Three", "Toiletry"], await DefaultCategoryNamesAsync());
    }

    [Fact]
    public async Task Rows_with_no_section_add_no_default_category()
    {
        var preview = await _app.Import.PreviewCsvAsync(GearCsv(",Bandana,Buff,28,,"));

        Assert.Empty(preview.NewDefaultCategoryNames);
    }

    [Fact]
    public async Task Without_the_option_the_default_categories_are_unchanged()
    {
        var defaults = await DefaultCategoryNamesAsync();

        await ImportWithGearListAsync(TripSheet);

        Assert.Equal(defaults, await DefaultCategoryNamesAsync());
    }

    [Fact]
    public async Task If_anything_fails_to_save_nothing_is_saved()
    {
        var defaults = await DefaultCategoryNamesAsync();
        var preview = await _app.Import.PreviewCsvAsync(TripSheet);
        _store.FailSavingWhen = record => record is GearList;

        await Assert.ThrowsAsync<IOException>(() => _app.Import.ImportAsync(
            preview, new ImportOptions(GearListName: "Summer", AddSectionsToDefaultCategories: true)));

        _store.FailSavingWhen = null;
        Assert.Empty(await _app.Gear.ListGearItemsAsync());
        Assert.Empty(await _app.Lists.ListGearListsAsync());
        Assert.Equal(defaults, await DefaultCategoryNamesAsync());
    }

    [Fact]
    public async Task A_preview_cant_be_imported_twice()
    {
        var preview = await _app.Import.PreviewCsvAsync(TripSheet);
        await _app.Import.ImportAsync(preview, new ImportOptions());

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => _app.Import.ImportAsync(preview, new ImportOptions()));

        Assert.Equal("Your gear has changed since this preview. Choose the file again.", refused.Message);
        Assert.Equal(4, (await _app.Gear.ListGearItemsAsync()).Count);
    }

    [Fact]
    public async Task New_gear_items_in_one_file_share_a_gear_type_spelling()
    {
        await _app.Import.ImportCsvAsync(GearCsv(
            "Shelter,Tent,Durston X-Mid 1,765,,",
            "Shelter,tent,Nemo Dragonfly,907,,"));

        Assert.Equal(["Tent", "Tent"], (await _app.Gear.ListGearItemsAsync()).Select(item => item.GearType));
    }
}
