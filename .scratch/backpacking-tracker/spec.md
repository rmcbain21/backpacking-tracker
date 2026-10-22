# Spec: Backpacking Tracker v1

Status: ready-for-agent

## Problem Statement

I track my backpacking gear in Excel sheets, one sheet per trip, with weights typed in three units and categories done by hand with section header rows. Every trip means copying a sheet, re-totalling weights, and keeping food, itinerary and notes somewhere else entirely. The sheets don't work well on my phone, which is where I am when I'm packing or on trail, and on trail I usually have no signal. Tools like Lighterpack and PackWizard do the weight maths but don't cover meals, daily itineraries, journal notes or trip history, and they need a connection.

## Solution

A personal, phone-first app that installs from the browser on my Android phone (and works on tablet and desktop too) and works fully offline. I keep a catalogue of my Gear Items and Food Items, build reusable Gear Lists and Meals, and create Trips from a Gear List. Each Trip has its own Packing List, a day-by-day plan (Trip Days with route, meals and journal notes) and stats, with pack weight (Base Weight, Worn Weight, Consumable Weight, Starting Pack Weight) calculated automatically. When a Trip is Completed its weights and nutrition are frozen as a permanent record. Data lives on each device and is backed up and synced between my devices through my own Google Drive, with no server to run. I can bring my existing spreadsheet gear in through a CSV import, and export everything to a JSON file.

## User Stories

### Gear

1. As a backpacker, I want to add a Gear Item with a name, brand/model and weight, so that my gear catalogue reflects what I own.
2. As a backpacker, I want to enter a Gear Item's weight in ounces, pounds or grams, so that I can type whatever my scale shows.
3. As a backpacker, I want the weight I store to be my measured weight, with a notes field for the manufacturer's figure, so that my pack totals are realistic.
4. As a backpacker, I want to optionally record a price and notes on a Gear Item, so that I remember what I paid and anything worth knowing about it.
5. As a backpacker, I want to assign each Gear Item to exactly one Category, so that my pack weight can be broken down by Category.
6. As a backpacker, I want a sensible default set of Categories (such as Shelter, Sleep, Pack, Cook, Water, Clothing, Electronics, Hygiene, First Aid, Misc) on first run, so that I can start without setup.
7. As a backpacker, I want to add, rename, reorder and remove my own Categories, so that the grouping matches how I think about gear (for example "Big Three + Sleeping Pad").
8. As a backpacker, I want to be prevented from removing a Category that Gear Items still use (or be asked where to move them), so that no Gear Item is left without a Category.
9. As a backpacker, I want to add many free-form Tags to a Gear Item (such as "winter" or "bear country"), so that I can find gear by situation.
10. As a backpacker, I want to search my Gear Items by name, brand/model and Tag, and filter by Category, so that I can find a piece of gear quickly on my phone.
11. As a backpacker, I want to edit any field of a Gear Item, so that I can correct mistakes or record a new measured weight.
12. As a backpacker, I want to Retire a Gear Item I no longer own, so that it disappears from my catalogue and new lists but stays on past Trips that used it.
13. As a backpacker, I want to see my Retired Gear Items and un-retire one, so that I can undo a mistake.
14. As a backpacker, I want to permanently delete a Gear Item only when no List Entry anywhere uses it, so that I can clean up typos without damaging history.

### Gear Lists and weight

15. As a backpacker, I want to create a named Gear List, so that I have a reusable starting point for Trips.
16. As a backpacker, I want to add Gear Items to a Gear List as List Entries with a quantity, so that I can record that I carry two of something.
17. As a backpacker, I want to set each List Entry's Carry Mode to base, worn or consumable, so that weight is classified correctly on that list.
18. As a backpacker, I want a new List Entry to default to the base Carry Mode, so that I only need to change the exceptions.
19. As a backpacker, I want the same Gear Item to have different Carry Modes on different lists, so that my puffy can be worn on one list and packed on another.
20. As a backpacker, I want to remove List Entries and change quantities, so that I can refine a list.
21. As a backpacker, I want to see a Gear List's Base Weight, Worn Weight, Consumable Weight and Starting Pack Weight, so that I know what I'll carry.
22. As a backpacker, I want to see the weight broken down by Category, with each Category's share of the total, so that I can see where my weight goes.
23. As a backpacker, I want a List Entry to always show the Gear Item's current weight, so that re-weighing an item updates every Gear List that uses it.
24. As a backpacker, I want to rename, duplicate and delete Gear Lists, so that I can manage variations like summer and winter kits.
25. As a backpacker, I want deleting a Gear List to leave every Trip created from it untouched, so that my trip records stay intact.

