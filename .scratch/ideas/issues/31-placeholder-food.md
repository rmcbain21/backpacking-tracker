# 31: Placeholder food for a town Resupply

**Status:** needs-triage

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
