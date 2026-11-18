# The V-22 Osprey's overlays and parts, first round (18 Nov 2026)

**Status (19 Nov 2026):** three rounds; every overlay and part now clean at full size, waiting on the owner's picks of three
pairs and the guns' size. Nothing is exported or shipped. This file started as the first round's record. Requests: [osprey-overlays.json](osprey-overlays.json),
all edits of the owner's pick through fal (Nano Banana Pro), at a US$3.50 cap; spent about US$2.70 by the table's ceilings, the
three refusals counted because fal may charge a refused request that has run.

## What happened

**The model does not draw a part alone.** Asked for the part "alone, on white, without the aircraft", it redrew the whole aircraft
with the part added, every time. Rule 7 of the paid rules says change the approach, so the approach changed: the redrawn aircraft
matches the pick closely, and compared with it only thin outline lines differ, while the added part is one solid shape. The new
`scripts/art/edit_difference.py` lifts that shape out onto transparency, free, keeping both pictures as they came.

| Request | What came back | Kept? |
| --- | --- | --- |
| Refuelling probe | Along the right of the nose, as asked | **Kept**, lifted out cleanly |
| Ramp gun, 7.62 mm | On the rear fuselage's centre line, barrel aft | **Kept**, lifted out cleanly |
| Ramp gun, .50 | Right place, far too large: its ammunition can is as wide as the fuselage and the barrel reaches past the tail | **To be scaled down by script** about its mount, to the 7.62 mm gun's proportions |
| Rescue hoist | Drawn beside the aircraft, not on its roof | Asked again |
| Still rotor | A three-bladed rotor over the fuselage; the blade over the fuselage lifts out incompletely, being the same grey | **To be rebuilt by script** from one whole blade and the hub, turned by 120 degrees |
| Nacelle at 30° | The aircraft unchanged | Not kept |
| Nacelle at 60° | The aircraft with its right nacelle removed | Not kept |
| Nacelle fully forward | Pointed caps added to the front of both nacelles | **The caps kept**, as the forward end of the script-made nacelles |
| Radar nose, inlet barriers, infrared jammer | Refused by the service ("did not generate the expected output"), no picture | Asked again, worded as an addition to the aircraft |

## What changes

- **The tilting nacelles are made by script**: the upright nacelle cut from the pick by its part mask, drawn longer toward the
  front as it tilts, with the kept pointed cap at its front. The model could not draw them, and a script keeps every angle
  consistent with the body.
- **Overlays are asked for as additions** ("add the X to this aircraft, change nothing else"), then lifted out.

## Second round (18 Nov 2026)