### Units and settings

26. As a backpacker, I want weights, distances and elevations shown in imperial units by default, so that the app speaks my units.
27. As a backpacker, I want a setting to switch to metric, so that I can change my mind without losing data.
28. As a backpacker, I want small weights shown in ounces and large totals in pounds and ounces, so that numbers are easy to read.

### Import and export

29. As a backpacker, I want to import Gear Items from a CSV file with fixed columns (Category, Name, Brand/Model, Weight in grams, Qty, Carry Mode), so that I don't have to type in my existing spreadsheet gear.
30. As a backpacker, I want the import to create any Categories it doesn't recognise, so that my spreadsheet's grouping comes across.
31. As a backpacker, I want a CSV row that matches an existing Gear Item (same name and brand/model, ignoring case) to reuse it rather than create a duplicate, so that importing several trip sheets doesn't double my catalogue.
32. As a backpacker, I want the import never to overwrite an existing Gear Item, and to flag rows whose weight differs from the stored one, so that I decide which weight is right.
33. As a backpacker, I want a blank Carry Mode in the CSV to mean base, and a blank Qty to mean 1, so that my partially filled sheets import cleanly.
34. As a backpacker, I want the option to also create a Gear List from the imported rows, so that my first trip sheet becomes my first Gear List in one step.
35. As a backpacker, I want an import summary listing created, reused, flagged and rejected rows (with the reason), so that I know exactly what happened.
36. As a backpacker, I want to export my whole database as a JSON file I can download, so that I have a backup that doesn't depend on Google.
37. As a backpacker, I want to import a JSON export, merged with my existing data using the same rules as sync, so that I can restore a backup or move providers.

### Trips

38. As a backpacker, I want to create a Trip with a name, start date and end date, from a Gear List or from nothing, so that I can start planning.
39. As a backpacker, I want creating a Trip from a Gear List to copy its List Entries into the Trip's own Packing List, so that I can tweak this trip without changing the Gear List.
40. As a backpacker, I want edits to the Gear List after the Trip is created never to change the Trip's Packing List, so that each Trip is independent.
41. As a backpacker, I want to add, remove and change List Entries on a Trip's Packing List, so that I can swap the tent for one trip only.
42. As a backpacker, I want a Trip to have one Trip Day per calendar day between its start and end dates, so that a 3-night trip has 4 days.
43. As a backpacker, I want each Trip Day to hold where it starts and ends, the campsite, planned distance, actual distance and elevation gain, so that I have a day-by-day route.
44. As a backpacker, I want to view the Trip's Itinerary as the route details of all its Trip Days in order, so that I can see the whole route at a glance.
45. As a backpacker, I want to write journal notes on each Trip Day, so that I can record what happened.
46. As a backpacker, I want to record an optional water carried weight on a Trip, counted as consumable, so that my Starting Pack Weight is honest.
47. As a backpacker, I want a Trip to show Base Weight, Worn Weight, Consumable Weight (consumable List Entries, Meal Plan food and water) and Starting Pack Weight, with a Category breakdown, so that I know my load for this trip.
48. As a backpacker, I want a packing checklist where I tick each Packing List entry as packed, so that I don't forget anything at the trailhead.
49. As a backpacker, I want to reset the packing checklist, so that I can re-check before leaving.
50. As a backpacker, I want to lengthen a Trip and get empty Trip Days added at the end, so that extending is easy.
51. As a backpacker, I want shortening a Trip to warn me which removed Trip Days have content and require confirmation, so that I don't lose planning by accident.
52. As a backpacker, I want moving a Trip's dates without changing its length to keep every Trip Day's contents, so that rescheduling is painless.
53. As a backpacker, I want a Trip to be Planned until I mark it Completed, so that I can tell upcoming trips from past ones.
54. As a backpacker, I want a Planned Trip to use the current weights and nutrition of its gear and food, so that my planning reflects my latest measurements.
55. As a backpacker, I want marking a Trip Completed to freeze every weight and nutrition value it uses, so that re-weighing gear later never changes a past Trip's numbers.
56. As a backpacker, I want to still edit journal notes, actual distances, elevation and other typed-in stats on a Completed Trip, so that I can fix typos and fill in gaps.
57. As a backpacker, I want to Reopen a Completed Trip back to Planned, with a fresh freeze when I complete it again, so that I can correct a Trip properly.
58. As a backpacker, I want to see a Trip's stats: days and nights, planned vs actual distance, elevation gain, Starting Pack Weight, Base Weight, calories per day, and total food cost, so that I can review and compare trips.
59. As a backpacker, I want to save a Trip's Packing List as a new Gear List, so that what I actually carried becomes my next starting point.
60. As a backpacker, I want to see my Trips grouped as upcoming (Planned) and past (Completed), so that I can find a trip quickly.
61. As a backpacker, I want to delete a Trip, so that I can remove one I created by mistake.

