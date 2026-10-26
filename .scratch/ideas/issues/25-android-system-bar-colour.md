# 25: Bug: Android system bar showing the default Blazor colour

**Status:** needs-info (fix made; check on the phone after Chrome refreshes the install)

**Report:** On Android, the bar around the phone's buttons showed the default Blazor/MudBlazor purple instead of matching the app. It may be fixed now: it has been showing green lately.

## Comments

**Explanation (2026-10-26):** Fixed by ticket 30 (commit 1b1c0d3, "Colour palettes and dark mode").

- Chrome colours an installed app's system bar from its **theme colour**. The first version of the app (ticket 01) kept the Blazor template's value, `#594ae2` (purple), in both `manifest.webmanifest` and the `theme-color` tag in `index.html`.
- Ticket 30 changed both to the Camping palette's top-bar green (`#1b5e20`), and added code in `MainLayout.razor` that updates the `theme-color` tag whenever the palette or light/dark mode changes, so the bar follows whichever palette is chosen.
- Two things can still look off: the manifest's colour is fixed, so while the app is starting (before Blazor loads) the bar is always green; and Android caches an installed app's manifest, so an old install can keep the purple until Chrome refreshes it (reinstalling forces it).

To close: on the phone, choose a palette that isn't green (e.g. Alpine Lake) and check the bar turns blue. If it does, close this.

**Check result (2026-10-26):** With a non-green palette, the bar stays green, so it isn't fully fixed. Green is the fixed `theme_color` in `manifest.webmanifest`, so the bar is being coloured from the manifest, not from the live `theme-color` tag that `MainLayout.razor` updates. Next step: find out whether the top status bar (clock, battery) changes colour while the bottom button bar stays green. If the top bar changes, Android/Chrome is taking the bottom bar's colour from the manifest when the app is installed, and the app can't change it while running; the fix would be a manifest colour that suits every palette. If the top bar stays green too, the live update isn't working and needs debugging.

**Answer and fix (2026-10-26):** The top status bar follows the palette, but the bottom button bar stays green. So Chrome colours the bottom bar from the manifest's `theme_color` when the app is installed, and the running app can't change it. Changed the manifest's `theme_color` from Camping green to a neutral dark charcoal, `#222628`, which suits every palette's dark top bar in light and dark mode. The `theme-color` tag in `index.html` stays green; it only shows on the top bar while the app starts, until Blazor sets the palette colour. Chrome refreshes an installed app's manifest on its own schedule (it can take a day or more); uninstalling and reinstalling picks up the change straight away.
