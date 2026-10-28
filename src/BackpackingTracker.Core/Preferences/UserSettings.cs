using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Preferences;

public sealed class UserSettings
{
    internal const string Table = "settings";

    // There is only ever one Settings record. A fixed id (rather than a new GUID per device)
    // means every device edits the same record, so sync combines it instead of duplicating it.
    private const string SettingsId = "00000000-0000-0000-0000-000000000001";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public UserSettings(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    /// <summary>The saved settings, or the defaults if they have never been changed.</summary>
    public async Task<Settings> GetAsync()
    {
        return await _store.LoadAsync<Settings>(Table, SettingsId)
            ?? new Settings { Id = SettingsId, UpdatedAt = DateTimeOffset.MinValue };
    }

    public Task SetUnitSystemAsync(UnitSystem unitSystem) => ChangeAsync(settings => settings with { UnitSystem = unitSystem });

    public Task SetPaletteAsync(ColourPalette palette) =>
        Enum.IsDefined(palette)
            ? ChangeAsync(settings => settings with { Palette = palette })
            : throw new RuleViolationException("Choose one of the palettes.");

    public Task SetAppearanceAsync(Appearance appearance) =>
        Enum.IsDefined(appearance)
            ? ChangeAsync(settings => settings with { Appearance = appearance })
            : throw new RuleViolationException("Choose Match my device, Light or Dark.");

    public Task SetTextSizeAsync(TextSize textSize) =>
        Enum.IsDefined(textSize)
            ? ChangeAsync(settings => settings with { TextSize = textSize })
            : throw new RuleViolationException("Choose one of the text sizes.");

    private async Task ChangeAsync(Func<Settings, Settings> change)
    {
        var settings = await GetAsync();
        await _store.SaveAsync(Table, change(settings) with { UpdatedAt = _clock.UtcNow });
    }
}
