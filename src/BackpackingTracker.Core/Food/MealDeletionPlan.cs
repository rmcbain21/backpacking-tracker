namespace BackpackingTracker.Core.Food;

/// <summary>Whether a Meal can be deleted, and what uses it if not.</summary>
public sealed record MealDeletionPlan(Meal Meal, IReadOnlyList<string> UsedBy) : DeletionPlan(UsedBy)
{
    protected override string Kind => "Meal";

    protected override string ItemName => Meal.Name;

    protected override bool ItemRetired => Meal.Retired;
}
