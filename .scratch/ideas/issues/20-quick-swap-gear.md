# 20: Quick swap gear of the same Gear Type

**Status:** ready-for-agent

**Idea:** On a Gear List or Packing List, swap an entry for another Gear Item of the same Gear Type (e.g. swap tents) and see the weight change, e.g. "X-Dome → X-Mid: −14.2 oz".

**Open questions:**
- Show the weight difference in the picker before swapping, as well as after?
- Keep the entry's quantity, Carry Mode and Category when swapping?
- Leave Retired gear out of the picker? Probably.
- Ties in with custom gear fields (13): the picker could also show R-value etc.

## Comments

**Triage (2026-10-26):** ready-for-agent.

- A "Swap" action on a List Entry, on both Gear Lists and Packing Lists.
- It opens a picker of other non-Retired Gear Items with the same Gear Type. Each shows its weight and the difference from the current one ("X-Mid, −14.2 oz"). Entries with no Gear Type can't be swapped (hide the action).
- Swapping changes only the Gear Item. Quantity, Carry Mode, Category and position stay the same.
- Afterwards, a short message shows the change: "Swapped X-Dome → X-Mid: −14.2 oz".
- Leave room for custom gear fields (13) to show in the picker later; don't build that here.
