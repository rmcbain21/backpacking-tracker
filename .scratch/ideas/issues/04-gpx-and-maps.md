# 04: GPX tracks and maps

**Status:** needs-triage

**Idea:** Attach route information to a Trip: at least import a GPX file per Trip or Trip Day to get distance and elevation gain, and maybe show the route on a map.

**Open questions:**
- The spec lists "GPX import, live GPS tracking and maps" as out of scope. Revisit that decision, ideally with an ADR.
- Minimum useful version: import GPX and read distance/elevation into the Trip Day, with no map?
- Planned route, recorded track (e.g. from a Garmin export), or both? A recorded track could fill in actual distance.
- Where would GPX files live? Sync is through a cloud file store, so storing them alongside the records might work.
- Overlaps with 03 (coordinates for weather) and 02 (sharing a route).

## Comments
