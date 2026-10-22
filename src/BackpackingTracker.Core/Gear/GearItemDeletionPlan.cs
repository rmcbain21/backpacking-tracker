namespace BackpackingTracker.Core.Gear;

/// <summary>Whether a Gear Item can be deleted, and what uses it if not.</summary>
public sealed record GearItemDeletionPlan(GearItem GearItem, IReadOnlyList<string> UsedBy)
{
    public bool CanDelete => UsedBy.Count == 0;

    /// <summary>True when it can't be deleted but could be Retired instead.</summary>
    public bool OffersRetire => !CanDelete && !GearItem.Retired;

    public string Title => CanDelete ? "Delete Gear Item" : "Can't delete";

    /// <summary>The button that goes ahead (Delete, or Retire instead), or null when there's nothing to do.</summary>
    public string? ConfirmButton => CanDelete ? "Delete" : OffersRetire ? "Retire" : null;

    public string Message
    {
        get
        {
            if (CanDelete)
            {
                return $"Delete {GearItem.Name}? This can't be undone.";
            }

            var refusal = $"{GearItem.Name} can't be deleted because {JoinUses()} {(UsedBy.Count == 1 ? "uses" : "use")} it.";
            return OffersRetire ? $"{refusal} Retire it instead?" : refusal;
        }
    }

    /// <summary>"A", "A and B", or "A, B and C".</summary>
    private string JoinUses() => UsedBy.Count == 1
        ? UsedBy[0]
        : $"{string.Join(", ", UsedBy.Take(UsedBy.Count - 1))} and {UsedBy[^1]}";
}
