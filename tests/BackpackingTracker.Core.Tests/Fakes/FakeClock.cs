using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Core.Tests.Fakes;

/// <summary>A clock that only moves when a test moves it.</summary>
public sealed class FakeClock : IClock
{
    public FakeClock(DateTimeOffset now) => UtcNow = now;

    public DateTimeOffset UtcNow { get; set; }
}
