# A Planned Trip's Packing List can be refreshed from a Gear List by hand

The spec first said a Packing List is copied from a Gear List once and that later Gear List edits never change it, with no link back beyond the Gear List's name. We keep the first half: a Gear List edit still never changes a Packing List on its own. But a Planned Trip can now be refreshed from a Gear List on request, and the Trip remembers which Gear List it came from by id. The backpacker often keeps improving a Gear List after creating a Trip from it (adding the bear canister, swapping the stove) and wants those changes on the upcoming Trip without redoing them by hand. They also sometimes want to start the Trip's Packing List again from a different Gear List when plans change.

The refresh shows a preview with one line per difference: entries to add, entries to change (quantity, Carry Mode, Category) and entries to remove. Each line has its own tick. Removals start unticked, because without a snapshot of the Gear List the app can't tell an entry the backpacker added to the Trip from one removed from the Gear List. Ticking everything makes the Packing List match the Gear List exactly, so "start again from another Gear List" is the same action.

## Considered Options

- **Keep Packing Lists fully independent**: rejected; re-applying Gear List changes to an upcoming Trip by hand is tedious and easy to get wrong.
- **Live link (the Packing List follows the Gear List)**: rejected; a Trip has to be tweakable on its own, and a Completed Trip has to stay a fixed record.
- **Replace only**: rejected; it throws away the Trip's own tweaks every time.
- **Store a snapshot of the Gear List on the Trip for a true three-way merge**: rejected for now; more data in every Trip for a case the per-line ticks already cover.

## Consequences

- Trips store their source Gear List id alongside its name. Older Trips have only the name; their refresh picker starts with nothing selected.
- Only a refresh changes the source; "Save as Gear List" doesn't.
- Completed Trips can't be refreshed (their Packing List is locked until Reopen), so frozen records are never rewritten.
- Categories are matched by name across the two lists (ADR 0003), so a Category renamed on the Gear List counts as a different Category.
