# 18: Servings per package on Food Items

**Status:** ready-for-agent

**Idea:** Record how many servings come in a package, e.g. a Peak Refuel meal packaged as 2 servings, so I can plan by package as well as by serving.

**Open questions:**
- Is the Food Item's weight and nutrition still entered per serving, with servings per package added on, or entered per package?
- In the Meal Plan, do I pick "1 package" or "2 servings"? Both?
- Would a packing view show "3 packages" rather than "6 servings"?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Weight, calories, protein and cost stay entered per serving, so stored nutrition doesn't change.
- Add an optional "servings per package" to a Food Item (e.g. 2 for a Peak Refuel pouch).
- In a Meal Plan, when the count is set, amounts can be picked by package or by serving. They're always stored as servings (1 package = 2 servings).
- Food and packing views show both when the count is set: "3 packages (6 servings)". A part package shows as e.g. "1.5 packages (3 servings)".
- Included in CSV/JSON import and export and sync.
