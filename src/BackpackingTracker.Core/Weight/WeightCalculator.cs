using BackpackingTracker.Core.Gear;

namespace BackpackingTracker.Core.Weight;

/// <summary>
/// Totals pack weight from List Entries: each entry weighs its Gear Item's weight times its quantity,
/// and counts towards Base, Worn or Consumable Weight by its Carry Mode. It also splits the Starting
/// Pack Weight by Category. Pure, so Gear Lists and Trips share it; Meal Plan food and water will be
/// added as further Consumable Weight.
/// </summary>
public static class WeightCalculator
{
    public static decimal EntryWeight(WeighedEntry entry) => entry.ItemWeightGrams * entry.Quantity;

    /// <param name="entries">The entries, with their Gear Items' weights and Categories.</param>
    /// <param name="categories">The backpacker's Categories, in their order, for the breakdown.</param>
    public static PackWeight Calculate(IEnumerable<WeighedEntry> entries, IReadOnlyList<Category> categories)
    {
        var all = entries.ToList();
        decimal TotalFor(CarryMode mode) => all.Where(entry => entry.CarryMode == mode).Sum(EntryWeight);

        var weight = new PackWeight
        {
            BaseGrams = TotalFor(CarryMode.Base),
            WornGrams = TotalFor(CarryMode.Worn),
            ConsumableGrams = TotalFor(CarryMode.Consumable),
        };

        return weight with { ByCategory = BreakDown(all, categories, weight.StartingPackGrams) };
    }

    /// <summary>
    /// What's in the pack (base and consumable entries; worn ones are left out), by Category in
    /// Category order, leaving out empty Categories. Entries whose Category no longer exists come
    /// last under "No Category"; entries whose Gear Item is gone weigh nothing and are left out.
    /// </summary>
    private static IReadOnlyList<CategoryWeight> BreakDown(
        IReadOnlyList<WeighedEntry> entries, IReadOnlyList<Category> categories, decimal startingPackGrams)
    {
        var inPack = entries.Where(entry => entry.CarryMode != CarryMode.Worn && entry.CategoryId is not null).ToList();
        var known = categories.Select(category => category.Id).ToHashSet();

        CategoryWeight? LineFor(Category category, IEnumerable<WeighedEntry> categoryEntries)
        {
            var grouped = categoryEntries.ToList();
            if (grouped.Count == 0)
            {
                return null;
            }

            var grams = grouped.Sum(EntryWeight);
            return new CategoryWeight(category, grams, startingPackGrams == 0 ? 0 : grams / startingPackGrams);
        }

        var lines = categories
            .OrderBy(category => category.SortOrder)
            .Select(category => LineFor(category, inPack.Where(entry => entry.CategoryId == category.Id)))
            .Append(LineFor(Category.None, inPack.Where(entry => !known.Contains(entry.CategoryId!))));

        return lines.OfType<CategoryWeight>().ToList();
    }
}
