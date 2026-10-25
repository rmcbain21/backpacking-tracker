using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Tests;

public class FoodItemTests
{
    private readonly InMemoryRecordStore _store = new();
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));

    private AppCore CreateApp() => new(_store, _clock);

    private static Task<FoodItem> AddAsync(AppCore app, string name, params string[] tags) =>
        app.Food.AddFoodItemAsync(new FoodItemDetails(name, 68m, 260m, Tags: tags));

    private static async Task<string[]> FoodNamesAsync(AppCore app, FoodFilter? filter = null) =>
        (await app.Food.ListFoodItemsAsync(filter ?? new FoodFilter())).Select(item => item.Name).ToArray();

    [Fact]
    public async Task An_added_food_item_is_remembered_after_the_app_is_reopened()
    {
        var added = await CreateApp().Food.AddFoodItemAsync(
            new FoodItemDetails("Clif Bar", 68m, 260m, ProteinGramsPerServing: 9m, CostPerServing: 1.79m, Tags: ["snack"]));

        var item = Assert.Single(await CreateApp().Food.ListFoodItemsAsync());

        Assert.Equal(added.Id, item.Id);
        Assert.Equal(("Clif Bar", 68m, 260m, 9m, 1.79m), (item.Name, item.ServingWeightGrams, item.CaloriesPerServing, item.ProteinGramsPerServing, item.CostPerServing));
        Assert.Equal(["snack"], item.Tags);
    }

    [Fact]
    public async Task Protein_and_cost_are_optional()
    {
        var item = await CreateApp().Food.AddFoodItemAsync(new FoodItemDetails("Ramen", 85m, 380m));

        Assert.Null(item.ProteinGramsPerServing);
        Assert.Null(item.CostPerServing);
    }

    [Fact]
    public async Task Food_items_are_listed_by_name()
    {
        var app = CreateApp();
        await AddAsync(app, "tortillas");
        await AddAsync(app, "Clif Bar");
        await AddAsync(app, "Peanut butter");

        Assert.Equal(["Clif Bar", "Peanut butter", "tortillas"], await FoodNamesAsync(app));
    }

    [Fact]
    public async Task Values_are_tidied_when_saved()
    {
        var item = await CreateApp().Food.AddFoodItemAsync(
            new FoodItemDetails("  Clif Bar  ", 68.04m, 259.6m, ProteinGramsPerServing: 9.25m, CostPerServing: 1.789m));

        Assert.Equal(("Clif Bar", 68.0m, 260m, 9.3m, 1.79m), (item.Name, item.ServingWeightGrams, item.CaloriesPerServing, item.ProteinGramsPerServing, item.CostPerServing));
    }

    [Theory]
    [InlineData("", 68, 260, null, null, "Give the Food Item a name.")]
    [InlineData("Clif Bar", 0, 260, null, null, "A serving must weigh more than zero.")]
    [InlineData("Clif Bar", 0.04, 260, null, null, "A serving must weigh more than zero.")]
    [InlineData("Clif Bar", 68, -1, null, null, "Calories can't be negative.")]
    [InlineData("Clif Bar", 68, 260, -1.0, null, "Protein can't be negative.")]
    [InlineData("Clif Bar", 68, 260, null, -1.0, "A cost can't be negative.")]
    public async Task Invalid_details_are_refused(string name, double grams, double calories, double? protein, double? cost, string message)
    {
        var app = CreateApp();
        var details = new FoodItemDetails(name, (decimal)grams, (decimal)calories, (decimal?)protein, (decimal?)cost);

        var refused = await Assert.ThrowsAsync<RuleViolationException>(() => app.Food.AddFoodItemAsync(details));

        Assert.Equal(message, refused.Message);
        Assert.Empty(await app.Food.ListFoodItemsAsync());
    }

    [Fact]
    public async Task Zero_calories_is_allowed_for_things_like_coffee()
    {
        var item = await CreateApp().Food.AddFoodItemAsync(new FoodItemDetails("Instant coffee", 2m, 0m));

        Assert.Equal(0m, item.CaloriesPerServing);
    }

    [Fact]
    public async Task Editing_replaces_the_details_and_keeps_the_id()
    {
        var app = CreateApp();
        var bar = await AddAsync(app, "Clif Bar");
        var later = _clock.UtcNow.AddDays(1);
        _clock.UtcNow = later;

        await app.Food.UpdateFoodItemAsync(bar.Id, new FoodItemDetails("Clif Builders", 68m, 290m, 20m, 2.29m, ["protein"]));

        var item = Assert.Single(await CreateApp().Food.ListFoodItemsAsync());
        Assert.Equal((bar.Id, "Clif Builders", 290m, 20m, later), (item.Id, item.Name, item.CaloriesPerServing, item.ProteinGramsPerServing, item.UpdatedAt));
    }

    [Fact]
    public async Task Editing_a_food_item_that_no_longer_exists_is_refused()
    {
        var refused = await Assert.ThrowsAsync<RuleViolationException>(
            () => CreateApp().Food.UpdateFoodItemAsync("gone", new FoodItemDetails("Clif Bar", 68m, 260m)));

        Assert.Equal("That Food Item no longer exists.", refused.Message);
    }

    [Fact]
    public async Task Tags_take_the_spelling_already_used_on_other_food()
    {
        var app = CreateApp();
        await AddAsync(app, "Oatmeal", "Breakfast");

        var granola = await AddAsync(app, "Granola", " breakfast ", "no-cook", "No-Cook");

        Assert.Equal(["Breakfast", "no-cook"], granola.Tags);
        Assert.Equal(["Breakfast", "no-cook"], await app.Food.ListTagsAsync());
    }

    [Fact]
    public async Task Food_tags_are_kept_apart_from_gear_tags()
    {
        var app = CreateApp();
        await app.Gear.AddGearItemAsync(new Gear.GearItemDetails("Tent", null, 900m, ["Breakfast"]));

        var oatmeal = await AddAsync(app, "Oatmeal", "breakfast");

        Assert.Equal(["breakfast"], oatmeal.Tags);
    }

    [Theory]
    [InlineData("bar", new[] { "Clif Bar", "Snickers bar" })]
    [InlineData("NO-COOK", new[] { "Clif Bar", "Tortillas" })]
    [InlineData("bar cook", new[] { "Clif Bar" })]
    [InlineData("  ", new[] { "Clif Bar", "Ramen", "Snickers bar", "Tortillas" })]
    [InlineData("cheese", new string[0])]
    public async Task Food_items_can_be_searched_by_name_and_tag(string search, string[] expected)
    {
        var app = CreateApp();
        await AddAsync(app, "Clif Bar", "no-cook", "snack");
        await AddAsync(app, "Snickers bar", "snack");
        await AddAsync(app, "Tortillas", "no-cook");
        await AddAsync(app, "Ramen", "dinner");

        Assert.Equal(expected, await FoodNamesAsync(app, new FoodFilter(search)));
    }

    // Calorie Density

    [Theory]
    [InlineData(68, 260, UnitSystem.Imperial, "108 cal/oz")]
    [InlineData(68, 260, UnitSystem.Metric, "382 cal/100 g")]
    [InlineData(85, 380, UnitSystem.Imperial, "127 cal/oz")]
    [InlineData(14, 120, UnitSystem.Metric, "857 cal/100 g")]
    [InlineData(2, 0, UnitSystem.Imperial, "0 cal/oz")]
    public async Task Calorie_density_is_per_ounce_in_imperial_and_per_100_g_in_metric(double grams, double calories, UnitSystem system, string expected)
    {
        var item = await CreateApp().Food.AddFoodItemAsync(new FoodItemDetails("Food", (decimal)grams, (decimal)calories));

        Assert.Equal(expected, CalorieDensity.Format(CalorieDensity.PerGram(item), system));
    }

    [Fact]
    public void Calorie_density_needs_a_weight()
    {
        Assert.Null(CalorieDensity.PerGram(500m, 0m));
        Assert.Equal("—", CalorieDensity.Format(null, UnitSystem.Imperial));
    }

    [Fact]
    public void Large_calorie_densities_get_a_thousands_separator()
    {
        Assert.Equal("1,250 cal/100 g", CalorieDensity.Format(12.5m, UnitSystem.Metric));
    }

    // Retire and delete

    [Fact]
    public async Task A_retired_food_item_leaves_the_food_page_and_is_listed_separately()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen");
        await AddAsync(app, "Clif Bar");

        await app.Food.RetireFoodItemAsync(ramen.Id);

        Assert.Equal(["Clif Bar"], await FoodNamesAsync(CreateApp()));
        Assert.Empty(await app.Food.ListFoodItemsAsync(new FoodFilter("ramen")));
        var retired = Assert.Single(await app.Food.ListRetiredFoodItemsAsync());
        Assert.Equal(("Ramen", true), (retired.Name, retired.Retired));
    }

    [Fact]
    public async Task A_retired_food_item_can_be_un_retired()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen");
        await app.Food.RetireFoodItemAsync(ramen.Id);

        await app.Food.UnretireFoodItemAsync(ramen.Id);

        Assert.Equal(["Ramen"], await FoodNamesAsync(CreateApp()));
        Assert.Empty(await app.Food.ListRetiredFoodItemsAsync());
    }

    [Fact]
    public async Task Retiring_stamps_the_food_item_with_the_current_time()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen");
        var later = _clock.UtcNow.AddDays(3);
        _clock.UtcNow = later;

        await app.Food.RetireFoodItemAsync(ramen.Id);

        Assert.Equal(later, Assert.Single(await app.Food.ListRetiredFoodItemsAsync()).UpdatedAt);
    }

    [Fact]
    public async Task A_retired_food_item_can_still_be_looked_up_by_id()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen");
        await app.Food.RetireFoodItemAsync(ramen.Id);

        var found = await CreateApp().Food.GetFoodItemAsync(ramen.Id);

        Assert.Equal("Ramen", found?.Name);
    }

    [Fact]
    public async Task Tags_only_on_retired_food_are_not_suggested()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen", "dinner");
        await AddAsync(app, "Clif Bar", "snack");

        await app.Food.RetireFoodItemAsync(ramen.Id);

        Assert.Equal(["snack"], await app.Food.ListTagsAsync());
    }

    [Fact]
    public async Task An_unused_food_item_can_be_deleted()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen");
        await AddAsync(app, "Clif Bar");

        var plan = await app.Food.PlanFoodItemDeletionAsync(ramen.Id);
        await app.Food.DeleteFoodItemAsync(ramen.Id);

        Assert.True(plan.CanDelete);
        Assert.Equal(("Delete Food Item", "Delete", "Delete Ramen? This can't be undone."), (plan.Title, plan.ConfirmButton, plan.Message));
        Assert.Equal(["Clif Bar"], await FoodNamesAsync(CreateApp()));
        Assert.Null(await app.Food.GetFoodItemAsync(ramen.Id));
    }

    [Fact]
    public async Task A_retired_food_item_can_be_deleted()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen");
        await app.Food.RetireFoodItemAsync(ramen.Id);

        await app.Food.DeleteFoodItemAsync(ramen.Id);

        Assert.Empty(await app.Food.ListRetiredFoodItemsAsync());
    }

    [Fact]
    public async Task Deleting_keeps_a_tombstone_so_the_deletion_syncs()
    {
        var app = CreateApp();
        var ramen = await AddAsync(app, "Ramen");
        var later = _clock.UtcNow.AddHours(2);
        _clock.UtcNow = later;

        await app.Food.DeleteFoodItemAsync(ramen.Id);

        var stored = await _store.LoadAsync<FoodItem>("foodItems", ramen.Id);
        Assert.NotNull(stored);
        Assert.True(stored.Deleted);
        Assert.Equal(later, stored.UpdatedAt);
    }

    [Fact]
    public async Task Deleting_or_retiring_a_food_item_that_no_longer_exists_is_refused()
    {
        var app = CreateApp();

        var delete = await Assert.ThrowsAsync<RuleViolationException>(() => app.Food.DeleteFoodItemAsync("gone"));
        var retire = await Assert.ThrowsAsync<RuleViolationException>(() => app.Food.RetireFoodItemAsync("gone"));

        Assert.Equal("That Food Item no longer exists.", delete.Message);
        Assert.Equal("That Food Item no longer exists.", retire.Message);
    }

    // The plan's wording on its own; MealTests covers the refusal through a real Meal.
    [Fact]
    public void A_food_item_in_use_cannot_be_deleted_and_retire_is_offered()
    {
        var ramen = new FoodItem { Id = "r", UpdatedAt = DateTimeOffset.UnixEpoch, Name = "Ramen", ServingWeightGrams = 85m, CaloriesPerServing = 380m };

        var inUse = new FoodItemDeletionPlan(ramen, ["Meal \"Ramen Bomb\""]);
        var inUseAndRetired = new FoodItemDeletionPlan(ramen with { Retired = true }, ["Meal \"Ramen Bomb\"", "Meal \"Pad Thai\""]);

        Assert.Equal(("Can't delete", "Retire"), (inUse.Title, inUse.ConfirmButton));
        Assert.Equal("Ramen can't be deleted because Meal \"Ramen Bomb\" uses it. Retire it instead?", inUse.Message);
        Assert.Null(inUseAndRetired.ConfirmButton);
        Assert.Equal("Ramen can't be deleted because Meal \"Ramen Bomb\" and Meal \"Pad Thai\" use it.", inUseAndRetired.Message);
    }

    // Typed nutrition

    [Theory]
    [InlineData("260", 260)]
    [InlineData(" 1,200 ", 1200)]
    [InlineData("259.5", 260)]
    [InlineData("0", 0)]
    public void Calories_are_read_to_the_nearest_whole_calorie(string text, decimal expected)
    {
        Assert.Equal(new NutritionParseResult(expected, null), NutritionInput.ParseCalories(text));
    }

    [Theory]
    [InlineData("", "Enter the calories in a serving.")]
    [InlineData("lots", "Enter a number, like 250.")]
    [InlineData("-5", "Calories can't be negative.")]
    public void Calories_are_required_and_must_be_a_number(string text, string error)
    {
        Assert.Equal(new NutritionParseResult(null, error), NutritionInput.ParseCalories(text));
    }

    [Theory]
    [InlineData("", null, null)]
    [InlineData("9.25", 9.3, null)]
    [InlineData("abc", null, "Enter a number, like 9.5.")]
    [InlineData("-1", null, "Protein can't be negative.")]
    public void Protein_is_optional_and_read_to_a_tenth_of_a_gram(string text, double? expected, string? error)
    {
        Assert.Equal(new NutritionParseResult((decimal?)expected, error), NutritionInput.ParseProtein(text));
    }
}
