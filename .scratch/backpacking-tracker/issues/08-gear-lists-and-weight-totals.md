# 08: Gear Lists and weight totals

**What to build:** I can create a named Gear List, add Gear Items to it as List Entries with a quantity and a Carry Mode (base, worn or consumable), and see its Base Weight, Worn Weight, Consumable Weight and Starting Pack Weight, always using each Gear Item's current weight.

**Blocked by:** 01 (Walking skeleton), 03 (Units display and settings)

**Status:** ready-for-agent

- [ ] I can create a Gear List with a name.
- [ ] I can add a Gear Item as a List Entry; quantity defaults to 1 and Carry Mode to base. Retired Gear Items are not offered.
- [ ] I can change a List Entry's quantity (whole number, at least 1) and Carry Mode, and remove it.
- [ ] The same Gear Item can have different Carry Modes on different Gear Lists.
- [ ] A pure weight calculator in the core returns Base Weight, Worn Weight, Consumable Weight and Starting Pack Weight (Base + Consumable) from List Entries, where an entry's weight is item weight times quantity. It is designed to also accept Meal Plan food and water later.
- [ ] Re-weighing a Gear Item updates every Gear List's totals.
- [ ] The Lists page shows Gear Lists and a Gear List's entries and totals in the chosen units.
- [ ] Core tests cover the calculator (Carry Modes, quantities) and the live weight behaviour.

## Comments

- From ticket 06: implement `IGearItemReferences` for Gear Lists (`FindUsesAsync` returns e.g. `Gear List "Summer kit"`) and register it once, in `AppCore`, with `Gear.AddReferences`. Then replace the stand-in reference test in `RetireAndDeleteTests` with one using a real Gear List, and drop `InternalsVisibleTo` if nothing else needs it.
- From ticket 06: List Entries must resolve Retired Gear Items (`GetGearItemAsync` finds them). Resolving every entry one by one reloads the catalogue each time; add a bulk lookup (by ids, including Retired) for weight totals.
- From ticket 06: the delete guard only sees this device. One device can delete a Gear Item while another adds it to a Gear List; after sync the List Entry points at a deleted item. Show such an entry as a deleted Gear Item rather than failing.
