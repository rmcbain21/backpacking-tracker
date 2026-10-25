# 23: Google OAuth client setup

**What to build:** The Google side of Drive sync exists so the app can sign in: a Google Cloud project with the Drive API enabled and an OAuth client authorised for the app's GitHub Pages address. Only a human can do this in the Google Cloud console.

**Blocked by:** 02 (Deploy to GitHub Pages)

**Status:** ready-for-human

- [x] A Google Cloud project exists with the Google Drive API enabled.
- [x] The OAuth consent screen is configured for personal use (testing mode with the user as a test user is fine) with only the `drive.appdata` scope.
- [x] A web OAuth client exists with the GitHub Pages origin, and the local development origin, as authorised JavaScript origins.
- [x] The client ID (not a secret) is recorded where the app's configuration reads it.
- Done 2026-10-25: Google Cloud project "Backpacking Tracker" with the Google Drive API enabled; Google Auth Platform set up as External, in Testing, with the owner's Gmail as the only test user and only the `drive.appdata` scope. Web client "Backpacking Tracker web" with authorised JavaScript origins `https://rmcbain21.github.io`, `http://localhost:5286` and `https://localhost:7087`, and no redirect URIs (Google Identity Services token popup).
- The client ID is in `src/BackpackingTracker.Web/wwwroot/appsettings.json` as `GoogleDrive:ClientId`, read through `builder.Configuration`. It isn't a secret; the client secret isn't used or stored anywhere.
- If the app's address or dev ports change, add the new origin under Google Auth Platform → Clients, or sign-in fails with `origin_mismatch`.
