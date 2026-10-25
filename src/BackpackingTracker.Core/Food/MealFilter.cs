namespace BackpackingTracker.Core.Food;

/// <summary>
/// Narrows the Meals shown. SearchText matches part of a Meal's Name, Tags or the names of its Food Items,
/// ignoring case; every word must match somewhere. Null or blank means no limit.
/// </summary>
public sealed record MealFilter(string? SearchText = null)
{
    public bool IsEmpty => SearchWords.Count == 0;

    private IReadOnlyList<string> SearchWords =>
        (SearchText ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>True when every search word appears in the Meal.</summary>
    public bool Matches(MealView view) =>
        SearchWords.All(word =>
            view.Meal.Name.Contains(word, StringComparison.OrdinalIgnoreCase)
            || view.Meal.Tags.Any(tag => tag.Contains(word, StringComparison.OrdinalIgnoreCase))
            || view.Components.Any(c => c.FoodItem?.Name.Contains(word, StringComparison.OrdinalIgnoreCase) == true));
}
