# The solar panel kit: paid request record

**Decision (owner, 27 Nov 2026):** attempt **1** ("upright, bigger box"), picked by card from the review card drawn by
`scripts/art/review_cards.py sprites` at 128 px.

- **Request:** `solar-panel-kit.json`, one item sprite, OpenAI gpt-image-2 at medium quality, 1024 x 1024, two attempts.
- **Why paid:** an in-world item (a kit lying in stockpiles), under `assets/AGENTS.md`.
- **Dry run:** at most US$0.20; the owner's go by card, 27 Nov 2026. **Run:** `python scripts/art-run.py generate
  assets/phobos-vehicles/requests/solar-panel-kit.json --stop-at <time> --max-usd 0.20`. **Spent:** about US$0.109.
- **Reference:** our approved house battery (`HouseBattery.png`), style only. Nothing else was uploaded.

| Attempt | Review at native and actual size | Outcome |
| --- | --- | --- |
| 1 | The panel stands upright in a silver frame with corner brackets, dark blue cells in a clean grid, a larger grey controller with red and black leads beside it. Clean outline; 0 loose specks after export | **Picked** |
| 2 | The panel lies tilted with a slim controller at its corner; brighter cells. Equally clean | Not picked: the owner preferred 1. Archived on `art-references` |
