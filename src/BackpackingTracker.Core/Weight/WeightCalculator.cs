using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Lists;

namespace BackpackingTracker.Core.Weight;

/// <summary>
/// Totals pack weight from List Entries: each entry weighs its Gear Item's weight times its quantity,
/// and counts towards Base, Worn or Consumable Weight by its Carry Mode. Water carried is further
/// Consumable Weight. It also splits the Starting Pack Weight by Category. Pure, so Gear Lists and Trips
/// share it; Meal Plan food will be added as further Consumable Weight.
/// </summary>
public static class WeightCalculator
{
    public static decimal EntryWeight(WeighedEntry entry) => entry.ItemWeightGrams * entry.Quantity;

    /// <param name="entries">The entries, with their Gear Items' weights and Categories.</param>
    /// <param name="categories">The list's Categories, in the list's order, for the breakdown.</param>
    /// <param name="waterGrams">Water carried on a Trip: Consumable Weight, with its own breakdown line.</param>
    public static PackWeight Calculate(IEnumerable<WeighedEntry> entries, IReadOnlyList<Category> categories, decimal waterGrams = 0m)
    {
        var all = entries.ToList();
        decimal TotalFor(CarryMode mode) => all.Where(entry => entry.CarryMode == mode).Sum(EntryWeight);

        var weight = new PackWeight
        {
            BaseGrams = TotalFor(CarryMode.Base),
            WornGrams = TotalFor(CarryMode.Worn),
            ConsumableGrams = TotalFor(CarryMode.Consumable) + waterGrams,
        };

        return weight with { ByCategory = BreakDown(all, categories, waterGrams, weight.StartingPackGrams) };
    }

    /// <summary>
    /// What's in the pack (base and consumable entries; worn ones are left out), by Category in
    /// Category order, leaving out empty Categories. Entries whose Category no longer exists come
    /// next under "No Category"; entries whose Gear Item is gone weigh nothing and are left out.
    /// Water carried comes last, as its own line.
    /// </summary>
    private static IReadOnlyList<CategoryWeight> BreakDown(
        IReadOnlyList<WeighedEntry> entries, IReadOnlyList<Category> categories, decimal waterGrams, decimal startingPackGrams)
    {
        var inPack = entries.Where(entry => entry.CarryMode != CarryMode.Worn && entry.CategoryId is not null).ToList();
        var known = categories.Select(category => category.Id).ToHashSet();

        CategoryWeight Line(Category category, decimal grams) =>
            new(category, grams, startingPackGrams == 0 ? 0 : grams / startingPackGrams);

        CategoryWeight? LineFor(Category category, IEnumerable<WeighedEntry> categoryEntries)
        {
            var grouped = categoryEntries.ToList();
            return grouped.Count == 0 ? null : Line(category, grouped.Sum(EntryWeight));
        }

        var lines = categories
            .Select(category => LineFor(category, inPack.Where(entry => entry.CategoryId == category.Id)))
            .Append(LineFor(Category.None, inPack.Where(entry => !known.Contains(entry.CategoryId!))))
            .Append(waterGrams > 0 ? Line(Category.Water, waterGrams) : null);

        return lines.OfType<CategoryWeight>().ToList();
    }
}
