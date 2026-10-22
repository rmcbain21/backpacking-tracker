using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Gear;

public sealed class GearCatalogue
{
    private const string Table = "gearItems";
    private const string CategoryTable = "categories";

    private readonly IRecordStore _store;
    private readonly IClock _clock;

    public GearCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<GearItem> AddGearItemAsync(GearItemDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Name))
        {
            throw new RuleViolationException("Give the Gear Item a name.");
        }

        if (details.WeightGrams < 0)
        {
            throw new RuleViolationException("A weight can't be negative.");
        }

        var categories = await ListCategoriesAsync();
        if (!categories.Any(category => category.Id == details.CategoryId))
        {
            throw new RuleViolationException("Choose a Category.");
        }

        var item = new GearItem
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = details.Name.Trim(),
            BrandModel = details.BrandModel?.Trim() ?? "",
            WeightGrams = Math.Round(details.WeightGrams, 1, MidpointRounding.AwayFromZero),
            CategoryId = details.CategoryId,
            Tags = await CleanUpTagsAsync(details.Tags),
        };

        await _store.SaveAsync(Table, item);
        return item;
    }

    public async Task<IReadOnlyList<GearItem>> ListGearItemsAsync()
    {
        var items = await _store.ListAsync<GearItem>(Table);
        return items
            // Gear Items saved before Categories existed have none; they belong in Misc.
            .Select(item => string.IsNullOrEmpty(item.CategoryId) ? item with { CategoryId = DefaultCategories.MiscId } : item)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// The Categories in the backpacker's chosen order. On first run this also creates the defaults.
    /// </summary>
    public async Task<IReadOnlyList<Category>> ListCategoriesAsync()
    {
        var categories = await _store.ListAsync<Category>(CategoryTable);

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

    private async Task<IReadOnlyList<Category>> CreateDefaultCategoriesAsync()
    {
        var categories = DefaultCategories.All
            .Select((category, index) => new Category
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

    public async Task<Category> AddCategoryAsync(string name)
    {
        var categories = await ListCategoriesAsync();
        var category = new Category
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
    private static string TrimAndCheckCategoryName(string name, IReadOnlyList<Category> categories, string? renamingId)
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

    public async Task RenameCategoryAsync(string categoryId, string newName)
    {
        var categories = await ListCategoriesAsync();
        var category = FindCategory(categories, categoryId);
        var name = TrimAndCheckCategoryName(newName, categories, renamingId: categoryId);

        await _store.SaveAsync(CategoryTable, category with { Name = name, UpdatedAt = _clock.UtcNow });
    }

    private static Category FindCategory(IReadOnlyList<Category> categories, string categoryId) =>
        categories.SingleOrDefault(c => c.Id == categoryId)
            ?? throw new RuleViolationException("That Category no longer exists.");

    /// <summary>Moves a Category to a new position (0 is the top); positions past either end stop there.</summary>
    public async Task MoveCategoryAsync(string categoryId, int newPosition)
    {
        var ordered = (await ListCategoriesAsync()).ToList();
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

    /// <summary>What removing a Category involves: how many Gear Items it has, and where they could move.</summary>
    public async Task<CategoryRemovalPlan> PlanCategoryRemovalAsync(string categoryId)
    {
        var categories = await ListCategoriesAsync();
        var category = FindCategory(categories, categoryId);
        var gearItemCount = (await ListGearItemsAsync()).Count(item => item.CategoryId == categoryId);

        return new CategoryRemovalPlan(category, gearItemCount, categories.Where(c => c.Id != categoryId).ToList());
    }

    /// <summary>
    /// Removes a Category. If Gear Items use it, they must be moved to another Category,
    /// so that no Gear Item is left without one.
    /// </summary>
    public async Task RemoveCategoryAsync(string categoryId, string? moveGearItemsToCategoryId = null)
    {
        var plan = await PlanCategoryRemovalAsync(categoryId);
        if (plan.MoveTargets.Count == 0)
        {
            throw new RuleViolationException("Keep at least one Category, so Gear Items have somewhere to go.");
        }

        if (plan.NeedsMoveTarget)
        {
            if (moveGearItemsToCategoryId is null)
            {
                throw new RuleViolationException(plan.Message);
            }

            if (moveGearItemsToCategoryId == categoryId)
            {
                throw new RuleViolationException("Choose a different Category to move the Gear Items to.");
            }

            var target = FindCategory(plan.MoveTargets, moveGearItemsToCategoryId);
            var items = (await ListGearItemsAsync()).Where(item => item.CategoryId == categoryId);
            foreach (var item in items)
            {
                await _store.SaveAsync(Table, item with { CategoryId = target.Id, UpdatedAt = _clock.UtcNow });
            }
        }

        // Kept as a tombstone so that the removal syncs to other devices.
        await _store.SaveAsync(CategoryTable, plan.Category with { Deleted = true, UpdatedAt = _clock.UtcNow });
    }

    /// <summary>
    /// Gear Items under their Category, in Category order, leaving out empty Categories. Gear Items whose
    /// Category no longer exists (removed on another device before a sync) come last, under "No Category".
    /// </summary>
    public async Task<IReadOnlyList<CategorySection>> ListGearItemsByCategoryAsync(GearFilter? filter = null)
    {
        filter ??= new GearFilter();
        var categories = await ListCategoriesAsync();
        var items = (await ListGearItemsAsync()).Where(filter.Matches).ToList();

        var sections = categories
            .Where(category => filter.OnlyCategoryId is null || category.Id == filter.OnlyCategoryId)
            .Select(category => new CategorySection(
                category,
                items.Where(item => item.CategoryId == category.Id).ToList()))
            .Where(section => section.GearItems.Count > 0)
            .ToList();

        var liveCategoryIds = categories.Select(category => category.Id).ToHashSet();
        var withoutCategory = items.Where(item => !liveCategoryIds.Contains(item.CategoryId)).ToList();
        if (filter.OnlyCategoryId is null && withoutCategory.Count > 0)
        {
            var noCategory = new Category { Id = "", UpdatedAt = DateTimeOffset.UnixEpoch, Name = "No Category", SortOrder = int.MaxValue };
            sections.Add(new CategorySection(noCategory, withoutCategory));
        }

        return sections;
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
}
