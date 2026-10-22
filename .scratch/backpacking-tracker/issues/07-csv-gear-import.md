# 07: CSV gear import

**What to build:** I can import my existing spreadsheet gear from a CSV file with fixed columns, so I don't have to type it in. Re-importing or importing another trip sheet reuses gear I already have instead of creating duplicates, and I get a clear summary of what happened.

**Blocked by:** 04 (Categories)

**Status:** ready-for-agent

- [ ] The CSV has a fixed header row: Category, Name, Brand/Model, Weight (g), Qty, Carry Mode. Blank Qty means 1 and blank Carry Mode means base (Qty and Carry Mode are validated now and used by ticket 11).
- [ ] Unknown Categories are created.
- [ ] A row matching an existing Gear Item on name + brand/model (case-insensitive, trimmed) reuses it and never modifies it; if the weight differs, the row is flagged with both weights.
- [ ] Otherwise a new Gear Item is created.
- [ ] Invalid rows (missing name, invalid weight, unknown Carry Mode) are rejected with row number and reason, and the rest still import.
- [ ] The import summary lists created, reused, flagged and rejected rows.
- [ ] The UI lets me pick a CSV file on my phone or desktop and shows the summary.
- [ ] Core tests cover matching, flags, defaults, rejected rows and Category creation.

## Comments

- From ticket 04: Category names are unique ignoring case on one device, but sync can still bring in two Categories with the same name (each device added "Big Three" before syncing). Matching a CSV row's Category by name should pick the first in Category order rather than fail.
