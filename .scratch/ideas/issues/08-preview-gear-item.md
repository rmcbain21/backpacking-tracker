# 08: Tap a List Entry to preview its Gear Item

**Status:** ready-for-agent

**Idea:** On a Gear List or Packing List, tap an entry to see its Gear Item's details (weight, Gear Type, Tags, notes, which lists it's on) in a pop-up, without leaving the list.

**Open questions:**
- Read-only preview, or editable in place?
- Which details are worth showing? "Which other lists and Trips use this" could be handy.
- How does a tap to preview avoid clashing with the existing controls on an entry (quantity, Carry Mode, Category)?
- Gear photos are out of scope in the spec, so the preview would be text only.

## Comments

**Triage (2026-10-26):** ready-for-agent.

- A read-only pop-up showing the Gear Item's details: name, Gear Type, weight, Tags, notes, Retired if so, and which Gear Lists and Trips use it.
- Opened from a small info button on each List Entry, not by tapping the whole entry, so it doesn't clash with the quantity, Carry Mode and Category controls.
- The pop-up has an "Edit" link to the Gear Item's page for changes.
- Works on both Gear Lists and Packing Lists. Text only (gear photos are out of scope).
