using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core;

/// <summary>
/// The application-level API the UI uses. Each area of the app hangs off it.
/// </summary>
public sealed class AppCore
{
    public AppCore(IRecordStore store, IClock clock)
    {
        Gear = new GearCatalogue(store, clock);
    }

    public GearCatalogue Gear { get; }
}
