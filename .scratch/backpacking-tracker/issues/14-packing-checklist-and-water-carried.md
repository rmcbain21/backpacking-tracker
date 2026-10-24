# 14: Packing checklist and water carried

**What to build:** Before leaving I can tick off each Packing List entry as packed and reset the checklist. I can record how much water I'm carrying on a Trip, and it counts as Consumable Weight in my Starting Pack Weight.

**Blocked by:** 12 (Trips from a Gear List)

**Status:** ready-for-agent

- [ ] Each Packing List entry has a packed flag I can tick and untick; progress (for example 12 of 30 packed) is shown.
- [ ] I can reset all packed flags at once.
- [ ] A Trip has an optional water carried weight, entered in my units and stored in grams.
- [ ] Water carried is included in Consumable Weight and Starting Pack Weight and appears as its own line in the Category breakdown.
- [ ] The checklist is usable one-handed on a phone and works offline.
- [ ] Core tests cover packed flags, reset and water in the totals.

## Comments

- From ticket 09: the spec says "Meal Plan food and water appear as their own breakdown lines". `WeightCalculator` builds `PackWeight.ByCategory` from List Entries only; add food or water as its own line (after the Categories), and include it in the Consumable Weight that the shares are taken from.
- From ticket 12: a Packing List is `Trip.PackingList` (`ListContents`), using the same `ListEntry` as Gear Lists. The packed flag can go on `ListEntry` (unused on Gear Lists); `ListContents.Copy()` should clear it when a Gear List is copied.
