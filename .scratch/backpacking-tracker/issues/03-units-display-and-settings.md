# 03: Units display and settings

**What to build:** Weights show in imperial by default (ounces for small values, lb + oz for larger totals), with a settings switch to metric. When I type a weight I can enter it in oz, lb or g, and it is stored in grams. Distances (miles/km) and elevations (feet/m) use the same setting once they exist. See ADR 0002.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

- [ ] A Settings record holds the display unit system; it defaults to imperial.
- [ ] The core's units module converts and formats stored metric values for display: weights under 1 lb in oz to one decimal, larger totals in lb + oz; metric shows g/kg; distance in miles or km; elevation in feet or metres.
- [ ] Weight input accepts oz, lb or g and is converted to grams (0.1 g precision); invalid input is rejected with a clear message.
- [ ] Round-tripping a typed imperial value displays the value I typed (to one decimal).
- [ ] A Settings page lets me switch units, and Gear Item weights redisplay immediately without any stored value changing.
- [ ] Core tests cover conversion, formatting thresholds, parsing and round-trips.
