using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Weight;

/// <summary>A List Entry with its Gear Item's weight looked up, ready to be totalled.</summary>
public sealed record WeighedEntry(decimal ItemWeightGrams, int Quantity, CarryMode CarryMode);
