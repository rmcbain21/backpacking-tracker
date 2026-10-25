namespace BackpackingTracker.Core.Ports;

/// <summary>
/// The one sync file in the backpacker's cloud drive (Google Drive's app-data folder), shared by all their
/// devices. Another drive, such as OneDrive, can be added by writing another adapter (ADR 0001).
/// Failures, such as no connection or an expired sign-in, throw <see cref="CloudFileStoreException"/>.
/// </summary>
public interface ICloudFileStore
{
    /// <summary>The file's contents, or null if no device has written it yet.</summary>
    Task<string?> ReadAsync();

    /// <summary>Creates or replaces the file.</summary>
    Task WriteAsync(string text);

    /// <summary>When the file was last written, or null if it doesn't exist yet.</summary>
    Task<DateTimeOffset?> GetLastModifiedAsync();
}

/// <summary>
/// The cloud drive couldn't be reached or refused the request. The message is written for the backpacker,
/// so the UI can show it as it is.
/// </summary>
public sealed class CloudFileStoreException : Exception
{
    public CloudFileStoreException(string message, Exception? innerException = null, bool signInNeeded = false)
        : base(message, innerException)
    {
        SignInNeeded = signInNeeded;
    }

    /// <summary>
    /// The backpacker must sign in again (no access token, or it expired). Signing in needs their tap, so
    /// automatic sync waits for it rather than retrying.
    /// </summary>
    public bool SignInNeeded { get; }
}
