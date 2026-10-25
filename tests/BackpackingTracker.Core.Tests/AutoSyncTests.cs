using BackpackingTracker.Core.Gear;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Core.Tests.Fakes;

namespace BackpackingTracker.Core.Tests;

/// <summary>Sync running by itself: on app open, a few seconds after changes, and again after a problem.</summary>
public class AutoSyncTests
{
    private readonly FakeClock _clock = new(new DateTimeOffset(2026, 5, 1, 9, 30, 0, TimeSpan.Zero));
    private readonly InMemoryCloudFileStore _drive = new();
    private readonly AppCore _phone;
    private readonly AppCore _laptop;

    public AutoSyncTests()
    {
        _phone = new AppCore(new InMemoryRecordStore(), _clock, _drive);
        _laptop = new AppCore(new InMemoryRecordStore(), _clock, _drive);
    }

    private void Wait(double seconds) => _clock.UtcNow = _clock.UtcNow.AddSeconds(seconds);

    private Task<GearItem> AddGearAsync(AppCore app, string name) =>
        app.Gear.AddGearItemAsync(new GearItemDetails(name, "Tent", 1000m));

    private static async Task<string[]> GearNamesAsync(AppCore app) =>
        (await app.Gear.ListGearItemsAsync()).Select(item => item.Name).Order().ToArray();

    /// <summary>The phone, connected and opened, with its opening sync done.</summary>
    private async Task OpenConnectedPhoneAsync()
    {
        await _phone.Sync.ConnectAsync();
        _phone.AutoSync.AppOpened();
        await _phone.AutoSync.TickAsync();
    }

    [Fact]
    public async Task Opening_the_app_syncs_when_connected()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        await AddGearAsync(_laptop, "Stove");
        await _laptop.Sync.SyncNowAsync();

        _phone.AutoSync.AppOpened();
        await _phone.AutoSync.TickAsync();

