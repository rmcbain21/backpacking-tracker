# 15: Refresh a Packing List from its Gear List

**Status:** ready-for-agent

**Idea:** If I update a Gear List after creating a Trip from it, let me pull those changes into the Trip's Packing List, or pick a different Gear List and start the Packing List again. Only while the Trip is Planned.

**Open questions:**
- This bends a current rule on purpose: a Packing List is copied once and then edited independently, and later changes to a Gear List never rewrite a Trip. A manual, opt-in refresh on a Planned Trip probably keeps the spirit of that; worth an ADR.
- Replace the whole Packing List, or merge (add new entries, update quantities, keep entries I added to the Trip myself)?
- Show a preview of what will change before applying, like the date-change preview?
- Does the Trip need to remember which Gear List it came from? (Check whether it already does.)

## Comments

**Triage (2026-10-27):** ready-for-agent, after a grilling session. Bends the "Gear List edits never change a Trip" rule on purpose: see ADR 0005. Spec updated (story 40, Trip data model, "Creating a Trip" and new "Refreshing a Packing List" behaviour rules).

*Source Gear List*
- The Trip stores its source Gear List's **id** as well as its name (today it only has the name, `Trip.CreatedFrom`). Creating a Trip from a Gear List sets both.
- Older Trips have only the name; their refresh picker starts with nothing selected.
- Only a refresh changes the source (to the list refreshed from). "Save as Gear List" doesn't.
- Idea 23's "Add to a Gear List" preselect can use the id once this lands, instead of matching by name.

*When and where*
- A **Refresh from Gear List** button on the Trip page's Packing List tab, next to "Save as Gear List". Available whenever the Trip is Planned, On Trail included; hidden on a Completed Trip.
- One action covers both "pull changes" and "start again from another list": the dialog has a Gear List picker with the source preselected, and any Gear List can be picked. If the source was deleted, the picker starts empty with a hint: from "Summer" (deleted).

*Preview*
- Nothing is saved until confirmed. The dialog lists one line per difference, each with a tick, plus the before → after Starting Pack Weight:
  - **Add** an entry on the Gear List but not the Packing List: ticked.
  - **Change** an entry on both whose quantity, Carry Mode or Category differs (one line per entry, showing each difference): ticked.
  - **Remove** an entry on the Packing List but not the Gear List: **unticked** (it may have been added to the Trip on purpose).
- **Tick all** makes the Packing List match the Gear List exactly (a full replace).
- Entries are matched by Gear Item (a list has one entry per Gear Item).
- Retired Gear Items on the Gear List are never added; the preview notes each skip ("Old stove is Retired, not added"). A Retired item already on the Packing List can still be changed or removed.
- If there are no differences: "Packing List already matches 'Summer'", nothing to confirm.

*Categories (ADR 0003)*
- An added or moved entry goes into the Packing List Category with the same name (ignoring case); if none, that Category is created at the end of the Packing List.
- A Packing List Category left empty after applying, and not on the Gear List, is removed.
- Existing Category order is kept. Renames on the Gear List aren't tracked: a renamed Category counts as a different one.

*Packed ticks*
- Same rule as adding to a list today (`ListContents.cs`): new entries start unpacked, a quantity increase unticks, everything else keeps its tick.

*Scope*
- Only the Packing List and its Categories change. Meal Plan, water carried, Trip Days and the Trip Review are untouched. Consumable gear entries are refreshed like any other.
- The Trip is one record, so sync and Sync Conflicts treat a refresh like any other Packing List edit.
