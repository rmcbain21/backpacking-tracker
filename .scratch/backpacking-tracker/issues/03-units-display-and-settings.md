# 03: Units display and settings

**What to build:** Weights show in imperial by default (ounces for small values, lb + oz for larger totals), with a settings switch to metric. When I type a weight I can enter it in oz, lb or g, and it is stored in grams. Distances (miles/km) and elevations (feet/m) use the same setting once they exist. See ADR 0002.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

- [x] A Settings record holds the display unit system; it defaults to imperial.
- [x] The core's units module converts and formats stored metric values for display: weights under 1 lb in oz to one decimal, larger totals in lb + oz; metric shows g/kg; distance in miles or km; elevation in feet or metres.
- [x] Weight input accepts oz, lb or g and is converted to grams (0.1 g precision); invalid input is rejected with a clear message.
- [x] Round-tripping a typed imperial value displays the value I typed (to one decimal).
- [x] A Settings page lets me switch units, and Gear Item weights redisplay immediately without any stored value changing.
- [x] Core tests cover conversion, formatting thresholds, parsing and round-trips.

## Comments

- Implemented. Weight entry is a number field plus an oz / lb / g unit picker (agreed with the user), starting on oz for imperial and g for metric. Display formats: "4.3 oz", "1 lb 11.2 oz"; "765.4 g", "1.23 kg"; "12.4 mi" / "20.0 km"; "2,349 ft" / "716 m". Distance and elevation formatting is in the core, ready for Trip Days.
- Typed pounds display as lb + oz (1.7 lb shows "1 lb 11.2 oz"): the same weight, in the display format the spec asks for.
- Decimal commas ("1,5") are rejected rather than guessed, because "1,500" could mean 1.5 or 1500.
- The Settings record uses a fixed id so it syncs as one record; the spec's Records and schema section now says so.
- Fixed a ticket-01 bug found while testing: MudForm.ResetAsync sets bound text fields to null, so a second add with an empty brand/model crashed. Brand/model is now optional in the core, with a test.
- Verified with a headless Edge test against the Release publish at phone width (imperial default, invalid input message, oz and lb entry, switching to metric and back, metric surviving a reload).
