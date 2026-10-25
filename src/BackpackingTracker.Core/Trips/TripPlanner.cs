using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// Trips, each with its own Packing List copied from a Gear List (or started empty) and then edited on
/// that Trip only, and one Trip Day per calendar day. A Planned Trip's weights always come from each Gear
/// Item's current weight; a Completed Trip's are frozen, and its Packing List, checklist and dates are locked
/// until it's Reopened. Its Trip Days' route and journal notes, and its water carried, stay editable.
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
        CheckDates(startDate, endDate);

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
        trip = trip with { Days = trip.DaysFor(startDate, endDate) };
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

    // ----- Trip Status, Save as Gear List and delete -----

    /// <summary>
    /// Marks the Trip Completed, copying each Packing List entry's current Gear Item weight onto the entry, so
    /// re-weighing gear later never changes this Trip's numbers. Its Packing List, checklist and dates are then
    /// locked until it's Reopened.
    /// </summary>
    public async Task CompleteTripAsync(string tripId)
    {
        var trip = await GetExistingTripAsync(tripId);
        if (trip.Status == TripStatus.Completed)
        {
            throw new RuleViolationException("This Trip is already Completed.");
        }

        var items = await _gear.GetGearItemsAsync(trip.PackingList.Entries.Select(entry => entry.GearItemId));
        await SaveAsync(trip with { Status = TripStatus.Completed, PackingList = trip.PackingList.Freeze(items) });
    }

    /// <summary>
    /// Sets a Completed Trip back to Planned and clears its frozen weights, so it uses each Gear Item's current
    /// weight again; completing it again takes a fresh freeze.
    /// </summary>
    public async Task ReopenTripAsync(string tripId)
    {
        var trip = await GetExistingTripAsync(tripId);
        if (trip.Status != TripStatus.Completed)
        {
            throw new RuleViolationException("This Trip is already Planned.");
        }

        await SaveAsync(trip with { Status = TripStatus.Planned, PackingList = trip.PackingList.Unfreeze() });
    }

    /// <summary>
    /// A new Gear List with copies of the Packing List's Categories and entries (Gear Item, quantity and Carry
    /// Mode, each with a new id, none packed). Later changes to either don't change the other.
    /// </summary>
    public async Task<GearList> SaveAsGearListAsync(string tripId, string name)
    {
        var trip = await GetExistingTripAsync(tripId);
        var contents = trip.PackingList.Copy();
        if (contents.Categories.Count > 0)
        {
            // An entry whose Category isn't on the list (only from a hand-edited file) goes in the first one.
            var known = contents.Categories.Select(category => category.Id).ToHashSet();
            var first = contents.Categories[0].Id;
            contents = contents with
            {
                Entries = contents.Entries.Select(e => known.Contains(e.CategoryId) ? e : e with { CategoryId = first }).ToList(),
            };
        }

        return await _lists.CreateGearListAsync(name, contents);
    }

    /// <summary>Deletes the Trip, Planned or Completed. Kept as a tombstone so the deletion syncs.</summary>
    public async Task DeleteTripAsync(string tripId)
    {
        var trip = await GetExistingTripAsync(tripId);
        await SaveAsync(trip with { Deleted = true });
    }

    // ----- The Packing List -----

    /// <summary>
    /// Adds the Gear Item into one of the Packing List's Categories, with quantity 1 and Carry Mode base. If it's
    /// already on the list, adds one to that entry's quantity instead. Retired gear can't be added.
    /// </summary>
    public async Task<ListEntry> AddEntryAsync(string tripId, string gearItemId, string categoryId)
    {
        var trip = await GetPlannedTripAsync(tripId, "its Packing List");
        var (packingList, entry) = trip.PackingList.AddEntry(await _gear.GetGearItemAsync(gearItemId), categoryId, ListKind);
        await SaveAsync(trip with { PackingList = packingList });
        return entry;
    }

    public async Task UpdateEntryAsync(string tripId, string entryId, int quantity, CarryMode carryMode)
    {
        var trip = await GetPlannedTripAsync(tripId, "its Packing List");
        await SaveAsync(trip with { PackingList = trip.PackingList.UpdateEntry(entryId, quantity, carryMode, ListKind) });
    }

    /// <summary>Moves an entry to another of the Packing List's Categories.</summary>
    public async Task MoveEntryAsync(string tripId, string entryId, string categoryId)
    {
        var trip = await GetPlannedTripAsync(tripId, "its Packing List");
        await SaveAsync(trip with { PackingList = trip.PackingList.MoveEntry(entryId, categoryId, ListKind) });
    }

    public async Task RemoveEntryAsync(string tripId, string entryId)
    {
        var trip = await GetPlannedTripAsync(tripId, "its Packing List");
        await SaveAsync(trip with { PackingList = trip.PackingList.RemoveEntry(entryId, ListKind) });
    }

    // ----- The packing checklist and water carried -----

    /// <summary>Ticks or unticks a Packing List entry on the packing checklist.</summary>
    public async Task SetPackedAsync(string tripId, string entryId, bool packed)
    {
        var trip = await GetPlannedTripAsync(tripId, "its packing checklist");
        await SaveAsync(trip with { PackingList = trip.PackingList.SetPacked(entryId, packed, ListKind) });
    }

    /// <summary>Unticks every entry on the packing checklist, to check the packing again.</summary>
    public async Task ResetPackedAsync(string tripId)
    {
        var trip = await GetPlannedTripAsync(tripId, "its packing checklist");
        await SaveAsync(trip with { PackingList = trip.PackingList.ResetPacked() });
    }

    /// <summary>Records the water carried from the trailhead, in grams, or clears it with null.</summary>
    public async Task SetWaterCarriedAsync(string tripId, decimal? grams)
    {
        if (grams < 0)
        {
            throw new RuleViolationException("Water carried can't be negative.");
        }

        var trip = await GetExistingTripAsync(tripId);
        await SaveAsync(trip with { WaterCarriedGrams = grams == 0 ? null : grams });
    }

    // ----- Trip Days and dates -----

    /// <summary>Replaces what's typed in on the Trip Day for that date: its route and journal notes.</summary>
    public async Task<TripDay> UpdateDayAsync(string tripId, DateOnly date, TripDayDetails details)
    {
        var trip = await GetExistingTripAsync(tripId);
        var index = trip.Days.ToList().FindIndex(day => day.Date == date);
        if (index < 0)
        {
            throw new RuleViolationException("That day is no longer part of the Trip.");
        }

        var day = trip.Days[index] with
        {
            Start = Trimmed(details.Start),
            End = Trimmed(details.End),
            Campsite = Trimmed(details.Campsite),
            PlannedDistanceMetres = NotNegative(details.PlannedDistanceMetres, "A planned distance"),
            ActualDistanceMetres = NotNegative(details.ActualDistanceMetres, "An actual distance"),
            ElevationGainMetres = NotNegative(details.ElevationGainMetres, "An elevation gain"),
            JournalNotes = Trimmed(details.JournalNotes),
        };
        await SaveAsync(trip with { Days = trip.Days.Select((d, i) => i == index ? day : d).ToList() });
        return day;
    }

    /// <summary>
    /// What changing the Trip's start or end date would do, with each day staying on its date: the dates it
    /// adds, and the days it removes and whether each has content.
    /// </summary>
    public async Task<DateChangePlan> PlanDateChangeAsync(string tripId, DateOnly startDate, DateOnly endDate)
    {
        CheckDates(startDate, endDate);
        return PlanDateChange(await GetPlannedTripAsync(tripId, "its dates"), startDate, endDate);
    }

    /// <summary>
    /// Changes the Trip's start or end date as PlanDateChangeAsync describes. If that removes days with
    /// content, it's refused unless removeDaysWithContent confirms it.
    /// </summary>
    public async Task<Trip> ChangeDatesAsync(string tripId, DateOnly startDate, DateOnly endDate, bool removeDaysWithContent = false)
    {
        CheckDates(startDate, endDate);
        var trip = await GetPlannedTripAsync(tripId, "its dates");
        var plan = PlanDateChange(trip, startDate, endDate);
        if (plan.NeedsConfirmation && !removeDaysWithContent)
        {
            throw new RuleViolationException($"{plan.DaysWithContent()} would be removed, with what's filled in. Confirm to remove.");
        }

        var changed = trip with { StartDate = startDate, EndDate = endDate, Days = trip.DaysFor(startDate, endDate) };
        await SaveAsync(changed);
        return changed;
    }

    /// <summary>
    /// Moves the whole Trip to a new start date, keeping its length: every day moves with its contents, in
    /// order, so Day 1's plan is on the new start date. Nothing is removed.
    /// </summary>
    public async Task<Trip> MoveTripAsync(string tripId, DateOnly startDate)
    {
        var trip = await GetPlannedTripAsync(tripId, "its dates");
        var endDate = startDate.AddDays(trip.EndDate.DayNumber - trip.StartDate.DayNumber);
        var moved = trip with { StartDate = startDate, EndDate = endDate, Days = trip.DaysMovedTo(startDate) };
        await SaveAsync(moved);
        return moved;
    }

    private static DateChangePlan PlanDateChange(Trip trip, DateOnly startDate, DateOnly endDate) =>
        new(startDate,
            endDate,
            Trip.Dates(startDate, endDate).Where(date => trip.Days.All(day => day.Date != date)).ToList(),
            trip.Days
                .Select((day, i) => new RemovedTripDay(i + 1, day))
                .Where(removed => removed.Day.Date < startDate || removed.Day.Date > endDate)
                .ToList());

    private static void CheckDates(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new RuleViolationException("The Trip can't end before it starts.");
        }
    }

    private static string? Trimmed(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();

    private static decimal? NotNegative(decimal? metres, string what) =>
        metres < 0 ? throw new RuleViolationException($"{what} can't be negative.") : metres;

    // ----- Views, loading and saving -----

    private static TripView View(Trip trip, IReadOnlyDictionary<string, GearItem> items)
    {
        var view = trip.PackingList.View(items, trip.WaterCarriedGrams ?? 0m);
        return new TripView(trip, view.Entries, view.Sections, view.Weight);
    }

    /// <summary>
    /// The Trips not deleted, each with one Trip Day per day of its dates; a Trip saved before Trip Days
    /// existed gets empty ones.
    /// </summary>
    private async Task<IReadOnlyList<Trip>> LoadTripsAsync() =>
        (await _store.ListAsync<Trip>(Table))
            .Where(trip => !trip.Deleted)
            .Select(trip => trip.Days.Count == Trip.DayCount(trip.StartDate, trip.EndDate)
                ? trip
                : trip with { Days = trip.DaysFor(trip.StartDate, trip.EndDate) })
            .ToList();

    private async Task<Trip?> FindTripAsync(string tripId) =>
        (await LoadTripsAsync()).SingleOrDefault(trip => trip.Id == tripId);

    private async Task<Trip> GetExistingTripAsync(string tripId) =>
        await FindTripAsync(tripId) ?? throw new RuleViolationException("That Trip no longer exists.");

    /// <summary>The Trip, refused if it's Completed: its Packing List, checklist and dates are locked until Reopen.</summary>
    /// <param name="what">What's locked, for the message: "its Packing List", "its dates" and so on.</param>
    private async Task<Trip> GetPlannedTripAsync(string tripId, string what)
    {
        var trip = await GetExistingTripAsync(tripId);
        return trip.Status == TripStatus.Completed
            ? throw new RuleViolationException($"This Trip is Completed. Reopen it to change {what}.")
            : trip;
    }

    private Task SaveAsync(Trip trip) => _store.SaveAsync(Table, trip with { UpdatedAt = _clock.UtcNow });

    /// <summary>The Trips whose Packing Lists use the Gear Item, by name, each once, so it can't be deleted from under them.</summary>
    public async Task<IReadOnlyList<string>> FindUsesAsync(string gearItemId) =>
        (await LoadTripsAsync())
            .Where(trip => trip.PackingList.Entries.Any(entry => entry.GearItemId == gearItemId))
            .OrderBy(trip => trip.Name, StringComparer.OrdinalIgnoreCase)
            .Select(trip => $"Trip \"{trip.Name}\"")
            .ToList();
}
