using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Weight;

namespace BackpackingTracker.Core.Lists;

/// <summary>
/// The Categories and List Entries of one Gear List or Packing List, with the entry rules both kinds of
/// list share. Each change returns new contents; the caller saves them in its own record.
/// </summary>
public sealed record ListContents
{
    /// <summary>The list's own Categories, in order (ADR 0003).</summary>
    public IReadOnlyList<Category> Categories { get; init; } = [];

    public IReadOnlyList<ListEntry> Entries { get; init; } = [];

    /// <summary>A copy for another list: the same Categories, and the same entries each with a new id, none ticked as packed.</summary>
    internal ListContents Copy() =>
        this with { Entries = Entries.Select(entry => entry with { Id = Guid.NewGuid().ToString(), Packed = false }).ToList() };

    /// <summary>
    /// Adds the Gear Item into one of the list's Categories, with quantity 1 and Carry Mode base. If it's
    /// already on the list, adds one to that entry's quantity instead (leaving it in its Category, and
    /// unticking it, since the extra one isn't packed yet), so a list has one entry per Gear Item. Retired
    /// gear can't be added.
    /// </summary>
    /// <param name="item">The Gear Item, or null if it no longer exists.</param>
    /// <param name="listKind">"Gear List" or "Packing List", for the messages.</param>
    internal (ListContents Contents, ListEntry Entry) AddEntry(GearItem? item, string categoryId, string listKind)
    {
        if (Categories.All(c => c.Id != categoryId))
        {
            throw new RuleViolationException("Choose a Category on this list.");
        }

        if (item is null)
        {
            throw new RuleViolationException("That Gear Item no longer exists.");
        }

        if (item.Retired)
        {
            throw new RuleViolationException($"{item.Name} is Retired. Un-retire it to add it to a {listKind}.");
        }

        var existing = Entries.FirstOrDefault(entry => entry.GearItemId == item.Id);
        if (existing is not null)
        {
            var more = existing with { Quantity = existing.Quantity + 1, Packed = false };
            return (Replace(more), more);
        }

        var entry = new ListEntry { Id = Guid.NewGuid().ToString(), GearItemId = item.Id, CategoryId = categoryId };
        return (this with { Entries = [.. Entries, entry] }, entry);
    }

    /// <summary>Changes an entry's quantity and Carry Mode. Raising the quantity unticks it, since the extra ones aren't packed yet.</summary>
    internal ListContents UpdateEntry(string entryId, int quantity, CarryMode carryMode, string listKind)
    {
        if (quantity < 1)
        {
            throw new RuleViolationException("Quantity must be 1 or more.");
        }

        if (!Enum.IsDefined(carryMode))
        {
            throw new RuleViolationException("Choose base, worn or consumable.");
        }

        var entry = FindEntry(entryId, listKind);
        return Replace(entry with { Quantity = quantity, CarryMode = carryMode, Packed = entry.Packed && quantity <= entry.Quantity });
    }

    /// <summary>Ticks or unticks an entry on the packing checklist.</summary>
    internal ListContents SetPacked(string entryId, bool packed, string listKind) =>
        Replace(FindEntry(entryId, listKind) with { Packed = packed });

    /// <summary>Unticks every entry, to check the packing again.</summary>
    internal ListContents ResetPacked() =>
        this with { Entries = Entries.Select(entry => entry with { Packed = false }).ToList() };

    /// <summary>Moves an entry to another of the list's Categories.</summary>
    internal ListContents MoveEntry(string entryId, string categoryId, string listKind) =>
        Replace(FindEntry(entryId, listKind) with { CategoryId = FindCategory(categoryId).Id });

    internal ListContents RemoveEntry(string entryId, string listKind)
    {
        FindEntry(entryId, listKind);
        return this with { Entries = Entries.Where(e => e.Id != entryId).ToList() };
    }

    internal Category FindCategory(string categoryId) =>
        Categories.SingleOrDefault(c => c.Id == categoryId)
            ?? throw new RuleViolationException("That Category is no longer on the list.");

    private ListEntry FindEntry(string entryId, string listKind) =>
        Entries.SingleOrDefault(entry => entry.Id == entryId)
            ?? throw new RuleViolationException($"That entry is no longer on the {listKind}.");

    private ListContents Replace(ListEntry changed) =>
        this with { Entries = Entries.Select(e => e.Id == changed.Id ? changed : e).ToList() };

    // ----- Views -----

    /// <summary>The entries with their Gear Items' current weights, arranged by Category, and the list's totals.</summary>
    /// <param name="items">The Gear Items the entries use, by id; a missing one was deleted on another device.</param>
    /// <param name="waterGrams">Water carried, on a Trip; counted as Consumable Weight.</param>
    internal ListContentsView View(IReadOnlyDictionary<string, GearItem> items, decimal waterGrams = 0m)
    {
        var entries = Entries
            .Select(entry =>
            {
                // A Gear Item deleted on another device before a sync is shown as missing and weighs nothing.
                var item = items.GetValueOrDefault(entry.GearItemId);
                var weighed = new WeighedEntry(item?.WeightGrams ?? 0m, entry.Quantity, entry.CarryMode, item is null ? null : entry.CategoryId);
                return (View: new ListEntryView(entry, item, WeightCalculator.EntryWeight(weighed)), Weighed: weighed);
            })
            .ToList();

        var views = entries.Select(e => e.View).ToList();
        var weight = WeightCalculator.Calculate(entries.Select(e => e.Weighed), Categories, waterGrams);
        return new ListContentsView(views, Sections(views), weight);
    }

    /// <summary>
    /// Every Category on the list, in order, with its entries by Name (deleted gear last) and their
    /// subtotal, worn entries included. An entry whose Category id isn't on the list (which the API never
    /// does, but a hand-edited or imported file could) comes last under "No Category" rather than vanishing.
    /// </summary>
    private IReadOnlyList<CategorySection> Sections(IReadOnlyList<ListEntryView> entries)
    {
        CategorySection SectionFor(Category category, int position, IEnumerable<ListEntryView> sectionEntries)
        {
            var sorted = sectionEntries
                .OrderBy(entry => entry.GearItem is null)
                .ThenBy(entry => entry.GearItem?.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
            return new CategorySection(category, position, sorted, sorted.Sum(entry => entry.EntryWeightGrams));
        }

        var sections = Categories
            .Select((category, position) => SectionFor(category, position, entries.Where(entry => entry.Entry.CategoryId == category.Id)))
            .ToList();

        var known = Categories.Select(category => category.Id).ToHashSet();
        var withoutCategory = entries.Where(entry => !known.Contains(entry.Entry.CategoryId)).ToList();
        if (withoutCategory.Count > 0)
        {
            sections.Add(SectionFor(Category.None, -1, withoutCategory));
        }

        return sections;
    }
}

/// <summary>A list's entries with their current weights, its Category sections and its totals.</summary>
internal sealed record ListContentsView(IReadOnlyList<ListEntryView> Entries, IReadOnlyList<CategorySection> Sections, PackWeight Weight);
