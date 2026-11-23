# Camp tents and the kitchen box: run and review (22 Nov 2026)

**What was bought.** Ten pictures for D'thorga Framework 0.217.0 and 0.223.0 ([vehicle accessories](../../../docs/development/archive/vehicle-accessories.md)):
the five tent kits (side fly, awning walls, rear tent, fold-out camper, deck tent) and five pieces that stand in the game world (the camper
bed, the camp heater, the kitchen box, the camp table, the camp stool). Two attempts each, from the OpenAI image API (gpt-image-2, edits
endpoint, medium quality), at the owner's word (22 Nov 2026: "unless it appears in the game-world itself, then it uses a paid service"; the
kits too, by the owner's card). Dry run first: at most US$2.98; the cap US$2.00. Spent: about **US$1.075** for twenty images (US$0.046 to
0.057 each, from the returned token counts). Request: [camp-tents-kitchen.json](camp-tents-kitchen.json); the run's report is
`assets/phobos-vehicles/generated/run-20261122-0302.md`.

**References, our own only, for style.** The kits used our approved roof tent kit and awning roll; the pieces our approved swag and roof tent,
seen from above. No game or other author's art was uploaded.

**Review (agent).** All twenty clean: straight on (the pieces seen from directly above), no text, marks or loose goods, in the camp gear's
style. A first review sheet knocked out the white with a plain cut-off and left a white fringe on every soft edge; the owner caught it. The
sheets were then drawn again from the real export (the background AI's mask, then `scripts/art/export_sprite.py`, which takes the white out
of soft edges), on soil-coloured ground, enlarged and at actual size: no fringe. The agent suggested 1 for all but the stool (2).

**Decision (owner, 22 Nov 2026, one card each):** side fly kit **1**, awning walls kit **1**, rear tent kit **1**, fold-out camper kit **1**, deck tent
kit **1**, camper bed **2**, camp heater **2**, kitchen box **1**, camp table **2** ("I used to have a table just like it, aluminium slats and all"),
camp stool **1**. Exported at 128 px a cell (the camper bed 256 x 256); the four rotatable pieces' east and south facings are the north one turned
by script. The picks go to `art-masters`, the other attempts to `art-references`.
