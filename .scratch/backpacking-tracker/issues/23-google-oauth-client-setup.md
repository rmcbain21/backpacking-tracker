# 23: Google OAuth client setup

**What to build:** The Google side of Drive sync exists so the app can sign in: a Google Cloud project with the Drive API enabled and an OAuth client authorised for the app's GitHub Pages address. Only a human can do this in the Google Cloud console.

**Blocked by:** 02 (Deploy to GitHub Pages)

**Status:** ready-for-human

- [ ] A Google Cloud project exists with the Google Drive API enabled.
- [ ] The OAuth consent screen is configured for personal use (testing mode with the user as a test user is fine) with only the `drive.appdata` scope.
- [ ] A web OAuth client exists with the GitHub Pages origin, and the local development origin, as authorised JavaScript origins.
- [ ] The client ID (not a secret) is recorded where the app's configuration reads it.
