using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>Which Trips are On Trail, how the Trips list groups them, and which one gets the card at the top.</summary>
public class OnTrailTests
{
    private readonly InMemoryRecordStore _store = new();

    // Midday on 10 July.
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public OnTrailTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private static DateOnly July(int day) => new(2026, 7, day);

    private Task<Trip> TripAsync(string name, int startDay, int endDay) =>
        _app.Trips.CreateTripAsync(name, July(startDay), July(endDay));

    private async Task<TripView> ViewAsync(string tripId) =>
        await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.");

    private static string[] Names(IEnumerable<TripView> views) => views.Select(view => view.Trip.Name).ToArray();

    [Theory]
    [InlineData(8, 12, true)]
    [InlineData(10, 10, true)]
    [InlineData(10, 14, true)]
    [InlineData(6, 10, true)]
    [InlineData(11, 14, false)]
    [InlineData(5, 9, false)]
    public async Task A_planned_trip_is_on_trail_when_today_falls_within_its_dates(int startDay, int endDay, bool onTrail)
    {
        var view = await ViewAsync((await TripAsync("Trip", startDay, endDay)).Id);

        Assert.Equal(onTrail, view.OnTrail);
    }

    [Fact]
    public async Task A_completed_trip_is_never_on_trail()
    {
        var trip = await TripAsync("Wind Rivers", 8, 12);

        await _app.Trips.CompleteTripAsync(trip.Id);

        Assert.False((await ViewAsync(trip.Id)).OnTrail);
    }

    [Fact]
    public async Task An_open_ended_trip_is_on_trail_from_its_start_until_its_completed()
    {
        var trip = await _app.Trips.CreateTripAsync("PCT", July(1), July(1), openEnded: true);

        var view = await ViewAsync(trip.Id);

        Assert.True(view.OnTrail);
        Assert.False(view.Overdue);
        Assert.Equal(10, view.TodayNumber);
        Assert.Null(view.TodayDay);
    }

    [Fact]
    public async Task A_planned_trip_whose_end_date_has_passed_is_overdue()
    {
        var trip = await TripAsync("Wind Rivers", 5, 9);

        var view = await ViewAsync(trip.Id);

        Assert.True(view.Overdue);
        Assert.False(view.OnTrail);
    }

    [Fact]
    public async Task Today_is_counted_from_the_start_date()
    {
        var trip = await TripAsync("Wind Rivers", 8, 12);

        var view = await ViewAsync(trip.Id);

        Assert.Equal(3, view.TodayNumber);
        Assert.Equal(July(10), view.TodayDay!.Date);
    }

    [Fact]
    public async Task The_trips_list_groups_on_trail_then_upcoming_with_overdue_then_past()
    {
        await TripAsync("Overdue", 1, 3);
        await TripAsync("Later", 20, 22);
        await TripAsync("Long one", 5, 15);
        await TripAsync("Short one", 9, 11);
        var done = await TripAsync("Done", 2, 4);
        await _app.Trips.CompleteTripAsync(done.Id);

        var trips = await _app.Trips.ListTripsAsync();

        Assert.Equal(["Long one", "Short one"], Names(trips.OnTrail));
        Assert.Equal(["Overdue", "Later"], Names(trips.Upcoming));
        Assert.Equal(["Done"], Names(trips.Past));
    }

    [Fact]
    public async Task An_overdue_trip_is_featured_first_the_one_that_ended_earliest()
    {
        await TripAsync("On Trail", 9, 11);
        await TripAsync("Ended later", 1, 8);
        await TripAsync("Ended earlier", 2, 5);

        Assert.Equal("Ended earlier", (await _app.Trips.ListTripsAsync()).Featured!.Trip.Name);
    }

    [Fact]
    public async Task Otherwise_the_on_trail_trip_that_started_earliest_is_featured()
    {
        await TripAsync("Later", 20, 22);
        await TripAsync("Started later", 9, 11);
        await TripAsync("Started earlier", 7, 12);

        Assert.Equal("Started earlier", (await _app.Trips.ListTripsAsync()).Featured!.Trip.Name);
    }

    [Fact]
    public async Task Otherwise_the_next_trip_to_start_is_featured()
    {
        await TripAsync("August", 31, 31);
        await TripAsync("Soon", 12, 14);
        var done = await TripAsync("Done", 1, 3);
        await _app.Trips.CompleteTripAsync(done.Id);

        var featured = (await _app.Trips.ListTripsAsync()).Featured!;

        Assert.Equal("Soon", featured.Trip.Name);
        Assert.Equal(2, featured.DaysToStart);
    }

    [Fact]
    public async Task With_only_completed_trips_nothing_is_featured()
    {
        var done = await TripAsync("Done", 1, 3);
        await _app.Trips.CompleteTripAsync(done.Id);

        Assert.Null((await _app.Trips.ListTripsAsync()).Featured);
    }
}
