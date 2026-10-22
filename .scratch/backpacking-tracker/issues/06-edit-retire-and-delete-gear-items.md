# 06: Edit, Retire and delete Gear Items

**What to build:** I can edit every field of a Gear Item (including optional price and notes), Retire gear I no longer own so it disappears from my catalogue but stays on anything that used it, see and un-retire Retired gear, and permanently delete a Gear Item only when nothing references it.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

- [x] I can edit name, brand/model, weight, Category, Tags, price and notes.
- [x] Retiring hides a Gear Item from the catalogue and from pickers, but it still resolves wherever it is referenced.
- [x] A Retired view lists Retired Gear Items, and I can un-retire one.
- [x] Hard delete is allowed only when nothing references the Gear Item; otherwise the core refuses and the UI offers Retire. The reference check is built so later tickets (List Entries, Packing Lists) can add their references to it.
- [x] Deleting sets the tombstone flag rather than removing the record from storage.
- [x] Core tests cover editing, Retire, un-retire, and the delete guard.

## Comments

- From ticket 01: `GearCatalogue.ListGearItemsAsync` doesn't filter out tombstoned (`Deleted`) records yet, because nothing could delete in ticket 01. Hard delete should set the tombstone and listing should skip it, with a test.
- From ticket 04: editing a Gear Item must require a Category, as adding does (ticket 04's "Adding or editing a Gear Item requires choosing a Category"). `GearItemDetails` is the shape to reuse for editing.
- From ticket 05: editing a Gear Item's Tags should reuse the same clean-up as adding (`CleanUpTagsAsync` in `GearCatalogue`, built on `TagRules`) and the `TagEditor` component, including `TagsIncludingTyped()` when saving.
- From ticket 05: `ListTagsAsync` (Tag suggestions and spelling) and search read every Gear Item. Once delete and Retire exist, deleted (tombstoned) Gear Items must be left out everywhere, and Retired ones left out of suggestions and the catalogue views.
- Implemented. `UpdateGearItemAsync` shares every rule with adding (`ApplyDetailsAsync`). Price is optional, to the cent, shown in dollars (`PriceInput`); notes are free text. Retire/un-retire, `ListRetiredGearItemsAsync`, and `GetGearItemAsync` (finds Retired items too). Deleting goes through `PlanGearItemDeletionAsync`, which asks every registered `IGearItemReferences` what uses the Gear Item and refuses, offering Retire, if anything does; the delete keeps a tombstone.
- Editing keeps the stored grams when the weight text is unchanged (`WeightInput.TextForEditing` / `ParseEdited`), so a weight shown in oz doesn't drift each time it's saved.
- UI: add and edit share one dialog behind a + button; tapping a Gear Item edits it (Retire and Delete are there); a Retired page lists Retired gear with Un-retire and Delete. The gear list shows price under the weight.
- Retired gear can't be edited from the Retired page (un-retire first).
- Verified with a headless Edge test against the Release publish at phone width.
