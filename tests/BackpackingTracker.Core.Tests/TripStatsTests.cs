using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>A Trip's stats: days and nights, route totals from its Trip Days, and its pack weights.</summary>
public class TripStatsTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static readonly DateOnly July4 = new(2026, 7, 4);

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public TripStatsTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<TripStats> StatsAsync(string tripId) =>
        (await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.")).Stats;

    private static DateOnly July(int day) => new(2026, 7, day);

    /// <summary>A July 1–4 Trip with a tent (765 g, base), a puffy (300 g, worn) and two gas canisters (200 g each, consumable).</summary>
    private async Task<(Trip Trip, GearItem Tent)> TripWithGearAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        var categoryId = trip.PackingList.Categories[0].Id;

        async Task<GearItem> AddAsync(string name, decimal grams, int quantity, CarryMode mode)
        {
            var item = await _app.Gear.AddGearItemAsync(new GearItemDetails(name, null, grams));
            var entry = await _app.Trips.AddEntryAsync(trip.Id, item.Id, categoryId);
            await _app.Trips.UpdateEntryAsync(trip.Id, entry.Id, quantity, mode);
            return item;
        }

        var tent = await AddAsync("Tent", 765m, 1, CarryMode.Base);
        await AddAsync("Puffy", 300m, 1, CarryMode.Worn);
        await AddAsync("Gas canister", 200m, 2, CarryMode.Consumable);
        return (trip, tent);
    }

    private Task SetRouteAsync(string tripId, int day, decimal? planned, decimal? actual, decimal? gain) =>
        _app.Trips.UpdateDayAsync(tripId, July(day), new TripDayDetails(
            PlannedDistanceMetres: planned, ActualDistanceMetres: actual, ElevationGainMetres: gain));

    // ----- Days and nights -----

    [Fact]
    public async Task A_four_day_trip_has_four_days_and_three_nights()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);

        var stats = await StatsAsync(trip.Id);

        Assert.Equal((4, 3), (stats.Days, stats.Nights));
    }

    [Fact]
    public async Task A_one_day_trip_has_no_nights()
    {
        var trip = await _app.Trips.CreateTripAsync("Day hike", July1, July1);

        var stats = await StatsAsync(trip.Id);

        Assert.Equal((1, 0), (stats.Days, stats.Nights));
    }

    [Fact]
    public async Task Days_follow_a_date_change()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);

        await _app.Trips.ChangeDatesAsync(trip.Id, July1, July(6));

        Assert.Equal((6, 5), ((await StatsAsync(trip.Id)).Days, (await StatsAsync(trip.Id)).Nights));
    }

    // ----- Route totals -----

    [Fact]
    public async Task Distances_and_elevation_gain_are_summed_over_the_trip_days()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        await SetRouteAsync(trip.Id, 1, 12_000m, 13_500m, 800m);
        await SetRouteAsync(trip.Id, 2, 18_000m, 17_000m, 1_200m);
        await SetRouteAsync(trip.Id, 3, 15_000m, 16_250m, 950m);

        var stats = await StatsAsync(trip.Id);

        Assert.Equal((45_000m, 46_750m, 2_950m), (stats.PlannedDistanceMetres, stats.ActualDistanceMetres, stats.ElevationGainMetres));
    }

    [Fact]
    public async Task Days_without_a_figure_are_left_out_of_its_total()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        await SetRouteAsync(trip.Id, 1, 12_000m, null, 800m);
        await SetRouteAsync(trip.Id, 3, 15_000m, 16_250m, null);

        var stats = await StatsAsync(trip.Id);

        Assert.Equal((27_000m, 16_250m, 800m), (stats.PlannedDistanceMetres, stats.ActualDistanceMetres, stats.ElevationGainMetres));
    }

    [Fact]
    public async Task A_route_total_no_day_has_filled_in_is_not_recorded_rather_than_zero()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        await SetRouteAsync(trip.Id, 2, 18_000m, null, null);

        var stats = await StatsAsync(trip.Id);

        Assert.Equal(18_000m, stats.PlannedDistanceMetres);
        Assert.Null(stats.ActualDistanceMetres);
        Assert.Null(stats.ElevationGainMetres);
    }

    [Fact]
    public async Task A_typed_in_zero_counts_as_recorded()
    {
        var trip = await _app.Trips.CreateTripAsync("Zero day", July1, July1);
        await SetRouteAsync(trip.Id, 1, 0m, 0m, 0m);

        var stats = await StatsAsync(trip.Id);

        Assert.Equal((0m, 0m, 0m), (stats.PlannedDistanceMetres, stats.ActualDistanceMetres, stats.ElevationGainMetres));
    }

    [Fact]
    public async Task Actual_distances_filled_in_after_completing_count()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        await SetRouteAsync(trip.Id, 1, 12_000m, null, null);
        await _app.Trips.CompleteTripAsync(trip.Id);

        await SetRouteAsync(trip.Id, 1, 12_000m, 13_500m, 800m);

        var stats = await StatsAsync(trip.Id);
        Assert.Equal((13_500m, 800m), (stats.ActualDistanceMetres, stats.ElevationGainMetres));
    }

    // ----- Pack weights -----

    [Fact]
    public async Task A_planned_trip_shows_its_starting_pack_and_base_weight_from_current_weights()
    {
        var (trip, tent) = await TripWithGearAsync();
        await _app.Trips.SetWaterCarriedAsync(trip.Id, 1000m);

        await _app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(tent.Name, null, 900m));

        var stats = await StatsAsync(trip.Id);
        Assert.Equal((2300m, 900m), (stats.StartingPackGrams, stats.BaseGrams));
    }

    [Fact]
    public async Task A_completed_trip_shows_its_frozen_starting_pack_and_base_weight()
    {
        var (trip, tent) = await TripWithGearAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        await _app.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails(tent.Name, null, 900m));

        var stats = await StatsAsync(trip.Id);
        Assert.Equal((1165m, 765m), (stats.StartingPackGrams, stats.BaseGrams));
    }

    [Fact]
    public async Task Listed_trips_carry_their_stats()
    {
        var (trip, _) = await TripWithGearAsync();
        await SetRouteAsync(trip.Id, 1, 12_000m, null, null);
        await _app.Trips.CompleteTripAsync(trip.Id);

        var listed = Assert.Single((await _app.Trips.ListTripsAsync()).Past);

        Assert.Equal((4, 12_000m, 1165m), (listed.Stats.Days, listed.Stats.PlannedDistanceMetres, listed.Stats.StartingPackGrams));
    }
}
