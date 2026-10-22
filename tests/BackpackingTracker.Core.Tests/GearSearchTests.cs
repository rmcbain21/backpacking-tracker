using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearSearchTests
{
    private readonly AppCore _app = new(new InMemoryRecordStore(), new FakeClock(DateTimeOffset.UnixEpoch));

    private async Task<string> CategoryIdAsync(string name) =>
        (await _app.Gear.ListCategoriesAsync()).Single(category => category.Name == name).Id;

    private async Task AddAsync(string name, string brandModel, string category, params string[] tags) =>
        await _app.Gear.AddGearItemAsync(new GearItemDetails(name, brandModel, 100m, await CategoryIdAsync(category), tags));

    private async Task AddSampleGearAsync()
    {
        await AddAsync("Tent", "Durston X-Mid 1", "Shelter", "winter");
        await AddAsync("Quilt", "EE Revelation 20", "Sleep", "winter", "summer");
        await AddAsync("Stove", "BRS-3000T", "Cook");
        await AddAsync("Canister", "BearVault BV450", "Misc", "bear country");
    }

    private async Task<string[]> FoundNamesAsync(GearFilter filter) =>
        (await _app.Gear.ListGearItemsByCategoryAsync(filter))
            .SelectMany(section => section.GearItems)
            .Select(item => item.Name)
            .ToArray();

    [Fact]
    public async Task With_no_filter_every_gear_item_is_listed()
    {
        await AddSampleGearAsync();

        Assert.Equal(["Tent", "Quilt", "Stove", "Canister"], await FoundNamesAsync(new GearFilter()));
    }

    [Theory]
    [InlineData("TENT", "Tent")]
    [InlineData("qui", "Quilt")]
    [InlineData("x-mid", "Tent")]
    [InlineData("revelation", "Quilt")]
    [InlineData("country", "Canister")]
    [InlineData("summ", "Quilt")]
    public async Task Search_matches_part_of_a_name_brand_model_or_tag_ignoring_case(string search, string expected)
    {
        await AddSampleGearAsync();

        Assert.Equal([expected], await FoundNamesAsync(new GearFilter(SearchText: search)));
    }

    [Fact]
    public async Task Search_finds_every_gear_item_with_a_matching_tag()
    {
        await AddSampleGearAsync();

        Assert.Equal(["Tent", "Quilt"], await FoundNamesAsync(new GearFilter(SearchText: "winter")));
    }

    [Fact]
    public async Task Every_word_searched_for_must_match_somewhere_on_the_gear_item()
    {
        await AddSampleGearAsync();

        Assert.Equal(["Quilt"], await FoundNamesAsync(new GearFilter(SearchText: "winter  quilt")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task A_blank_search_lists_everything(string? search)
    {
        await AddSampleGearAsync();

        Assert.Equal(4, (await FoundNamesAsync(new GearFilter(SearchText: search))).Length);
    }

    [Fact]
    public async Task A_search_that_matches_nothing_lists_nothing()
    {
        await AddSampleGearAsync();

        Assert.Empty(await _app.Gear.ListGearItemsByCategoryAsync(new GearFilter(SearchText: "kayak")));
    }

    [Fact]
    public async Task The_category_filter_lists_only_that_category()
    {
        await AddSampleGearAsync();

        var sections = await _app.Gear.ListGearItemsByCategoryAsync(new GearFilter(CategoryId: await CategoryIdAsync("Sleep")));

        Assert.Equal(["Sleep"], sections.Select(section => section.Category.Name));
        Assert.Equal(["Quilt"], sections[0].GearItems.Select(item => item.Name));
    }

    [Fact]
    public async Task Search_and_the_category_filter_combine()
    {
        await AddSampleGearAsync();
        var shelter = await CategoryIdAsync("Shelter");

        Assert.Equal(["Tent"], await FoundNamesAsync(new GearFilter("winter", shelter)));
        Assert.Empty(await FoundNamesAsync(new GearFilter("stove", shelter)));
    }

    [Fact]
    public async Task Tags_do_not_change_a_gear_item_weight()
    {
        var misc = await CategoryIdAsync("Misc");

        var tagged = await _app.Gear.AddGearItemAsync(
            new GearItemDetails("Puffy", "", 283.5m, misc, ["winter", "bear country", "camp"]));

        Assert.Equal(283.5m, tagged.WeightGrams);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("  ", "", true)]
    [InlineData("tent", null, false)]
    [InlineData(null, "some-category", false)]
    public void A_filter_with_no_search_text_or_category_is_empty(string? searchText, string? categoryId, bool expected)
    {
        Assert.Equal(expected, new GearFilter(searchText, categoryId).IsEmpty);
    }
}
