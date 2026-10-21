# 12: Trips from a Gear List

**What to build:** I can create a Trip with a name and start and end dates, either from a Gear List or empty. The Trip gets its own Packing List, a copy of the Gear List's entries that I can change for this trip only, and shows its weight totals. My Trips are listed as upcoming (Planned) and past (Completed).

**Blocked by:** 08 (Gear Lists and weight totals)

**Status:** ready-for-agent

- [ ] Creating a Trip from a Gear List deep-copies its List Entries into the Trip's Packing List; later edits to the Gear List never change the Trip, and edits to the Packing List never change the Gear List.
- [ ] Deleting the source Gear List leaves the Trip untouched.
- [ ] I can add, remove and change Packing List entries (quantity, Carry Mode).
- [ ] The Trip shows Base Weight, Worn Weight, Consumable Weight and Starting Pack Weight using the weight calculator, with current Gear Item weights.
- [ ] New Trips have Trip Status Planned; the Trips page lists upcoming and past Trips.
- [ ] The Gear Item hard-delete guard counts Packing List entries as references.
- [ ] Core tests cover the copy independence in both directions, Packing List edits, Trip totals and the delete guard.
