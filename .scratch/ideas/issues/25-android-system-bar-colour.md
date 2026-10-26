# 25: Bug: Android system bar showing the default Blazor colour

**Status:** needs-info

**Report:** On Android, the bar around the phone's buttons showed the default Blazor/MudBlazor purple instead of matching the app. It may be fixed now: it has been showing green lately.

## Comments

**Explanation (2026-10-26):** Fixed by ticket 30 (commit 1b1c0d3, "Colour palettes and dark mode").

- Chrome colours an installed app's system bar from its **theme colour**. The first version of the app (ticket 01) kept the Blazor template's value, `#594ae2` (purple), in both `manifest.webmanifest` and the `theme-color` tag in `index.html`.
- Ticket 30 changed both to the Camping palette's top-bar green (`#1b5e20`), and added code in `MainLayout.razor` that updates the `theme-color` tag whenever the palette or light/dark mode changes, so the bar follows whichever palette is chosen.
- Two things can still look off: the manifest's colour is fixed, so while the app is starting (before Blazor loads) the bar is always green; and Android caches an installed app's manifest, so an old install can keep the purple until Chrome refreshes it (reinstalling forces it).

To close: on the phone, choose a palette that isn't green (e.g. Alpine Lake) and check the bar turns blue. If it does, close this.
