namespace BackpackingTracker.Core.Lists;

/// <summary>
/// A group on one Gear List (or Packing List) that its List Entries are arranged under, used to break
/// down pack weight. Each list has its own; a list's order of Categories is the order they're kept in.
/// </summary>
public sealed record Category
{
    /// <summary>
    /// Stands in for a Category id that isn't on the list (the API never does this, but a hand-edited or imported
    /// file could), so entries pointing at it are still shown. Never stored.
    /// </summary>
    public static Category None { get; } = new() { Id = "", Name = "No Category" };

    public required string Id { get; init; }

    public required string Name { get; init; }
}
