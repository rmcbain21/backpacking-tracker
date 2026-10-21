# 16: Trip stats

**What to build:** Each Trip shows its stats: days and nights, total planned versus actual distance, total elevation gain, Starting Pack Weight and Base Weight, so I can review and compare trips. Food stats come in ticket 20.

**Blocked by:** 14 (Packing checklist and water carried), 15 (Completing and reopening Trips)

**Status:** ready-for-agent

- [ ] Days equals the number of Trip Days; nights equals days minus 1.
- [ ] Planned and actual distance are the sums over Trip Days; elevation gain is the sum over Trip Days.
- [ ] Starting Pack Weight and Base Weight come from the weight calculator, using frozen values on Completed Trips.
- [ ] The stats are shown on the Trip in the chosen units, grouped into calculated weight stats and typed-in route stats.
- [ ] Core tests cover each stat for Planned and Completed Trips.