        Assert.Equal(["Stove"], await GearNamesAsync(_phone));
    }

    [Fact]
    public async Task Nothing_syncs_when_google_drive_is_not_connected()
    {
        _phone.AutoSync.AppOpened();
        await AddGearAsync(_phone, "Duplex");
        Wait(10);

        await _phone.AutoSync.TickAsync();

        Assert.Equal(0, _drive.Reads);
        Assert.Equal(SyncProblem.None, _phone.AutoSync.Problem);
    }

    [Fact]
    public async Task A_change_syncs_a_few_seconds_later_not_straight_away()
    {
        await OpenConnectedPhoneAsync();
        var readsAfterOpening = _drive.Reads;
        await AddGearAsync(_phone, "Duplex");

        Wait(AutoSync.ChangeDelay.TotalSeconds - 1);
        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsAfterOpening, _drive.Reads);

        Wait(1);
        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsAfterOpening + 1, _drive.Reads);
        Assert.Contains("Duplex", _drive.Text);
    }

    [Fact]
    public async Task Changes_in_quick_succession_sync_once_after_the_last_one()
    {
        await OpenConnectedPhoneAsync();
        var readsAfterOpening = _drive.Reads;

        for (var i = 0; i < 4; i++)
        {
            await AddGearAsync(_phone, $"Stake {i}");
            Wait(AutoSync.ChangeDelay.TotalSeconds - 1);
            await _phone.AutoSync.TickAsync();
        }

        Assert.Equal(readsAfterOpening, _drive.Reads);
        Wait(1);
        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsAfterOpening + 1, _drive.Reads);
    }

    [Fact]
    public async Task What_a_sync_brings_in_does_not_set_off_another_sync()
    {
        await _laptop.Sync.ConnectAsync();
        await AddGearAsync(_laptop, "Stove");
        await _laptop.Sync.SyncNowAsync();
        await OpenConnectedPhoneAsync();
        var readsAfterOpening = _drive.Reads;

        Wait(60);
        await _phone.AutoSync.TickAsync();

        Assert.Equal(readsAfterOpening, _drive.Reads);
    }

    [Fact]
    public async Task Nothing_syncs_while_offline_and_it_syncs_on_coming_back_online()
    {
        await OpenConnectedPhoneAsync();
        _phone.AutoSync.SetOnline(false);
        var readsBefore = _drive.Reads;
        await AddGearAsync(_phone, "Duplex");
        Wait(60);

        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsBefore, _drive.Reads);
        Assert.Equal(SyncProblem.Offline, _phone.AutoSync.Problem);

        _phone.AutoSync.SetOnline(true);
        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsBefore + 1, _drive.Reads);
        Assert.Equal(SyncProblem.None, _phone.AutoSync.Problem);
    }

    [Fact]
    public async Task A_sync_never_starts_while_another_is_running()
    {
        await OpenConnectedPhoneAsync();
        await AddGearAsync(_phone, "Duplex");
        Wait(AutoSync.ChangeDelay.TotalSeconds);
        var readsBefore = _drive.Reads;
        _drive.AfterNextRead = async () =>
        {
            // A change and a tick arrive mid-sync: the tick must not start a second sync.
            await AddGearAsync(_phone, "Stove");
            Wait(AutoSync.ChangeDelay.TotalSeconds);
            await _phone.AutoSync.TickAsync();
        };

        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsBefore + 1, _drive.Reads);

        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsBefore + 2, _drive.Reads);
        Assert.Contains("Stove", _drive.Text);
    }

    [Fact]
    public async Task An_expired_sign_in_asks_to_sign_in_and_stops_trying_until_then()
    {
        await _phone.Sync.ConnectAsync();
        _drive.SignedIn = false;
        _phone.AutoSync.AppOpened();

        await _phone.AutoSync.TickAsync();
        Assert.Equal(SyncProblem.SignInNeeded, _phone.AutoSync.Problem);

        var readsBefore = _drive.Reads;
        await AddGearAsync(_phone, "Duplex");
        Wait(120);
        await _phone.AutoSync.TickAsync();
        Assert.Equal(SyncProblem.SignInNeeded, _phone.AutoSync.Problem);
        Assert.Equal(1, _drive.SignInRefusals);

        _drive.SignedIn = true;
        await _phone.AutoSync.SyncNowAsync();
        Assert.Equal(SyncProblem.None, _phone.AutoSync.Problem);
        Assert.Contains("Duplex", _drive.Text);
        Assert.Equal(readsBefore + 1, _drive.Reads);
    }

    [Fact]
    public async Task A_failed_sync_shows_why_keeps_local_data_and_tries_again_a_minute_later()
    {
        await OpenConnectedPhoneAsync();
        _drive.FailWrites = true;
        await AddGearAsync(_phone, "Duplex");
        Wait(AutoSync.ChangeDelay.TotalSeconds);

        await _phone.AutoSync.TickAsync();
        Assert.Equal((SyncProblem.Failed, "No connection."), (_phone.AutoSync.Problem, _phone.AutoSync.Message));
        Assert.Equal(["Duplex"], await GearNamesAsync(_phone));

        _drive.FailWrites = false;
        var readsAfterFailing = _drive.Reads;
        Wait(AutoSync.RetryDelay.TotalSeconds - 1);
        await _phone.AutoSync.TickAsync();
        Assert.Equal(readsAfterFailing, _drive.Reads);

        Wait(1);
        await _phone.AutoSync.TickAsync();
        Assert.Equal(SyncProblem.None, _phone.AutoSync.Problem);
        Assert.Null(_phone.AutoSync.Message);
        Assert.Contains("Duplex", _drive.Text);
    }

    [Fact]
    public async Task Sync_now_reports_a_failure_instead_of_throwing()
    {
        await _phone.Sync.ConnectAsync();
        _drive.FailWrites = true;
        await AddGearAsync(_phone, "Duplex");

        var summary = await _phone.AutoSync.SyncNowAsync();

        Assert.Null(summary);
        Assert.Equal(SyncProblem.Failed, _phone.AutoSync.Problem);
    }

    [Fact]
    public async Task Settling_a_sync_conflict_sets_off_a_sync()
    {
        await OpenConnectedPhoneAsync();
        await _laptop.Sync.ConnectAsync();
        var tent = await AddGearAsync(_phone, "Duplex");
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        Wait(60);
        await _phone.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Duplex", "Tent", 950m));
        Wait(60);
        await _laptop.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Duplex", "Tent", 900m));
        await _laptop.Sync.SyncNowAsync();
        await _phone.Sync.SyncNowAsync();
        var conflict = Assert.Single(await _phone.Sync.ListConflictsAsync());
        await _phone.AutoSync.TickAsync(); // runs the sync the edits above were waiting for, so none is due
        Wait(60);
        await _phone.AutoSync.TickAsync();
        var readsBefore = _drive.Reads;

        await _phone.Sync.SettleAsync(conflict.Id, SyncConflictChoice.ThisDevice);
        Wait(AutoSync.ChangeDelay.TotalSeconds);
        await _phone.AutoSync.TickAsync();

        Assert.Equal(readsBefore + 1, _drive.Reads);
    }

    [Fact]
    public async Task Disconnecting_clears_a_waiting_sign_in_prompt_and_stops_syncing()
    {
        await _phone.Sync.ConnectAsync();
        _drive.SignedIn = false;
        await _phone.AutoSync.SyncNowAsync();

        await _phone.AutoSync.DisconnectAsync();
        _drive.SignedIn = true;
        _phone.AutoSync.AppOpened();
        await _phone.AutoSync.TickAsync();

        Assert.Equal(SyncProblem.None, _phone.AutoSync.Problem);
        Assert.Equal(0, _drive.Reads);
    }

    [Fact]
    public async Task Connecting_syncs_straight_away()
    {
        await AddGearAsync(_phone, "Duplex");

        await _phone.AutoSync.ConnectAsync();

        Assert.Contains("Duplex", _drive.Text);
    }

    /// <summary>Counts the phone's ChangesArrived signals.</summary>
    private Func<int> CountArrivals()
    {
        var arrivals = 0;
        _phone.AutoSync.ChangesArrived += () => arrivals++;
        return () => arrivals;
    }

    [Fact]
    public async Task Pages_hear_when_a_sync_brings_in_a_record_from_another_device()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        await AddGearAsync(_laptop, "Stove");
        await _laptop.Sync.SyncNowAsync();
        var arrivals = CountArrivals();

        await _phone.AutoSync.SyncNowAsync();

        Assert.Equal(1, arrivals());
    }

    [Fact]
    public async Task Pages_hear_when_a_sync_brings_in_a_deletion()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var stove = await AddGearAsync(_laptop, "Stove");
        await _laptop.Sync.SyncNowAsync();
        await _phone.AutoSync.SyncNowAsync();
        Wait(60);
        await _laptop.Gear.DeleteGearItemAsync(stove.Id);
        await _laptop.Sync.SyncNowAsync();
        var arrivals = CountArrivals();

        await _phone.AutoSync.SyncNowAsync();

        Assert.Equal(1, arrivals());
    }

    [Fact]
    public async Task Pages_hear_when_a_sync_finds_a_new_sync_conflict()
    {
        await _phone.Sync.ConnectAsync();
        await _laptop.Sync.ConnectAsync();
        var tent = await AddGearAsync(_phone, "Duplex");
        await _phone.Sync.SyncNowAsync();
        await _laptop.Sync.SyncNowAsync();
        Wait(60);
        await _laptop.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Duplex", "Tent", 900m));
        await _laptop.Sync.SyncNowAsync();
        Wait(60);
        await _phone.Gear.UpdateGearItemAsync(tent.Id, new GearItemDetails("Duplex", "Tent", 950m));
        var arrivals = CountArrivals();

        await _phone.AutoSync.SyncNowAsync();
        Assert.Equal(1, (await _phone.Sync.GetStatusAsync()).PendingConflicts);
        Assert.Equal(1, arrivals());

        await _phone.AutoSync.SyncNowAsync();
        Assert.Equal(1, arrivals());
    }

    [Fact]
    public async Task Pages_hear_nothing_when_a_sync_brings_in_nothing_or_only_sends_this_devices_changes()
    {
        await _phone.Sync.ConnectAsync();
        var arrivals = CountArrivals();

        await _phone.AutoSync.SyncNowAsync();
        await AddGearAsync(_phone, "Duplex");
        await _phone.AutoSync.SyncNowAsync();

        Assert.Equal(0, arrivals());
    }

    [Fact]
    public async Task Listeners_hear_when_a_sync_starts_and_ends()
    {
        await _phone.Sync.ConnectAsync();
        var heard = new List<bool>();
        _phone.AutoSync.Changed += () => heard.Add(_phone.AutoSync.Syncing);

        await _phone.AutoSync.SyncNowAsync();

        Assert.Equal([true, false], heard);
    }
}
