using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class CategoryTests
{
    private static readonly string[] DefaultNames =
        ["Shelter", "Sleep", "Pack", "Cook", "Water", "Clothing", "Electronics", "Hygiene", "First Aid", "Misc"];

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static async Task<string[]> CategoryNamesAsync(AppCore app) =>
        (await app.Gear.ListCategoriesAsync()).Select(category => category.Name).ToArray();

    [Fact]
    public async Task On_first_run_the_default_categories_exist_in_order()
    {
        var app = CreateApp();

        Assert.Equal(DefaultNames, await CategoryNamesAsync(app));
    }

    [Fact]
    public async Task The_default_categories_are_only_created_once()
    {
        await CategoryNamesAsync(CreateApp());
        await CategoryNamesAsync(CreateApp());

        Assert.Equal(DefaultNames, await CategoryNamesAsync(CreateApp()));
    }

    [Fact]
    public async Task Every_device_creates_the_default_categories_with_the_same_ids()
    {
        var phone = new AppCore(new InMemoryRecordStore(), _clock);
        var tablet = new AppCore(new InMemoryRecordStore(), _clock);

        var phoneIds = (await phone.Gear.ListCategoriesAsync()).Select(category => category.Id);
        var tabletIds = (await tablet.Gear.ListCategoriesAsync()).Select(category => category.Id);

        Assert.Equal(phoneIds, tabletIds);
        Assert.All(phoneIds, id => Assert.True(Guid.TryParse(id, out _)));
    }

    [Fact]
    public async Task An_added_category_goes_at_the_end()
    {
        var app = CreateApp();

        var added = await app.Gear.AddCategoryAsync("  Big Three ");

        Assert.Equal("Big Three", added.Name);
        string[] expected = [.. DefaultNames, "Big Three"];
        Assert.Equal(expected, await CategoryNamesAsync(app));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task A_category_needs_a_name(string name)
    {
        var app = CreateApp();

        await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.AddCategoryAsync(name));

        Assert.Equal(DefaultNames, await CategoryNamesAsync(app));
    }

    [Fact]
    public async Task Two_categories_cannot_share_a_name()
    {
        var app = CreateApp();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.AddCategoryAsync("shelter"));

        Assert.Equal("There is already a Category called Shelter.", refused.Message);
        Assert.Equal(DefaultNames, await CategoryNamesAsync(app));
    }

    private static async Task<Category> CategoryNamedAsync(AppCore app, string name) =>
        (await app.Gear.ListCategoriesAsync()).Single(category => category.Name == name);

    [Fact]
    public async Task A_renamed_category_keeps_its_place_and_is_stamped_with_the_current_time()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");
        var later = new DateTimeOffset(2026, 5, 2, 8, 0, 0, TimeSpan.Zero);
        _clock.UtcNow = later;

        await app.Gear.RenameCategoryAsync(shelter.Id, " Big Three ");

        Assert.Equal("Big Three", (await CategoryNamesAsync(CreateApp()))[0]);
        Assert.Equal(later, (await CategoryNamedAsync(app, "Big Three")).UpdatedAt);
    }

    [Fact]
    public async Task A_category_can_be_renamed_to_a_different_capitalisation_of_its_own_name()
    {
        var app = CreateApp();
        var firstAid = await CategoryNamedAsync(app, "First Aid");

        await app.Gear.RenameCategoryAsync(firstAid.Id, "First aid");

        Assert.Contains("First aid", await CategoryNamesAsync(app));
    }

    [Theory]
    [InlineData("Sleep", "There is already a Category called Sleep.")]
    [InlineData(" ", "Give the Category a name.")]
    public async Task Renaming_refuses_a_blank_or_taken_name(string newName, string expectedMessage)
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.RenameCategoryAsync(shelter.Id, newName));

        Assert.Equal(expectedMessage, refused.Message);
        Assert.Equal(DefaultNames, await CategoryNamesAsync(app));
    }

    [Fact]
    public async Task A_category_can_be_moved_to_a_new_position()
    {
        var app = CreateApp();
        var water = await CategoryNamedAsync(app, "Water");

        await app.Gear.MoveCategoryAsync(water.Id, 0);

        Assert.Equal(
            ["Water", "Shelter", "Sleep", "Pack", "Cook", "Clothing", "Electronics", "Hygiene", "First Aid", "Misc"],
            await CategoryNamesAsync(CreateApp()));
    }

    [Fact]
    public async Task A_category_can_be_moved_down_one_place()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");

        await app.Gear.MoveCategoryAsync(shelter.Id, 1);

        Assert.Equal(["Sleep", "Shelter", "Pack"], (await CategoryNamesAsync(app))[..3]);
    }

    [Fact]
    public async Task Moving_above_the_top_stops_at_the_top()
    {
        var app = CreateApp();
        var cook = await CategoryNamedAsync(app, "Cook");

        await app.Gear.MoveCategoryAsync(cook.Id, -1);

        Assert.Equal(["Cook", "Shelter", "Sleep"], (await CategoryNamesAsync(app))[..3]);
    }

    [Fact]
    public async Task Moving_below_the_bottom_stops_at_the_bottom()
    {
        var app = CreateApp();
        var cook = await CategoryNamedAsync(app, "Cook");

        await app.Gear.MoveCategoryAsync(cook.Id, 99);

        Assert.Equal(["First Aid", "Misc", "Cook"], (await CategoryNamesAsync(app))[^3..]);
    }

    private static Task AddGearItemAsync(AppCore app, string name, Category category) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, "", 100m, category.Id));

    [Fact]
    public async Task An_unused_category_can_be_removed()
    {
        var app = CreateApp();
        var hygiene = await CategoryNamedAsync(app, "Hygiene");

        await app.Gear.RemoveCategoryAsync(hygiene.Id);

        Assert.DoesNotContain("Hygiene", await CategoryNamesAsync(CreateApp()));
    }

    [Fact]
    public async Task Removed_default_categories_do_not_come_back()
    {
        var app = CreateApp();
        foreach (var category in (await app.Gear.ListCategoriesAsync()).Where(c => c.Name != "Misc"))
        {
            await app.Gear.RemoveCategoryAsync(category.Id);
        }

        Assert.Equal(["Misc"], await CategoryNamesAsync(CreateApp()));
    }

    [Fact]
    public async Task The_last_category_cannot_be_removed()
    {
        var app = CreateApp();
        var categories = await app.Gear.ListCategoriesAsync();
        foreach (var category in categories.Skip(1))
        {
            await app.Gear.RemoveCategoryAsync(category.Id);
        }

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.RemoveCategoryAsync(categories[0].Id));

        Assert.Equal("Keep at least one Category, so Gear Items have somewhere to go.", refused.Message);
        Assert.Equal(["Shelter"], await CategoryNamesAsync(app));
    }

    [Fact]
    public async Task The_default_categories_are_created_as_never_edited()
    {
        // Every device creates the same defaults; an epoch timestamp lets sync tell an
        // untouched default from one the backpacker renamed or moved on another device.
        var categories = await CreateApp().Gear.ListCategoriesAsync();

        Assert.All(categories, category => Assert.Equal(DateTimeOffset.UnixEpoch, category.UpdatedAt));
    }

    [Fact]
    public async Task The_name_of_a_removed_category_can_be_used_again()
    {
        var app = CreateApp();
        await app.Gear.RemoveCategoryAsync((await CategoryNamedAsync(app, "Pack")).Id);

        await app.Gear.AddCategoryAsync("Pack");

        Assert.Equal("Pack", (await CategoryNamesAsync(app))[^1]);
    }

    [Fact]
    public async Task Planning_to_remove_a_used_category_says_where_its_gear_items_can_go()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");
        await AddGearItemAsync(app, "Tent", shelter);
        await AddGearItemAsync(app, "Stakes", shelter);

        var plan = await app.Gear.PlanCategoryRemovalAsync(shelter.Id);

        Assert.True(plan.NeedsMoveTarget);
        Assert.Equal("Shelter has 2 Gear Items. Choose a Category to move them to.", plan.Message);
        Assert.Equal(DefaultNames[1..], plan.MoveTargets.Select(category => category.Name));
    }

    [Fact]
    public async Task Planning_to_remove_a_category_with_one_gear_item_says_so()
    {
        var app = CreateApp();
        var cook = await CategoryNamedAsync(app, "Cook");
        await AddGearItemAsync(app, "Stove", cook);

        var plan = await app.Gear.PlanCategoryRemovalAsync(cook.Id);

        Assert.Equal("Cook has 1 Gear Item. Choose a Category to move it to.", plan.Message);
    }

    [Fact]
    public async Task Planning_to_remove_an_unused_category_just_asks_to_confirm()
    {
        var app = CreateApp();

        var plan = await app.Gear.PlanCategoryRemovalAsync((await CategoryNamedAsync(app, "Sleep")).Id);

        Assert.False(plan.NeedsMoveTarget);
        Assert.Equal("Remove Sleep?", plan.Message);
    }

    [Fact]
    public async Task Removing_a_category_that_gear_items_use_is_refused()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");
        await AddGearItemAsync(app, "Tent", shelter);
        await AddGearItemAsync(app, "Stakes", shelter);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Gear.RemoveCategoryAsync(shelter.Id));

        Assert.Equal("Shelter has 2 Gear Items. Choose a Category to move them to.", refused.Message);
        Assert.Contains("Shelter", await CategoryNamesAsync(app));
    }

    [Fact]
    public async Task Removing_a_used_category_can_move_its_gear_items_to_another()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");
        var misc = await CategoryNamedAsync(app, "Misc");
        await AddGearItemAsync(app, "Tent", shelter);
        await AddGearItemAsync(app, "Stakes", shelter);
        var later = _clock.UtcNow.AddHours(1);
        _clock.UtcNow = later;

        await app.Gear.RemoveCategoryAsync(shelter.Id, moveGearItemsToCategoryId: misc.Id);

        Assert.DoesNotContain("Shelter", await CategoryNamesAsync(app));
        var items = await CreateApp().Gear.ListGearItemsAsync();
        Assert.All(items, item => Assert.Equal(misc.Id, item.CategoryId));
        Assert.All(items, item => Assert.Equal(later, item.UpdatedAt));
    }

    [Fact]
    public async Task Gear_items_cannot_be_moved_into_the_category_being_removed()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");
        await AddGearItemAsync(app, "Tent", shelter);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.RemoveCategoryAsync(shelter.Id, moveGearItemsToCategoryId: shelter.Id));

        Assert.Equal("Choose a different Category to move the Gear Items to.", refused.Message);
        Assert.Contains("Shelter", await CategoryNamesAsync(app));
    }

    [Fact]
    public async Task Gear_items_cannot_be_moved_into_a_category_that_does_not_exist()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");
        await AddGearItemAsync(app, "Tent", shelter);

        await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Gear.RemoveCategoryAsync(shelter.Id, moveGearItemsToCategoryId: "not-a-category"));

        Assert.Contains("Shelter", await CategoryNamesAsync(app));
    }

    [Fact]
    public async Task A_removed_category_cannot_be_chosen_for_a_new_gear_item()
    {
        var app = CreateApp();
        var water = await CategoryNamedAsync(app, "Water");
        await app.Gear.RemoveCategoryAsync(water.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() => AddGearItemAsync(app, "Filter", water));
    }

    [Fact]
    public async Task Gear_items_whose_category_was_removed_elsewhere_are_listed_under_no_category()
    {
        // Sync can bring in a Gear Item whose Category another device removed. The API can't
        // create one, so this test writes it to the store directly.
        var app = CreateApp();
        await AddGearItemAsync(app, "Tent", await CategoryNamedAsync(app, "Shelter"));
        await _store.SaveAsync("gearItems", new GearItem
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = "Toothbrush",
            WeightGrams = 12m,
            CategoryId = "a-category-removed-on-another-device",
        });

        var sections = await app.Gear.ListGearItemsByCategoryAsync();

        Assert.Equal(["Shelter", "No Category"], sections.Select(section => section.Category.Name));
        Assert.Equal(["Toothbrush"], sections[1].GearItems.Select(item => item.Name));
    }

    [Fact]
    public async Task Gear_items_are_grouped_by_category_in_category_order()
    {
        var app = CreateApp();
        var shelter = await CategoryNamedAsync(app, "Shelter");
        var sleep = await CategoryNamedAsync(app, "Sleep");
        var cook = await CategoryNamedAsync(app, "Cook");
        await AddGearItemAsync(app, "Tent", shelter);
        await AddGearItemAsync(app, "Stove", cook);
        await AddGearItemAsync(app, "Quilt", sleep);
        await AddGearItemAsync(app, "Pot", cook);
        await app.Gear.MoveCategoryAsync(cook.Id, 0);

        var sections = await app.Gear.ListGearItemsByCategoryAsync();

        Assert.Equal(["Cook", "Shelter", "Sleep"], sections.Select(section => section.Category.Name));
        Assert.Equal(["Pot", "Stove"], sections[0].GearItems.Select(item => item.Name));
        Assert.Equal(["Tent"], sections[1].GearItems.Select(item => item.Name));
        Assert.Equal(["Quilt"], sections[2].GearItems.Select(item => item.Name));
    }

    [Fact]
    public async Task Moving_a_category_only_restamps_the_categories_whose_position_changed()
    {
        var app = CreateApp();
        var sleep = await CategoryNamedAsync(app, "Sleep");

        await app.Gear.MoveCategoryAsync(sleep.Id, 0);

        var categories = await app.Gear.ListCategoriesAsync();
        Assert.Equal(_clock.UtcNow, categories.Single(c => c.Name == "Sleep").UpdatedAt);
        Assert.Equal(_clock.UtcNow, categories.Single(c => c.Name == "Shelter").UpdatedAt);
        Assert.Equal(DateTimeOffset.UnixEpoch, categories.Single(c => c.Name == "Pack").UpdatedAt);
    }
}
