# 25: Sync Conflicts

**What to build:** When the same record was changed on two devices since they last synced, sync doesn't silently overwrite either. It shows a Sync Conflict with both versions side by side, keeps each device's version until I decide, shows a badge with how many are waiting, and lets me settle each one by choosing a version.

**Blocked by:** 24 (Google Drive sync)

**Status:** ready-for-agent

- [x] The merge detects a Sync Conflict when both sides changed a record since base and the contents differ; identical changes are not a conflict.
- [x] While unsettled, the local version stays on the device and the remote version stays in the sync file; the conflict is stored locally and survives reloads.
- [x] A badge shows the number of pending Sync Conflicts.
- [x] A conflict view shows both versions side by side, with the differing fields highlighted, in display units.
- [x] Choosing a version writes it with a new `updatedAt`, clears the conflict, and wins on the next sync on every device.
- [x] Two-device tests cover detection, identical edits, a conflict on a tombstone versus an edit, settling on either device, and propagation after settling.

## Comments

- From ticket 08: List Entries live inside the Gear List record, so every quantity or Carry Mode change rewrites the whole list. Edits to different entries of the same list on two devices before syncing become one Sync Conflict for the whole list. Showing the two versions side by side should make the entry-level differences easy to see.
- From ticket 24: the place to detect a Sync Conflict is the "both changed" branch of `RecordMerge.RemoteWins` (both sides' `UpdatedAt` differ from the base), which currently lets the later edit win. Compare serialized contents without `updatedAt` there (see the ticket 05 note) before calling it a conflict. A conflicted record should keep the local version on the device and the remote version in the file, and should keep its old base so it stays detected until settled.
- Decided with the owner (2026-10-25): with no base (a device's first sync of a record) differing contents are a Sync Conflict too, except that a side never edited (`UpdatedAt` = Unix epoch) counts as unchanged. To keep that rare, Disconnect now keeps the sync history (bases, last synced, pending conflicts) and only clears `Connected`. Trips and Gear Lists show only the differing lines plus "N other details are the same"; other records show every line. The badge is on the top bar's Settings icon, with a "N Sync Conflicts to settle" button in the sync section.
- Implemented: `RecordMerge.WithBase` returns `Conflicts`. Both sides changed since base (or no base, as above) and contents differ → conflict; contents compared as JSON without `updatedAt` (`RecordTable.SameContents`); two tombstones always agree. A conflicted record keeps its local version on the device, the remote version in the file, and its old base (if any), so each sync finds it again. Pending conflicts live in `SyncState.Conflicts` (`PendingConflict`: table, record id, the other device's version as JSON), replaced by each successful sync, so a conflict that stops being one (the sides come to agree) clears itself.
- `CloudSync.ListConflictsAsync` builds `SyncConflictView`s (`ConflictDescriber`: per-type lines in display units, names of referenced Gear Items, Food Items and Meals, a "Deleted" line when either side is a tombstone). `SettleAsync(id, ThisDevice | OtherDevice)` saves the chosen version with a new `UpdatedAt` and sets the base to the other device's `UpdatedAt`, in one `SaveAllAsync`, so the next sync sends it and every device takes it. `SyncStatus.PendingConflicts` feeds the badge (`PendingConflicts` service in the web layout).
- Web: `/sync/conflicts` page. Checked with headless Edge at phone width, light and dark, using a conflict planted in IndexedDB (a real two-device conflict needs Google sign-in): badge count and screen-reader label, the side-by-side lines with differing ones bold and tinted, "Keep other device's" takes that version and clears the badge.
