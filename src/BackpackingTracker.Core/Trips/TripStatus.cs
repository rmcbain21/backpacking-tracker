using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Trips;

/// <summary>Where a Trip is in its life. Planned Trips are upcoming; Completed Trips are past.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<TripStatus>))]
public enum TripStatus
{
    Planned,
    Completed,
}
