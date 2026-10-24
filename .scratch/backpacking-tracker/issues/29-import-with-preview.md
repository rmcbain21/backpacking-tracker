# 29: Import with preview and an optional Gear List

**What to build:** Importing a spreadsheet shows me a preview of exactly what will happen before anything is saved. Gear comes in as Gear Type + Name, and I can choose to also create a Gear List from the sheet, with its sections as the list's Categories, and to add the sections to my Default Categories. Replaces ticket 11. See ADR 0003.

**Blocked by:** 27 (Gear Type and Name), 28 (Categories belong to Gear Lists)

**Status:** ready-for-agent

- [x] Trip sheet layout: section headers are Categories; Item → Gear Type, Name → Name, g → weight; subtotal and spacer rows skipped (as now). The other layout's columns become Category, Gear Type, Name, Weight (g), Qty, Carry Mode.
- [x] A row matches an existing Gear Item when its Gear Type and Name both match, ignoring case and spaces (Retired gear included, never changed); a different weight is flagged with both weights. Rows with no weight or 0 g are rejected (as now).
- [x] Choosing a file shows a preview: Gear Items to be created and reused, flagged weights, rejected rows with reasons, and, when a Gear List will be created, its Categories and entries. Nothing is saved until I tap Import.
- [x] Options beside the preview: "Create a Gear List" (on by default, name pre-filled from the file name, editable) and "Add this sheet's sections to my Default Categories" (on by default).
- [x] The created Gear List has the sheet's sections as its Categories, in sheet order, with one entry per Gear Item under its section; repeated rows for the same Gear Item in a section add up into one entry's quantity. Qty and Carry Mode come from the row when the layout has them (defaults 1 and base).
- [x] Import saves exactly what the preview showed, all at once: if anything fails, nothing is saved.
- [x] Core tests cover the new mapping and matching, the preview making no changes, the Gear List and its Categories, combining repeated rows, the Default Categories option, and all-or-nothing saving.

## Comments

- Implemented: `GearImport.PreviewCsvAsync` builds an `ImportPreview` without saving (new Gear Items exist only in the preview); `ImportAsync(preview, ImportOptions)` saves the new Gear Items, the Gear List and new Default Categories through `IRecordStore.SaveAllAsync`, one Dexie transaction. Importing a stale or already-imported preview is refused. Verified with a headless Edge test at phone width against the real trip sheet (59 to create / 1 reused / 15 rejected; 6 Categories).
- Decisions: the same Gear Item in two sections gets an entry in each (spec: "one List Entry per Gear Item per section"); repeated rows keep the first row's Carry Mode; rows with no section go under "Other", which isn't added to the Default Categories; a section whose rows were all rejected is still a Category, so the gear can be weighed and added there.