### Food

62. As a backpacker, I want to add a Food Item with a name, serving weight and calories per serving, so that I can plan food by weight and calories.
63. As a backpacker, I want to optionally record protein and cost per serving on a Food Item, so that I can track protein and food spend.
64. As a backpacker, I want to see each Food Item's Calorie Density, so that I can pick efficient food.
65. As a backpacker, I want to add Tags to Food Items (such as "no-cook" or "breakfast"), so that I can filter food.
66. As a backpacker, I want to create a Meal as a named combination of Food Items with servings of each, so that I can reuse combos like a Ramen Bomb.
67. As a backpacker, I want to see a Meal's total weight, calories and Calorie Density, so that I can compare Meals.
68. As a backpacker, I want to add Tags to Meals, so that I can filter Meals.
69. As a backpacker, I want to Retire Food Items and Meals I no longer use, with the same rules as Gear Items, so that my lists stay clean without breaking past Trips.
70. As a backpacker, I want each Trip Day to have Breakfast, Lunch, Dinner and Snacks slots, so that I can plan each day's food.
71. As a backpacker, I want to put Meals or individual Food Items in a slot, each with a quantity, so that I don't have to wrap a single bar in a Meal.
72. As a backpacker, I want any slot to be allowed to stay empty, so that the first and last days can be partial.
73. As a backpacker, I want to view the Trip's Meal Plan as all its Trip Days' slots together, with calories and food weight per day, so that I can see whether each day is fuelled.
74. As a backpacker, I want Meal Plan food weight counted in the Trip's Consumable Weight, so that pack weight includes food.
75. As a backpacker, I want to copy the Meal Plan from an earlier Trip into this one, day by day, so that I can reuse a plan that worked.

### Offline, install and devices

76. As a backpacker, I want to install the app on my Android phone's home screen, so that it opens like an app.
77. As a backpacker, I want every feature except sync to work with no signal, so that the app works on trail.
78. As a backpacker, I want my data to stay on the device between sessions, so that closing the app loses nothing.
79. As a backpacker, I want a phone layout with bottom navigation (Gear, Lists, Trips, Food) and one column, so that I can use it one-handed.
80. As a backpacker, I want a tablet and desktop layout with a navigation drawer and list-plus-detail side by side, so that bigger screens are useful.

### Sync

81. As a backpacker, I want to connect my Google Drive account once, so that my data backs up and syncs.
82. As a backpacker, I want my data stored in a hidden app folder in my Drive, so that it doesn't clutter my files.
83. As a backpacker, I want sync to run automatically when the app opens and shortly after I make changes, when online, so that I don't have to think about it.
84. As a backpacker, I want a "Sync now" button and a "last synced" indicator, so that I can force a sync and know how current my data is.
85. As a backpacker, I want records changed on only one device to merge silently, so that normal use needs no attention.
86. As a backpacker, I want a deletion on one device to stay deleted on the others, so that deleted things don't come back.
87. As a backpacker, I want to be shown a Sync Conflict, with both versions side by side, when the same record changed on two devices since they last synced, so that nothing is overwritten without my say.
88. As a backpacker, I want to settle each Sync Conflict by choosing one version, so that I stay in control.
89. As a backpacker, I want a badge showing how many Sync Conflicts are waiting, with each device keeping its own version until I decide, so that I can deal with them later.
90. As a backpacker, I want to disconnect Google Drive, so that I can stop syncing without losing local data.
91. As a backpacker, I want sync failures (no signal, expired sign-in) to show a clear message and retry later, so that I know sync is pending.

## Implementation Decisions

