# The C-27J Spartan's body and propeller: paid pictures, stage 1 (26 Nov 2026)

**Status (26 Nov 2026):** eight pictures made on Google Nano Banana Pro through fal.ai: two tries of the bare body; two of the
propeller seen face on, **superseded: face-on, orange bands, rejected by the owner**; and four of it redrawn edge on from above, one
of them given a clean outline by script. **The owner's picks (26 Nov 2026, Picks page): body try 1, and the propeller's try 4
outlined by script**; both settled. **Exported into D'thorga Vehicles** by `assets/phobos-vehicles/export_c27j.py`
(`mods/PhobosVehicles/Textures/Phobos/Vehicles/C27J/`; record `assets/records/PhobosVehicles/Textures/Phobos/Vehicles/C27J/C27J.record.md`),
and the Def's colours and propeller hubs set from it. **Archived**: the picks to `art-masters` (`vehicles/c27j/`, 8181f10), every other
try and the plan drawings to `art-references` (`generated/vehicles/c27j/`, 94f5351), none truly fugly. Not yet tested in game.

The owner's question card of 26 Nov 2026 chose Nano Banana Pro through fal, "as the Osprey", with a cap of US$3.00 for this stage
and a later stage of six overlay edits together, raised the same day to US$3.60 for the propeller's redraw
([the C-27J's design](../../../docs/development/vehicle-c27j.md#art-owners-card-nano-banana-pro-through-fal-cap-by-card-after-this-plan)).
Requests: [c27j-paid.json](c27j-paid.json) (the propeller entry holds the correction, tries 5 and 6; tries 1 to 4 each keep their own
prompt and references in their per-image record). Made under [the paid rules](../../../docs/development/paid-image-generation-rules.md#running-the-fal-engine).
How each picture was made is written per image beside it (`assets/phobos-vehicles/generated/c27j-*/c27j-*-N.json`: prompt,
references with roles and SHA-256, fal's request ID, the size that came back, the AI disclosure and the terms note).

## References (all our own art)

| Picture | Image 1 | Image 2 (layout) |
| --- | --- | --- |
| Body | Finish only: our Osprey body, the owner's pick of 18 Nov 2026 (`osprey-trial-nano-banana-pro-1.png`, on `art-masters`, SHA-256 `1de0291e...`) | `references/c27j-plan.png`, drawn by [draw_c27j_plan.py](../draw_c27j_plan.py) (SHA-256 `6b682559...`) |
| Propeller 1, 2 (superseded) | Finish only: our Osprey's clean rotor (`osprey-rotor-clean-4k-1.png`, on `art-masters`, SHA-256 `941a55ad...`) | `references/c27j-propeller-plan.png`, face on (SHA-256 `b9ac1242...`) |
| Propeller 3, 4 | Outline weight and shading only: the Osprey body pick, as above | `references/c27j-propeller-edge-plan.png`, edge on from above (SHA-256 `35074067...`) |
| Propeller 5, 6 (the correction) | Try 4 itself: the drawing to repeat with crisp outlines | The edge-on plan, as above |

**The plan drawings** are Pillow shapes placed from published figures: length 22.7 m, span 28.7 m, wing area 82 m² **[S]**; propeller
4.115 m across, six blades (EASA type certificate data) **[P]**; fuselage 3.70 m across (AeroCorner) **[S]**. Everything else is
approximate, by eye, for layout only, and says so in the script: the wing's place and chords (the drawn wing comes to 81.8 m², against
the published 82), the engines 4.5 m out from the centre line, the nacelles, the gear fairings, an 11 m tailplane, the spinner (1.05 m
long, 0.78 m across) and the blades' shape and depth seen from above. **No tailplane span, engine spacing or wheel track was found
published** (searched 26 Nov 2026). No photograph, drawing or other author's art was used. **The owner's photograph of a US Coast Guard
HC-27J's propeller was not uploaded or used**: the redraw works from the owner's description of it (all black, glossy pointed
bullet spinner with no hub detail, six slim scimitar blades, no coloured tip bands).

**The edge-on view** (owner's card, 26 Nov 2026: the propeller ships seen from straight above). A tractor propeller turns in an
upright plane, so from above it is a thin bar across the spinner. With one blade straight out to each side, the other four point 60
degrees toward or away from the camera and show at half their length (cos 60), as shorter slivers over the inner half of each long one.

## Size, against the 1.5x rule

| | In game (drawn) | The 1.5x rule asks | Made | Against the drawn size |
| --- | --- | --- | --- | --- |
| Body | 34 x 27 cells at 1.175 cells a metre (owner's card): 4,352 x 3,456 px at 128 px a cell | 6,528 px across the span | 4,608 x 3,712 px (4K at 5:4, the largest the model makes); span 4,267 px (try 1) and 4,310 px (try 2) | **About 1.0x**: 126.5 and 127.8 px a cell |
| Propeller, edge on | 4.115 m, 4.8 cells: 619 px across | 929 px | 3,168 x 1,344 px (2K at 21:9); the bar about 2,790 px across | **About 4.5x** |

- **The body cannot meet the 1.5x rule in one picture.** 4K at 5:4 is the most Nano Banana Pro makes, and the aircraft already fills
  over nine tenths of the width. It is about the drawn size at 128 px a cell, and a little above the roughly 120 px a cell the design
  plans to ship at (the 4K texture limit, [the design](../../../docs/development/vehicle-c27j.md#read-this-first)). So the body can
  be shipped at its planned size without scaling up, but **a later enlargement of the C-27J would need it made again** (or made in two
  halves and joined, which the Osprey record judged not worth it). Measured against the biggest on the owner's list, the Osprey
  (about 30 x 20.5 cells) and the Mi-38 it was scaled from: the C-27J is now the largest picture of ours.
- **The propeller meets the rule at 2K.** The face-on tries were made at 4K, about 6x, more than needed.

## What each made (agent's review at full size, a third size, and the card)

Costs are the engine's table ceilings for Nano Banana Pro (US$0.30 at 4K, US$0.15 at 2K); fal's answers carry no price, so the true
spend is on the owner's fal dashboard.

| Try | Seconds | Cost ceiling | Review |
| --- | --- | --- | --- |
| Body 1 | 55 | US$0.30 | Straight overhead, nose up, pure white ground (no pixel outside the aircraft below 251 of 255). **No propellers**: each nacelle ends in a bare pointed spinner. Span to length 1.256 (real 1.264). Engines 4.5 m out, wing at the plan's place, tailplane about 10.9 m. Nacelles end at the wing's trailing edge with a small exhaust; small gear fairings ahead of and behind the wing; the fin reads as a slim strip running over the tailplane. Rivet rows and flap lines in the Osprey's finish. No text or markings |
| Body 2 | 64 | US$0.30 | The same layout and finish, span to length 1.262. **Nacelles are longer pods** reaching well behind the trailing edge, nearer the plan; the wing-to-fuselage fillets are squarer. Pure white ground, no propellers, no markings |
| Propeller 1 (4K) | 66 | US$0.30 | **Superseded: face-on, orange bands, rejected by the owner** ("Neither propeller is up to standard"). Six grey blades face on, a seam across each, an orange tip band, a domed spinner |
| Propeller 2 (4K) | 64 | US$0.30 | **Superseded: face-on, orange bands, rejected by the owner.** As try 1, with wider, scimitar-shaped blades |
| Propeller 3 (2K) | 47 | US$0.15 | **Rejected by the agent: eight slivers, two angled forward** out of the propeller's plane, so it is not edge on; small grey squares at the blade roots after the cut-out. All black, pure white ground |
| Propeller 4 (2K) | 50 | US$0.15 | **The brief's shape**: a glossy black pointed spinner forward, thin curved black slivers sweeping back, one long and two short each side, all black, no bands; the bar about a tenth as deep as it is wide. **But the outer halves of the long blades are soft and blocky, with no outline, and leave a pale grey fringe after the cut-out**, plain on dark stone |
| Propeller 5 (2K, correction of 4) | 40 | US$0.15 | **Rejected by the agent: the correction lost the edge-on view.** Crisp outlines, but six leaf-shaped blades fanned round the spinner like a flower |
| Propeller 6 (2K, correction of 4) | 33 | US$0.15 | **Rejected by the agent, for the same reason**: one long pair straight out, but four blades fanned diagonally round the spinner |
| Propeller 4, outlined (script, free) | | none | **Try 4 with its outline redrawn by script** (rule 11, after one paid correction failed; rule 7): its dark shape smoothed and given an unbroken 4 to 5 px near-black outline, its own pixels laid back inside. No pale fringe on soil or stone; the blade edges are a little wavy at full size and smooth at the shipped size. How: `generated/c27j-propeller/c27j-propeller-4-outlined.json` |

**Spent, by the ceilings: US$1.80**: US$1.20 for the body and the face-on propeller, US$0.60 for the redraw (two tries and one
correction of two, at US$0.15, each run capped at US$0.30), within the owner's raised US$3.60. US$1.80 remains for stage 2.

**The agent's lean (for the owner to overrule):** body **try 2**, for its longer nacelles; propeller **try 4 outlined**, the only
edge-on try without a fringe. Both bodies are usable.

## Checks

- **Near-copy.** The run's own check had nothing to compare against, by design: every reference was our own art
  (`art-run.py` leaves our own folders out). Run again by hand against all the references: nothing flagged; every try is "close to
  our own art", as the layout role asked, each to its own plan drawing, none to the Osprey or its rotor. A trial run against an
  unrelated folder of the game's sprites flagged three of the first tries on structure alone against a 64 px UI icon, with hash
  distances of 28 to 30 (the limit is 12): mostly-white pictures against a mostly-empty icon, not a likeness.
- **Edges.** The edge check finds **0 loose specks and no haze on every export** (printed on each card and on the Picks page). It
  cannot see a fringe in another colour: try 4's pale grey fringe was found on the card, as were try 3's grey squares.
- **Cards** (from the real export, `scripts/art/review_cards.py sprites`): the body at 432 px on its short side, 5:4, which is about
  16 px a cell, a zoomed-out view (the shipped size, 3,456 px, is too large to show enlarged three times); the propeller at 300 px on
  its short side, 21:9, so its bar is about its shipped 619 px. Cards: `K:\phobos-rimworld\art\cards\c27j-paid\01-c27j-body.png` and
  `01-c27j-propeller.png`. The propeller's card shows only try 4 and its outlined version: it was drawn from a copy of the request
  naming those two (the review tool reads tries by seed), and the offer's source points back at this request.

## Owed, and things to know

- **The spinner is large beside the bar** (1.05 m long by 0.78 m across in the plan, by eye): worth the owner's look on the body.
  Laid at each nacelle's front, the bar sits across the spinner the body already draws; which spinner shows is decided when the overlay
  is laid on.
- Done after the picks: the spinning blur (by script), the east and south facings, the paint masks (by region) and the archive.
  The paint repair with Segment Anything's parts map, tried 26 Nov 2026 on the processor (the owner asked for the work without the
  graphics card): 0 visible flecks with or without it, so the region paint stays (`export_c27j.py`). Still owed: the parts cut for
  damage looks. Stage 2's overlays are made: [c27j-overlays-record.md](c27j-overlays-record.md).
- The run report says the spend was "worked out from the token counts OpenAI returned" and that the images were "made on OpenAI's
  servers": wording carried over from the OpenAI engine; these were fal's, priced at the table's ceiling.
