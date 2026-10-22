namespace BackpackingTracker.Core.Gear;

/// <summary>What the backpacker fills in to add a Gear Item.</summary>
public sealed record GearItemDetails(
    string Name,
    string? BrandModel,
    decimal WeightGrams,
    string CategoryId,
    IReadOnlyList<string>? Tags = null);
