# 30: Merge duplicate Gear Items

**Status:** ready-for-agent

**Idea:** Found as a bug (2026-10-28): a test CSV import on one device had synced to Drive. Importing the same sheet later on another device created the same Gear Items with new ids, because CSV import only matches against the device's own gear. Syncing then brought in the test Trip and a second copy of every Gear Item. Sync matches records by id, so "Tent (A)" and "Tent (B)" are two records to it. Already flagged in ticket 24's notes ("sync before importing on a second device", or a duplicate check after merge). This gives the backpacker a way to find duplicate Gear Items and Merge them into one.

## Comments

**Triage (2026-10-28):** ready-for-agent, after a grilling session. New glossary term: **Merge**. New ADR 0009 (a merged Gear Item's tombstone remembers what it was merged into). Spec updated (story 104, Gear Item record).

Running example: after a sync there's **Tent "Duplex" (A)** on the test Trip and **Tent "Duplex" (B)** on the real Trip.

*What counts as a duplicate*
- Gear Items only. Food Items and Meals can be duplicated the same way but rarely in bulk. Build so Food Items could reuse it, but don't build that now.
- Same Gear Type and Name, ignoring case and spaces: the same rule CSV import uses to reuse a Gear Item. Weight, Tags, notes etc. may differ.
- Retired Gear Items are included.
- When Hiking Partners (24) land, add "same owner" to the rule: your Duplex and Sam's Duplex are two tents. Not part of this issue.

*Finding them*
- After a sync (or JSON import) that brings in new Gear Items, check for duplicates. If any groups are found that haven't been dismissed, show a banner on the Gear page: "3 possible duplicate Gear Items · Review". No top-bar badge; it isn't urgent like a Sync Conflict.
- A "Find duplicate gear" item in the Gear Closet's menu opens the same review page at any time. This also covers duplicates that already exist.

*Review page*
- One group per set of duplicates, items side by side (like a Sync Conflict), showing what uses each ("Gear List "Summer kit", Trip "JMT 2026"").
- Pick the item to keep. Default: the one used by more Gear Lists and Trips.
- **Merge**, after a confirm that says exactly what changes: "Keep Duplex (B). Duplex (A) will be deleted, and 1 Gear List and 2 Trips will use Duplex (B) instead." No Undo.
- **Not duplicates**: the group isn't offered again. Stored in a synced record, so other devices don't ask either. Remembered for that exact set of Gear Item ids: if a third "Stuff sack" turns up, the group is offered again.
- A group where any item is in an unsettled Sync Conflict is shown but blocked: "Settle its Sync Conflict first". Otherwise settling later could bring back a version of an item already merged away.

*What Merge does*
- The kept Gear Item stays whole, unchanged. No field-by-field merging; edit it afterwards if needed.
- Every other item in the group becomes a tombstone that records the kept item's id (ADR 0009).
- Every List Entry pointing at a merged item is repointed to the kept item: Gear Lists and Trips' Packing Lists, **Completed Trips included** (bypassing the Completed lock). Frozen weights on Completed Trips stay as they were, so history doesn't change.
- A list holding entries for both the kept item and a merged one keeps one entry: the kept item's (its Category, Qty, Carry Mode). Qty is not added. On a Packing List it's packed if either entry was. If the list only has merged items (A and C, keep B), the first entry is repointed and the others dropped.
- Saved as one batch of local changes, so the next sync sends them all.

*Sync safety*
- If another device adds A to a list before it syncs the Merge, that list ends up pointing at A's tombstone. After each sync (and JSON import), any List Entry pointing at a tombstoned Gear Item with a "merged into" id is repointed to that item (following a chain, A → B → C), applying the "one entry per list" rule above. That's a normal local edit, sent on the next sync.

*Naming in the code*
- The spec now calls sync's record-by-record step **Combine**, so **Merge** only means this. The sync code still says merge: `RecordMerge` (`WithBase`, `IncomingWinners`), `SyncMergeResult`, `MergedRecord`, `MergeChange`, and comments in `CloudSync.cs`. Rename them to Combine (`RecordCombine`, `SyncCombineResult`, etc.) as part of this work, before adding the Merge code beside them.

*Tests (core API, two devices where sync is involved)*
- Duplicates found by Gear Type and Name ignoring case and spaces; Retired included; different Gear Type not matched.
- Merge repoints Gear Lists, Planned and Completed Trips; frozen weights unchanged; both-on-one-list leaves one entry, Qty not added, packed kept.
- Not duplicates syncs to the other device; a new matching item reopens the group.
- Blocked while a Sync Conflict is pending on one of the items.
- Race: device 1 merges A into B, device 2 adds A to a list, both sync: the list ends up on B.
