using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Food;

/// <summary>
/// The backpacker's Meals, with totals always from each Food Item's current values. Retire and delete
/// follow the same rules as Gear Items and Food Items.
/// </summary>
public sealed class MealCatalogue : IFoodItemReferences
{
    internal const string Table = "meals";

    private readonly IRecordStore _store;
    private readonly IClock _clock;
    private readonly FoodCatalogue _food;
    private readonly List<IMealReferences> _references = [];

    public MealCatalogue(IRecordStore store, IClock clock, FoodCatalogue food)
    {
        _store = store;
        _clock = clock;
        _food = food;
    }

    public async Task<Meal> CreateMealAsync(MealDetails details)
    {
        var meal = await ApplyDetailsAsync(
            new Meal { Id = Guid.NewGuid().ToString(), UpdatedAt = _clock.UtcNow, Name = "" },
            details);

        await _store.SaveAsync(Table, meal);
        return meal;
    }

    /// <summary>Replaces every field the backpacker can edit, applying the same rules as creating.</summary>
    public async Task<Meal> UpdateMealAsync(string mealId, MealDetails details)
    {
        var existing = await GetExistingMealAsync(mealId);

        var meal = await ApplyDetailsAsync(existing with { UpdatedAt = _clock.UtcNow }, details);

        await _store.SaveAsync(Table, meal);
        return meal;
    }

    /// <summary>
    /// Checks the details and copies them, cleaned up, onto the Meal. The same Food Item given twice becomes one
    /// component with the servings added together. A Retired Food Item can stay in a Meal that already has it,
    /// but can't be newly added.
    /// </summary>
    private async Task<Meal> ApplyDetailsAsync(Meal meal, MealDetails details)
    {
        if (string.IsNullOrWhiteSpace(details.Name))
        {
            throw new RuleViolationException("Give the Meal a name.");
        }

        var components = new List<MealComponent>();
        foreach (var component in details.Components)
        {
            var servings = ServingsInput.Round(component.Servings);
            if (servings <= 0)
            {
                throw new RuleViolationException("Servings must be more than zero.");
            }

            var index = components.FindIndex(c => c.FoodItemId == component.FoodItemId);
            if (index >= 0)
            {
                components[index] = components[index] with { Servings = components[index].Servings + servings };
            }
            else
            {
                components.Add(new MealComponent(component.FoodItemId, servings));
            }
        }

        if (components.Count == 0)
        {
            throw new RuleViolationException("Add at least one Food Item to the Meal.");
        }

        var foodItems = await _food.GetFoodItemsAsync(components.Select(c => c.FoodItemId));
        foreach (var component in components)
        {
            if (!foodItems.TryGetValue(component.FoodItemId, out var item))
            {
                throw new RuleViolationException("One of the Food Items no longer exists.");
            }

            if (item.Retired && !meal.Components.Any(c => c.FoodItemId == item.Id))
            {
                throw new RuleViolationException($"{item.Name} is Retired, so it can't be added to a Meal.");
            }
        }

        return meal with
        {
            Name = details.Name.Trim(),
            Components = components,
            Tags = await CleanUpTagsAsync(details.Tags),
        };
    }

    /// <summary>The Food page's Meals: those in use, by Name, with their totals. Leaves out Retired ones.</summary>
    public Task<IReadOnlyList<MealView>> ListMealsAsync() => ListMealsAsync(new MealFilter());

    /// <summary>The Meals in use narrowed by search text, by Name, with their totals.</summary>
    public async Task<IReadOnlyList<MealView>> ListMealsAsync(MealFilter filter) =>
        (await ViewAsync((await LoadMealsAsync()).Where(meal => !meal.Retired).ToList()))
            .Where(filter.Matches)
            .ToList();

    public async Task<IReadOnlyList<Meal>> ListRetiredMealsAsync() =>
        (await LoadMealsAsync()).Where(meal => meal.Retired).ToList();

    /// <summary>
    /// A Meal by id, Retired or not, with its totals, so anything that refers to it can still show it.
    /// Null if it doesn't exist or was deleted.
    /// </summary>
    public async Task<MealView?> GetMealAsync(string mealId)
    {
        var meal = (await LoadMealsAsync()).SingleOrDefault(m => m.Id == mealId);
        return meal is null ? null : (await ViewAsync([meal]))[0];
    }

