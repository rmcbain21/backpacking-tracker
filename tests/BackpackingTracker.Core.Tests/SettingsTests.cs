using BackpackingTracker.Core.Gear;
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
        var shelter = (await app.Gear.ListCategoriesAsync())[0];
        await app.Gear.AddGearItemAsync(new GearItemDetails("Tent", "Durston X-Mid 1", 765.4m, shelter.Id));

        await app.Settings.SetUnitSystemAsync(UnitSystem.Metric);

        Assert.Equal(765.4m, Assert.Single(await app.Gear.ListGearItemsAsync()).WeightGrams);
    }
}
