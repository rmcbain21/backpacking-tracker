# 20: Freezing food and food stats

**What to build:** Completing a Trip also freezes the weight, calories, protein and cost of everything in its Meal Plan, so editing a Food Item or Meal later never changes a past Trip. Trip stats gain calories per day and total food cost.

**Blocked by:** 16 (Trip stats), 19 (Meal Plan on Trip Days)

**Status:** ready-for-agent

- [ ] Completing a Trip copies each slot entry's current weight, calories, protein and cost (resolving Meals through their components) onto the slot entry.
- [ ] A Completed Trip's food figures use frozen values; Reopen clears them.
- [ ] Stats include calories per day (total Meal Plan calories divided by number of Trip Days) and total food cost (sum over slot entries with a cost).
- [ ] Editing a Food Item or Meal after completion changes Planned Trips but not Completed ones.
- [ ] Core tests cover nutrition freezing, Reopen and both food stats.

## Comments
- From ticket 15: freeze the slot entries in `TripPlanner.CompleteTripAsync` and clear them in `ReopenTripAsync`, as `ListContents.Freeze` / `Unfreeze` do for Packing List weights.
- From ticket 16: add calories per day and food cost to `TripStats` (`TripStats.For`, shown on the Stats tab's cards), and decide whether they're null like the route totals when the Meal Plan is empty.
