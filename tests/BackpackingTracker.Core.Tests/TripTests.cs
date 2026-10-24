using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Creating Trips, from a Gear List or empty, and editing their Packing Lists.</summary>
public class TripTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static readonly DateOnly July4 = new(2026, 7, 4);

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public TripTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<TripView> TripAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private async Task<GearListView> GearListAsync(string gearListId) =>
        await _app.Lists.GetGearListAsync(gearListId) ?? throw new InvalidOperationException("No such Gear List.");

    /// <summary>Each entry as (Category, Gear Item id, quantity, Carry Mode), in list order.</summary>
    private static (string Category, string GearItemId, int Quantity, CarryMode CarryMode)[] Entries(IEnumerable<CategorySection> sections) =>
        sections
            .SelectMany(s => s.Entries.Select(e => (s.Category.Name, e.Entry.GearItemId, e.Entry.Quantity, e.Entry.CarryMode)))
            .ToArray();

    private Task<GearItem> AddGearAsync(string name, decimal grams) =>
        _app.Gear.AddGearItemAsync(new GearItemDetails(name, null, grams));

    /// <summary>
    /// A "Summer" Gear List: a tent (765 g) under "Big Three", a puffy (300 g, worn) under Clothing and
    /// two gas canisters (200 g each, consumable) under Cook.
    /// </summary>
    private async Task<(GearList List, GearItem Tent, GearItem Puffy, GearItem Gas)> SummerListAsync()
    {
        var tent = await AddGearAsync("Durston X-Mid 1", 765m);
        var puffy = await AddGearAsync("Puffy", 300m);
        var gas = await AddGearAsync("Gas canister", 200m);
        var list = await _app.Lists.CreateGearListAsync("Summer");
        string CategoryId(string name) => list.Categories.Single(c => c.Name == name).Id;

        await _app.Lists.RenameCategoryAsync(list.Id, CategoryId("Shelter"), "Big Three");
        await _app.Lists.AddEntryAsync(list.Id, tent.Id, CategoryId("Shelter"));
        var puffyEntry = await _app.Lists.AddEntryAsync(list.Id, puffy.Id, CategoryId("Clothing"));
        await _app.Lists.UpdateEntryAsync(list.Id, puffyEntry.Id, 1, CarryMode.Worn);
        var gasEntry = await _app.Lists.AddEntryAsync(list.Id, gas.Id, CategoryId("Cook"));
        await _app.Lists.UpdateEntryAsync(list.Id, gasEntry.Id, 2, CarryMode.Consumable);
        return ((await GearListAsync(list.Id)).GearList, tent, puffy, gas);
    }

    // ----- Creating a Trip -----

    [Fact]
    public async Task A_trip_from_a_gear_list_gets_a_copy_of_its_categories_and_entries()
    {
        var (summer, _, _, _) = await SummerListAsync();

        var trip = await _app.Trips.CreateTripAsync("  Wind River High Route ", July1, July4, summer.Id);

        var view = await TripAsync(trip.Id);
        var gearList = await GearListAsync(summer.Id);
        Assert.Equal("Wind River High Route", view.Trip.Name);
        Assert.Equal((July1, July4), (view.Trip.StartDate, view.Trip.EndDate));
        Assert.Equal("Summer", view.Trip.CreatedFrom);
        Assert.Equal(gearList.GearList.Categories.Select(c => c.Name), view.Trip.PackingList.Categories.Select(c => c.Name));
        Assert.Equal(Entries(gearList.Sections), Entries(view.Sections));
        Assert.Empty(view.Trip.PackingList.Entries.Select(e => e.Id).Intersect(summer.Entries.Select(e => e.Id)));
    }

    [Fact]
    public async Task A_new_trip_is_planned()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);

        Assert.Equal(TripStatus.Planned, (await TripAsync(trip.Id)).Trip.Status);
        Assert.True(Guid.TryParse(trip.Id, out _));
        Assert.Equal(_clock.UtcNow, trip.UpdatedAt);
    }

    [Fact]
    public async Task An_empty_trip_starts_with_the_default_categories_and_no_entries()
    {
        await _app.Gear.AddDefaultCategoryAsync("Packrafting");

        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);

        var view = await TripAsync(trip.Id);
        var defaults = await _app.Gear.ListDefaultCategoriesAsync();
        Assert.Equal(defaults.Select(c => c.Name), view.Trip.PackingList.Categories.Select(c => c.Name));
        Assert.Empty(view.Entries);
        Assert.Null(view.Trip.CreatedFrom);
        Assert.Equal(0m, view.Weight.StartingPackGrams);
    }

    [Fact]
    public async Task A_one_day_trip_can_start_and_end_on_the_same_day()
    {
        var trip = await _app.Trips.CreateTripAsync("Day hike", July1, July1);

        Assert.Equal(July1, (await TripAsync(trip.Id)).Trip.EndDate);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_trip_needs_a_name(string name)
    {
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.CreateTripAsync(name, July1, July4));

        Assert.Equal("Give the Trip a name.", refused.Message);
        Assert.Empty((await _app.Trips.ListTripsAsync()).Upcoming);
    }

    [Fact]
    public async Task A_trip_cant_end_before_it_starts()
    {
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.CreateTripAsync("Backwards", July4, July1));

        Assert.Equal("The Trip can't end before it starts.", refused.Message);
        Assert.Empty((await _app.Trips.ListTripsAsync()).Upcoming);
    }

    [Fact]
    public async Task A_trip_cant_be_created_from_a_deleted_gear_list()
    {
        var list = await _app.Lists.CreateGearListAsync("Summer");
        await _app.Lists.DeleteGearListAsync(list.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, list.Id));

        Assert.Equal("That Gear List no longer exists.", refused.Message);
    }

    // ----- Independence from the Gear List -----

    [Fact]
    public async Task Changing_the_gear_list_afterwards_doesnt_change_the_trip()
    {
        var (summer, tent, puffy, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var before = Entries((await TripAsync(trip.Id)).Sections);
        var tentEntry = summer.Entries.Single(e => e.GearItemId == tent.Id);
        var puffyEntry = summer.Entries.Single(e => e.GearItemId == puffy.Id);

        await _app.Lists.UpdateEntryAsync(summer.Id, tentEntry.Id, 2, CarryMode.Worn);
        await _app.Lists.RemoveEntryAsync(summer.Id, puffyEntry.Id);
        await _app.Lists.AddEntryAsync(summer.Id, (await AddGearAsync("Bear can", 900m)).Id);
        await _app.Lists.RenameCategoryAsync(summer.Id, summer.Categories[0].Id, "Big Five");
        await _app.Lists.AddCategoryAsync(summer.Id, "Snow");

        var after = await TripAsync(trip.Id);
        Assert.Equal(before, Entries(after.Sections));
        Assert.Equal("Big Three", after.Trip.PackingList.Categories[0].Name);
        Assert.DoesNotContain("Snow", after.Trip.PackingList.Categories.Select(c => c.Name));
    }

    [Fact]
    public async Task Changing_the_packing_list_doesnt_change_the_gear_list()
    {
        var (summer, tent, puffy, _) = await SummerListAsync();
        var before = Entries((await GearListAsync(summer.Id)).Sections);
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var packingList = trip.PackingList;
        var tentEntry = packingList.Entries.Single(e => e.GearItemId == tent.Id);
        var puffyEntry = packingList.Entries.Single(e => e.GearItemId == puffy.Id);

        await _app.Trips.UpdateEntryAsync(trip.Id, tentEntry.Id, 2, CarryMode.Worn);
        await _app.Trips.RemoveEntryAsync(trip.Id, puffyEntry.Id);
        await _app.Trips.AddEntryAsync(trip.Id, (await AddGearAsync("Bear can", 900m)).Id, packingList.Categories[0].Id);
        await _app.Trips.MoveEntryAsync(trip.Id, tentEntry.Id, packingList.Categories[1].Id);

        Assert.Equal(before, Entries((await GearListAsync(summer.Id)).Sections));
    }

    [Fact]
    public async Task Deleting_the_gear_list_leaves_the_trip_untouched()
    {
        var (summer, _, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var before = await TripAsync(trip.Id);

        await _app.Lists.DeleteGearListAsync(summer.Id);

        var after = await TripAsync(trip.Id);
        Assert.Equal(Entries(before.Sections), Entries(after.Sections));
        Assert.Equal(
            (before.Weight.BaseGrams, before.Weight.WornGrams, before.Weight.ConsumableGrams),
            (after.Weight.BaseGrams, after.Weight.WornGrams, after.Weight.ConsumableGrams));
        Assert.Equal("Summer", after.Trip.CreatedFrom);
    }

    // ----- Editing the Packing List -----

    [Fact]
    public async Task Gear_can_be_added_to_the_packing_list_and_adding_it_again_adds_one_more()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        var water = trip.PackingList.Categories.Single(c => c.Name == "Water");
        var bottle = await AddGearAsync("Smartwater bottle", 35m);

        var first = await _app.Trips.AddEntryAsync(trip.Id, bottle.Id, water.Id);
        var second = await _app.Trips.AddEntryAsync(trip.Id, bottle.Id, water.Id);

        var view = await TripAsync(trip.Id);
        Assert.Equal(first.Id, second.Id);
        Assert.Equal([("Water", bottle.Id, 2, CarryMode.Base)], Entries(view.Sections));
        Assert.Equal(70m, view.Weight.BaseGrams);
    }

    [Fact]
    public async Task A_packing_list_entrys_quantity_and_carry_mode_can_be_changed()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var tentEntry = trip.PackingList.Entries.Single(e => e.GearItemId == tent.Id);

        await _app.Trips.UpdateEntryAsync(trip.Id, tentEntry.Id, 3, CarryMode.Consumable);

        var changed = (await TripAsync(trip.Id)).Entries.Single(e => e.Entry.Id == tentEntry.Id);
        Assert.Equal((3, CarryMode.Consumable), (changed.Entry.Quantity, changed.Entry.CarryMode));
        Assert.Equal(2295m, changed.EntryWeightGrams);
    }

    [Fact]
    public async Task A_packing_list_entry_can_be_moved_to_another_category()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var tentEntry = trip.PackingList.Entries.Single(e => e.GearItemId == tent.Id);
        var pack = trip.PackingList.Categories.Single(c => c.Name == "Pack");

        await _app.Trips.MoveEntryAsync(trip.Id, tentEntry.Id, pack.Id);

        var section = (await TripAsync(trip.Id)).Sections.Single(s => s.Category.Name == "Pack");
        Assert.Equal([tent.Id], section.Entries.Select(e => e.Entry.GearItemId));
    }

    [Fact]
    public async Task A_packing_list_entry_can_be_removed()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var tentEntry = trip.PackingList.Entries.Single(e => e.GearItemId == tent.Id);

        await _app.Trips.RemoveEntryAsync(trip.Id, tentEntry.Id);

        var view = await TripAsync(trip.Id);
        Assert.DoesNotContain(tent.Id, view.Entries.Select(e => e.Entry.GearItemId));
        Assert.Equal(0m, view.Weight.BaseGrams);
    }

    [Fact]
    public async Task Retired_gear_cant_be_added_to_a_packing_list()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        var tarp = await AddGearAsync("Tarp", 300m);
        await _app.Gear.RetireGearItemAsync(tarp.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddEntryAsync(trip.Id, tarp.Id, trip.PackingList.Categories[0].Id));

        Assert.Equal("Tarp is Retired. Un-retire it to add it to a Packing List.", refused.Message);
    }

    [Fact]
    public async Task Gear_that_was_retired_after_it_went_on_the_gear_list_is_still_copied()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        await _app.Gear.RetireGearItemAsync(tent.Id);

        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);

        Assert.Contains(tent.Id, trip.PackingList.Entries.Select(e => e.GearItemId));
    }

    [Fact]
    public async Task A_packing_list_entry_needs_a_quantity_of_at_least_one()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var tentEntry = trip.PackingList.Entries.Single(e => e.GearItemId == tent.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.UpdateEntryAsync(trip.Id, tentEntry.Id, 0, CarryMode.Base));

        Assert.Equal("Quantity must be 1 or more.", refused.Message);
    }

    [Fact]
    public async Task Gear_goes_into_one_of_the_packing_lists_own_categories()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        var bottle = await AddGearAsync("Smartwater bottle", 35m);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.AddEntryAsync(trip.Id, bottle.Id, "not-a-category"));

        Assert.Equal("Choose a Category on this list.", refused.Message);
    }

    [Fact]
    public async Task A_removed_entry_cant_be_changed()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        var tentEntry = trip.PackingList.Entries.Single(e => e.GearItemId == tent.Id);
        await _app.Trips.RemoveEntryAsync(trip.Id, tentEntry.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.UpdateEntryAsync(trip.Id, tentEntry.Id, 2, CarryMode.Base));

        Assert.Equal("That entry is no longer on the Packing List.", refused.Message);
    }

    [Fact]
    public async Task Changing_a_packing_list_stamps_the_trip_with_the_current_time()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        var later = _clock.UtcNow.AddHours(2);
        _clock.UtcNow = later;

        await _app.Trips.AddEntryAsync(trip.Id, (await AddGearAsync("Tarp", 300m)).Id, trip.PackingList.Categories[0].Id);

        Assert.Equal(later, (await TripAsync(trip.Id)).Trip.UpdatedAt);
    }

    // ----- Totals -----

    [Fact]
    public async Task A_trip_shows_its_base_worn_consumable_and_starting_pack_weight()
    {
        var (summer, _, _, _) = await SummerListAsync();

        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);

        var weight = (await TripAsync(trip.Id)).Weight;
        Assert.Equal(765m, weight.BaseGrams);
        Assert.Equal(300m, weight.WornGrams);
        Assert.Equal(400m, weight.ConsumableGrams);
        Assert.Equal(1165m, weight.StartingPackGrams);
        Assert.Equal(["Big Three", "Cook"], weight.ByCategory.Select(line => line.Category.Name));
    }

    [Fact]
    public async Task A_planned_trip_uses_each_gear_items_current_weight()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);

        await _app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(tent.Name, null, 800m));

        Assert.Equal(800m, (await TripAsync(trip.Id)).Weight.BaseGrams);
    }

    // ----- Listing Trips -----

    [Fact]
    public async Task Planned_trips_are_listed_as_upcoming_by_start_date()
    {
        await _app.Trips.CreateTripAsync("August", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 12));
        await _app.Trips.CreateTripAsync("July", July1, July4);
        await _app.Trips.CreateTripAsync("June", new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 21));

        var trips = await _app.Trips.ListTripsAsync();

        Assert.Equal(["June", "July", "August"], trips.Upcoming.Select(view => view.Trip.Name));
        Assert.Empty(trips.Past);
    }

    [Fact]
    public async Task Listed_trips_show_their_weights()
    {
        var (summer, _, _, _) = await SummerListAsync();
        await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);

        var listed = Assert.Single((await _app.Trips.ListTripsAsync()).Upcoming);

        Assert.Equal(1165m, listed.Weight.StartingPackGrams);
    }

    [Fact]
    public async Task There_is_no_trip_with_an_unknown_id()
    {
        Assert.Null(await _app.Trips.GetTripAsync("not-a-trip"));
    }

    // ----- The Gear Item delete guard -----

    [Fact]
    public async Task Gear_on_a_packing_list_cant_be_deleted_only_retired()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        await _app.Lists.DeleteGearListAsync(summer.Id);

        var plan = await _app.Gear.PlanGearItemDeletionAsync(tent.Id);

        Assert.False(plan.CanDelete);
        Assert.True(plan.OffersRetire);
        Assert.Equal(["Trip \"Wind River High Route\""], plan.UsedBy);
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Gear.DeleteGearItemAsync(tent.Id));
    }

    [Fact]
    public async Task Gear_used_on_a_gear_list_and_a_trip_names_both()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);

        var plan = await _app.Gear.PlanGearItemDeletionAsync(tent.Id);

        Assert.Equal(["Gear List \"Summer\"", "Trip \"Wind River High Route\""], plan.UsedBy);
    }

    [Fact]
    public async Task Gear_removed_from_every_packing_list_can_be_deleted()
    {
        var (summer, tent, _, _) = await SummerListAsync();
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4, summer.Id);
        await _app.Lists.DeleteGearListAsync(summer.Id);
        await _app.Trips.RemoveEntryAsync(trip.Id, trip.PackingList.Entries.Single(e => e.GearItemId == tent.Id).Id);

        await _app.Gear.DeleteGearItemAsync(tent.Id);

        Assert.Null(await _app.Gear.GetGearItemAsync(tent.Id));
    }
}
