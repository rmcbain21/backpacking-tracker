# 32: Text size

**What to build:** In Settings I can make the app's text smaller or larger, so screens feel less cramped on a phone. The choice applies straight away and is kept with my other settings, so it syncs between devices.

**Blocked by:** 30 (Colour palettes and dark mode)

**Status:** ready-for-agent

- [x] Four fixed steps rather than a slider: Extra small (81.25%), Small (87.5%), Default (100%, the default), Large (112.5%).
- [x] Settings shows them as a radio group; choosing one changes the app straight away.
- [x] Saved in the Settings record, like the palette and appearance.
- [x] Core tests cover the default, remembering the choice, leaving the colours alone, and refusing an unknown value.

## Comments

- Text first: spacing set in px (row heights, padding classes, bottom navigation) is unchanged. If screens still feel loose, a separate Compact/density setting is the next step.
- Implemented: `Settings.TextSize` (`TextSize`), set with `UserSettings.SetTextSizeAsync`, stored by name. Web: `ThemeState` carries the text size; `MainLayout` sets the page's root font size via `setRootFontSize` (index.html), and since MudBlazor's typography is in rem, all text scales with it. `ThemeState` starts at Default so the app doesn't flash at Extra small before settings load. Verified with headless Edge at phone width (390px): body text 13/14/16/18px for each step, the choice surviving a reload.
