# 25: Sync Conflicts

**What to build:** When the same record was changed on two devices since they last synced, sync doesn't silently overwrite either. It shows a Sync Conflict with both versions side by side, keeps each device's version until I decide, shows a badge with how many are waiting, and lets me settle each one by choosing a version.

**Blocked by:** 24 (Google Drive sync)

**Status:** ready-for-agent

- [ ] The merge detects a Sync Conflict when both sides changed a record since base and the contents differ; identical changes are not a conflict.
- [ ] While unsettled, the local version stays on the device and the remote version stays in the sync file; the conflict is stored locally and survives reloads.
- [ ] A badge shows the number of pending Sync Conflicts.
- [ ] A conflict view shows both versions side by side, with the differing fields highlighted, in display units.
- [ ] Choosing a version writes it with a new `updatedAt`, clears the conflict, and wins on the next sync on every device.
- [ ] Two-device tests cover detection, identical edits, a conflict on a tombstone versus an edit, settling on either device, and propagation after settling.
