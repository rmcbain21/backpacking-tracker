using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Gear;

public sealed class GearCatalogue
{
    private const string Table = "gearItems";
    private const string CategoryTable = "categories";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly List<IGearItemReferences> _references = [];

    public GearCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<GearItem> AddGearItemAsync(GearItemDetails details)
    {
        var item = await ApplyDetailsAsync(
            new GearItem { Id = Guid.NewGuid().ToString(), UpdatedAt = _clock.UtcNow, Name = "", WeightGrams = 0 },
            details);

        await _store.SaveAsync(Table, item);
        return item;
    }

    /// <summary>Replaces every field the backpacker can edit, applying the same rules as adding.</summary>
    public async Task<GearItem> UpdateGearItemAsync(string gearItemId, GearItemDetails details)
    {
        var existing = await GetExistingGearItemAsync(gearItemId);

        var item = await ApplyDetailsAsync(existing with { UpdatedAt = _clock.UtcNow }, details);

        await _store.SaveAsync(Table, item);
        return item;
    }

    /// <summary>Checks the details and copies them, cleaned up, onto the Gear Item.</summary>
    private async Task<GearItem> ApplyDetailsAsync(GearItem item, GearItemDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Name))
        {
            throw new RuleViolationException("Give the Gear Item a name.");
        }

        if (details.WeightGrams < 0)
        {
            throw new RuleViolationException("A weight can't be negative.");
        }

        if (details.Price < 0)
        {
            throw new RuleViolationException("A price can't be negative.");
        }

        return item with
        {
            Name = details.Name.Trim(),
            GearType = await CleanUpGearTypeAsync(details.GearType),
            WeightGrams = Math.Round(details.WeightGrams, 1, MidpointRounding.AwayFromZero),
            Tags = await CleanUpTagsAsync(details.Tags),
            Price = details.Price is { } price ? Math.Round(price, 2, MidpointRounding.AwayFromZero) : null,
            Notes = details.Notes?.Trim() ?? "",
            LegacyBrandModel = null,
        };
    }

    /// <summary>The catalogue: Gear Items the backpacker owns, by Gear Type then Name. Leaves out Retired ones.</summary>
    public Task<IReadOnlyList<GearItem>> ListGearItemsAsync() => ListGearItemsAsync(new GearFilter());

    /// <summary>The catalogue narrowed by search text and Gear Type, by Gear Type then Name.</summary>
    public async Task<IReadOnlyList<GearItem>> ListGearItemsAsync(GearFilter filter) =>
        (await LoadGearItemsAsync()).Where(item => !item.Retired && filter.Matches(item)).ToList();

    public async Task<IReadOnlyList<GearItem>> ListRetiredGearItemsAsync() =>
        (await LoadGearItemsAsync()).Where(item => item.Retired).ToList();
    /// <summary>
    /// A Gear Item by id, Retired or not, so anything that refers to it can still show it.
    /// Null if it doesn't exist or was deleted.
    /// </summary>
    public async Task<GearItem?> GetGearItemAsync(string gearItemId) =>
        (await LoadGearItemsAsync()).SingleOrDefault(item => item.Id == gearItemId);

    /// <summary>
    /// The Gear Items with these ids, Retired or not, keyed by id, loading the catalogue once.
    /// Ids of deleted or unknown Gear Items are left out.
    /// </summary>
    public async Task<IReadOnlyDictionary<string, GearItem>> GetGearItemsAsync(IEnumerable<string> gearItemIds)
    {
        var wanted = gearItemIds.ToHashSet();
        return (await LoadGearItemsAsync())
            .Where(item => wanted.Contains(item.Id))
            .ToDictionary(item => item.Id);
    }

    /// <summary>A Gear Item by id, Retired or not; refuses if it doesn't exist or was deleted.</summary>
    private async Task<GearItem> GetExistingGearItemAsync(string gearItemId) =>
        await GetGearItemAsync(gearItemId) ?? throw new RuleViolationException("That Gear Item no longer exists.");

    public Task RetireGearItemAsync(string gearItemId) => SetRetiredAsync(gearItemId, retired: true);

    public Task UnretireGearItemAsync(string gearItemId) => SetRetiredAsync(gearItemId, retired: false);

    private async Task SetRetiredAsync(string gearItemId, bool retired)
    {
        var item = await GetExistingGearItemAsync(gearItemId);

        await _store.SaveAsync(Table, item with { Retired = retired, UpdatedAt = _clock.UtcNow });
    }

    /// <summary>
    /// Every Gear Item that hasn't been deleted, Retired or not, sorted by Gear Type (those without one
    /// last), then Name. Records saved before Gear Types existed are converted as they're read.
    /// </summary>
    private async Task<IReadOnlyList<GearItem>> LoadGearItemsAsync()
    {
        var items = await _store.ListAsync<GearItem>(Table);
        return items
            .Where(item => !item.Deleted)
            .Select(ConvertOldRecord)
            .OrderBy(item => item.GearType.Length == 0)
            .ThenBy(item => item.GearType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// A record saved before ticket 27 had a name (what the gear is, such as "Tent") and a brand/model
    /// (such as "Durston X-Mid 1"). Its name becomes the Gear Type and its brand/model the Name; with no
    /// brand/model, the name serves as both. Records from before Categories had no Category: Misc.
    /// </summary>
    private static GearItem ConvertOldRecord(GearItem item)
    {
        if (item.LegacyBrandModel is null)
        {
            return item;
        }

        var brandModel = item.LegacyBrandModel.Trim();
        return item with
        {
            GearType = item.Name,
            Name = brandModel.Length > 0 ? brandModel : item.Name,
            LegacyBrandModel = null,
            LegacyCategoryId = string.IsNullOrEmpty(item.LegacyCategoryId) ? DefaultCategories.MiscId : item.LegacyCategoryId,
        };
    }

    /// <summary>Every Gear Type in use in the catalogue, once each, in alphabetical order; used for suggestions.</summary>
    public async Task<IReadOnlyList<string>> ListGearTypesAsync() =>
        (await ListGearItemsAsync())
            .Select(item => item.GearType)
            .Where(gearType => gearType.Length > 0)
            .Distinct(TagRules.Comparer)
            .Order(TagRules.Comparer)
            .ToList();

    /// <summary>Trims a Gear Type and gives it the spelling it already has elsewhere, like a Tag.</summary>
    private async Task<string> CleanUpGearTypeAsync(string? gearType)
    {
        var trimmed = gearType?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            return "";
        }

        var gearTypesInUse = await ListGearTypesAsync();
        return gearTypesInUse.FirstOrDefault(t => TagRules.Comparer.Equals(t, trimmed)) ?? trimmed;
    }


    /// <summary>
    /// The Default Categories in the backpacker's chosen order. On first run this also creates them.
    /// </summary>
    public async Task<IReadOnlyList<DefaultCategory>> ListDefaultCategoriesAsync()
    {
        var categories = await _store.ListAsync<DefaultCategory>(CategoryTable);

        // Only on first run: once any Category has existed, its tombstone stays in the store.
        if (categories.Count == 0)
        {
            categories = await CreateDefaultCategoriesAsync();
        }

        return categories
            .Where(category => !category.Deleted)
            .OrderBy(category => category.SortOrder)
            // Two devices can each add a Category at the same position before syncing.
            .ThenBy(category => category.Id, StringComparer.Ordinal)
            .ToList();
    }

    private async Task<IReadOnlyList<DefaultCategory>> CreateDefaultCategoriesAsync()
    {
        var categories = DefaultCategories.All
            .Select((category, index) => new DefaultCategory
            {
                Id = category.Id,
                // Every device creates these same records. The epoch marks them as never edited,
                // so sync can tell an untouched default from one changed on another device.
                UpdatedAt = DateTimeOffset.UnixEpoch,
                Name = category.Name,
                SortOrder = index,
            })
            .ToList();

        foreach (var category in categories)
        {
            await _store.SaveAsync(CategoryTable, category);
        }

        return categories;
    }

    public async Task<DefaultCategory> AddDefaultCategoryAsync(string name)
    {
        var categories = await ListDefaultCategoriesAsync();
        var category = new DefaultCategory
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = TrimAndCheckCategoryName(name, categories, renamingId: null),
            SortOrder = categories.Max(c => c.SortOrder) + 1,
        };

        await _store.SaveAsync(CategoryTable, category);
        return category;
    }

    /// <summary>Returns the trimmed name, or refuses a blank name or one another Category already has.</summary>
    private static string TrimAndCheckCategoryName(string name, IReadOnlyList<DefaultCategory> categories, string? renamingId)
    {
        var trimmed = name.Trim();
        if (trimmed.Length == 0)
        {
            throw new RuleViolationException("Give the Category a name.");
        }

        var clash = categories.FirstOrDefault(c =>
            c.Id != renamingId && string.Equals(c.Name, trimmed, StringComparison.OrdinalIgnoreCase));
        if (clash is not null)
        {
            throw new RuleViolationException($"There is already a Category called {clash.Name}.");
        }

        return trimmed;
    }

    public async Task RenameDefaultCategoryAsync(string categoryId, string newName)
    {
        var categories = await ListDefaultCategoriesAsync();
        var category = FindCategory(categories, categoryId);
        var name = TrimAndCheckCategoryName(newName, categories, renamingId: categoryId);

        await _store.SaveAsync(CategoryTable, category with { Name = name, UpdatedAt = _clock.UtcNow });
    }

    private static DefaultCategory FindCategory(IReadOnlyList<DefaultCategory> categories, string categoryId) =>
        categories.SingleOrDefault(c => c.Id == categoryId)
            ?? throw new RuleViolationException("That Category no longer exists.");

    /// <summary>Moves a Category to a new position (0 is the top); positions past either end stop there.</summary>
    public async Task MoveDefaultCategoryAsync(string categoryId, int newPosition)
    {
        var ordered = (await ListDefaultCategoriesAsync()).ToList();
        var category = FindCategory(ordered, categoryId);

        ordered.Remove(category);
        ordered.Insert(Math.Clamp(newPosition, 0, ordered.Count), category);

        for (var position = 0; position < ordered.Count; position++)
        {
            if (ordered[position].SortOrder != position)
            {
                await _store.SaveAsync(CategoryTable, ordered[position] with { SortOrder = position, UpdatedAt = _clock.UtcNow });
            }
        }
    }

    /// <summary>Removes a Category; at least one must stay. Kept as a tombstone so the removal syncs.</summary>
    public async Task RemoveDefaultCategoryAsync(string categoryId)
    {
        var categories = await ListDefaultCategoriesAsync();
        var category = FindCategory(categories, categoryId);
        if (categories.Count == 1)
        {
            throw new RuleViolationException("Keep at least one Category.");
        }

        await _store.SaveAsync(CategoryTable, category with { Deleted = true, UpdatedAt = _clock.UtcNow });
    }

    /// <summary>Every Tag in use on a Gear Item, once each, in alphabetical order; used for suggestions.</summary>
    public async Task<IReadOnlyList<string>> ListTagsAsync()
    {
        var items = await ListGearItemsAsync();
        return items
            .SelectMany(item => item.Tags)
            .Distinct(TagRules.Comparer)
            .Order(TagRules.Comparer)
            .ToList();
    }

    /// <summary>
    /// Trims Tags, drops blank and repeated ones (ignoring case), and gives a Tag the spelling it
    /// already has elsewhere, so "Winter" and "winter" don't become two Tags.
    /// </summary>
    private async Task<IReadOnlyList<string>> CleanUpTagsAsync(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0)
        {
            return [];
        }

        var tagsInUse = await ListTagsAsync();
        IReadOnlyList<string> cleaned = [];
        foreach (var tag in tags)
        {
            var spelling = tagsInUse.FirstOrDefault(t => TagRules.Comparer.Equals(t, tag.Trim())) ?? tag;
            cleaned = TagRules.Add(cleaned, spelling);
        }

        return cleaned;
    }

    /// <summary>Lets an area that refers to Gear Items (Gear Lists, Trips) take part in the delete check. Called by AppCore.</summary>
    internal void AddReferences(IGearItemReferences references) => _references.Add(references);

    public async Task<GearItemDeletionPlan> PlanGearItemDeletionAsync(string gearItemId)
    {
        var item = await GetExistingGearItemAsync(gearItemId);

        var usedBy = new List<string>();
        foreach (var references in _references)
        {
            usedBy.AddRange(await references.FindUsesAsync(gearItemId));
        }

        return new GearItemDeletionPlan(item, usedBy);
    }

    /// <summary>
    /// Deletes a Gear Item that nothing uses; otherwise refuses, suggesting Retire.
    /// The record is kept as a tombstone so the deletion syncs to other devices.
    /// </summary>
    public async Task DeleteGearItemAsync(string gearItemId)
    {
        var plan = await PlanGearItemDeletionAsync(gearItemId);
        if (!plan.CanDelete)
        {
            throw new RuleViolationException(plan.Message);
        }

        await _store.SaveAsync(Table, plan.GearItem with { Deleted = true, UpdatedAt = _clock.UtcNow });
    }
}
