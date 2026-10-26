# 07: Switch units without going to Settings

**Status:** ready-for-agent

**Idea:** A quick toggle on a page (e.g. on a Gear List or a Trip) to flip between imperial and metric, instead of changing the unit system in Settings.

**Open questions:**
- Is the toggle a shortcut that changes the Settings value for every page, or a temporary view for this page only?
- If it changes Settings, it syncs to my other devices. Is that wanted?
- Where does it go so it doesn't crowd the top bar on a phone?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- A shortcut to the existing Settings value (`UserSettings.SetUnitSystemAsync`), not a separate per-page view. Flipping it changes units on every page, and it syncs to other devices like any Settings change.
- Shown on pages with weights: Gear Lists, Packing Lists/Trips, Gear Items, and food pages.
- Placed out of the top bar on a phone, e.g. a small "oz / g" toggle next to the weight totals.
