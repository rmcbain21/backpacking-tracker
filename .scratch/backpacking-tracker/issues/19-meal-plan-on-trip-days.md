# 19: Meal Plan on Trip Days

**What to build:** Each Trip Day has Breakfast, Lunch, Dinner and Snacks slots where I put Meals or individual Food Items with a quantity. I can see the Trip's Meal Plan with calories and food weight per day, and the food counts in the Trip's Consumable Weight and Starting Pack Weight.

**Blocked by:** 13 (Trip Days, Itinerary and date changes), 18 (Meals)

**Status:** ready-for-agent

- [ ] Every Trip Day has four slots; each holds zero or more slot entries, each a Meal or a Food Item with a quantity.
- [ ] Empty slots are allowed (partial first and last days).
- [ ] The Meal Plan view shows all Trip Days' slots with calories and food weight per day.
- [ ] Meal Plan food weight is included in Consumable Weight and Starting Pack Weight and appears as its own line in the Category breakdown.
- [ ] Planned Trips use current Food Item and Meal values; Meal Plan changes are refused on Completed Trips (consistent with ticket 15).
- [ ] The Food Item and Meal hard-delete guards count slot entries as references.
- [ ] Removing a Trip Day with meals counts as content in the date-change preview.
- [ ] Core tests cover slot entries, per-day totals, food in Consumable Weight, delete guards and the date-change interaction.

## Comments

- From ticket 09: the spec says "Meal Plan food and water appear as their own breakdown lines". `WeightCalculator` builds `PackWeight.ByCategory` from List Entries only; add food or water as its own line (after the Categories), and include it in the Consumable Weight that the shares are taken from.
- From ticket 13: add the slots to `TripDay` and include them in `TripDay.HasContent()`, which the date-change preview already uses. `TripDayDetails` / `UpdateDayAsync` replace only route and notes, so slots need their own methods.
- From ticket 14: water goes in through `WeightCalculator.Calculate(..., waterGrams)` as a "Water" line (`Category.Water`); add food the same way, its line before Water.
- From ticket 15: on a Completed Trip, changing the Meal Plan (adding, changing or removing slot entries) is locked; load the Trip with `GetPlannedTripAsync(tripId, "its Meal Plan")` as the Packing List methods do.
- From ticket 18: register Trips with `Meals.AddReferences(...)` (`IMealReferences`) and `Food.AddReferences(...)` (`IFoodItemReferences`) in AppCore so slot entries block deletes. A slot entry's Meal weight and calories come from `MealCatalogue.GetMealAsync(...)` / `MealView.Totals`; a Food Item entry's from its serving values. `ServingsInput` reads fractional quantities.
