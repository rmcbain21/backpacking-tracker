# 02: Deploy to GitHub Pages

**What to build:** The app is live at a GitHub Pages address and updates automatically when main is pushed, so I can install it on my phone from a real URL rather than a dev machine.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

Note: the repository `rmcbain21/backpacking-tracker` already exists and is the `origin` remote. Check its visibility before enabling Pages (Pages on a private repo needs a paid plan).

- [x] A GitHub repository exists and is the `origin` remote; main is pushed.
- [ ] A GitHub Actions workflow builds and publishes the Blazor app to GitHub Pages on push to main.
- [ ] The published base path matches the Pages URL, and deep links and reloads work (no 404 on refresh of a sub-page).
- [ ] The Pages site installs as a PWA on Android Chrome and works offline after the first visit.
- [ ] The Pages URL is recorded in the repo docs (it is needed later for the Google OAuth client).

## Comments

- From ticket 01: the base path is hard-coded as `/` in two places that both need the Pages subpath: `<base href="/" />` in `src/BackpackingTracker.Web/wwwroot/index.html` and `const base = "/";` in `src/BackpackingTracker.Web/wwwroot/service-worker.published.js`.
