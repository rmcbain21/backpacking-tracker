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
