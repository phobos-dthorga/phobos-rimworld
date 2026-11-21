# Camp gear drawn on our vehicles: run and review (22 Nov 2026)

**Why.** After the deck tent pilot, the owner chose **everything on ours** by card (22 Nov 2026): fitted camp gear drawn on our own vehicles,
paid (in-world art), never on other authors' vehicles. 17 overlays: a roof tent on five trucks and the flatbed, a rolled bag on the side rail
of five trucks, the Pintle bowser body (fuel bowser, water bowser, shower trailer), the Billy and the flatbed, the light and roomy deck tents
on the tipper, and the fold-out camper on the flatbed. D'thorga Vehicles 0.103.0, row 359; not yet tested in game.

**How.** Each an edit of a crop of the vehicle's own **tinted game picture at twice its size** (paid rule 13; so every overlay registers with
what the game draws, and our own art is the only reference), from [camp-gear-overlays.json](camp-gear-overlays.json), OpenAI gpt-image-2,
medium, two attempts. Lifted with `scripts/art/sam_lift.py --cpu`; exported at the picture's own scale. The working scripts are kept
locally in `.local/art/camp-overlays/` (plan, crops, lift, export, cards, Def wiring).

**Spend.** Dry run ceiling US$3.43; two runs, capped at US$2.00 and US$1.90: about **US$0.17 + US$1.13 = US$1.30** for 34 pictures, in the
owner's range of "roughly US$1 to 2". The first run stopped after six: the service refuses a picture below its least pixel count (640 x 768
and 512 x 1152 refused; 512 x 1280 made), so small crops were sent half as large again.

**What went wrong, and what changed.**

- **An export scale the wrong way round** (the agent's): the doubled pictures' transform said 2 where the exporter wants game pixels per
  master pixel, 0.5; every overlay landed off the canvas. Caught on the review sheet before anything shipped.
- **The roof tents brought a patch of the cab with them**: invisible in the default colours, a mismatched square on a repainted truck. Lifted
  again keeping only what is darker than the cab (`darker_than` 110); ladders kept where Segment Anything held them.
- **Dark gear over dark metal would not lift** (most side rolls, the flatbed's roof tent): Segment Anything caught scraps, and lifting by
  difference failed too. Two tries at one defect, so by paid rule 7 the route changed, to a free one (rule 11): **the side roll is the Billy's
  one clean lifted roll (attempt 2) laid by script along each vehicle's left rail at its own length; the flatbed's roof tent is the survey
  truck's lifted shell sized to the deck.** One picture each, no pick.

**Review (agent).** Laid on each vehicle's tinted picture over soil-coloured ground: all inside their vehicle, no white fringe. Suggested and
shipped: tipper roof tent **2**, tipper light deck tent **1**, tipper roomy deck tent **1**, tanker roof tent **2**, survey roof tent **2**,
drill rig roof tent **2**, Busbar roof tent **2**, fold-out camper **2**.

**Decision (owner):** waiting, for the eight with two attempts.
