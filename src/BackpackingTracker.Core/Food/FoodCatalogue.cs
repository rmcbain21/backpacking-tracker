using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Food;

/// <summary>The backpacker's library of Food Items. Retire and delete follow the same rules as Gear Items.</summary>
public sealed class FoodCatalogue
{
    internal const string Table = "foodItems";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly List<IFoodItemReferences> _references = [];

    public FoodCatalogue(IRecordStore store, IClock clock)
    {
        _store = store;
        _clock = clock;
    }

    public async Task<FoodItem> AddFoodItemAsync(FoodItemDetails details)
    {
        var item = await ApplyDetailsAsync(
            new FoodItem { Id = Guid.NewGuid().ToString(), UpdatedAt = _clock.UtcNow, Name = "", ServingWeightGrams = 0, CaloriesPerServing = 0 },
            details);

        await _store.SaveAsync(Table, item);
        return item;
    }

    /// <summary>Replaces every field the backpacker can edit, applying the same rules as adding.</summary>
    public async Task<FoodItem> UpdateFoodItemAsync(string foodItemId, FoodItemDetails details)
    {
        var existing = await GetExistingFoodItemAsync(foodItemId);

        var item = await ApplyDetailsAsync(existing with { UpdatedAt = _clock.UtcNow }, details);

        await _store.SaveAsync(Table, item);
        return item;
    }

    /// <summary>Checks the details and copies them, cleaned up, onto the Food Item.</summary>
    private async Task<FoodItem> ApplyDetailsAsync(FoodItem item, FoodItemDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Name))
        {
            throw new RuleViolationException("Give the Food Item a name.");
        }

        // Calorie Density divides by the serving weight, so a serving must weigh something once rounded to 0.1 g.
        var servingWeight = Math.Round(details.ServingWeightGrams, 1, MidpointRounding.AwayFromZero);
        if (servingWeight <= 0)
        {
            throw new RuleViolationException("A serving must weigh more than zero.");
        }

        if (details.CaloriesPerServing < 0)
        {
            throw new RuleViolationException("Calories can't be negative.");
        }

        if (details.ProteinGramsPerServing < 0)
        {
            throw new RuleViolationException("Protein can't be negative.");
        }

        if (details.CostPerServing < 0)
        {
            throw new RuleViolationException("A cost can't be negative.");
        }

        return item with
        {
            Name = details.Name.Trim(),
            ServingWeightGrams = servingWeight,
            CaloriesPerServing = Math.Round(details.CaloriesPerServing, 0, MidpointRounding.AwayFromZero),
            ProteinGramsPerServing = details.ProteinGramsPerServing is { } protein ? Math.Round(protein, 1, MidpointRounding.AwayFromZero) : null,
            CostPerServing = details.CostPerServing is { } cost ? Math.Round(cost, 2, MidpointRounding.AwayFromZero) : null,
            Tags = await CleanUpTagsAsync(details.Tags),
        };
    }

    /// <summary>The Food page: Food Items in use, by Name. Leaves out Retired ones.</summary>
    public Task<IReadOnlyList<FoodItem>> ListFoodItemsAsync() => ListFoodItemsAsync(new FoodFilter());

    /// <summary>The Food Items in use narrowed by search text, by Name.</summary>
    public async Task<IReadOnlyList<FoodItem>> ListFoodItemsAsync(FoodFilter filter) =>
        (await LoadFoodItemsAsync()).Where(item => !item.Retired && filter.Matches(item)).ToList();

    public async Task<IReadOnlyList<FoodItem>> ListRetiredFoodItemsAsync() =>
        (await LoadFoodItemsAsync()).Where(item => item.Retired).ToList();

    /// <summary>
    /// A Food Item by id, Retired or not, so anything that refers to it can still show it.
    /// Null if it doesn't exist or was deleted.
    /// </summary>
    public async Task<FoodItem?> GetFoodItemAsync(string foodItemId) =>
        (await LoadFoodItemsAsync()).SingleOrDefault(item => item.Id == foodItemId);

    /// <summary>A Food Item by id, Retired or not; refuses if it doesn't exist or was deleted.</summary>
    private async Task<FoodItem> GetExistingFoodItemAsync(string foodItemId) =>
        await GetFoodItemAsync(foodItemId) ?? throw new RuleViolationException("That Food Item no longer exists.");

    public Task RetireFoodItemAsync(string foodItemId) => SetRetiredAsync(foodItemId, retired: true);

    public Task UnretireFoodItemAsync(string foodItemId) => SetRetiredAsync(foodItemId, retired: false);

    private async Task SetRetiredAsync(string foodItemId, bool retired)
    {
        var item = await GetExistingFoodItemAsync(foodItemId);

        await _store.SaveAsync(Table, item with { Retired = retired, UpdatedAt = _clock.UtcNow });
    }

    /// <summary>Every Food Item that hasn't been deleted, Retired or not, by Name.</summary>
    private async Task<IReadOnlyList<FoodItem>> LoadFoodItemsAsync()
    {
        var items = await _store.ListAsync<FoodItem>(Table);
        return items
            .Where(item => !item.Deleted)
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Every Tag in use on a Food Item, once each, in alphabetical order; used for suggestions.</summary>
    public async Task<IReadOnlyList<string>> ListTagsAsync()
    {
        var items = await ListFoodItemsAsync();
        return items
            .SelectMany(item => item.Tags)
            .Distinct(TagRules.Comparer)
            .Order(TagRules.Comparer)
            .ToList();
    }

    /// <summary>
    /// Trims Tags, drops blank and repeated ones (ignoring case), and gives a Tag the spelling it
    /// already has on other food, so "Breakfast" and "breakfast" don't become two Tags.
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

    /// <summary>Lets an area that refers to Food Items (Meals, Meal Plans) take part in the delete check. Called by AppCore.</summary>
    internal void AddReferences(IFoodItemReferences references) => _references.Add(references);

    public async Task<FoodItemDeletionPlan> PlanFoodItemDeletionAsync(string foodItemId)
    {
        var item = await GetExistingFoodItemAsync(foodItemId);

        var usedBy = new List<string>();
        foreach (var references in _references)
        {
            usedBy.AddRange(await references.FindUsesAsync(foodItemId));
        }

        return new FoodItemDeletionPlan(item, usedBy);
    }

    /// <summary>
    /// Deletes a Food Item that nothing uses; otherwise refuses, suggesting Retire.
    /// The record is kept as a tombstone so the deletion syncs to other devices.
    /// </summary>
    public async Task DeleteFoodItemAsync(string foodItemId)
    {
        var plan = await PlanFoodItemDeletionAsync(foodItemId);
        if (!plan.CanDelete)
        {
            throw new RuleViolationException(plan.Message);
        }

        await _store.SaveAsync(Table, plan.FoodItem with { Deleted = true, UpdatedAt = _clock.UtcNow });
    }
}
