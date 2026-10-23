using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Weight;

/// <summary>
/// A List Entry with its Gear Item's weight and Category looked up, ready to be totalled.
/// CategoryId is null when the Gear Item is gone (it then weighs nothing).
/// </summary>
public sealed record WeighedEntry(decimal ItemWeightGrams, int Quantity, CarryMode CarryMode, string? CategoryId = null);
