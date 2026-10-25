# 33: Pages refresh after a sync brings in changes

**What to build:** When an automatic sync (or Sync now) brings in changes from my other devices, the page I'm looking at shows them straight away, without my having to leave the page and come back. For example, I add a Gear Item on my laptop, and the Gear page already open on my phone shows it a few seconds after the phone syncs.

**Blocked by:** 26 (Automatic sync and error handling)

**Status:** ready-for-agent

- [x] The core says when a sync brought in changes, separately from a sync merely starting or ending, so pages don't reload after every sync that found nothing new.
- [x] Every page that shows records reloads its data when changes arrive: Gear, Retired gear, Categories, Lists, a Gear List, Trips, a Trip, Food (Food Items and Meals), Retired food, and Sync Conflicts. Settings already follows sync (ticket 26).
- [x] A page keeps where I am: the same tab on a Trip, the same search text and filters on Gear and Food, and the scroll position.
- [x] An open dialog isn't closed or reset by a sync; what I've typed stays. The page underneath refreshes, and the dialog saves against the latest record as it does now.
- [x] If the record a page shows was deleted on another device (a Trip or Gear List), the page shows its existing "no longer exists" state instead of failing.
- [x] A page that's no longer shown stops listening (no reloads after navigating away).
- [x] Core tests cover that the signal is raised when a sync brings in records (including deletions) and not when it brings in nothing, nor when only this device's changes were sent.

## Comments

- From ticket 26: `AutoSync.Changed` fires when a sync starts or ends and when the connection changes, which is too often to reload on. `CloudSync.SyncNowAsync` already returns a `ChangeSummary` of what changed on this device; a `ChangesArrived` event on `AutoSync`, raised after a sync whose summary isn't `NothingChanged`, is one way to get the signal. Note that a tombstone the device never had (`MergeChange.Unseen`) isn't counted in the summary, which is right: nothing visible changed.
- Settling a Sync Conflict on another device arrives as an ordinary change, so it's covered by the same signal.
- Pages load in `OnInitializedAsync` (most through a `LoadAsync` method, which a refresh can call again). `TripPage` and `GearListPage` already render a "no longer exists" state when their record is gone. A small shared helper (subscribe on init, `InvokeAsync` the reload, unsubscribe on dispose) would keep the pages from each repeating the same wiring.
- Implemented: `AutoSync.ChangesArrived`, raised after a sync whose `ChangeSummary` isn't `NothingChanged`. `ChangeSummary` gained `ConflictsChanged` (the waiting Sync Conflicts differ from before the sync), because a new conflict changes no records on this device (each side keeps its version) but the Sync Conflicts page must still show it. Settings shows "Synced." when only conflicts changed.
- Web: `Components/RefreshAfterSync.razor`, put on each page under its `<PageTitle>` with the page's own reload (`OnInitializedAsync`, `OnParametersSetAsync` or `LoadAsync`, which re-read with the page's current tab, search and filters). It runs the reload through `InvokeAsync`, so the page redraws in place (scroll kept), and unsubscribes on dispose. Dialogs hold their own copy of what they edit, so a refresh underneath doesn't touch them.
- Checked end to end with headless Edge: two browser contexts as two devices, with Google's sign-in script and the Drive REST calls answered by a fake in the test script (so the real `googleDrive.js` ran). The laptop added two Gear Items; its automatic sync wrote them; the phone, with its Gear page open and "st" in the search, synced on coming back online and the page redrew with the stove only, search kept.
- Worth knowing: an app left open in the foreground doesn't pull other devices' changes on its own; it syncs on opening, coming back to the front, coming back online, and a few seconds after its own changes (ticket 26). A periodic pull (say every few minutes while open and signed in) would be a small addition to `AutoSync` if wanted.
