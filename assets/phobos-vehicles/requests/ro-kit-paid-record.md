# The reverse-osmosis kit's pictures (26 Nov 2026)

**Request**: [ro-kit-paid.json](ro-kit-paid.json). **Why**: the owner's idea and cards of 26 Nov 2026, a reverse-osmosis kit for the water
bowser and the Ullage "with its own, unique, art", and a membrane cartridge item with its own picture, made with the kit's pilot
([work on the road](../../../docs/development/vehicle-road-work.md)). Both appear in the game world, so both went to the paid route
(`assets/AGENTS.md`).

| | |
| --- | --- |
| Engine | OpenAI image API, medium quality, 1024 by 1024, two tries each |
| Dry run | 4 images, at most about US$0.60 |
| Real run | `--max-usd 0.40`: 4 images made, **about US$0.242 spent** (cabinet US$0.066 each, cartridge US$0.055 each) |
| References | Our own art only: the Ullage's hose reel overlay and the Pintle bowser body (cabinet); our tyre set and drive belt items (cartridge), each with its named role in the request |
| AI disclosure | Every picture here is AI-generated (OpenAI), from the agent's prompts, picked by the owner |

## Review (the agent, at native size, before the owner saw them)

- **Cabinet 1**: a long, narrow steel cabinet with a louvred lid, two white membrane housings, blue pipes and a pump motor; seen
  straight down, no tilt, no text, no shadow outside. As asked.
- **Cabinet 2**: the same parts on a squarer frame with corner bolts and a tidier pipe run; straight down, clean.
- **Cartridge 1 and 2**: a white filter cartridge with dark end caps and a pale blue stub, at the parts' sixty-degree look; 2 has a
  ribbed, spoked end cap. Both clean; 1 reads more cleanly at 64 px.
- **Cards** drawn with `scripts/art/review_cards.py sprites` at their shipped sizes (cartridge 64 px, cabinet 96 px), on soil and stone:
  0 loose specks on all four. Offered on D'thorga Companion's Picks page too.
- The background AI had already looked at both picks (`clean_backgrounds.py masks --gpu`, the owner's go of 26 Nov 2026).

## The owner's picks (26 Nov 2026, by card)

- **Cabinet 2** ("squarer, neater pipes"), over the agent's lean, 1.
- **Cartridge 1** ("crisper"), the agent's lean.

## Shipped

`python assets/phobos-vehicles/export_ro_kit.py ship` (reproducible; `stage` draws the preview):

- The cabinet laid on each body by script, fitted into a box in the body's in-game pixels (north facing) and turned through its
  transform, so east and south register: **the bowser** across its rear deck in the dark box's place beside the hose reel (box 308, 974,
  404, 1046; the cabinet turned a quarter first, as a new derived file), **the Ullage** upright on the left frame strip between the
  brackets behind the cab (box 110, 656, 168, 734). Agent placement, to revise once seen in game.
- The cartridge at 64 px, and the kit's icon (the cabinet, 128 px) for the upgrade nodes and the **Treat water** button.

The picks went to `art-masters` (0249867) and the unpicked tries to `art-references` (aa310fc), each with its sidecar.
