using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Core.Tests;

/// <summary>A Trip's Trip Days: one per calendar day, what's typed in on each, and changing the Trip's dates.</summary>
public class TripDayTests
{
    private static readonly DateOnly July1 = new(2026, 7, 1);
    private static readonly DateOnly July4 = new(2026, 7, 4);

    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly AppCore _app;

    public TripDayTests()
    {
        _app = new AppCore(_store, _clock);
    }

    private async Task<Trip> TripAsync(string tripId) =>
        (await _app.Trips.GetTripAsync(tripId) ?? throw new InvalidOperationException("No such Trip.")).Trip;

    private static DateOnly July(int day) => new(2026, 7, day);

    /// <summary>The same dates and the same Trip Days (records hold lists, so the Trips themselves never compare equal).</summary>
    private static void AssertUnchanged(Trip before, Trip after)
    {
        Assert.Equal((before.StartDate, before.EndDate), (after.StartDate, after.EndDate));
        Assert.Equal(before.Days, after.Days);
    }

    /// <summary>A 3-night Trip, July 1 to 4, with each day's campsite set to "Camp 1" to "Camp 4".</summary>
    private async Task<Trip> FourDaysWithCampsitesAsync()
    {
        var trip = await _app.Trips.CreateTripAsync("Wind River High Route", July1, July4);
        for (var day = 1; day <= 4; day++)
        {
            await _app.Trips.UpdateDayAsync(trip.Id, July(day), new TripDayDetails(Campsite: $"Camp {day}"));
        }

        return await TripAsync(trip.Id);
    }

    // ----- Trip Days from the dates -----

