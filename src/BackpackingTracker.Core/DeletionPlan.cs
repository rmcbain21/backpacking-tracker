namespace BackpackingTracker.Core;

/// <summary>
/// Whether a catalogue item (a Gear Item or Food Item) can be deleted, and what uses it if not.
/// Anything in use can't be deleted, only Retired, so nothing is left pointing at nothing.
/// </summary>
public abstract record DeletionPlan(IReadOnlyList<string> UsedBy)
{
    /// <summary>What kind of item it is, as the backpacker reads it, such as "Gear Item".</summary>
    protected abstract string Kind { get; }

    protected abstract string ItemName { get; }

    protected abstract bool ItemRetired { get; }

    public bool CanDelete => UsedBy.Count == 0;

    /// <summary>True when it can't be deleted but could be Retired instead.</summary>
    public bool OffersRetire => !CanDelete && !ItemRetired;

    public string Title => CanDelete ? $"Delete {Kind}" : "Can't delete";

    /// <summary>The button that goes ahead (Delete, or Retire instead), or null when there's nothing to do.</summary>
    public string? ConfirmButton => CanDelete ? "Delete" : OffersRetire ? "Retire" : null;

    public string Message
    {
        get
        {
            if (CanDelete)
            {
                return $"Delete {ItemName}? This can't be undone.";
            }

            var refusal = $"{ItemName} can't be deleted because {JoinUses()} {(UsedBy.Count == 1 ? "uses" : "use")} it.";
            return OffersRetire ? $"{refusal} Retire it instead?" : refusal;
        }
    }

    /// <summary>"A", "A and B", or "A, B and C".</summary>
    private string JoinUses() => UsedBy.Count == 1
        ? UsedBy[0]
        : $"{string.Join(", ", UsedBy.Take(UsedBy.Count - 1))} and {UsedBy[^1]}";
}
