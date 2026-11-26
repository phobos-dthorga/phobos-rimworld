# The lithium house battery kit: paid request record

**Decision (owner, 27 Nov 2026):** attempt **1** ("squarer, chunkier"), picked by card from the review card drawn by
`scripts/art/review_cards.py sprites` at 128 px.

- **Request:** `house-battery-lithium.json`, one item sprite, OpenAI gpt-image-2 at medium quality, 1024 x 1024, two attempts.
- **Why paid:** an in-world item (a kit lying in stockpiles), under `assets/AGENTS.md`.
- **Dry run:** at most US$0.20; the owner's go by card, 27 Nov 2026. **Run:** `python scripts/art-run.py generate
  assets/phobos-vehicles/requests/house-battery-lithium.json --stop-at <time> --max-usd 0.20`. **Spent:** about US$0.110.
- **Reference:** our approved lead-acid house battery (`HouseBattery.png`), style only. Nothing else was uploaded.

| Attempt | Review at native and actual size | Outcome |
| --- | --- | --- |
| 1 | Low, wide box, pale blue-grey with a blue band; deep-set red and black terminals; large vented board panel; isolator on the side with its cables. Clean outline, no text or marks; 0 loose specks after export. Clearly not the lead-acid one at actual size | **Picked** |
| 2 | Taller, closer to a real battery's proportions, slimmer board panel, wider band. Equally clean; a little nearer in shape to the lead-acid one | Not picked: the owner preferred 1. Archived on `art-references` |
