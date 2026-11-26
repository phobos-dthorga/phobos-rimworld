# Airdrop pictures: run and review (25 Nov 2026)

**The owner's direction.** Answer 9 of the airdrop's question cards (25 Nov 2026, `docs/development/airdrop.md`): paid art, **US$1.50
cap**, nine in-world pictures with two tries each, a rigger's table of our own, picks on D'thorga Companion's Picks page; the buttons'
icons on the free night run.

**What was bought.** Eighteen pictures from the OpenAI image API (gpt-image-2, edits endpoint, medium quality) under the rule that what
appears in the game world is made paid. Request: [airdrop-paid.json](airdrop-paid.json).

| Step | Run | Cost |
| --- | --- | --- |
| Dry run of all nine (ceiling, US$0.149 a picture at most) | none sent | at most US$2.68 |
| Pilot: the open canopy, two tries, cap US$0.35 | `generated/run-20261125-0900.md` | US$0.112 |
| The other eight, two tries each, cap US$1.38 (US$1.50 less the pilot) | `generated/run-20261125-0901.md` | US$0.770 |
| **Total** | 18 pictures, no refusals, no timeouts | **US$0.882 of US$1.50** |

Per picture, from the returned token counts: about US$0.055 at 1024 x 1024, US$0.033 at 768 x 1536 (the platform), US$0.021 at
1536 x 512 (the table). Every returned size equalled the size asked.

**References, our own only, for style**, each with its role in the request and the prompt: the swag, the deck tent kit, the roof tent
kit, the sling set, the camp table and the parts bench. No game or other author's art was uploaded.

**Pilot (agent, at native size).** Both canopy tries were true overhead circles with a clean outline, the gores shaded in olive tones,
a pale vent and line stubs at the rim; nothing hanging below. The family went ahead unchanged.

**Review (agent, from the originals).** All eighteen match the brief: overhead plan for the canopy, bundle, platform and table; the
item angle of our approved kits for the five items; no text, logos, people or vehicles. Notes for the owner: the personnel parachute
reads as a pack with shoulder straps (both tries); the two platform tries are near twins; the table's two tries differ mostly in the
paddle (try 1 long and dark, try 2 short and pale) and the pack at the left end.

**The rigger's table.** 4 x 1 cells drawn at (4.5, 1.5), as the Def (`Airdrop_RiggersTable.xml`) and as our parts bench (3 x 1 at
3.5 x 1.5): generated at 1536 x 512 (3:1), exported at 576 x 192 north. North has the rigger standing at the bottom edge (the
interaction cell in front, `(0,0,-1)`); east, south and west are it turned by script. The Def uses CutoutComplex, so the ship script
draws a colour mask per facing (red: the steel frame and wooden top; never the canopy and gear on it). Colour alone cannot tell the
top from try 1's khaki shot bags (129,113,75 against the wood's 145,121,90), so the mask goes by region: the frame is the band outside
the top's inner rectangle; inside it, whatever is not the top's colour is a thing on the table, closed and filled solid, and only tint
joined to the frame is kept, so no island or speck is left. The close is set per try from its preview (try 1: 2 pixels; try 2: 1).
`python assets/vehicle-systems/ship_airdrop.py --mask-preview` draws both tries' masks tinted as wood and steel (agent-checked, both clean).

**Decision.** Waiting: offered on the Picks page as `airdrop-canopy`, `airdrop-bundle`, `airdrop-platform`, `airdrop-spent-canopy`,
`airdrop-personnel-chute`, `airdrop-cargo-chute`, `airdrop-heavy-rig`, `airdrop-spent-platform`, `airdrop-riggers-table`. On the
answers: `python assets/vehicle-systems/ship_airdrop.py` (it reads them), then archive the picks to `art-masters` and the other tries to
`art-references` with `scripts/art/archive_branch.py`, and `picks settle` each in the commit that ships it.

## Second batch: the guided cargo parachute and the ankle braces (25 Nov 2026)

**The owner's direction.** A question card of 25 Nov 2026: two more in-world items for the airdrop (the guided cargo parachute and the
parachute ankle braces), two tries each, about US$0.22, from the US$0.62 left of the day's US$1.50 cap; "Paid, then commit together".
The run's own cap was US$0.40. The two requests are added to the same file, [airdrop-paid.json](airdrop-paid.json)
(`airdrop-guided-chute`, `airdrop-ankle-braces`), and the run skipped the eighteen pictures already made.

