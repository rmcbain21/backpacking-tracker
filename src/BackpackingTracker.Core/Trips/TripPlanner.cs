using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// Trips, each with its own Packing List copied from a Gear List (or started empty) and then edited on
/// that Trip only. A Planned Trip's weights always come from each Gear Item's current weight.
/// </summary>
public sealed class TripPlanner : IGearItemReferences
{
    internal const string Table = "trips";

    private const string ListKind = "Packing List";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly GearCatalogue _gear;
    private readonly GearLists _lists;

    public TripPlanner(IRecordStore store, IClock clock, GearCatalogue gear, GearLists lists)
    {
        _store = store;
        _clock = clock;
        _gear = gear;
        _lists = lists;
    }

    /// <summary>
    /// Makes a Planned Trip. From a Gear List, its Packing List is a copy of the list's Categories and
    /// entries (each entry with a new id), and nothing links back to the list but its name. Without one,
    /// the Packing List starts with a copy of the Default Categories and no entries.
    /// </summary>
    public async Task<Trip> CreateTripAsync(string name, DateOnly startDate, DateOnly endDate, string? fromGearListId = null)
    {
        var trimmed = string.IsNullOrWhiteSpace(name) ? throw new RuleViolationException("Give the Trip a name.") : name.Trim();
        if (endDate < startDate)
        {
            throw new RuleViolationException("The Trip can't end before it starts.");
        }

        var gearList = fromGearListId is null ? null : await _lists.GetExistingGearListAsync(fromGearListId);
        var trip = new Trip
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = trimmed,
            StartDate = startDate,
            EndDate = endDate,
            CreatedFrom = gearList?.Name,
            PackingList = gearList?.Contents.Copy() ?? new ListContents { Categories = await _lists.CopyDefaultCategoriesAsync() },
        };
        await _store.SaveAsync(Table, trip);
        return trip;
    }

    public async Task<TripListing> ListTripsAsync()
    {
        var trips = await LoadTripsAsync();
        var items = await _gear.GetGearItemsAsync(trips.SelectMany(trip => trip.PackingList.Entries).Select(entry => entry.GearItemId));
        var views = trips.Select(trip => View(trip, items)).ToList();

        return new TripListing(
            views.Where(view => view.Trip.Status == TripStatus.Planned)
                .OrderBy(view => view.Trip.StartDate)
                .ThenBy(view => view.Trip.Name, StringComparer.OrdinalIgnoreCase)
                .ToList(),
            views.Where(view => view.Trip.Status == TripStatus.Completed)
                .OrderByDescending(view => view.Trip.StartDate)
                .ThenBy(view => view.Trip.Name, StringComparer.OrdinalIgnoreCase)
                .ToList());
    }

    /// <summary>A Trip with its Packing List's current weights and totals, or null if there's no such Trip.</summary>
    public async Task<TripView?> GetTripAsync(string tripId)
    {
        var trip = await FindTripAsync(tripId);
        if (trip is null)
        {
            return null;
        }

        var items = await _gear.GetGearItemsAsync(trip.PackingList.Entries.Select(entry => entry.GearItemId));
        return View(trip, items);
    }

    // ----- The Packing List -----

    /// <summary>
    /// Adds the Gear Item into one of the Packing List's Categories, with quantity 1 and Carry Mode base. If it's
    /// already on the list, adds one to that entry's quantity instead. Retired gear can't be added.
    /// </summary>
    public async Task<ListEntry> AddEntryAsync(string tripId, string gearItemId, string categoryId)
    {
        var trip = await GetExistingTripAsync(tripId);
        var (packingList, entry) = trip.PackingList.AddEntry(await _gear.GetGearItemAsync(gearItemId), categoryId, ListKind);
        await SaveAsync(trip with { PackingList = packingList });
        return entry;
    }

    public async Task UpdateEntryAsync(string tripId, string entryId, int quantity, CarryMode carryMode)
    {
        var trip = await GetExistingTripAsync(tripId);
        await SaveAsync(trip with { PackingList = trip.PackingList.UpdateEntry(entryId, quantity, carryMode, ListKind) });
    }

    /// <summary>Moves an entry to another of the Packing List's Categories.</summary>
    public async Task MoveEntryAsync(string tripId, string entryId, string categoryId)
    {
        var trip = await GetExistingTripAsync(tripId);
        await SaveAsync(trip with { PackingList = trip.PackingList.MoveEntry(entryId, categoryId, ListKind) });
    }

    public async Task RemoveEntryAsync(string tripId, string entryId)
    {
        var trip = await GetExistingTripAsync(tripId);
        await SaveAsync(trip with { PackingList = trip.PackingList.RemoveEntry(entryId, ListKind) });
    }

    // ----- Views, loading and saving -----

    private static TripView View(Trip trip, IReadOnlyDictionary<string, GearItem> items)
    {
        var view = trip.PackingList.View(items);
        return new TripView(trip, view.Entries, view.Sections, view.Weight);
    }

    private async Task<IReadOnlyList<Trip>> LoadTripsAsync() =>
        (await _store.ListAsync<Trip>(Table)).Where(trip => !trip.Deleted).ToList();

    private async Task<Trip?> FindTripAsync(string tripId) =>
        (await LoadTripsAsync()).SingleOrDefault(trip => trip.Id == tripId);

    private async Task<Trip> GetExistingTripAsync(string tripId) =>
        await FindTripAsync(tripId) ?? throw new RuleViolationException("That Trip no longer exists.");

    private Task SaveAsync(Trip trip) => _store.SaveAsync(Table, trip with { UpdatedAt = _clock.UtcNow });

    /// <summary>The Trips whose Packing Lists use the Gear Item, by name, each once, so it can't be deleted from under them.</summary>
    public async Task<IReadOnlyList<string>> FindUsesAsync(string gearItemId) =>
        (await LoadTripsAsync())
            .Where(trip => trip.PackingList.Entries.Any(entry => entry.GearItemId == gearItemId))
            .OrderBy(trip => trip.Name, StringComparer.OrdinalIgnoreCase)
            .Select(trip => $"Trip \"{trip.Name}\"")
            .ToList();
}
