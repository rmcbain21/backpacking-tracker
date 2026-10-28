# A merged Gear Item's tombstone remembers what it was merged into

Sync matches records by id, so the same piece of gear created on two devices (most often by importing the same CSV sheet on each before syncing) becomes two Gear Items. The backpacker can now Merge duplicates: keep one Gear Item, repoint every List Entry to it and delete the others (idea 30). A Merge happens on one device, but another device may add a merged item to a list before it syncs, leaving an entry pointing at a deleted Gear Item, which the app otherwise never allows. So a merged Gear Item's tombstone records the id of the item it was merged into, and after every sync or JSON import, any entry still pointing at it is repointed to that item.

## Considered Options

- **A plain tombstone, and treat entries pointing at deleted gear as broken**: rejected; every list, weight and checklist would need to cope with a missing Gear Item, and the backpacker would have to fix each one by hand.
- **A separate synced table of merges (A → B)**: rejected; it's a second record that must arrive with the tombstone, and the tombstone already exists for exactly this item.
- **Merge automatically when everything matches**: rejected; two identical "Stuff sack" items can be two stuff sacks, and there's no Undo.

## Consequences

- The Gear Item record gains an optional "merged into" id, only set on a tombstone. The sync file's format version doesn't change: older records simply don't have it.
- Repointing after sync is a local edit, so it can touch a Completed Trip's Packing List, the same as a Merge does. Frozen weights stay as they were.
- Merges can chain (A into B, later B into C); repointing follows the chain.
- A Gear Item in an unsettled Sync Conflict can't be merged, so settling can't bring back a version of an item already merged away.
