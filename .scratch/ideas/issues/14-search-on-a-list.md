# 14: Search within a Gear List

**Status:** ready-for-agent

**Idea:** A search box on a Gear List (and Packing List) to find an entry by name or Gear Type, for long lists.

**Open questions:**
- Filter the list as I type, or highlight and scroll to matches?
- Search Tags too?
- Same search on the "add gear to list" picker, if it doesn't have one already?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- A search box on both Gear Lists and Packing Lists.
- Filters the list as you type. Categories with no matching entries are hidden; the weight totals and breakdown still show the whole list.
- Matches on Gear Item name, Gear Type and Tags, the same way the "add gear" picker's search does (`AddGearDialog.razor` already has one). Share the matching code rather than writing it twice.
- Clearing the box shows the full list again. The search isn't remembered between visits.
