using System.Text.Json.Serialization;

namespace BackpackingTracker.Core.Gear;

public sealed record GearItem : StoredRecord
{
    /// <summary>What the backpacker calls it, usually the product, such as "SWD Long Haul 50 EPLX".</summary>
    public required string Name { get; init; }

    /// <summary>What kind of gear it is, such as Backpack or Tent; "" when not given.</summary>
    public string GearType { get; init; } = "";

    /// <summary>Measured weight in grams, to 0.1 g (ADR 0002).</summary>
    public required decimal WeightGrams { get; init; }

    /// <summary>Free-form labels used to find gear, such as "winter". Never affect weight.</summary>
    public IReadOnlyList<string> Tags { get; init; } = [];

    /// <summary>What the backpacker paid, if recorded.</summary>
    public decimal? Price { get; init; }

    /// <summary>Anything worth remembering, such as the manufacturer's stated weight.</summary>
    public string Notes { get; init; } = "";

    /// <summary>No longer owned: hidden from the catalogue and pickers, but still found by id.</summary>
    public bool Retired { get; init; }

    /// <summary>
    /// Brand/model from records saved before Gear Types (ticket 27). Only read, to convert old records:
    /// such a record's name becomes its Gear Type and this becomes its Name.
    /// </summary>
    [JsonPropertyName("brandModel")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyBrandModel { get; init; }

    /// <summary>
    /// The Category a Gear Item had before Categories moved onto Gear Lists (ADR 0003). Kept so existing
    /// List Entries can be put in the matching Category of their list (ticket 28).
    /// </summary>
    [JsonPropertyName("categoryId")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? LegacyCategoryId { get; init; }
}
