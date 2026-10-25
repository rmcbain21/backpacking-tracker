using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Trips;

/// <summary>The four parts of a Trip Day's Meal Plan, in the order they're eaten. Stored by name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MealSlot>))]
public enum MealSlot
{
    Breakfast,
    Lunch,
    Dinner,
    Snacks,
}
