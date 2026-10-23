namespace BackpackingTracker.Core.Weight;

/// <summary>The weight figures for a Gear List or Trip, in grams.</summary>
public sealed record PackWeight
{
    public required decimal BaseGrams { get; init; }

    public required decimal WornGrams { get; init; }

    public required decimal ConsumableGrams { get; init; }

    /// <summary>Base Weight plus Consumable Weight: everything in the pack at the trailhead.</summary>
    public decimal StartingPackGrams => BaseGrams + ConsumableGrams;

    /// <summary>How the Starting Pack Weight splits across Categories, in Category order. Worn entries are left out.</summary>
    public IReadOnlyList<CategoryWeight> ByCategory { get; init; } = [];
}
