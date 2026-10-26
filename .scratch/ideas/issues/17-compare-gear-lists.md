# 17: Compare two Gear Lists

**Status:** ready-for-agent

**Idea:** Pick two Gear Lists (or a Gear List and a Packing List) and see the weight differences: totals side by side, per-Category differences, and which entries are only on one list.

**Open questions:**
- Is the comparison per Category, per entry, or both?
- Compare one Trip's Packing List with another's, to see how my pack changed over time?
- Overlaps with quick swap (20), which is a one-item comparison.

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Pick any two lists to compare: Gear Lists or Trip Packing Lists, in any mix (e.g. Summer vs Winter, or last year's Trip vs this year's).
- Top: Base, Worn and Consumable totals side by side, with the difference.
- Then each Category's total on each side and the difference, with Categories matched by name (Categories belong to each list, ADR 0003). A Category on only one side shows as such.
- Each Category opens to show its entries matched by Gear Item: only on A, only on B, or on both with different quantity or Carry Mode. Entries that are the same on both are hidden by default.
- Read-only. Reached from a "Compare" action on a list page, choosing the other list.
