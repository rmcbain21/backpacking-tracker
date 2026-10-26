# 21: Icons for Carry Mode

**Status:** ready-for-agent

**Idea:** Show Carry Mode (base, worn, consumable) as icons instead of words on List Entries, to save space on a phone.

**Open questions:**
- Which icons read clearly without a label (backpack, shirt, fork or flame)?
- Keep a tooltip or long-press label, and a text label for screen readers.
- Icons only on the phone layout, or everywhere?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- Icons everywhere, not just on the phone layout, so the layout stays the same across screen sizes.
- Base: backpack. Worn: shirt (or similar clothing icon). Consumable: fork and knife. Use the closest MudBlazor Material icons.
- Each icon has a tooltip (long-press on a phone) with the Carry Mode name, and an aria-label for screen readers.
- Only the display changes; picking a Carry Mode works as it does now.
