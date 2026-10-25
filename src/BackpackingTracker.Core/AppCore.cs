using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Import;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Preferences;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core;

/// <summary>
/// The application-level API the UI uses. Each area of the app hangs off it.
/// </summary>
public sealed class AppCore
{
    /// <param name="cloud">The cloud drive to sync through; null where there is none, such as tests that don't sync.</param>
    public AppCore(IRecordStore store, IClock clock, ICloudFileStore? cloud = null)
    {
        // The areas save through a store that tells automatic sync about each change; sync uses the plain one.
        var watched = new ChangeWatchingRecordStore(store, () => AutoSync.LocalChanged());
        Gear = new GearCatalogue(watched, clock);
        Settings = new UserSettings(watched, clock);
        Lists = new GearLists(watched, clock, Gear);
        Import = new GearImport(watched, Gear, Lists);
        Food = new FoodCatalogue(watched, clock);
        Meals = new MealCatalogue(watched, clock, Food);
        Trips = new TripPlanner(watched, clock, Gear, Lists, Food, Meals);
        Backup = new Backup(watched);
        Sync = new CloudSync(store, clock, cloud, Settings, settled: () => AutoSync.LocalChanged());
        AutoSync = new AutoSync(Sync, clock);

        // Every area that refers to Gear Items takes part in the Gear Item delete check.
        Gear.AddReferences(Lists);
        Gear.AddReferences(Trips);

        // Likewise for Food Items (in Meals and Meal Plans) and Meals (in Meal Plans).
        Food.AddReferences(Meals);
        Food.AddReferences(Trips);
        Meals.AddReferences(Trips);
    }

    public GearCatalogue Gear { get; }

    public UserSettings Settings { get; }

    public GearImport Import { get; }

    public GearLists Lists { get; }

    public TripPlanner Trips { get; }

    public FoodCatalogue Food { get; }

    public MealCatalogue Meals { get; }

    public Backup Backup { get; }

    public CloudSync Sync { get; }

    public AutoSync AutoSync { get; }
}
