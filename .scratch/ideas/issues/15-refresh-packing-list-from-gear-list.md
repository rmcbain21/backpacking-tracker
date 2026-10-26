# 15: Refresh a Packing List from its Gear List

**Status:** needs-triage

**Idea:** If I update a Gear List after creating a Trip from it, let me pull those changes into the Trip's Packing List, or pick a different Gear List and start the Packing List again. Only while the Trip is Planned.

**Open questions:**
- This bends a current rule on purpose: a Packing List is copied once and then edited independently, and later changes to a Gear List never rewrite a Trip. A manual, opt-in refresh on a Planned Trip probably keeps the spirit of that; worth an ADR.
- Replace the whole Packing List, or merge (add new entries, update quantities, keep entries I added to the Trip myself)?
- Show a preview of what will change before applying, like the date-change preview?
- Does the Trip need to remember which Gear List it came from? (Check whether it already does.)

## Comments
