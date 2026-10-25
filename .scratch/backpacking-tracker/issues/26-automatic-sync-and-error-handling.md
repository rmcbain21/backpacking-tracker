# 26: Automatic sync and error handling

**What to build:** Sync runs by itself when the app opens and a few seconds after I make changes, whenever there's a connection, so I don't have to think about it. When it can't sync (no signal, expired sign-in), I see a clear message and it tries again later.

**Blocked by:** 24 (Google Drive sync)

**Status:** ready-for-agent

- [x] When connected and online, sync runs on app open and after local changes, debounced by a few seconds.
- [x] No sync is attempted while offline; it runs when the device comes back online.
- [x] Overlapping sync runs are prevented.
- [x] An expired or missing sign-in shows a "sign in to sync" prompt instead of failing silently.
- [x] Network and Drive errors show a clear message, keep local data, and retry on the next trigger.
- [x] Core tests cover debouncing, skip-when-offline, no overlap and error states, using the fakes and controllable clock.

## Comments

- From ticket 24: Google's access token is held in memory only and lasts about an hour, and getting a new one opens a pop-up, which browsers only allow straight after a tap. So a sync on app open or after changes will usually find no token: it can't sign in by itself and should show the "sign in to sync" prompt (a tap on it can then call `GoogleDriveFileStore.SignInAsync`, and the pop-up closes by itself for an account that already allowed access). Sign-in and network failures arrive as `CloudFileStoreException` (from the adapter) and refusals such as an unreadable sync file as `RuleViolationException`; both messages are ready to show.
- Decided with the owner (2026-10-25): when sync needs Google sign-in, a slim banner under the top bar says "Sync is paused" with Sign in and ×; × hides it until the problem clears or the app is opened again.
- Implemented: core `AutoSync` (`AppCore.AutoSync`), driven by `TickAsync` (the web app ticks every second) and the clock, so it's tested without timers. Due now on `AppOpened` (also when the PWA comes back to the front) and on coming back online; due `ChangeDelay` (5 s) after the last local change; due `RetryDelay` (1 min) after a failure. Skips ticks while offline, while not connected, and while `SignInNeeded` (until a sync the backpacker starts succeeds). One sync at a time (a `SemaphoreSlim`; a tick that finds one running leaves its sync due). `SyncNowAsync`, `ConnectAsync` and `DisconnectAsync` go through it, never throw for sync failures, and report `Problem` (`None`, `Offline`, `SignInNeeded`, `Failed`) and `Message`; `Changed` fires when a sync starts or ends and when the connection changes.
- Local changes are spotted by `ChangeWatchingRecordStore`, the store every area saves through; `CloudSync` keeps the plain store so what a sync brings in doesn't set off another sync. Settling a Sync Conflict reports a change too. `CloudFileStoreException.SignInNeeded` marks a missing or expired Google token.
- Web: `AutoSyncRunner` (tick loop, `js/network.js` for online/offline and visibility, `SignInAndSyncAsync` for taps), started by MainLayout after first render. Top bar: a sync icon when connected (synced, turning while syncing, offline, problem), and the banner for `SignInNeeded` and `Failed` (Try now). The layout re-reads Settings and the pending Sync Conflict count after each sync; the Settings page shows the problem message in the sync section and live last-synced time. Checked with headless Edge at phone width: on open with no token the banner and icon show; offline swaps the icon to offline and hides the banner; back online brings it back; × hides it; Sign in opens Google's pop-up and closing it shows "Google sign-in was cancelled".
- Not done: pages showing records (Gear, Trips, ...) don't redraw when an automatic sync brings in changes; they show them next time they load. A follow-up could have pages listen to `AutoSync.Changed`.
