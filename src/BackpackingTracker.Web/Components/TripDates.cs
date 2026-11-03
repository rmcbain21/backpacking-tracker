using System.Globalization;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Web.Components;

/// <summary>
/// How a Trip's dates read on screen, such as "Jul 1 – Jul 4, 2026 · 4 days". An Open-ended Trip's end date is never
/// shown: it reads "Started Apr 15, 2026 · day 40 so far".
/// </summary>
public static class TripDates
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    public static string Format(Trip trip)
    {
        var days = trip.EndDate.DayNumber - trip.StartDate.DayNumber + 1;
        return trip.OpenEnded
            ? $"Started {Date(trip.StartDate)} · day {days} so far"
            : $"{Range(trip.StartDate, trip.EndDate)} · {(days == 1 ? "1 day" : $"{days} days")}";
    }

    /// <summary>A long date, such as "Sep 2, 2026", for a finish estimate.</summary>
    public static string Date(DateOnly date) => date.ToString("MMM d, yyyy", English);

    /// <summary>One day, such as "Wed, Jul 1".</summary>
    public static string Day(DateOnly date) => date.ToString("ddd, MMM d", English);

    private static string Range(DateOnly start, DateOnly end)
    {
        if (start == end)
        {
            return start.ToString("MMM d, yyyy", English);
        }

        var from = start.Year == end.Year ? start.ToString("MMM d", English) : start.ToString("MMM d, yyyy", English);
        return $"{from} – {end.ToString("MMM d, yyyy", English)}";
    }
}
