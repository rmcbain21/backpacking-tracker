using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

public class TagTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static Task<GearItem> AddTaggedAsync(AppCore app, string name, params string[] tags) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, null, 100m, tags));

    [Fact]
    public async Task A_gear_item_has_no_tags_unless_given_some()
    {
        var app = CreateApp();

        await app.Gear.AddGearItemAsync(new GearItemDetails("Bandana", null, 28m));

        Assert.Empty(Assert.Single(await app.Gear.ListGearItemsAsync()).Tags);
    }

    [Fact]
    public async Task A_gear_item_keeps_its_tags_after_the_app_is_reopened()
    {
        await AddTaggedAsync(CreateApp(), "Puffy", "winter", "bear country");

        var item = Assert.Single(await CreateApp().Gear.ListGearItemsAsync());

        Assert.Equal(["winter", "bear country"], item.Tags);
    }

    [Fact]
    public async Task Tags_are_trimmed_and_blank_tags_are_dropped()
    {
        var app = CreateApp();

        var item = await AddTaggedAsync(app, "Puffy", "  winter ", "", "   ");

        Assert.Equal(["winter"], item.Tags);
    }

    [Fact]
    public async Task A_tag_appears_only_once_on_a_gear_item_ignoring_case()
    {
        var app = CreateApp();

        var item = await AddTaggedAsync(app, "Puffy", "winter", "Winter", "WINTER ");

        Assert.Equal(["winter"], item.Tags);
    }

    [Fact]
    public async Task A_new_tag_takes_the_spelling_of_a_matching_tag_already_in_use()
    {
        var app = CreateApp();
        await AddTaggedAsync(app, "Puffy", "Bear Country");

        var canister = await AddTaggedAsync(app, "Bear canister", "bear country");

        Assert.Equal(["Bear Country"], canister.Tags);
    }

    [Theory]
    [InlineData("summer", new[] { "winter", "summer" })]
    [InlineData("  summer ", new[] { "winter", "summer" })]
    [InlineData("WINTER", new[] { "winter" })]
    [InlineData("  ", new[] { "winter" })]
    public void Adding_a_tag_while_editing_trims_it_and_skips_blanks_and_repeats(string typed, string[] expected)
    {
        Assert.Equal(expected, TagRules.Add(["winter"], typed));
    }

    [Theory]
    [InlineData(null, new[] { "Bear Country", "desert" })]
    [InlineData("", new[] { "Bear Country", "desert" })]
    [InlineData("DES", new[] { "desert" })]
    [InlineData("country", new[] { "Bear Country" })]
    [InlineData("kayak", new string[0])]
    public void Suggestions_leave_out_chosen_tags_and_match_what_is_typed(string? typed, string[] expected)
    {
        string[] tagsInUse = ["Bear Country", "desert", "winter"];

        Assert.Equal(expected, TagRules.Suggest(tagsInUse, chosen: ["WINTER"], typed));
    }

    [Fact]
    public async Task Tags_in_use_are_suggested_once_each_in_alphabetical_order()
    {
        var app = CreateApp();
        await AddTaggedAsync(app, "Puffy", "winter", "Bear Country");
        await AddTaggedAsync(app, "Bear canister", "bear country", "desert");

        Assert.Equal(["Bear Country", "desert", "winter"], await app.Gear.ListTagsAsync());
    }
}
