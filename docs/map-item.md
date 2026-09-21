# Map item plan

Testaldi's first custom item is a holdable map intended to appear on floors 1 and 2.

## Implementation plan

- Use the current Baldi's Basics Plus Dev API as a hard dependency.
- Clone the game's existing Map item component rather than recreating map behavior from scratch.
- Give the clone its own extended item enum, name/description, and sprites.
- Add the item to the procedural generator's potential-items list on F1 and F2.
- Keep it as a normal inventory item.
- Do not copy the game's proprietary DLLs or assets into this repository.

The repository currently declares Dev API 11.1.1.0 as the minimum runtime API. The actual game/API DLL references remain local build dependencies and are not committed.
