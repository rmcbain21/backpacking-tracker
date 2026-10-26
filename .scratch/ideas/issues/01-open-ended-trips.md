# 01: Open-ended Trips for thru hikes

**Status:** ready-for-agent

**Idea:** A thru hike often has no known finishing day, so a fixed start and end date doesn't fit. A "thru hike" mode would let me start a Trip with only a start date and add Trip Days one at a time as I go. Optionally I set an expected total distance (e.g. 2,650 mi for the PCT) so stats can show how far along I am.

**Open questions:**
- Is this a new kind of Trip, or just a Trip whose end date is left blank until it's Completed?
- How does adding a day interact with Skipped days and Meal Plans?
- Where does "expected total distance" show up: progress on the Trip page, miles remaining, average per day?
- Does the Trip need a planned-end estimate (expected distance ÷ average daily distance)?

## Comments

**Triage (2026-10-26):** ready-for-agent, after a grilling session. New glossary terms: **Open-ended Trip** and **Expected Distance**.

*How it's stored*
- An Open-ended Trip is a normal Trip with an open-ended flag. The end date stays required and always equals the last Trip Day so far, so everything that already uses the end date (`Trip.DaysFor`, the date-change preview, `MoveTripAsync`, the day repair in `TripPlanner.LoadTripsAsync`, the sync conflict view) keeps working unchanged. No ADR needed; no existing rule changes.
- The end date is hidden on screen, never shown or picked: the Trip label reads "Started 15 Apr · day 40 so far" (`TripDates.cs`), and the Change Dates dialog shows only the start date. Each Trip Day still shows its own date.

*Turning it on and off*
- An "Open-ended" switch when creating a Trip hides the end date picker; the Trip starts with just day 1.
- The switch can be turned on or off any time while the Trip is Planned. Turning it off keeps the current last day as a normal end date and shows the date pickers again.
- Completing an Open-ended Trip turns the flag off, so it ends on its last day. Reopening doesn't turn it back on (the backpacker can).

*Adding and removing days*
- "Add day" adds the next date after the last day (no picking a date, so no gaps). Underneath this is the same as changing the end date to end + 1.
- When adding, an optional "copy meals from the previous day"; otherwise the new day starts with empty meal slots.
- "Remove last day" removes the last Trip Day, with the existing date-change warning if that day has content (route, meals, notes). Can't remove day 1.
- Moving the start date works as it does now.

*Expected Distance and progress*
- An optional Expected Distance on any Trip, not just Open-ended ones (stored metric, shown in the chosen units, ADR 0002).
- Progress on the Trip page and in its stats uses actual distance only: "1,032 of 2,650 mi (39%), 1,618 mi to go".

*Finish estimate (Open-ended Trips with an Expected Distance only)*
- Pace = total actual distance ÷ number of non-Skipped Trip Days so far. Zero days (hiked days with 0 distance) count and pull the pace down; Skipped days don't count.
- Shown only once at least 3 hiked days have an actual distance: "At your pace: about 2 Sep (≈ 60 days to go)".

*Elsewhere*
- Itinerary text (02): on an Open-ended Trip, the expected-finish line uses the estimate ("Expected finish: about 2 Sep at current pace"), and is left out when there is no estimate yet.
- Trip Status is unchanged: an Open-ended Trip stays Planned for its whole length, so its weights follow the current Gear Items (correcting a weight in July changes April's Starting Pack Weight). Accepted for now; to revisit with mid-Trip gear changes (see 24).
- Changing gear partway through (e.g. picking up a bear canister for the Sierra) is out of scope here; noted on 24.
- The flag and Expected Distance are included in JSON export/import and sync.
