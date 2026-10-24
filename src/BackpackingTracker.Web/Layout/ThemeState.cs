using BackpackingTracker.Core.Preferences;

namespace BackpackingTracker.Web.Layout;

/// <summary>
/// The palette and appearance in use, shared so that a change made in Settings shows straight away:
/// the Settings page saves the choice and sets it here, and MainLayout applies it.
/// </summary>
public sealed class ThemeState
{
    public ColourPalette Palette { get; private set; }

    public Appearance Appearance { get; private set; }

    public event Action? Changed;

    public void Set(ColourPalette palette, Appearance appearance)
    {
        Palette = palette;
        Appearance = appearance;
        Changed?.Invoke();
    }
}
