using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Trips;

/// <summary>
/// Trips, each with its own Packing List copied from a Gear List (or started empty) and then edited on
/// that Trip only, and one Trip Day per calendar day with its route, journal notes and Meal Plan. A Planned
/// Trip's weights always come from each Gear Item's current weight, and its food from each Meal's and Food
/// Item's current values; a Completed Trip's weights are frozen, and its Packing List, checklist, Meal Plan and
/// dates are locked until it's Reopened. Its Trip Days' route and journal notes, and its water carried, stay editable.
/// </summary>
public sealed class TripPlanner : IGearItemReferences, IFoodItemReferences, IMealReferences
{
    internal const string Table = "trips";

    private const string ListKind = "Packing List";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly GearCatalogue _gear;
    private readonly GearLists _lists;
    private readonly FoodCatalogue _food;
    private readonly MealCatalogue _meals;

    public TripPlanner(IRecordStore store, IClock clock, GearCatalogue gear, GearLists lists, FoodCatalogue food, MealCatalogue meals)
    {
        _store = store;
        _clock = clock;
        _gear = gear;
        _lists = lists;
        _food = food;
        _meals = meals;
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
        var views = await ViewAsync(await LoadTripsAsync());

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

    /// <summary>A Trip with its Packing List's current weights, its Meal Plan and its totals, or null if there's no such Trip.</summary>
    public async Task<TripView?> GetTripAsync(string tripId)
    {
        var trip = await FindTripAsync(tripId);
        return trip is null ? null : (await ViewAsync([trip]))[0];
    }

    // ----- Trip Status, Save as Gear List and delete -----

    /// <summary>
    /// Marks the Trip Completed, copying each Packing List entry's current Gear Item weight onto the entry, and each
    /// slot entry's current weight, calories, protein and cost (a Meal's through its Food Items) onto the slot entry,
    /// so later edits to gear or food never change this Trip's numbers. Its Packing List, checklist, Meal Plan and
    /// dates are then locked until it's Reopened.
    /// </summary>
    public async Task CompleteTripAsync(string tripId)
    {
        var trip = await GetExistingTripAsync(tripId);
        if (trip.Status == TripStatus.Completed)
        {
            throw new RuleViolationException("This Trip is already Completed.");
        }

        var items = await _gear.GetGearItemsAsync(trip.PackingList.Entries.Select(entry => entry.GearItemId));
        var (meals, foodItems) = await LookUpFoodAsync([trip]);
        await SaveAsync(trip with
        {
            Status = TripStatus.Completed,
            PackingList = trip.PackingList.Freeze(items),
            // A slot entry whose Meal or Food Item is gone is left unfrozen, so it still counts for nothing.
            Days = WithSlotEntries(trip, entry => entry with { Frozen = MealPlanView.CurrentFigures(entry, meals, foodItems) }),
        });
    }

    /// <summary>
    /// Sets a Completed Trip back to Planned and clears its frozen weights and food figures, so it uses each Gear
    /// Item's, Meal's and Food Item's current values again; completing it again takes a fresh freeze.
    /// </summary>
    public async Task ReopenTripAsync(string tripId)
    {
        var trip = await GetExistingTripAsync(tripId);
        if (trip.Status != TripStatus.Completed)
        {
            throw new RuleViolationException("This Trip is already Planned.");
        }

        await SaveAsync(trip with
        {
            Status = TripStatus.Planned,
            PackingList = trip.PackingList.Unfreeze(),
            Days = WithSlotEntries(trip, entry => entry with { Frozen = null }),
        });
    }

    private static IReadOnlyList<TripDay> WithSlotEntries(Trip trip, Func<SlotEntry, SlotEntry> change) =>
        trip.Days.Select(day => day with { SlotEntries = day.SlotEntries.Select(change).ToList() }).ToList();

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

    // ----- The Meal Plan -----

    /// <summary>
    /// Puts a Meal in one of the Trip Day's slots with the quantity (1 unless given). If the slot already has that
    /// Meal, adds to its quantity instead. Retired Meals can't be added.
    /// </summary>
    public async Task<SlotEntry> AddMealToSlotAsync(string tripId, DateOnly date, MealSlot slot, string mealId, decimal quantity = 1m)
    {
        var meal = await _meals.GetMealAsync(mealId) ?? throw new RuleViolationException("That Meal no longer exists.");
        if (meal.Meal.Retired)
        {
            throw new RuleViolationException($"{meal.Meal.Name} is Retired, so it can't be added to the Meal Plan.");
        }

        return await AddToSlotAsync(tripId, date, slot, quantity, entry => entry.MealId == mealId,
            new SlotEntry { Id = Guid.NewGuid().ToString(), Slot = slot, MealId = mealId, Quantity = quantity });
    }

    /// <summary>
    /// Puts servings of a Food Item in one of the Trip Day's slots (1 unless given). If the slot already has that
    /// Food Item, adds to its servings instead. Retired Food Items can't be added.
    /// </summary>
    public async Task<SlotEntry> AddFoodItemToSlotAsync(string tripId, DateOnly date, MealSlot slot, string foodItemId, decimal quantity = 1m)
    {
        var item = await _food.GetFoodItemAsync(foodItemId) ?? throw new RuleViolationException("That Food Item no longer exists.");
        if (item.Retired)
        {
            throw new RuleViolationException($"{item.Name} is Retired, so it can't be added to the Meal Plan.");
        }

        return await AddToSlotAsync(tripId, date, slot, quantity, entry => entry.FoodItemId == foodItemId,
            new SlotEntry { Id = Guid.NewGuid().ToString(), Slot = slot, FoodItemId = foodItemId, Quantity = quantity });
    }

    private async Task<SlotEntry> AddToSlotAsync(
        string tripId, DateOnly date, MealSlot slot, decimal quantity, Func<SlotEntry, bool> sameThing, SlotEntry newEntry)
    {
        CheckSlot(slot);
        var amount = CheckedQuantity(quantity);
        SlotEntry added = newEntry with { Quantity = amount };
        await ChangeDayAsync(tripId, date, day =>
        {
            var existing = day.SlotEntries.FirstOrDefault(entry => entry.Slot == slot && sameThing(entry));
            if (existing is null)
            {
                return day with { SlotEntries = [.. day.SlotEntries, added] };
            }

            added = existing with { Quantity = existing.Quantity + amount };
            return day with { SlotEntries = day.SlotEntries.Select(entry => entry.Id == existing.Id ? added : entry).ToList() };
        });
        return added;
    }

    /// <summary>Changes how many of a Meal, or servings of a Food Item, a slot entry has.</summary>
    public async Task UpdateSlotEntryAsync(string tripId, DateOnly date, string slotEntryId, decimal quantity)
    {
        var amount = CheckedQuantity(quantity);
        await ChangeDayAsync(tripId, date, day =>
        {
            FindSlotEntry(day, slotEntryId);
            return day with
            {
                SlotEntries = day.SlotEntries.Select(entry => entry.Id == slotEntryId ? entry with { Quantity = amount } : entry).ToList(),
            };
        });
    }

    public async Task RemoveSlotEntryAsync(string tripId, DateOnly date, string slotEntryId) =>
        await ChangeDayAsync(tripId, date, day =>
        {
            FindSlotEntry(day, slotEntryId);
            return day with { SlotEntries = day.SlotEntries.Where(entry => entry.Id != slotEntryId).ToList() };
        });

    /// <summary>What copying the source Trip's Meal Plan onto this Trip would do (see MealPlanCopyPlan).</summary>
    public async Task<MealPlanCopyPlan> PlanMealPlanCopyAsync(string tripId, string sourceTripId)
    {
        var (_, plan, _) = await PrepareMealPlanCopyAsync(tripId, sourceTripId);
        return plan;
    }

    /// <summary>
    /// Copies the source Trip's Meal Plan onto this Trip as PlanMealPlanCopyAsync describes: each copied entry refers to
    /// the same Meal or Food Item with the same quantity, gets a new id, and carries no frozen figures. If that replaces
    /// days with food planned, it's refused unless replaceExisting confirms it.
    /// </summary>
    public async Task CopyMealPlanAsync(string tripId, string sourceTripId, bool replaceExisting = false)
    {
        var (trip, plan, copiedDays) = await PrepareMealPlanCopyAsync(tripId, sourceTripId);
        if (plan.NeedsConfirmation && !replaceExisting)
        {
            throw new RuleViolationException($"{plan.ReplacedDaysText} already {(plan.ReplacedDays.Count == 1 ? "has" : "have")} food planned. Confirm to replace it.");
        }

        await SaveAsync(trip with
        {
            Days = trip.Days.Select((day, i) => i < copiedDays.Count ? day with { SlotEntries = copiedDays[i] } : day).ToList(),
        });
    }

    /// <summary>The target Trip, what copying would do, and the copied slot entries for each of its first days.</summary>
    private async Task<(Trip Trip, MealPlanCopyPlan Plan, IReadOnlyList<IReadOnlyList<SlotEntry>> CopiedDays)> PrepareMealPlanCopyAsync(
        string tripId, string sourceTripId)
    {
        var trip = await GetPlannedTripAsync(tripId, "its Meal Plan");
        if (sourceTripId == tripId)
        {
            throw new RuleViolationException("Choose another Trip to copy from.");
        }

        var source = await FindTripAsync(sourceTripId) ?? throw new RuleViolationException("That Trip no longer exists.");
        if (source.Days.All(day => day.SlotEntries.Count == 0))
        {
            throw new RuleViolationException($"{source.Name} has no Meal Plan to copy.");
        }

        var (meals, foodItems) = await LookUpFoodAsync([source]);
        var skipped = new List<string>();

        (string Name, bool Retired)? Describe(SlotEntry entry) =>
            entry.MealId is { } mealId
                ? meals.TryGetValue(mealId, out var meal) ? (meal.Meal.Name, meal.Meal.Retired) : null
                : foodItems.TryGetValue(entry.FoodItemId ?? "", out var item) ? (item.Name, item.Retired) : null;

        // Kept: what's still in use. Retired things are hidden from new plans; deleted ones can't be referred to.
        bool Keep(SlotEntry entry)
        {
            if (Describe(entry) is not { } found)
            {
                return false;
            }

            if (found.Retired && !skipped.Contains(found.Name))
            {
                skipped.Add(found.Name);
            }

            return !found.Retired;
        }

        var dayCount = Math.Min(trip.Days.Count, source.Days.Count);
        var copiedDays = source.Days.Take(dayCount)
            .Select(day => (IReadOnlyList<SlotEntry>)day.SlotEntries
                .Where(Keep)
                .Select(entry => entry with { Id = Guid.NewGuid().ToString(), Frozen = null })
                .ToList())
            .ToList();
        var replacedDays = trip.Days.Take(dayCount)
            .Select((day, i) => (day, Number: i + 1))
            .Where(d => d.day.SlotEntries.Count > 0)
            .Select(d => d.Number)
            .ToList();

        return (trip, new MealPlanCopyPlan(source.Name, dayCount, replacedDays, skipped), copiedDays);
    }

    /// <summary>Changes the Meal Plan of the Trip Day for that date; refused on a Completed Trip.</summary>
    private async Task ChangeDayAsync(string tripId, DateOnly date, Func<TripDay, TripDay> change)
    {
        var trip = await GetPlannedTripAsync(tripId, "its Meal Plan");
        var index = trip.Days.ToList().FindIndex(day => day.Date == date);
        if (index < 0)
        {
            throw new RuleViolationException("That day is no longer part of the Trip.");
        }

        var changed = change(trip.Days[index]);
        await SaveAsync(trip with { Days = trip.Days.Select((d, i) => i == index ? changed : d).ToList() });
    }

    private static SlotEntry FindSlotEntry(TripDay day, string slotEntryId) =>
        day.SlotEntries.SingleOrDefault(entry => entry.Id == slotEntryId)
            ?? throw new RuleViolationException("That is no longer in the Meal Plan.");

    private static void CheckSlot(MealSlot slot)
    {
        if (!Enum.IsDefined(slot))
        {
            throw new RuleViolationException("Choose Breakfast, Lunch, Dinner or Snacks.");
        }
    }

    /// <summary>The quantity to 0.01, refused unless it's more than zero.</summary>
    private static decimal CheckedQuantity(decimal quantity)
    {
        var rounded = ServingsInput.Round(quantity);
        return rounded > 0 ? rounded : throw new RuleViolationException("A quantity must be more than zero.");
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

    /// <summary>The Trips as views, looking up the Gear Items, Meals and Food Items they use once for all of them.</summary>
    private async Task<IReadOnlyList<TripView>> ViewAsync(IReadOnlyList<Trip> trips)
    {
        var items = await _gear.GetGearItemsAsync(trips.SelectMany(trip => trip.PackingList.Entries).Select(entry => entry.GearItemId));
        var (meals, foodItems) = await LookUpFoodAsync(trips);

        return trips.Select(trip =>
        {
            var mealPlan = MealPlanView.For(trip, meals, foodItems);
            var view = trip.PackingList.View(items, trip.WaterCarriedGrams ?? 0m, mealPlan.FoodGrams);
            return new TripView(trip, view.Entries, view.Sections, view.Weight, mealPlan);
        }).ToList();
    }

    /// <summary>The Meals and Food Items the Trips' slot entries use, Retired or not, by id.</summary>
    private async Task<(IReadOnlyDictionary<string, MealView> Meals, IReadOnlyDictionary<string, FoodItem> FoodItems)> LookUpFoodAsync(
        IReadOnlyList<Trip> trips)
    {
        var slotEntries = trips.SelectMany(trip => trip.Days).SelectMany(day => day.SlotEntries).ToList();
        var meals = await _meals.GetMealsAsync(slotEntries.Select(entry => entry.MealId).OfType<string>());
        var foodItems = await _food.GetFoodItemsAsync(slotEntries.Select(entry => entry.FoodItemId).OfType<string>());
        return (meals, foodItems);
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

    /// <summary>The Trips whose Meal Plans have the Food Item in a slot, by name, each once.</summary>
    async Task<IReadOnlyList<string>> IFoodItemReferences.FindUsesAsync(string foodItemId) =>
        await FindMealPlanUsesAsync(entry => entry.FoodItemId == foodItemId);

    /// <summary>The Trips whose Meal Plans have the Meal in a slot, by name, each once.</summary>
    async Task<IReadOnlyList<string>> IMealReferences.FindUsesAsync(string mealId) =>
        await FindMealPlanUsesAsync(entry => entry.MealId == mealId);

    private async Task<IReadOnlyList<string>> FindMealPlanUsesAsync(Func<SlotEntry, bool> uses) =>
        (await LoadTripsAsync())
            .Where(trip => trip.Days.SelectMany(day => day.SlotEntries).Any(uses))
            .OrderBy(trip => trip.Name, StringComparer.OrdinalIgnoreCase)
            .Select(trip => $"Trip \"{trip.Name}\"")
            .ToList();
}
