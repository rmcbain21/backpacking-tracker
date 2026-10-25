# 22: JSON export and import

**What to build:** I can export my whole database as a JSON file I download, and import such a file back, merged with my existing data per record, so I have a backup that doesn't depend on Google. The file format is the sync file format that ticket 24 uses.

**Blocked by:** 19 (Meal Plan on Trip Days)

**Status:** ready-for-agent

- [x] The export is one JSON document with a format version and every record of every type, including tombstones.
- [x] Import merges per record: a record only in the file is added; for a record on both sides, the one with the later `updatedAt` wins; tombstones in the file delete local records with an older `updatedAt`.
- [x] Import rejects files with an unknown format version with a clear message and changes nothing.
- [x] Export then import into an empty app reproduces the same data; importing the same file twice changes nothing.
- [x] The Settings page offers Export (downloads a file) and Import (picks a file) and shows what the import changed.
- [x] The merge lives in the core so the sync engine can reuse it.
- [x] Core tests cover round-trips, newer-wins, tombstones, idempotency and the version check.
- Implemented in `Core/Sync`: `SyncFile` (format version 1: `{"formatVersion": 1, "records": {"gearItems": [...], ...}}`, records in id order so the same data gives the same file), `RecordMerge.IncomingWinners` (newer `UpdatedAt` wins, tie keeps the device's copy, tombstones are ordinary records) and `Backup` (`AppCore.Backup.ExportAsync` / `ImportAsync`, saved in one `SaveAllAsync`). `RecordTable.All` lists every table with its record type; a new record type must be added there or it won't be exported or synced.
- For ticket 24: `RecordMerge` is the newer-wins rule only. Sync adds the base comparison (bookkeeping) on top to detect Sync Conflicts; JSON import has no base, so it never produces conflicts.
- Settings are included, so importing a file with newer Settings changes units/theme (the page reapplies the theme after import). A tombstone only in the file is saved (so the deletion still syncs) but not counted in the summary. Records in old layouts (pre-ticket-27 Gear Items) are exported as stored and converted when read.
- Web: Settings → Backup section with Export (downloads `backpacking-tracker-YYYY-MM-DD.json` via `window.downloadFile` in index.html) and Import (shows "Gear Items: 1 added, 1 updated · …", "Nothing changed", or the refusal). Verified with headless Edge at phone width: export, re-import (nothing changed), edited file (1 added, 1 updated, visible on Food page), not-an-export and format-version-2 refusals.
