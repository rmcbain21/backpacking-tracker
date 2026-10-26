# 22: Loading indicator on the Trip page

**Status:** ready-for-agent

**Idea:** The Trip page takes a second to load. Show a loading indicator (spinner or skeleton) so it doesn't look stuck.

**Open questions:**
- First check why it's slow: if the delay can be cut (e.g. by loading less up front), that may beat adding a spinner.
- Spinner, or a skeleton layout of the page?
- Do any other pages need the same?

## Comments

**Triage (2026-10-26):** ready-for-agent.

- First measure why the Trip page is slow (what's loaded and computed before the first render) and cut the delay where that's cheap, e.g. render the page first and load the slower sections after.
- If it still takes a noticeable moment, show a skeleton layout of the page (MudBlazor `MudSkeleton`) instead of a spinner.
- Note in the ticket what was slow and what changed. Check whether other big pages (Gear List, Meal Plan) have the same delay and say so, but only fix the Trip page here.
