namespace BackpackingTracker.Core.Gear;

public sealed record GearItem : StoredRecord
{
    public required string Name { get; init; }

    public string BrandModel { get; init; } = "";

    /// <summary>Measured weight in grams, to 0.1 g (ADR 0002).</summary>
    public required decimal WeightGrams { get; init; }

    public string CategoryId { get; init; } = "";

    /// <summary>Free-form labels used to find gear, such as "winter". Never affect weight.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>What the backpacker paid, if recorded.</summary>
    public decimal? Price { get; init; }

    /// <summary>Anything worth remembering, such as the manufacturer's stated weight.</summary>
    public string Notes { get; init; } = "";

    /// <summary>No longer owned: hidden from the catalogue and pickers, but still found by id.</summary>
    public bool Retired { get; init; }
}
