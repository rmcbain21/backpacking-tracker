# 28: On Trail view

**Status:** ready-for-agent

**Idea:** The app today is good for preparing for a Trip. This is a mode for actually being on it, especially a thru hike: check or update today's Trip Day, mark food eaten, view some stats, see what to buy at the next resupply. Came out of grilling 16, which built the featured Trip card as its doorway.

**Open questions:**
- A separate page, or a different set of tabs on the Trip page while the Trip is On Trail?
- What goes on it first? Candidates: today's Trip Day (route, actual distance, notes), today's meals with food eaten (26), progress and pace from Expected Distance (01), resupply list (27).
- "Add today (day 41)" on an Open-ended Trip, reusing 01's Add day; only when the next date is today.
- Once it exists, should the featured card's quick links (16) become a single "Open On Trail" button?
- Works offline on trail as everything else does (ADR 0001); anything extra needed?
- Waits on Open-ended Trips (01).

## Comments

**Triage (2026-10-27):** ready-for-agent, after a grilling session. **Build after 01 (Open-ended Trips) and 16 (featured Trip card)**: it uses 01's Add day, Expected Distance and finish estimate, and 16's On Trail rule, featured card and `?tab=` address. No new glossary term, ADR or spec change.

*Scope*
- 28 builds the view itself from what exists (plus 01 and 16). Food eaten (26) and the resupply list (27) stay separate ideas; each adds its own section to the Today tab once grilled.

*Where*
- A **Today** tab on the Trip page, first in the tab bar, shown only while the Trip is On Trail, and the tab the page opens on while On Trail (`?tab=today`; the default otherwise stays Packing List). Not a separate page.
- Every On Trail Trip gets it, not only Open-ended ones. Open-ended-only parts (adding days, finish estimate, "Finish the Trip") show only there.

*Contents, top to bottom*
1. **Header:** "Day 3 of 5" (Open-ended: "Day 40") with today's date. A Skipped day (ticket 31) shows a Skipped badge and the usual un-skip, and stays editable.
2. **Today's Trip Day:** start, end, campsite and planned distance shown; **actual distance** and **journal notes** editable inline. Inline fields save when you leave the field, with a brief "Saved"; distance typed in the chosen units (ADR 0002). An **Edit day** button opens the existing Trip Day dialog for everything else.
3. **Distance so far** (always): "52.3 mi hiked in 4 days · 13.1 mi/day", from actual distances; Skipped days not counted, zero days counted (as in 01). With an Expected Distance, underneath: "1,032 of 2,650 mi (39%) · 1,618 to go", plus 01's finish estimate on an Open-ended Trip.
4. **Today's meals:** read-only list of today's slots and the day's calories. Tapping opens the Meals tab scrolled to today.
5. **Tomorrow**, one line if there's a next day: "Tomorrow: Tuolumne → Glen Aulin, 12.4 mi". Tapping opens that Trip Day's dialog.

*Adding today (Open-ended Trips)*
- When today has no Trip Day yet, the tab shows one button in its place: "Add today (Day 41)" when the last day is yesterday, or "Add 3 days up to today" when it's further back. It adds every missing date in order (01's Add day, repeated), with 01's "copy meals from the previous day" option applied to each, then shows today.

*Finishing*
- On a fixed-date Trip's last day: "Last day: Mark Completed when you're off trail", using the existing complete flow.
- On an Open-ended Trip: a small "Finish the Trip" link at the bottom (the same Mark Completed).

*Change to the featured card (16)*
- Once this lands, the On Trail card's **Today** link opens the Today tab (`?tab=today`) instead of the Trip Day dialog, and shows even when today has no Trip Day yet (the tab is where it's added). Meals, Checklist and Stats links stay. 16 is left as written so it can be built first.
