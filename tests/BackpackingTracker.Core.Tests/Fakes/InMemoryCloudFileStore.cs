using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Tests.Fakes;

/// <summary>One sync file shared by every device in a test, like a Google Drive app-data folder.</summary>
public sealed class InMemoryCloudFileStore : ICloudFileStore
{
    private int _writes;

    /// <summary>The file's contents; null until a device first writes it.</summary>
    public string? Text { get; set; }

    public DateTimeOffset? LastModified { get; private set; }

    /// <summary>When true, writing fails, as it would with no signal.</summary>
    public bool FailWrites { get; set; }

    /// <summary>Runs once, just after the next read: lets a test have another device sync in the middle of a sync.</summary>
    public Func<Task>? AfterNextRead { get; set; }

    public async Task<string?> ReadAsync()
    {
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
        if (FailWrites)
        {
            throw new CloudFileStoreException("No connection.");
        }

        Text = text;
        LastModified = DateTimeOffset.UnixEpoch.AddSeconds(++_writes);
        return Task.CompletedTask;
    }

    public Task<DateTimeOffset?> GetLastModifiedAsync() => Task.FromResult(LastModified);
}
