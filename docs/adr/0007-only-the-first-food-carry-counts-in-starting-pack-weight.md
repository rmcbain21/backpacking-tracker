# Only the first Food Carry's food counts in Starting Pack Weight

Starting Pack Weight is what the backpacker carries at the trailhead, but Consumable Weight used to include the food of the whole Meal Plan. That is right for a 4-day trip and badly wrong for a thru hike: 30 days of food is about 60 lb, while the pack never holds more than one resupply's worth. Now a Trip Day can be marked as a Resupply, splitting the Trip into Food Carries, and Consumable Weight (and so Starting Pack Weight and the Category breakdown's food line) counts only the first Food Carry's food. A Trip with no Resupply is one Food Carry, so its weight is unchanged.

## Considered Options

- **Keep counting the whole Meal Plan**: rejected; it makes Starting Pack Weight meaningless on any Trip with a resupply.
- **A live "pack weight now" that falls as food is eaten (idea 26)**: rejected as the headline figure; Starting Pack Weight stays a single trailhead number. Each Food Carry shows its own food weight, and the Today tab shows food left in the current one, which gives the rise and fall without changing the definition.
- **Flag individual slot entries as "not carried from the start"**: rejected; marking the day where food is bought covers the common case (town food, mail drops) with one switch.

## Consequences

- Marking or unmarking a Resupply changes Starting Pack Weight, so the switch locks with the Meal Plan when the Trip is Completed.
- Food eaten in town on the evening before a Resupply day is in the earlier Food Carry and still counts as carried; accepted.
- Fuel and other consumable Packing List entries still count in full; restocking them at a Resupply is out of scope.
