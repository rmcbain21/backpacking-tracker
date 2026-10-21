# 01: Walking skeleton: add and list Gear Items

**What to build:** The first end-to-end path through the whole app. I can open the app on my phone, install it to my home screen, add a Gear Item (name, brand/model, weight in grams), and see it in my gear list, still there after closing and reopening the app, with no signal. This ticket sets up the shape every later ticket builds on: the app core library and its public API, the test project with in-memory fakes, the record store port with its IndexedDB (Dexie) adapter, and the MudBlazor shell. See the spec's Implementation Decisions and ADR 0001.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A solution exists with the app core (plain C# library, no Blazor or JavaScript dependency), an xUnit test project, and a Blazor WebAssembly standalone app with PWA support on the current LTS .NET.
- [ ] The core depends only on the record store and clock ports; the tests use an in-memory record store and a controllable clock.
- [ ] Every stored record carries a GUID string `id`, a UTC `updatedAt` set from the clock, and a `deleted` tombstone flag.
- [ ] Weights are stored in grams with 0.1 g precision (ADR 0002).
- [ ] Through the core API I can add a Gear Item and list Gear Items; tests cover adding, listing and persistence through the record store.
- [ ] The record store adapter persists records in IndexedDB via Dexie through a thin JavaScript interop module; data survives an app reload.
- [ ] The app shell uses MudBlazor with bottom navigation (Gear, Lists, Trips, Food) on phone widths and a navigation drawer on tablet and desktop widths; Lists, Trips and Food can be placeholders.
- [ ] The Gear page lets me add a Gear Item and shows the list.
- [ ] The app is installable from Chrome on Android and loads and works offline after the first visit (service worker caches the app shell).
