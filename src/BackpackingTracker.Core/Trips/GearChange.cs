using System.Text.Json.Serialization;
using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Trips;

/// <summary>What a Gear Change does to the gear carried. Stored by name.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<GearChangeKind>))]
public enum GearChangeKind
{
    /// <summary>Pick up a Gear Item, such as a bear canister.</summary>
    Add,

    /// <summary>Send a Gear Item home, such as the puffy.</summary>
    Remove,

    /// <summary>Carry a different number of a Gear Item, such as Fuel canister ×1 → ×2.</summary>
    ChangeQuantity,
}

/// <summary>
/// An add, removal or quantity change to the backpacker's gear at a Resupply, so later Food Carries carry different gear
/// from the trailhead Packing List (which stays as it is). Kept on its Resupply day, in the order made; at most one per
/// Gear Item there.
/// </summary>
public sealed record GearChange
{
    public required string Id { get; init; }

    public required GearChangeKind Kind { get; init; }

    public required string GearItemId { get; init; }

    /// <summary>On an Add, how many are picked up; on a ChangeQuantity, how many are carried from here; 0 on a Remove.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int Quantity { get; init; }

    /// <summary>On an Add, how the picked-up gear is carried.</summary>
    public CarryMode CarryMode { get; init; } = CarryMode.Base;

    /// <summary>On an Add, which of the Packing List's Categories the gear goes under; null otherwise.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? CategoryId { get; init; }

    /// <summary>Ticked on the checklist once picked up, sent home or changed.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public bool Done { get; init; }

    /// <summary>On an Add, the Gear Item's weight in grams when the Trip was Completed; null on a Planned Trip.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? FrozenWeightGrams { get; init; }
}
