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
    public AppCore(IRecordStore store, IClock clock)
    {
        Gear = new GearCatalogue(store, clock);
        Settings = new UserSettings(store, clock);
        Lists = new GearLists(store, clock, Gear);
        Import = new GearImport(store, Gear, Lists);
        Food = new FoodCatalogue(store, clock);
        Meals = new MealCatalogue(store, clock, Food);
        Trips = new TripPlanner(store, clock, Gear, Lists, Food, Meals);
        Backup = new Backup(store);

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
}