Architecture follows ADR 0001 (offline-first Blazor WebAssembly PWA, no server, Google Drive sync) and ADR 0002 (store metric, display imperial). Terms follow `GLOSSARY.md`.

### Modules

- **App core**: a plain C# class library with no Blazor or JavaScript dependency. It holds the domain model and all behaviour, and exposes one application-level API used by the UI. Sub-areas:
  - **Gear catalogue**: Gear Items, Categories, Tags, Retired state, retire/delete rules.
  - **Lists**: Gear Lists and List Entries.
  - **Weight**: a pure calculator that, given List Entries, resolved item weights, Meal Plan food and water, returns Base Weight, Worn Weight, Consumable Weight, Starting Pack Weight and the Category breakdown. Used by both Gear Lists and Trips.
  - **Food**: Food Items, Meals, Calorie Density.
  - **Trips**: Trips, Packing List, Trip Days, Meal Plan slots, Trip Status transitions, freezing, date changes, stats, "Save as Gear List", copying a Meal Plan.
  - **Units**: conversion and formatting between stored metric values and the chosen display units, and parsing typed input in oz, lb or g.
  - **Import/export**: CSV gear import and JSON export/import.
  - **Sync engine**: merges local records with the sync file and produces Sync Conflicts.
- **Ports used by the core** (interfaces the core depends on, with real adapters in the app):
  - **Record store**: save, load and list records per record type on this device. Real adapter: IndexedDB via Dexie through a thin JavaScript interop module.
  - **Cloud file store**: read the sync file, write the sync file, get its last-modified time. Real adapter: Google Drive `appDataFolder`, signing in with Google Identity Services and the `drive.appdata` scope only. This is the seam for adding OneDrive later.
  - **Clock**: current time, for `updatedAt` and sync bookkeeping.
- **Web app**: Blazor WebAssembly standalone app on the current LTS .NET, created with PWA support (service worker caches the app shell for offline use). MudBlazor for UI. Pages are thin: they call the core API and render results; any logic that grows in a page moves into the core.

### Records and schema

- Every stored record has: a string `id` (GUID generated on the device), `updatedAt` (UTC timestamp from the Clock), and `deleted` (tombstone flag; deleted records are kept so deletions sync). These exist from slice 1 even though sync arrives in slice 5, so no data migration is needed.
- Record types are top-level: Category, Gear Item, Gear List, Food Item, Meal, Trip, Settings. Child data is nested inside its parent and syncs as part of it: List Entries inside a Gear List; Packing List entries and Trip Days (with their meal slots and journal notes) inside a Trip; Meal components inside a Meal.
- All weights are stored in grams with 0.1 g precision; all distances and elevations in metres (ADR 0002).
- **Category**: name, sort order. The default Categories have fixed ids and an `updatedAt` of the Unix epoch, meaning "never edited", so every device creates identical defaults and sync can tell an untouched default from an edited one. Removed Categories are kept as tombstones, and the last Category cannot be removed.
- **Gear Item**: name, brand/model, weight, Category, Tags, optional price, optional notes, Retired flag.
- **Gear List**: name, List Entries.
- **List Entry**: Gear Item reference, quantity (whole number, at least 1), Carry Mode (base, worn or consumable; defaults to base). On a Packing List it also has a packed flag, and when the Trip is Completed, a frozen weight.
- **Food Item**: name, serving weight, calories per serving, optional protein per serving, optional cost per serving, Tags, Retired flag.
- **Meal**: name, components (Food Item reference and number of servings), Tags, Retired flag.
- **Trip**: name, start date, end date, Trip Status (Planned or Completed), Packing List, optional water carried, Trip Days.
- **Trip Day**: date, start, end, campsite, planned distance, actual distance, elevation gain, journal notes, and four meal slots (Breakfast, Lunch, Dinner, Snacks). Each slot holds zero or more slot entries: either a Meal or a Food Item reference with a quantity. When the Trip is Completed, each slot entry also holds frozen weight and nutrition.
- **Settings**: display unit system (imperial by default). Device-local sync bookkeeping (see Sync) is stored separately and never synced. There is exactly one Settings record, with the fixed id `00000000-0000-0000-0000-000000000001` instead of a device-generated GUID, so every device edits the same record and sync merges it rather than creating one per device.

### Behaviour rules

