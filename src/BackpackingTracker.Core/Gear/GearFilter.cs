namespace BackpackingTracker.Core.Gear;

/// <summary>
/// Narrows the gear shown. SearchText matches part of a Gear Item's name, brand/model or Tags, ignoring
/// case; every word must match somewhere. CategoryId limits it to one Category. Null or blank means no limit.
/// </summary>
public sealed record GearFilter(string? SearchText = null, string? CategoryId = null)
{
    public bool IsEmpty => SearchWords.Count == 0 && OnlyCategoryId is null;

    /// <summary>The one Category to show, or null for all of them.</summary>
    public string? OnlyCategoryId => string.IsNullOrWhiteSpace(CategoryId) ? null : CategoryId;

    private IReadOnlyList<string> SearchWords =>
        (SearchText ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>True when every search word appears in the Gear Item's name, brand/model or one of its Tags.</summary>
    public bool Matches(GearItem item) =>
        SearchWords.All(word =>
            item.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
            || item.BrandModel.Contains(word, StringComparison.OrdinalIgnoreCase)
            || item.Tags.Any(tag => tag.Contains(word, StringComparison.OrdinalIgnoreCase)));
}
