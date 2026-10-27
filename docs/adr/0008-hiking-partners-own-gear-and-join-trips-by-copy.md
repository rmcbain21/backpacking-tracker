# Hiking Partners own gear and Gear Lists, and join a Trip by copy

The spec ruled out other users, sharing, splitting group gear and combining Gear Lists into one Trip. The backpacker does pack with others (a brother), lends them gear, and wants a checklist for them and a list of what to bring for them when meeting at the trailhead. We now allow a **Hiking Partner**: a name the backpacker adds, who owns a Gear Closet and Gear Lists inside the backpacker's own data but never signs in. Every Gear Item and Gear List gets an owner. A Hiking Partner's Gear List can include the backpacker's gear, which is how borrowing is recorded. A Hiking Partner joins a Trip through a copy of one of their Gear Lists, a partner Packing List, following the same copy-then-edit rule as the backpacker's own (and refresh, ADR 0005).

## Considered Options

- **A plain checklist per person on the Trip, no catalogue**: rejected; the same partner comes on several Trips, and borrowing needs their list to point at the backpacker's real Gear Items.
- **A separate catalogue per person**: rejected; an owner on Gear Item reuses every gear feature (Gear Type, Tags, Custom Fields, Retire, Compare) instead of duplicating them.
- **Real second users with their own sign-in and sync**: rejected; still out of scope.
- **Splitting group gear (my tent carried by my brother leaves my weight automatically)**: still out of scope; the backpacker removes it from their own Packing List.

## Consequences

- Weight is optional on a Hiking Partner's gear; their totals show from known weights. The backpacker's own weight stays required.
- Only the backpacker's Packing List feeds Trip weights, stats, the Meal Plan and the Trip Review. Borrowed gear on a partner's list doesn't count in the backpacker's weight.
- The same Gear Item of the backpacker's can end up on two Packing Lists on one Trip; the app warns but doesn't block.
- Existing Gear Items and Gear Lists are read as owned by the backpacker.
