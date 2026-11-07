# 29: Gear changes at a Resupply

**Status:** done

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

*Implemented (2026-11-06)*
- Core: `GearChange` on `TripDay.GearChanges` (kind Add / Remove / ChangeQuantity, Gear Item, quantity, Carry Mode and Category for an Add, done tick, frozen weight for an Add once Completed). **One change per Gear Item at a Resupply**: picking up the same item again adds to its quantity (and unticks it); a send-home replaces a quantity change of the same item and vice versa; mixing an Add with either is refused. Only gear carried just before the Resupply can be sent home or changed, and a ChangeQuantity to the same number is refused. `TripPlanner.AddGearPickUpAsync`, `SendGearHomeAsync`, `ChangeGearQuantityAsync`, `UpdateGearChangeAsync` (a different quantity unticks), `RemoveGearChangeAsync`, `SetGearChangeDoneAsync`; all refused off a Resupply day and on a Completed Trip.
- `CarriedGear` on each `FoodCarry` (`FoodCarry.Gear`): the Packing List with each Resupply's changes folded in, in order. An Add of gear already carried adds to it; a Remove or ChangeQuantity of gear no longer carried keeps a `Problem` ("Puffy isn't carried by day 15") and is skipped in weights. `FoodCarry.PackOutGrams` = base + consumable gear + food + water carried (the first Food Carry's equals Starting Pack Weight, which is unchanged). `MealPlanView.GearBefore(date)` gives the gear to pick from. `TripStats.HeaviestPack` (null without a Resupply).
- Hooks: Mark Completed freezes picked-up gear's weight, Reopen clears it; turning off a Resupply drops its Gear Changes (the dialog warns first); Gear Item deletion counts Gear Changes as uses; Merge (30) repoints them (keeping the first if two land on one Gear Item at a Resupply); the sync conflict view has a "Day N gear changes" line; `TripDay.HasContent` counts them.
- Web: the Trip Day dialog on a saved Resupply day has a **Gear changes** section (`GearChangesEditor`, saving as you go): Pick up gear (the gear picker, into a Packing List Category; Carry Mode and −/+ on the row), Send home or change… (pick from gear carried by then, then "Send … home" or "Carry ×n"), delete, and flagged problems. Packing List tab: one block per Resupply with changes, "At Kennedy Meadows (day 4) · Base 4 lb 3.0 oz · + Bear canister · − Puffy", opening that day. Checklist tab: "Kennedy Meadows: pick up / send home · 1 of 2 done" with tap-to-tick rows ("Pick up Bear canister", "Send home Puffy", "Fuel canister ×1 → ×2"). Today tab: "Base Weight: …" under Food left, and "Resupply today: … · 0 of 1 food bought · 1 of 2 gear changes done". Shopping headings add "… pack". Stats: "Heaviest pack: 8 lb 8.6 oz out of Lone Pine".
- Not done here: Trip Review (23) and Refresh from Gear List (15) aren't built yet; whichever lands second ties in ("Gear used" includes picked-up gear; refresh works on the trailhead list only). Hiking Partners (24) don't get Gear Changes.
- Tests: `GearChangeTests` (14). Checked in headless Chromium at 390 px with a seeded 7-day Trip, two Resupplies and changes: Today lines, Packing List summary, checklist ticks, Send home or change → Carry ×2 and Pick up through the dialog, Shopping pack weights, Stats heaviest pack; no console errors.
