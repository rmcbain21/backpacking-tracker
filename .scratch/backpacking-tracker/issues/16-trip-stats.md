# 16: Trip stats

**What to build:** Each Trip shows its stats: days and nights, total planned versus actual distance, total elevation gain, Starting Pack Weight and Base Weight, so I can review and compare trips. Food stats come in ticket 20.

**Blocked by:** 14 (Packing checklist and water carried), 15 (Completing and reopening Trips)

**Status:** ready-for-agent

- [x] Days equals the number of Trip Days; nights equals days minus 1.
- [x] Planned and actual distance are the sums over Trip Days; elevation gain is the sum over Trip Days.
- [x] Starting Pack Weight and Base Weight come from the weight calculator, using frozen values on Completed Trips.
- [x] The stats are shown on the Trip in the chosen units, grouped into calculated weight stats and typed-in route stats.
- [x] Core tests cover each stat for Planned and Completed Trips.

## Comments
- From ticket 13: sum `Trip.Days` (`PlannedDistanceMetres`, `ActualDistanceMetres`, `ElevationGainMetres`, each nullable) and format with `UnitFormat.Distance` / `Elevation`.
- From ticket 13: a Skipped status on Trip Days is planned in ticket 31; days and distances here may later leave out Skipped days.
- Implemented: `TripView.Stats` (`TripStats.For(trip, weight)`): `Days` (Trip Days count), `Nights` (days − 1), `PlannedDistanceMetres` / `ActualDistanceMetres` / `ElevationGainMetres` (sums over Trip Days of what's filled in; null when no day has that figure, so "not recorded" isn't shown as 0; a typed-in 0 counts), and `StartingPackGrams` / `BaseGrams` from the Trip's `PackWeight`, so a Completed Trip's are frozen and water carried is included. Listed Trips carry them too. Tests in `TripStatsTests`.
- Web: a fourth "Stats" tab on the Trip page: "4 days · 3 nights", then a Route card (typed in on the Itinerary; unrecorded totals show "—") and a Pack weight card (says whether it's from current weights or frozen). Trip tabs are now in sentence case with no letter-spacing and `MinimumTabWidth="64px"`, so all four fit at 390px without scroll arrows. Verified with a headless Edge test at phone width, light and dark.
