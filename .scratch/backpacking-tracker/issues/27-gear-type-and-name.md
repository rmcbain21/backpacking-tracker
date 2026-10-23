# 27: Gear Type and Name

**What to build:** A Gear Item is known by its Name (such as "SWD Long Haul 50 EPLX") and its Gear Type (such as Backpack), instead of a name, a brand/model and a Category. The Gear page lists my gear sorted by Gear Type, and I can filter by Gear Type. My existing gear is converted without losing anything. See ADR 0003 and GLOSSARY.md (Gear Item, Gear Type).

**Blocked by:** None (reworks parts of 01, 04, 05 and 06)

**Status:** ready-for-agent

- [ ] A Gear Item has a Name (required) and a Gear Type (optional, free text). The separate Brand/Model field and the Gear Item's Category are gone.
- [ ] Gear Type is cleaned up like a Tag: trimmed, and a Gear Type matching one already in use (ignoring case) takes the existing spelling, so "power cord" becomes "Power Cord" if that exists. Gear Types in use are suggested when adding or editing.
- [ ] Existing Gear Items are converted when read: the old name becomes the Gear Type and the old brand/model becomes the Name (an item with no brand/model keeps its name as the Name and gets the old name as its Gear Type too). Nothing the backpacker typed is lost.
- [ ] The Categories that existed before (the defaults and any the backpacker made) become the Default Categories (ticket 28 uses them).
- [ ] The Gear page lists Gear Items sorted by Gear Type, then Name, each row showing the Name with the Gear Type beneath. Search matches Name, Gear Type and Tags. A Gear Type filter replaces the Category filter.
- [ ] Adding and editing a Gear Item ask for Name and Gear Type (with suggestions); no Category.
- [ ] Deleting, Retiring and the reference check are unchanged.
- [ ] Core tests cover Gear Type clean-up and suggestions, the conversion of old records, sorting, search and the Gear Type filter.