- **Weight**: an entry's weight is item weight × quantity. Base Weight, Worn Weight and Consumable Weight sum entries by Carry Mode. Consumable Weight also includes Meal Plan food (each slot entry's weight × quantity) and water carried. Starting Pack Weight = Base Weight + Consumable Weight. The Category breakdown groups entries by their Gear Item's Category; Meal Plan food and water appear as their own breakdown lines.
- **Retired and delete**: Retiring hides an item from catalogue views and pickers but keeps it resolvable everywhere it is referenced. Hard delete is allowed only when nothing references the item (no List Entry, slot entry or Meal component); otherwise the core refuses and offers Retire.
- **Creating a Trip from a Gear List** deep-copies its List Entries into the Packing List. No link back to the Gear List is kept beyond an optional "created from" name for display.
- **Trip Status**: Planned → Completed copies the current weight of every Packing List entry and the current weight, calories, protein and cost of every slot entry (resolving Meals through their components) onto the Trip. Completed Trips compute all stats from frozen values. Reopen (Completed → Planned) clears the frozen values. On a Completed Trip, journal notes, route fields, distances, elevation and water carried stay editable; Packing List and Meal Plan composition are locked until Reopen.
- **Date changes**: the core exposes a preview of a date change, listing the Trip Days that would be removed and whether each has content. Applying the change requires explicit confirmation when any removed day has content. Lengthening appends empty days; shifting with the same length re-dates days in order and keeps their contents.
- **Stats** (calculated): days and nights, total planned and actual distance (sum of Trip Days), total elevation gain (sum of Trip Days), Starting Pack Weight, Base Weight, calories per day (total Meal Plan calories ÷ number of Trip Days), total food cost (from slot entries with a cost).
- **Save as Gear List** copies the Packing List's entries (Gear Item, quantity, Carry Mode) into a new Gear List.
- **Copy Meal Plan** copies slot contents from a source Trip's days onto this Trip's days by day index, up to the shorter Trip's length, replacing the target slots after confirmation.
- **Units**: imperial display shows weights under 1 lb in ounces to one decimal, and larger totals in lb + oz; distance in miles; elevation in feet. Typed weights accept oz, lb or g and are converted to grams.

### CSV import

- Fixed header row: Category, Name, Brand/Model, Weight (g), Qty, Carry Mode. Blank Qty means 1, blank Carry Mode means base.
- For each row, an unknown Category is created. A Gear Item matching on name + brand/model (case-insensitive, trimmed) is reused and never modified; a weight difference is reported as a flag. Otherwise a new Gear Item is created.
- Optionally, the import also creates a named Gear List with one List Entry per row, using that row's Qty and Carry Mode.
- The result is an import summary: created, reused, flagged (with both weights) and rejected rows (with row number and reason, such as a missing name or an invalid weight). Rejected rows don't stop the rest.

### Sync

- **Sync file**: one JSON document in the Drive app-data folder containing a format version and every record of every type, including tombstones. The JSON export uses the same format.
- **Bookkeeping** (per device, local only): for each record, the `updatedAt` it had at the last successful sync (its base), plus the time of the last successful sync.
- **Merge**, per record, comparing local, remote and base:
  - Only one side changed since base → take that side.
  - Both changed and the contents are identical → take either, no conflict.
  - Both changed and the contents differ → a Sync Conflict. The local version stays on the device, the remote version stays in the sync file, and the conflict is stored locally until settled. Settling chooses one version, which is written with a new `updatedAt` and wins on the next sync.
  - A record that exists on only one side is taken from that side.
- **Run**: read the sync file (or treat it as empty if none exists yet), merge, save merged records locally, write the merged file back, update bookkeeping. A failed write leaves bookkeeping unchanged so the next sync retries.
- **Triggers**: on app open and a few seconds after local changes (debounced) when online, plus "Sync now". The UI shows last-synced time, the pending Sync Conflict count, and sync errors.
- **Google sign-in**: access tokens are held in memory only; when one expires, the next sync asks to sign in again. Disconnecting forgets the token and leaves local data untouched.
- **JSON import** runs the same merge with the imported file as "remote".

### Hosting and delivery

- Hosted on GitHub Pages, deployed by a GitHub Actions workflow on push to main. This needs a GitHub repository and remote, which doesn't exist yet. The published base path must match the Pages URL, and that URL is registered as an authorised origin for the Google OAuth client.
- Issues and specs stay in the local `.scratch/` tracker.

### Build order (slices, each usable on its own)

