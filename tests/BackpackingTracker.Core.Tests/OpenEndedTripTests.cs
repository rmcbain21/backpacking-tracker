using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Open-ended Trips, whose Trip Days are added one at a time, and progress against an Expected Distance.</summary>
public class OpenEndedTripTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on the PCT's start date, so the Trip has started.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 4, 15, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public OpenEndedTripTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly April(int day) => new(2026, 4, day);

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private Task<Trip> PctAsync() => _app.Trips.CreateTripAsync("PCT", April(15), April(15), openEnded: true);

    /// <summary>An Open-ended Trip of that many days so far, each with the actual distance in metres (null for none).</summary>
    private async Task<Trip> HikedAsync(params decimal?[] actualMetres)
    {
        var trip = await PctAsync();
        for (var i = 1; i < actualMetres.Length; i++)
        {
            await _app.Trips.AddDayAsync(trip.Id);
        }

        for (var i = 0; i < actualMetres.Length; i++)
        {
            await _app.Trips.UpdateDayAsync(trip.Id, April(15 + i), new TripDayDetails(ActualDistanceMetres: actualMetres[i]));
        }

        return trip;
    }

    [Fact]
    public async Task An_open_ended_trip_starts_with_just_day_one_whatever_end_date_is_given()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", April(15), April(30), openEnded: true);

        Assert.True(trip.OpenEnded);
        Assert.Equal(April(15), trip.EndDate);
        Assert.Equal([April(15)], trip.Days.Select(day => day.Date));
    }

    [Fact]
    public async Task Adding_a_day_adds_the_next_date_and_moves_the_end_date()
    {
        var trip = await PctAsync();

        var added = await _app.Trips.AddDayAsync(trip.Id);
        await _app.Trips.AddDayAsync(trip.Id);

        Assert.Equal(April(16), added.Date);
        var saved = (await ViewAsync(trip.Id)).Trip;
        Assert.Equal(April(17), saved.EndDate);
        Assert.Equal([April(15), April(16), April(17)], saved.Days.Select(day => day.Date));
    }

    [Fact]
    public async Task An_added_day_starts_empty_unless_it_copies_the_previous_days_meals()
    {
        var trip = await PctAsync();
        var oats = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Oatmeal", 50m, 200m));
        var first = await _app.Trips.AddFoodItemToSlotAsync(trip.Id, April(15), MealSlot.Breakfast, oats.Id, 2m);

        var empty = await _app.Trips.AddDayAsync(trip.Id);
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, April(16), MealSlot.Breakfast, oats.Id, 2m);
        var copied = await _app.Trips.AddDayAsync(trip.Id, copyMealsFromPreviousDay: true);

        Assert.Empty(empty.SlotEntries);
        var entry = Assert.Single(copied.SlotEntries);
        Assert.Equal((MealSlot.Breakfast, oats.Id, 2m), (entry.Slot, entry.FoodItemId, entry.Quantity));
        Assert.NotEqual(first.Id, entry.Id);
        Assert.Equal(entry, Assert.Single((await ViewAsync(trip.Id)).Trip.Days[2].SlotEntries));
    }

    [Fact]
    public async Task Only_an_open_ended_trip_adds_days_one_at_a_time()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", April(15), April(18));

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.AddDayAsync(trip.Id));
    }

    [Fact]
    public async Task Removing_the_last_day_moves_the_end_date_back()
    {
        var trip = await PctAsync();
        await _app.Trips.AddDayAsync(trip.Id);

        await _app.Trips.RemoveLastDayAsync(trip.Id);

        var saved = (await ViewAsync(trip.Id)).Trip;
        Assert.Equal(April(15), saved.EndDate);
        Assert.Single(saved.Days);
    }

    [Fact]
    public async Task Day_one_cant_be_removed()
    {
        var trip = await PctAsync();

        var refusal = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.RemoveLastDayAsync(trip.Id));

        Assert.Equal("Day 1 can't be removed.", refusal.Message);
    }

    [Fact]
    public async Task Removing_a_last_day_with_content_needs_confirmation()
    {
        var trip = await PctAsync();
        await _app.Trips.AddDayAsync(trip.Id);
        await _app.Trips.UpdateDayAsync(trip.Id, April(16), new TripDayDetails(JournalNotes: "Hot."));

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.RemoveLastDayAsync(trip.Id));
        Assert.Equal(2, (await ViewAsync(trip.Id)).Trip.Days.Count);

        await _app.Trips.RemoveLastDayAsync(trip.Id, removeDayWithContent: true);
        Assert.Single((await ViewAsync(trip.Id)).Trip.Days);
    }

    [Fact]
    public async Task Turning_open_ended_off_keeps_the_last_day_as_the_end_date()
    {
        var trip = await PctAsync();
        await _app.Trips.AddDayAsync(trip.Id);

        await _app.Trips.SetOpenEndedAsync(trip.Id, false);

        var saved = (await ViewAsync(trip.Id)).Trip;
        Assert.False(saved.OpenEnded);
        Assert.Equal(April(16), saved.EndDate);
        Assert.Equal(2, saved.Days.Count);
    }

    [Fact]
    public async Task A_trip_with_an_end_date_can_be_made_open_ended()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", April(15), April(18));

        await _app.Trips.SetOpenEndedAsync(trip.Id, true);
        await _app.Trips.AddDayAsync(trip.Id);

        var saved = (await ViewAsync(trip.Id)).Trip;
        Assert.True(saved.OpenEnded);
        Assert.Equal(April(19), saved.EndDate);
    }

    [Fact]
    public async Task Completing_an_open_ended_trip_ends_it_on_its_last_day_and_reopening_leaves_it_ended()
    {
        var trip = await PctAsync();
        await _app.Trips.AddDayAsync(trip.Id);

        await _app.Trips.CompleteTripAsync(trip.Id);
        Assert.False((await ViewAsync(trip.Id)).Trip.OpenEnded);

        await _app.Trips.ReopenTripAsync(trip.Id);
        var saved = (await ViewAsync(trip.Id)).Trip;
        Assert.False(saved.OpenEnded);
        Assert.Equal(April(16), saved.EndDate);
    }

    [Fact]
    public async Task A_completed_trip_cant_be_made_open_ended()
    {
        var trip = await PctAsync();
        await _app.Trips.CompleteTripAsync(trip.Id);

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetOpenEndedAsync(trip.Id, true));
    }

    [Fact]
    public async Task Progress_counts_only_the_actual_distance_against_the_expected_distance()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", April(15), April(17));
        await _app.Trips.UpdateDayAsync(trip.Id, April(15), new TripDayDetails(PlannedDistanceMetres: 50_000m, ActualDistanceMetres: 10_000m));
        await _app.Trips.UpdateDayAsync(trip.Id, April(16), new TripDayDetails(PlannedDistanceMetres: 50_000m));

        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 40_000m);

        var progress = (await ViewAsync(trip.Id)).Stats.Progress!;
        Assert.Equal((10_000m, 40_000m, 30_000m, 25), (progress.ActualMetres, progress.ExpectedMetres, progress.RemainingMetres, progress.Percent));
    }

    [Fact]
    public async Task There_is_no_progress_without_an_expected_distance_and_zero_clears_it()
    {
        var trip = await PctAsync();
        Assert.Null((await ViewAsync(trip.Id)).Stats.Progress);

        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 4_264_000m);
        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 0m);

        Assert.Null((await ViewAsync(trip.Id)).Trip.ExpectedDistanceMetres);
        Assert.Null((await ViewAsync(trip.Id)).Stats.Progress);
    }

    [Fact]
    public async Task An_expected_distance_cant_be_negative()
    {
        var trip = await PctAsync();

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.SetExpectedDistanceAsync(trip.Id, -1m));
    }

    [Fact]
    public async Task Going_further_than_expected_leaves_nothing_to_go()
    {
        var trip = await HikedAsync(30_000m, 30_000m, 30_000m);
        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 60_000m);

        var stats = (await ViewAsync(trip.Id)).Stats;

        Assert.Equal((0m, 150), (stats.Progress!.RemainingMetres, stats.Progress.Percent));
        Assert.Null(stats.FinishEstimate);
    }

    [Fact]
    public async Task The_finish_estimate_uses_the_pace_over_every_day_hiked()
    {
        // 60 km over 3 days is 20 km a day; 100 km to go is 5 more days after day 3, 17 Apr.
        var trip = await HikedAsync(25_000m, 15_000m, 20_000m);
        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 160_000m);

        var estimate = (await ViewAsync(trip.Id)).Stats.FinishEstimate;

        Assert.Equal(new FinishEstimate(April(22), 5), estimate);
    }

    [Fact]
    public async Task Zero_days_pull_the_pace_down_and_skipped_days_dont_count()
    {
        // 60 km over 4 days hiked (one a zero day) is 15 km a day; 40 km to go rounds up to 3 days after 19 Apr.
        var trip = await HikedAsync(30_000m, 0m, 30_000m, null, 0m);
        await _app.Trips.SetDaySkippedAsync(trip.Id, April(18), skipped: true);
        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 100_000m);

        var estimate = (await ViewAsync(trip.Id)).Stats.FinishEstimate;

        Assert.Equal(new FinishEstimate(April(22), 3), estimate);
    }

    [Fact]
    public async Task There_is_no_finish_estimate_until_three_days_have_an_actual_distance()
    {
        var trip = await HikedAsync(20_000m, 20_000m, null);
        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 4_264_000m);

        Assert.Null((await ViewAsync(trip.Id)).Stats.FinishEstimate);
    }

    [Fact]
    public async Task Only_an_open_ended_trip_gets_a_finish_estimate()
    {
        var trip = await HikedAsync(20_000m, 20_000m, 20_000m);
        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 4_264_000m);
        await _app.Trips.SetOpenEndedAsync(trip.Id, false);

        var stats = (await ViewAsync(trip.Id)).Stats;

        Assert.NotNull(stats.Progress);
        Assert.Null(stats.FinishEstimate);
    }

    [Fact]
    public async Task The_open_ended_flag_and_expected_distance_go_through_export_and_import()
    {
        var trip = await PctAsync();
        await _app.Trips.SetExpectedDistanceAsync(trip.Id, 4_264_000m);
        var laptop = new AppCore(new InMemoryRecordStore(), _clock);

        await laptop.Backup.ImportAsync(await _app.Backup.ExportAsync());

        var copy = (await laptop.Trips.GetTripAsync(trip.Id))!.Trip;
        Assert.Equal((true, 4_264_000m), (copy.OpenEnded, copy.ExpectedDistanceMetres));
    }
}
