# 13: Trip Days, Itinerary and date changes

**What to build:** A Trip has one Trip Day per calendar day from its start to end date. Each Trip Day holds where it starts and ends, the campsite, planned and actual distance, elevation gain and journal notes. I can read the Itinerary as all the days' route details in order. Changing the Trip's dates adds, keeps or removes days safely.

**Blocked by:** 12 (Trips from a Gear List)

**Status:** ready-for-agent

- [ ] A Trip from date A to date B has (B minus A plus 1) Trip Days, so a 3-night trip has 4 days.
- [ ] Each Trip Day has start, end, campsite, planned distance, actual distance, elevation gain (stored in metres) and journal notes, all optional.
- [ ] An Itinerary view shows the route details of all Trip Days in order, in the chosen units.
- [ ] The core offers a preview of a date change listing the Trip Days that would be removed and whether each has content.
- [ ] Applying a change that removes days with content requires explicit confirmation.
- [ ] Lengthening appends empty days; shifting dates without changing length re-dates days in order and keeps their contents.
- [ ] Core tests cover day generation, the preview, confirmation, lengthening, shortening and shifting.
