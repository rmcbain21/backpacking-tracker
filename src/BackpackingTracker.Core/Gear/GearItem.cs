namespace BackpackingTracker.Core.Gear;

public sealed record GearItem : StoredRecord
{
    public required string Name { get; init; }

    public string BrandModel { get; init; } = "";

    /// <summary>Measured weight in grams, to 0.1 g (ADR 0002).</summary>
    public required decimal WeightGrams { get; init; }
}
