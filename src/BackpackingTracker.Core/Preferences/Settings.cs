using BackpackingTracker.Core.Units;

namespace BackpackingTracker.Core.Preferences;

/// <summary>The backpacker's preferences. There is one Settings record, shared across devices by sync.</summary>
public sealed record Settings : StoredRecord
{
    public UnitSystem UnitSystem { get; init; } = UnitSystem.Imperial;

    public ColourPalette Palette { get; init; } = ColourPalette.Camping;

    public Appearance Appearance { get; init; } = Appearance.MatchDevice;

    public TextSize TextSize { get; init; } = TextSize.Default;
}
