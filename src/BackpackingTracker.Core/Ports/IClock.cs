namespace BackpackingTracker.Core.Ports;

public interface IClock
{
    DateTimeOffset UtcNow { get; }

    /// <summary>Today's date where the backpacker is, to compare with Trip dates (which are local calendar days).</summary>
    DateOnly Today { get; }
}
