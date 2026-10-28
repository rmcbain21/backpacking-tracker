# 24: Hiking Partners and their gear

**Status:** ready-for-agent

**Idea:** Add another person's gear (e.g. my brother's) to a Trip, mostly to use as a checklist when packing together. Maybe as separate "gear closets", one per person.

**Open questions:**
- The spec rules out other users, sharing Gear Lists, splitting group gear, and combining several Gear Lists into one Trip. This needs an ADR revisiting that.
- Smallest version: a second checklist on the Trip that doesn't count toward my Starting Pack Weight?
- Do the other person's Gear Items live alongside mine (with an owner), or in a separate closet?
- Does it need their weights at all, or just a checklist?

## Comments

**Note (2026-10-26, from grilling 01):** Think about this together with changing gear partway through a long Trip, e.g. picking up a bear canister for the Sierra and sending the puffy home in Oregon. Both mean one Trip with more than one set of gear (mine + my brother's, or section 1 + section 2), so one design might cover both. Also revisit there whether an Open-ended Trip's weights should stop following the current Gear Items once it's under way.

**Triage (2026-10-27):** ready-for-agent, after a grilling session. Retitled from "Include someone else's gear in a Trip". New glossary terms: **Hiking Partner** and **Gear Closet**; **Gear Item** and **Packing List** reworded. Revisits the spec's out-of-scope list: see ADR 0008. Spec updated (stories 99–102, data model, out of scope). Changing gear partway through a Trip, from the note above, is split out to **idea 29**. Builds on 15 (refresh) for partner Packing Lists; whichever lands second wires them together.

*Hiking Partners and Gear Closets*
- A **Hiking Partner** record: just a name. Rename any time. Delete only when none of their gear or lists are on any Trip, after a confirmation naming how many Gear Items and Gear Lists go with them; otherwise Retire their items instead.
- Every **Gear Item** has an owner: the backpacker or a Hiking Partner. Existing records are the backpacker's.
- The Gear page gets a switch at the top (**Me | Sam | …**) showing one Gear Closet at a time, default Me. Gear Type, Tags, Custom Fields (13), Retire, search, sort and Compare all work within the shown closet.
- Weight is **optional** on a Hiking Partner's Gear Items; the backpacker's stay required.
- CSV import (backlog tickets 07, 29) gets an owner picker so a partner's spreadsheet goes into their closet.

*Partner Gear Lists*
- Every **Gear List** has an owner; the Lists page gets the same **Me | Sam** switch.
- A partner's Gear List can hold that partner's gear **and the backpacker's** (borrowed). The backpacker's lists hold only their own gear.
- New partner Gear Lists start from the backpacker's Default Categories.
- Totals show from known weights, with "3 items without a weight" when some are missing.
- Quick swap (20) on a partner list offers that partner's gear plus the backpacker's.

*On a Trip*
- Add a Hiking Partner by picking one of their Gear Lists. It's **copied** into a partner Packing List on the Trip (same copy-then-edit rule as the backpacker's, and Refresh from Gear List from 15). Several partners per Trip; one Packing List each.
- Packing List and Checklist tabs show a section per person ("Me", "Sam"), each with its own packed ticks.
- Partner Packing Lists never feed the Trip's weights, stats, Meal Plan or Trip Review (23). No Meal Plan for partners.
- Completed: partner Packing Lists lock and their known weights freeze like the backpacker's; Reopen unlocks.

*Borrowed gear*
- The backpacker's Gear Items on a partner's Packing List are borrowed: labelled "from you" in the partner's section, and not counted in the backpacker's weight.
- The backpacker's **Checklist** gets a **"Bring for Sam"** group under their own items, listing those borrowed items, ticked as packed by the backpacker. This group is new work here.
- If one of the backpacker's Gear Items is on more than one Packing List on the same Trip, warn ("Jetboil is also on your Packing List"); don't block.

*Out of scope*
- Splitting group gear: carrying a partner's (or lending one's own) gear never moves weight automatically. Remove it from your own Packing List by hand.
- Sign-in or sync for Hiking Partners.

*From 30 (Merge duplicate Gear Items)*
- Duplicates must also have the same owner: the backpacker's Duplex and Sam's Duplex are two tents. Add this to 30's matching rule when owners arrive.
