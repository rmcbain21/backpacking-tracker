using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Tests.Fakes;

/// <summary>A clock that only moves when a test moves it.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }

    /// <summary>The date of <see cref="UtcNow"/>, as if the backpacker were on UTC.</summary>
    public DateOnly Today => DateOnly.FromDateTime(UtcNow.UtcDateTime);
}
