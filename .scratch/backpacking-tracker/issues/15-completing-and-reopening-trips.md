# 15: Completing and reopening Trips

**What to build:** I can mark a Trip Completed, which freezes every Packing List weight so re-weighing gear later never changes this Trip's numbers. On a Completed Trip I can still fix journal notes, route details, distances, elevation and water, but the Packing List is locked until I Reopen it. I can also save a Trip's Packing List as a new Gear List, and delete a Trip.

**Blocked by:** 13 (Trip Days, Itinerary and date changes)

**Status:** ready-for-agent

- [x] Completing a Trip copies each Packing List entry's current Gear Item weight onto the entry, and all weight figures for a Completed Trip use the frozen values.
- [x] Re-weighing a Gear Item afterwards does not change the Completed Trip's figures, but does change Planned Trips.
- [x] On a Completed Trip, journal notes, route fields, distances, elevation and water carried stay editable; Packing List changes are refused.
- [x] Reopen sets the Trip back to Planned and clears frozen values; completing again takes a fresh freeze.
- [x] "Save as Gear List" creates a new Gear List from the Packing List's entries (Gear Item, quantity, Carry Mode).
- [x] I can delete a Trip after confirmation.
- [x] Core tests cover freezing, the edit locks, Reopen, Save as Gear List and delete.
- From ticket 12: `TripListing.Past` (Completed Trips, most recent start date first) has no test yet, since no Trip could be Completed; add one here. "Save as Gear List" can use `ListContents.Copy()` (as Duplicate does) and `GearLists.NewGearList`. Packing List edits all go through `TripPlanner`'s Add/Update/Move/RemoveEntryAsync, so that's where to refuse them on a Completed Trip.

## Comments
- From ticket 13: route fields and journal notes are edited through `TripPlanner.UpdateDayAsync`; leave it open on Completed Trips. Decide whether `ChangeDatesAsync` should be allowed on a Completed Trip.
- From ticket 14: on a Completed Trip, keep `SetWaterCarriedAsync` allowed (water stays editable per the spec). Decide whether ticking the checklist is locked too; `SetPackedAsync` / `ResetPackedAsync` are separate from the Add/Update/Move/Remove entry methods. "Save as Gear List" via `ListContents.Copy()` already clears Packed.
- Decided: on a Completed Trip, date changes (`PlanDateChangeAsync`, `ChangeDatesAsync`, `MoveTripAsync`) and the packing checklist (`SetPackedAsync`, `ResetPackedAsync`) are locked too, like the Packing List: Reopen first. Changing dates adds and removes Trip Days, and the Meal Plan on them is locked; ticks on a past Trip mean nothing.
- Implemented: `ListEntry.FrozenWeightGrams` (left out of stored JSON when null). `TripPlanner.CompleteTripAsync` freezes each entry's current Gear Item weight (`ListContents.Freeze`; an entry whose Gear Item is already gone stays unfrozen and weighs nothing); `ListContents.View` uses the frozen weight when set, so totals, subtotals and the breakdown all follow, even if the Gear Item is deleted later on another device. `ReopenTripAsync` clears them (`Unfreeze`). Completing a Completed Trip, or Reopening a Planned one, is refused. Locked edits go through `GetPlannedTripAsync(tripId, what)`: "This Trip is Completed. Reopen it to change its Packing List." (or "its packing checklist", "its dates"). `UpdateDayAsync` and `SetWaterCarriedAsync` stay open. `SaveAsGearListAsync(tripId, name)` copies the Packing List (`ListContents.Copy()` now also clears frozen weights) into `GearLists.CreateGearListAsync(name, contents)`; an entry whose Category isn't on the list goes in the first one. `DeleteTripAsync` tombstones the Trip, Planned or Completed, which frees its gear for deletion. Tests in `TripStatusTests`, including the `TripListing.Past` order.
- Web: the Trip page has a ⋮ menu: Mark Completed / Reopen (each confirmed), Save as Gear List (asks the name, defaulting to the Trip's, then opens the new list) and Delete (confirmed, back to Trips). On a Completed Trip an info note says weights are frozen; the change-dates button, the + buttons and the FAB are hidden; Packing List rows are read-only (`ListEntryRow ReadOnly`, showing "× 1 · Base"); the checklist shows its ticks but doesn't tick and has no Reset. Water and the Itinerary stay editable. Verified with a headless Edge test at phone width, light and dark.
