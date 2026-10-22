# 05: Tags and gear search

**What to build:** I can put free-form Tags on a Gear Item (such as "winter" or "bear country") and find gear by searching its name, brand/model or Tags.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

- [x] A Gear Item can have zero or more Tags; Tags are trimmed and matched case-insensitively, with no duplicates on one item.
- [x] When tagging, existing Tags are suggested.
- [x] A search box filters Gear Items by name, brand/model and Tag (case-insensitive, partial match).
- [x] Search combines with the Category filter once both exist.
- [x] Tags never affect any weight figure.
- [x] Core tests cover tagging, de-duplication and search matching.

## Comments

- From ticket 04: the Gear page groups Gear Items by Category but has no Category *filter* yet. This ticket owns adding it (story 10, "filter by Category"), alongside search.
- Implemented. Tags are stored on the Gear Item (`GearItemDetails.Tags`), trimmed, blank ones dropped, and de-duplicated ignoring case. A new Tag matching one already in use (ignoring case) takes the existing spelling, so suggestions don't fork. Tag rules live in `TagRules`; `ListTagsAsync` gives suggestions.
- Search is case-insensitive and partial over name, brand/model and Tags; with several words, every word must match somewhere ("winter quilt"). Search and the Category filter combine through `GearFilter` on `ListGearItemsByCategoryAsync`.
- Tags can only be set when adding until ticket 06 adds editing.
- The Category filter has no option for the "No Category" section (which only appears after sync).
- Found while testing: a Tag typed but not yet added would have been lost when tapping Add; committing it on blur instead moved the Add button mid-tap. The form now asks the Tag editor for its Tags including any typed text when Add is tapped.
- Verified with a headless Edge test against the Release publish at phone width, using real clicks.
