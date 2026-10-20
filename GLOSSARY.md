# Backpacking Tracker

A personal app for one backpacker to keep track of their gear, food and trips, with pack weight as the central concern.

## Gear

**Gear Item**:
A single physical piece of equipment the backpacker owns, such as a tent or a stove.
_Avoid_: Item, product, equipment

**Retired**:
The state of a Gear Item, Food Item or Meal that is no longer in use. It is hidden from new lists and plans but stays attached to the past Trips that used it.
_Avoid_: Deleted, archived

**Category**:
The one group a Gear Item belongs to, such as Shelter, Sleep or Cook, used to break down pack weight. The backpacker defines their own Categories.
_Avoid_: Type, group

**Tag**:
A free-form label on a Gear Item, such as "winter" or "bear country", used to find gear. A Gear Item can have many Tags.
_Avoid_: Label, keyword

**Gear List**:
A reusable, named set of List Entries that serves as the starting point for a Trip's Packing List.
_Avoid_: Gearlist, template, kit, loadout

**Packing List**:
A Trip's own set of List Entries, copied from a Gear List when the Trip is created and then edited independently.
_Avoid_: Trip gear list, pack

**List Entry**:
One line on a Gear List or Packing List: a Gear Item, how many of it, and its Carry Mode.
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
The weight of things used up during the Trip: consumable List Entries such as fuel, the food in the Meal Plan, and any water carried.

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

**Trip Status**:
Where a Trip is in its life: Planned or Completed. A Planned Trip reflects the current weights and nutrition of its gear and food; a Completed Trip has those values frozen.
_Avoid_: Active, in progress

**Trip Day**:
One calendar day of a Trip, holding its route (start, end, campsite, planned and actual distance), its meal slots and its journal notes.
_Avoid_: Itinerary Day, leg, segment, stage

**Itinerary**:
The route details of a Trip's Trip Days, read in order. A view across the Trip Days, not a separate list.
_Avoid_: Route, plan

## Sync

**Sync Conflict**:
A record changed on two devices since they last synced, which the backpacker must settle by choosing one version.
_Avoid_: Merge conflict, collision
