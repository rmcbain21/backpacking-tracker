# Custom Field units are labels, never converted

ADR 0002 stores weights and distances in metric and shows them in the backpacker's chosen units. Custom Fields don't follow it: a Custom Field's unit ("mg", "in", "°F") is a label the backpacker types, shown exactly as typed, and the app never converts between units. Converting would mean the app knowing every unit someone might type, and most Custom Fields (R-value, sodium in mg) have no imperial/metric pair anyway. To keep totals and comparisons honest, values are combined only when their name and unit both match. Picking a suggested name fills in the unit last used with it, so mismatches stay rare.

## Considered Options

- **Convert known units (in/cm, °F/°C, oz/g)**: rejected; a short list of known units adds rules for little gain, and anything outside the list would still be a label.
- **No units, just numbers**: rejected; "Length: 72" is ambiguous, and the unit is what tells two sodium fields apart.

## Consequences

- A Gear Item with "Length: 183 cm" and another with "Length: 72 in" show in separate Compare columns, as do "Sodium mg" and "Sodium g" totals: a visible hint to fix one.
- Switching display units to metric doesn't change Custom Field values.
