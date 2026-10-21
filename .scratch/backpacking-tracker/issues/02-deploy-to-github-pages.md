# 02: Deploy to GitHub Pages

**What to build:** The app is live at a GitHub Pages address and updates automatically when main is pushed, so I can install it on my phone from a real URL rather than a dev machine.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

Note: creating the GitHub repository is outward-facing. Confirm the repository name and visibility with the user before creating it and adding the remote.

- [ ] A GitHub repository exists and is the `origin` remote; main is pushed.
- [ ] A GitHub Actions workflow builds and publishes the Blazor app to GitHub Pages on push to main.
- [ ] The published base path matches the Pages URL, and deep links and reloads work (no 404 on refresh of a sub-page).
- [ ] The Pages site installs as a PWA on Android Chrome and works offline after the first visit.
- [ ] The Pages URL is recorded in the repo docs (it is needed later for the Google OAuth client).
