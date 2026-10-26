# 02: Share an Itinerary as text

**Status:** ready-for-agent

**Idea:** Copy a plain-text summary of a Trip to paste into a message for someone back home: dates, each Trip Day's start, end and campsite, expected finish, and optionally a Garmin tracking link (e.g. an inReach MapShare URL stored on the Trip).

**Open questions:**
- The spec puts "sharing Trips" out of scope. That meant accounts and other users; copying text is a one-way share, so it probably doesn't conflict, but confirm.
- Which details go in by default? Exit route, car location, emergency contact, "call SAR if not heard from by…"?
- Is the Garmin link a field on the Trip, or a saved setting reused across Trips?
- Copy to clipboard only, or also the phone's share sheet?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- In scope: copying text isn't sharing with other users. The spec's out-of-scope line now says so.
- A "Share itinerary" action on a Trip opens the phone's share sheet, falling back to copy to clipboard where there isn't one.
- The text: Trip name and dates; each Trip Day's date, start → end and campsite, with Skipped days marked; the expected finish date.
- Two new optional fields on the Trip: a tracking link (e.g. an inReach MapShare URL) and a "Contact search and rescue if not heard from by" date/time. Each is included only when filled in.
- No exit route, car location or emergency contact fields for now.
- Plain text that reads well in a text message: no Markdown, short lines.
