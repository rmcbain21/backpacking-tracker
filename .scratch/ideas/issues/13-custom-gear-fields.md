# 13: Custom fields on Gear Items

**Status:** ready-for-agent

**Idea:** Optional name/value pairs on a Gear Item for specs the app has no field for, such as a sleeping pad's R-value or a bag's temperature rating. Gear Items with the same Gear Type and matching field names could be compared side by side.

**Open questions:**
- Free-form field names, or suggested names per Gear Type (Sleeping Pad → R-value)?
- What does comparison look like: a table of all Gear Items of one Gear Type, one column per field?
- Text values only, or numbers (with units) so they can be sorted?
- Shares a design with custom nutrition fields (12).
- Ties in with quick swap (20), which also compares gear of the same Gear Type.

## Comments

**Triage (2026-10-27):** ready-for-agent, after a grilling session together with idea 12. New glossary term: **Custom Field**. Units are labels, never converted: see ADR 0006. Spec updated (stories 92, 94–95, Gear Item and Custom Field data model, out of scope).

*Shared with idea 12*
- One Custom Field mechanism for Gear Items and Food Items: a name, a value and an optional unit label, stored inside the item (so sync and JSON export/import carry it; CSV import ignores it).
- Whichever of 12 and 13 is built first adds the shared Custom Field editor and name suggestions; the second reuses them.
- Names are free text. While typing, the editor suggests names already used on other Gear Items, those of the **same Gear Type** first; picking one fills in the unit last used with it (Length → in).
- Names match ignoring case. An item can't have the same name twice. Clearing a value removes the field. Fields show in the order added; no limit on how many.

*On Gear Items*
- A value is a number when it parses as one (R-value: 4.5, Length: 72 in), otherwise text (Fabric: 20D nylon). No type to pick.

*Compare*
- From the Gear Items page: filter by a Gear Type, then **Compare**. A table with one row per non-Retired Gear Item of that type and columns for weight plus every Custom Field (name + unit) used on that type.
- Tap a column header to sort: numbers as numbers, text alphabetically, items missing the field always last. Weight shows in the chosen display units (ADR 0002); Custom Fields show as typed.
- A name used with two units ("Length cm", "Length in") is two columns.

*Elsewhere*
- Shown in the Gear Item add/edit dialog (where they're edited), the Gear Item preview (idea 08), and under each option in the quick swap picker (idea 20), e.g. "X-Mid · −14.2 oz · 2-person, 2 doors".
- Not shown on List Entry rows.

*Not now*
- Renaming a field on every item at once; searching or sorting the gear catalogue by a Custom Field.
