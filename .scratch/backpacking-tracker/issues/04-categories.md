# 04: Categories

**What to build:** Every Gear Item belongs to exactly one Category. On first run I get a sensible default set, and I can add, rename, reorder and remove my own. I can filter my gear by Category.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

- [x] On first run, default Categories exist: Shelter, Sleep, Pack, Cook, Water, Clothing, Electronics, Hygiene, First Aid, Misc.
- [x] I can add, rename, reorder and remove Categories.
- [x] Removing a Category that Gear Items use is refused unless I choose another Category to move them to.
- [x] Adding or editing a Gear Item requires choosing a Category.
- [x] The gear page groups or filters Gear Items by Category, in Category order.
- [x] Core tests cover defaults, rename, reorder, and the remove guard and move.

## Comments

- Implemented. Category operations are on `app.Gear` (the spec puts Categories in the Gear catalogue): `ListCategoriesAsync`, `AddCategoryAsync`, `RenameCategoryAsync`, `MoveCategoryAsync(id, newPosition)`, `PlanCategoryRemovalAsync`, `RemoveCategoryAsync(id, moveGearItemsToCategoryId)`, `ListGearItemsByCategoryAsync`. `AddGearItemAsync` now takes a `GearItemDetails` record. Rule violations throw `RuleViolationException` with a message for the backpacker.
- "Adding or editing a Gear Item requires choosing a Category": adding is done here; editing is ticket 06.
- The Gear page groups by Category. A Category *filter* is left to ticket 05 (search and filter).
- Default Categories have fixed ids and an epoch `updatedAt` (never edited), so every device creates the same records and sync doesn't see them as conflicting edits. Removal keeps a tombstone; the last Category can't be removed.
- Gear Items saved before Categories existed (tickets 01-03) are read as Misc.
- Gear Items whose Category no longer exists (only possible once sync brings in another device's removal) show under "No Category" at the end of the Gear page instead of disappearing.
- Category names are unique ignoring case; reordering uses up/down buttons.
- Verified with a headless Edge test against the Release publish at phone width, including an old-style Gear Item planted in IndexedDB.
- Rethink (ADR 0003): Categories now belong to each Gear List and a Gear Item has a Gear Type and Name instead of a Category and Brand/Model. Tickets 27, 28 and 29 rework the parts of this ticket that used the Gear Item's Category or Brand/Model.
