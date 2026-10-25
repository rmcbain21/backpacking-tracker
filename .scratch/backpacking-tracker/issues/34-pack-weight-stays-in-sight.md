# 34: Starting Pack Weight stays in sight while scrolling a Gear List

**What to build:** On my phone, when I scroll down a long Gear List to change quantities or carry modes, I can still see the Starting Pack Weight without scrolling back up. For example, I switch my jacket from Base to Worn halfway down the list and see the Starting Pack Weight drop straight away.

**Status:** ready-for-agent

- [x] Once the Starting Pack Weight row in the totals scrolls up under the top bar, the totals shrink to a slim bar pinned under the top bar: Starting Pack Weight, Base Weight and the Starting Pack Weight figure. Scrolling back up to the totals hides it again.
- [x] The bar follows changes made on the list (quantity, carry mode, add, remove) and after a sync.
- [x] Tapping the bar scrolls back up to the full totals.
- [x] The figures stay on one line on a narrow phone; the label gives way ("Starting Pack W…") before the numbers do.
- [x] Hidden, the bar can't be tapped or reached by keyboard or screen reader. Motion is skipped for "reduce motion".
- [x] On a tablet or desktop the bar sits beside the navigation drawer, not under it.

## Comments

- Prototyped three ways on the Gear List page: (A) the totals shrink to a bar under the top bar, (B) a bar above the bottom navigation that opens to the full figures, (C) a Weight button that folds the bottom navigation away to show the weight. Chose **A**. The variants are kept for reference on branch `prototype/sticky-pack-weight` (commit 8db83da); never merge it.
- Implemented as `ShrinksToTop` on `Components/PackTotals.razor`, with `wwwroot/js/packTotals.js` watching the Starting Pack Weight row. Only the Gear List page turns it on; a Trip's totals could too with the same one attribute.
- Checked in headless Edge at 360px wide with eight Gear Items on a list: hidden at the top, shown after scrolling, hidden again after tapping it (back at the top), and gone after leaving the page.
- Added to a Trip's Packing List tab too (`ShrinksToTop` on its `PackTotals`). The other tabs have no totals, so no bar; MudTabs only renders the tab showing, so the bar stops on leaving the tab and starts again on coming back. Checked in headless Edge at 360px: shown after scrolling, none on Checklist, working again back on Packing List, and tapping it goes back up.
- Not handled: when the "Sync is paused" banner is showing, the bar covers it while it's shown.
