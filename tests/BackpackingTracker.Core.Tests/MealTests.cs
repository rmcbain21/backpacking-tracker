using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Tests;

public class MealTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static Task<FoodItem> RamenAsync(AppCore app) =>
        app.Food.AddFoodItemAsync(new FoodItemDetails("Ramen", 85m, 380m, ProteinGramsPerServing: 10m, CostPerServing: 0.50m));

    private static Task<FoodItem> PotatoesAsync(AppCore app) =>
        app.Food.AddFoodItemAsync(new FoodItemDetails("Instant potatoes", 28m, 100m, ProteinGramsPerServing: 2m));

    /// <summary>A Ramen Bomb: one packet of ramen and 1.5 servings of instant potatoes.</summary>
    private static async Task<(Meal Meal, FoodItem Ramen, FoodItem Potatoes)> RamenBombAsync(AppCore app, params string[] tags)
    {
        var ramen = await RamenAsync(app);
        var potatoes = await PotatoesAsync(app);
        var meal = await app.Meals.CreateMealAsync(
            new MealDetails("Ramen Bomb", [new(ramen.Id, 1m), new(potatoes.Id, 1.5m)], tags));
        return (meal, ramen, potatoes);
    }

    private static async Task<string[]> MealNamesAsync(AppCore app, MealFilter? filter = null) =>
        (await app.Meals.ListMealsAsync(filter ?? new MealFilter())).Select(view => view.Meal.Name).ToArray();

    [Fact]
    public async Task A_created_meal_is_remembered_after_the_app_is_reopened()
    {
        var (meal, ramen, potatoes) = await RamenBombAsync(CreateApp(), "dinner");

        var view = Assert.Single(await CreateApp().Meals.ListMealsAsync());

        Assert.Equal((meal.Id, "Ramen Bomb"), (view.Meal.Id, view.Meal.Name));
        Assert.Equal([new MealComponent(ramen.Id, 1m), new MealComponent(potatoes.Id, 1.5m)], view.Meal.Components);
        Assert.Equal(["Ramen", "Instant potatoes"], view.Components.Select(c => c.FoodItem?.Name));
        Assert.Equal(["dinner"], view.Meal.Tags);
    }

    [Fact]
    public async Task A_meal_adds_up_its_food_items_times_their_servings()
    {
        var app = CreateApp();
        await RamenBombAsync(app);

        var totals = Assert.Single(await app.Meals.ListMealsAsync()).Totals;

        Assert.Equal((127m, 530m, 13m), (totals.WeightGrams, totals.Calories, totals.ProteinGrams));
        Assert.Equal("118 cal/oz", CalorieDensity.Format(totals.CaloriesPerGram, UnitSystem.Imperial));
        Assert.Equal("417 cal/100 g", CalorieDensity.Format(totals.CaloriesPerGram, UnitSystem.Metric));
    }

    [Fact]
    public async Task Protein_and_cost_count_only_the_food_items_that_record_them()
    {
        var app = CreateApp();
        await RamenBombAsync(app);
        var water = await app.Food.AddFoodItemAsync(new FoodItemDetails("Electrolyte tab", 4m, 10m));
        await app.Meals.CreateMealAsync(new MealDetails("Drink", [new(water.Id, 2m)]));

        var views = await app.Meals.ListMealsAsync();

        var drink = views.Single(v => v.Meal.Name == "Drink").Totals;
        var bomb = views.Single(v => v.Meal.Name == "Ramen Bomb").Totals;
        Assert.Equal((null, null), (drink.ProteinGrams, drink.Cost));
        Assert.Equal(0.50m, bomb.Cost);
    }

    [Fact]
    public async Task Totals_follow_the_food_items_current_values()
    {
        var app = CreateApp();
        var (_, ramen, _) = await RamenBombAsync(app);

        await app.Food.UpdateFoodItemAsync(ramen.Id, new FoodItemDetails("Ramen", 90m, 400m, 10m, 0.50m));

        var totals = Assert.Single(await app.Meals.ListMealsAsync()).Totals;
        Assert.Equal((132m, 550m), (totals.WeightGrams, totals.Calories));
    }

    [Fact]
    public async Task Meals_are_listed_by_name()
    {
        var app = CreateApp();
        var ramen = await RamenAsync(app);
        foreach (var name in new[] { "ramen bomb", "Breakfast", "Pad Thai" })
        {
            await app.Meals.CreateMealAsync(new MealDetails(name, [new(ramen.Id, 1m)]));
        }

        Assert.Equal(["Breakfast", "Pad Thai", "ramen bomb"], await MealNamesAsync(app));
    }

    [Fact]
    public async Task The_same_food_item_given_twice_becomes_one_component()
    {
        var app = CreateApp();
        var ramen = await RamenAsync(app);

        var meal = await app.Meals.CreateMealAsync(new MealDetails("Double ramen", [new(ramen.Id, 1m), new(ramen.Id, 0.5m)]));

        Assert.Equal([new MealComponent(ramen.Id, 1.5m)], meal.Components);
    }

    [Fact]
    public async Task Names_are_trimmed_and_servings_rounded_to_a_hundredth()
    {
        var app = CreateApp();
        var ramen = await RamenAsync(app);

        var meal = await app.Meals.CreateMealAsync(new MealDetails("  Ramen  ", [new(ramen.Id, 0.333m)]));

        Assert.Equal(("Ramen", 0.33m), (meal.Name, Assert.Single(meal.Components).Servings));
    }

    [Fact]
    public async Task Invalid_meals_are_refused()
    {
        var app = CreateApp();
        var ramen = await RamenAsync(app);

        async Task<string> RefusalAsync(MealDetails details) =>
            (await Assert.ThrowsAsync<RuleViolationException>(() => app.Meals.CreateMealAsync(details))).Message;

        Assert.Equal("Give the Meal a name.", await RefusalAsync(new MealDetails(" ", [new(ramen.Id, 1m)])));
        Assert.Equal("Add at least one Food Item to the Meal.", await RefusalAsync(new MealDetails("Empty", [])));
        Assert.Equal("Servings must be more than zero.", await RefusalAsync(new MealDetails("None", [new(ramen.Id, 0m)])));
        Assert.Equal("Servings must be more than zero.", await RefusalAsync(new MealDetails("Tiny", [new(ramen.Id, 0.004m)])));
        Assert.Equal("One of the Food Items no longer exists.", await RefusalAsync(new MealDetails("Ghost", [new("gone", 1m)])));
        Assert.Empty(await app.Meals.ListMealsAsync());
    }

    [Fact]
    public async Task Editing_replaces_the_details_and_keeps_the_id()
    {
        var app = CreateApp();
        var (meal, ramen, _) = await RamenBombAsync(app);
        var later = _clock.UtcNow.AddDays(1);
        _clock.UtcNow = later;

        await app.Meals.UpdateMealAsync(meal.Id, new MealDetails("Plain ramen", [new(ramen.Id, 2m)], ["quick"]));

        var view = Assert.Single(await CreateApp().Meals.ListMealsAsync());
        Assert.Equal((meal.Id, "Plain ramen", later), (view.Meal.Id, view.Meal.Name, view.Meal.UpdatedAt));
        Assert.Equal([new MealComponent(ramen.Id, 2m)], view.Meal.Components);
        Assert.Equal(["quick"], view.Meal.Tags);
    }

    [Fact]
    public async Task A_retired_food_item_cannot_be_added_to_a_meal()
    {
        var app = CreateApp();
        var ramen = await RamenAsync(app);
        await app.Food.RetireFoodItemAsync(ramen.Id);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Meals.CreateMealAsync(new MealDetails("Ramen", [new(ramen.Id, 1m)])));

        Assert.Equal("Ramen is Retired, so it can't be added to a Meal.", refused.Message);
    }

    [Fact]
    public async Task A_meal_keeps_a_food_item_retired_after_it_was_added_and_still_counts_it()
    {
        var app = CreateApp();
        var (meal, ramen, potatoes) = await RamenBombAsync(app);
        await app.Food.RetireFoodItemAsync(ramen.Id);

        await app.Meals.UpdateMealAsync(meal.Id, new MealDetails("Ramen Bomb", [new(ramen.Id, 1m), new(potatoes.Id, 2m)]));

        var view = Assert.Single(await app.Meals.ListMealsAsync());
        Assert.Equal(141m, view.Totals.WeightGrams);
        Assert.True(view.Components[0].FoodItem?.Retired);
    }

    [Fact]
    public async Task Meal_tags_take_the_spelling_already_used_on_other_meals()
    {
        var app = CreateApp();
        var ramen = await RamenAsync(app);
        await app.Meals.CreateMealAsync(new MealDetails("Ramen Bomb", [new(ramen.Id, 1m)], ["Dinner"]));

        var padThai = await app.Meals.CreateMealAsync(new MealDetails("Pad Thai", [new(ramen.Id, 1m)], [" dinner ", "hot", "Hot"]));

        Assert.Equal(["Dinner", "hot"], padThai.Tags);
        Assert.Equal(["Dinner", "hot"], await app.Meals.ListTagsAsync());
    }

    [Theory]
    [InlineData("bomb", new[] { "Ramen Bomb" })]
    [InlineData("DINNER", new[] { "Pad Thai", "Ramen Bomb" })]
    [InlineData("potatoes", new[] { "Ramen Bomb" })]
    [InlineData("ramen dinner", new[] { "Pad Thai", "Ramen Bomb" })]
    [InlineData("cheese", new string[0])]
    public async Task Meals_can_be_searched_by_name_tag_and_food_item(string search, string[] expected)
    {
        var app = CreateApp();
        var (_, ramen, _) = await RamenBombAsync(app, "dinner");
        await app.Meals.CreateMealAsync(new MealDetails("Pad Thai", [new(ramen.Id, 1m)], ["dinner"]));

        Assert.Equal(expected, await MealNamesAsync(app, new MealFilter(search)));
    }

    // Retire and delete

    [Fact]
    public async Task A_retired_meal_leaves_the_food_page_and_can_be_un_retired()
    {
        var app = CreateApp();
        var (meal, _, _) = await RamenBombAsync(app);

        await app.Meals.RetireMealAsync(meal.Id);

        Assert.Empty(await app.Meals.ListMealsAsync());
        Assert.Equal(["Ramen Bomb"], (await app.Meals.ListRetiredMealsAsync()).Select(m => m.Name));
        Assert.Equal("Ramen Bomb", (await app.Meals.GetMealAsync(meal.Id))?.Meal.Name);

        await app.Meals.UnretireMealAsync(meal.Id);

        Assert.Equal(["Ramen Bomb"], await MealNamesAsync(CreateApp()));
        Assert.Empty(await app.Meals.ListRetiredMealsAsync());
    }

    [Fact]
    public async Task An_unused_meal_can_be_deleted_leaving_a_tombstone()
    {
        var app = CreateApp();
        var (meal, _, _) = await RamenBombAsync(app);
        var later = _clock.UtcNow.AddHours(2);
        _clock.UtcNow = later;

        var plan = await app.Meals.PlanMealDeletionAsync(meal.Id);
        await app.Meals.DeleteMealAsync(meal.Id);

        Assert.Equal(("Delete Meal", "Delete Ramen Bomb? This can't be undone."), (plan.Title, plan.Message));
        Assert.Empty(await app.Meals.ListMealsAsync());
        Assert.Null(await app.Meals.GetMealAsync(meal.Id));
        var stored = await _store.LoadAsync<Meal>("meals", meal.Id);
        Assert.Equal((true, later), (stored!.Deleted, stored.UpdatedAt));
    }

    [Fact]
    public async Task Changing_a_meal_that_no_longer_exists_is_refused()
    {
        var app = CreateApp();
        var ramen = await RamenAsync(app);

        var update = await Assert.ThrowsAsync<RuleViolationException>(
            () => app.Meals.UpdateMealAsync("gone", new MealDetails("Ramen", [new(ramen.Id, 1m)])));
        var delete = await Assert.ThrowsAsync<RuleViolationException>(() => app.Meals.DeleteMealAsync("gone"));

        Assert.Equal("That Meal no longer exists.", update.Message);
        Assert.Equal("That Meal no longer exists.", delete.Message);
    }

    // The Food Item delete guard counts Meals

    [Fact]
    public async Task A_food_item_in_a_meal_cannot_be_deleted_and_retire_is_offered()
    {
        var app = CreateApp();
        var (_, ramen, _) = await RamenBombAsync(app);

        var plan = await app.Food.PlanFoodItemDeletionAsync(ramen.Id);
        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Food.DeleteFoodItemAsync(ramen.Id));

        Assert.Equal(["Meal \"Ramen Bomb\""], plan.UsedBy);
        Assert.Equal("Retire", plan.ConfirmButton);
        Assert.Equal("Ramen can't be deleted because Meal \"Ramen Bomb\" uses it. Retire it instead?", refused.Message);
        Assert.NotNull(await app.Food.GetFoodItemAsync(ramen.Id));
    }

    [Fact]
    public async Task A_retired_meal_still_counts_as_using_its_food_items()
    {
        var app = CreateApp();
        var (meal, ramen, _) = await RamenBombAsync(app);
        await app.Meals.CreateMealAsync(new MealDetails("Pad Thai", [new(ramen.Id, 1m)]));
        await app.Meals.RetireMealAsync(meal.Id);

        var plan = await app.Food.PlanFoodItemDeletionAsync(ramen.Id);

        Assert.Equal(["Meal \"Pad Thai\"", "Meal \"Ramen Bomb\""], plan.UsedBy);
    }

    [Fact]
    public async Task A_food_item_can_be_deleted_once_no_meal_uses_it()
    {
        var app = CreateApp();
        var (meal, ramen, potatoes) = await RamenBombAsync(app);

        await app.Meals.UpdateMealAsync(meal.Id, new MealDetails("Potatoes", [new(potatoes.Id, 2m)]));
        await app.Food.DeleteFoodItemAsync(ramen.Id);

        Assert.Null(await app.Food.GetFoodItemAsync(ramen.Id));
    }

    // Typed servings

    [Theory]
    [InlineData("1", 1)]
    [InlineData(" 0.5 ", 0.5)]
    [InlineData("1.333", 1.33)]
    public void Servings_are_read_to_a_hundredth(string text, decimal expected)
    {
        Assert.Equal(new ServingsParseResult(expected, null), ServingsInput.Parse(text));
    }

    [Theory]
    [InlineData("", "Enter the servings.")]
    [InlineData("half", "Enter a number, like 1.5.")]
    [InlineData("0", "Servings must be more than zero.")]
    [InlineData("-1", "Servings must be more than zero.")]
    public void Servings_must_be_a_number_above_zero(string text, string error)
    {
        Assert.Equal(new ServingsParseResult(null, error), ServingsInput.Parse(text));
    }

    [Theory]
    [InlineData(1, "1")]
    [InlineData(0.5, "0.5")]
    [InlineData(1.25, "1.25")]
    public void Servings_are_shown_without_trailing_zeros(decimal servings, string expected)
    {
        Assert.Equal(expected, ServingsInput.Format(servings));
    }
}
