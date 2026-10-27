# Backpacking Tracker

A personal app for one backpacker to keep track of their gear, food and trips, with pack weight as the central concern.

## Gear

**Gear Item**:
A single physical piece of equipment owned by the backpacker or a Hiking Partner, such as a tent or a stove, known by its Name (such as "SWD Long Haul 50 EPLX") and its Gear Type.
_Avoid_: Item, product, equipment

**Gear Closet**:
The Gear Items one person owns: the backpacker's, or one Hiking Partner's. The Gear page shows one Gear Closet at a time.
_Avoid_: Catalogue, inventory

**Hiking Partner**:
Someone the backpacker packs with, such as a brother, who owns a Gear Closet and Gear Lists in the app but never uses it themselves. A Hiking Partner joins a Trip through a copy of one of their Gear Lists.
_Avoid_: User, companion, buddy

**Gear Type**:
What kind of gear a Gear Item is, such as Backpack, Tent or Water Bottle, typed freely by the backpacker. It belongs to the Gear Item, so it is the same on every list.
_Avoid_: Item type, kind, category

**Retired**:
The state of a Gear Item, Food Item or Meal that is no longer in use. It is hidden from new lists and plans but stays attached to the past Trips that used it.
_Avoid_: Deleted, archived

**Custom Field**:
A name, value and optional unit the backpacker adds to a Gear Item or Food Item for something the app has no field for, such as a sleeping pad's R-value or a food's sodium. On a Food Item it is a number per serving and is totalled like protein; on a Gear Item it can be a number or text and is used to compare gear of one Gear Type.
_Avoid_: Attribute, property, spec

**Category**:
A group on one Gear List or Packing List that its List Entries are arranged under, such as "Big Three + Sleeping Pad" or "Food and Water", used to break down pack weight. Each list has its own Categories, so renaming one on a list changes no other list, and the same Gear Item can be in different Categories on different lists.
_Avoid_: Section, group

**Default Categories**:
The backpacker's usual set of Categories, copied onto each new Gear List as its starting Categories. Changing them later doesn't change existing lists.
_Avoid_: Template, master categories

**Tag**:
A free-form label on a Gear Item, such as "winter" or "bear country", used to find gear. A Gear Item can have many Tags.
_Avoid_: Label, keyword

**Gear List**:
A reusable, named set of List Entries that serves as the starting point for a Trip's Packing List.
_Avoid_: Gearlist, template, kit, loadout

**Packing List**:
A Trip's own set of List Entries for one person, copied from a Gear List and then edited independently. A Trip has the backpacker's Packing List and one per Hiking Partner on it; only the backpacker's counts in the Trip's weights.
_Avoid_: Trip gear list, pack

**List Entry**:
One line on a Gear List or Packing List: a Gear Item, how many of it, its Carry Mode, and its Category on that list.
_Avoid_: Line item, row, pack item

**Carry Mode**:
How a List Entry is carried on that list: base, worn or consumable. It belongs to the List Entry, not the Gear Item, so the same jacket can be worn on one list and packed on another.
_Avoid_: Weight type, weight class

## Weight

**Base Weight**:
The total weight of List Entries whose Carry Mode is base: everything in the pack that is neither worn nor consumed.

**Worn Weight**:
The total weight of List Entries whose Carry Mode is worn: gear on the body rather than in the pack.

**Consumable Weight**:
The weight of things used up during the Trip: consumable List Entries such as fuel, the food in the Meal Plan up to the first Resupply (the first Food Carry; the whole Meal Plan when there's no Resupply), and any water carried.

**Starting Pack Weight**:
Base Weight plus Consumable Weight at the trailhead, before anything is used up. The headline weight figure for a Trip.
_Avoid_: Total weight, skin-out weight

## Food

**Food Item**:
A single food, with its serving weight and nutrition, such as a Clif bar or a packet of ramen.
_Avoid_: Food, ingredient

**Meal**:
A reusable, named combination of Food Items eaten together, such as a "Ramen Bomb".
_Avoid_: Recipe

**Meal Plan**:
The Meals and Food Items assigned to each Trip Day's slots (Breakfast, Lunch, Dinner, Snacks). A view across the Trip's Trip Days, not a separate list.
_Avoid_: Menu, food plan

**Calorie Density**:
Calories per unit of weight for a Food Item or Meal; the figure backpackers use to judge how efficient food is to carry.
_Avoid_: Cal/oz ratio

## Trips

**Trip**:
One specific outing, with its own Packing List, Trip Days and stats. Later changes to a Gear List never rewrite it.
_Avoid_: Hike, outing, adventure

**Open-ended Trip**:
A Planned Trip with no set end date, whose Trip Days are added one at a time as it's hiked (e.g. a thru hike). Its end date is always its last Trip Day so far.
_Avoid_: Thru-hike mode, ongoing, active

**Expected Distance**:
The total distance a Trip is expected to cover, used for its progress and, on an Open-ended Trip, the finish estimate.
_Avoid_: Target distance, goal

**Trip Status**:
Where a Trip is in its life: Planned or Completed. A Planned Trip reflects the current weights and nutrition of its gear and food; a Completed Trip has those values frozen.
_Avoid_: Active, in progress

**On Trail**:
A Planned Trip whose dates include today; an Open-ended Trip is On Trail from its start date until it's Completed. Worked out from the dates, not a Trip Status.
_Avoid_: Active, current, in progress

**Trip Day**:
One calendar day of a Trip, holding its route (start, end, campsite, planned and actual distance), its meal slots and its journal notes.
_Avoid_: Itinerary Day, leg, segment, stage

**Itinerary**:
The route details of a Trip's Trip Days, read in order. A view across the Trip Days, not a separate list.
_Avoid_: Route, plan

**Skipped**:
A Trip Day that was planned but not hiked, marked once the Trip has started instead of changing its dates, so the plan is kept. Stats leave it out of what was hiked, but its planned distance and its food still count.
_Avoid_: Cancelled, missed, zero day

**Resupply**:
A Trip Day marked as where food is restocked before that day's hiking, such as a town or a mailed box, optionally with the place's name. Never the first Trip Day.
_Avoid_: Town stop, mail drop

**Food Carry**:
The run of Trip Days whose food is bought at one place: from the start of the Trip, or from a Resupply, up to the day before the next Resupply. A Trip with no Resupply is one Food Carry. Only the first Food Carry's food counts in Consumable Weight.
_Avoid_: Stretch, leg, section

**Trip Review**:
The backpacker's look back at a Trip as a whole: which gear went unused, what they wished they'd brought, a closing note and a rating. Can be filled in once the Trip has started and still edited after it's Completed.
_Avoid_: Post-mortem, debrief, retro

## Sync

**Sync Conflict**:
A record changed on two devices since they last synced, which the backpacker must settle by choosing one version.
_Avoid_: Merge conflict, collision