1. **Gear**: app skeleton (PWA, MudBlazor, responsive navigation, offline storage, GitHub Pages deploy), Gear Items, Categories, Tags, Retired, units display and settings, CSV gear import (without Gear List creation).
2. **Gear Lists and weight**: Gear Lists, List Entries, Carry Mode, the weight calculator and Category breakdown, and the CSV import option to create a Gear List.
3. **Trips**: Trips from a Gear List, Packing List and packing checklist, Trip Days and Itinerary, journal notes, water carried, date changes, Trip Status with freeze and Reopen, stats (food-dependent stats show zero until slice 4), Save as Gear List.
4. **Food**: Food Items, Meals, Calorie Density, meal slots and Meal Plan, food in Consumable Weight, calories per day and food cost stats, freezing nutrition, copying a Meal Plan.
5. **Sync**: Google Drive adapter and sign-in, the sync engine, Sync Conflicts UI, sync triggers and status, JSON export and import.

## Testing Decisions

- **One test surface: the app core's public API.** Tests are xUnit tests that call the core the way the UI does and assert on what comes back, such as totals, lists, statuses, import summaries and Sync Conflicts. They never assert on private methods, internal data structures or how records are laid out in storage. The one exception: a test of data that the API can no longer create (records saved by an older version, or brought in by sync from another device) may write that record to the in-memory store directly, with a comment saying why. Likewise, until the sync engine exists (ticket 24), a test may read the store to check that a deletion left a tombstone, and a test may register a stand-in `IGearItemReferences` through the internal `AddReferences` hook to exercise the delete guard before Gear Lists exist (ticket 08 replaces it with a real one).
- **Ports are replaced by in-memory implementations**: an in-memory record store, an in-memory cloud file store and a controllable clock. These fakes live with the tests and are kept simple and trustworthy.
- **Sync is tested as two devices**: two core instances with separate in-memory record stores share one in-memory cloud file store. Tests edit on both, sync both, and check the outcome: silent merges, tombstones that propagate, identical edits that don't conflict, real Sync Conflicts and settling them, and retrying after a failed write.
- **What gets tested in the core**: weight calculations (including Carry Mode, quantities, food, water and the Category breakdown); Retire and hard-delete rules; Trip creation copying a Gear List; Trip Status freezing and Reopen; date-change preview and application; stats; Save as Gear List; Meal Plan copying; unit conversion, formatting and input parsing; CSV import (matching, flags, rejected rows, defaults, optional Gear List); and JSON export → import round-trips.
- **Not unit-tested**: the Dexie/IndexedDB interop, the Google Drive adapter and the MudBlazor pages. They are kept thin and checked by hand, with at most one smoke test each. If a page grows logic, move the logic into the core instead of adding component tests.
- **A good test** describes one behaviour in domain language (for example "completing a Trip freezes Gear Item weights"), builds its data through the core API, and survives refactoring of the core's internals.
- **Prior art**: none yet; this is a new codebase. The first slice establishes the test project, the in-memory fakes and the conventions that later slices follow.

## Out of Scope

- Accounts, other users, sharing Trips or Gear Lists, and splitting group gear.
- Any server or database of our own.
- OneDrive or other cloud providers (the cloud file store seam allows adding them later).
- GPX import, live GPS tracking and maps.
- Barcode scanning and nutrition lookup (Open Food Facts).
- Gear wishlist, gear photos.
- Combining several Gear Lists into one Trip; bulk-adding gear by Tag.
- Day-by-day pack weight as food is eaten; marking Meals as eaten.
- Reusable Meal Plan templates (copying from an earlier Trip covers this).
- Importing past Trips from old spreadsheets, and Lighterpack or PackWizard formats.
- Fat and carbohydrate tracking.
- Native app store builds and iOS-specific offline work.

## Further Notes

- The user's existing gear is in Excel, one sheet per trip. Columns are Category (as section header rows), Item, Name, oz, lb, g, Qty and Type (mostly blank). Rearranging one sheet into the fixed CSV columns (section header → Category, Item → Name, Name → Brand/Model, g → Weight) seeds the Gear Items and the first Gear List. Later sheets add only new gear, because of matching.
- Primary device: an Android Samsung phone using Chrome. Design and test the phone layout first.
- The user is learning Blazor for work. Prefer idiomatic, straightforward Blazor and C# over clever patterns.
- Before slice 5, a Google Cloud project with an OAuth client for the GitHub Pages origin must exist. Setting it up is a human-only step.
