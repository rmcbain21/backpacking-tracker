# 23: Trip post-mortem

**Status:** ready-for-agent

**Idea:** After a Trip, record a review:
- mark which List Entries were actually used
- note what I wished I'd brought
- a closing note
- a rating

**Open questions:**
- Part of completing a Trip, or something I can fill in any time afterwards?
- Could "not used" feed back into Gear Lists, e.g. a hint that an item went unused on the last three Trips?
- Is "wished I'd brought" free text, or picked from my Gear Items so I can add them to the Gear List in one step?
- Rating scale: stars out of 5?
- Overlaps a little with journal notes on Trip Days; the post-mortem is for the Trip as a whole.

## Comments

**Triage (2026-10-27):** ready-for-agent, after a grilling session. New glossary term: **Trip Review** (used for this feature from here on instead of "post-mortem").

*When*
- Available once the Trip has started (the existing `HasStarted` rule: today is on or after the start date) and still editable after the Trip is Completed, like Trip Day route and journal notes. The Completed lock on the Packing List, checklist, dates and Meal Plan doesn't apply to the Review.
- After Mark Completed, the Trip page switches to the Review tab. No dialog; the Review can be left blank.
- Reopening a Trip keeps its Review.

*Where*
- A **Review** tab on the Trip page after Stats, shown only once the Trip has started. Top to bottom:
  1. **Rating:** optional, 1–5 stars, for the Trip as a whole.
  2. **Closing note:** one free-text note. No per-entry notes (could be its own idea later).
  3. **Wished I'd brought:** a list of lines, each either a picked Gear Item or free text.
  4. **Gear used:** the Packing List grouped by Category. Tapping a row cycles Used → Didn't use → Emergency only (like ticking the Checklist). Unmarked means Used.

*Gear used*
- A mark covers the whole List Entry, whatever its quantity. Removing an entry from the Packing List removes its mark.
- **Emergency only** is for safety gear carried in case (first aid kit, satellite messenger), so it never counts as unused.
- Unused weight: "You carried 14 oz of Base Weight you didn't use": the total of Didn't use entries with base Carry Mode only (worn and consumable entries can be marked but aren't counted). Uses frozen weights on a Completed Trip, current weights otherwise; shown in the chosen units (ADR 0002).

*Wished I'd brought*
- A picked Gear Item line has **Add to a Gear List**: choose the list, with the one whose name matches the Trip's `CreatedFrom` preselected if it still exists. Adds it the same way as adding gear to a list today.
- A free-text line has **Create Gear Item**: opens the Add Gear dialog with the Name filled in. Once saved, the line becomes a picked Gear Item line.

*Hint on Gear Lists*
- On a Gear List, an entry shows **"Unused on last 3 Trips"** when its Gear Item was marked Didn't use on each of the 3 most recent Completed Trips whose Packing List had it (matched by Gear Item, whatever Gear List the Trips came from). Emergency only never counts. Fixed threshold of 3; information only, no prompt to remove.

*Elsewhere*
- Past Trip rows on the Trips list show the star rating when set.
- Stored inside the Trip record (marks on its List Entries, the rest on the Trip), so it syncs with the Trip and Sync Conflicts treat it like the rest of the Trip. Included in JSON export/import; not in CSV.
