# 24: Google Drive sync

**What to build:** I can connect my Google Drive once, then tap "Sync now" to back up and sync my data through a hidden file in my Drive's app folder. Changes made on one device show up on my others, deletions stay deleted, and I can see when I last synced. I can disconnect without losing local data.

**Blocked by:** 22 (JSON export and import), 23 (Google OAuth client setup)

**Status:** ready-for-agent

- [x] The core depends on a cloud file store port (read the sync file, write the sync file, get last-modified); tests use an in-memory implementation.
- [x] The Google Drive adapter signs in with Google Identity Services using only the `drive.appdata` scope and stores the sync file in the app-data folder. Access tokens are held in memory only.
- [x] Each device keeps local-only sync bookkeeping: per record, the `updatedAt` at the last successful sync, plus the last successful sync time.
- [x] A sync reads the file (or treats it as empty if missing), merges per record (only one side changed since base takes that side; a record on only one side is taken), saves locally, writes the file, then updates bookkeeping. Same-record changes on both sides fall back to newer-wins until ticket 25.
- [x] A failed write leaves bookkeeping unchanged so the next sync retries.
- [x] The Settings page has Connect, Disconnect, "Sync now" and a "last synced" indicator.
- [x] Tests simulate two devices (two core instances, separate in-memory record stores, one shared in-memory cloud file store) covering propagation of adds, edits and tombstones in both directions, and retry after a failed write.

## Comments

- From ticket 04: the default Categories (and the Settings record, ticket 03) have fixed ids so every device creates the same records. Defaults are created with `updatedAt` = Unix epoch, meaning "never edited": when a record has no base yet, a side whose `updatedAt` is the epoch should be treated as unchanged, so a new device's untouched defaults never conflict with edits made elsewhere.
- From ticket 04: after a merge, a Gear Item can point at a Category another device removed. The Gear page shows such items under "No Category"; consider whether the merge should instead move them somewhere.
- From ticket 04: reordering Categories rewrites `SortOrder` on every Category whose position changed, so reordering on two devices before syncing can produce several Sync Conflicts at once.
- From ticket 05: `GearItem` is a C# record, but its `Tags` list (and later nested lists, such as List Entries in a Gear List) compare by reference in the record's built-in `Equals`. To decide "both changed and the contents are identical", compare serialized contents (excluding `updatedAt`), not `Equals`, or every tagged Gear Item will look like a Sync Conflict.
- From ticket 05: a new Tag takes the spelling of a matching Tag already on the device, but sync can still bring in "Winter" and "winter" from two devices. Suggestions de-duplicate ignoring case, so this is cosmetic.
- From ticket 06: the tombstone test in `RetireAndDeleteTests` reads the store directly because nothing else could see a tombstone before sync. Replace it with a two-device test (delete on one device, sync both, the item is gone on the other) and drop the store read.
- From ticket 07: CSV import matches against this device's gear only. If two devices each import the same sheet before syncing, both create the Gear Items, and sync brings in duplicates (different ids). Worth a hint in the UI ("sync before importing on a second device"), or a duplicate check after merge.
- Implemented: port `ICloudFileStore` (read, write, last-modified; failures throw `CloudFileStoreException` with words for the backpacker). Engine `CloudSync` (`AppCore.Sync`: `GetStatusAsync`, `ConnectAsync`, `DisconnectAsync`, `SyncNowAsync` returning a `ChangeSummary`, renamed from ticket 22's `ImportSummary`). Bookkeeping is a local-only `SyncState` record (table `syncState`, not in `RecordTable.All`, so never in the sync file or an export): connected flag, last synced time, and each record's base `UpdatedAt` keyed `table/id`.
- Merge is `RecordMerge.WithBase`: a side has changed when its `UpdatedAt` differs from the base (not "is later than", so an edit on a device whose clock is behind still wins). Only one side changed → that side; both changed, or no base → later edit wins, tie keeps local. No base + one side at the Unix epoch (untouched defaults) therefore always loses to an edit.
- Lost-update guard: the engine notes the file's last-modified before reading and checks it again before writing; if another device wrote in between, it starts again (3 tries). The file is only written when the merge changed it (or there's no file yet).
- Disconnect forgets all bookkeeping (and the in-memory token) and keeps the records; reconnecting merges like a first sync.
- The tombstone test in `RetireAndDeleteTests` that read the store is replaced by two-device deletion tests in `SyncTests`.
- Web: `GoogleDriveFileStore` + `wwwroot/js/googleDrive.js` (Google Identity Services token pop-up, Drive v3 REST with `spaces=appDataFolder`, file `backpacking-tracker-sync.json`; created with a metadata POST then a media PATCH). Settings → "Google Drive sync": Connect (signs in, then syncs), Sync now (signs in first if the token is missing or expired), Disconnect, last synced, the summary or the error. Checked with headless Edge at phone width: Connect opens Google's sign-in with the right client id, scope and origin, and closing it shows "Google sign-in was cancelled". The owner then signed in and synced for real (2026-10-25).
