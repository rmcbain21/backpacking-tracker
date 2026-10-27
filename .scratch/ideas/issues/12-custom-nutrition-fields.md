# 12: Extra nutrition fields on Food Items

**Status:** ready-for-agent

**Idea:** Optional name/value pairs on a Food Item for nutrition the app doesn't track, such as sodium.

**Open questions:**
- Free-form names, or a short fixed list (sodium, potassium, sugar…) to keep names consistent so they can be totalled?
- Should they add up across a Meal and the Meal Plan, e.g. sodium per day? That only works if names and units match.
- The spec puts fat and carbohydrate tracking out of scope. Does this reopen that, or are those left out on purpose?
- Shares a design with custom gear fields (13); one mechanism for both?

## Comments

**Triage (2026-10-27):** ready-for-agent, after a grilling session together with idea 13. New glossary term: **Custom Field**. Units are labels, never converted: see ADR 0006. Spec updated (stories 92–93, Food Item and Custom Field data model, out of scope).

*Shared with idea 13*
- One Custom Field mechanism for Food Items and Gear Items: a name, a value and an optional unit label, stored inside the item (so sync and JSON export/import carry it; CSV import ignores it).
- Whichever of 12 and 13 is built first adds the shared Custom Field editor and name suggestions; the second reuses them.
- Names are free text. While typing, the editor suggests names already used on other Food Items; picking one fills in the unit last used with it (Sodium → mg).
- Names match ignoring case. An item can't have the same name twice. Clearing a value removes the field. Fields show in the order added; no limit on how many.
- Values combine (in totals) only when name and unit both match, so "Sodium mg" and "Sodium g" show as two lines.

*On Food Items*
- A Custom Field's value is a **number per serving** with an optional unit, like protein.
- Fat and carbs are allowed as Custom Fields; the spec's out-of-scope line now covers only built-in fat and carb fields.

*Totals: exactly like protein*
- Wherever protein shows (Food Item, Meal totals, each Trip Day in the Meal Plan, Trip stats), each Custom Field shows the same way, after protein, alphabetically.
- Totals scale by servings and quantity, count only the Food Items that record that field, and show nothing when none do (`MealTotals.SumKnown`).
- Frozen onto slot entries when the Trip is Completed and cleared on Reopen, alongside protein (`FoodFigures`).

*Not now*
- Renaming a field on every item at once; searching or sorting Food Items by a Custom Field.
