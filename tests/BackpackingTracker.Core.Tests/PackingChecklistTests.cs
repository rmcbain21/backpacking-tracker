using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>A Trip's packing checklist (ticking Packing List entries as packed) and the water carried.</summary>
public class PackingChecklistTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static readonly DateOnly July4 = new(2026, 7, 4);

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public PackingChecklistTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<TripView> TripAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    /// <summary>An empty Trip with a tent (765 g, base), a puffy (300 g, worn) and two gas canisters (200 g each, consumable).</summary>
    private async Task<(Trip Trip, ListEntry Tent, ListEntry Puffy, ListEntry Gas)> TripWithGearAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        var categoryId = trip.PackingList.Categories[0].Id;

        async Task<ListEntry> AddAsync(string name, decimal grams, int quantity, CarryMode mode)
        {
            var item = await _app.Gear.AddGearItemAsync(new GearItemDetails(name, null, grams));
            var entry = await _app.Trips.AddEntryAsync(trip.Id, item.Id, categoryId);
            await _app.Trips.UpdateEntryAsync(trip.Id, entry.Id, quantity, mode);
            return entry;
        }

        var tent = await AddAsync("Tent", 765m, 1, CarryMode.Base);
        var puffy = await AddAsync("Puffy", 300m, 1, CarryMode.Worn);
        var gas = await AddAsync("Gas canister", 200m, 2, CarryMode.Consumable);
        return ((await TripAsync(trip.Id)).Trip, tent, puffy, gas);
    }

    private static bool PackedOf(TripView view, ListEntry entry) =>
        view.Trip.PackingList.Entries.Single(e => e.Id == entry.Id).Packed;

    // ----- The packing checklist -----

    [Fact]
    public async Task Entries_start_unpacked()
    {
        var (trip, _, _, _) = await TripWithGearAsync();

        var view = await TripAsync(trip.Id);

        Assert.Equal((0, 3), (view.PackedCount, view.ChecklistCount));
    }

    [Fact]
    public async Task An_entry_can_be_ticked_and_unticked_and_progress_counts_entries()
    {
        var (trip, tent, _, gas) = await TripWithGearAsync();

        await _app.Trips.SetPackedAsync(trip.Id, tent.Id, true);
        await _app.Trips.SetPackedAsync(trip.Id, gas.Id, true);
        var ticked = await TripAsync(trip.Id);
        await _app.Trips.SetPackedAsync(trip.Id, tent.Id, false);
        var unticked = await TripAsync(trip.Id);

        Assert.True(PackedOf(ticked, tent));
        Assert.Equal((2, 3), (ticked.PackedCount, ticked.ChecklistCount));
        Assert.False(PackedOf(unticked, tent));
        Assert.True(PackedOf(unticked, gas));
        Assert.Equal(1, unticked.PackedCount);
    }

    [Fact]
    public async Task Reset_unticks_every_entry()
    {
        var (trip, tent, puffy, gas) = await TripWithGearAsync();
        foreach (var entry in new[] { tent, puffy, gas })
        {
            await _app.Trips.SetPackedAsync(trip.Id, entry.Id, true);
        }

        await _app.Trips.ResetPackedAsync(trip.Id);

        var view = await TripAsync(trip.Id);
        Assert.Equal(0, view.PackedCount);
        Assert.Equal(3, view.ChecklistCount);
    }

    [Fact]
    public async Task Packing_changes_no_weights()
    {
        var (trip, tent, _, _) = await TripWithGearAsync();
        var before = (await TripAsync(trip.Id)).Weight;

        await _app.Trips.SetPackedAsync(trip.Id, tent.Id, true);

        Assert.Equal(before.StartingPackGrams, (await TripAsync(trip.Id)).Weight.StartingPackGrams);
    }

    [Fact]
    public async Task Raising_the_quantity_unticks_an_entry_since_the_extra_one_isnt_packed()
    {
        var (trip, tent, _, gas) = await TripWithGearAsync();
        await _app.Trips.SetPackedAsync(trip.Id, tent.Id, true);
        await _app.Trips.SetPackedAsync(trip.Id, gas.Id, true);

        await _app.Trips.UpdateEntryAsync(trip.Id, gas.Id, 3, CarryMode.Consumable);
        var tentItemId = tent.GearItemId;
        await _app.Trips.AddEntryAsync(trip.Id, tentItemId, trip.PackingList.Categories[0].Id);

        var view = await TripAsync(trip.Id);
        Assert.False(PackedOf(view, gas));
        Assert.False(PackedOf(view, tent));
    }

    [Fact]
    public async Task Lowering_the_quantity_or_changing_carry_mode_keeps_an_entry_ticked()
    {
        var (trip, _, puffy, gas) = await TripWithGearAsync();
        await _app.Trips.SetPackedAsync(trip.Id, gas.Id, true);
        await _app.Trips.SetPackedAsync(trip.Id, puffy.Id, true);

        await _app.Trips.UpdateEntryAsync(trip.Id, gas.Id, 1, CarryMode.Consumable);
        await _app.Trips.UpdateEntryAsync(trip.Id, puffy.Id, 1, CarryMode.Base);

        var view = await TripAsync(trip.Id);
        Assert.True(PackedOf(view, gas));
        Assert.True(PackedOf(view, puffy));
    }

    [Fact]
    public async Task Ticking_an_entry_no_longer_on_the_list_is_refused()
    {
        var (trip, tent, _, _) = await TripWithGearAsync();
        await _app.Trips.RemoveEntryAsync(trip.Id, tent.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetPackedAsync(trip.Id, tent.Id, true));

        Assert.Equal("That entry is no longer on the Packing List.", refused.Message);
    }

    [Fact]
    public async Task Checklist_changes_on_a_missing_trip_are_refused()
    {
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetPackedAsync("missing", "entry", true));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ResetPackedAsync("missing"));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetWaterCarriedAsync("missing", 1000m));
    }

    [Fact]
    public async Task Ticking_on_a_trip_never_touches_its_source_gear_list()
    {
        var tent = await _app.Gear.AddGearItemAsync(new GearItemDetails("Tent", null, 765m));
        var list = await _app.Lists.CreateGearListAsync("Summer");
        await _app.Lists.AddEntryAsync(list.Id, tent.Id, list.Categories[0].Id);
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4, list.Id);

        await _app.Trips.SetPackedAsync(trip.Id, trip.PackingList.Entries[0].Id, true);

        var gearList = await _app.Lists.GetGearListAsync(list.Id);
        Assert.False(gearList!.GearList.Entries.Single().Packed);
    }

    // ----- Water carried -----

    [Fact]
    public async Task Water_carried_counts_as_consumable_and_starting_pack_weight()
    {
        var (trip, _, _, _) = await TripWithGearAsync();

        await _app.Trips.SetWaterCarriedAsync(trip.Id, 2000m);

        var view = await TripAsync(trip.Id);
        Assert.Equal(2000m, view.Trip.WaterCarriedGrams);
        Assert.Equal(765m, view.Weight.BaseGrams);
        Assert.Equal(300m, view.Weight.WornGrams);
        Assert.Equal(2400m, view.Weight.ConsumableGrams);
        Assert.Equal(3165m, view.Weight.StartingPackGrams);
    }

    [Fact]
    public async Task Water_carried_is_its_own_last_line_in_the_breakdown()
    {
        var (trip, _, _, _) = await TripWithGearAsync();

        await _app.Trips.SetWaterCarriedAsync(trip.Id, 2000m);

        var lines = (await TripAsync(trip.Id)).Weight.ByCategory;
        Assert.Equal([(lines[0].Category.Name, 1165m), ("Water", 2000m)], lines.Select(l => (l.Category.Name, l.Grams)));
        Assert.Equal(2000m / 3165m, lines[^1].Share);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    public async Task Clearing_water_carried_takes_it_out_of_the_totals(int? grams)
    {
        var (trip, _, _, _) = await TripWithGearAsync();
        await _app.Trips.SetWaterCarriedAsync(trip.Id, 2000m);

        await _app.Trips.SetWaterCarriedAsync(trip.Id, grams);

        var view = await TripAsync(trip.Id);
        Assert.Null(view.Trip.WaterCarriedGrams);
        Assert.Equal(400m, view.Weight.ConsumableGrams);
        Assert.DoesNotContain(view.Weight.ByCategory, line => line.Category == Category.Water);
    }

    [Fact]
    public async Task Negative_water_is_refused()
    {
        var (trip, _, _, _) = await TripWithGearAsync();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetWaterCarriedAsync(trip.Id, -1m));

        Assert.Equal("Water carried can't be negative.", refused.Message);
    }

    [Fact]
    public async Task Water_alone_makes_a_starting_pack_weight()
    {
        var trip = await _app.Trips.CreateTripAsync("Day hike", July1, July1);

        await _app.Trips.SetWaterCarriedAsync(trip.Id, 1500m);

        var weight = (await TripAsync(trip.Id)).Weight;
        Assert.Equal(1500m, weight.StartingPackGrams);
        Assert.Equal([("Water", 1m)], weight.ByCategory.Select(l => (l.Category.Name, l.Share)));
    }
}
