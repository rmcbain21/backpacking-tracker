namespace BackpackingTracker.Core.Gear;

/// <summary>Whether a Gear Item can be deleted, and what uses it if not.</summary>
public sealed record GearItemDeletionPlan(GearItem GearItem, IReadOnlyList<string> UsedBy) : DeletionPlan(UsedBy)
{
    protected override string Kind => "Gear Item";

    protected override string ItemName => GearItem.Name;

    protected override bool ItemRetired => GearItem.Retired;
}
