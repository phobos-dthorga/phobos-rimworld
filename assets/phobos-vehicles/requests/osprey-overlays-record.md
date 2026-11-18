# The V-22 Osprey's overlays and parts, first round (18 Nov 2026)

**Status (19 Nov 2026):** three rounds; every overlay and part clean at full size and picked by the owner, the guns at real
size. Nothing is exported or shipped. This file started as the first round's record. Requests: [osprey-overlays.json](osprey-overlays.json),
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

The owner, on seeing the page: "These are SO MUCH BETTER."

**The owner's picks (19 Nov 2026):**

| Question | Answer | Made |
| --- | --- | --- |
| .50 gun | **(b), the compact one** | |
| Rescue hoist | **(b), drum on top** | Its mounting plate, chopped by Segment Anything, measured at full size and added back whole (`add_polygons`) |
| Inlet barrier | **(a), the coarser grille** | |
| The guns' size | **Real size** | Both shrunk about their mount by `derive_osprey_parts.py` (.50 to 1.65 m, 7.62 mm to 1.25 m); the 7.62 mm moved onto the .50's ramp mount, as the two are either/or on one mount |

The nine paid pictures are on `art-references` (`generated/vehicles/osprey/overlays/`), as generated, beside the earlier twelve.

## Export at game size (19 Nov 2026)

The owner's go. The background AI looked at the nine originals first (2.4 minutes on the processor beside the game; masks on K:,
the owner's chosen drive). [export_osprey.py](../export_osprey.py) then made 39 textures in under a minute, staged on K: with a review
page (`osprey-export/review/index.html` in the art working folder), each inspected at native size. **Not in the mod yet**: the
textures ship with the Osprey's Def.

- **Body:** 20.5 cells nose to tail, 18.3 wide, on a 19 x 21 canvas; east and south the north turned. Its 4K master is shrunk
  once to just above that size first (with the AI's mask), so the paint step stays within memory beside the game.
- **Paint:** the airframe's grey takes the main colour, the orange bands the second; glass is kept out by a saturation ceiling
  (`export_vehicle.classify`, new), a colourless pixel counts as the grey paint whatever its hue (new), and the bands' range starts
  at hue 0 for their one-pixel blend. One pixel is trimmed off the silhouette's outer edge (`export_body(trim=)`, new): the
  outline faded into the white through a grey ring that showed as a dotted rim. Pale unpainted pixels went from 2,054 to 0; 11
  painted pixels remain on the silhouette's edge, at one tail fin where the drawn outline is thinnest.
- **Overlays on the body's canvas:** probe, radar nose (a grey radome against the paint), hoist (b), jammer: bare metal.
- **Turrets:** both guns centred on their mount, barrel up; the mount on the centre line (the .50 was drawn 0.25 cells right).
- **Our renderer's pieces:** rotor and spinning disc at 11.61 m across; the left nacelle at 0 to 90 degrees, plain and with the
  inlet barrier, with paint masks; the barrier's grille kept out of the paint, in its drawn colours.
- **`Osprey.layout.json`:** hubs, nacelle pivots and gun mount in cells from the body's centre, for the Def and the renderer.

**Damage looks (19 Nov 2026):** for the design's outer parts, by script, free: the cockpit, both cabins, both wings and the tail
on the body's canvas, and each nacelle at every tilt (so the damage tilts with it; left and right differ). Each is the bulk
tanker's damage marks (our art) at the same size in game, a different stretch for each part, clipped to the part's mask from
Segment Anything. Agent placements, for the owner to revise. The export now makes 65 textures.

**Paint check:** pale unpainted pixels 0. A stand-in count from the mask alone (the parts-map checker needs the graphics card)
finds about 70 one- and two-pixel painted islands on the body, all inside panel seam lines: the Osprey's seams are a lighter,
purple-tinted grey, so the region rule paints some of their pixels. They read as a faintly dashed seam at full zoom. The
"repaired" paint, with a Segment Anything map of the body, is what removes them: a short graphics card job with the game closed.

**Repaired paint (19 Nov 2026, the owner's go with the game closed):** Segment Anything's parts map of the shrunk body, 2 min 42 s
on the graphics card (4,087 points, 3,239 parts; kept on K:), and the body exported with paint `repaired`. The seam-line islands
went from 70 to 49. The rest are painted bits inside the seam lines themselves, which the repair does not reach (it repaints an
island inside a part of another paint; here the line is the unpainted part). Treating the Osprey's lighter seam lines as outline
would remove them, at the cost of the darkest panels (the sponsons' sides) staying dark grey under any livery: put to the owner.

**Seams (19 Nov 2026, the owner's look at a map of the 49 flecks: "I am quite the perfectionist"):** the rims in the master
are clean; the fault was the export's. A seam's darkest pixels fell under the outline value and stayed bare, its lighter ones were
painted, so a hatch rim showed grey bars among dark red ones. Now each thin dark mark (a seam, a hatch rim, a rivet: darker than
0.45 and under 9 px wide, so a wide dark panel such as a sponson side is not caught) is taken whole and given the paint of the
panels around it, so a seam reads as a darker groove in the paint; one touching glass stays bare, and the silhouette's own outline
is left as drawn (`export_vehicle.thin_lines`, off unless a vehicle asks). Tried first the other way (seams never painted): bare
grey lines with a pale fringe, worse. Flecks 49 to 8 single pixels. No art was changed.

**Upgrade icons:** the owner's choice: all 17 drawn on the local model, overnight
([osprey-upgrade-icons.json](osprey-upgrade-icons.json): 34 pictures, about 1.5 hours; two of our drill rig's upgrade icons as
style references, with the private style model, as the last local batch).

## Next

1. The owner's picks of the 17 icons; then their export into `Osprey/Upgrades/`.
2. The Osprey's Def and renderer take these textures (the first batch in the design record).
