using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Units;

[JsonConverter(typeof(JsonStringEnumConverter<UnitSystem>))]
public enum UnitSystem
{
    Imperial,
    Metric,
}
