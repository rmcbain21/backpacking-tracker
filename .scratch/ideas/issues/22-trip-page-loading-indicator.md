# 22: Loading indicator on the Trip page

**Status:** ready-for-agent

**Idea:** The Trip page takes a second to load. Show a loading indicator (spinner or skeleton) so it doesn't look stuck.

**Open questions:**
- First check why it's slow: if the delay can be cut (e.g. by loading less up front), that may beat adding a spinner.
- Spinner, or a skeleton layout of the page?
- Do any other pages need the same?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- First measure why the Trip page is slow (what's loaded and computed before the first render) and cut the delay where that's cheap, e.g. render the page first and load the slower sections after.
- If it still takes a noticeable moment, show a skeleton layout of the page (MudBlazor `MudSkeleton`) instead of a spinner.
- Note in the ticket what was slow and what changed. Check whether other big pages (Gear List, Meal Plan) have the same delay and say so, but only fix the Trip page here.

*Implemented (2026-10-28)*
- Measured on a Release build (as deployed) in headless Edge at phone width, with a realistic export imported: 150 Gear Items, 8 Gear Lists of 45, 60 Food Items, 20 Meals, 15 Trips of 5–14 days with full Meal Plans. Opening a Trip from the Trips list, a 45-entry Packing List:

  | | tap to painted | CPU slowed 4x (phone stand-in) |
  |---|---|---|
  | Before | ~1100 ms | ~6.0 s |
  | After | ~520 ms | ~2.9 s |

- What was slow: not the data. Loading the Trip, Gear Items, Meals and Food Items from IndexedDB took ~100 ms (~0.5 s at 4x). The rest was drawing the rows: every row's Carry Mode `MudSelect` (~500 ms of the 1100) and move `MudMenu` (~130 ms) each set up a popover when drawn. Rows with no controls at all (as on a Completed Trip) paint in ~350 ms.
- Changed (decided with the owner): a row's Carry Mode is a plain "Base ▾" button and Move a plain icon button; tapping either opens one small picker dialog (`Dialogs/PickDialog.razor`) for that entry. Cancel changes nothing. `ListEntryRow` opens the pickers itself, so Gear Lists, which use the same row, get the same speed-up (a 45-entry Gear List opens in ~175 ms).
- The Trip page shows a `MudSkeleton` outline until the Trip has loaded, ~120 ms after the tap at 4x, instead of a blank page.
- Not measured: the Meals tab (only drawn when it's opened). What's left in a row's cost is its four icon buttons; rows with none paint ~170 ms sooner, if it's ever worth more.
