# 17: Food Items

**What to build:** I can keep a library of Food Items with a name, serving weight and calories per serving (plus optional protein and cost per serving), see each one's Calorie Density, tag them, and Retire or delete them with the same rules as gear.

**Blocked by:** 03 (Units display and settings), 06 (Edit, Retire and delete Gear Items)

**Status:** ready-for-agent

- [x] I can add and edit a Food Item: name, serving weight, calories per serving (required); protein per serving and cost per serving (optional); Tags.
- [x] Calorie Density is shown per Food Item (calories per oz in imperial, per 100 g in metric).
- [x] Food Items can be searched by name and Tag.
- [x] Retire, un-retire and the hard-delete guard work as for Gear Items, reusing the same reference-check approach.
- [x] The Food page lists Food Items and works on phone widths.
- [x] Core tests cover Calorie Density, Retire and the delete guard.

## Comments

- From ticket 06: Food Items and Meals follow the same Retire and delete rules as Gear Items (story 69). The Gear catalogue's pattern to copy: a Retired flag hidden from catalogue views but found by id, a deletion plan gathered from registered reference checks (Meals and Meal Plans use Food Items; Meal Plans use Meals), a tombstone on delete. `IGearItemReferences` is Gear-specific, so Food needs its own.
- Implemented: `FoodCatalogue` (`App.Food`) with `FoodItem` records in the `foodItems` table: add, edit, search (`FoodFilter`, name and Tags), Retire/un-retire, and the delete guard through `IFoodItemReferences` (registered with `Food.AddReferences` in AppCore, like Gear). Food Tags are kept apart from Gear Tags. Serving weight must be more than zero (after rounding to 0.1 g) since Calorie Density divides by it; zero calories is allowed (coffee). `CalorieDensity` gives cal/oz (imperial) or cal/100 g (metric) and has a grams-and-calories overload for Meal totals in ticket 18. `NutritionInput` reads calories (required, whole) and protein (optional, 0.1 g); cost reuses `PriceInput`.
- The Retire/delete message logic moved from `GearItemDeletionPlan` into a shared `DeletionPlan` base, which `FoodItemDeletionPlan` also uses; the web prompt is now `ConfirmDeletionAsync(DeletionPlan)`.
- Web: the Food page (search, rows with serving weight, Calorie Density, calories · protein · cost, Tags), `FoodItemDialog` (add/edit/Retire/Delete), and Retired food at `/food/retired`. Verified with headless Edge at phone width: validation, add, search by Tag, edit, Retire, un-retire, delete, and imperial/metric Calorie Density.
- Nothing refers to Food Items yet, so the "in use, Retire instead" refusal is only tested on the plan itself; ticket 18's Meals are the first real reference.
