# 21: Copy a Meal Plan

**What to build:** When planning a Trip, I can copy the Meal Plan from an earlier Trip, so I can reuse a plan that worked without a separate template.

**Blocked by:** 19 (Meal Plan on Trip Days)

**Status:** ready-for-agent

- [ ] I can pick a source Trip; slot contents are copied by day position (Day 1 to Day 1), up to the shorter Trip's length.
- [ ] If any target slot already has content, I'm asked to confirm before it is replaced.
- [ ] Copying is refused when the target Trip is Completed.
- [ ] Copied slot entries reference the same Meals and Food Items (not frozen values from a Completed source).
- [ ] Core tests cover day mapping, length mismatch in both directions, the confirmation and the Completed refusal.
- From ticket 19: a day's Meal Plan is `TripDay.SlotEntries`; copying means giving each copied entry a new `Id` and saving through the Planned-only path (`GetPlannedTripAsync(..., "its Meal Plan")`). `HasContent`-style check for "target slot already has content": `day.SlotEntries.Any(e => e.Slot == slot)`.
- From ticket 20: copied slot entries must drop `Frozen` (set it to null), so a copy from a Completed Trip references the Meals and Food Items rather than their frozen figures.
