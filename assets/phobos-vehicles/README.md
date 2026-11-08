# D'thorga Vehicles art

Art for our own vehicles. Rules: [paid image generation](../../docs/development/paid-image-generation-rules.md). Design:
[bulk tanker](../../docs/development/utility-vehicles-design.md). Generated images stay local in `generated/` until picked.

## Tanker pilot: north base body (8 Nov 2026)

**Status: the owner picked attempt 2 (8 Nov 2026). All four originals are saved exactly as generated: attempt 2 on `art-masters` (a876027, `vehicles/tanker/`), attempts 1, 3 and 4 on `art-references` (a8f4871, `vehicles/tanker-pilot/`), checked byte for byte. Nothing is shipped, and nothing has been seen in game.**

| Item | Value |
| --- | --- |
| Request | [requests/tanker-pilot.json](requests/tanker-pilot.json), which holds the exact prompt |
| Tool | OpenAI gpt-image-2 through `/images/generations`, medium quality, no references; AI-generated |
| Size | Asked for and returned 768 x 1536 (twice the in-game 384 x 768) |
| Cost | US$0.0295 an image, US$0.118 in all, from the token counts OpenAI returned; the cap was US$0.50 |
| Target proportion | The footprint is 2 by 5 cells, so width over length of about 0.40 |

**Review of each attempt** (agent's, at full, native 384 x 768 and quarter size; the owner's pick was attempt 2, over the agent's first choice of attempt 3):

| Attempt | Width over length | Review | Verdict | SHA-256 |
| --- | --- | --- | --- | --- |
| 1 | 0.307 | Straight down. Plain treadless tyres stand apart from the chassis with no mudguards, so the rear wheels look bolted on. Open ladder frame; the cradles are clear. Reads well at quarter size. | Second tier: wheels | `7000e0e6130cf74d…` |
| 2 | 0.327 | Straight down. The strongest silhouette at quarter size, with treaded tyres and a solid deck. The orange stripe sits behind the cab roof rather than across the cab. Straight cradles. | **Owner's pick** (agent's second choice) | `3c7b752f08adb7ee…` |
| 3 | 0.310 | Straight down. Curved saddle cradles explain a round tank before the tank is drawn; mudguards tie the rear wheels to the chassis; treaded tyres. The front fenders are rather large. | Agent's first choice | `0c1fa5e0afe0a010…` |
| 4 | 0.313 | The bonnet tapers like a trapezoid and the windscreen wraps, which reads as perspective even though nothing else is tilted. The orange stripe is translucent and the frame rails seem to float over the deck. | Reject: perspective cue on the bonnet (rule: reject tilt on sight) | `5e8cc15616eaae58…` |

**True of all four:** straight down, the deck is empty, no text, the pump housing is on the right as asked, and the background is
near-white rather than pure white (the cut-out must allow for that). The deck carries a faint mottle despite "no texture noise".

**The one real defect, shared by all four: too long and narrow.** Width over length, wheels included, is 0.31 to 0.33 against the 0.40
asked for, as the rules warned that sizes are weak guidance. Following the rule against rerolling for what a script can do, this is
a job for the export script: widen the picture by a measured factor, or change the design's footprint. **To settle when the pick is
made.** A side-by-side with a vehicle already in the game has not been done, so how it sits beside them is unknown.
