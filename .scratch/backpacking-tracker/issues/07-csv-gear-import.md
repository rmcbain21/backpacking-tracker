# 07: CSV gear import

**What to build:** I can import my existing spreadsheet gear from a CSV file with fixed columns, so I don't have to type it in. Re-importing or importing another trip sheet reuses gear I already have instead of creating duplicates, and I get a clear summary of what happened.

**Blocked by:** 04 (Categories)

**Status:** ready-for-agent

- [x] The CSV has a fixed header row: Category, Name, Brand/Model, Weight (g), Qty, Carry Mode. Blank Qty means 1 and blank Carry Mode means base (Qty and Carry Mode are validated now and used by ticket 11).
- [x] Unknown Categories are created.
- [x] A row matching an existing Gear Item on name + brand/model (case-insensitive, trimmed) reuses it and never modifies it; if the weight differs, the row is flagged with both weights.
- [x] Otherwise a new Gear Item is created.
- [x] Invalid rows (missing name, invalid weight, unknown Carry Mode) are rejected with row number and reason, and the rest still import.
- [x] The import summary lists created, reused, flagged and rejected rows.
- [x] The UI lets me pick a CSV file on my phone or desktop and shows the summary.
- [x] Core tests cover matching, flags, defaults, rejected rows and Category creation.

## Comments

- From ticket 04: Category names are unique ignoring case on one device, but sync can still bring in two Categories with the same name (each device added "Big Three" before syncing). Matching a CSV row's Category by name should pick the first in Category order rather than fail.
- Implemented as its own area, `app.Import.ImportCsvAsync(csvText)` (the spec lists Import/export separately), returning a `CsvImportSummary`: created, reused, flagged (with the CSV and saved weights) and rejected (row number and reason) rows, plus Categories created. Row numbers count the header as row 1, as a spreadsheet does.
- The header must name the six columns in order, ignoring case and spaces; extra columns must be empty (Excel sometimes adds blank ones).
- Matching (name + brand/model, ignoring case and spaces) includes Retired gear, so an old trip sheet doesn't recreate gear that was retired; matched gear is never changed.
- A blank Category goes to Misc (by its fixed id, so a renamed Misc still works).
- Weights may use thousands separators ("1,234.5"), as Excel saves a formatted cell. A file that wasn't saved as UTF-8 (Excel's plain "CSV") is refused with a message, because its letters like é would be garbled and never match a later correct import.
- Carry Mode accepts base, worn or consumable (any case), not abbreviations.
- Qty and Carry Mode are validated and kept on each row result for ticket 11; `CarryMode` (Base, Worn, Consumable) is in `BackpackingTracker.Core.Gear` for ticket 08 to use.
- An import is not all-or-nothing: if something other than a rejected row fails part way, the rows before it stay imported, and importing again reuses them.
- Verified with a headless Edge test against the Release publish at phone width, including an Excel-style file (byte-order mark, \r\n, quoted name with a comma).
