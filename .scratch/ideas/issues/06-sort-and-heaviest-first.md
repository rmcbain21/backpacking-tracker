# 06: Sort lists, and a heaviest-first weight view

**Status:** ready-for-agent

**Idea:** Sort options for gear, such as by name or by weight. On a Gear List or Packing List, also a "pure weight" view: every List Entry in one flat list, ignoring Categories, sorted heaviest first, so I can see straight away which items cost the most weight.

**Open questions:**
- Where do sort options apply: the Gear Items page, Gear Lists, Packing Lists, or all three?
- Does sorting by weight use one item's weight or the List Entry's total (weight × quantity)? Total seems more useful for "what's heaviest".
- Should the flat view show each entry's share of Base Weight (e.g. "Tent, 12%")?
- Worn and consumable entries: include them, filter by Carry Mode, or show base entries only by default?
- Is the chosen sort remembered between visits?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Sort by name or by weight on the Gear Items page, Gear Lists and Packing Lists. On lists, "weight" means the entry's total (weight × quantity), so 8 × 3 oz stakes (1.5 lb) count as one 1.5 lb entry.
- On lists, sorting happens inside each Category. The default is the hand-made order (05); a sort is a view and never changes the stored order.
- A "heaviest first" view on Gear Lists and Packing Lists: every entry in one flat list, ignoring Categories, by total weight, each with its share of Base Weight (e.g. "Tent, 24%").
- The flat view shows base entries by default, with a filter to include worn and consumable entries (those show no % of Base Weight).
- The chosen sort/view is remembered per page on this device only (a per-viewer convenience, not a synced Setting).
