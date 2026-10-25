using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Sync;

/// <summary>Why automatic sync isn't running right now, if anything.</summary>
public enum SyncProblem
{
    None,

    /// <summary>The device has no connection; sync runs when it comes back.</summary>
    Offline,

    /// <summary>Google needs the backpacker to sign in again, which takes their tap; sync waits until then.</summary>
    SignInNeeded,

    /// <summary>The last sync failed (see <see cref="AutoSync.Message"/>); it tries again a minute later.</summary>
    Failed,
}

/// <summary>
/// Runs sync by itself when it's connected: when the app opens, a few seconds after local changes (so a burst
/// of edits syncs once), when the device comes back online, and a minute after a failed sync. Never two syncs
/// at once. The app calls <see cref="TickAsync"/> every second or so; a sync runs on the tick once one is due.
/// </summary>
public sealed class AutoSync
{
    /// <summary>How long after the last local change a sync runs.</summary>
    public static readonly TimeSpan ChangeDelay = TimeSpan.FromSeconds(5);

    /// <summary>How long after a failed sync the next try is.</summary>
    public static readonly TimeSpan RetryDelay = TimeSpan.FromMinutes(1);

    private readonly CloudSync _sync;
    private readonly IClock _clock;
    private readonly SemaphoreSlim _running = new(1, 1);
    private DateTimeOffset? _dueAt;
    private bool _online = true;

    public AutoSync(CloudSync sync, IClock clock)
    {
        _sync = sync;
        _clock = clock;
    }

    public bool Syncing { get; private set; }

    public SyncProblem Problem { get; private set; }

    /// <summary>Words for the backpacker about <see cref="Problem"/>; null when there's none.</summary>
    public string? Message { get; private set; }

    /// <summary>Raised when a sync starts or ends, or the connection comes or goes.</summary>
    public event Action? Changed;

    /// <summary>
    /// Raised after a sync that changed records on this device (added, updated or deleted something from
    /// another device), so pages showing records can reload. Not raised when a sync only sent this device's changes.
    /// </summary>
    public event Action? ChangesArrived;

    /// <summary>The app has opened, or come back to the front: sync now.</summary>
    public void AppOpened() => _dueAt = _clock.UtcNow;

    /// <summary>Connects this device (after the backpacker has signed in) and syncs straight away.</summary>
    public async Task<ChangeSummary?> ConnectAsync()
    {
        await _sync.ConnectAsync();
        return await SyncNowAsync();
    }

    /// <summary>Disconnects this device: automatic sync stops, and any problem it was showing goes with it.</summary>
    public async Task DisconnectAsync()
    {
        await _sync.DisconnectAsync();
        _dueAt = null;
        SetProblem(_online ? SyncProblem.None : SyncProblem.Offline, _online ? null : Message);
        Changed?.Invoke();
    }

    public void SetOnline(bool online)
    {
        if (online == _online)
        {
            return;
        }

        _online = online;
        if (online)
        {
            if (Problem == SyncProblem.Offline)
            {
                SetProblem(SyncProblem.None, null);
            }

            _dueAt = _clock.UtcNow;
        }
        else
        {
            SetProblem(SyncProblem.Offline, "You're offline. Changes will sync when you're back online.");
        }

        Changed?.Invoke();
    }

    /// <summary>Something was saved on this device: sync once things have been quiet for <see cref="ChangeDelay"/>.</summary>
    internal void LocalChanged() => _dueAt = _clock.UtcNow + ChangeDelay;

    /// <summary>Runs a sync if one is due, the device is online and connected, and none is running.</summary>
    public async Task TickAsync()
    {
        if (_dueAt is not { } due || _clock.UtcNow < due || !_online || Problem == SyncProblem.SignInNeeded)
        {
            return;
        }

        // A sync is already running: leave this one due for the next tick.
        if (!_running.Wait(0))
        {
            return;
        }

        try
        {
            if ((await _sync.GetStatusAsync()).Connected)
            {
                await RunAsync();
            }
            else
            {
                _dueAt = null;
            }
        }
        finally
        {
            _running.Release();
        }
    }

    /// <summary>
    /// Syncs now, after any sync that's running, as when the backpacker taps Sync now (signing in first if they
    /// had to). Returns what changed on this device, or null if it failed: then <see cref="Problem"/> says why.
    /// </summary>
    public async Task<ChangeSummary?> SyncNowAsync()
    {
        await _running.WaitAsync();
        try
        {
            return await RunAsync();
        }
        finally
        {
            _running.Release();
        }
    }

    private async Task<ChangeSummary?> RunAsync()
    {
        // Cleared first, so a change made while this sync runs is still due afterwards.
        _dueAt = null;
        Syncing = true;
        Changed?.Invoke();
        try
        {
            var summary = await _sync.SyncNowAsync();
            SetProblem(SyncProblem.None, null);
            if (!summary.NothingChanged)
            {
                ChangesArrived?.Invoke();
            }

            return summary;
        }
        catch (CloudFileStoreException signIn) when (signIn.SignInNeeded)
        {
            SetProblem(SyncProblem.SignInNeeded, signIn.Message);
        }
        catch (Exception exception)
        {
            // Sync runs unattended, so any failure is reported rather than thrown, and tried again later.
            SetProblem(SyncProblem.Failed, exception is CloudFileStoreException or RuleViolationException
                ? exception.Message
                : $"Sync failed ({exception.Message}). It will try again in a minute.");
            var retryAt = _clock.UtcNow + RetryDelay;
            _dueAt = _dueAt is { } sooner && sooner < retryAt ? sooner : retryAt;
        }
        finally
        {
            Syncing = false;
            Changed?.Invoke();
        }

        return null;
    }

    private void SetProblem(SyncProblem problem, string? message)
    {
        Problem = problem;
        Message = message;
    }
}
