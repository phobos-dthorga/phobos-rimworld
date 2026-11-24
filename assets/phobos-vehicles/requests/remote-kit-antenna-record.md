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

**Decision:** waiting for the owner's pick. Then: the pick to `art-masters`, the other to `art-references`, and the fitting by
script on the five trucks' cabs in their three facings.
