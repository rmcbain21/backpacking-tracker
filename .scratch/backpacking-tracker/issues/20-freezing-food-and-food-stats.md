# 20: Freezing food and food stats

**What to build:** Completing a Trip also freezes the weight, calories, protein and cost of everything in its Meal Plan, so editing a Food Item or Meal later never changes a past Trip. Trip stats gain calories per day and total food cost.

**Blocked by:** 16 (Trip stats), 19 (Meal Plan on Trip Days)

**Status:** ready-for-agent

- [x] Completing a Trip copies each slot entry's current weight, calories, protein and cost (resolving Meals through their components) onto the slot entry.
- [x] A Completed Trip's food figures use frozen values; Reopen clears them.
- [x] Stats include calories per day (total Meal Plan calories divided by number of Trip Days) and total food cost (sum over slot entries with a cost).
- [x] Editing a Food Item or Meal after completion changes Planned Trips but not Completed ones.
- [x] Core tests cover nutrition freezing, Reopen and both food stats.

## Comments
- From ticket 15: freeze the slot entries in `TripPlanner.CompleteTripAsync` and clear them in `ReopenTripAsync`, as `ListContents.Freeze` / `Unfreeze` do for Packing List weights.
- From ticket 16: add calories per day and food cost to `TripStats` (`TripStats.For`, shown on the Stats tab's cards), and decide whether they're null like the route totals when the Meal Plan is empty.
- From ticket 19: freeze onto `SlotEntry` (add frozen weight, calories, protein and cost fields) and have `MealPlanView.For` prefer them, like `ListContents.View` prefers `FrozenWeightGrams`. `MealPlanView.Calories` already totals the plan's calories for calories per day; `MealTotals` has protein and cost per Meal.
- Implemented: `SlotEntry.Frozen` (`FoodFigures`: one Meal's or serving's weight, calories, protein and cost), set by `CompleteTripAsync` from `MealPlanView.CurrentFigures` (a Meal through its `MealTotals`) and cleared by `ReopenTripAsync`. `MealPlanView.For` prefers frozen figures; an entry whose Meal or Food Item was already gone at completion stays unfrozen and counts for nothing, while a frozen one keeps counting if it's deleted later. `SlotEntryView` now carries protein and cost; `MealPlanView.Cost` sums the known costs.
- Stats: `TripStats.CaloriesPerDay` (Meal Plan calories over every Trip Day, so a partial day lowers the average) and `TripStats.FoodCost`, both null (shown as a dash) when the Meal Plan is empty or nothing has a cost. The Stats tab has a Food card. Verified with headless Edge at phone width: stats before and after planning, unchanged on a Completed Trip after the Food Item was edited, and current again after Reopen.
