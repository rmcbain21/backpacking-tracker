using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Tests.Fakes;

/// <summary>One sync file shared by every device in a test, like a Google Drive app-data folder.</summary>
public sealed class InMemoryCloudFileStore : ICloudFileStore
{
    private int _writes;

    /// <summary>The file's contents; null until a device first writes it.</summary>
    public string? Text { get; set; }

    public DateTimeOffset? LastModified { get; private set; }

    /// <summary>How many times the file has been read: how many syncs got going.</summary>
    public int Reads { get; private set; }

    /// <summary>When false, every call fails asking for sign-in, as when Google's access token has expired.</summary>
    public bool SignedIn { get; set; } = true;

    /// <summary>How many calls were refused for want of a sign-in: how many syncs tried while signed out.</summary>
    public int SignInRefusals { get; private set; }

    /// <summary>When true, writing fails, as it would with no signal.</summary>
    public bool FailWrites { get; set; }

    /// <summary>Runs once, just after the next read: lets a test act in the middle of a sync.</summary>
    public Func<Task>? AfterNextRead { get; set; }

    public async Task<string?> ReadAsync()
    {
        RequireSignIn();
        Reads++;
        var text = Text;
        if (AfterNextRead is { } afterRead)
        {
            AfterNextRead = null;
            await afterRead();
        }

        return text;
    }

    public Task WriteAsync(string text)
    {
        RequireSignIn();
        if (FailWrites)
        {
            throw new CloudFileStoreException("No connection.");
        }

        Text = text;
        LastModified = DateTimeOffset.UnixEpoch.AddSeconds(++_writes);
        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> GetLastModifiedAsync()
    {
        RequireSignIn();
        return Task.FromResult(LastModified);
    }

    private void RequireSignIn()
    {
        if (!SignedIn)
        {
            SignInRefusals++;
            throw new CloudFileStoreException("Sign in again.", signInNeeded: true);
        }
    }
}