| Step | Run | Cost |
| --- | --- | --- |
| Dry run (ceiling, US$0.149 a picture at most) | none sent | at most US$0.60 |
| Both, two tries each, cap US$0.40 | `generated/run-20261125-1034.md` | US$0.221 |
| **Day's total** | 22 pictures, no refusals, no timeouts | **US$1.103 of US$1.50** |

About US$0.055 a picture at 1024 x 1024, medium quality, as before; every returned size equalled the size asked.

**References, our own only, for style**: the two parachute items picked earlier the same day, `Airdrop/CargoParachute.png` and
`Airdrop/PersonnelParachute.png`, each with its role in the request and the prompt. No game or other author's art was uploaded.

**Review (agent, from the originals and the review cards).** All four match the brief: the item angle of the earlier parachute items,
a thick near-black outline, olive and tan, no text, logos or people. Guided chute: try 1 is a flat strapped bag with a tan box and a
thin upright antenna, the coiled lines at the lower right; try 2 has the box strapped lower with a short knob of an antenna and the
risers trailing out at the bottom. Ankle braces: try 1 reads as two short braces with a foot stirrup and a padded collar; try 2 is
open-backed shells (it could read as shin or forearm guards). The cards' edge check found 0 loose specks on all four.
Cards: `cards/airdrop-paid/01-airdrop-guided-chute.png` and `01-airdrop-ankle-braces.png` in the art working folder (the card number is 01 because each was drawn with `--only`).

**Decision.** Waiting: offered on the Picks page as `airdrop-guided-chute` and `airdrop-ankle-braces`. On the answers, the same as the
first batch: `python assets/vehicle-systems/ship_airdrop.py` (or `--pick airdrop-guided-chute=N`), archive, and `picks settle`.

## Third batch: the drop zone marker (26 Nov 2026)

**The owner's direction.** A question card of 26 Nov 2026 ("Aim point and less scatter ... One paid picture"): the drop zone marker,
a pathfinder's VS-17 signal panel (about 1.8 x 0.6 m) staked flat on the ground, one picture, two tries, **hard cap US$0.15**. Added
to the same file, [airdrop-paid.json](airdrop-paid.json) (`airdrop-dz-marker`); the runs skipped every picture already made.

| Step | Run | Cost |
| --- | --- | --- |
| Dry run (ceiling, US$0.149 a picture at most) | none sent | at most US$0.30 |
| Try 1, cap US$0.15 (the run then stopped on its look-ahead) | `generated/run-20261126-1136.md` | US$0.0574 |
| Try 2, cap US$0.15 (try 1 skipped) | `generated/run-20261126-1136.md` | US$0.0574 |
| **Total** | 2 pictures, no refusals, no timeouts | **US$0.115 of US$0.15** |

**A deviation, and why.** The engine's look-ahead counts US$0.149 a picture (an allowance of 6,000 tokens a reference, overstated on
purpose), so one run under a US$0.15 cap can only ever make one picture. Each try was therefore its own run, each capped at US$0.15. That
was safe on the real figures: the cost is worked out from the returned token counts, which depend only on size, quality, references and
prompt, and every 1024 x 1024 medium picture of this file so far has cost US$0.055 to US$0.063; both tries came back at 749 input
tokens (320 image, 429 text) and 1,756 output, US$0.0574 each. All three runs fell in the same minute, so the one report file holds the
last run only; each picture's own record (`airdrop-dz-marker-1.json`, `-2.json`) keeps its tokens and cost. Requested and returned
size 1024 x 1024 both times.

**References, our own only, for style**, each with its role in the request and the prompt: `Airdrop/CargoBundle.png` (the approved
piece seen straight from above: outline, shading, overhead camera, finish) and `Airdrop/SpentParachute.png` (outline and cel shading
only). No game or other author's art was uploaded. The near-copy check (`near_copy.py --only airdrop-dz-marker`, the Airdrop folder
as our own art): 0 flags, 0 likenesses.

**Review (agent, from the originals and the review card at 256 x 256, x3 and actual size, on soil and stone).** Both are straight
overhead: a fluorescent orange panel with a stitched hem and a few soft wrinkles, the upper right corner folded back to show the cerise
reverse, a steel peg through a loop at each corner with a small patch of brown dirt; thick near-black outline; no text, people or
ground beyond the pegs. The panel comes out about 2.3 to 1 rather than the 3 to 1 asked (a VS-17 is 3 to 1); a small loss at this
size. Try 1: the panel fills more of the canvas, its folded corner a crisp triangle, its upper right peg pulled out on a strap. Try 2:
a little smaller, its pegs on short strap tabs set out at all four corners, the fold softer and rounded. Both orange and cerise stay
bright at actual size. The card's edge check found 0 loose specks on both. Card: `cards/airdrop-paid/01-airdrop-dz-marker.png` in the
art working folder.

