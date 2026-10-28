namespace BackpackingTracker.Core.Gear;

/// <summary>
/// The Categories created on first run. They have fixed ids (rather than a new GUID per device)
/// so that every device creates the same records and sync combines them instead of duplicating them.
/// </summary>
public static class DefaultCategories
{
    public const string MiscId = "00000000-0000-0000-0001-000000000010";

    public static readonly IReadOnlyList<(string Id, string Name)> All =
    [
        ("00000000-0000-0000-0001-000000000001", "Shelter"),
        ("00000000-0000-0000-0001-000000000002", "Sleep"),
        ("00000000-0000-0000-0001-000000000003", "Pack"),
        ("00000000-0000-0000-0001-000000000004", "Cook"),
        ("00000000-0000-0000-0001-000000000005", "Water"),
        ("00000000-0000-0000-0001-000000000006", "Clothing"),
        ("00000000-0000-0000-0001-000000000007", "Electronics"),
        ("00000000-0000-0000-0001-000000000008", "Hygiene"),
        ("00000000-0000-0000-0001-000000000009", "First Aid"),
        (MiscId, "Misc"),
    ];
}
