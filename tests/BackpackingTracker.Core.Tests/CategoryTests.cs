using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>The Default Categories: the backpacker's usual set, which each new Gear List starts with (ADR 0003).</summary>
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

        Assert.Equal("Keep at least one Category.", refused.Message);
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
