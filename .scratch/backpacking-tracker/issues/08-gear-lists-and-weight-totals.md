# 08: Gear Lists and weight totals

**What to build:** I can create a named Gear List, add Gear Items to it as List Entries with a quantity and a Carry Mode (base, worn or consumable), and see its Base Weight, Worn Weight, Consumable Weight and Starting Pack Weight, always using each Gear Item's current weight.

**Blocked by:** 01 (Walking skeleton), 03 (Units display and settings)

**Status:** ready-for-agent

- [x] I can create a Gear List with a name.
- [x] I can add a Gear Item as a List Entry; quantity defaults to 1 and Carry Mode to base. Retired Gear Items are not offered.
- [x] I can change a List Entry's quantity (whole number, at least 1) and Carry Mode, and remove it.
- [x] The same Gear Item can have different Carry Modes on different Gear Lists.
- [x] A pure weight calculator in the core returns Base Weight, Worn Weight, Consumable Weight and Starting Pack Weight (Base + Consumable) from List Entries, where an entry's weight is item weight times quantity. It is designed to also accept Meal Plan food and water later.
- [x] Re-weighing a Gear Item updates every Gear List's totals.
- [x] The Lists page shows Gear Lists and a Gear List's entries and totals in the chosen units.
- [x] Core tests cover the calculator (Carry Modes, quantities) and the live weight behaviour.

## Comments

- From ticket 06: implement `IGearItemReferences` for Gear Lists (`FindUsesAsync` returns e.g. `Gear List "Summer kit"`) and register it once, in `AppCore`, with `Gear.AddReferences`. Then replace the stand-in reference test in `RetireAndDeleteTests` with one using a real Gear List, and drop `InternalsVisibleTo` if nothing else needs it.
- From ticket 06: List Entries must resolve Retired Gear Items (`GetGearItemAsync` finds them). Resolving every entry one by one reloads the catalogue each time; add a bulk lookup (by ids, including Retired) for weight totals.
- From ticket 06: the delete guard only sees this device. One device can delete a Gear Item while another adds it to a Gear List; after sync the List Entry points at a deleted item. Show such an entry as a deleted Gear Item rather than failing.
- From ticket 07: `CarryMode` (Base, Worn, Consumable) already exists in `BackpackingTracker.Core.Gear`, stored as a string in JSON. Use it for List Entries rather than adding another.
- Implemented as `app.Lists` (`GearLists`). A Gear List record holds its List Entries (each with its own id, Gear Item id, quantity, Carry Mode) and syncs as one record. A list has one entry per Gear Item: adding a Gear Item already on it adds one to that entry's quantity (spec updated to say so).
- The weight calculator is its own area, `Core/Weight` (`WeightCalculator`, `PackWeight`, `WeighedEntry`), pure, and takes List Entries only for now; `PackWeight` has named properties so food, water and the Category breakdown can be added.
- Totals use each Gear Item's current weight through one bulk lookup (`GearCatalogue.GetGearItemsAsync`), Retired gear included. An entry whose Gear Item was deleted on another device shows as "Deleted Gear Item" and weighs nothing.
- Gear Lists implement `IGearItemReferences` and are registered once in `AppCore`; the stand-in test and `InternalsVisibleTo` are gone.
- UI: Lists tab (each list with its Starting Pack Weight; + to create), a Gear List page (totals; per entry: -/+ quantity, Carry Mode, remove), and an Add gear picker with search that marks gear already on the list.
- Found while testing on a phone-sized screen: the + button covered the last row's buttons; pages now leave room to scroll the last row above it.
- Rethink (ADR 0003): Categories now belong to each Gear List and a Gear Item has a Gear Type and Name instead of a Category and Brand/Model. Tickets 27, 28 and 29 rework the parts of this ticket that used the Gear Item's Category or Brand/Model.
