# 13: Trip Days, Itinerary and date changes

**What to build:** A Trip has one Trip Day per calendar day from its start to end date. Each Trip Day holds where it starts and ends, the campsite, planned and actual distance, elevation gain and journal notes. I can read the Itinerary as all the days' route details in order. Changing the Trip's dates adds, keeps or removes days safely.

**Blocked by:** 12 (Trips from a Gear List)

**Status:** ready-for-agent

- [x] A Trip from date A to date B has (B minus A plus 1) Trip Days, so a 3-night trip has 4 days.
- [x] Each Trip Day has start, end, campsite, planned distance, actual distance, elevation gain (stored in metres) and journal notes, all optional.
- [x] An Itinerary view shows the route details of all Trip Days in order, in the chosen units.
- [x] The core offers a preview of a date change listing the Trip Days that would be removed and whether each has content.
- [x] Applying a change that removes days with content requires explicit confirmation.
- [x] Changing the start or end keeps each day on its date: days outside the new dates are removed and new dates get empty days, at either end. (Changed from "lengthening appends empty days" after discussion, so a late start removes the first day, not the last.)
- [x] Moving the whole Trip keeps its length and re-dates every day in order with its contents.
- [x] Core tests cover day generation, the preview, confirmation, lengthening, shortening and moving.

## Comments

- Implemented: `TripDay` (date, start, end, campsite, planned/actual distance and elevation gain in metres, journal notes, all optional; `HasContent()`) kept in `Trip.Days`, one per date in order. `TripPlanner.UpdateDayAsync(tripId, date, TripDayDetails)` trims text, leaves blanks empty and refuses negative numbers. `PlanDateChangeAsync` returns a `DateChangePlan` (`AddedDates`, `RemovedDays` each with Day number and `HasContent`, `NeedsConfirmation`); `ChangeDatesAsync(..., removeDaysWithContent)` keeps each day on its date and refuses without confirmation when a removed day has content. `MoveTripAsync(tripId, newStart)` keeps the length and re-dates every day in order; it never removes anything. Trips saved before Trip Days get empty days on load. `Units/RouteInput` parses distances (mi/km) and elevation (ft/m) to metres, keeping stored values while the text is untouched. Tests in `TripDayTests` and `RouteInputTests`.
- Web: the Trip page has Packing List and Itinerary tabs; tapping a day opens `TripDayDialog`; the calendar button by the dates opens `ChangeDatesDialog`, which offers "Move the trip" (the default; the end follows the start) or "Change the start or end" (previews the days added and removed, and shows "Remove days and change" when days with content would go).
- Follow-up: marking a planned day that didn't happen as Skipped, once the Trip is under way, is ticket 31. Verified with a headless Edge test at phone width, light and dark.
