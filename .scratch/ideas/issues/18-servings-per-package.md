# 18: Servings per package on Food Items

**Status:** done

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

*Implemented (2026-11-06)*
- Core: optional `FoodItem.ServingsPerPackage` (`FoodItemDetails.ServingsPerPackage`; more than zero, to 0.01). Amounts stay stored as servings. `ServingsInput.Amount` ("3 packages (6 servings)", "1.5 packages (3 servings)"), `ToBuy` ("2 packages (3 of 4 servings)"), `WholePackages`, `ParsePerPackage`. `ShoppingLine.ServingsPerPackage` and `Packages` (rounded up; Meal components count too), so 27's "rounded up to whole packages when known" (story 97) is now done. `SlotEntryView.ServingsPerPackage`. Sync conflict view has a "Servings per package" line.
- Import/export: JSON backup and sync carry the new field as part of the Food Item record. There's no food CSV import (CSV import is gear only), so nothing to add there.
- Web: the Food Item dialog has "Servings per package (optional)"; the Food page and the Meal Plan picker show "2 servings a package". A Food Item's slot entry with the count shows "Peak Refuel Chili Mac · 1.5 packages (3 servings)"; its dialog has a **Servings | Packages** toggle (switching converts the typed amount; "1.5 packages (3 servings)" shows under the field). Shopping lines read "3 packages (5 of 6 servings)".
- Not done: picking by package when tapping in the add dialog (a tap still adds 1 serving; change it to packages in the entry's dialog), and Meal components stay in servings.
- Tests: `ServingsPerPackageTests` (15). Checked in headless Chromium at 390 px: Food page line, Meals entry text, Packages toggle saving 1.5 packages as 3 servings, Shopping rounding; no console errors.
