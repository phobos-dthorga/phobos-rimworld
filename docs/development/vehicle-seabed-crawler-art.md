# The Seabed Crawler's picture: research and art record (11 Nov 2026)

**Record and plan. The pictures follow under [the paid generation rules](paid-image-generation-rules.md), and nothing is bought before the owner sees the dry run's cost ceiling and says go. Nothing here is tested in game.** The owner asked on 11 November 2026 for the Crawler's art, as a research-and-record piece first. Agent-authored for the owner's review; every proportion and placement below is **[A]** authored unless it says otherwise. The boat's design is in [the Crawler record](vehicle-seabed-crawler.md); the real boat behind it is in [the military vehicles research](military-vehicles-research.md).

## Read this first

- **Owner's choices (11 Nov 2026):** the boat itself is **4 x 9 cells, drawn 5 x 10** ("I want the players to feel the weight and size of it"; it was 3 x 8 drawn 4 x 9); the hull wears **D'thorga house colours**, the Def's grey-blue with one muted orange accent, so the fleet matches, not NR-1's orange sail; and the paid batches run as **the body pilot first, then every visible node's overlay**, built or not.
- **Canvas 1024 x 2048**, the survey truck's. Drawn 10 cells at 128 px a cell is 1280 px in game; the rule asks 1.5 times that, 1920 px, and this canvas gives 1.6. Overlays use the same canvas so they register over the body without scaling.
- **The Crawler sets its own mark.** The biggest boat on the owner's list is Vanilla Vehicles Expanded's Trawler, 2 x 5 cells drawn 3 x 6 (its north picture 384 x 768, 128 px a cell); the Dinghy is 2 x 4 drawn 3 x 5. Nothing else on the list is a Vehicle Framework boat (RimShips is subscribed but off and has no vehicle Defs). Measured 10 and 11 November 2026 from the installed Defs.
- **What must read at half size**, the zoomed-out view players use most: a long cigar hull, a pale casing strip down its middle, a sail forward of midships, and a stern with planes and a shrouded propeller. Everything smaller is a bonus.
- **What the body never shows:** wheels, the arm, domes, the mast, sensor strips, the drill, a raised periscope, wake, water, lamps, text. Those are the six overlays below, the game's own effects, or nothing.
- **The submerged look needs no picture.** D'thorga Framework draws the game's shadow rectangle for a spotted hull (the footprint times 1.15) and a faint mark for the player's own boat; both follow the new size by themselves (`Submersibles.cs`, in [the submersibles framework](submarine-framework.md)).
- **The six visible nodes are placed on the canvas before the first picture**, so a later node never redraws the body (the owner's rule of 8 November 2026 for every upgradeable vehicle).

## The real boat from above

NR-1 (US Navy, launched 25 January 1969) as the sources describe it:

- About 45 m long overall with a 3.8 m hull diameter and a maximum beam of 4.8 m at the stern stabilisers; about 400 t **[S, Wikipedia; S, the Navy fact sheet as reproduced on NASA's JASON VII page]**.
- A bright orange sail, a flat superstructure deck and a boxy keel, with several external protrusions, some retractable, in contrast with the smooth black hulls of attack boats **[S, GlobalSecurity design page]**.
- Diving planes on the sail, a conventional rudder aft, two propellers and four ducted thrusters, two forward and two aft **[S, Wikipedia; S, the Navy fact sheet]**.
- Two retractable rubber-tyred bottoming wheels under the keel, hydraulic manipulators under the hull, a television periscope on a fixed mast **[S, H.I. Sutton; S, Wikipedia]**.
- Sutton alone writes that the orange was part of a research-and-rescue cover and that the boat may have been painted black on covert missions: **unverified**, from one author, and of no consequence here since the owner chose house colours.

**What we keep [A]:** the cigar hull with the flat casing, the sail a third of the way back from the bow with its planes, four ducted thrusters at the flanks, a shrouded propeller with a cross of stern planes, and the wheels, arm and sensors as separate overlays, as they are separate things on the real boat. **What we drop [A]:** the orange sail, the markings, and the real proportions: NR-1 is twelve times longer than it is wide, ours is drawn twice as long as it is wide, a gameplay shape the owner chose so the boat has presence on a map.

## The canvas and the overlay plan

The north body on the 1024 x 2048 canvas: bow at the TOP edge (the Def puts the hydrophones at z 4 and the propeller at z -4, so +z is the bow), the hull about seven eighths of the canvas height and centred, on plain white for the cut-out. The export scales it to 10 cells long and centres it on the 5 x 10 drawn canvas, 640 x 1280 px north; east and south are turned by script, west is mirrored by Vehicle Framework.

The game gives a 4-wide hull the columns x -1 to 2 and a 9-long one z -4 to 4 (`GenAdj.OccupiedRect`, the game's `all.cs:14627-14631`), so the centreline runs between columns 0 and 1. Each visible node, its place in Def cells, and what its overlay draws **[A, owner to revise]**:

| Node | Built? | Place (Def cells, bow +z) | The overlay shows | Layer |
| --- | --- | --- | --- | --- |
| `BottomingWheels` | yes | (-1, 2) and (2, 2), the flanks beside the sail | two small dark wheel housings bulging from the hull's sides | 1 |
| `Hydrophones` | yes | the flanks from z 1 to z 4 | a thin pale sensor strip along each flank, ending at the bow | 1 |
| `ActiveSonar` | no | (0..1, 4), the bow tip | a round pale dome capping the bow | 1 |
| `Manipulator` | no | (-1, 3), the port foredeck | a folded two-joint arm lying on its cradle | 1 |
| `RadarMast` | no | (0..1, 2), the sail top | a short mast with a bar antenna, stowed flat along the sail | 1 |
| `SeabedDrill` | no | (0..1, -1), the after-deck | a squat hoist frame over a drill well in the casing | 1 |

The two built nodes are wired as `graphicOverlays` on their `UpgradeNode`s as soon as their pictures are picked; the four unbuilt nodes' pictures ship as files with their records and are wired when each node is built. Damage overlays (the tanker has them; the Crawler has none) are a follow-on, not in this work. The dev-only submarine test hull is the same size and takes the Crawler's body once it exists. The drill rig and tipper still borrow the tanker's picture: out of scope here.

## The prompts

The pilot request is `assets/phobos-vehicles/requests/seabed-crawler-pilot.json`: `openai`, quality `medium`, 1024 x 2048, four attempts, one reference (the approved bulk tanker body, `bulk-tanker-body-north-2.png`, role *approved vehicle of the same maker: match its outline weight, shading, palette and finish only; do not copy its shape or layout*). Its prompt, as sent:

```text
ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking straight down at the object's TOP SURFACE ONLY. Rectangular edges run horizontally and vertically on the canvas. Show no vertical front or side faces.

Create an ORIGINAL game vehicle sprite for the D'thorga seabed crawler: a research submarine that drives along the bottom of lakes and coasts for a hard-scrabble frontier colony, seen surfaced. Image 1 is our approved truck from the same maker: match its outline weight, shading, palette and finish only. Do NOT copy its shape or layout.

Draw the BARE HULL only. Wheels, the manipulator arm, sonar domes, the radar mast, sensor strips and the drill are drawn separately as overlays.

Tall canvas, twice as tall as wide. The hull is about two fifths as wide as it is long, with the stern planes a little wider than the hull. It fills seven eighths of the canvas height, centred, on a plain pure white background. North facing: the rounded bow at the TOP edge, the propeller at the BOTTOM edge. Cool dark grey-blue steel hull plating, a pale grey flat deck casing strip along the centre line, a dark steel sail with one muted orange band around it, a dark bronze propeller shroud, dark rubber-lipped thruster ducts.

Its deck plan, from top to bottom: a smoothly rounded bow; the pale casing strip running from just behind the bow to the stern; a rounded-rectangle sail a third of the way back from the bow, wider than the casing, with one short diving plane out each side; one small round ducted thruster let into each flank near the bow and one near the stern, four in all; a gently tapering stern ending in a cross of four small planes around a round shrouded propeller.

Style: a top-down sprite for the game RimWorld: a thick near-black outline around every shape, soft flat cel shading with two or three tones per material, pale panels, rounded corners, a few small clean details, no texture noise. Not a photograph, not a 3D render, not pixel art.

No periscope, mast, antenna, arm, wheels, dome, open hatch, wake, water, waves, foam, cargo, people, animals, turrets, dust, damage, text, letters, numbers, logos, dials, screens or lamps. No floor, ground, sea, shadow outside the vehicle, perspective, tilt or visible side faces. Top surfaces only.
```

The overlay requests (`seabed-crawler-overlays.json`, written after the pick) use the same skeleton with the pick as Image 1 (*our approved hull: match its outline weight, shading and finish; draw only the [part] in its place; everything else pure white*), each with its `fit_box_in_master_pixels` computed from the pick's `transform.json` and the table above, three attempts each.

## The flow and the costs

1. `python scripts/art-run.py generate assets/phobos-vehicles/requests/seabed-crawler-pilot.json --dry-run`: the cost ceiling and anything the API would refuse, with nothing sent. **Then the owner's go.**
2. The real run with `--stop-at`, `--max-usd 0.50` and `--near-copy-against C:/AI/lora-experiments/vanilla/dataset` (a paid batch whose only references are our own art runs no near-copy check by itself). A paid-only run needs no graphics card and may run beside the game.
3. Review every attempt at native and half size against the checklist in the paid rules, written below; the owner picks or asks for one correction; one correction, then a change of approach.
4. The overlay batch the same way, about eighteen images; then `scripts/art/export_vehicle.py body` and `overlay`, the Defs, and the art branches (`art-masters` for the pick and masters, `art-references` for the rest with reasons).

**The dry run of 11 November 2026** (`art-run.py generate ... --dry-run`, nothing sent): the engine's ceiling is **up to US$0.119 an image at 1024 x 2048 medium, so at most about US$0.47 for the four pilot attempts**; no refusal. The ceiling is the engine's upper bound before the API reports token counts; the tanker's 768 x 1536 images came in at US$0.0295 each, so the real cost should be well under it. The overlay batch of about eighteen images would have a ceiling near US$2.15 on the same figure. **The run itself waits for the owner's go.**

## The review

Attempts not yet made. Each attempt's review (camera, silhouette, cut-out, what reads at half size beside the tanker, nothing painted that the framework draws, near-copy result, returned size), the pick, any deviation from the plan and the archive commits are written here the day they are made.

## Sources

- Wikipedia editors, [American submarine NR-1](https://en.wikipedia.org/wiki/American_submarine_NR-1) **[S]**: dimensions, planes on the sail, rudder, propellers and thrusters, the TV periscope.
- GlobalSecurity.org, [NR-1 design](https://www.globalsecurity.org/military/systems/ship/nr-1-design.htm) **[S]**: the orange sail, flat superstructure deck, boxy keel and external protrusions.
- The US Navy's NR-1 fact sheet as reproduced on NASA's [JASON VII expedition page](https://seawifs.gsfc.nasa.gov/jason/HTML/EXPEDITIONS_JASON_7_NR1.html) **[S, a secondary copy of a primary sheet]**: 44.4 m overall, 3.8 m diameter, 4.8 m beam at the stabilisers, four ducted thrusters.
- H.I. Sutton, [The US Navy's nuclear powered midget submarine: NR-1](https://www.hisutton.com/NR-1.html) **[S]**: the bottoming wheels and under-hull manipulators; his paint-scheme claim is unverified.
- The Trawler's and Dinghy's sizes: Vanilla Vehicles Expanded's installed Defs, read 11 November 2026 (other authors' files, not copied).
- Our own rules: [paid image generation rules](paid-image-generation-rules.md), [utility vehicles design](utility-vehicles-design.md) principle 8.

## Who chose what

- **Owner (11 Nov 2026):** the art now, as research and record first; 4 x 9 cells for weight and size; house colours; pilot then every visible node.
- **Agent, owner to revise:** the 5 x 10 drawn size (a cell of margin each way, as every vehicle of ours); the canvas; the node places and what each overlay shows; the prompt; the hull as the main paint and the orange band as the accent for the pattern shader; leaving the Crawler's mass (400) and body size (7) as they are, which the owner may want to revisit now the boat is bigger.
