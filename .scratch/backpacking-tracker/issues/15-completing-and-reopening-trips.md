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
