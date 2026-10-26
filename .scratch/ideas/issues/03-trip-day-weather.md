# 03: Weather on Trip Days

**Status:** ready-for-agent

**Idea:** Save the weather for each Trip Day, either by entering it myself (temperature range, conditions, notes) or by fetching it from a weather API.

**Open questions:**
- Manual entry only, API only, or manual with an optional fetch?
- Forecast before the Trip, actual weather afterwards, or both?
- An API needs a location per Trip Day. Today Trip Days only have text start/end/campsite names, not coordinates. Open-Meteo is free with no API key and has both forecast and historical data, so it's a likely candidate.
- The spec rules out any server of our own, so calls would come straight from the app, and would need to work (or fail quietly) offline on trail.

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Manual entry only for now. No API calls.
- On each Trip Day, all optional: low and high temperature (stored metric, shown in the chosen units, ADR 0002), conditions picked from a short list (sunny, partly cloudy, cloudy, rain, snow, wind, can pick more than one) and a weather note.
- Can be filled in before the Trip (forecast) or after (what happened); there's one set of fields, not separate forecast/actual.
- Included in JSON export/import, sync and the itinerary text (02) when filled in.
- Later: fetching from Open-Meteo once Trip Days have a location (a GPX track's start point, 04, could give one). Not part of this ticket.
