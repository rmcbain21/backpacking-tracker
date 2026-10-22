# 05: Tags and gear search

**What to build:** I can put free-form Tags on a Gear Item (such as "winter" or "bear country") and find gear by searching its name, brand/model or Tags.

**Blocked by:** 01 (Walking skeleton)

**Status:** ready-for-agent

- [ ] A Gear Item can have zero or more Tags; Tags are trimmed and matched case-insensitively, with no duplicates on one item.
- [ ] When tagging, existing Tags are suggested.
- [ ] A search box filters Gear Items by name, brand/model and Tag (case-insensitive, partial match).
- [ ] Search combines with the Category filter once both exist.
- [ ] Tags never affect any weight figure.
- [ ] Core tests cover tagging, de-duplication and search matching.

## Comments

- From ticket 04: the Gear page groups Gear Items by Category but has no Category *filter* yet. This ticket owns adding it (story 10, "filter by Category"), alongside search.
