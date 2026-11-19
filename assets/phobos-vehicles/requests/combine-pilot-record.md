# The combine harvester pilot: run and review (19 Nov 2026)

**What was bought.** Three attempts of one vehicle body, D'thorga Farming's combine harvester, north facing, bare (no front), from
the OpenAI image API (gpt-image-2, medium quality, 1536 x 2048), at the owner's go of 19 Nov 2026 with a US$0.50 cap. Dry run first:
at most US$0.46. Spent: about **US$0.223** (US$0.0742 each, from the returned token counts; none an estimate). Request:
[combine-pilot.json](combine-pilot.json); design: [farming batch one](../../../docs/development/farming-batch-one.md).

**Reference, our own.** Image 1, the approved Talus tipper's north body, for style only. No game or other author's art was uploaded.
The runner's near-copy check flagged none of the three.

**Review (agent, at the size shown, about native).** All three are straight down with no tilt or visible side face, on white, with
no text, crop, grain, people or front. All three have the planned layout: a cab at the front centre, an open grain tank behind it,
the unloading auger lying along the left side, an engine deck with grilles and an air intake, a narrow tail, big front drive tyres
and small rear steering tyres, and the yellow accent. The top of the canvas is clear for the front overlay (about 8.5 to 9.5 % of the
height; the request asked for a tenth).

| Attempt | What is right | What is wrong |
| --- | --- | --- |
| 1 | Reads most as a combine: lugged tyres front and rear, an open tank with a rim and a pale floor, the auger from the front of the tank to a mount at the rear, a compact engine deck, a short straw tail | The feeder throat in front of the cab is barely there, so the front overlay must sit against the cab's front panel |
| 2 | Clean outline; a solid engine deck with two grille blocks | **The tank reads as closed**: three raised ribs look like a lid, not an open tank; the rear tyres have a road tread, like a trailer's |
| 3 | Housings either side of the cab read as the feeder's sides; the tank floor has panel lines | The long tapering engine deck and tail make it look like a dragster; road-tread rear tyres; the auger stops short of the rear |

**Agent's lean: attempt 1.** Its width with the tyres is about 54 % of the canvas against the planned 67 % (4 of 6 units), so the
export will set the drawn size to fit the 4-cell footprint rather than regenerate (rule 11: scripts do sizes for free).

**Decision:** waiting on the owner's pick, from `assets/phobos-vehicles/generated/review.html`. The pick goes to `art-masters` and
the other two to `art-references` with their reasons, through `scripts/art/archive_branch.py`.
