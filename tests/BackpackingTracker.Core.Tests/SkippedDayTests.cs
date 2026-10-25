using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Marking a Trip Day Skipped once the Trip is under way: the plan is kept, and stats count what was hiked.</summary>
public class SkippedDayTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on the Trip's first day, so it has started.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 1, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public SkippedDayTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private async Task<TripDay> DayAsync(string tripId, int day) => (await ViewAsync(tripId)).Trip.Days[day - 1];

    /// <summary>A July 1–4 Trip, each day with its route figures: 10 km planned, 9 km hiked, 500 m of climbing.</summary>
    private async Task<Trip> FourDayTripAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July(1), July(4));
        for (var day = 1; day <= 4; day++)
        {
            await _app.Trips.UpdateDayAsync(trip.Id, July(day), new TripDayDetails(
                Start: $"Camp {day - 1}", End: $"Camp {day}", PlannedDistanceMetres: 10_000m, ActualDistanceMetres: 9_000m,
                ElevationGainMetres: 500m, JournalNotes: $"Day {day}"));
        }

        return trip;
    }

    [Fact]
    public async Task A_skipped_day_keeps_its_route_notes_and_meals()
    {
        var trip = await FourDayTripAsync();
        var oats = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Oatmeal", 50m, 200m));
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(1), MealSlot.Breakfast, oats.Id);
        var before = await DayAsync(trip.Id, 1);

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);

        var day = await DayAsync(trip.Id, 1);
        Assert.True(day.Skipped);
        Assert.Equal(before with { Skipped = true }, day);
        Assert.Equal(July(1), (await ViewAsync(trip.Id)).Trip.StartDate);
    }

    [Fact]
    public async Task A_skipped_day_can_be_unmarked()
    {
        var trip = await FourDayTripAsync();
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: false);

        Assert.False((await DayAsync(trip.Id, 1)).Skipped);
    }

    [Fact]
    public async Task Editing_a_skipped_days_route_keeps_it_skipped()
    {
        var trip = await FourDayTripAsync();
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);

        await _app.Trips.UpdateDayAsync(trip.Id, July(1), new TripDayDetails(JournalNotes: "Arrived a day late: flight delayed."));

        Assert.True((await DayAsync(trip.Id, 1)).Skipped);
    }

    [Fact]
    public async Task Days_can_be_skipped_only_once_the_trip_has_started()
    {
        _clock.UtcNow = new DateTimeOffset(2026, 6, 30, 12, 0, 0, TimeSpan.Zero);
        var trip = await FourDayTripAsync();

        Assert.False((await ViewAsync(trip.Id)).HasStarted);
        var refusal = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetDaySkippedAsync(trip.Id, July(2), skipped: true));
        Assert.Contains("started", refusal.Message);

        _clock.UtcNow = _clock.UtcNow.AddDays(1);
        Assert.True((await ViewAsync(trip.Id)).HasStarted);
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(2), skipped: true);
        Assert.True((await DayAsync(trip.Id, 2)).Skipped);
    }

    [Fact]
    public async Task A_day_can_be_unmarked_even_before_the_trip_starts()
    {
        var trip = await FourDayTripAsync();
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);
        await _app.Trips.MoveTripAsync(trip.Id, July(20));

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(20), skipped: false);

        Assert.False((await DayAsync(trip.Id, 1)).Skipped);
    }

    [Fact]
    public async Task Stats_count_the_days_hiked_and_the_days_planned()
    {
        var trip = await FourDayTripAsync();

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);

        var stats = (await ViewAsync(trip.Id)).Stats;
        Assert.Equal((3, 2), (stats.Days, stats.Nights));
        Assert.Equal((4, 3), (stats.PlannedDays, stats.PlannedNights));
    }

    [Fact]
    public async Task With_no_skipped_days_hiked_and_planned_counts_are_the_same()
    {
        var trip = await FourDayTripAsync();

        var stats = (await ViewAsync(trip.Id)).Stats;

        Assert.Equal((4, 3, 4, 3), (stats.Days, stats.Nights, stats.PlannedDays, stats.PlannedNights));
    }

    [Fact]
    public async Task Hiked_nights_never_go_below_zero()
    {
        var trip = await _app.Trips.CreateTripAsync("Overnighter", July(1), July(2));
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(2), skipped: true);

        var stats = (await ViewAsync(trip.Id)).Stats;

        Assert.Equal((0, 0), (stats.Days, stats.Nights));
    }

    [Fact]
    public async Task Actual_distance_and_climbing_leave_out_skipped_days_but_planned_distance_keeps_them()
    {
        var trip = await FourDayTripAsync();

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);

        var stats = (await ViewAsync(trip.Id)).Stats;
        Assert.Equal(40_000m, stats.PlannedDistanceMetres);
        Assert.Equal(27_000m, stats.ActualDistanceMetres);
        Assert.Equal(1_500m, stats.ElevationGainMetres);
    }

    [Fact]
    public async Task A_skipped_days_food_still_counts_in_the_pack_and_calories_per_day_still_divide_by_every_day()
    {
        var trip = await FourDayTripAsync();
        var oats = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Oatmeal", 100m, 400m));
        for (var day = 1; day <= 4; day++)
        {
            await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(day), MealSlot.Breakfast, oats.Id);
        }

        var before = (await ViewAsync(trip.Id)).Stats;

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);

        var after = (await ViewAsync(trip.Id)).Stats;
        Assert.Equal(before.StartingPackGrams, after.StartingPackGrams);
        Assert.Equal(400m, after.CaloriesPerDay);
    }

    [Fact]
    public async Task Skipped_can_be_set_and_cleared_on_a_completed_trip()
    {
        var trip = await FourDayTripAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(3), skipped: true);
        Assert.True((await DayAsync(trip.Id, 3)).Skipped);

        await _app.Trips.SetDaySkippedAsync(trip.Id, July(3), skipped: false);
        Assert.False((await DayAsync(trip.Id, 3)).Skipped);
    }

    [Fact]
    public async Task Removing_a_skipped_day_with_nothing_else_on_it_still_asks_first()
    {
        var trip = await _app.Trips.CreateTripAsync("Overnighter", July(1), July(3));
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(3), skipped: true);

        var plan = await _app.Trips.PlanDateChangeAsync(trip.Id, July(1), July(2));

        Assert.True(plan.NeedsConfirmation);
    }

    [Fact]
    public async Task A_sync_conflict_on_a_trip_shows_which_days_are_skipped()
    {
        var drive = new InMemoryCloudFileStore();
        var phone = new AppCore(new InMemoryRecordStore(), _clock, drive);
        var laptop = new AppCore(new InMemoryRecordStore(), _clock, drive);
        await phone.Sync.ConnectAsync();
        await laptop.Sync.ConnectAsync();
        var trip = await phone.Trips.CreateTripAsync("Wind Rivers", July(1), July(2));
        await phone.Sync.SyncNowAsync();
        await laptop.Sync.SyncNowAsync();
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);
        await phone.Trips.SetDaySkippedAsync(trip.Id, July(1), skipped: true);
        _clock.UtcNow = _clock.UtcNow.AddMinutes(5);
        await laptop.Trips.UpdateDayAsync(trip.Id, July(1), new TripDayDetails(Campsite: "Island Lake"));

        await phone.Sync.SyncNowAsync();
        await laptop.Sync.SyncNowAsync();

        var conflict = Assert.Single(await laptop.Sync.ListConflictsAsync());
        var skipped = conflict.Lines.Single(line => line.Label == "Day 1 skipped");
        Assert.Equal(("No", "Yes"), (skipped.ThisDevice, skipped.OtherDevice));
    }
}
