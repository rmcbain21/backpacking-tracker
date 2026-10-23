using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class GearSearchTests
{
    private readonly AppCore _app = new(new InMemoryRecordStore(), new FakeClock(DateTimeOffset.UnixEpoch));

    private Task AddAsync(string name, string gearType, params string[] tags) =>
        _app.Gear.AddGearItemAsync(new GearItemDetails(name, gearType, 100m, tags));

    private async Task AddSampleGearAsync()
    {
        await AddAsync("Durston X-Mid 1", "Tent", "winter");
        await AddAsync("EE Revelation 20", "Quilt", "winter", "summer");
        await AddAsync("BRS-3000T", "Stove");
        await AddAsync("BearVault BV450", "Bear Canister", "bear country");
        await AddAsync("Igneous 1L", "Water Bottle");
    }

    private async Task<string[]> FoundNamesAsync(GearFilter filter) =>
        (await _app.Gear.ListGearItemsAsync(filter)).Select(item => item.Name).ToArray();

    [Fact]
    public async Task With_no_filter_every_gear_item_is_listed_by_gear_type()
    {
        await AddSampleGearAsync();

        Assert.Equal(
            ["BearVault BV450", "EE Revelation 20", "BRS-3000T", "Durston X-Mid 1", "Igneous 1L"],
            await FoundNamesAsync(new GearFilter()));
    }

    [Theory]
    [InlineData("DURSTON", "Durston X-Mid 1")]
    [InlineData("revel", "EE Revelation 20")]
    [InlineData("tent", "Durston X-Mid 1")]
    [InlineData("bottle", "Igneous 1L")]
    [InlineData("country", "BearVault BV450")]
    [InlineData("summ", "EE Revelation 20")]
    public async Task Search_matches_part_of_a_name_gear_type_or_tag_ignoring_case(string search, string expected)
    {
        await AddSampleGearAsync();

        Assert.Equal([expected], await FoundNamesAsync(new GearFilter(SearchText: search)));
    }

    [Fact]
    public async Task Search_finds_every_gear_item_with_a_matching_tag()
    {
        await AddSampleGearAsync();

        Assert.Equal(["EE Revelation 20", "Durston X-Mid 1"], await FoundNamesAsync(new GearFilter(SearchText: "winter")));
    }

    [Fact]
    public async Task Every_word_searched_for_must_match_somewhere_on_the_gear_item()
    {
        await AddSampleGearAsync();

        Assert.Equal(["EE Revelation 20"], await FoundNamesAsync(new GearFilter(SearchText: "winter  quilt")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task A_blank_search_lists_everything(string? search)
    {
        await AddSampleGearAsync();

        Assert.Equal(5, (await FoundNamesAsync(new GearFilter(SearchText: search))).Length);
    }

    [Fact]
    public async Task A_search_that_matches_nothing_lists_nothing()
    {
        await AddSampleGearAsync();

        Assert.Empty(await FoundNamesAsync(new GearFilter(SearchText: "kayak")));
    }

    [Theory]
    [InlineData("Water Bottle")]
    [InlineData(" water bottle ")]
    public async Task The_gear_type_filter_lists_only_that_gear_type(string gearType)
    {
        await AddSampleGearAsync();
        await AddAsync("Mazama M!GO 1L", "Water Bottle");

        Assert.Equal(["Igneous 1L", "Mazama M!GO 1L"], await FoundNamesAsync(new GearFilter(GearType: gearType)));
    }

    [Fact]
    public async Task Search_and_the_gear_type_filter_combine()
    {
        await AddSampleGearAsync();

        Assert.Equal(["Durston X-Mid 1"], await FoundNamesAsync(new GearFilter("winter", "Tent")));
        Assert.Empty(await FoundNamesAsync(new GearFilter("stove", "Tent")));
    }

    [Fact]
    public async Task Tags_do_not_change_a_gear_item_weight()
    {
        var tagged = await _app.Gear.AddGearItemAsync(
            new GearItemDetails("OV Vario", "Puffy", 283.5m, ["winter", "bear country", "camp"]));

        Assert.Equal(283.5m, tagged.WeightGrams);
    }

    [Theory]
    [InlineData(null, null, true)]
    [InlineData("  ", "", true)]
    [InlineData("tent", null, false)]
    [InlineData(null, "Tent", false)]
    public void A_filter_with_no_search_text_or_gear_type_is_empty(string? searchText, string? gearType, bool expected)
    {
        Assert.Equal(expected, new GearFilter(searchText, gearType).IsEmpty);
    }
}
