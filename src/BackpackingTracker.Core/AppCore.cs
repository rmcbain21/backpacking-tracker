using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Import;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Preferences;

namespace BackpackingTracker.Core;

/// <summary>
/// The application-level API the UI uses. Each area of the app hangs off it.
/// </summary>
public sealed class AppCore
{
    public AppCore(IRecordStore store, IClock clock)
    {
        Gear = new GearCatalogue(store, clock);
        Settings = new UserSettings(store, clock);
        Import = new GearImport(Gear);
    }

    public GearCatalogue Gear { get; }

    public UserSettings Settings { get; }

    public GearImport Import { get; }
}
