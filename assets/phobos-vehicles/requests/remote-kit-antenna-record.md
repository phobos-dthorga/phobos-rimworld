# The remote control kit's antenna: run and review (24 Nov 2026)

**What was bought.** Two images, two tries of one part, from the OpenAI image API (gpt-image-2, high quality), on the owner's "Proceed
with Step #4" (the step's art comes first, by their rule), with a US$0.55 cap. Dry run first: at most US$0.52. Spent: about
**US$0.28** (US$0.138 each). Request: [remote-kit-antenna.json](remote-kit-antenna.json); design: [autonomous vehicles by
radio](../../../docs/development/autonomous-vehicles.md), step 4.

**The idea [A].** Seen from straight above, an upright whip is a dot, so it is drawn as real military trucks carry their whips:
bent back and tied down along the vehicle, from a round mount on the cab, with the data radio's box beside it.

**Reference, our own.** The Talus tipper's picked body (`tipper-body-north-3.png`), for style and scale only. No game or other
author's art was uploaded.

**Review (agent, each at full size and on the card at 128 by 213 game pixels, about 1.7 cells):** both drawn alone on white as
asked, with the thick outline and steel greys of our trucks, nothing extra drawn, and no loose specks after export.

- **Try 1:** a shallow single arc; a spring on a small round mount; a flat rectangular radio box beside it; a tie-down clip at the tip.
- **Try 2:** a gentle S-curve; a larger round mount with the spring coiled on it; a square radio box with its cable looped in;
  a small clip near the tip.

**Card:** `review_cards.py sprites ... --size 128 --aspect 3:5`, offered on D'thorga Companion's Picks page.

**Decision (owner, 25 Nov 2026):** try 1 ("I'm gobsmacked you were able to do that image without dithering! Perhaps #1?"). Fitted by `assets/phobos-vehicles/export_remote_kit.py` on the five trucks' cabs in their three facings (D'thorga Vehicles 0.117.2): the mount near the front of each roof on its right, the whip tied back along it, 190 game pixels long; agent placement, checked by the agent tinted at game pixels and enlarged in all three facings. **Kept local for now:** the near-copy check (`scripts/art/near_copy.py`, 25 Nov 2026)
flags both tries on structure alone (0.64 to 0.67, at or over 0.6) against two of the game's sprites, a security door's under-layer and
the select-carried-pawn button, at hash distances of 29 to 32 (a copy is 12 or less) and different proportions. Looked at side by side
by the agent: a grey square and a tiny pawn beside a long thin whip, plainly unlike; it is the open fault T16 (flat patches, here the
white canvas, score as alike). By the rule a flagged image stays off the public branches, so try 1 goes to `art-masters` and try 2 to
`art-references` once T16 is fixed and the check passes them.
