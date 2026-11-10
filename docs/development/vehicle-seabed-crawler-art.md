# The Seabed Crawler's picture: research and art record (11 Nov 2026)

**The pilot is picked and exported (Vehicles 0.38.0): the base hull and two refit hulls. The overlays follow under [the paid generation rules](paid-image-generation-rules.md), and nothing is bought before the owner sees the dry run's cost ceiling and says go. Nothing here is tested in game.** The owner asked on 11 November 2026 for the Crawler's art, as a research-and-record piece first. Agent-authored for the owner's review; every proportion and placement below is **[A]** authored unless it says otherwise. The boat's design is in [the Crawler record](vehicle-seabed-crawler.md); the real boat behind it is in [the military vehicles research](military-vehicles-research.md).

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

The north body on the 1024 x 2048 canvas: bow at the TOP edge (the Def puts the hydrophones at z 4 and the propeller at z -4, so +z is the bow), the hull about seven eighths of the canvas height and centred, on plain white for the cut-out. The export scales it to 9 cells long, the footprint (`--cells-long 9`, as the tanker's 2 x 6 took 6), and centres it on the 5 x 10 drawn canvas, 640 x 1280 px north; east and south are turned by script, west is mirrored by Vehicle Framework.

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

### The pilot, 11 November 2026 (owner's go at 01:45; agent's review, the owner picks)

**Run:** `art-run.py generate assets/phobos-vehicles/requests/seabed-crawler-pilot.json --stop-at 04:30 --max-usd 0.50 --near-copy-against C:/AI/lora-experiments/vanilla/dataset`. Four attempts, each returned at the requested 1024 x 2048, **US$0.0472 each, US$0.19 in all** (from the returned token counts; the dry run's ceiling was US$0.47). The originals and their per-image records are in `assets/phobos-vehicles/generated/seabed-crawler-body-north/` (ignored on `main`; they go to the art branches with the pick). Run report: `generated/run-20261111-0146.md`.

**Near-copy check, against the game-art set in `C:/AI/lora-experiments/vanilla/dataset`: attempt 2 is flagged** as too close to the game's Hemogen amplifier (`HemogenAmplifier_south.png`), on the outline hash alone (distance 11, the limit is 12; the structure score is 0.28, far under the 0.60 limit). Judged by eye it is a tall rounded silhouette that happens to hash alike, not a copy, **but the rule holds regardless: attempt 2 cannot be picked and stays local, off the public art branches.** Attempts 1, 3 and 4 are clear with room to spare: re-run on this batch alone with the fixed runner below, their nearest game pictures score 0.37 to 0.40 on structure and 25 to 33 on hash distance (limits 0.60 and 12).

**A fault in the runner found by this run, and fixed the same night:** `art-run.py` gave the checker the whole `generated` folder, so it compared every earlier batch again and flagged 38 pictures from other work (hoses, gizmo icons) alongside this one. Those 38 are old findings at the same thresholds, not new ones, and are out of this record's scope. The runner now passes `--only` for each request folder the batch wrote to (`scripts/art/near_copy.py`, `scripts/art-run.py`, with a test), and the checker read through `--only` takes a folder's own images, not its subfolders, so review sheets beside the attempts are left out.

**What all four got right:** straight overhead with no tilt or side faces; the bow at the top; a clean outline on pure white, so the cut-out is easy; the pale casing strip, a sail forward of midships with one orange band and a plane each side, four round thrusters at the flanks, a cross of stern planes around a shrouded propeller; nothing the overlays will draw (no wheels, arm, dome, mast or periscope); the outline weight and cel shading sit well beside the approved tanker. All four read as a submarine at half size.

**The one defect, in all four: the hull is slimmer than asked.** The prompt asked for a hull two fifths as wide as it is long; they came back at 0.24 to 0.28 (measured on the non-white pixels over the after half of the hull). Exported at the footprint length, 9 cells (the export scales to the footprint, as the tanker's 2 x 6 took `--cells-long 6`; the plan's "10" was a mistake, corrected here), that is **2.2 to 2.5 cells of hull and 2.9 to 3.3 cells across the planes, on a footprint 4 cells wide.** The trucks fill their footprints (the tanker's export was 1.96 cells wide on 2), and the owner chose 4 x 9 so the boat feels big, so this matters.

| Attempt | Across the planes, at 9 cells long | Hull alone | Notes |
| --- | --- | --- | --- |
| 1 | 2.86 cells | 0.24 of its length | the longest panelled casing; olive planes; the sail sits about a third back as asked; the slimmest |
| 2 | 3.27 cells | 0.25 | **flagged by the near-copy check, cannot be picked.** The widest silhouette; a big square blue-grey sail with three panels; small stern; a rounded, blunt bow; plainest finish |
| 3 | 2.91 cells | 0.28 | the fullest hull, egg-shaped; olive-bronze sail and planes; the strongest stern cross; **a few tiny glyph-like marks at the front of the sail** (would be painted out by script) |
| 4 | 3.12 cells | 0.26 | the neatest sail, chamfered with the orange band set into it; long casing; a propeller with visible blades; the sort step called it a near-duplicate of 1 by silhouette hash, though it looks distinct |

**Two ways to fix the width, for the owner to choose:**

1. **Free: widen the pick by script** to fill the footprint. At 1.35 times across, attempt 3 reaches 3.94 cells and attempt 1 3.87; attempt 4 overshoots to 4.20 and would want about 1.28. A sideways stretch turns round things oval (the thrusters, the propeller): small at 1.25 to 1.35, most visible on attempt 4's bladed propeller. The review sheet shows each attempt as made and widened, on its 5 x 10 canvas with the 4 x 9 footprint outlined and the tanker beside them.
2. **One paid correction** (rule 7: one targeted correction, then a change of approach): the pick as Image 1 with the role *this hull: keep its layout and finish exactly; make it wider*, and the sentence changed to give a measured width ("the hull's width is 40 percent of its length; at the sail, with the planes, it spans 45 percent of the canvas width"). About US$0.05 an attempt; the result can drift from the pick, as reference edits do.

**Agent's recommendation:** attempt 3 widened 1.35 times by script. It has the fullest hull, so it needs the least stretch for its circles; its stern reads best at half size; and it costs nothing more. Attempt 4 is the runner-up for its neater sail. The owner picks.

**Review sheets** (made by script, not shipped): `crawler-review-native.png` and `crawler-review-half.png`, written to the session's scratch folder and copied beside the originals in `generated/seabed-crawler-body-north/review/`.

### The pick and the export, 11 November 2026 (Vehicles 0.38.0)

**Owner's pick (11 Nov 2026, "your suggestions are fine", answering four proposals at once):** attempt 3 as the base hull, widened by script; **and attempts 1 and 4 as refit hulls**, the owner's own idea from the night before ("They're close enough in design that we could use them as variants when certain upgrade pathways are reached"). Attempt 1, with its long panelled casing, is the **laboratory refit**; attempt 4, with its neat faired sail, is the **sensor refit**. What each refit does is in [the Crawler record](vehicle-seabed-crawler.md#the-hull-refits-as-built).

**Widened by script, free,** with `assets/phobos-vehicles/widen_master.py`, new in this change: a sideways stretch about the hull's own centre line on a canvas of the same size, so length, bow and stern stay put. 1.35 times for attempts 1 and 3, 1.28 for attempt 4. On attempt 3 it also painted out the glyph-like marks on the sail (one box, 470,582 to 570,612 in the widened picture, filled with the median colour of a ring around it). The masters are untouched; each widened file has a `.widen.json` beside it with both hashes.

**Exported** with `scripts/art/export_vehicle.py body ... --cells-long 9 --draw 5 10 --main-hue 185 240 --accent-hue 8 32 --min-saturation 0.05`:

| Texture | From | Across, at 9 cells long |
| --- | --- | --- |
| `SeabedCrawler` | attempt 3 | 3.94 cells |
| `SeabedCrawler_Research` | attempt 1 | 3.86 cells |
| `SeabedCrawler_Sensor` | attempt 4 | 3.98 cells |

- **A change to the export tool:** the hull's grey-blue measured a saturation of only 0.06 to 0.12, under the tool's 0.16 floor for paint, so the hull would have had no mask and the repaint tool could not have touched it. `export_vehicle.py` takes `--min-saturation` now (default unchanged, with a test); 0.05 picks up the hull and still leaves the outline, the pale casing and the olive sail unpainted.
- **The masks, checked by script:** a red and green test paint recolours exactly the hull and the band. The Defs keep the house grey-blue (70, 78, 86) and give the band a muted orange (214, 124, 44); the exporter's own suggestion, which reproduces the master exactly, was a lighter (100, 104, 112) and (236, 128, 41).
- **The texture record** is `mods/PhobosVehicles/Textures/Phobos/Vehicles/SeabedCrawler/SeabedCrawler.record.md`, with every master's and widened file's hash. The export's transform files stay in `assets/phobos-vehicles/generated/export/`, ignored on `main`, as the tanker's do; the overlay batch needs `SeabedCrawler.transform.json` from there.
- **The refit hulls and the overlays:** the overlays are placed on the base hull's canvas. The refit hulls share its length, its centre and its stern and bow, but attempt 4's sail sits further forward than the other two and is narrower, so **an overlay on the sail top (the radar mast) will need a version per hull**; the bow dome, the flank strips, the wheels and the after-deck drill sit where all three agree. To be settled with the overlay batch, which still waits for its own dry run and the owner's go (ceiling about US$2.15).

**Archived:** the three picks' masters with their records, the widened files and the export transforms to `art-masters` (`vehicles/seabed-crawler/`, commit c258504); nothing went to `art-references`, since every clear attempt was picked. Attempt 2 stays on the owner's PC only, as the near-copy rule requires.

## Sources

- Wikipedia editors, [American submarine NR-1](https://en.wikipedia.org/wiki/American_submarine_NR-1) **[S]**: dimensions, planes on the sail, rudder, propellers and thrusters, the TV periscope.
- GlobalSecurity.org, [NR-1 design](https://www.globalsecurity.org/military/systems/ship/nr-1-design.htm) **[S]**: the orange sail, flat superstructure deck, boxy keel and external protrusions.
- The US Navy's NR-1 fact sheet as reproduced on NASA's [JASON VII expedition page](https://seawifs.gsfc.nasa.gov/jason/HTML/EXPEDITIONS_JASON_7_NR1.html) **[S, a secondary copy of a primary sheet]**: 44.4 m overall, 3.8 m diameter, 4.8 m beam at the stabilisers, four ducted thrusters.
- H.I. Sutton, [The US Navy's nuclear powered midget submarine: NR-1](https://www.hisutton.com/NR-1.html) **[S]**: the bottoming wheels and under-hull manipulators; his paint-scheme claim is unverified.
- The Trawler's and Dinghy's sizes: Vanilla Vehicles Expanded's installed Defs, read 11 November 2026 (other authors' files, not copied).
- Our own rules: [paid image generation rules](paid-image-generation-rules.md), [utility vehicles design](utility-vehicles-design.md) principle 8.

## Who chose what

- **Owner (11 Nov 2026):** the art now, as research and record first; 4 x 9 cells for weight and size; house colours; pilot then every visible node; attempt 3 as the base hull widened by script; attempts 1 and 4 as refit hulls on upgrade pathways (the owner's idea), mapped as the agent proposed.
- **Agent, owner to revise:** the 5 x 10 drawn size (a cell of margin each way, as every vehicle of ours); the canvas; the node places and what each overlay shows; the prompt; the hull as the main paint and the orange band as the accent for the pattern shader; leaving the Crawler's mass (400) and body size (7) as they are, which the owner may want to revisit now the boat is bigger.
