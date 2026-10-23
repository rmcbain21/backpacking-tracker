namespace BackpackingTracker.Core.Lists;

/// <summary>
/// What removing a Category from a Gear List involves, so the UI can ask the right question:
/// a simple confirmation, or which Category its entries should move to.
/// </summary>
public sealed record CategoryRemovalPlan(Category Category, int EntryCount, IReadOnlyList<Category> MoveTargets)
{
    public bool NeedsMoveTarget => EntryCount > 0;

    public string Message => EntryCount switch
    {
        0 => $"Remove {Category.Name}?",
        1 => $"{Category.Name} has 1 entry. Choose a Category to move it to.",
        _ => $"{Category.Name} has {EntryCount} entries. Choose a Category to move them to.",
    };
}
