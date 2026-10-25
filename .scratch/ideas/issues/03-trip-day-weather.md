# 03: Weather on Trip Days

**Status:** needs-triage

**Idea:** Save the weather for each Trip Day, either by entering it myself (temperature range, conditions, notes) or by fetching it from a weather API.

**Open questions:**
- Manual entry only, API only, or manual with an optional fetch?
- Forecast before the Trip, actual weather afterwards, or both?
- An API needs a location per Trip Day. Today Trip Days only have text start/end/campsite names, not coordinates. Open-Meteo is free with no API key and has both forecast and historical data, so it's a likely candidate.
- The spec rules out any server of our own, so calls would come straight from the app, and would need to work (or fail quietly) offline on trail.

## Comments
