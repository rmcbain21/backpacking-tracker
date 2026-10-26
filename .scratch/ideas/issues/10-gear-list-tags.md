# 10: Tags on Gear Lists

**Status:** ready-for-agent

**Idea:** Tag a Gear List as e.g. "Summer", "Winter" or "Comfort", so I can filter my lists and pick the right starting point for a Trip.

**Open questions:**
- Gear Items already have Tags. Should Gear Lists share the same Tags, or have their own separate set?
- Where do list Tags show up: the Gear Lists page filter, the "create Trip" list picker, or both?
- Would Trips benefit from the same thing (tag a Trip as "Winter"), e.g. for stats?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Gear Lists use the same Tags as Gear Items: same suggestions, same matching rules (`TagRules`), one shared list of Tag names.
- Tags are edited on the Gear List page.
- The Gear Lists page gets a Tag filter.
- The "create Trip" Gear List picker shows each list's Tags.
- Tags aren't copied onto the Trip. Tagging Trips is left for later.
- Included in JSON export/import and sync.
