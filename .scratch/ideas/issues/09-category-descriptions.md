# 09: Descriptions under Category titles

**Status:** ready-for-agent

**Idea:** An optional short description under a Category's title on a Gear List, to say what belongs there.

Example:

> **Big 3 + Pad**
> Tent, sleeping bag, backpack

**Open questions:**
- Is the description stored per list like the Category name, and copied onto the Packing List when a Trip is created?
- Do Default Categories carry a description too, so new lists get it?
- Is it text I type, or could it be generated from what's in the Category (e.g. its Gear Types)?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- An optional free-text description on a Category, shown under its title on the list in smaller, muted text.
- Stored per list, like the Category name, and copied onto the Packing List when a Trip is created (ADR 0003: Categories belong to each Gear List).
- Default Categories can carry a description too, so new lists start with it.
- Edited wherever the Category name is edited. Not generated from the Category's contents.
- Included in JSON export/import and sync.
