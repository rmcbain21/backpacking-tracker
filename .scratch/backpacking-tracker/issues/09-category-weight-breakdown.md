# 09: Category weight breakdown

**What to build:** On a Gear List I can see how its weight splits across Categories, with each Category's weight and share of the total, so I can see where my weight goes.

**Blocked by:** 04 (Categories), 08 (Gear Lists and weight totals)

**Status:** ready-for-agent

- [x] The weight calculator returns a breakdown by the Gear Item's Category, with weight and percentage share, in Category order.
- [x] Categories with no entries on the list are omitted.
- [x] The Gear List page shows the breakdown (a simple table or bar is fine on phone).
- [x] Core tests cover the breakdown sums and shares.

## Comments

- From ticket 08: `WeighedEntry` (in `Core/Weight`) has no Category yet; add it, and give `PackWeight` a breakdown. An entry whose Gear Item was deleted on another device has no Category: give it its own breakdown line rather than dropping its (zero) weight silently.
- Implemented in `WeightCalculator` (Core/Weight): `PackWeight.ByCategory` lists each Category's weight and share (0 to 1) in Category order, leaving out empty Categories. Agreed with the user: the breakdown splits the Starting Pack Weight, so worn entries are left out (Worn Weight stays its own total). Entries whose Category no longer exists go last under "No Category" (`Category.None`, shared with the Gear page grouping); entries whose Gear Item was deleted weigh nothing and are left out rather than shown as a 0 g line. Shares are shown as whole percentages (`UnitFormat.Share`).
- The Gear List page shows "Where the pack weight goes": each Category with its weight, share and a bar.
- Verified with a headless Edge test at phone width: Tent 900 g (Shelter), Stove 100 g (Cook), Rain Jacket 200 g worn, Fuel 100 g consumable gives Shelter 900 g 82%, Cook 200 g 18%; making the jacket base adds Clothing 200 g and gives 69% / 15% / 15%.
