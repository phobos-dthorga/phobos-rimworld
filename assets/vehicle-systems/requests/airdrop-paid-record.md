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
