using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Preferences;

public sealed class UserSettings
{
    private const string Table = "settings";

    // There is only ever one Settings record. A fixed id (rather than a new GUID per device)
    // means every device edits the same record, so sync merges it instead of duplicating it.
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

    public async Task SetUnitSystemAsync(UnitSystem unitSystem)
    {
        var settings = await GetAsync();
        await _store.SaveAsync(Table, settings with { UnitSystem = unitSystem, UpdatedAt = _clock.UtcNow });
    }
}
