# 18: Meals

**What to build:** I can create a Meal, a named combination of Food Items with a number of servings of each (such as a Ramen Bomb), and see its total weight, calories and Calorie Density. Meals can be tagged, Retired and deleted with the same rules as gear and food.

**Blocked by:** 17 (Food Items)

**Status:** ready-for-agent

- [x] I can create and edit a Meal with a name, Tags and components (Food Item plus servings, which may be fractional).
- [x] A Meal shows total weight, calories, protein (where known), cost (where known) and Calorie Density, from its components' current values.
- [x] Retired Food Items are not offered when adding components.
- [x] The Food Item hard-delete guard counts Meal components as references.
- [x] Meals can be Retired, un-retired and hard-deleted when unreferenced.
- [x] Core tests cover Meal totals, the extended Food Item delete guard and Meal Retire/delete.

## Comments

- From ticket 06: Food Items and Meals follow the same Retire and delete rules as Gear Items (story 69). The Gear catalogue's pattern to copy: a Retired flag hidden from catalogue views but found by id, a deletion plan gathered from registered reference checks (Meals and Meal Plans use Food Items; Meal Plans use Meals), a tombstone on delete. `IGearItemReferences` is Gear-specific, so Food needs its own.
- From ticket 17: register Meals with `Food.AddReferences(...)` in AppCore by implementing `IFoodItemReferences` (describe uses as `Meal "Ramen Bomb"`), and test the refusal through a real Meal. For a Meal's own delete plan, derive from `DeletionPlan` (see `FoodItemDeletionPlan`). Meal Calorie Density: `CalorieDensity.PerGram(totalCalories, totalGrams)`.
- Implemented: `MealCatalogue` (`App.Meals`) with `Meal` records in the `meals` table: create, edit, search (`MealFilter`: name, Tags and the names of its Food Items), Retire/un-retire, delete guard through `IMealReferences` (nothing registers yet; Meal Plans will). `MealView` carries each component's Food Item and `MealTotals` (weight, calories, Calorie Density, and protein and cost summed over the Food Items that record them, null when none do). It registers as an `IFoodItemReferences`, so a Food Item in any Meal, Retired or not, can't be deleted.
- Rules: at least one Food Item; servings more than zero, to 0.01; the same Food Item twice becomes one component with servings added. A Retired Food Item can't be newly added, but stays in (and counts toward) a Meal that already has it. Meal Tags are their own pool, like Food and Gear Tags.
- Web: the Food page has Food Items and Meals tabs (search and + follow the tab); `MealDialog` adds Food Items by search-and-tap (Retired ones not offered), with a servings field per Food Item and live totals; Retired food lists Retired Meals too. Verified with headless Edge at phone width: create (and the empty-Meal refusal), live totals, search by Food Item, the Food Item delete refusal naming the Meal, Retired food not offered, Retire, and delete from the Retired page.
