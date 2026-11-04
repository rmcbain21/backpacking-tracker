# 31: Placeholder food for a town Resupply

**Status:** ready-for-agent

**Idea:** Came up after 27 shipped (2026-11-04). The shopping list works when every Food Item is known ahead of time: food from home, or a mail drop. A town Resupply is often different. I'm going to a store and don't know what it has, only that I need "3 breakfasts and a couple of dinners". I'd like to enter placeholders instead of picking exact Food Items, perhaps on the Shopping view.

Example: a Resupply at Kennedy Meadows (day 6), with days 6–8 in that Food Carry.

```
Day 6 (Kennedy Meadows)   Breakfast: [any breakfast]  ~400 cal
                          Dinner:    [any dinner]     ~700 cal
Shopping · Kennedy Meadows:  3 breakfasts · 2 dinners · 2 lb 2 oz (estimated)
```

**Open questions:**
- **Where it's entered.** As a slot entry on a Trip Day with no Food Item yet (so it stays in the By day plan), or as a count on the Food Carry ("3 breakfasts"), tied to no day? Entering it from the Shopping view was the first thought.
- **What it weighs.** Food left, a Food Carry's food weight, and (in the first Food Carry) Starting Pack Weight all need a number. Is it a typed guess (calories and/or weight), a default per slot, or nothing?
- **After the store.** Do I swap a placeholder for what I actually bought, or leave it, so ticking it as eaten (26) still works? Does swapping matter for stats ("Eaten … of … cal")?
- **Only for Resupplies?** Or also for a Trip not yet started whose plan isn't filled in ("some lunch on day 3")?
- **On the shopping list.** How do placeholders show next to real Food Items: their own lines ("3 breakfasts"), grouped by slot? Can they be ticked as bought?
- **Interaction with 18 (servings per package).** Probably none, since a placeholder has no package, but check.
- Glossary: a new term (Placeholder? Flexible meal?) if it stays.

## Comments

**Note (2026-11-04):** related to how 27 builds the shopping list. Every line comes from slot entries on the Food Carry's days, so today there's no way to put anything on the list without assigning it to a day. A placeholder would be the first thing on the list without a Food Item behind it.

**Triage (2026-11-04):** ready-for-agent, after a grilling session. New glossary term: **Placeholder**. Spec updated (story 109, Trip Day data model, Weight rule). No ADR: no existing rule changes. Builds on 26 and 27 (both done).

Running example: a Resupply at **Kennedy Meadows on day 6**, days 6–8 in that Food Carry; breakfasts on days 6–8 and dinners on days 6–7 are Placeholders.

*What a Placeholder is*
- A **slot entry with no Meal or Food Item**, on any Trip Day (not only in Resupply Food Carries; it also covers an unfinished plan before the Trip). Added from a slot's + on the Meals tab, through a new **Placeholder** choice in the add dialog.
- It holds its slot (from where it's added), an optional **label** ("any dinner", "pizza in town"), an optional **estimated calories** and **estimated weight** (for one), and a quantity (default 1).
- A blank estimate counts as zero and is flagged wherever food is totalled, e.g. "2 lb 2 oz food (1 without a weight)", and likewise for a day's calories.
- Adding a Placeholder with the same slot and label to a day that already has one adds to its quantity (like adding the same Food Item twice).
- A default estimate per slot (in Settings) could come later if typing the same guesses gets tedious; not now.

*How it counts*
- Like any slot entry: the day's calories and food weight, the Food Carry's weight, food left (Today tab), Consumable Weight and Starting Pack Weight when in the first Food Carry, and the eaten stats ("Eaten … of … cal" uses the estimate, or zero).
- Ticked as eaten (26) like anything else. Copied, unticked, by 01's "copy meals from the previous day" and by 21's Meal Plan copy.
- Mark Completed and Reopen leave it as it is: its estimates are already fixed numbers, so there's nothing to freeze.

*Replacing it*
- A Placeholder's dialog (tap it on the Meals tab) gets **Replace with…**: the usual Meal / Food Item picker, and the chosen item takes its place in the same slot, keeping its quantity and eaten tick. "Snack ×2" → "Clif Bar ×2".
- If that slot already has the chosen item, the two merge into one entry; it stays ticked as eaten only if both were ticked (a swap, not adding food, so it doesn't follow the add-more-unticks rule).
- Leaving a Placeholder in place is always fine; nothing forces a Replace.

*On screen*
- Meals and Today tabs: shown by its label, or "Any breakfast" (from its slot) when unlabelled, in italics with a small ? icon. Figures read "~400 cal · ~4 oz".
- Shopping (27): under each Food Carry's Food Item lines, a **"To choose at the store"** subheading with one line per slot and label, quantities and estimates totalled: "Breakfast ×3 · ~12 oz", "Dinner ×1 · ~7 oz", "Dinner: pizza in town ×1". Each line can be ticked as bought, with the same rule as Food Items: the tick goes away if its count goes up.
- Sync conflict view: a Placeholder shows as its label, or "Any breakfast".

*Not tied in*
- 18 (servings per package): no tie-in. A Placeholder has no package; only Food Item lines round up.
