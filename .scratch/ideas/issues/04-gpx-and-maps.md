# 04: GPX tracks and maps

**Status:** ready-for-agent

**Idea:** Attach route information to a Trip: at least import a GPX file per Trip or Trip Day to get distance and elevation gain, and maybe show the route on a map.

**Open questions:**
- The spec lists "GPX import, live GPS tracking and maps" as out of scope. Revisit that decision, ideally with an ADR.
- Minimum useful version: import GPX and read distance/elevation into the Trip Day, with no map?
- Planned route, recorded track (e.g. from a Garmin export), or both? A recorded track could fill in actual distance.
- Where would GPX files live? Sync is through a cloud file store, so storing them alongside the records might work.
- Overlaps with 03 (coordinates for weather) and 02 (sharing a route).

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Decided in ADR 0004: import a GPX file onto a Trip Day to fill in its distance and elevation gain. No map, and the file isn't stored. The spec's out-of-scope line is updated.
- An "Import GPX" action on a Trip Day. A route fills in planned distance; a recorded track fills in actual distance and elevation gain. If the file has both, ask which to use.
- Show a preview of the values that will be set (and any it replaces) before applying, like the import preview.
- Elevation gain needs light smoothing of the track's elevation points so GPS noise doesn't inflate it; note the method in code.
- Test with exports from Gaia GPS (what the backpacker mainly uses) and Garmin.
- Later, maybe: embed a Gaia GPS shared map on the Trip by link, or keep the GPX files for a map. Not part of this ticket.
