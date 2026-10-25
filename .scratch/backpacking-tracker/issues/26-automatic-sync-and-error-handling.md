# 26: Automatic sync and error handling

**What to build:** Sync runs by itself when the app opens and a few seconds after I make changes, whenever there's a connection, so I don't have to think about it. When it can't sync (no signal, expired sign-in), I see a clear message and it tries again later.

**Blocked by:** 24 (Google Drive sync)

**Status:** ready-for-agent

- [ ] When connected and online, sync runs on app open and after local changes, debounced by a few seconds.
- [ ] No sync is attempted while offline; it runs when the device comes back online.
- [ ] Overlapping sync runs are prevented.
- [ ] An expired or missing sign-in shows a "sign in to sync" prompt instead of failing silently.
- [ ] Network and Drive errors show a clear message, keep local data, and retry on the next trigger.
- [ ] Core tests cover debouncing, skip-when-offline, no overlap and error states, using the fakes and controllable clock.

## Comments

- From ticket 24: Google's access token is held in memory only and lasts about an hour, and getting a new one opens a pop-up, which browsers only allow straight after a tap. So a sync on app open or after changes will usually find no token: it can't sign in by itself and should show the "sign in to sync" prompt (a tap on it can then call `GoogleDriveFileStore.SignInAsync`, and the pop-up closes by itself for an account that already allowed access). Sign-in and network failures arrive as `CloudFileStoreException` (from the adapter) and refusals such as an unreadable sync file as `RuleViolationException`; both messages are ready to show.
