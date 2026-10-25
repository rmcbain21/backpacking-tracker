# 31: Skipped Trip Days

**What to build:** Once a Trip is under way, I can mark a Trip Day as Skipped instead of changing the Trip's dates, so the record keeps what I planned as well as what happened. For example, if I arrive a day late I mark Day 1 Skipped and note why; the Trip still runs Jul 1–4 and Day 1's plan is kept. Changing the dates (ticket 13) stays the way to change the plan before the trip.

**Blocked by:** 13 (Trip Days, Itinerary and date changes), 16 (Trip stats), 19 (Meal Plan on Trip Days)

**Status:** ready-for-agent

- [x] A Trip Day can be marked Skipped, and un-marked. It keeps all its route details, notes and meals.
- [x] Skipped is offered only from the Trip's start date onwards (based on the date, not a new Trip Status).
- [x] The Itinerary shows Skipped days clearly, still in order.
- [x] Stats show what was hiked: days, nights, actual distance and elevation gain leave out Skipped days. Planned distance still includes them, so planned and actual can be compared.
- [x] A Skipped day's food still counts in Consumable Weight and Starting Pack Weight, since it was still carried.
- [x] Skipped can still be set and cleared on a Completed Trip, like the other route fields (ticket 15).
- [x] When any day is Skipped, days and nights show the hiked count and the planned count ("3 days, 2 nights hiked (4 planned)"); hiked nights are hiked days minus one, never below zero.
- [x] Calories per day still divides by every Trip Day, Skipped or not.
- [x] A Sync Conflict on a Trip shows a "Day N skipped" line (ticket 25's `ConflictDescriber`).
- [x] Core tests cover marking and un-marking, the date rule, stats (including the planned count and calories per day) and food weight.

## Comments

- Decided with the backpacker: the status is just "Skipped" (a hiked day needs no mark); stats leave Skipped days out except planned distance; their food still counts; it's offered from the start date. Add **Skipped** to `GLOSSARY.md` under Trips when this is built.
- Decided with the backpacker (2026-10-25): when any day is Skipped, days and nights show both, such as "3 days, 2 nights hiked (4 planned)"; with none Skipped they show as now. Calories per day still divides by all planned Trip Days, so it keeps meaning how well each day's food was planned and doesn't jump when a day is Skipped.
- From ticket 16: `TripStats.For` counts every Trip Day for Days/Nights and sums every day's distances; leave out Skipped days there if the spec says so.
- Implemented: `TripDay.Skipped` (left out of the JSON when false, so existing records and sync files don't change). `TripPlanner.SetDaySkippedAsync(tripId, date, skipped)` is separate from `UpdateDayAsync`, so editing a day's route never clears it; marking is refused before the start date ("Until then, change its dates instead"), un-marking is always allowed, and both work on a Completed Trip. A Skipped day counts as content (`HasContent`), so a date change that would remove it asks first. "Today" comes from the new `IClock.Today` (local date; Trip dates are local calendar days), and `TripView.HasStarted` tells the UI whether to offer it.
- `TripStats`: `Days`/`Nights` are hiked (Skipped left out), new `PlannedDays`/`PlannedNights` count every day, `AnySkipped` when they differ. Actual distance and elevation gain leave Skipped days out; planned distance, food weight and calories per day (still over every day) don't.
- Web: the day dialog's "Skipped: planned but not hiked" switch, shown once the Trip has started (or on a day already Skipped). Itinerary: a Skipped chip, the day's details faded, still in order. Stats: "3 days · 2 nights hiked" with "(4 planned)" under it. `ConflictDescriber` adds a "Day N skipped" line. `GLOSSARY.md` has **Skipped**. Checked with headless Edge at phone width on an Oct 24–27 Trip (today Oct 25): mark Day 1 Skipped with a note, then the Itinerary, stats (planned 16.0 mi, actual 7.0 mi) and un-marking.
