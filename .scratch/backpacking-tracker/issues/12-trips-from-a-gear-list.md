# 12: Trips from a Gear List

**What to build:** I can create a Trip with a name and start and end dates, either from a Gear List or empty. The Trip gets its own Packing List, a copy of the Gear List's entries that I can change for this trip only, and shows its weight totals. My Trips are listed as upcoming (Planned) and past (Completed).

**Blocked by:** 08 (Gear Lists and weight totals)

**Status:** ready-for-agent

- [x] Creating a Trip from a Gear List deep-copies its List Entries into the Trip's Packing List; later edits to the Gear List never change the Trip, and edits to the Packing List never change the Gear List.
- [x] Deleting the source Gear List leaves the Trip untouched.
- [x] I can add, remove and change Packing List entries (quantity, Carry Mode).
- [x] The Trip shows Base Weight, Worn Weight, Consumable Weight and Starting Pack Weight using the weight calculator, with current Gear Item weights.
- [x] New Trips have Trip Status Planned; the Trips page lists upcoming and past Trips.
- [x] The Gear Item hard-delete guard counts Packing List entries as references.
- [x] Core tests cover the copy independence in both directions, Packing List edits, Trip totals and the delete guard.

## Comments

- From ticket 06: Trips (Packing Lists) must also implement `IGearItemReferences` and register in `AppCore`, so a Gear Item on a Trip can't be deleted (Retire is offered instead). As with Gear Lists, a Packing List entry may point at a Gear Item deleted on another device before a sync.
- From ticket 08: when copying a Gear List's entries into a Packing List, give each copied entry a new id. Totals should come from `WeightCalculator` in `Core/Weight` (shared with Gear Lists).
- Implemented: the entry rules Gear Lists and Packing Lists share (add, update, move, remove, copy, and the view with sections and totals) moved from `GearLists` into `Lists/ListContents` (Categories + Entries). `GearList` keeps its stored shape and converts with `Contents` / `With(...)`. New `Core/Trips`: `Trip` (name, start/end `DateOnly`, `TripStatus`, `CreatedFrom` Gear List name for display only, `PackingList` as `ListContents`, stored in table `trips`) and `TripPlanner` (`AppCore.Trips`): `CreateTripAsync(name, start, end, fromGearListId?)` (an empty Trip gets a copy of the Default Categories), `ListTripsAsync` (Upcoming soonest first, Past most recent first), `GetTripAsync`, and Add/Update/Move/RemoveEntryAsync. It implements `IGearItemReferences` ("Trip \"Name\""). Tests in `TripTests`.
- Web: Trips page (Upcoming / Past), `NewTripDialog` (shows refusals in the dialog so nothing typed is lost), Trip page with totals, breakdown and the Packing List, and "Plan a Trip" in the Gear List menu. `PackTotals`, `ListEntryRow` and `AddGearDialog` (now given `Add` / `LoadEntries` callbacks) are shared with the Gear List page. Verified with a headless Edge test at phone width, light and dark: create from a Gear List and empty, edit the Packing List, Gear List unchanged.
- Not in this ticket: adding, renaming, reordering or removing Categories on a Packing List (entries can move between its Categories), and renaming a Trip.
