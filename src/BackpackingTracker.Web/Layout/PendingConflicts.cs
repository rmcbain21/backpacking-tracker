namespace BackpackingTracker.Web.Layout;

/// <summary>
/// How many Sync Conflicts are waiting, shared so the badge on the top bar's Settings icon updates as soon as a
/// sync finds one or the backpacker settles one: those pages set it here, and MainLayout shows it.
/// </summary>
public sealed class PendingConflicts
{
    public int Count { get; private set; }

    public event Action? Changed;

    public void Set(int count)
    {
        if (count != Count)
        {
            Count = count;
            Changed?.Invoke();
        }
    }
}
