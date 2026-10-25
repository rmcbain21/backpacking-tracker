namespace BackpackingTracker.Core.Food;

/// <summary>Whether a Food Item can be deleted, and what uses it if not.</summary>
public sealed record FoodItemDeletionPlan(FoodItem FoodItem, IReadOnlyList<string> UsedBy) : DeletionPlan(UsedBy)
{
    protected override string Kind => "Food Item";

    protected override string ItemName => FoodItem.Name;

    protected override bool ItemRetired => FoodItem.Retired;
}
