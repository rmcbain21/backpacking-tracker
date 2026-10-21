# 24: Google Drive sync

**What to build:** I can connect my Google Drive once, then tap "Sync now" to back up and sync my data through a hidden file in my Drive's app folder. Changes made on one device show up on my others, deletions stay deleted, and I can see when I last synced. I can disconnect without losing local data.

**Blocked by:** 22 (JSON export and import), 23 (Google OAuth client setup)

**Status:** ready-for-agent

- [ ] The core depends on a cloud file store port (read the sync file, write the sync file, get last-modified); tests use an in-memory implementation.
- [ ] The Google Drive adapter signs in with Google Identity Services using only the `drive.appdata` scope and stores the sync file in the app-data folder. Access tokens are held in memory only.
- [ ] Each device keeps local-only sync bookkeeping: per record, the `updatedAt` at the last successful sync, plus the last successful sync time.
- [ ] A sync reads the file (or treats it as empty if missing), merges per record (only one side changed since base takes that side; a record on only one side is taken), saves locally, writes the file, then updates bookkeeping. Same-record changes on both sides fall back to newer-wins until ticket 25.
- [ ] A failed write leaves bookkeeping unchanged so the next sync retries.
- [ ] The Settings page has Connect, Disconnect, "Sync now" and a "last synced" indicator.
- [ ] Tests simulate two devices (two core instances, separate in-memory record stores, one shared in-memory cloud file store) covering propagation of adds, edits and tombstones in both directions, and retry after a failed write.
