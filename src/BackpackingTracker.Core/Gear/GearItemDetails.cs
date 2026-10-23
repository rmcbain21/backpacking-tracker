namespace BackpackingTracker.Core.Gear;

/// <summary>What the backpacker fills in to add or edit a Gear Item.</summary>
public sealed record GearItemDetails(
    string Name,
    string? GearType,
    decimal WeightGrams,
    IReadOnlyList<string>? Tags = null,
    decimal? Price = null,
    string? Notes = null);
