namespace BackpackingTracker.Core.Gear;

/// <summary>
/// When two Gear Items are the same gear: same Gear Type and Name, ignoring case and surrounding spaces. CSV
/// import reuses a Gear Item that matches a row, and Gear Items that match each other are offered for a Merge.
/// </summary>
internal static class GearMatch
{
    public static bool Matches(GearItem item, string gearType, string name) =>
        string.Equals(item.GearType.Trim(), gearType.Trim(), StringComparison.OrdinalIgnoreCase)
        && string.Equals(item.Name.Trim(), name.Trim(), StringComparison.OrdinalIgnoreCase);

    /// <summary>The same for every Gear Item that matches this one.</summary>
    public static string Key(GearItem item) =>
        $"{item.GearType.Trim().ToUpperInvariant()}\n{item.Name.Trim().ToUpperInvariant()}";
}
