# 26: Mark food eaten on a Trip Day

**Status:** done

**Idea:** While On Trail, tick off the Meals and Food Items in a Trip Day's slots as I eat them, to see what's left and whether I'm eating to plan.

**Open questions:**
- Does ticking food change a "now" pack weight (Starting Pack Weight minus what's eaten), or only show progress?
- What about food eaten that wasn't in the Meal Plan, or planned food that wasn't eaten?
- Does it feed Trip stats (calories actually eaten per day vs planned)?
- Is it frozen with the rest when the Trip is Completed?
- Part of the On Trail view (28); for thru hikes, waits on Open-ended Trips (01).

## Comments

**Note (2026-10-27):** 28 was triaged as a **Today** tab on the Trip page (shown while On Trail). When this is grilled, it adds its own section to that tab rather than 28 waiting on it.

**Triage (2026-10-27):** ready-for-agent, after a grilling session. Spec's out-of-scope line reworded: marking food eaten is now in scope; day-by-day pack weight as food is eaten stays out (see the note on 27). No glossary term or ADR.

*What a tick is*
- One **eaten** tick per slot entry, covering its whole quantity. No partial ticks, no ticking Food Items inside a Meal. Ate one of two bars: change the quantity to 1, then tick.
- Unticked means not eaten; no third state. Stored on the slot entry inside the Trip, so it syncs with it.

*When*
- Once the Trip has started (`HasStarted`: today on or after the start date), on any Trip Day, so past days can be caught up. Skipped days (31) can be ticked like any other.
- Locked with the Meal Plan when the Trip is Completed; Reopen unlocks and keeps the ticks.
- Copying meals never copies ticks: 01's "copy meals from the previous day" and ticket 21's Meal Plan copy both bring entries unticked.

*Where*
- **Meals tab**: a tick on each slot entry once the Trip has started, tap again to untick. Each day shows "Eaten 1,850 of 2,900 cal" and has an **All eaten** action that ticks every entry on that day.
- **Today tab** (28): the meals section gains the ticks and the same "Eaten … of … cal" line in its header; otherwise still read-only and still opens the Meals tab when tapped. 26 doesn't wait for 28: whichever lands second adds the ticks to the Today tab.
- **Stats tab**: "Eaten 52,000 of 58,000 cal planned (90%) · 2,600 cal/day eaten". While Planned it counts days up to and including today; once Completed, every day. Uses frozen calories on a Completed Trip.

*Pack weight: progress only*
- Ticking food never changes Starting Pack Weight, Consumable Weight or any other weight. A "food left" weight only makes sense with resupplies (it falls as food is eaten and jumps back up at each resupply), so it belongs to 27.
- Unplanned food (a town burger) is added as a normal slot entry and ticked, so it counts in Consumable Weight like all Meal Plan food. Accepted until 27 models resupplies.

*Calories only*
- Only calories are tracked as eaten. Protein and Custom Fields (12) stay plan-only: an exception to 12's "mirror protein everywhere".

*Implemented (2026-11-04)*
- Core: `SlotEntry.Eaten` (left out of the JSON when false). `TripPlanner.SetSlotEntryEatenAsync` ticks or unticks one entry and `SetDayEatenAsync` ticks a whole day; both refused before `HasStarted` and on a Completed Trip. Reopen keeps the ticks. Both meal copies (01's Add day and 21's Meal Plan copy) go through one `CopyOf`, which drops the tick along with the frozen figures.
- Decided while building: adding more of something already in a slot merges into that entry, as before, but **unticks** it, since the added food isn't eaten yet and a tick covers the whole quantity. Changing an entry's quantity keeps its tick (the "change to 1, then tick" case).
- `DayMealPlan.EatenCalories` / `AllEaten`; `TripStats.Eaten` (`FoodEaten`: eaten and planned calories, days counted, percent, cal/day eaten). Days up to and including today while Planned, every day once Completed (frozen calories), Skipped days included. Null until anything is ticked, so old Trips don't show "Eaten 0 of …".
- Trip page: Meals tab shows a tick on each entry once started (tapping the tick doesn't open the entry dialog; a plain icon on a Completed Trip), plus "Eaten 1,850 of 2,900 cal" and **All eaten** under each day's heading. Ticked entries fade. The Today tab's meals now list one entry per line with its tick, and the header shows the eaten line; tapping elsewhere still opens the Meals tab. Stats tab: "Eaten … of … cal planned (90%) · 2,600 cal/day eaten", with a caption for which days count.
- Tests: `FoodEatenTests` (15). Checked in headless Chromium at phone width with a seeded 3-day Trip: tick on Today stays on Today and updates the header; All eaten on day 1; per-day lines; Stats line counts only days up to today; no console errors.
