# Offline-first Blazor PWA with no server, synced through Google Drive

The app is a Blazor WebAssembly PWA hosted as static files on GitHub Pages, with no backend of our own. Each device keeps its data in IndexedDB (via Dexie behind a C# storage interface) and works fully offline, because the app is most useful on trail with no signal. Backup and cross-device sync go through a single data file in the backpacker's own Google Drive app-data folder, merged per record (the most recent edit wins, with Sync Conflicts settled by the backpacker), which avoids running and paying for a server for a single-user app.

## Considered Options

- **A backend with its own database**: rejected; hosting, auth and maintenance cost for one user.
- **TypeScript + React**: a smaller download and more mature offline tooling, but Blazor was chosen to build skills used at work.
- **SQLite in the browser via EF Core**: familiar data access, but a younger and less-proven path than IndexedDB.

## Consequences

- The cloud drive sits behind a storage-provider interface (read file, write file, last modified), so OneDrive or another provider can be added by writing one adapter.
- Integrity rules a database would enforce, such as not deleting gear a Trip uses, live in the domain code.
