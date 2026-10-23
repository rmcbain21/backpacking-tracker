using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Gear;

/// <summary>How a List Entry is carried on that list. Belongs to the List Entry, not the Gear Item.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<CarryMode>))]
public enum CarryMode
{
    Base,
    Worn,
    Consumable,
}
