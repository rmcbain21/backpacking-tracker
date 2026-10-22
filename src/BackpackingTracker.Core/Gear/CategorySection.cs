namespace BackpackingTracker.Core.Gear;

/// <summary>A Category and the Gear Items in it, for showing gear under Category headings.</summary>
public sealed record CategorySection(Category Category, IReadOnlyList<GearItem> GearItems);
