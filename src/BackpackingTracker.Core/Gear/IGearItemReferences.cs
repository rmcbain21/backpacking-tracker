namespace BackpackingTracker.Core.Gear;

/// <summary>
/// An area of the app that can refer to Gear Items, such as Gear Lists or Trips. The Gear catalogue
/// asks every registered area before deleting a Gear Item, so nothing is left pointing at nothing.
/// </summary>
public interface IGearItemReferences
{
    /// <summary>What uses the Gear Item, described for the backpacker (for example: Gear List "Summer kit").</summary>
    Task<IReadOnlyList<string>> FindUsesAsync(string gearItemId);
}