**Decision.** Replaced the same day, before the owner picked: a question card of 26 Nov 2026 chose "Panel + smoke pot + lamp", so
tries 1 and 2 were taken off the Picks page and the pick `airdrop-dz-marker` now offers tries 3 and 4 (the fourth batch, below).
Tries 1 and 2 stay in `generated/airdrop-dz-marker/` with their own records (their prompt is there, the request file now holds the
new one); they go to `art-references` with the unpicked try of the fourth batch, as superseded.

## Fourth batch: the drop zone marker with its smoke pot and lamp (26 Nov 2026)

**The owner's direction.** A question card of 26 Nov 2026, "Panel + smoke pot + lamp": straight overhead, on plain white, the same
VS-17 style panel (fluorescent orange, cerise folded-back corner, pegged at each corner, about 3:1) with, staked beside it, a small
violet smoke canister (M18 style, seen from above) with a thin violet wisp only, never a plume (the game spawns the moving smoke during
drops), and a small battery beacon lamp with an unlit amber lens (the game lights a glow at night). No text, people or sun-angle
shadows. Two tries, **hard cap US$0.15 for the pair**. The request `airdrop-dz-marker` in [airdrop-paid.json](airdrop-paid.json) was
given the new prompt and seeds 3 and 4, so its pick id and ship slot stay the same and the new tries replace the waiting ones cleanly
(offering the same name again replaces its pick). Same engine and settings: OpenAI gpt-image-2, edits endpoint, medium, 1024 x 1024,
the same two references.

| Step | Run | Cost |
| --- | --- | --- |
| Dry run (ceiling, US$0.149 a picture at most) | none sent | at most US$0.30 |
| Try 3, cap US$0.15 (the run then stopped on its look-ahead) | `generated/run-20261126-1236.md` | US$0.0585 |
| Try 4, cap US$0.15 (try 3 skipped) | `generated/run-20261126-1237.md` | US$0.0585 |
| **Total** | 2 pictures, no refusals, no timeouts | **US$0.117 of US$0.15** |

**The same deviation as the third batch, and why.** The look-ahead counts US$0.149 a picture, so each try was its own run capped at
US$0.15; try 4 was bought only after try 3 came back at US$0.0585, which left US$0.0915 of the pair's cap against a cost fixed by the
token counts (both tries: 981 input tokens, 320 image and 661 text, the longer prompt; 1,756 output). Requested and returned size
1024 x 1024 both times.

**References, our own only, for style**, as in the third batch: `Airdrop/CargoBundle.png` and `Airdrop/SpentParachute.png`, each with
its role in the request and the prompt. No game or other author's art was uploaded. The near-copy check (`near_copy.py --only
airdrop-dz-marker`, the Airdrop folder as our own art): 0 flags, 0 likenesses.

**Review (agent, from the originals and the review card at 256 x 256, x3 and actual size, on soil and stone).** Both are straight
overhead in the layout asked: the panel across the top, the smoke canister below its lower left corner, the lamp below its lower right;
thick near-black outline; no text, people, ground or cast shadows. The wisp came out as a wisp in both (a small outlined curl, not a
plume). Both canisters show a sliver of their olive side below the violet top (a slight tilt on that one object); both lamps show the
tip of their stake poking out below the housing. The panel is still short of 3:1, again: try 3 about 2.65 to 1, try 4 about 2.2 to 1.
Try 3: a longer, slimmer panel, its pegs bent hooks at the corners, a crisp folded corner, a taller question-mark-shaped wisp, a bright
amber lens. Try 4: a deeper panel with bigger round-headed pegs; its upper right peg and loop sit apart from the panel beside the folded
corner (reads as the loop pulled free), a short squiggle of a wisp, a darker amber lens. At actual size the canister and lamp are small
dots under the panel in both; the violet and amber still read. The card's edge check found 0 loose specks on both. Card:
`cards/airdrop-paid/01-airdrop-dz-marker.png` in the art working folder.

**Decision.** Waiting: offered on the Picks page as `airdrop-dz-marker` (tries 3 and 4, slot `Airdrop/DropZoneMarker.png`). On the
answer: `python assets/vehicle-systems/ship_airdrop.py` (or `--pick airdrop-dz-marker=3` or `=4`), which runs the background AI on the
processor and exports at 256 x 256; then archive (the pick to `art-masters`; the other try and tries 1 and 2 to `art-references`) and
`picks settle`, as before.
