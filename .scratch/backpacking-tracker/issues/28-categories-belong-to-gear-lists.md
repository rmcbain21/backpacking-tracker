# 28: Categories belong to Gear Lists

**What to build:** Each Gear List has its own Categories, starting as a copy of my Default Categories, and its List Entries are arranged under them like the sections of my trip sheet. I add gear into a Category, move entries between Categories, and see each Category's subtotal and the pack weight breakdown by the list's Categories. See ADR 0003 and GLOSSARY.md (Category, Default Categories, List Entry).

**Blocked by:** 27 (Gear Type and Name)

**Status:** ready-for-agent

- [x] The Categories page edits the Default Categories (add, rename, reorder, remove). Changing them doesn't change existing Gear Lists.
- [x] A new Gear List starts with a copy of the Default Categories. On a Gear List I can add, rename, reorder and remove its own Categories; this changes no other list and not the Default Categories. Removing a Category that has entries asks which Category to move them to.
- [x] Every List Entry is in one of its list's Categories. The Gear List page shows entries under their Category headings in the list's Category order, sorted by Name within a Category; each heading shows the Category's subtotal including worn entries. Categories with no entries are shown (so they can be added to) but collapsed or marked empty.
- [x] Each Category heading has a + that opens the Add gear picker with that Category chosen; the list's main + opens the same picker with a Category dropdown of the list's Categories. The dropdown can be changed before adding. An entry has a Category picker to move it.
- [x] Adding a Gear Item already on the list (in any Category) adds one to that entry's quantity.
- [x] The breakdown ("Where the pack weight goes") splits the Starting Pack Weight by the list's Categories, worn entries left out (as in ticket 09).
- [x] Existing Gear Lists (from ticket 08) get a copy of the Default Categories, and each existing entry goes in the Category its Gear Item used to have, or the first Category.
- [x] Core tests cover copying the Default Categories, editing a list's Categories independently of others and of the defaults, adding into a Category, moving entries, removing a Category with entries, subtotals and the breakdown.

## Comments

- Implemented: `DefaultCategory` (stored, Default Categories page) and `Lists.Category` (inside each Gear List); entries carry `CategoryId`; sections with subtotals (worn included); breakdown by list Categories. Old lists are converted once on first read and saved. Verified with a headless Edge test at phone width.
- For ticket 29: it needs a way to create a Gear List with its own Categories and entries in one save; the fixed-column change is already done (ticket 27).
- For ticket 24: list Categories are copied with the default ids, so never match Categories by id across lists; `LegacyCategoryId` stays on old Gear Items and should be left out of the sync file.
