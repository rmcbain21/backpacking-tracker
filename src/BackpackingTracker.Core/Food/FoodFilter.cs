namespace BackpackingTracker.Core.Food;

/// <summary>
/// Narrows the food shown. SearchText matches part of a Food Item's Name or Tags, ignoring case;
/// every word must match somewhere. Null or blank means no limit.
/// </summary>
public sealed record FoodFilter(string? SearchText = null)
{
    public bool IsEmpty => SearchWords.Count == 0;

    private IReadOnlyList<string> SearchWords =>
        (SearchText ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>True when every search word appears in the Food Item.</summary>
    public bool Matches(FoodItem item) =>
        SearchWords.All(word =>
            item.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
            || item.Tags.Any(tag => tag.Contains(word, StringComparison.OrdinalIgnoreCase)));
}
