# 16: A home page

**Status:** ready-for-agent

**Idea:** A start page with a quick link to the Trip I'm on or about to go on, and quick actions for it (Packing List, today's Trip Day, Meal Plan). Trips grouped as Planning → Current → Completed.

**Open questions:**
- Trip Status today is only Planned or Completed. "Current" could be worked out from dates (today falls between start and end) rather than being a new status. The glossary avoids the word "Active".
- Which quick actions matter most on trail: today's Trip Day notes, the Packing List checklist, Starting Pack Weight?
- Also a place for recent Gear Lists or stats?
- Ties in with open-ended Trips (01), which have no end date.

## Comments

**Triage (2026-10-27):** ready-for-agent, after a grilling session. Scoped down to a featured Trip card, the doorway to being On Trail. Managing a Trip while on it (food eaten, resupply list, adding today's day) moved to 28. New glossary term: **On Trail**.

*Where it lives*
- No new page and no 5th nav item. The card goes at the top of the Trips page, and the app opens on Trips: Trips moves to `/`, Gear moves to `/gear` (update `NavItem.All` and any links to Gear). Keep the nav order as it is.

*On Trail*
- A Trip is On Trail when it is Planned and today falls between its start and end dates. An Open-ended Trip (01) is On Trail from its start date until it's Completed, since its end date is just the last day so far.
- Worked out from dates; Trip Status is unchanged (still Planned or Completed). Sits next to the existing `HasStarted` check in `TripPlanner`.

*Which Trip gets the card*
1. A stale Trip: Planned, but its end date has passed. Shown first, with "Finished? Mark Completed". If more than one, the one that ended earliest.
2. Otherwise the On Trail Trip. If two overlap, the one that started earliest.
3. Otherwise the next Planned Trip starting today or later.
4. Otherwise no card.

*What the card shows*
- Always: the Trip's name, dates (as `TripDates`) and Starting Pack Weight.
- On Trail: "On Trail · Day 3 of 5" (Open-ended: "On Trail · Day 40"). Quick actions: **Today: Day 3** (opens that Trip Day's dialog: route, actual distance, notes), **Meals** (scrolled to today), **Checklist**, **Stats**. If today has no Trip Day yet (an Open-ended Trip whose day isn't added), leave out the Today action; adding it belongs to 28.
- Upcoming: "Starts in 12 days" ("Starts tomorrow", "Starts today"). Quick actions: **Packing List**, **Checklist**, **Start now**.
- Stale: **Mark Completed** (the existing complete flow), then **Stats**.
- Quick actions link to a Trip page tab, so the Trip page reads its tab from the address, e.g. `trips/{id}?tab=checklist` (`packing`, `checklist`, `itinerary`, `meals`, `stats`). Default stays Packing List.

*Start now*
- On an upcoming Trip's card, moves its start date to today and keeps its length (7–9 Nov becomes 6–8 Nov), going through the existing date-change preview and warnings (`DateChangePlan`). On an Open-ended Trip it moves only the start date. Not a new Trip Status.

*Trips list groups*
- **On Trail → Upcoming → Past** (was Upcoming / Past). Stale Trips stay under Upcoming. Each group is left out when empty, as now. The featured Trip is still listed in its group.

*Out of scope*
- Recent Gear Lists or overall stats on the start page.
- A Start button that sets a new "Started" Trip Status: dates plus Start now cover leaving early. Revisit only if a real need turns up.
- The On Trail view itself (28), food eaten (26), resupply list (27).
