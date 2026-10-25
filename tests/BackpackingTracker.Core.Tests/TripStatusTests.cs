using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Completing and Reopening Trips (freezing weights, the edit locks), Save as Gear List and deleting a Trip.</summary>
public class TripStatusTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static readonly DateOnly July4 = new(2026, 7, 4);

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public TripStatusTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<TripView> TripAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    /// <summary>
    /// A Trip with a tent (765 g, base) under Shelter, a puffy (300 g, worn) under Clothing and two gas canisters
    /// (200 g each, consumable) under Cook: 765 g base, 300 g worn, 400 g consumable.
    /// </summary>
    private async Task<(Trip Trip, GearItem Tent, ListEntry TentEntry)> TripWithGearAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        string CategoryId(string name) => trip.PackingList.Categories.Single(c => c.Name == name).Id;

        async Task<(GearItem, ListEntry)> AddAsync(string name, decimal grams, string category, int quantity, CarryMode mode)
        {
            var item = await _app.Gear.AddGearItemAsync(new GearItemDetails(name, null, grams));
            var entry = await _app.Trips.AddEntryAsync(trip.Id, item.Id, CategoryId(category));
            await _app.Trips.UpdateEntryAsync(trip.Id, entry.Id, quantity, mode);
            return (item, entry);
        }

        var (tent, tentEntry) = await AddAsync("Tent", 765m, "Shelter", 1, CarryMode.Base);
        await AddAsync("Puffy", 300m, "Clothing", 1, CarryMode.Worn);
        await AddAsync("Gas canister", 200m, "Cook", 2, CarryMode.Consumable);
        return ((await TripAsync(trip.Id)).Trip, tent, tentEntry);
    }

    private Task ReweighAsync(GearItem item, decimal grams) =>
        _app.Gear.UpdateGearItemAsync(item.Id, new GearItemDetails(item.Name, null, grams));

    // ----- Completing -----

    [Fact]
    public async Task Completing_a_trip_marks_it_completed()
    {
        var (trip, _, _) = await TripWithGearAsync();

        await _app.Trips.CompleteTripAsync(trip.Id);

        Assert.Equal(TripStatus.Completed, (await TripAsync(trip.Id)).Trip.Status);
    }

    [Fact]
    public async Task Completing_a_trip_freezes_gear_item_weights()
    {
        var (trip, tent, tentEntry) = await TripWithGearAsync();

        await _app.Trips.CompleteTripAsync(trip.Id);
        await ReweighAsync(tent, 900m);

        var view = await TripAsync(trip.Id);
        Assert.Equal(765m, view.Trip.PackingList.Entries.Single(e => e.Id == tentEntry.Id).FrozenWeightGrams);
        Assert.Equal(765m, view.Entries.Single(e => e.Entry.Id == tentEntry.Id).EntryWeightGrams);
        Assert.Equal((765m, 300m, 400m, 1165m), (view.Weight.BaseGrams, view.Weight.WornGrams, view.Weight.ConsumableGrams, view.Weight.StartingPackGrams));
    }

    [Fact]
    public async Task The_category_breakdown_and_subtotals_of_a_completed_trip_use_frozen_weights()
    {
        var (trip, tent, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        await ReweighAsync(tent, 900m);

        var view = await TripAsync(trip.Id);
        Assert.Equal(765m, view.Weight.ByCategory.Single(line => line.Category.Name == "Shelter").Grams);
        Assert.Equal(765m, view.Sections.Single(s => s.Category.Name == "Shelter").SubtotalGrams);
    }

    [Fact]
    public async Task Reweighing_gear_still_changes_planned_trips()
    {
        var (completed, tent, _) = await TripWithGearAsync();
        var planned = await _app.Trips.CreateTripAsync("Next summer", July1.AddYears(1), July4.AddYears(1));
        await _app.Trips.AddEntryAsync(planned.Id, tent.Id, planned.PackingList.Categories[0].Id);
        await _app.Trips.CompleteTripAsync(completed.Id);

        await ReweighAsync(tent, 900m);

        Assert.Equal(765m, (await TripAsync(completed.Id)).Weight.BaseGrams);
        Assert.Equal(900m, (await TripAsync(planned.Id)).Weight.BaseGrams);
    }

    [Fact]
    public async Task A_completed_trip_keeps_its_weights_when_its_gear_is_deleted_on_another_device()
    {
        var (trip, tent, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        // Another device deleted the tent before it saw this Trip; the sync brings the tombstone in.
        await _store.SaveAsync("gearItems", tent with { Deleted = true });

        Assert.Equal(765m, (await TripAsync(trip.Id)).Weight.BaseGrams);
    }

    [Fact]
    public async Task Water_carried_counts_on_a_completed_trip()
    {
        var (trip, _, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        await _app.Trips.SetWaterCarriedAsync(trip.Id, 2000m);

        var weight = (await TripAsync(trip.Id)).Weight;
        Assert.Equal(2400m, weight.ConsumableGrams);
        Assert.Equal(3165m, weight.StartingPackGrams);
    }

    [Fact]
    public async Task A_completed_trip_cant_be_completed_again()
    {
        var (trip, _, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.CompleteTripAsync(trip.Id));

        Assert.Equal("This Trip is already Completed.", refused.Message);
    }

    [Fact]
    public async Task Completing_stamps_the_trip_with_the_current_time()
    {
        var (trip, _, _) = await TripWithGearAsync();
        _clock.UtcNow = _clock.UtcNow.AddDays(70);

        await _app.Trips.CompleteTripAsync(trip.Id);

        Assert.Equal(_clock.UtcNow, (await TripAsync(trip.Id)).Trip.UpdatedAt);
    }

    // ----- What's locked on a Completed Trip -----

    [Fact]
    public async Task The_packing_list_of_a_completed_trip_is_locked()
    {
        var (trip, tent, tentEntry) = await TripWithGearAsync();
        var otherCategory = trip.PackingList.Categories.Single(c => c.Name == "Cook").Id;
        await _app.Trips.CompleteTripAsync(trip.Id);

        Func<Task>[] edits =
        [
            () => _app.Trips.AddEntryAsync(trip.Id, tent.Id, otherCategory),
            () => _app.Trips.UpdateEntryAsync(trip.Id, tentEntry.Id, 2, CarryMode.Base),
            () => _app.Trips.MoveEntryAsync(trip.Id, tentEntry.Id, otherCategory),
            () => _app.Trips.RemoveEntryAsync(trip.Id, tentEntry.Id),
        ];

        foreach (var edit in edits)
        {
            var refused = await Assert.ThrowsAsync<RuleViolationException>(edit);
            Assert.Equal("This Trip is Completed. Reopen it to change its Packing List.", refused.Message);
        }

        var entry = Assert.Single((await TripAsync(trip.Id)).Trip.PackingList.Entries, e => e.Id == tentEntry.Id);
        Assert.Equal((1, CarryMode.Base), (entry.Quantity, entry.CarryMode));
    }

    [Fact]
    public async Task The_packing_checklist_of_a_completed_trip_is_locked()
    {
        var (trip, _, tentEntry) = await TripWithGearAsync();
        await _app.Trips.SetPackedAsync(trip.Id, tentEntry.Id, true);
        await _app.Trips.CompleteTripAsync(trip.Id);

        var untick = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetPackedAsync(trip.Id, tentEntry.Id, false));
        var reset = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ResetPackedAsync(trip.Id));

        Assert.Equal("This Trip is Completed. Reopen it to change its packing checklist.", untick.Message);
        Assert.Equal(untick.Message, reset.Message);
        Assert.Equal(1, (await TripAsync(trip.Id)).PackedCount);
    }

    [Fact]
    public async Task The_dates_of_a_completed_trip_are_locked()
    {
        var (trip, _, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);
        var july2 = July1.AddDays(1);

        Func<Task>[] changes =
        [
            () => _app.Trips.PlanDateChangeAsync(trip.Id, july2, July4),
            () => _app.Trips.ChangeDatesAsync(trip.Id, july2, July4, removeDaysWithContent: true),
            () => _app.Trips.MoveTripAsync(trip.Id, july2),
        ];

        foreach (var change in changes)
        {
            var refused = await Assert.ThrowsAsync<RuleViolationException>(change);
            Assert.Equal("This Trip is Completed. Reopen it to change its dates.", refused.Message);
        }

        Assert.Equal((July1, July4), ((await TripAsync(trip.Id)).Trip.StartDate, (await TripAsync(trip.Id)).Trip.EndDate));
    }

    [Fact]
    public async Task Journal_notes_and_route_details_stay_editable_on_a_completed_trip()
    {
        var (trip, _, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        await _app.Trips.UpdateDayAsync(trip.Id, July1, new TripDayDetails(
            Campsite: "Lake 10,800", ActualDistanceMetres: 18_500m, ElevationGainMetres: 1_200m, JournalNotes: "Hail at the pass."));

        var day = (await TripAsync(trip.Id)).Trip.Days[0];
        Assert.Equal(("Lake 10,800", 18_500m, 1_200m, "Hail at the pass."), (day.Campsite, day.ActualDistanceMetres, day.ElevationGainMetres, day.JournalNotes));
    }

    // ----- Reopening -----

    [Fact]
    public async Task Reopening_sets_the_trip_back_to_planned_with_current_weights()
    {
        var (trip, tent, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);
        await ReweighAsync(tent, 900m);

        await _app.Trips.ReopenTripAsync(trip.Id);

        var view = await TripAsync(trip.Id);
        Assert.Equal(TripStatus.Planned, view.Trip.Status);
        Assert.All(view.Trip.PackingList.Entries, entry => Assert.Null(entry.FrozenWeightGrams));
        Assert.Equal(900m, view.Weight.BaseGrams);
    }

    [Fact]
    public async Task Completing_again_takes_a_fresh_freeze()
    {
        var (trip, tent, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);
        await ReweighAsync(tent, 900m);
        await _app.Trips.ReopenTripAsync(trip.Id);

        await _app.Trips.CompleteTripAsync(trip.Id);
        await ReweighAsync(tent, 1000m);

        Assert.Equal(900m, (await TripAsync(trip.Id)).Weight.BaseGrams);
    }

    [Fact]
    public async Task A_reopened_trips_packing_list_and_dates_can_be_changed_again()
    {
        var (trip, _, tentEntry) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);
        await _app.Trips.ReopenTripAsync(trip.Id);

        await _app.Trips.UpdateEntryAsync(trip.Id, tentEntry.Id, 2, CarryMode.Base);
        await _app.Trips.SetPackedAsync(trip.Id, tentEntry.Id, true);
        await _app.Trips.MoveTripAsync(trip.Id, July1.AddDays(7));

        var view = await TripAsync(trip.Id);
        Assert.Equal(1530m, view.Weight.BaseGrams);
        Assert.Equal(1, view.PackedCount);
        Assert.Equal(July1.AddDays(7), view.Trip.StartDate);
    }

    [Fact]
    public async Task A_planned_trip_cant_be_reopened()
    {
        var (trip, _, _) = await TripWithGearAsync();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ReopenTripAsync(trip.Id));

        Assert.Equal("This Trip is already Planned.", refused.Message);
    }

    // ----- Listing -----

    [Fact]
    public async Task Completed_trips_are_listed_as_past_most_recent_first()
    {
        var june = await _app.Trips.CreateTripAsync("June", new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 21));
        var july = await _app.Trips.CreateTripAsync("July", July1, July4);
        var may = await _app.Trips.CreateTripAsync("May", new DateOnly(2026, 5, 9), new DateOnly(2026, 5, 10));
        await _app.Trips.CreateTripAsync("August", new DateOnly(2026, 8, 10), new DateOnly(2026, 8, 12));
        foreach (var trip in new[] { june, july, may })
        {
            await _app.Trips.CompleteTripAsync(trip.Id);
        }

        var trips = await _app.Trips.ListTripsAsync();

        Assert.Equal(["July", "June", "May"], trips.Past.Select(view => view.Trip.Name));
        Assert.Equal(["August"], trips.Upcoming.Select(view => view.Trip.Name));
    }

    [Fact]
    public async Task Listed_completed_trips_show_their_frozen_weights()
    {
        var (trip, tent, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);
        await ReweighAsync(tent, 900m);

        var listed = Assert.Single((await _app.Trips.ListTripsAsync()).Past);

        Assert.Equal(1165m, listed.Weight.StartingPackGrams);
    }

    // ----- Save as Gear List -----

    [Fact]
    public async Task A_packing_list_can_be_saved_as_a_new_gear_list()
    {
        var (trip, tent, tentEntry) = await TripWithGearAsync();
        await _app.Trips.SetPackedAsync(trip.Id, tentEntry.Id, true);

        var list = await _app.Trips.SaveAsGearListAsync(trip.Id, "  Wind River kit ");

        var view = await _app.Lists.GetGearListAsync(list.Id) ?? throw new InvalidOperationException("No such Gear List.");
        Assert.Equal("Wind River kit", view.GearList.Name);
        Assert.Equal(trip.PackingList.Categories, view.GearList.Categories);
        Assert.Equal(
            trip.PackingList.Entries.Select(e => (e.GearItemId, e.CategoryId, e.Quantity, e.CarryMode)),
            view.GearList.Entries.Select(e => (e.GearItemId, e.CategoryId, e.Quantity, e.CarryMode)));
        Assert.DoesNotContain(view.GearList.Entries, e => trip.PackingList.Entries.Any(t => t.Id == e.Id));
        Assert.All(view.GearList.Entries, e => Assert.False(e.Packed));
        Assert.Equal(1165m, view.Weight.StartingPackGrams);
    }

    [Fact]
    public async Task A_gear_list_saved_from_a_completed_trip_uses_current_weights()
    {
        var (trip, tent, _) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);
        await ReweighAsync(tent, 900m);

        var list = await _app.Trips.SaveAsGearListAsync(trip.Id, "Wind River kit");

        var view = await _app.Lists.GetGearListAsync(list.Id) ?? throw new InvalidOperationException("No such Gear List.");
        Assert.All(view.GearList.Entries, e => Assert.Null(e.FrozenWeightGrams));
        Assert.Equal(900m, view.Weight.BaseGrams);
    }

    [Fact]
    public async Task Changing_the_saved_gear_list_doesnt_change_the_trip()
    {
        var (trip, _, _) = await TripWithGearAsync();
        var list = await _app.Trips.SaveAsGearListAsync(trip.Id, "Wind River kit");

        await _app.Lists.RemoveEntryAsync(list.Id, list.Entries[0].Id);

        Assert.Equal(3, (await TripAsync(trip.Id)).Trip.PackingList.Entries.Count);
    }

    [Fact]
    public async Task A_saved_gear_list_needs_a_name()
    {
        var (trip, _, _) = await TripWithGearAsync();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SaveAsGearListAsync(trip.Id, " "));

        Assert.Equal("Give the Gear List a name.", refused.Message);
    }

    // ----- Deleting -----

    [Fact]
    public async Task A_trip_can_be_deleted()
    {
        var (planned, _, _) = await TripWithGearAsync();
        var completed = await _app.Trips.CreateTripAsync("June", new DateOnly(2026, 6, 20), new DateOnly(2026, 6, 21));
        await _app.Trips.CompleteTripAsync(completed.Id);

        await _app.Trips.DeleteTripAsync(planned.Id);
        await _app.Trips.DeleteTripAsync(completed.Id);

        var trips = await _app.Trips.ListTripsAsync();
        Assert.Empty(trips.Upcoming);
        Assert.Empty(trips.Past);
        Assert.Null(await _app.Trips.GetTripAsync(planned.Id));
    }

    [Fact]
    public async Task A_deleted_trip_is_kept_as_a_tombstone_so_the_deletion_syncs()
    {
        var (trip, _, _) = await TripWithGearAsync();
        _clock.UtcNow = _clock.UtcNow.AddDays(1);

        await _app.Trips.DeleteTripAsync(trip.Id);

        var stored = await _store.LoadAsync<Trip>("trips", trip.Id);
        Assert.NotNull(stored);
        Assert.True(stored.Deleted);
        Assert.Equal(_clock.UtcNow, stored.UpdatedAt);
    }

    [Fact]
    public async Task Gear_on_a_deleted_trip_can_be_deleted()
    {
        var (trip, tent, _) = await TripWithGearAsync();

        await _app.Trips.DeleteTripAsync(trip.Id);

        Assert.Empty(await _app.Trips.FindUsesAsync(tent.Id));
    }
}
