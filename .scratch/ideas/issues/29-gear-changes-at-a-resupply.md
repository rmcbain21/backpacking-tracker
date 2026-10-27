# 29: Gear changes at a Resupply

**Status:** ready-for-agent

**Idea:** On a long Trip, change gear partway through: pick up a bear canister for the Sierra, send the puffy home in Oregon. The natural place is a Resupply day (27): "at Kennedy Meadows: + bear canister, − puffy", so the pack weight for each Food Carry reflects the gear carried then.

**Open questions:**
- Is Starting Pack Weight still the trailhead figure, with later Food Carries showing their own pack weight?
- How is a change recorded: adds and removes on a Resupply day, or a separate Packing List per Food Carry?
- How does the packing checklist work for gear picked up mid-Trip?
- From 01: should an Open-ended Trip's weights stop following the current Gear Items once it's under way? Today a Planned Trip uses current weights, so correcting a weight in July changes April's Starting Pack Weight.
- Split out of 24 (Hiking Partners) on 2026-10-27; builds on 27 (Resupply and Food Carries).

## Comments

**Triage (2026-10-27):** ready-for-agent, after a grilling session. **Builds on 27** (Gear Changes need Resupply days). New glossary term: **Gear Change**. No new ADR: Starting Pack Weight keeps its meaning; ADR 0007's Consequences now point here. Spec updated (story 103, Trip Day data model, out of scope wording).

*What a Gear Change is*
- Stored on a **Resupply** day (27) only; there's no gear-only stop. A stop where only gear changes is still marked as a Resupply, starting a Food Carry with little to buy; accepted.
- Three kinds: **add** a Gear Item (quantity, Carry Mode, Category from the Packing List's Categories), **remove** an entry, **change quantity** (e.g. Fuel canister ×1 → ×2).
- The Packing List stays the trailhead list. The gear carried in a Food Carry is the Packing List plus every Gear Change up to and including that Food Carry's Resupply.
- Backpacker's own pack only; not on Hiking Partners' Packing Lists (24).

*Weights*
- Starting Pack Weight is unchanged: the trailhead figure.
- Each Food Carry heading on Meals → Shopping (27) adds its pack weight out: that Food Carry's gear (Base + consumable entries) plus its food plus water carried, e.g. "Kennedy Meadows · 5 days · 6.8 lb food · 24.9 lb pack".
- Today tab (28): the current Food Carry's Base Weight, next to "Food left".
- Stats tab: "Heaviest pack: 27.1 lb out of Kennedy Meadows".
- Open-ended Trips keep following current Gear Item weights (01's question, settled: no change). A correction applies everywhere; replacing gear is a different Gear Item, recorded as a Gear Change (− old tent, + new tent).

*Editing and showing*
- A Resupply day's Trip Day dialog gets a **Gear changes** section: Add (Gear Item picker, non-Retired), Remove or Change quantity (picking from the gear carried at that point).
- The Packing List tab shows the trailhead list, then one summary block per Resupply with changes: "At Kennedy Meadows (day 5): + Bear canister · − Puffy". Tapping opens that day's dialog.
- Checklist tab: a group per Resupply with changes, "Kennedy Meadows: pick up / send home", each change with a done tick.
- Today tab's "Resupply today" line (27) adds the gear count: "3 of 12 food bought · 0 of 2 gear changes done".

*When things stop making sense*
- A Gear Change that no longer applies (e.g. "− Puffy" after the puffy was taken off the Packing List) is kept but flagged ("Puffy isn't carried by day 15") and ignored in weights until fixed or deleted.
- Turning off a Resupply that has Gear Changes warns, then removes them. A date change that removes the day counts them as content in its preview.

*Elsewhere*
- Completed: Gear Changes lock and added items' weights freeze; Reopen unlocks.
- Refresh from Gear List (15) and "Save as Gear List" work on the trailhead Packing List only.
- Trip Review (23): gear added by a Gear Change appears in "Gear used".
- Fuel: a Gear Change can change how much is carried, but the shopping list stays food only.
