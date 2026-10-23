using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Weight;

/// <summary>
/// Totals pack weight from List Entries: each entry weighs its Gear Item's weight times its quantity,
/// and counts towards Base, Worn or Consumable Weight by its Carry Mode. Pure, so Gear Lists and
/// Trips share it; Meal Plan food and water will be added as further Consumable Weight.
/// </summary>
public static class WeightCalculator
{
    public static decimal EntryWeight(WeighedEntry entry) => entry.ItemWeightGrams * entry.Quantity;

    public static PackWeight Calculate(IEnumerable<WeighedEntry> entries)
    {
        var all = entries.ToList();
        decimal TotalFor(CarryMode mode) => all.Where(entry => entry.CarryMode == mode).Sum(EntryWeight);

        return new PackWeight
        {
            BaseGrams = TotalFor(CarryMode.Base),
            WornGrams = TotalFor(CarryMode.Worn),
            ConsumableGrams = TotalFor(CarryMode.Consumable),
        };
    }
}
