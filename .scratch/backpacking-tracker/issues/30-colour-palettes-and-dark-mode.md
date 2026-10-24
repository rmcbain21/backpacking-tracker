# 30: Colour palettes and dark mode

**What to build:** In Settings I can choose one of five nature-inspired colour palettes and whether the app is light, dark, or matches my device. The choice applies straight away and is kept with my other settings, so it syncs between devices.

**Blocked by:** 03 (Units display and settings)

**Status:** ready-for-agent

- [x] Five palettes, each with a light and a dark version: Camping (the default, from the earlier PackUp app), Alpine Lake, Canyon Sandstone, Moss & Lichen, Twilight Campfire.
- [x] Appearance: Match my device (the default), Light or Dark. Match my device follows the device's dark mode setting, including when it changes while the app is open.
- [x] Settings shows the palettes as swatches (main colour, accent, background) and the three Appearance choices; choosing one changes the app straight away.
- [x] Both choices are saved in the Settings record, like Units.
- [x] Screens take their colours from the theme, so every palette works in light and dark.
- [x] Core tests cover the defaults, remembering each choice, and refusing an unknown value.

## Comments

- Decided with a throwaway prototype: branch `prototype/palettes` (commit d49e685) puts a palette switcher on the real screens. The backpacker liked all five, so all are selectable rather than picking one.
- Implemented: `Settings.Palette` (`ColourPalette`) and `Settings.Appearance`, set with `UserSettings.SetPaletteAsync` / `SetAppearanceAsync`, stored by name. Web: `AppThemes` holds the five MudBlazor themes; `ThemeState` lets the Settings page change the theme live; `MainLayout` follows the device's dark mode for Match my device and sets the phone's status-bar colour to the top bar's. Template colours (loading circle, `code`, manifest and theme-color meta) now use Camping. Verified with a headless Edge test at phone width: device dark mode switching, each Appearance, palette choice surviving a reload.
- For new screens: use theme colours (MudBlazor `Color.*` or `var(--mud-palette-*)`), never fixed colours, and check them in light and dark.
