# The bulk bunker pilot: run and review (17 Nov 2026)

**What was bought.** Two attempts of one building, the bulk bunker (Vehicles 0.63.0; [universal hoppers](../../../docs/development/universal-hoppers.md),
UH2), from the OpenAI image API (gpt-image-2, edits endpoint, medium quality, 1536 x 2048), at the owner's go with a US$0.50 cap
(17 Nov 2026). Dry run first: at most US$0.40. Spent: about **US$0.155** (US$0.0775 each, from the returned token counts). Request:
[bulk-bunker-pilot.json](bulk-bunker-pilot.json).

**References, both our own.** Image 1, the approved vehicle parts bench, for style only. Image 2, the bunker's stand-in drawn in code
(`draw_bulk_standins.py`), for layout only. No game or other author's art was uploaded.

**Review (agent, at native size and on dark ground).** Both are straight down with no tilt or visible side face, empty, walls on three
sides, the striped wheel stop across the open end, the drain grate near the back wall, and floor joints one cell apart. Neither has text,
goods or perspective. Attempt 1 has a crisper, even near-black outline, the closest to the game's look, sharp chamfered wall corners and a
faint tipping stain. Attempt 2 is softer, with rounded wall ends and bolt heads, and a speckled patch that reads more like dust.

**Decision (owner, 17 Nov 2026):** attempt 1 ships. Exported with `scripts/art/export_sprite.py --size 384 --aspect 3:4` after the
background AI had looked at it (on the processor). Master on `art-masters` (`vehicles/stations/bulk-bunker-pilot-1`); **attempt 2 is
kept on `art-references` for other uses** (owner: "We could still use #2 for other uses, perhaps"), such as a second style of bunker.
