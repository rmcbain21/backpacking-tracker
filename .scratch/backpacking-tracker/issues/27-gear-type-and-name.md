# 27: Gear Type and Name

**What to build:** A Gear Item is known by its Name (such as "SWD Long Haul 50 EPLX") and its Gear Type (such as Backpack), instead of a name, a brand/model and a Category. The Gear page lists my gear sorted by Gear Type, and I can filter by Gear Type. My existing gear is converted without losing anything. See ADR 0003 and GLOSSARY.md (Gear Item, Gear Type).

**Blocked by:** None (reworks parts of 01, 04, 05 and 06)

**Status:** ready-for-agent

- [x] A Gear Item has a Name (required) and a Gear Type (optional, free text). The separate Brand/Model field and the Gear Item's Category are gone.
- [x] Gear Type is cleaned up like a Tag: trimmed, and a Gear Type matching one already in use (ignoring case) takes the existing spelling, so "power cord" becomes "Power Cord" if that exists. Gear Types in use are suggested when adding or editing.
- [x] Existing Gear Items are converted when read: the old name becomes the Gear Type and the old brand/model becomes the Name (an item with no brand/model keeps its name as the Name and gets the old name as its Gear Type too). Nothing the backpacker typed is lost.
- [x] The Categories that existed before (the defaults and any the backpacker made) become the Default Categories (ticket 28 uses them).
- [x] The Gear page lists Gear Items sorted by Gear Type, then Name, each row showing the Name with the Gear Type beneath. Search matches Name, Gear Type and Tags. A Gear Type filter replaces the Category filter.
- [x] Adding and editing a Gear Item ask for Name and Gear Type (with suggestions); no Category.
- [x] Deleting, Retiring and the reference check are unchanged.
- [x] Core tests cover Gear Type clean-up and suggestions, the conversion of old records, sorting, search and the Gear Type filter.

## Comments

- Implemented. `GearItem` has Name and GearType; `GearItemDetails` is (Name, GearType, WeightGrams, Tags, Price, Notes). Gear Type is cleaned up like a Tag (`TagRules`), suggested from `ListGearTypesAsync` (non-Retired gear), and the Gear page uses `ListGearItemsAsync(GearFilter)` sorted by Gear Type (none last) then Name, with a Gear Type filter.
- Old records are converted as they're read (`GearCatalogue.ConvertOldRecord`): `brandModel` present means an old record. The old Category id stays on the record as `LegacyCategoryId` (not shown) for ticket 28 to place existing List Entries; until then the Gear List breakdown uses it.
- The fixed CSV layout is now Category, Gear Type, Name, Weight (g), Qty, Carry Mode; a trip sheet's Item is the Gear Type. Matching is on Gear Type + Name. A row with a Gear Type but no Name uses the Gear Type as its Name. The import no longer creates Categories; each row result keeps its Category for ticket 29.
- Removing a Default Category no longer moves gear (gear has no Category); at least one must stay.
- Verified with a headless Edge test at phone width: two old-format records planted in IndexedDB show converted; a typed and a suggested Gear Type; the Gear Type filter and search; the user's real trip sheet imports as 59 created, 1 reused, 15 rejected, sorted by Gear Type.
