# 14: Packing checklist and water carried

**What to build:** Before leaving I can tick off each Packing List entry as packed and reset the checklist. I can record how much water I'm carrying on a Trip, and it counts as Consumable Weight in my Starting Pack Weight.

**Blocked by:** 12 (Trips from a Gear List)

**Status:** ready-for-agent

- [x] Each Packing List entry has a packed flag I can tick and untick; progress (for example 12 of 30 packed) is shown.
- [x] I can reset all packed flags at once.
- [x] A Trip has an optional water carried weight, entered in my units and stored in grams.
- [x] Water carried is included in Consumable Weight and Starting Pack Weight and appears as its own line in the Category breakdown.
- [x] The checklist is usable one-handed on a phone and works offline.
- [x] Core tests cover packed flags, reset and water in the totals.

## Comments

- From ticket 09: the spec says "Meal Plan food and water appear as their own breakdown lines". `WeightCalculator` builds `PackWeight.ByCategory` from List Entries only; add food or water as its own line (after the Categories), and include it in the Consumable Weight that the shares are taken from.
- From ticket 12: a Packing List is `Trip.PackingList` (`ListContents`), using the same `ListEntry` as Gear Lists. The packed flag can go on `ListEntry` (unused on Gear Lists); `ListContents.Copy()` should clear it when a Gear List is copied.
- Implemented: `ListEntry.Packed` (left out of stored JSON when false, so Gear Lists are unchanged); `ListContents.Copy()` clears it. Raising an entry's quantity (Update, or adding the same Gear Item again) unticks it, since the extra one is not packed yet; lowering it or changing Carry Mode keeps it ticked. `TripPlanner.SetPackedAsync`, `ResetPackedAsync` and `SetWaterCarriedAsync` (grams, null or 0 clears it, negative refused). `Trip.WaterCarriedGrams`; `WeightCalculator.Calculate(..., waterGrams)` adds it to Consumable Weight and puts a "Water" line (`Category.Water`, never stored) last in the breakdown, after "No Category". `TripView.PackedCount` / `ChecklistCount` count entries, not quantities. Tests in `PackingChecklistTests` and `CategoryBreakdownTests`.
- Web: the Trip page has Packing List, Checklist and Itinerary tabs. The Checklist tab shows progress with Reset (confirmed), then each entry by Category as a 56px row that ticks with one tap anywhere; packed rows are struck through. Water carried is a row at the foot of the totals (`PackTotals` now takes child content) opening `WaterDialog` (litres by default, or oz/lb/g; "No water" clears), and shows the water as litres and weight, such as "2.0 L · 4 lb 6.5 oz". `WeightUnit.Litres` (1 L = 1000 g) is offered for water only: `WeightInput.WaterUnits` vs `GearUnits`. Ticking works offline, since everything is stored on the device. MudTabs needed `MinimumTabWidth="80px"` for three tabs to fit at phone width. Verified with a headless Edge test at phone width, light and dark.
