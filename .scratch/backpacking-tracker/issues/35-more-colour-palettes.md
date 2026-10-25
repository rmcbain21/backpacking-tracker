# 35: Five more colour palettes

**What to build:** In Settings I can choose from five more colour palettes beside the original five, each with a light and a dark version. For example, I pick Topo Map and the app turns map-paper buff with contour-brown buttons; on my other phone it changes too after a sync.

**Blocked by:** 30 (Colour palettes and dark mode)

**Status:** ready-for-agent

- [x] Five new palettes, each light and dark: Glacier, Topo Map, Autumn Larch, Trail Blaze, Northern Lights. Camping stays the default.
- [x] They show in Settings as swatches after the original five, and choosing one changes the app straight away.
- [x] Saved by name in the Settings record, like the others, so they sync; Sync Conflicts shows their names.
- [x] Core test: a new palette is remembered after the app is reopened.

## Comments

- Decided with a throwaway prototype: branch `prototype/more-palettes` (commit e742ce6) puts a switcher for the five candidates on the real screens. The backpacker chose to add all five.
- Implemented: five more `ColourPalette` members and their themes in `AppThemes` (copied from the prototype). Settings builds its swatches from `AppThemes.All`, so no page change was needed. Verified with a headless Edge screenshot of Settings showing all ten swatches.
