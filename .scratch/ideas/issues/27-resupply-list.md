# 27: Resupply and Food Carries

**Status:** ready-for-agent

**Idea:** Total up the Food Items in a Meal Plan into a shopping list, so I know what to buy before the Trip or at a resupply stop.

**Open questions:**
- Per whole Trip, or per stretch of Trip Days between resupplies on a thru hike? How is a resupply point marked?
- Does it round up to whole packages using servings per package (18)?
- Can items be ticked off as bought?
- Lives on the Trip page, the On Trail view (28), or both?
- For thru hikes, waits on Open-ended Trips (01).

## Comments

**Note (2026-10-27):** 28 was triaged as a **Today** tab on the Trip page (shown while On Trail). When this is grilled, it adds its own section to that tab rather than 28 waiting on it.

**Note (2026-10-27), from grilling 26:** marking food eaten (26) deliberately leaves pack weight alone. Once resupply points exist, consider a **food weight carried** figure for a thru hike: it falls each day as food is eaten and jumps back up at each resupply (start with 4 days of food, drop to almost nothing, resupply, back up). Related: food bought and eaten on trail (a town burger) currently counts in Consumable Weight and Starting Pack Weight like all Meal Plan food; resupplies may need a way to mark food as not carried from the start.

**Triage (2026-10-27):** ready-for-agent, after a grilling session. Retitled from "Grocery / resupply list". New glossary terms: **Resupply** and **Food Carry**; **Consumable Weight** reworded. Changes a weight rule: see ADR 0007. Spec updated (story 74, new stories 96–98, Trip and Trip Day data model, Weight rule, out of scope). Doesn't wait on 01, 18, 26 or 28: each tie-in below is added by whichever lands second.

*Resupply and Food Carries*
- A **Resupply** switch on a Trip Day (in the Trip Day dialog), meaning "food is restocked before this day's hiking", with an optional place name (shown as the day's start when blank). Hidden on day 1.
- The Trip Days split into **Food Carries**: from the start (bought "from home") up to the day before the first Resupply, then from each Resupply to the day before the next. No Resupply days: one Food Carry for the whole Trip.
- The switch belongs to the Trip Day: it moves with its day on date changes; a removed day takes it along, and the date-change preview counts a Resupply as content. 01's "copy meals from the previous day" doesn't copy it.

*Starting Pack Weight (ADR 0007)*
- Consumable Weight, and so Starting Pack Weight, counts only the **first Food Carry's** Meal Plan food. The Category breakdown's Meal Plan food line follows. A Trip with no Resupply is unchanged.
- This also settles 26's town-food question: food in later Food Carries was never counted as carried from the start.
- Food only: fuel and other consumable Packing List entries still count in full (out of scope).

*Shopping list*
- On the **Meals tab**, a **By day | Shopping** toggle at the top. Shopping shows one heading per Food Carry, e.g. "From home · days 1–4", "Kennedy Meadows · days 5–9 · 6.8 lb food · 3 of 12 bought". Food Carries that are over are collapsed.
- Under each heading, one line per Food Item: slot entries totalled across the Food Carry's days, Meals broken into their Food Items, servings × quantity summed. Each line shows servings and weight; the heading shows the Food Carry's total weight and calories. Skipped days' food counts.
- With servings per package (18): "3 servings → 2 packages", rounded up. Servings only otherwise.
- **Bought** ticks per line per Food Carry, with a **Reset**; stored on the Trip so they sync. A line's quantity going up unticks it; anything else keeps the tick; a line that disappears loses it.

*Food weight*
- Each Food Carry heading shows its food weight (what's carried out of that stop).
- **Today tab (28)**: "Food left: 3.2 lb · 2 days to Kennedy Meadows": the food on today and the rest of the current Food Carry's days, less slot entries ticked as eaten (26) when present.
- Today tab also gets one line: "Next resupply: Kennedy Meadows, day 5 (in 2 days)" or, on the day, "Resupply today: Kennedy Meadows · 3 of 12 bought", opening Meals → Shopping at that Food Carry. Left out when there's no later Resupply.

*Completed Trips*
- Resupply switches and bought ticks lock with the Meal Plan; Food Carry totals use frozen weights and calories. Reopen unlocks.

*Elsewhere*
- Itinerary text (02): a Resupply day's line adds "Resupply: Kennedy Meadows".
- Stats tab: "3 resupplies · longest Food Carry 6 days (8.1 lb)".
- A chart of food weight over the Trip could come later on the Stats tab; not now.

**Note (2026-10-27), from grilling 29:** "fuel and other consumable Packing List entries are out of scope" above means the shopping list stays food only. Carrying more or less fuel after a Resupply is now covered by a **Gear Change** (idea 29), e.g. Fuel canister ×1 → ×2, which counts in that Food Carry's pack weight but not in Starting Pack Weight.
