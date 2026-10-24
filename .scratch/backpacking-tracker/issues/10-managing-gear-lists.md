# 10: Managing Gear Lists

**What to build:** I can rename, duplicate and delete Gear Lists to manage variations like summer and winter kits, and a Gear Item used on any Gear List can no longer be hard-deleted (only Retired).

**Blocked by:** 06 (Edit, Retire and delete Gear Items), 08 (Gear Lists and weight totals)

**Status:** ready-for-agent

- [x] I can rename a Gear List.
- [x] Duplicating a Gear List creates a new Gear List with copies of all its List Entries.
- [x] Deleting a Gear List tombstones it after confirmation.
- [x] The Gear Item hard-delete guard counts List Entries on Gear Lists as references.
- [x] Core tests cover rename, duplicate, delete, and the extended delete guard.

## Comments

- From ticket 08: "The Gear Item hard-delete guard counts List Entries on Gear Lists as references" is already done (Gear Lists implement `IGearItemReferences`, registered in `AppCore`, with tests in `RetireAndDeleteTests`). Duplicating a Gear List should give the copied List Entries new ids.
- Implemented: `GearLists.RenameGearListAsync`, `DuplicateGearListAsync(id, name)` (copies Categories with their ids, which are per list, and entries with new ids) and `DeleteGearListAsync` (tombstone; its Gear Items are untouched and become deletable if no other list uses them). The Gear List page has a menu by the title: Rename, Duplicate (name pre-filled "<name> copy", opens the copy), Delete (confirm, back to Gear Lists). Verified with a headless Edge test at phone width.
