namespace BackpackingTracker.Core.Gear;

/// <summary>
/// Narrows the gear shown. SearchText matches part of a Gear Item's Name, Gear Type or Tags, ignoring
/// case; every word must match somewhere. GearType limits it to one Gear Type, ignoring case.
/// Null or blank means no limit.
/// </summary>
public sealed record GearFilter(string? SearchText = null, string? GearType = null)
{
    public bool IsEmpty => SearchWords.Count == 0 && OnlyGearType is null;

    /// <summary>The one Gear Type to show, or null for all of them.</summary>
    public string? OnlyGearType => string.IsNullOrWhiteSpace(GearType) ? null : GearType.Trim();

    private IReadOnlyList<string> SearchWords =>
        (SearchText ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>True when the Gear Item is of the chosen Gear Type and every search word appears in it.</summary>
    public bool Matches(GearItem item) =>
        (OnlyGearType is null || string.Equals(item.GearType, OnlyGearType, StringComparison.OrdinalIgnoreCase))
        && SearchWords.All(word =>
            item.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
            || item.GearType.Contains(word, StringComparison.OrdinalIgnoreCase)
            || item.Tags.Any(tag => tag.Contains(word, StringComparison.OrdinalIgnoreCase)));
}
