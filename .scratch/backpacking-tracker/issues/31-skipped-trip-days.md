# 31: Skipped Trip Days

**What to build:** Once a Trip is under way, I can mark a Trip Day as Skipped instead of changing the Trip's dates, so the record keeps what I planned as well as what happened. For example, if I arrive a day late I mark Day 1 Skipped and note why; the Trip still runs Jul 1–4 and Day 1's plan is kept. Changing the dates (ticket 13) stays the way to change the plan before the trip.

**Blocked by:** 13 (Trip Days, Itinerary and date changes), 16 (Trip stats), 19 (Meal Plan on Trip Days)

**Status:** needs-triage

- [ ] A Trip Day can be marked Skipped, and un-marked. It keeps all its route details, notes and meals.
- [ ] Skipped is offered only from the Trip's start date onwards (based on the date, not a new Trip Status).
- [ ] The Itinerary shows Skipped days clearly, still in order.
- [ ] Stats show what was hiked: days, nights, actual distance and elevation gain leave out Skipped days. Planned distance still includes them, so planned and actual can be compared.
- [ ] A Skipped day's food still counts in Consumable Weight and Starting Pack Weight, since it was still carried.
- [ ] Skipped can still be set and cleared on a Completed Trip, like the other route fields (ticket 15).
- [ ] Core tests cover marking and un-marking, the date rule, stats and food weight.

## Comments

- Decided with the backpacker: the status is just "Skipped" (a hiked day needs no mark); stats leave Skipped days out except planned distance; their food still counts; it's offered from the start date. Add **Skipped** to `GLOSSARY.md` under Trips when this is built.
- Open: whether "days and nights" in the stats should show the planned count too, for example "3 days hiked of 4 planned".
