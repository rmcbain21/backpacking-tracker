using System.Text.Json.Nodes;
using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Ports;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Tests.Fakes;
using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Tests;

/// <summary>Syncing two devices through one shared sync file.</summary>
public class SyncTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly InMemoryCloudFileStore _drive = new();
    private readonly AppCore _phone;
    private readonly AppCore _laptop;

    public SyncTests()
    {
        // Two devices with their own data and one Drive, sharing a clock so tests control which edit is newer.
        _phone = new AppCore(new InMemoryRecordStore(), _clock, _drive);
        _laptop = new AppCore(new InMemoryRecordStore(), _clock, _drive);
    }

    private void Later() => _clock.UtcNow = _clock.UtcNow.AddMinutes(5);

    private static Task<GearItem> AddGearAsync(AppCore app, string name, decimal grams = 1000m) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, "Tent", grams));

    private static Task UpdateWeightAsync(AppCore app, GearItem item, decimal grams) =>
        app.Gear.UpdateGearItemAsync(item.Id, new GearItemDetails(item.Name, item.GearType, grams));

    private static async Task<string[]> GearNamesAsync(AppCore app) =>
        (await app.Gear.ListGearItemsAsync()).Select(item => item.Name).Order().ToArray();

    private static async Task<decimal?> WeightAsync(AppCore app, GearItem item) =>
        (await app.Gear.GetGearItemAsync(item.Id))?.WeightGrams;

    private async Task ConnectBothAsync()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
    }

    [Fact]
    public async Task Gear_added_on_one_device_shows_up_on_the_other()
    {
        await ConnectBothAsync();
        await AddGearAsync(_phone, "Duplex");

        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();

        Assert.Equal(["Duplex"], await GearNamesAsync(_laptop));
    }

    [Fact]
    public async Task Each_device_ends_up_with_both_devices_additions()
    {
        await ConnectBothAsync();
        await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_laptop, "Stove");

        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();

        Assert.Equal(["Duplex", "Stove"], await GearNamesAsync(_phone));
        Assert.Equal(["Duplex", "Stove"], await GearNamesAsync(_laptop));
    }

    [Fact]
    public async Task Edits_travel_in_both_directions()
    {
        await ConnectBothAsync();
        var tent = await AddGearAsync(_phone, "Duplex", 1000m);
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();

        Later();
        await UpdateWeightAsync(_laptop, tent, 950m);
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();
        Assert.Equal(950m, await WeightAsync(_phone, tent));

        Later();
        await UpdateWeightAsync(_phone, tent, 900m);
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        Assert.Equal(900m, await WeightAsync(_laptop, tent));
    }

    [Fact]
    public async Task A_deletion_on_one_device_deletes_the_record_on_the_other()
    {
        await ConnectBothAsync();
        var tarp = await AddGearAsync(_phone, "Old tarp");
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();

        Later();
        await _laptop.Gear.DeleteGearItemAsync(tarp.Id);
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();

        Assert.Null(await _phone.Gear.GetGearItemAsync(tarp.Id));
    }

    [Fact]
    public async Task A_deletion_stays_deleted_after_the_other_device_syncs_again()
    {
        await ConnectBothAsync();
        var tarp = await AddGearAsync(_phone, "Old tarp");
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        Later();
        await _phone.Gear.DeleteGearItemAsync(tarp.Id);
        await _phone.Sync.SyncNowAsync();

        // The laptop still has the old copy, unchanged since the last sync: it mustn't bring it back.
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();

        Assert.Null(await _phone.Gear.GetGearItemAsync(tarp.Id));
        Assert.Null(await _laptop.Gear.GetGearItemAsync(tarp.Id));
    }

    [Fact]
    public async Task A_change_only_this_device_made_wins_even_if_its_clock_was_behind()
    {
        await ConnectBothAsync();
        var tent = await AddGearAsync(_phone, "Duplex", 1000m);
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();

        // The laptop's clock is wrong: its edit is stamped earlier than the version it last synced.
        _clock.UtcNow = _clock.UtcNow.AddHours(-1);
        await UpdateWeightAsync(_laptop, tent, 950m);
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();

        Assert.Equal(950m, await WeightAsync(_laptop, tent));
        Assert.Equal(950m, await WeightAsync(_phone, tent));
    }

    [Fact]
    public async Task When_both_devices_changed_a_record_the_later_edit_wins()
    {
        // Until Sync Conflicts (ticket 25) let the backpacker choose.
        await ConnectBothAsync();
        var tent = await AddGearAsync(_phone, "Duplex", 1000m);
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        Later();
        await UpdateWeightAsync(_phone, tent, 950m);
        Later();
        await UpdateWeightAsync(_laptop, tent, 900m);

        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();

        Assert.Equal(900m, await WeightAsync(_phone, tent));
        Assert.Equal(900m, await WeightAsync(_laptop, tent));
    }

    [Fact]
    public async Task A_new_devices_untouched_default_categories_do_not_undo_edits_made_elsewhere()
    {
        await ConnectBothAsync();
        var shelter = (await _phone.Gear.ListDefaultCategoriesAsync())[0];
        Later();
        await _phone.Gear.RenameDefaultCategoryAsync(shelter.Id, "Sleep system");
        await _phone.Sync.SyncNowAsync();

        // The laptop creates the same first-run defaults, never edited, before its first sync.
        await _laptop.Gear.ListDefaultCategoriesAsync();
        await _laptop.Sync.SyncNowAsync();

        Assert.Equal("Sleep system", (await _laptop.Gear.ListDefaultCategoriesAsync())[0].Name);
    }

    [Fact]
    public async Task Settings_sync_too()
    {
        await ConnectBothAsync();
        await _phone.Settings.SetUnitSystemAsync(UnitSystem.Metric);

        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();

        Assert.Equal(UnitSystem.Metric, (await _laptop.Settings.GetAsync()).UnitSystem);
    }

    [Fact]
    public async Task The_first_sync_with_no_file_yet_writes_this_devices_data_to_the_file()
    {
        await _phone.Sync.ConnectAsync();
        var tent = await AddGearAsync(_phone, "Duplex");

        await _phone.Sync.SyncNowAsync();

        var gear = JsonNode.Parse(_drive.Text!)!["records"]!["gearItems"]!.AsArray();
        Assert.Equal(tent.Id, (string)Assert.Single(gear)!["id"]!);
    }

    [Fact]
    public async Task The_sync_file_holds_no_sync_bookkeeping_and_neither_does_an_export()
    {
        await _phone.Sync.ConnectAsync();
        await AddGearAsync(_phone, "Duplex");

        await _phone.Sync.SyncNowAsync();

        Assert.DoesNotContain("syncState", _drive.Text);
        Assert.DoesNotContain("syncState", await _phone.Backup.ExportAsync());
    }

    [Fact]
    public async Task The_summary_says_what_arrived_from_other_devices()
    {
        await ConnectBothAsync();
        var tent = await AddGearAsync(_phone, "Duplex");
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        Later();
        await UpdateWeightAsync(_phone, tent, 950m);
        await AddGearAsync(_phone, "Stove");
        await _phone.Sync.SyncNowAsync();

        var summary = await _laptop.Sync.SyncNowAsync();

        Assert.Equal([new RecordTypeChanges("Gear Items", Added: 1, Updated: 1, Deleted: 0)], summary.Changes);
        Assert.True((await _laptop.Sync.SyncNowAsync()).NothingChanged);
    }

    [Fact]
    public async Task A_sync_records_when_it_happened()
    {
        await _phone.Sync.ConnectAsync();
        Assert.Null((await _phone.Sync.GetStatusAsync()).LastSyncedAt);
        Later();

        await _phone.Sync.SyncNowAsync();

        Assert.Equal(_clock.UtcNow, (await _phone.Sync.GetStatusAsync()).LastSyncedAt);
    }

    [Fact]
    public async Task A_failed_write_is_retried_by_the_next_sync()
    {
        await ConnectBothAsync();
        await _phone.Sync.SyncNowAsync();
        var lastSynced = (await _phone.Sync.GetStatusAsync()).LastSyncedAt;
        Later();
        await AddGearAsync(_phone, "Duplex");
        _drive.FailWrites = true;

        await Assert.ThrowsAsync<CloudFileStoreException>(() => _phone.Sync.SyncNowAsync());

        Assert.Equal(lastSynced, (await _phone.Sync.GetStatusAsync()).LastSyncedAt);
        _drive.FailWrites = false;
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        Assert.Equal(["Duplex"], await GearNamesAsync(_laptop));
    }

    [Fact]
    public async Task A_device_that_syncs_in_the_middle_of_another_devices_sync_keeps_its_changes()
    {
        await ConnectBothAsync();
        await AddGearAsync(_phone, "Duplex");
        await AddGearAsync(_laptop, "Stove");
        _drive.AfterNextRead = () => _laptop.Sync.SyncNowAsync();

        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();

        Assert.Equal(["Duplex", "Stove"], await GearNamesAsync(_phone));
        Assert.Equal(["Duplex", "Stove"], await GearNamesAsync(_laptop));
    }

    [Fact]
    public async Task Syncing_needs_google_drive_connected_first()
    {
        var refusal = await Assert.ThrowsAsync<RuleViolationException>(() => _phone.Sync.SyncNowAsync());

        Assert.Contains("Connect", refusal.Message);
    }

    [Fact]
    public async Task Disconnecting_keeps_the_data_on_the_device()
    {
        await _phone.Sync.ConnectAsync();
        await AddGearAsync(_phone, "Duplex");
        await _phone.Sync.SyncNowAsync();

        await _phone.Sync.DisconnectAsync();

        var status = await _phone.Sync.GetStatusAsync();
        Assert.False(status.Connected);
        Assert.Null(status.LastSyncedAt);
        Assert.Equal(["Duplex"], await GearNamesAsync(_phone));
    }

    [Fact]
    public async Task Being_connected_is_remembered_after_the_app_is_reopened()
    {
        var store = new InMemoryRecordStore();
        await new AppCore(store, _clock, _drive).Sync.ConnectAsync();

        Assert.True((await new AppCore(store, _clock, _drive).Sync.GetStatusAsync()).Connected);
    }

    [Fact]
    public async Task A_sync_file_in_an_unknown_format_is_refused_and_changes_nothing()
    {
        await _phone.Sync.ConnectAsync();
        _drive.Text = """{"formatVersion": 99, "records": {}}""";
        await AddGearAsync(_phone, "Duplex");

        var refusal = await Assert.ThrowsAsync<RuleViolationException>(() => _phone.Sync.SyncNowAsync());

        Assert.Contains("newer version", refusal.Message);
        Assert.Contains("99", _drive.Text);
        Assert.Null((await _phone.Sync.GetStatusAsync()).LastSyncedAt);
    }
}
