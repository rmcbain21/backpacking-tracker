namespace BackpackingTracker.Core.Gear;

/// <summary>
/// What removing a Category involves, so the UI can ask the right question:
/// a simple confirmation, or which Category its Gear Items should move to.
/// </summary>
public sealed record CategoryRemovalPlan(Category Category, int GearItemCount, IReadOnlyList<Category> MoveTargets)
{
    public bool NeedsMoveTarget => GearItemCount > 0;

    public string Message => GearItemCount switch
    {
        0 => $"Remove {Category.Name}?",
        1 => $"{Category.Name} has 1 Gear Item. Choose a Category to move it to.",
        _ => $"{Category.Name} has {GearItemCount} Gear Items. Choose a Category to move them to.",
    };
}
