using BackpackingTracker.Core;
using BackpackingTracker.Core.Sync;
using BackpackingTracker.Web.Adapters;
using Microsoft.JSInterop;

namespace BackpackingTracker.Web.Layout;

/// <summary>
/// Drives the core's automatic sync in the browser: ticks it every second, tells it when the device goes
/// online or offline and when the app is opened or comes back to the front (wwwroot/js/network.js), and
/// signs in to Google before a sync the backpacker asked for.
/// </summary>
public sealed class AutoSyncRunner : IAsyncDisposable
{
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(1);

    private readonly AppCore _app;
    private readonly GoogleDriveFileStore _drive;
    private readonly IJSRuntime _js;
    private readonly CancellationTokenSource _stop = new();
    private DotNetObjectReference<AutoSyncRunner>? _self;
    private IJSObjectReference? _module;

    public AutoSyncRunner(AppCore app, GoogleDriveFileStore drive, IJSRuntime js)
    {
        _app = app;
        _drive = drive;
        _js = js;
    }

    /// <summary>Starts automatic sync, once, when the app has rendered and JavaScript is available.</summary>
    public async Task StartAsync()
    {
        if (_self is not null)
        {
            return;
        }

        _self = DotNetObjectReference.Create(this);
        _module = await _js.InvokeAsync<IJSObjectReference>("import", "./js/network.js");
        _app.AutoSync.SetOnline(await _module.InvokeAsync<bool>("watch", _self));
        _app.AutoSync.AppOpened();
        await _drive.PreloadAsync();
        _ = TickLoopAsync();
    }

    /// <summary>
    /// Signs in to Google if there's no access token yet (call it straight from a tap: it may open Google's
    /// pop-up), then syncs. Returns what changed, or null if the sync failed (see <see cref="AutoSync.Problem"/>).
    /// Sign-in failures, such as a closed pop-up, throw <see cref="Core.Ports.CloudFileStoreException"/>.
    /// </summary>
    public async Task<ChangeSummary?> SignInAndSyncAsync()
    {
        if (!await _drive.IsSignedInAsync())
        {
            await _drive.SignInAsync();
        }

        return await _app.AutoSync.SyncNowAsync();
    }

    [JSInvokable]
    public void SetOnline(bool online) => _app.AutoSync.SetOnline(online);

    [JSInvokable]
    public void AppResumed() => _app.AutoSync.AppOpened();

    private async Task TickLoopAsync()
    {
        using var timer = new PeriodicTimer(TickInterval);
        while (await timer.WaitForNextTickAsync(_stop.Token))
        {
            try
            {
                await _app.AutoSync.TickAsync();
            }
            catch (Exception exception)
            {
                // Keep ticking: a failed tick (such as storage being briefly unavailable) is tried again next second.
                Console.Error.WriteLine($"Automatic sync tick failed: {exception}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await _stop.CancelAsync();
        if (_module is not null)
        {
            await _module.DisposeAsync();
        }

        _self?.Dispose();
    }
}
