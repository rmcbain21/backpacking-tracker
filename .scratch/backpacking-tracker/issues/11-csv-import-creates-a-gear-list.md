# 11: CSV import creates a Gear List

**What to build:** When importing gear from CSV, I can choose to also create a named Gear List from the imported rows, so my first trip sheet becomes both my gear catalogue and my first Gear List in one step.

**Blocked by:** 07 (CSV gear import), 08 (Gear Lists and weight totals)

**Status:** ready-for-agent

- [ ] The import offers an optional Gear List name; when given, a Gear List is created with one List Entry per imported (non-rejected) row.
- [ ] Each List Entry uses the row's Gear Item (created or reused), Qty (default 1) and Carry Mode (default base).
- [ ] The import summary names the created Gear List.
- [ ] Core tests cover Gear List creation from rows, including reused Gear Items and defaults.
