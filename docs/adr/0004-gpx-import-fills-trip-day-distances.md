# GPX import fills in Trip Day distances; no maps or stored tracks

The spec first ruled out GPX import along with live GPS tracking and maps. We now allow one narrow part of it: importing a GPX file onto a Trip Day to fill in its distance and elevation gain. A route (planned in e.g. Gaia GPS) fills in planned distance; a recorded track (e.g. a Garmin or Gaia export) fills in actual distance and elevation gain. The app reads the file, shows what it will set, and keeps only those numbers; the GPX file itself isn't stored. Typing distances in by hand is the slow part of logging a Trip, and Trip Days already have the fields, so this adds little new data.

## Considered Options

- **Keep GPX out of scope**: rejected; the distances already exist in the backpacker's GPS app, and retyping them is error-prone.
- **Store the GPX file alongside the Trip in the cloud file store**: not now; it makes sync files much larger and is only worth it once there's a map to show it on.
- **Show the route on a map**: not now. Maps and live tracking stay out of scope. Embedding a Gaia GPS shared map by link may be looked at later.

## Consequences

- Elevation gain worked out from a GPX file depends on how it's smoothed and can differ from what Gaia or Garmin show; the preview lets the backpacker see the number before it's saved.
- With no stored track, a later map feature would need the GPX files imported again (or a link to the route in Gaia).
- A track's start point could later give a Trip Day a location, e.g. for fetching weather (idea 03).