    /// <summary>Looks up each Meal's Food Items (loading the catalogue once) and adds up its totals.</summary>
    private async Task<IReadOnlyList<MealView>> ViewAsync(IReadOnlyList<Meal> meals)
    {
        var foodItems = await _food.GetFoodItemsAsync(meals.SelectMany(m => m.Components).Select(c => c.FoodItemId));
        return meals.Select(meal =>
        {
            var components = meal.Components
                .Select(c => new MealComponentView(c, foodItems.GetValueOrDefault(c.FoodItemId)))
                .ToList();
            var totals = MealTotals.Of(components
                .Where(c => c.FoodItem is not null)
                .Select(c => (c.FoodItem!, c.Component.Servings)));
            return new MealView(meal, components, totals);
        }).ToList();
    }

    private async Task<Meal> GetExistingMealAsync(string mealId) =>
        (await LoadMealsAsync()).SingleOrDefault(m => m.Id == mealId)
            ?? throw new RuleViolationException("That Meal no longer exists.");

    public Task RetireMealAsync(string mealId) => SetRetiredAsync(mealId, retired: true);

    public Task UnretireMealAsync(string mealId) => SetRetiredAsync(mealId, retired: false);

    private async Task SetRetiredAsync(string mealId, bool retired)
    {
        var meal = await GetExistingMealAsync(mealId);

        await _store.SaveAsync(Table, meal with { Retired = retired, UpdatedAt = _clock.UtcNow });
    }

    /// <summary>Every Meal that hasn't been deleted, Retired or not, by Name.</summary>
    private async Task<IReadOnlyList<Meal>> LoadMealsAsync()
    {
        var meals = await _store.ListAsync<Meal>(Table);
        return meals
            .Where(meal => !meal.Deleted)
            .OrderBy(meal => meal.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Every Tag in use on a Meal, once each, in alphabetical order; used for suggestions.</summary>
    public async Task<IReadOnlyList<string>> ListTagsAsync() =>
        (await LoadMealsAsync())
            .Where(meal => !meal.Retired)
            .SelectMany(meal => meal.Tags)
            .Distinct(TagRules.Comparer)
            .Order(TagRules.Comparer)
            .ToList();

    /// <summary>
    /// Trims Tags, drops blank and repeated ones (ignoring case), and gives a Tag the spelling it
    /// already has on other Meals, so "Dinner" and "dinner" don't become two Tags.
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

    /// <summary>The Meals that use the Food Item, Retired or not, by name, so it can't be deleted from under them.</summary>
    public async Task<IReadOnlyList<string>> FindUsesAsync(string foodItemId) =>
        (await LoadMealsAsync())
            .Where(meal => meal.Components.Any(c => c.FoodItemId == foodItemId))
            .Select(meal => $"Meal \"{meal.Name}\"")
            .ToList();

    /// <summary>Lets an area that refers to Meals (Meal Plans) take part in the delete check. Called by AppCore.</summary>
    internal void AddReferences(IMealReferences references) => _references.Add(references);

    public async Task<MealDeletionPlan> PlanMealDeletionAsync(string mealId)
    {
        var meal = await GetExistingMealAsync(mealId);

        var usedBy = new List<string>();
        foreach (var references in _references)
        {
            usedBy.AddRange(await references.FindUsesAsync(mealId));
        }

        return new MealDeletionPlan(meal, usedBy);
    }

    /// <summary>
    /// Deletes a Meal that nothing uses; otherwise refuses, suggesting Retire.
    /// The record is kept as a tombstone so the deletion syncs to other devices.
    /// </summary>
    public async Task DeleteMealAsync(string mealId)
    {
        var plan = await PlanMealDeletionAsync(mealId);
        if (!plan.CanDelete)
        {
            throw new RuleViolationException(plan.Message);
        }

        await _store.SaveAsync(Table, plan.Meal with { Deleted = true, UpdatedAt = _clock.UtcNow });
    }
}
