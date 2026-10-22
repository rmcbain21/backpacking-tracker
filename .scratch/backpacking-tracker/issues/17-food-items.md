# 17: Food Items

**What to build:** I can keep a library of Food Items with a name, serving weight and calories per serving (plus optional protein and cost per serving), see each one's Calorie Density, tag them, and Retire or delete them with the same rules as gear.

**Blocked by:** 03 (Units display and settings), 06 (Edit, Retire and delete Gear Items)

**Status:** ready-for-agent

- [ ] I can add and edit a Food Item: name, serving weight, calories per serving (required); protein per serving and cost per serving (optional); Tags.
- [ ] Calorie Density is shown per Food Item (calories per oz in imperial, per 100 g in metric).
- [ ] Food Items can be searched by name and Tag.
- [ ] Retire, un-retire and the hard-delete guard work as for Gear Items, reusing the same reference-check approach.
- [ ] The Food page lists Food Items and works on phone widths.
- [ ] Core tests cover Calorie Density, Retire and the delete guard.

## Comments

- From ticket 06: Food Items and Meals follow the same Retire and delete rules as Gear Items (story 69). The Gear catalogue's pattern to copy: a Retired flag hidden from catalogue views but found by id, a deletion plan gathered from registered reference checks (Meals and Meal Plans use Food Items; Meal Plans use Meals), a tombstone on delete. `IGearItemReferences` is Gear-specific, so Food needs its own.
