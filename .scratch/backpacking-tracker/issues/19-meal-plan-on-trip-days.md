# 19: Meal Plan on Trip Days

**What to build:** Each Trip Day has Breakfast, Lunch, Dinner and Snacks slots where I put Meals or individual Food Items with a quantity. I can see the Trip's Meal Plan with calories and food weight per day, and the food counts in the Trip's Consumable Weight and Starting Pack Weight.

**Blocked by:** 13 (Trip Days, Itinerary and date changes), 18 (Meals)

**Status:** ready-for-agent

- [x] Every Trip Day has four slots; each holds zero or more slot entries, each a Meal or a Food Item with a quantity.
- [x] Empty slots are allowed (partial first and last days).
- [x] The Meal Plan view shows all Trip Days' slots with calories and food weight per day.
- [x] Meal Plan food weight is included in Consumable Weight and Starting Pack Weight and appears as its own line in the Category breakdown.
- [x] Planned Trips use current Food Item and Meal values; Meal Plan changes are refused on Completed Trips (consistent with ticket 15).
- [x] The Food Item and Meal hard-delete guards count slot entries as references.
- [x] Removing a Trip Day with meals counts as content in the date-change preview.
- [x] Core tests cover slot entries, per-day totals, food in Consumable Weight, delete guards and the date-change interaction.

## Comments

- From ticket 09: the spec says "Meal Plan food and water appear as their own breakdown lines". `WeightCalculator` builds `PackWeight.ByCategory` from List Entries only; add food or water as its own line (after the Categories), and include it in the Consumable Weight that the shares are taken from.
- From ticket 13: add the slots to `TripDay` and include them in `TripDay.HasContent()`, which the date-change preview already uses. `TripDayDetails` / `UpdateDayAsync` replace only route and notes, so slots need their own methods.
- From ticket 14: water goes in through `WeightCalculator.Calculate(..., waterGrams)` as a "Water" line (`Category.Water`); add food the same way, its line before Water.
- From ticket 15: on a Completed Trip, changing the Meal Plan (adding, changing or removing slot entries) is locked; load the Trip with `GetPlannedTripAsync(tripId, "its Meal Plan")` as the Packing List methods do.
- From ticket 18: register Trips with `Meals.AddReferences(...)` (`IMealReferences`) and `Food.AddReferences(...)` (`IFoodItemReferences`) in AppCore so slot entries block deletes. A slot entry's Meal weight and calories come from `MealCatalogue.GetMealAsync(...)` / `MealView.Totals`; a Food Item entry's from its serving values. `ServingsInput` reads fractional quantities.
- Implemented: `TripDay.SlotEntries` (`SlotEntry`: Slot, MealId or FoodItemId, Quantity), included in `HasContent()`, so the date-change preview counts days with meals. `TripDay` now has value equality that compares slot entries one by one. `TripPlanner` gains `AddMealToSlotAsync` / `AddFoodItemToSlotAsync` (adding the same thing to a slot again adds to its quantity; Retired refused), `UpdateSlotEntryAsync` and `RemoveSlotEntryAsync`, all locked on a Completed Trip via `GetPlannedTripAsync(..., "its Meal Plan")`. `TripView.MealPlan` (`MealPlanView`) gives each day's four slots with entry weight and calories (Meal totals or Food Item servings × quantity) and per-day and Trip totals. Food weight goes through `WeightCalculator.Calculate(..., foodGrams)` as a "Food" line (`Category.Food`) before Water. `TripPlanner` registers as `IFoodItemReferences` and `IMealReferences` (explicit implementations, since `IGearItemReferences` has the same method).
- Web: a Meals tab on the Trip page (between Itinerary and Stats) with the Trip's food total, each day's calories and food weight, and the four slots with + (`AddToSlotDialog`: search Meals and Food Items, tap to add, "Added ×n") and tap-to-change entries (`SlotEntryDialog`: quantity or Remove). Read-only on a Completed Trip. Verified with headless Edge at phone width, including the five tabs fitting, the Food breakdown line and the Completed lock.
