using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Weight;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Lists;

/// <summary>Gear Lists and their List Entries, with weights always from each Gear Item's current weight.</summary>
public sealed class GearLists : IGearItemReferences
{
    private const string Table = "gearLists";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly GearCatalogue _gear;

    public GearLists(IRecordStore store, IClock clock, GearCatalogue gear)
    {
        _store = store;
        _clock = clock;
        _gear = gear;
    }

    public async Task<GearList> CreateGearListAsync(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new RuleViolationException("Give the Gear List a name.");
        }

        var list = new GearList { Id = Guid.NewGuid().ToString(), UpdatedAt = _clock.UtcNow, Name = name.Trim() };
        await _store.SaveAsync(Table, list);
        return list;
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

    /// <summary>
    /// Adds the Gear Item with quantity 1 and Carry Mode base; if it's already on the list, adds one to
    /// that entry's quantity instead, so a list has one entry per Gear Item. Retired gear can't be added.
    /// </summary>
    public async Task<ListEntry> AddEntryAsync(string gearListId, string gearItemId)
    {
        var list = await GetExistingGearListAsync(gearListId);
        var item = await _gear.GetGearItemAsync(gearItemId)
            ?? throw new RuleViolationException("That Gear Item no longer exists.");
        if (item.Retired)
        {
            throw new RuleViolationException($"{item.Name} is Retired. Un-retire it to add it to a Gear List.");
        }

        var existing = list.Entries.FirstOrDefault(entry => entry.GearItemId == item.Id);
        if (existing is not null)
        {
            var more = existing with { Quantity = existing.Quantity + 1 };
            await SaveAsync(list with { Entries = list.Entries.Select(e => e.Id == existing.Id ? more : e).ToList() });
            return more;
        }

        var entry = new ListEntry { Id = Guid.NewGuid().ToString(), GearItemId = item.Id };
        await SaveAsync(list with { Entries = [.. list.Entries, entry] });
        return entry;
    }

    public async Task UpdateEntryAsync(string gearListId, string entryId, int quantity, CarryMode carryMode)
    {
        if (quantity < 1)
        {
            throw new RuleViolationException("Quantity must be 1 or more.");
        }

        if (!Enum.IsDefined(carryMode))
        {
            throw new RuleViolationException("Choose base, worn or consumable.");
        }

        var list = await GetExistingGearListAsync(gearListId);
        var entry = FindEntry(list, entryId);
        var changed = entry with { Quantity = quantity, CarryMode = carryMode };
        await SaveAsync(list with { Entries = list.Entries.Select(e => e.Id == entryId ? changed : e).ToList() });
    }

    public async Task RemoveEntryAsync(string gearListId, string entryId)
    {
        var list = await GetExistingGearListAsync(gearListId);
        FindEntry(list, entryId);
        await SaveAsync(list with { Entries = list.Entries.Where(e => e.Id != entryId).ToList() });
    }

    private static GearListView View(GearList list, IReadOnlyDictionary<string, GearItem> items)
    {
        var entries = list.Entries
            .Select(entry =>
            {
                // A Gear Item deleted on another device before a sync is shown as missing and weighs nothing.
                var item = items.GetValueOrDefault(entry.GearItemId);
                var weighed = new WeighedEntry(item?.WeightGrams ?? 0m, entry.Quantity, entry.CarryMode);
                return (View: new ListEntryView(entry, item, WeightCalculator.EntryWeight(weighed)), Weighed: weighed);
            })
            .ToList();

        var weight = WeightCalculator.Calculate(entries.Select(e => e.Weighed));
        return new GearListView(list, entries.Select(e => e.View).ToList(), weight);
    }

    private async Task<IReadOnlyList<GearList>> LoadGearListsAsync() =>
        (await _store.ListAsync<GearList>(Table))
            .Where(list => !list.Deleted)
            .OrderBy(list => list.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    private async Task<GearList?> FindGearListAsync(string gearListId) =>
        (await LoadGearListsAsync()).SingleOrDefault(list => list.Id == gearListId);

    private async Task<GearList> GetExistingGearListAsync(string gearListId) =>
        await FindGearListAsync(gearListId) ?? throw new RuleViolationException("That Gear List no longer exists.");

    private static ListEntry FindEntry(GearList list, string entryId) =>
        list.Entries.SingleOrDefault(entry => entry.Id == entryId)
            ?? throw new RuleViolationException("That entry is no longer on the Gear List.");

    private Task SaveAsync(GearList list) => _store.SaveAsync(Table, list with { UpdatedAt = _clock.UtcNow });

    /// <summary>The Gear Lists that use the Gear Item, by name, each once, so it can't be deleted from under them.</summary>
    public async Task<IReadOnlyList<string>> FindUsesAsync(string gearItemId) =>
        (await LoadGearListsAsync())
            .Where(list => list.Entries.Any(entry => entry.GearItemId == gearItemId))
            .Select(list => $"Gear List \"{list.Name}\"")
            .ToList();
}
