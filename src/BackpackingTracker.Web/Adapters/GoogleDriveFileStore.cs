using BackpackingTracker.Core.Ports;
using Microsoft.JSInterop;

namespace BackpackingTracker.Web.Adapters;

/// <summary>
/// Keeps the sync file in the backpacker's Google Drive app-data folder (wwwroot/js/googleDrive.js), signing in
/// with Google Identity Services and only the drive.appdata scope. The access token is held in memory only.
/// </summary>
public sealed class GoogleDriveFileStore : ICloudFileStore, IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> _module;
    private readonly string _clientId;

    public GoogleDriveFileStore(IJSRuntime js, IConfiguration configuration)
    {
        _module = new Lazy<Task<IJSObjectReference>>(
            () => js.InvokeAsync<IJSObjectReference>("import", "./js/googleDrive.js").AsTask());
        _clientId = configuration["GoogleDrive:ClientId"]
            ?? throw new InvalidOperationException("GoogleDrive:ClientId is missing from wwwroot/appsettings.json.");
    }

    /// <summary>Loads Google's sign-in script, so <see cref="SignInAsync"/> can open its pop-up straight from a tap.</summary>
    public Task PreloadAsync() => CallAsync("preload");

    public Task<bool> IsSignedInAsync() => CallAsync<bool>("isSignedIn");

    /// <summary>Opens Google's sign-in pop-up. Call it straight from a tap, or the browser may block the pop-up.</summary>
    public Task SignInAsync() => CallAsync("signIn", _clientId);

    /// <summary>Forgets the access token.</summary>
    public Task SignOutAsync() => CallAsync("signOut");

    public Task<string?> ReadAsync() => CallAsync<string?>("read");

    public Task WriteAsync(string text) => CallAsync("write", text);

    public async Task<DateTimeOffset?> GetLastModifiedAsync()
    {
        var modified = await CallAsync<string?>("lastModified");
        return modified is null ? null : DateTimeOffset.Parse(modified, System.Globalization.CultureInfo.InvariantCulture);
    }

    private async Task CallAsync(string function, params object?[] args) =>
        await CallAsync<object?>(function, args);

    private async Task<T> CallAsync<T>(string function, params object?[] args)
    {
        try
        {
            var module = await _module.Value;
            return await module.InvokeAsync<T>(function, args);
        }
        catch (JSException exception)
        {
            throw new CloudFileStoreException(Describe(exception), exception);
        }
    }

    /// <summary>The words for an error code thrown by googleDrive.js.</summary>
    private static string Describe(JSException exception)
    {
        // The message starts with the code; Blazor may add the JavaScript stack after it.
        var code = exception.Message.Split('\n')[0].Trim();
        return code switch
        {
            "offline" => "Couldn't reach Google Drive. Check your connection and try again.",
            "cancelled" => "Google sign-in was cancelled.",
            "popup_failed_to_open" => "The Google sign-in window was blocked. Allow pop-ups for this site and try again.",
            "scope-denied" or "access_denied" =>
                "Google Drive access wasn't allowed, so the app can't sync. Connect again and allow access to the app's own data.",
            "signin-required" => "Your Google sign-in has expired. Tap Sync now to sign in again.",
            _ when code.StartsWith("http-", StringComparison.Ordinal) =>
                $"Google Drive refused the request (error {code["http-".Length..]}). Try again later.",
            _ => $"Google Drive sync failed ({code}).",
        };
    }

    public async ValueTask DisposeAsync()
    {
        if (_module.IsValueCreated)
        {
            var module = await _module.Value;
            await module.DisposeAsync();
        }
    }
}
