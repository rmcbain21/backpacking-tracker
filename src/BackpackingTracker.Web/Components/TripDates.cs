using System.Globalization;
using BackpackingTracker.Core.Trips;

namespace BackpackingTracker.Web.Components;

/// <summary>How a Trip's dates read on screen, such as "Jul 1 – Jul 4, 2026 · 4 days".</summary>
public static class TripDates
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-US");

    public static string Format(Trip trip)
    {
        var days = trip.EndDate.DayNumber - trip.StartDate.DayNumber + 1;
        return $"{Range(trip.StartDate, trip.EndDate)} · {(days == 1 ? "1 day" : $"{days} days")}";
    }

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
