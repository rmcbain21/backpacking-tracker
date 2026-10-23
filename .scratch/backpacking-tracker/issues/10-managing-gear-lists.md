# 10: Managing Gear Lists

**What to build:** I can rename, duplicate and delete Gear Lists to manage variations like summer and winter kits, and a Gear Item used on any Gear List can no longer be hard-deleted (only Retired).

**Blocked by:** 06 (Edit, Retire and delete Gear Items), 08 (Gear Lists and weight totals)

**Status:** ready-for-agent

- [ ] I can rename a Gear List.
- [ ] Duplicating a Gear List creates a new Gear List with copies of all its List Entries.
- [ ] Deleting a Gear List tombstones it after confirmation.
- [ ] The Gear Item hard-delete guard counts List Entries on Gear Lists as references.
- [ ] Core tests cover rename, duplicate, delete, and the extended delete guard.

## Comments

- From ticket 08: "The Gear Item hard-delete guard counts List Entries on Gear Lists as references" is already done (Gear Lists implement `IGearItemReferences`, registered in `AppCore`, with tests in `RetireAndDeleteTests`). Duplicating a Gear List should give the copied List Entries new ids.