    [Theory]
    [InlineData(1, 4, 4)]
    [InlineData(1, 1, 1)]
    [InlineData(30, 31, 2)]
    public async Task A_trip_has_one_day_per_calendar_day_from_start_to_end(int startDay, int endDay, int expectedDays)
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July(startDay), July(endDay));

        var days = (await TripAsync(trip.Id)).Days;
        Assert.Equal(expectedDays, days.Count);
        Assert.Equal(Enumerable.Range(startDay, expectedDays).Select(July), days.Select(d => d.Date));
    }

    [Fact]
    public async Task A_trip_across_a_month_end_has_a_day_for_each_date()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", new DateOnly(2026, 6, 29), new DateOnly(2026, 7, 2));

        Assert.Equal(
            [new DateOnly(2026, 6, 29), new DateOnly(2026, 6, 30), July(1), July(2)],
            (await TripAsync(trip.Id)).Days.Select(d => d.Date));
    }

    [Fact]
    public async Task New_trip_days_are_empty()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);

        Assert.All((await TripAsync(trip.Id)).Days, day => Assert.False(day.HasContent()));
    }

    [Fact]
    public async Task A_trip_saved_before_trip_days_gets_empty_days_for_its_dates()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);
        await _store.SaveAsync("trips", trip with { Days = [] });

        var days = (await TripAsync(trip.Id)).Days;
        Assert.Equal([July(1), July(2), July(3), July(4)], days.Select(d => d.Date));
        Assert.All(days, day => Assert.False(day.HasContent()));
    }

    // ----- What's typed in on a day -----

    [Fact]
    public async Task A_day_holds_its_route_and_journal_notes()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);

        await _app.Trips.UpdateDayAsync(trip.Id, July(2), new TripDayDetails(
            " Big Sandy trailhead ", "Temple Lake", "Lake outlet", 13679.4m, 14500m, 716m, " Windy over the pass. "));

        var days = (await TripAsync(trip.Id)).Days;
        var day = days[1];
        Assert.Equal(July(2), day.Date);
        Assert.Equal(("Big Sandy trailhead", "Temple Lake", "Lake outlet"), (day.Start, day.End, day.Campsite));
        Assert.Equal((13679.4m, 14500m, 716m), (day.PlannedDistanceMetres, day.ActualDistanceMetres, day.ElevationGainMetres));
        Assert.Equal("Windy over the pass.", day.JournalNotes);
        Assert.True(day.HasContent());
        Assert.All(days.Where(d => d.Date != July(2)), d => Assert.False(d.HasContent()));
    }

    [Fact]
    public async Task Every_field_on_a_day_is_optional_and_blank_text_leaves_it_empty()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);
        await _app.Trips.UpdateDayAsync(trip.Id, July1, new TripDayDetails(Start: "Trailhead", PlannedDistanceMetres: 8000m));

        var cleared = await _app.Trips.UpdateDayAsync(trip.Id, July1, new TripDayDetails(Start: "  ", JournalNotes: ""));

        Assert.False(cleared.HasContent());
        Assert.False((await TripAsync(trip.Id)).Days[0].HasContent());
    }

    [Theory]
    [InlineData(-1, null, null, "A planned distance can't be negative.")]
    [InlineData(null, -1, null, "An actual distance can't be negative.")]
    [InlineData(null, null, -1, "An elevation gain can't be negative.")]
    public async Task Negative_distances_and_elevation_are_refused(
        int? planned, int? actual, int? elevation, string expected)
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.UpdateDayAsync(
            trip.Id, July1, new TripDayDetails(PlannedDistanceMetres: planned, ActualDistanceMetres: actual, ElevationGainMetres: elevation)));

        Assert.Equal(expected, refused.Message);
    }

    [Fact]
    public async Task A_date_outside_the_trip_is_refused()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.UpdateDayAsync(trip.Id, July(5), new TripDayDetails(Campsite: "Nowhere")));

        Assert.Equal("That day is no longer part of the Trip.", refused.Message);
    }

    [Fact]
    public async Task Day_edits_and_date_changes_on_a_missing_trip_are_refused()
    {
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() =>
            _app.Trips.UpdateDayAsync("missing", July1, new TripDayDetails()));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.PlanDateChangeAsync("missing", July1, July4));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ChangeDatesAsync("missing", July1, July4));
        await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.MoveTripAsync("missing", July1));

        Assert.Equal("That Trip no longer exists.", refused.Message);
    }

    // ----- The date-change preview -----

    [Fact]
    public async Task The_preview_of_an_earlier_end_lists_the_removed_days_and_whether_each_has_content()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);
        await _app.Trips.UpdateDayAsync(trip.Id, July(4), new TripDayDetails(JournalNotes: "Hiked out."));

        var plan = await _app.Trips.PlanDateChangeAsync(trip.Id, July1, July(2));

        Assert.Equal([(3, July(3), false), (4, July(4), true)], plan.RemovedDays.Select(d => (d.DayNumber, d.Day.Date, d.HasContent)));
        Assert.Empty(plan.AddedDates);
        Assert.True(plan.NeedsConfirmation);
    }

    [Fact]
    public async Task The_preview_of_a_later_start_lists_the_first_days_as_removed()
    {
        var trip = await FourDaysWithCampsitesAsync();

        var plan = await _app.Trips.PlanDateChangeAsync(trip.Id, July(2), July4);

        Assert.Equal([(1, July1, true)], plan.RemovedDays.Select(d => (d.DayNumber, d.Day.Date, d.HasContent)));
        Assert.True(plan.NeedsConfirmation);
    }

    [Fact]
    public async Task Removing_only_empty_days_needs_no_confirmation()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);
        await _app.Trips.UpdateDayAsync(trip.Id, July1, new TripDayDetails(Campsite: "Camp 1"));

        var plan = await _app.Trips.PlanDateChangeAsync(trip.Id, July1, July(2));

        Assert.Equal(2, plan.RemovedDays.Count);
        Assert.False(plan.NeedsConfirmation);
    }

    [Fact]
    public async Task The_preview_of_a_longer_trip_lists_the_added_dates_and_removes_nothing()
    {
        var trip = await FourDaysWithCampsitesAsync();

        var plan = await _app.Trips.PlanDateChangeAsync(trip.Id, new DateOnly(2026, 6, 30), July(5));

        Assert.Equal([new DateOnly(2026, 6, 30), July(5)], plan.AddedDates);
        Assert.Empty(plan.RemovedDays);
        Assert.False(plan.NeedsConfirmation);
    }

    [Fact]
    public async Task The_preview_changes_nothing()
    {
        var trip = await FourDaysWithCampsitesAsync();

        await _app.Trips.PlanDateChangeAsync(trip.Id, July1, July1);

        AssertUnchanged(trip, await TripAsync(trip.Id));
    }

    [Fact]
    public async Task New_dates_ending_before_they_start_are_refused()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);

        var preview = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.PlanDateChangeAsync(trip.Id, July4, July1));
        var change = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ChangeDatesAsync(trip.Id, July4, July1));

        Assert.Equal("The Trip can't end before it starts.", preview.Message);
        Assert.Equal(preview.Message, change.Message);
        Assert.Equal((July1, July4), ((await TripAsync(trip.Id)).StartDate, (await TripAsync(trip.Id)).EndDate));
    }

    // ----- Changing the start or end date: each day stays on its date -----

    [Fact]
    public async Task A_later_end_keeps_every_day_and_adds_empty_days_at_the_end()
    {
        var trip = await FourDaysWithCampsitesAsync();

        await _app.Trips.ChangeDatesAsync(trip.Id, July1, July(6));

        var changed = await TripAsync(trip.Id);
        Assert.Equal((July1, July(6)), (changed.StartDate, changed.EndDate));
        Assert.Equal(Enumerable.Range(1, 6).Select(July), changed.Days.Select(d => d.Date));
        Assert.Equal(["Camp 1", "Camp 2", "Camp 3", "Camp 4", null, null], changed.Days.Select(d => d.Campsite));
    }

    [Fact]
    public async Task An_earlier_start_adds_an_empty_day_at_the_start()
    {
        var trip = await FourDaysWithCampsitesAsync();
        var june30 = new DateOnly(2026, 6, 30);

        await _app.Trips.ChangeDatesAsync(trip.Id, june30, July4);

        Assert.Equal(
            [(june30, null), (July1, "Camp 1"), (July(2), "Camp 2"), (July(3), "Camp 3"), (July4, "Camp 4")],
            (await TripAsync(trip.Id)).Days.Select(d => (d.Date, d.Campsite)));
    }

    [Fact]
    public async Task An_earlier_end_removes_days_from_the_end()
    {
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);
        await _app.Trips.UpdateDayAsync(trip.Id, July1, new TripDayDetails(Campsite: "Camp 1"));

        await _app.Trips.ChangeDatesAsync(trip.Id, July1, July(2));

        var changed = await TripAsync(trip.Id);
        Assert.Equal((July1, July(2)), (changed.StartDate, changed.EndDate));
        Assert.Equal([(July1, "Camp 1"), (July(2), null)], changed.Days.Select(d => (d.Date, d.Campsite)));
    }

    [Fact]
    public async Task A_later_start_removes_the_first_days_and_keeps_the_rest_on_their_dates()
    {
        var trip = await FourDaysWithCampsitesAsync();

        await _app.Trips.ChangeDatesAsync(trip.Id, July(2), July4, removeDaysWithContent: true);

        var changed = await TripAsync(trip.Id);
        Assert.Equal((July(2), July4), (changed.StartDate, changed.EndDate));
        Assert.Equal([(July(2), "Camp 2"), (July(3), "Camp 3"), (July4, "Camp 4")], changed.Days.Select(d => (d.Date, d.Campsite)));
    }

    [Fact]
    public async Task Removing_days_with_content_is_refused_without_confirmation()
    {
        var trip = await FourDaysWithCampsitesAsync();

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => _app.Trips.ChangeDatesAsync(trip.Id, July1, July(2)));

        Assert.Equal("Days 3 and 4 would be removed, with what's filled in. Confirm to remove.", refused.Message);
        AssertUnchanged(trip, await TripAsync(trip.Id));
    }

    [Fact]
    public async Task Removing_days_with_content_goes_ahead_when_confirmed()
    {
        var trip = await FourDaysWithCampsitesAsync();

        await _app.Trips.ChangeDatesAsync(trip.Id, July1, July(2), removeDaysWithContent: true);

        var changed = await TripAsync(trip.Id);
        Assert.Equal([(July1, "Camp 1"), (July(2), "Camp 2")], changed.Days.Select(d => (d.Date, d.Campsite)));
    }

    [Fact]
    public async Task Changing_both_dates_keeps_the_days_still_inside_them()
    {
        var trip = await FourDaysWithCampsitesAsync();

        await _app.Trips.ChangeDatesAsync(trip.Id, July(3), July(6), removeDaysWithContent: true);

        Assert.Equal(
            [(July(3), "Camp 3"), (July4, "Camp 4"), (July(5), null), (July(6), null)],
            (await TripAsync(trip.Id)).Days.Select(d => (d.Date, d.Campsite)));
    }

    // ----- Moving the whole Trip: every day moves with it -----

    [Fact]
    public async Task Moving_the_trip_keeps_its_length_and_moves_every_day_in_order()
    {
        var trip = await FourDaysWithCampsitesAsync();

        await _app.Trips.MoveTripAsync(trip.Id, July(8));

        var moved = await TripAsync(trip.Id);
        Assert.Equal((July(8), July(11)), (moved.StartDate, moved.EndDate));
        Assert.Equal(
            [(July(8), "Camp 1"), (July(9), "Camp 2"), (July(10), "Camp 3"), (July(11), "Camp 4")],
            moved.Days.Select(d => (d.Date, d.Campsite)));
    }

    [Fact]
    public async Task Moving_the_trip_by_a_day_keeps_every_day_rather_than_dropping_one()
    {
        var trip = await FourDaysWithCampsitesAsync();

        await _app.Trips.MoveTripAsync(trip.Id, July(2));

        Assert.Equal(
            [(July(2), "Camp 1"), (July(3), "Camp 2"), (July4, "Camp 3"), (July(5), "Camp 4")],
            (await TripAsync(trip.Id)).Days.Select(d => (d.Date, d.Campsite)));
    }

    [Fact]
    public async Task Date_changes_keep_the_packing_list()
    {
        var tent = await _app.Gear.AddGearItemAsync(new Gear.GearItemDetails("Tent", null, 765m));
        var trip = await _app.Trips.CreateTripAsync("Trip", July1, July4);
        await _app.Trips.AddEntryAsync(trip.Id, tent.Id, trip.PackingList.Categories[0].Id);

        await _app.Trips.ChangeDatesAsync(trip.Id, July(2), July(3));
        await _app.Trips.MoveTripAsync(trip.Id, July(9));

        Assert.Equal([tent.Id], (await TripAsync(trip.Id)).PackingList.Entries.Select(e => e.GearItemId));
    }
}

