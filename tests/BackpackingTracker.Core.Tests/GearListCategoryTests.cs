using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>Each Gear List has its own Categories, copied from the Default Categories when it's made (ADR 0003).</summary>
public class GearListCategoryTests
{
    private static readonly string[] DefaultNames =
        ["Shelter", "Sleep", "Pack", "Cook", "Water", "Clothing", "Electronics", "Hygiene", "First Aid", "Misc"];

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<GearListView> ViewAsync(AppCore app, string listId) => (await app.Lists.GetGearListAsync(listId))!;

    private static async Task<string[]> CategoryNamesAsync(AppCore app, string listId) =>
        (await ViewAsync(app, listId)).GearList.Categories.Select(category => category.Name).ToArray();

    private static async Task<Category> CategoryNamedAsync(AppCore app, string listId, string name) =>
        (await ViewAsync(app, listId)).GearList.Categories.Single(category => category.Name == name);

    private static Task<GearItem> AddGearAsync(AppCore app, string name, decimal grams) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, null, grams));

    [Fact]
    public async Task A_new_gear_list_starts_with_a_copy_of_the_default_categories()
    {
        var app = CreateApp();

        var list = await app.Lists.CreateGearListAsync("Summer kit");

        Assert.Equal(DefaultNames, await CategoryNamesAsync(CreateApp(), list.Id));
    }

    [Fact]
    public async Task Renaming_a_category_on_one_list_changes_no_other_list_and_not_the_defaults()
    {
        var app = CreateApp();
        var summer = await app.Lists.CreateGearListAsync("Summer kit");
        var winter = await app.Lists.CreateGearListAsync("Winter kit");

        await app.Lists.RenameCategoryAsync(summer.Id, (await CategoryNamedAsync(app, summer.Id, "Shelter")).Id, "Big 5");

        Assert.Equal("Big 5", (await CategoryNamesAsync(app, summer.Id))[0]);
        Assert.Equal("Shelter", (await CategoryNamesAsync(app, winter.Id))[0]);
        Assert.Equal("Shelter", (await app.Gear.ListDefaultCategoriesAsync())[0].Name);
    }

    [Fact]
    public async Task Changing_the_default_categories_later_changes_no_existing_list()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var shelter = (await app.Gear.ListDefaultCategoriesAsync())[0];

        await app.Gear.RenameDefaultCategoryAsync(shelter.Id, "Big Three");
        await app.Gear.AddDefaultCategoryAsync("Food and Water");

        Assert.Equal(DefaultNames, await CategoryNamesAsync(app, list.Id));
        Assert.Equal("Big Three", (await CategoryNamesAsync(app, (await app.Lists.CreateGearListAsync("Next")).Id))[0]);
    }

    [Fact]
    public async Task A_list_can_get_its_own_categories_in_its_own_order()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");

        var big = await app.Lists.AddCategoryAsync(list.Id, " Big Three + Sleeping Pad ");
        await app.Lists.MoveCategoryAsync(list.Id, big.Id, 0);

        var names = await CategoryNamesAsync(CreateApp(), list.Id);
        Assert.Equal("Big Three + Sleeping Pad", names[0]);
        Assert.Equal(11, names.Length);
    }

    [Theory]
    [InlineData("shelter", "There is already a Category called Shelter on this list.")]
    [InlineData("  ", "Give the Category a name.")]
    public async Task A_list_category_needs_a_name_no_other_category_on_the_list_has(string name, string message)
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Lists.AddCategoryAsync(list.Id, name));

        Assert.Equal(message, refused.Message);
    }

    [Fact]
    public async Task Gear_is_added_into_the_chosen_category()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var cook = await CategoryNamedAsync(app, list.Id, "Cook");

        var entry = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Pocket Rocket 2", 76m)).Id, cook.Id);

        Assert.Equal(cook.Id, entry.CategoryId);
    }

    [Fact]
    public async Task Gear_cannot_be_added_into_a_category_the_list_does_not_have()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Lists.AddEntryAsync(list.Id, "unused", "not-a-category"));

        Assert.Equal("Choose a Category on this list.", refused.Message);
    }

    [Fact]
    public async Task Adding_gear_already_on_the_list_into_another_category_adds_to_its_quantity_where_it_is()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var bottle = await AddGearAsync(app, "Igneous 1L", 70m);
        var water = await CategoryNamedAsync(app, list.Id, "Water");
        await app.Lists.AddEntryAsync(list.Id, bottle.Id, water.Id);

        await app.Lists.AddEntryAsync(list.Id, bottle.Id, (await CategoryNamedAsync(app, list.Id, "Cook")).Id);

        var entry = Assert.Single((await ViewAsync(app, list.Id)).Entries);
        Assert.Equal((2, water.Id), (entry.Entry.Quantity, entry.Entry.CategoryId));
    }

    [Fact]
    public async Task An_entry_can_move_to_another_category()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var entry = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Sawyer Squeeze", 108m)).Id,
            (await CategoryNamedAsync(app, list.Id, "Misc")).Id);
        var water = await CategoryNamedAsync(app, list.Id, "Water");

        await app.Lists.MoveEntryAsync(list.Id, entry.Id, water.Id);

        Assert.Equal(water.Id, Assert.Single((await ViewAsync(CreateApp(), list.Id)).Entries).Entry.CategoryId);
    }

    [Fact]
    public async Task Entries_are_shown_under_their_categories_in_list_order_by_name_with_subtotals_including_worn()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var cook = await CategoryNamedAsync(app, list.Id, "Cook");
        var clothing = await CategoryNamedAsync(app, list.Id, "Clothing");
        await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Toaks 750", 115m)).Id, cook.Id);
        await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Pocket Rocket 2", 76m)).Id, cook.Id);
        var jacket = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "OV Tushar", 235m)).Id, clothing.Id);
        await app.Lists.UpdateEntryAsync(list.Id, jacket.Id, 1, CarryMode.Worn);

        var sections = (await ViewAsync(app, list.Id)).Sections;

        Assert.Equal(DefaultNames, sections.Select(section => section.Category.Name));
        var cookSection = sections.Single(section => section.Category.Id == cook.Id);
        Assert.Equal(["Pocket Rocket 2", "Toaks 750"], cookSection.Entries.Select(entry => entry.GearItem!.Name));
        Assert.Equal(191m, cookSection.SubtotalGrams);
        Assert.Equal(235m, sections.Single(section => section.Category.Id == clothing.Id).SubtotalGrams);
        Assert.Empty(sections.Single(section => section.Category.Name == "Shelter").Entries);
    }

    [Fact]
    public async Task The_breakdown_uses_the_list_categories_and_leaves_out_worn_entries()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Example");
        var big = await app.Lists.AddCategoryAsync(list.Id, "Big Three");
        var cook = await CategoryNamedAsync(app, list.Id, "Cook");
        var clothing = await CategoryNamedAsync(app, list.Id, "Clothing");
        await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Tent", 900m)).Id, big.Id);
        await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Stove", 100m)).Id, cook.Id);
        var jacket = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Rain Jacket", 200m)).Id, clothing.Id);
        await app.Lists.UpdateEntryAsync(list.Id, jacket.Id, 1, CarryMode.Worn);

        var lines = (await ViewAsync(app, list.Id)).Weight.ByCategory;

        Assert.Equal([("Cook", 100m), ("Big Three", 900m)], lines.Select(line => (line.Category.Name, line.Grams)));
    }

    [Fact]
    public async Task Removing_a_category_with_entries_asks_where_to_move_them()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var shelter = await CategoryNamedAsync(app, list.Id, "Shelter");
        var misc = await CategoryNamedAsync(app, list.Id, "Misc");
        await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Tent", 900m)).Id, shelter.Id);
        await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Stakes", 60m)).Id, shelter.Id);

        var plan = await app.Lists.PlanCategoryRemovalAsync(list.Id, shelter.Id);
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Lists.RemoveCategoryAsync(list.Id, shelter.Id));
        await app.Lists.RemoveCategoryAsync(list.Id, shelter.Id, moveEntriesToCategoryId: misc.Id);

        Assert.True(plan.NeedsMoveTarget);
        Assert.Equal("Shelter has 2 entries. Choose a Category to move them to.", plan.Message);
        Assert.Equal(plan.Message, refused.Message);
        Assert.DoesNotContain("Shelter", await CategoryNamesAsync(app, list.Id));
        Assert.All((await ViewAsync(app, list.Id)).Entries, entry => Assert.Equal(misc.Id, entry.Entry.CategoryId));
    }

    [Fact]
    public async Task An_empty_category_is_simply_removed_but_the_last_one_stays()
    {
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var categories = (await ViewAsync(app, list.Id)).GearList.Categories;

        foreach (var category in categories.Skip(1))
        {
            Assert.False((await app.Lists.PlanCategoryRemovalAsync(list.Id, category.Id)).NeedsMoveTarget);
            await app.Lists.RemoveCategoryAsync(list.Id, category.Id);
        }

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Lists.RemoveCategoryAsync(list.Id, categories[0].Id));
        Assert.Equal("Keep at least one Category on the list.", refused.Message);
        Assert.Equal(["Shelter"], await CategoryNamesAsync(app, list.Id));
    }

    [Fact]
    public async Task An_entry_whose_category_was_removed_elsewhere_shows_under_no_category()
    {
        // An entry whose Category id isn't on the list (a hand-edited or imported file); the API never makes one.
        var app = CreateApp();
        var list = await app.Lists.CreateGearListAsync("Summer kit");
        var entry = await app.Lists.AddEntryAsync(list.Id, (await AddGearAsync(app, "Tent", 900m)).Id,
            (await CategoryNamedAsync(app, list.Id, "Shelter")).Id);
        var stored = (await ViewAsync(app, list.Id)).GearList;
        await _store.SaveAsync("gearLists", stored with { Entries = [entry with { CategoryId = "removed-elsewhere" }] });

        var sections = (await ViewAsync(app, list.Id)).Sections;

        Assert.Equal("No Category", sections[^1].Category.Name);
        Assert.Equal(["Tent"], sections[^1].Entries.Select(e => e.GearItem!.Name));
    }

    [Fact]
    public async Task A_gear_list_from_before_list_categories_gets_the_defaults_and_its_entries_their_old_category()
    {
        // Gear Lists from ticket 08 had no Categories, and their Gear Items had one; the API can no longer
        // make either, so this test writes them to the store directly.
        var app = CreateApp();
        var defaults = await app.Gear.ListDefaultCategoriesAsync();
        var cook = defaults.Single(category => category.Name == "Cook");
        await _store.SaveAsync("gearItems", new GearItem
        {
            Id = "old-stove", UpdatedAt = _clock.UtcNow, Name = "Stove", WeightGrams = 76m,
            LegacyBrandModel = "Pocket Rocket 2", LegacyCategoryId = cook.Id,
        });
        var added = await AddGearAsync(app, "Sawyer Squeeze", 108m);
        await _store.SaveAsync("gearLists", new GearList
        {
            Id = "old-list", UpdatedAt = _clock.UtcNow, Name = "Summer kit",
            Entries =
            [
                new ListEntry { Id = "e1", GearItemId = "old-stove" },
                new ListEntry { Id = "e2", GearItemId = added.Id },
            ],
        });

        var view = await ViewAsync(CreateApp(), "old-list");

        Assert.Equal(DefaultNames, view.GearList.Categories.Select(category => category.Name));
        Assert.Equal(cook.Id, view.Entries.Single(e => e.Entry.Id == "e1").Entry.CategoryId);
        Assert.Equal(view.GearList.Categories[0].Id, view.Entries.Single(e => e.Entry.Id == "e2").Entry.CategoryId);

        // Once converted, the old list is a list like any other: changing the defaults leaves it alone.
        await app.Gear.RenameDefaultCategoryAsync(cook.Id, "Kitchen");
        await app.Gear.RemoveDefaultCategoryAsync(defaults.Single(category => category.Name == "Misc").Id);
        Assert.Equal(DefaultNames, await CategoryNamesAsync(CreateApp(), "old-list"));
    }
}
