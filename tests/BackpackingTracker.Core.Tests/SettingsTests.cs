using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Preferences;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Tests;

public class SettingsTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    [Fact]
    public async Task Units_are_imperial_by_default()
    {
        var app = CreateApp();

        var settings = await app.Settings.GetAsync();

        Assert.Equal(UnitSystem.Imperial, settings.UnitSystem);
    }

    [Fact]
    public async Task Switching_to_metric_is_remembered_after_the_app_is_reopened()
    {
        await CreateApp().Settings.SetUnitSystemAsync(UnitSystem.Metric);

        var settings = await CreateApp().Settings.GetAsync();

        Assert.Equal(UnitSystem.Metric, settings.UnitSystem);
    }

    [Fact]
    public async Task Switching_back_to_imperial_replaces_the_earlier_choice()
    {
        var app = CreateApp();
        await app.Settings.SetUnitSystemAsync(UnitSystem.Metric);

        await app.Settings.SetUnitSystemAsync(UnitSystem.Imperial);

        Assert.Equal(UnitSystem.Imperial, (await CreateApp().Settings.GetAsync()).UnitSystem);
    }

    [Fact]
    public async Task Changing_settings_stamps_them_with_the_current_time()
    {
        var app = CreateApp();
        var later = new DateTimeOffset(2026, 6, 2, 18, 0, 0, TimeSpan.Zero);
        _clock.UtcNow = later;

        await app.Settings.SetUnitSystemAsync(UnitSystem.Metric);

        Assert.Equal(later, (await app.Settings.GetAsync()).UpdatedAt);
    }

    [Fact]
    public async Task Changing_units_leaves_stored_gear_weights_alone()
    {
        var app = CreateApp();
        await app.Gear.AddGearItemAsync(new GearItemDetails("Durston X-Mid 1", "Tent", 765.4m));

        await app.Settings.SetUnitSystemAsync(UnitSystem.Metric);

        Assert.Equal(765.4m, Assert.Single(await app.Gear.ListGearItemsAsync()).WeightGrams);
    }

    [Fact]
    public async Task The_palette_is_camping_and_appearance_matches_the_device_by_default()
    {
        var settings = await CreateApp().Settings.GetAsync();

        Assert.Equal(ColourPalette.Camping, settings.Palette);
        Assert.Equal(Appearance.MatchDevice, settings.Appearance);
    }

    [Fact]
    public async Task A_chosen_palette_is_remembered_after_the_app_is_reopened()
    {
        await CreateApp().Settings.SetPaletteAsync(ColourPalette.CanyonSandstone);

        Assert.Equal(ColourPalette.CanyonSandstone, (await CreateApp().Settings.GetAsync()).Palette);
    }

    [Fact]
    public async Task A_chosen_appearance_is_remembered_after_the_app_is_reopened()
    {
        await CreateApp().Settings.SetAppearanceAsync(Appearance.Dark);

        Assert.Equal(Appearance.Dark, (await CreateApp().Settings.GetAsync()).Appearance);
    }

    [Fact]
    public async Task Each_setting_changes_without_resetting_the_others()
    {
        var app = CreateApp();
        await app.Settings.SetUnitSystemAsync(UnitSystem.Metric);
        await app.Settings.SetPaletteAsync(ColourPalette.AlpineLake);

        await app.Settings.SetAppearanceAsync(Appearance.Light);

        var settings = await CreateApp().Settings.GetAsync();
        Assert.Equal((UnitSystem.Metric, ColourPalette.AlpineLake, Appearance.Light), (settings.UnitSystem, settings.Palette, settings.Appearance));
    }

    [Fact]
    public async Task An_unknown_palette_or_appearance_is_refused()
    {
        var app = CreateApp();

        var palette = await Assert.ThrowsAsync<RuleViolationException>(() => app.Settings.SetPaletteAsync((ColourPalette)99));
        var appearance = await Assert.ThrowsAsync<RuleViolationException>(() => app.Settings.SetAppearanceAsync((Appearance)99));

        Assert.Equal("Choose one of the palettes.", palette.Message);
        Assert.Equal("Choose Match my device, Light or Dark.", appearance.Message);
        Assert.Equal(ColourPalette.Camping, (await app.Settings.GetAsync()).Palette);
    }
}
