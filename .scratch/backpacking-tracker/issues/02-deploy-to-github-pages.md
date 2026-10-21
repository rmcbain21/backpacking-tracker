# 02: Deploy to GitHub Pages

**What to build:** The app is live at a GitHub Pages address and updates automatically when main is pushed, so I can install it on my phone from a real URL rather than a dev machine.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

Note: the repository `rmcbain21/backpacking-tracker` already exists and is the `origin` remote. Check its visibility before enabling Pages (Pages on a private repo needs a paid plan).

- [x] A GitHub repository exists and is the `origin` remote; main is pushed.
- [x] A GitHub Actions workflow builds and publishes the Blazor app to GitHub Pages on push to main.
- [x] The published base path matches the Pages URL, and deep links and reloads work (no 404 on refresh of a sub-page).
- [x] The Pages site installs as a PWA on Android Chrome and works offline after the first visit.
- [x] The Pages URL is recorded in the repo docs (it is needed later for the Google OAuth client).

## Comments

- From ticket 01: the base path is hard-coded as `/` in two places that both need the Pages subpath: `<base href="/" />` in `src/BackpackingTracker.Web/wwwroot/index.html` and `const base = "/";` in `src/BackpackingTracker.Web/wwwroot/service-worker.published.js`.
- Implemented `.github/workflows/deploy.yml` (test, set base path, publish, copy `index.html` to `404.html`, deploy with `actions/deploy-pages`). Base path `/backpacking-tracker/` is set before publish because the service worker asset manifest hashes `index.html`; the service worker now derives its base from `self.registration.scope`. Pages URL recorded in `README.md`.
- Verified locally with a headless Edge test against a server that mimics Pages (site under `/backpacking-tracker/`, unknown paths get `404.html` with status 404): a fresh deep link to `/trips` starts the app, navigation stays under the base path, the manifest and service worker scope resolve under the base path, and an offline reload of `/food` loads with data.
- Still to do by hand: make the repository public, set Settings → Pages → Source to "GitHub Actions", push `main`, then install on the Android phone and check it works in airplane mode (this also closes ticket 01's last criterion).
- 2026-10-21: repository made public, Pages source set to GitHub Actions, first deploy succeeded. The same headless Edge checks pass against the live site (deep link, base path, manifest, service worker scope, offline reload with data). Remaining: install on the Android phone and check it in airplane mode.
- 2026-10-21: installed from the GitHub Pages URL on the Android phone (Chrome) and confirmed working offline.
