using System.Text.Json.Nodes;
using BackpackingTracker.Core.Food;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Preferences;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Tests;

/// <summary>Exporting everything to a JSON file and importing such a file back, merged record by record.</summary>
public class BackupTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly InMemoryRecordStore _phoneStore = new();
    private readonly AppCore _phone;
    private readonly AppCore _laptop;

    public BackupTests()
    {
        // Two devices with their own data, sharing one clock so tests control which edit is newer.
        _phone = new AppCore(_phoneStore, _clock);
        _laptop = new AppCore(new InMemoryRecordStore(), _clock);
    }

    private void Later() => _clock.UtcNow = _clock.UtcNow.AddMinutes(5);

    private static Task<GearItem> AddGearAsync(AppCore app, string name, decimal grams = 1000m) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, "Tent", grams));

    private static async Task<string[]> GearNamesAsync(AppCore app) =>
        (await app.Gear.ListGearItemsAsync()).Select(item => item.Name).Order().ToArray();

    /// <summary>A phone with a bit of everything: gear, a Gear List, food, a Meal, a Trip and Settings.</summary>
    private async Task FillPhoneAsync()
    {
        var tent = await AddGearAsync(_phone, "Duplex");
        var list = await _phone.Lists.CreateGearListAsync("Summer");
        await _phone.Lists.AddEntryAsync(list.Id, tent.Id);
        var oats = await _phone.Food.AddFoodItemAsync(new FoodItemDetails("Oatmeal", 50m, 200m));
        await _phone.Meals.CreateMealAsync(new MealDetails("Breakfast", [new MealComponent(oats.Id, 2m)]));
        await _phone.Trips.CreateTripAsync("Wind Rivers", new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 3));
        await _phone.Settings.SetUnitSystemAsync(UnitSystem.Metric);
        await _phone.Gear.AddDefaultCategoryAsync("Fishing");
    }

    [Fact]
    public async Task An_export_holds_a_format_version_and_every_record_including_deleted_ones()
    {
        var kept = await AddGearAsync(_phone, "Duplex");
        var gone = await AddGearAsync(_phone, "Old tarp");
        await _phone.Gear.DeleteGearItemAsync(gone.Id);

        var file = JsonNode.Parse(await _phone.Backup.ExportAsync())!;

        Assert.Equal(SyncFile.FormatVersion, (int)file["formatVersion"]!);
        var gear = file["records"]!["gearItems"]!.AsArray();
        Assert.Contains(gear, record => (string)record!["id"]! == kept.Id);
        Assert.Contains(gear, record => (string)record!["id"]! == gone.Id && (bool)record["deleted"]!);
    }

    [Fact]
    public async Task Exporting_then_importing_into_an_empty_app_reproduces_the_same_data()
    {
        await FillPhoneAsync();
        var export = await _phone.Backup.ExportAsync();

        await _laptop.Backup.ImportAsync(export);

        Assert.Equal(export, await _laptop.Backup.ExportAsync());
        Assert.Equal(["Duplex"], await GearNamesAsync(_laptop));
        Assert.Equal(UnitSystem.Metric, (await _laptop.Settings.GetAsync()).UnitSystem);
    }

    [Fact]
    public async Task Importing_the_same_file_twice_changes_nothing_the_second_time()
    {
        await FillPhoneAsync();
        var export = await _phone.Backup.ExportAsync();
        await _laptop.Backup.ImportAsync(export);
        var afterFirst = await _laptop.Backup.ExportAsync();

        var summary = await _laptop.Backup.ImportAsync(export);

        Assert.True(summary.NothingChanged);
        Assert.Equal(afterFirst, await _laptop.Backup.ExportAsync());
    }

    [Fact]
    public async Task Records_only_in_the_file_are_added_alongside_existing_ones()
    {
        await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_laptop, "Stove");

        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());

        Assert.Equal(["Duplex", "Stove"], await GearNamesAsync(_laptop));
    }

    [Fact]
    public async Task A_newer_version_in_the_file_replaces_the_local_one()
    {
        var tent = await AddGearAsync(_phone, "Duplex", 1000m);
        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());
        Later();
        await _phone.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Duplex", "Tent", 950m));

        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());

        Assert.Equal(950m, (await _laptop.Gear.GetGearItemAsync(tent.Id))!.WeightGrams);
    }

    [Fact]
    public async Task An_older_version_in_the_file_leaves_the_local_one_alone()
    {
        var tent = await AddGearAsync(_phone, "Duplex", 1000m);
        var oldBackup = await _phone.Backup.ExportAsync();
        Later();
        await _phone.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Duplex", "Tent", 950m));

        var summary = await _phone.Backup.ImportAsync(oldBackup);

        Assert.True(summary.NothingChanged);
        Assert.Equal(950m, (await _phone.Gear.GetGearItemAsync(tent.Id))!.WeightGrams);
    }

    [Fact]
    public async Task A_deletion_in_the_file_deletes_an_older_local_record()
    {
        var tarp = await AddGearAsync(_phone, "Old tarp");
        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());
        Later();
        await _phone.Gear.DeleteGearItemAsync(tarp.Id);

        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());

        Assert.Null(await _laptop.Gear.GetGearItemAsync(tarp.Id));
    }

    [Fact]
    public async Task A_deletion_in_the_file_does_not_delete_a_record_edited_since()
    {
        var tarp = await AddGearAsync(_phone, "Old tarp");
        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());
        Later();
        await _phone.Gear.DeleteGearItemAsync(tarp.Id);
        Later();
        await _laptop.Gear.UpdateGearItemAsync(tarp.Id, new GearItemDetails("Old tarp", "Tarp", 300m));

        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());

        Assert.Equal(300m, (await _laptop.Gear.GetGearItemAsync(tarp.Id))!.WeightGrams);
    }

    [Fact]
    public async Task A_deletion_only_in_the_file_is_kept_so_it_still_syncs_but_is_not_reported()
    {
        var tarp = await AddGearAsync(_phone, "Old tarp");
        await _phone.Gear.DeleteGearItemAsync(tarp.Id);

        var summary = await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());

        Assert.True(summary.NothingChanged);
        Assert.Equal(await _phone.Backup.ExportAsync(), await _laptop.Backup.ExportAsync());
    }

    [Fact]
    public async Task The_summary_counts_what_was_added_updated_and_deleted_for_each_record_type()
    {
        var tent = await AddGearAsync(_phone, "Duplex");
        var tarp = await AddGearAsync(_phone, "Old tarp");
        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());
        Later();
        await _phone.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Duplex", "Tent", 950m));
        await _phone.Gear.DeleteGearItemAsync(tarp.Id);
        await AddGearAsync(_phone, "Stove");
        await _phone.Food.AddFoodItemAsync(new FoodItemDetails("Oatmeal", 50m, 200m));

        var summary = await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());

        Assert.False(summary.NothingChanged);
        Assert.Equal(
            [new RecordTypeChanges("Gear Items", Added: 1, Updated: 1, Deleted: 1), new RecordTypeChanges("Food Items", Added: 1, Updated: 0, Deleted: 0)],
            summary.Changes);
    }

    [Fact]
    public async Task A_file_with_an_unknown_format_version_is_refused_and_changes_nothing()
    {
        await AddGearAsync(_phone, "Duplex");
        var file = JsonNode.Parse(await _phone.Backup.ExportAsync())!;
        file["formatVersion"] = SyncFile.FormatVersion + 1;
        await AddGearAsync(_laptop, "Stove");
        var before = await _laptop.Backup.ExportAsync();

        var refusal = await Assert.ThrowsAsync<RuleViolationException>(() => _laptop.Backup.ImportAsync(file.ToJsonString()));

        Assert.Contains("version", refusal.Message);
        Assert.Equal(before, await _laptop.Backup.ExportAsync());
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{"name": "something else"}""")]
    [InlineData("""{"formatVersion": 1, "records": {"gearItems": [{"name": "no id"}]}}""")]
    public async Task A_file_that_is_not_an_export_is_refused_and_changes_nothing(string text)
    {
        await AddGearAsync(_laptop, "Stove");
        var before = await _laptop.Backup.ExportAsync();

        var refusal = await Assert.ThrowsAsync<RuleViolationException>(() => _laptop.Backup.ImportAsync(text));

        Assert.Contains("export", refusal.Message);
        Assert.Equal(before, await _laptop.Backup.ExportAsync());
    }

    [Fact]
    public async Task Gear_saved_before_gear_types_is_converted_when_imported()
    {
        // A record in the pre-ticket-27 layout, which the API can no longer create, so it's written directly.
        await _phoneStore.SaveAsync("gearItems", new GearItem
        {
            Id = Guid.NewGuid().ToString(),
            UpdatedAt = _clock.UtcNow,
            Name = "Tent",
            WeightGrams = 1000m,
            LegacyBrandModel = "Duplex",
        });

        await _laptop.Backup.ImportAsync(await _phone.Backup.ExportAsync());

        var tent = Assert.Single(await _laptop.Gear.ListGearItemsAsync());
        Assert.Equal(("Duplex", "Tent"), (tent.Name, tent.GearType));
    }
}
