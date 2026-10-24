# 15: Completing and reopening Trips

**What to build:** I can mark a Trip Completed, which freezes every Packing List weight so re-weighing gear later never changes this Trip's numbers. On a Completed Trip I can still fix journal notes, route details, distances, elevation and water, but the Packing List is locked until I Reopen it. I can also save a Trip's Packing List as a new Gear List, and delete a Trip.

**Blocked by:** 13 (Trip Days, Itinerary and date changes)

**Status:** ready-for-agent

- [ ] Completing a Trip copies each Packing List entry's current Gear Item weight onto the entry, and all weight figures for a Completed Trip use the frozen values.
- [ ] Re-weighing a Gear Item afterwards does not change the Completed Trip's figures, but does change Planned Trips.
- [ ] On a Completed Trip, journal notes, route fields, distances, elevation and water carried stay editable; Packing List changes are refused.
- [ ] Reopen sets the Trip back to Planned and clears frozen values; completing again takes a fresh freeze.
- [ ] "Save as Gear List" creates a new Gear List from the Packing List's entries (Gear Item, quantity, Carry Mode).
- [ ] I can delete a Trip after confirmation.
- [ ] Core tests cover freezing, the edit locks, Reopen, Save as Gear List and delete.
- From ticket 12: `TripListing.Past` (Completed Trips, most recent start date first) has no test yet, since no Trip could be Completed; add one here. "Save as Gear List" can use `ListContents.Copy()` (as Duplicate does) and `GearLists.NewGearList`. Packing List edits all go through `TripPlanner`'s Add/Update/Move/RemoveEntryAsync, so that's where to refuse them on a Completed Trip.

## Comments
- From ticket 13: route fields and journal notes are edited through `TripPlanner.UpdateDayAsync`; leave it open on Completed Trips. Decide whether `ChangeDatesAsync` should be allowed on a Completed Trip.
