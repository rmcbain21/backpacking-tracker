# 18: Meals

**What to build:** I can create a Meal, a named combination of Food Items with a number of servings of each (such as a Ramen Bomb), and see its total weight, calories and Calorie Density. Meals can be tagged, Retired and deleted with the same rules as gear and food.

**Blocked by:** 17 (Food Items)

**Status:** ready-for-agent

- [ ] I can create and edit a Meal with a name, Tags and components (Food Item plus servings, which may be fractional).
- [ ] A Meal shows total weight, calories, protein (where known), cost (where known) and Calorie Density, from its components' current values.
- [ ] Retired Food Items are not offered when adding components.
- [ ] The Food Item hard-delete guard counts Meal components as references.
- [ ] Meals can be Retired, un-retired and hard-deleted when unreferenced.
- [ ] Core tests cover Meal totals, the extended Food Item delete guard and Meal Retire/delete.

## Comments

- From ticket 06: Food Items and Meals follow the same Retire and delete rules as Gear Items (story 69). The Gear catalogue's pattern to copy: a Retired flag hidden from catalogue views but found by id, a deletion plan gathered from registered reference checks (Meals and Meal Plans use Food Items; Meal Plans use Meals), a tombstone on delete. `IGearItemReferences` is Gear-specific, so Food needs its own.
