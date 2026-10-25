namespace BackpackingTracker.Core.Food;

/// <summary>
/// An area of the app that can refer to Meals, such as Meal Plans. The Meal catalogue asks every
/// registered area before deleting a Meal, so nothing is left pointing at nothing.
/// </summary>
public interface IMealReferences
{
    /// <summary>What uses the Meal, described for the backpacker (for example: Trip "Wind Rivers").</summary>
    Task<IReadOnlyList<string>> FindUsesAsync(string mealId);
}
