# 22: JSON export and import

**What to build:** I can export my whole database as a JSON file I download, and import such a file back, merged with my existing data per record, so I have a backup that doesn't depend on Google. The file format is the sync file format that ticket 24 uses.

**Blocked by:** 19 (Meal Plan on Trip Days)

**Status:** ready-for-agent

- [ ] The export is one JSON document with a format version and every record of every type, including tombstones.
- [ ] Import merges per record: a record only in the file is added; for a record on both sides, the one with the later `updatedAt` wins; tombstones in the file delete local records with an older `updatedAt`.
- [ ] Import rejects files with an unknown format version with a clear message and changes nothing.
- [ ] Export then import into an empty app reproduces the same data; importing the same file twice changes nothing.
- [ ] The Settings page offers Export (downloads a file) and Import (picks a file) and shows what the import changed.
- [ ] The merge lives in the core so the sync engine can reuse it.
- [ ] Core tests cover round-trips, newer-wins, tombstones, idempotency and the version check.