[osprey-overlays-2.json](osprey-overlays-2.json): the four asked again as additions ("add the X to this aircraft, change nothing
else"), on the owner's US$1.50 limit; all four made, about US$1.20 by the ceilings. Then lifted out by difference.

| Request | Result |
| --- | --- |
| Rescue hoist | **Kept**: on the fuselage roof's right half, ahead of the tail |
| Radar nose | **Kept**: a subtle fuller cap at the nose tip |
| Infrared jammer | **Kept**: a small domed turret on each sponson's outer side |
| Inlet barriers | **Wrong**: drawn as frames on the wings beside the nacelles. Asked twice now, so by rule 7 it changes route: **to be drawn by script** on each nacelle's part mask, free |

## Made by script (free)

`assets/phobos-vehicles/derive_osprey_parts.py`, into `generated/osprey-derived/`, each with a record:

- **The .50 gun**, shrunk about its mount to its real length beside the 7.62 mm gun (1.65 m against 1.25 m, from general knowledge).
- **The still rotor**, rebuilt from its one whole blade turned by 120 degrees twice; the ragged inner blade rebuilt from a clean
  cross-section; the hub cap is the pick's own, enlarged.
- **The spinning disc**: forty turns over a third of a turn, mixed by cover, its densest ring brought up to a little under half.
- **The nacelle at 0, 30, 60 and 90 degrees**: the upright one cut from the pick, hub taken off; tilted ones drawn longer toward the
  front, with the pointed cap growing at the front. A faint ghost of the hub's rim remains, to clean at export.

**Spent in all on the overlays:** about US$3.90 by the ceilings (US$2.70 and US$1.20), within the owner's two limits.

## The owner's look (18 Nov 2026): "very badly mangled"

The owner looked at the review page at full size and found the rotor, hoist, radar nose and .50 gun "very badly mangled". **The
agent's fault**: they were judged from thumbnails and shown without the full-size inspection the paid rules require. At full size:

| Piece | What is wrong | Why |
| --- | --- | --- |
| Rotor | Broken outline slivers along the blades; the hub far too large | The source blade was drawn with broken outlines; the hub was enlarged to hide ragged roots |
| Hoist | Gaps along its arm; not a convincing hoist | Lifting by difference loses wherever the part is the body's grey; and the drawing itself is poor |
| Radar nose | Doubled outlines and see-through patches | The redrawn nose sits a little off the pick's, and the difference cannot tell the two apart |
| .50 gun | See-through patches; still too large, overhanging the fuselage | The same loss of grey parts; the shrink was to length, not to the fuselage's width |
| 7.62 mm gun | A ghost of its mount ring, half see-through | The same loss |
| Nacelles | A ghost of the hub's rim | The patch did not reach the rim |
| Probe, jammer turrets | Fine at full size | |

**The lesson, for this engine:** lifting by difference finds *where* a part is, but must not decide its outline: anything the
body's colour comes out see-through. And the model draws a part alone only when nothing else is in its references.

## Third round (18 to 19 Nov 2026): pieces of the body, and Segment Anything

The owner's go for the graphics card and a US$2 limit. [osprey-overlays-3.json](osprey-overlays-3.json): nine pictures, **US$1.50**
by the ceilings (eight at 2K, one at 4K). Made into `generated/osprey-overlays-sam/` (lifted) and `generated/osprey-derived/`
(by script); the review page is `generated/osprey-parts-review/index.html`, every piece inspected laid on the body at native size.

**What changed:**

- **Overlays are drawn on a piece of the body** ([cut_osprey_references.py](../cut_osprey_references.py): the ramp and one nacelle,
  cropped from the pick), never the whole aircraft. With no aircraft to redraw, the model added only the part, and all six came out
  clean. Now rule 13 of the paid rules.
- **Parts are lifted with Segment Anything** (`scripts/art/sam_lift.py`): the difference only says where the part is. Run on the
  processor (`--cpu`) once the game was open; about 30 to 90 seconds a batch.
- **Two graphics card tools did not see the game running.** `name_parts.py` and `paint_segments.py` looked only for the 1.6 name,
  `RimWorldWin64.exe`; 1.7 runs as `RimWorld.exe`. The owner started the game during the second Segment Anything run, which shared
  the graphics card with it for about two minutes while it loaded. Both now ask `safeguards.py`, which knows both names, and
  `tests/test_art_tools.py` refuses a tool that names the game by itself.

| Piece | How now | State |
| --- | --- | --- |
| Refuelling probe, 7.62 mm gun | The first round's edits, lifted again with Segment Anything | Clean |
| Infrared jammer | The same, clipped to each dome's measured circle (radius 114 px) | Clean, whole domes |
| Radar nose | **Made from the pick's own nose tip** by script, a shade darker, with the edit's seam line | Lines up exactly; the model's nose was a slightly different shape and doubled the outline |
| .50 gun | Redrawn on the ramp piece; two to pick from | Clean; about 3.5 m and 2.7 m long as drawn |
| Rescue hoist | Redrawn on the ramp piece, at the ramp as the sourced figures place it; two to pick from | (a) clean; (b) its mounting plate ragged |
| Inlet barrier | Redrawn on the nacelle piece; two to pick from | Both clean, on the nacelle at last |
| Still rotor | Redrawn from the script-built one, alone; then again at 4K | Clean, small hub; 4K because at 2K it was 1.1 times its drawn size in game, under the 1.5 rule |
| Spinning rotor | From the 4K rotor by script | Clean |
| Nacelles 0 to 90 | The hub taken off whole rows at a time; the pointed cap now covers the rounded front to its seam | No ghost, no gap |

**Open with the owner:** which of each pair; and the guns' size. The 7.62 mm gun is drawn about 4.2 m long, the .50s 3.5 m and
2.7 m, against an M240's 1.25 m and an M2's 1.65 m [A: from general knowledge]. Shrinking them by script is free.

## Next

1. The owner's picks and the guns' size.
2. The new paid pictures to `art-references`, as generated, with the earlier rounds' twelve.
3. The background AI and export at the body's scale.
