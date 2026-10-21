# backpacking-tracker
Application to track backpacking gearlists, meals and itineraries

## Live app

https://rmcbain21.github.io/backpacking-tracker/

Deployed to GitHub Pages by `.github/workflows/deploy.yml` on every push to `main`. This URL is also the authorised JavaScript origin for the Google OAuth client (origin `https://rmcbain21.github.io`).

## Running locally

```
dotnet run --project src/BackpackingTracker.Web
dotnet test BackpackingTracker.slnx
```

Offline caching only happens in a published build (`service-worker.published.js`); `dotnet run` uses a service worker that caches nothing.

## Hosting notes

- The app is served from the `/backpacking-tracker/` subpath. The workflow rewrites `<base href="/" />` in `wwwroot/index.html` **before** `dotnet publish`, because the service worker's asset manifest holds a hash of `index.html`; editing it after publishing breaks offline install. The service worker takes its base URL from its own registration scope.
- GitHub Pages serves `404.html` for unknown paths, so the workflow copies `index.html` to `404.html` and deep links and reloads of sub-pages still start the app.
- One-time repository setup: the repository is public, and **Settings → Pages → Build and deployment → Source** is set to **GitHub Actions**.
