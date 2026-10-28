using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Lists;

/// <summary>
/// Gear Lists, each with its own Categories and its List Entries, with weights always from each
/// Gear Item's current weight.
/// </summary>
public sealed class GearLists : IGearItemReferences
{
    internal const string Table = "gearLists";

    private const string ListKind = "Gear List";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly GearCatalogue _gear;

    public GearLists(IRecordStore store, IClock clock, GearCatalogue gear)
    {
        _store = store;
        _clock = clock;
        _gear = gear;
    }

    /// <summary>Makes a Gear List that starts with a copy of the Default Categories.</summary>
    public async Task<GearList> CreateGearListAsync(string name)
    {
        var list = new GearList
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = TrimAndCheckListName(name),
            Categories = await CopyDefaultCategoriesAsync(),
        };
        await _store.SaveAsync(Table, list);
        return list;
    }

    public async Task RenameGearListAsync(string gearListId, string newName)
    {
        var list = await GetExistingGearListAsync(gearListId);
        await SaveAsync(list with { Name = TrimAndCheckListName(newName) });
    }

    /// <summary>
    /// A new Gear List with copies of the list's Categories and List Entries (each entry with a new id),
    /// to make a variation such as a winter kit. Later changes to either list don't change the other.
    /// </summary>
    public async Task<GearList> DuplicateGearListAsync(string gearListId, string name)
    {
        var original = await GetExistingGearListAsync(gearListId);
        var copy = original.With(original.Contents.Copy()) with
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = TrimAndCheckListName(name),
        };
        await _store.SaveAsync(Table, copy);
        return copy;
    }

    /// <summary>
    /// Makes a Gear List with these Categories and entries (a Trip's Packing List, copied by the caller) and
    /// saves it. Each entry must be in one of the Categories.
    /// </summary>
    internal async Task<GearList> CreateGearListAsync(string name, ListContents contents)
    {
        var list = NewGearList(name, contents.Categories, contents.Entries);
        await _store.SaveAsync(Table, list);
        return list;
    }

    /// <summary>Deletes the Gear List. Kept as a tombstone so the deletion syncs; its Gear Items are untouched.</summary>
    public async Task DeleteGearListAsync(string gearListId)
    {
        var list = await GetExistingGearListAsync(gearListId);
        await SaveAsync(list with { Deleted = true });
    }

    private static string TrimAndCheckListName(string name) =>
        string.IsNullOrWhiteSpace(name) ? throw new RuleViolationException("Give the Gear List a name.") : name.Trim();

    /// <summary>
    /// A new Gear List with these Categories and entries, not saved (for the import, which saves it along
    /// with its Gear Items). Each entry must be in one of the Categories.
    /// </summary>
    internal GearList NewGearList(string name, IReadOnlyList<Category> categories, IReadOnlyList<ListEntry> entries)
    {
        var trimmed = TrimAndCheckListName(name);
        if (categories.Count == 0)
        {
            throw new RuleViolationException("A Gear List needs at least one Category.");
        }

        var categoryIds = categories.Select(category => category.Id).ToHashSet();
        if (entries.Any(entry => !categoryIds.Contains(entry.CategoryId)))
        {
            throw new ArgumentException("Every entry must be in one of the list's Categories.", nameof(entries));
        }

        return new GearList
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = trimmed,
            Categories = categories,
            Entries = entries,
        };
    }

    /// <summary>Every Gear List by name, each with its entries' current weights and totals.</summary>
    public async Task<IReadOnlyList<GearListView>> ListGearListsAsync()
    {
        var lists = await LoadGearListsAsync();
        var items = await _gear.GetGearItemsAsync(lists.SelectMany(list => list.Entries).Select(entry => entry.GearItemId));
        return lists.Select(list => View(list, items)).ToList();
    }

    /// <summary>A Gear List with its entries' current weights and totals, or null if there's no such list.</summary>
    public async Task<GearListView?> GetGearListAsync(string gearListId)
    {
        var list = await FindGearListAsync(gearListId);
        if (list is null)
        {
            return null;
        }

        var items = await _gear.GetGearItemsAsync(list.Entries.Select(entry => entry.GearItemId));
        return View(list, items);
    }

    // ----- The list's Categories -----

    public async Task<Category> AddCategoryAsync(string gearListId, string name)
    {
        var list = await GetExistingGearListAsync(gearListId);
        var category = new Category { Id = Guid.NewGuid().ToString(), Name = TrimAndCheckCategoryName(list, name, renamingId: null) };
        await SaveAsync(list with { Categories = [.. list.Categories, category] });
        return category;
    }

    public async Task RenameCategoryAsync(string gearListId, string categoryId, string newName)
    {
        var list = await GetExistingGearListAsync(gearListId);
        var category = FindCategory(list, categoryId);
        var renamed = category with { Name = TrimAndCheckCategoryName(list, newName, renamingId: categoryId) };
        await SaveAsync(list with { Categories = list.Categories.Select(c => c.Id == categoryId ? renamed : c).ToList() });
    }

    /// <summary>Moves a Category to a new position on the list (0 is the top); past either end stops there.</summary>
    public async Task MoveCategoryAsync(string gearListId, string categoryId, int newPosition)
    {
        var list = await GetExistingGearListAsync(gearListId);
        var category = FindCategory(list, categoryId);
        var ordered = list.Categories.Where(c => c.Id != categoryId).ToList();
        ordered.Insert(Math.Clamp(newPosition, 0, ordered.Count), category);
        await SaveAsync(list with { Categories = ordered });
    }

    /// <summary>What removing a Category from the list involves: how many entries it has, and where they could move.</summary>
    public async Task<CategoryRemovalPlan> PlanCategoryRemovalAsync(string gearListId, string categoryId)
    {
        var list = await GetExistingGearListAsync(gearListId);
        return PlanRemoval(list, FindCategory(list, categoryId));
    }

    /// <summary>
    /// Removes a Category from the list. Its entries must move to another of the list's Categories,
    /// so no entry is left without one; at least one Category must stay.
    /// </summary>
    public async Task RemoveCategoryAsync(string gearListId, string categoryId, string? moveEntriesToCategoryId = null)
    {
        var list = await GetExistingGearListAsync(gearListId);
        var plan = PlanRemoval(list, FindCategory(list, categoryId));
        if (plan.MoveTargets.Count == 0)
        {
            throw new RuleViolationException("Keep at least one Category on the list.");
        }

        var entries = list.Entries;
        if (plan.NeedsMoveTarget)
        {
            if (moveEntriesToCategoryId is null)
            {
                throw new RuleViolationException(plan.Message);
            }

            var target = plan.MoveTargets.SingleOrDefault(c => c.Id == moveEntriesToCategoryId)
                ?? throw new RuleViolationException("Choose a different Category on this list to move the entries to.");
            entries = entries.Select(e => e.CategoryId == categoryId ? e with { CategoryId = target.Id } : e).ToList();
        }

        await SaveAsync(list with { Categories = plan.MoveTargets, Entries = entries });
    }

    private static CategoryRemovalPlan PlanRemoval(GearList list, Category category) =>
        new(category,
            list.Entries.Count(entry => entry.CategoryId == category.Id),
            list.Categories.Where(c => c.Id != category.Id).ToList());

    /// <summary>Returns the trimmed name, or refuses a blank name or one another Category on the list has.</summary>
    private static string TrimAndCheckCategoryName(GearList list, string name, string? renamingId)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            throw new RuleViolationException("Give the Category a name.");
        }

        var clash = list.Categories.FirstOrDefault(c =>
            c.Id != renamingId && string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (clash is not null)
        {
            throw new RuleViolationException($"There is already a Category called {clash.Name} on this list.");
        }

        return trimmed;
    }

    private static Category FindCategory(GearList list, string categoryId) => list.Contents.FindCategory(categoryId);

    // ----- The list's entries -----

    /// <summary>
    /// Adds the Gear Item into one of the list's Categories, with quantity 1 and Carry Mode base. If it's
    /// already on the list, adds one to that entry's quantity instead (leaving it in its Category), so a
    /// list has one entry per Gear Item. Retired gear can't be added.
    /// </summary>
    public async Task<ListEntry> AddEntryAsync(string gearListId, string gearItemId, string categoryId)
    {
        var list = await GetExistingGearListAsync(gearListId);
        var (contents, entry) = list.Contents.AddEntry(await _gear.GetGearItemAsync(gearItemId), categoryId, ListKind);
        await SaveAsync(list.With(contents));
        return entry;
    }

    public async Task UpdateEntryAsync(string gearListId, string entryId, int quantity, CarryMode carryMode)
    {
        var list = await GetExistingGearListAsync(gearListId);
        await SaveAsync(list.With(list.Contents.UpdateEntry(entryId, quantity, carryMode, ListKind)));
    }

    /// <summary>Moves an entry to another of the list's Categories.</summary>
    public async Task MoveEntryAsync(string gearListId, string entryId, string categoryId)
    {
        var list = await GetExistingGearListAsync(gearListId);
        await SaveAsync(list.With(list.Contents.MoveEntry(entryId, categoryId, ListKind)));
    }

    public async Task RemoveEntryAsync(string gearListId, string entryId)
    {
        var list = await GetExistingGearListAsync(gearListId);
        await SaveAsync(list.With(list.Contents.RemoveEntry(entryId, ListKind)));
    }

    // ----- Views -----

    private static GearListView View(GearList list, IReadOnlyDictionary<string, GearItem> items)
    {
        var view = list.Contents.View(items);
        return new GearListView(list, view.Entries, view.Sections, view.Weight);
    }

    // ----- Loading and saving -----

    /// <summary>The Default Categories as a new list's own Categories (a Gear List's, or an empty Trip's Packing List).</summary>
    internal async Task<IReadOnlyList<Category>> CopyDefaultCategoriesAsync() =>
        (await _gear.ListDefaultCategoriesAsync()).Select(d => new Category { Id = d.Id, Name = d.Name }).ToList();

    private async Task<IReadOnlyList<GearList>> LoadGearListsAsync()
    {
        var lists = (await _store.ListAsync<GearList>(Table))
            .Where(list => !list.Deleted)
            .OrderBy(list => list.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return lists.Any(IsFromBeforeListCategories) ? await ConvertOldListsAsync(lists) : lists;
    }

    private static bool IsFromBeforeListCategories(GearList list) =>
        list.Categories.Count == 0 || list.Entries.Any(entry => entry.CategoryId.Length == 0);

    /// <summary>
    /// Gear Lists saved before lists had their own Categories (ticket 28) get a copy of the Default
    /// Categories, and each entry goes in the Category its Gear Item had then, or the list's first
    /// Category. Each converted list is saved straight away, so later changes to the Default Categories
    /// don't change it.
    /// </summary>
    private async Task<IReadOnlyList<GearList>> ConvertOldListsAsync(IReadOnlyList<GearList> lists)
    {
        var defaults = await CopyDefaultCategoriesAsync();
        var items = await _gear.GetGearItemsAsync(lists.SelectMany(list => list.Entries).Select(entry => entry.GearItemId));

        var converted = new List<GearList>();
        foreach (var list in lists)
        {
            if (!IsFromBeforeListCategories(list))
            {
                converted.Add(list);
                continue;
            }

            var categories = list.Categories.Count > 0 ? list.Categories : defaults;
            var categoryIds = categories.Select(category => category.Id).ToHashSet();
            string CategoryFor(ListEntry entry) =>
                items.GetValueOrDefault(entry.GearItemId)?.LegacyCategoryId is { } old && categoryIds.Contains(old)
                    ? old
                    : categories[0].Id;

            var withCategories = list with
            {
                Categories = categories,
                Entries = list.Entries.Select(e => e.CategoryId.Length == 0 ? e with { CategoryId = CategoryFor(e) } : e).ToList(),
                UpdatedAt = _clock.UtcNow,
            };
            await _store.SaveAsync(Table, withCategories);
            converted.Add(withCategories);
        }

        return converted;
    }

    private async Task<GearList?> FindGearListAsync(string gearListId) =>
        (await LoadGearListsAsync()).SingleOrDefault(list => list.Id == gearListId);

    internal async Task<GearList> GetExistingGearListAsync(string gearListId) =>
        await FindGearListAsync(gearListId) ?? throw new RuleViolationException("That Gear List no longer exists.");

    private Task SaveAsync(GearList list) => _store.SaveAsync(Table, list with { UpdatedAt = _clock.UtcNow });

    /// <summary>
    /// The Gear Lists using Gear Items merged away, with their entries moved onto the Gear Items they were merged
    /// into (see <see cref="ListContents.Repoint"/>), ready to save; lists that use none are left out.
    /// </summary>
    internal async Task<IReadOnlyList<GearList>> RepointAsync(IReadOnlyDictionary<string, string> mergedInto) =>
        (await LoadGearListsAsync())
            .Where(list => list.Contents.Uses(mergedInto))
            .Select(list => list.With(list.Contents.Repoint(mergedInto)) with { UpdatedAt = _clock.UtcNow })
            .ToList();

    /// <summary>The Gear Lists that use the Gear Item, by name, each once, so it can't be deleted from under them.</summary>
    public async Task<IReadOnlyList<string>> FindUsesAsync(string gearItemId) =>
        (await LoadGearListsAsync())
            .Where(list => list.Entries.Any(entry => entry.GearItemId == gearItemId))
            .Select(list => $"Gear List \"{list.Name}\"")
            .ToList();
}
