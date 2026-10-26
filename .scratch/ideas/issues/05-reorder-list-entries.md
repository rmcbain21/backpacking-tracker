# 05: Reorder List Entries by hand

**Status:** ready-for-agent

**Idea:** On a Gear List, drag List Entries up and down to put them in my own order inside a Category (e.g. tent, then stakes, then footprint).

**Open questions:**
- Does the order apply only within a Category, or can I also reorder the Categories themselves?
- Is the order copied onto a Trip's Packing List when the Trip is created, and can the Packing List be reordered separately?
- Drag handles on a phone can fight with scrolling. Up/down buttons, or a "reorder mode" that shows handles only while it's on?
- How does a hand-made order sit alongside sort options (06)? Probably: the hand order is the default, and sorting is a temporary view.

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Reorder List Entries within a Category, on both Gear Lists and Packing Lists. Categories can already be reordered (`GearLists.MoveCategoryAsync`); this adds the same for entries.
- When a Trip is created, the entries' order is copied with them, like Categories. After that each list has its own order.
- On the page: a "Reorder" button turns on a reorder mode that shows up/down arrows on each entry, so dragging doesn't fight with scrolling on a phone. Turning it off hides the arrows.
- Moving an entry to another Category puts it at the bottom of that Category.
- This hand-made order is the default view. Sorting (06) is a view on top of it and never changes the stored order.
- Stored order needs to survive sync, including two devices reordering before syncing (as with `DefaultCategory.SortOrder`).
