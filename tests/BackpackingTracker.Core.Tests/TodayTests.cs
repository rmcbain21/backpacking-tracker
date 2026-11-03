using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>What the Today tab works from while On Trail: today and tomorrow, the distance so far, and adding days up to today.</summary>
public class TodayTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on 10 July.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public TodayTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private Task<Trip> OpenEndedAsync(int startDay) => _app.Trips.CreateTripAsync("PCT", July(startDay), July(startDay), openEnded: true);

    [Fact]
    public async Task The_distance_so_far_counts_days_up_to_today_leaving_out_skipped_ones()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July(7), July(12));
        await _app.Trips.UpdateDayAsync(trip.Id, July(7), new TripDayDetails(ActualDistanceMetres: 20_000m));
        await _app.Trips.UpdateDayAsync(trip.Id, July(8), new TripDayDetails(ActualDistanceMetres: 0m));
        await _app.Trips.UpdateDayAsync(trip.Id, July(9), new TripDayDetails(ActualDistanceMetres: 5_000m));
        await _app.Trips.SetDaySkippedAsync(trip.Id, July(9), skipped: true);
        await _app.Trips.UpdateDayAsync(trip.Id, July(10), new TripDayDetails(ActualDistanceMetres: 16_000m));
        await _app.Trips.UpdateDayAsync(trip.Id, July(11), new TripDayDetails(PlannedDistanceMetres: 12_000m));

        var soFar = (await ViewAsync(trip.Id)).SoFar;

        Assert.Equal(new DistanceSoFar(36_000m, 3), soFar);
        Assert.Equal(12_000m, soFar.MetresPerDay);
    }

    [Fact]
    public async Task Tomorrow_is_the_next_trip_day_if_there_is_one()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July(9), July(11));
        await _app.Trips.UpdateDayAsync(trip.Id, July(11), new TripDayDetails(Start: "Tuolumne", End: "Glen Aulin"));

        Assert.Equal("Glen Aulin", (await ViewAsync(trip.Id)).TomorrowDay!.End);

        await _app.Trips.ChangeDatesAsync(trip.Id, July(9), July(10), removeDaysWithContent: true);
        Assert.Null((await ViewAsync(trip.Id)).TomorrowDay);
    }

    [Theory]
    [InlineData(8, 10, true)]
    [InlineData(8, 11, false)]
    public async Task The_last_day_is_today_on_a_trip_ending_today(int startDay, int endDay, bool lastDay)
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July(startDay), July(endDay));

        Assert.Equal(lastDay, (await ViewAsync(trip.Id)).IsLastDay);
    }

    [Fact]
    public async Task An_open_ended_trip_never_has_a_last_day()
    {
        var trip = await OpenEndedAsync(10);

        Assert.False((await ViewAsync(trip.Id)).IsLastDay);
    }

    [Fact]
    public async Task An_open_ended_trip_counts_the_days_missing_up_to_today()
    {
        var trip = await OpenEndedAsync(7);

        Assert.Equal(3, (await ViewAsync(trip.Id)).DaysToAddUpToToday);

        await _app.Trips.AddDaysUpToTodayAsync(trip.Id);
        Assert.Equal(0, (await ViewAsync(trip.Id)).DaysToAddUpToToday);
    }

    [Fact]
    public async Task Adding_days_up_to_today_adds_every_missing_date_in_order()
    {
        var trip = await OpenEndedAsync(7);

        var added = await _app.Trips.AddDaysUpToTodayAsync(trip.Id);

        Assert.Equal(3, added);
        var view = await ViewAsync(trip.Id);
        Assert.Equal([July(7), July(8), July(9), July(10)], view.Trip.Days.Select(day => day.Date));
        Assert.Equal(July(10), view.Trip.EndDate);
        Assert.NotNull(view.TodayDay);
    }

    [Fact]
    public async Task Adding_days_up_to_today_can_copy_the_meals_onto_each()
    {
        var trip = await OpenEndedAsync(8);
        var oats = await _app.Food.AddFoodItemAsync(new FoodItemDetails("Oatmeal", 50m, 200m));
        await _app.Trips.AddFoodItemToSlotAsync(trip.Id, July(8), MealSlot.Breakfast, oats.Id);

        await _app.Trips.AddDaysUpToTodayAsync(trip.Id, copyMealsFromPreviousDay: true);

        var days = (await ViewAsync(trip.Id)).Trip.Days;
        Assert.All(days, day => Assert.Equal(oats.Id, Assert.Single(day.SlotEntries).FoodItemId));
        Assert.Equal(3, days.Select(day => day.SlotEntries[0].Id).Distinct().Count());
    }

    [Fact]
    public async Task Adding_days_up_to_today_is_refused_once_today_is_there()
    {
        var trip = await OpenEndedAsync(10);

        var refusal = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.AddDaysUpToTodayAsync(trip.Id));

        Assert.Equal("The Trip already has a day for today.", refusal.Message);
    }

    [Fact]
    public async Task Only_an_open_ended_trip_adds_days_up_to_today()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind Rivers", July(7), July(8));

        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.AddDaysUpToTodayAsync(trip.Id));
    }
}
