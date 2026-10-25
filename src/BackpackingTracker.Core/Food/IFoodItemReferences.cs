namespace BackpackingTracker.Core.Food;

/// <summary>
/// An area of the app that can refer to Food Items, such as Meals or Meal Plans. The Food catalogue
/// asks every registered area before deleting a Food Item, so nothing is left pointing at nothing.
/// </summary>
public interface IFoodItemReferences
{
    /// <summary>What uses the Food Item, described for the backpacker (for example: Meal "Ramen Bomb").</summary>
    Task<IReadOnlyList<string>> FindUsesAsync(string foodItemId);
}
