using BackpackingTracker.Core.Ports;

namespace BackpackingTracker.Web.Adapters;

public sealed class SystemClock : IClock
{
    public DateTimeOffset UtcNow => DateTimeOffset.UtcNow;
}
