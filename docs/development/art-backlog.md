# Art backlog: what to make with the local image model (6 Nov 2026, updated 7 Nov 2026)

**Status (18 Nov 2026):** night 6's picks ship (Framework 0.137.1, Vehicles 0.65.4, Patches 0.23.0, Medical 0.18.1): every picture on
[the stand-in art audit](#stand-in-art-audit-17-nov-2026)'s drawn-in-code list, plus the battery warmer and Billy's tent lamp and heater;
the service lead, sonar ping and hitch came from the paid API (about US$0.33) after the local tries were turned down. Night 3's picks ship (Medical 0.14.1, Patches 0.16.9, Radio 0.1.7); the
store's frost is drawn in code (Medical 0.15.0); vehicle bodies and parts come from the paid API. Not done: style model v3 (its training was
cut off by the stop time on 8 Nov, with no model saved), the radio whip (held), Billy's canvas wall atlas and door, and the loading chute.

**Night 8 ran on 20 Nov 2026 (06:33 to 09:14, stop time 12:00; the owner's go: "queue any GPU-related work as I'm about to go to
bed")**: `.local/art/night-8.json`, all 61 images made, about 2.6 minutes each; graphics memory freed after. One near-copy flag
(fuel heater 11 against a bed, a plain white picture, likely false). **Picks pending**: the picking page is
`.local/art/night-8-review/index.html`, with the agent's leans; the agent suggests rewording the heavy-duty brakes (both read as a
turbocharger) and the clutch kit (a blank disc and an odd ring). In order: the rest of night 7's stretch icons (13 images: cab insulation's second seed, the
fuel heater, heavy-duty air filter, the three boost tunes and the clutch kit; the game opening ended that run), the queued **Deliver** and
**Drive back** icons, then two new requests, each subject in **two wordings by two seeds** (seeds 11 and 12):
`assets/vehicle-systems/requests/bulk-and-salvage-icons.json` (the **Pour** button, which borrows the game's eject picture as Deliver
does, and the **salvaged vehicle parts** item, which borrows the drivetrain kit's) and `assets/phobos-vehicles/requests/farming-icons.json`
(D'thorga Farming's **Harvest** and **Paddock** buttons and its six upgrade icons, now cut from the machines' own pictures; the Paddock
button has no picture of its own and shows the game's growing-zone one). **Agent's choices:** for the three fronts, the second wording
also takes the cut picture as a third reference so the drawn icon keeps the shipped front's shape; the "ships as" paths for Pour and
salvaged parts are proposals. **Left out, with reasons:** style model v3 (its caching stalled the last try; it needs its config looked at
first, and a night of its own), the farming machines' sounds (real machines take real recordings, by the owner's rule), the loading chute
(route not chosen) and Billy's canvas door (with its wall atlas, drawn by script).

**Old mod names below.** On 7 Nov 2026 Medical Equipment, Burns and Smoke, Emergency Dosing and Triage became parts of D'thorga Medical,
and D'thorga Vehicle Patches became part of D'thorga Patches ([mod consolidation](mod-consolidation.md)). The rows keep the names they
were written with.

**Update, 12 Nov 2026: most of this list is done.** The owner judged night 3's candidates and picked the medical second set, the
electrolyser, the chamber's top-down redraw, both bomb cases and the radio base station (Medical 0.14.1, Patches 0.16.9, Radio 0.1.7); the
paid image API drew the tipper, drill rig and generator set and their parts, and the Crawler's and survey truck's overlays (Vehicles
0.43.0). The store's frost is a shader, not a picture (owner). **Still outstanding:** the drill rig's well-pipe rack (refused twice by the
API, so it waits for the owner's GPU), the radio whip (held until the radio resumes), the Crawler's laboratory, scrubber and refit icons,
the survey truck's geophone spikes and resistivity reel (they ride on the moving boom), the new vehicles' damage pictures, the sonar ping
and triage tag icons, and the optional Factory Additions buildings. **Later the same day** the store's frost was drawn in code, the geophones and resistivity reel
were made and picked, and every new vehicle got damage pictures from the tanker's marks (Framework 0.100.0, Vehicles 0.44.0, Medical
0.15.0). **Later still (round three, 12 Nov 2026):** the Pintle trailer's own body and greedy boards, the tipper's loading grab, and the
recovery winch and long-range tank on our trucks, from the paid API ([round three](../../assets/phobos-vehicles/requests/round-three-record.md)). Every pick, flag and cost is in
[the night's record](../../assets/phobos-vehicles/requests/night-2026-11-12-record.md). The tables below are the record as last written on
7 Nov 2026.

Every texture and preview the Phobos RimWorld mods ship or still need, which of them suit the local image model, and what has to
happen before a run. Written on 6 Nov 2026, before anything on it was generated. How the pipeline works is in the
[art pipeline](art-pipeline.md); the picks so far are in [its list of picks](art-pipeline.md#the-owners-picks).

## Stand-in art audit (17 Nov 2026)

At the owner's request, every shipped picture (475 PNGs under `mods/**/Textures`) was paired with its asset record and every Def's
texture path checked. No vehicle body is drawn in code any more; what is left is mostly buttons. **This list supersedes the older rows
below where they differ.**

**The run is prepared** (17 Nov 2026): `python scripts/art-run.py night .local/art/night-6.json --stop-at HH:MM`, with the game closed.
Its dry run passed: 54 images, two seeds each (the bomb fills, the stand-in icons with two wordings for the work lamps, the Crawler's
icons and the triage tags), about two and a half hours. The requests for the first three were written with Billy on 17 Nov; only the
triage tags (`assets/phobos-medical/requests/triage-tags.json`, a disc and a luggage-style label, two wordings) are new. The tag is
exported with `export_sprite.py --silhouette`, so the game can tint it, and its outline is then drawn from the pick by script.

**Night 6 ran on 18 Nov 2026** (06:53, paused at 07:13 when the owner started the game, resumed 08:19 on the owner's word, finished 10:11),
all 62 candidates at about two and a half minutes each, with Klein 4B base and the private style model. **Agent's addition, for the owner
to revise:** `assets/phobos-vehicles/requests/night-6-extras.json`, run last: the litter "collect" button (it was missing from the list),
a battery warmer icon (it borrows the deep-cycle battery's), and a first try at Billy's tent lamp and heater. The near-copy check flagged
`sonar-ping-12` against a vanilla icon of four skulls on a cross; looked at by the agent, a false positive (concentric rings on a mostly
white ground). The picking page is `.local/art/night-6-review/index.html`, each candidate beside the picture the mod uses now.
**Agent's first read, not a pick:** the bomb fills, work lamps, fire bucket, water drop, grab, sonar ping, the Crawler's icons, the
triage silhouettes and the tent lamp look strong; the stretcher buttons (litter kit, unload, collect) came out as signboards, and the
hitch and stop-winch buttons read poorly, so those may want a second wording.

**Night 7 is queued (19 Nov 2026, the owner's go: "can begin when I hopefully go to bed")**: `.local/art/night-7.json`, started by a
watcher once RimWorld has been closed for ten minutes, stop time 15:00. Its dry run passed: 144 images and 39 sound clips, in this order:
the trucks' drawn armour kits as edits of each picked body's cab or bed (`assets/phobos-vehicles/requests/truck-armour-kits.json`, cut by
`assets/phobos-vehicles/draw_kit_crops.py`, the kit then lifted out with `scripts/art/edit_difference.py`), the seven kit icons, the
Osprey's 17 upgrade icons (its session picks them; their fast-rope and rescue-hoist pictures also serve the fittings), the flight and
fire-mode buttons drawn in code today, candidate sounds (`assets/phobos-sounds/requests/night-7.json`: the Osprey's first set, S112 to
S115 and an armour-plate fitting), then the borrowed icons as a stretch (`assets/vehicle-systems/requests/stretch-icons.json`).
**Agent's choice:** the edits run without the private style model, so the body's own look carries; three seeds each, seed 11 first.
**First eight (19 Nov 2026, 08:56 to 09:07, stopped when the game opened):** the local model redraws the cab rather than editing it
(shapes drift, some turned end to end, both ram bars put on the roof), so lifting a kit out by difference will mostly fail; one
armoured cab and one heavy-plates try keep the layout. The plan was reordered: icons, buttons and sounds first, then the ram bar and
body skirt **drawn alone** (`assets/phobos-vehicles/requests/truck-kit-parts.json`, fitted by script as the pipe rack was), then the
remaining cab edits. If the cab edits stay poor, the cab kits want the paid edit route, which held a body's shape (owner's choice).

**Drawn in code, for the next local run (owner's pick, 17 Nov 2026; a go and a stop time first):**

| Mod | Pictures |
| --- | --- |
| D'thorga Framework | Fittings: **work lamps** (a redraw, owner, 16 Nov: the last try looked like headlights), auxiliary fuel tank, ferry tank, litter kit; the cold-weather family; hitch, unhitch, winch, stop winch; litter unload, service lead, water drop, sonar ping, the station's grab; the fire bucket (an item seen on the map) |
| D'thorga Vehicles | The Crawler's laboratory, advanced laboratory, laboratory refit, scrubber and sensor refit icons. **The loading chute** (one cell, Vehicles 0.64.0): a stand-in drawn by `assets/phobos-vehicles/draw_bulk_standins.py`; route not chosen; not in the night-6 run. (The bulk bunker has its own picture since Vehicles 0.64.1, OpenAI, the owner's pick of 17 Nov 2026; attempt 2 kept on `art-references` for other uses.) |
| D'thorga Patches | The Mk. 84 fills: incendiary, smoke and tox canisters and the EMP coil pack (briefs in the Vehicle Patches table below) |
| D'thorga Medical | The triage tag and its outline |

**Borrowed pictures:**

| Thing | Uses | Status |
| --- | --- | --- |
| Billy's canvas walls and door, tent lamp, tent heater | The game's smooth wall atlas and simple door (tinted canvas), standing lamp and heater, by path | **Own pictures in a later batch** (owner, 17 Nov 2026); the wall atlas from a script, as its edges must line up |
| Framework 0.123.0's seven fittings, three boost tunes and the clutch kit | Our own nearest icons: brake pad set (heavy-duty brakes), oil cooler (transmission cooler), the dashboard's starter and cab lamps (gear-reduction starter, cab insulation), block heater (fuel heater), deep-cycle battery (battery warmer), snorkel (heavy-duty air filter), the engine tunes' three (low, standard, high boost), drivetrain kit (clutch kit) | **Shipped, Framework 0.191.1** (night 8, the owner's picks of 20 Nov 2026), but for the heavy-duty brakes and the clutch kit: **redraw after the style model's training** |
| Pintle P4 shower trailer | The bowsers' body | Kept: the owner's choice (17 Nov 2026) |
| Submarine test hull | The Crawler's body | Kept: dev-only, cannot be built |
| Deep hopper and fabrication bay | Vanilla Expanded's art, through their parents | Optional, low priority (below) |
| Liquid oxygen store's button, belt toggle, tap arrows | The game's or Vanilla Expanded's own icons, in place | Allowed reuse, not stand-ins |
| The gathering run's **Gather** and **Gather what** buttons (Framework 0.143.0) | The game's haul designator and stone-chunks category icons, by path in `CompProperties_PhobosGatherRun` | **Shipped, Framework 0.191.1**: gather-run-11 and gather-what-11 (the owner's picks of 20 Nov 2026) |
| **Deliver** (Framework 0.175.0) | The game's eject picture, `UI/Commands/PodEject` (the cryptosleep casket's) | **Shipped, Framework 0.191.1**: deliver-12 (night 8, the owner's pick of 20 Nov 2026) |
| **Purge the tank** button (Framework 0.242.0) | Our own tank-contents icon, `Tank/Contents` | **Own picture in the next local night run** (the owner's go of 23 Nov 2026): request `assets/vehicle-systems/requests/purge-tank-icon.json`, to ship as `Tank/Purge`; drawn on both style models, vanilla v1 and house-v1 (agent's choice, 23 Nov 2026), 4 pictures; **night 11 ran on 23 Nov 2026** (10:04 to 10:59, the owner's go before heading out): 16 tries in four wordings, two seeds more of the first (`purge-tank-icon-more.json`, on the owner's word to fill the time with more tries), nothing flagged; **cards on the Companion's Picks page, waiting for the owner's pick** |
| **Drive back after delivering** (Framework 0.175.0) | Our depot's **Go to bay** picture, `Stations/BayGoTo` | **Shipped, Framework 0.191.1**: deliver-drive-back-12, a truck in a U-turn arrow (night 8, the owner's pick of 20 Nov 2026) |


**Kept as they are:** the load switch (owner, 10 Nov 2026), the store's frost (drawn in code by design) and the coax atlases (drawn by
script; records added 17 Nov 2026). **Held:** the whip antenna (until the radio resumes) and the drill rig's well-pipe rack (refused
twice by the paid API). The audit also gave the bulk tanker's seven upgrade icons and the coax atlases the records they lacked, and
removed "placeholder" from comments over art that had shipped.

## Read this first (gotchas)

- **Every run needs the owner's go and a stop time** (owner rule). No GPU work while the owner plays; the runner refuses while
  RimWorld is open and frees the card's memory when it ends.
- **The model is good at one thing here: a single object seen from above**, a building or an item, on a plain background. It is poor
  at tiling textures and at any text, which stay scripted (agent judgement, from the runs so far). **Button icons are untested:** a
  game button icon is a simple painted object, so the model may do well; the first icon batch should try both routes side by side.
- **A giant red X on a button means a missing texture, not missing art.** It is the game's stand-in when a file isn't found, and
  it is a bug to fix in code straight away, before any new icon exists.
- **Text on Workshop previews is never generated.** The model draws a background or hero image; a script adds the mod's name.
- **Every picked machine and item has its Def since Medical Equipment 0.3.0.** Outstanding art on 7 Nov: the electrolyser, the chamber's
  top-down redraw at 1 by 3, and the store's frost. **Done since:** the electrolyser and the chamber shipped in Medical 0.14.1 (12 Nov
  2026), and the frost is drawn in code (Medical 0.15.0).
- **Art the mods don't ship lives off main** (owner, 7 Nov 2026): full-size masters on the `art-masters` branch, references and
  superseded picks on `art-references`, added with `scripts/art/archive_branch.py`.
- **Everything that ships keeps an asset record** (tool, model, prompt, seeds, hashes, date, AI disclosure) and passes the near-copy
  check first. Output of the private style model may ship, by the owner's choice (option A, 5 Nov 2026).

## Before the first run

| # | Step | Who | Blocks |
| --- | --- | --- | --- |
| 1 | **Give a go and a stop time** | Owner | Every run |
| 2 | **Decide what happens to the two radio images changed in the working tree since 19 Oct** (`Handheld.png` and `Preview.png`, each with an `.export.json`). They are an old session's uncommitted exports of candidates nobody picked | Owner: keep for review, or discard | The radio batch, so it starts from a clean tree |
| 3 | **Radio art: pick from the 19 Oct candidates, or remake them.** Recommendation (agent): remake them through the current runner with references and the style model, as they were made before that route existed | Owner | The radio batch |
| 4 | **Done 6 Nov:** a flat top-down stand-in for the hyperbaric chamber, `assets/phobos-medical/references/HyperbaricChamber-large.png` (1024 x 1536, the 2:3 shape), drawn by `draw_placeholders.py HyperbaricChamber`. The model redraws its layout instead of inventing angles | Agent, no GPU | Done |
| 5 | **Done 6 Nov:** request files `assets/phobos-medical/requests/second-batch.json`, `assets/water-tap/requests/redraw.json`, `assets/phobos-previews/requests/backgrounds.json` and `assets/phobos-radio/requests/remake-1.json`, and `scripts/art/make_preview.py`, which puts the house name and the mod's name on a generated background | Agent, no GPU | Done |
| 6 | **Proposed 6 Nov, for the owner to confirm:** see [map sizes](#map-sizes-agent-proposal) below | Agent proposes, owner confirms | Exporting those three, not generating them |
| 7 | Optional: **clean the PC first.** The card reached about 88 C with thermal slowdown during image generation on 5 Nov | Owner and Hiddensoul | Nothing; runs are only slower |
| 8 | Optional: Hiddensoul's ComfyUI workflow, and whether to accept FLUX 3 Image Klein's licence | Owner | Nothing; the current models work |

## The list

**Local model?** "Yes" means a single object or picture the model handles; "Script" means it stays drawn by code; "Part" means the
model draws the picture and a script does the rest.

### D'thorga Medical (Medical Equipment's art)

Since 7 Nov 2026 Medical Equipment is a part of D'thorga Medical, and the five medical mods share one Workshop preview: Medical
Equipment's picked background, re-titled by script.

| Asset | Now | Needs | Local model? | Waiting on |
| --- | --- | --- | --- | --- |
| Oxygen concentrator (1x1) | Shipped pick, 0.2.3 | Nothing | | |
| Electrolyser (2x2) | **Shipped pick, Medical 0.14.1 (12 Nov 2026): top-down 12 from night 3.** Was a scripted placeholder in 0.3.0; no pick: none suited (owner, 7 Nov) | Nothing | Yes | Done |
| Liquid oxygen store (2x2) | Shipped pick, 0.3.0, one view (it doesn't rotate); the owner wants to see it in game before judging it | Nothing, unless the owner asks for a redraw after seeing it | | Owner's look in game |
| Liquid oxygen store's frost | **Drawn in code, Medical 0.15.0 (12 Nov 2026)**: the owner ruled a picture unneeded (12 Nov: "the frost was a shader"), and night 3's four attempts redrew the whole store at an angle | Nothing | Script | Done |
| Hyperbaric chamber (1x3) | **Shipped pick, Medical 0.14.1 (12 Nov 2026): depth 11 from night 3's depth-control trial.** Was a scripted placeholder in 0.3.0; pick 102 for its look was drawn at an angle, and rotating it didn't fix that (tried 7 Nov) | Nothing | Yes | Done |
| Field oxygen kit (item) | Shipped pick, 0.3.0: oxygen-mask-104 | Nothing | | |
| Silver burn dressing (medicine) | Shipped pick, 0.3.0: 101 | Nothing | | |
| Manual resuscitator (item) | Shipped pick, 0.3.0: 103 | Nothing | | |
| Bedside oxygen stand | Shipped pick, 0.2.3 (replacing 0.1.1's) | Nothing | | |
| Burn unit bed | Shipped pick, 0.2.0. East and north are the top-down view turned | Nothing: a bed drawn from directly above needs no separate side views (agent judgement) | | |
| Ventilator (1x1) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a bedside cabinet from above with a breathing-wave screen, a bellows column and a tube to the bed | Yes | Done |
| Safety shower and eyewash (1x1) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a green drench head with a pull handle over a grated tray, and an eyewash bowl, from above | Yes | Done |
| Oxygen cylinder trolley (1x1, VGE) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a wheeled frame with two oxygen cylinders end-on, a regulator and a mask tube, from above | Yes | Done |
| Nebuliser (1x1) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a small compressor with a tube to a mask and a drug cup, from above | Yes | Done |
| Bronchodilator vial (item) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a strip of twist-top plastic vials | Yes | Done |
| Anaesthesia machine (1x1) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a trolley with a monitor, coloured vaporisers, flow meters and a breathing bag, from above | Yes | Done |
| Needle decompression kit (item, More Injuries) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a long needle with a green hub beside its sheath | Yes | Done |
| Fluids kit (item) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a clear bag of fluid with a label and a coiled giving set | Yes | Done |
| Steriliser (1x1) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a square autoclave with a round pressure door and locking wheel, from above | Yes | Done |
| Carbon monoxide and smoke alarm (ceiling) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a small white ceiling disc with a grille and a red light, seen from below | Yes | Done |
| Intubation kit (item) | Shipped pick, Medical 0.14.1 (12 Nov 2026, from night 3); scripted placeholder in 0.7.0 | Generated art: a laryngoscope and a clear breathing tube with its cuff | Yes | Done |
| Workshop preview (640x360) | Shipped pick: hero-medical-2, a field hospital tent at dusk (10 Nov 2026, replacing the 7 Nov ward) | Nothing | | |

### Proposed medical equipment (agent ideas, 6 Nov 2026, for the owner)

Ideas only: **nothing here is designed, built or approved**, and the logic is for a later session. Checked against the owner's mods on
6 Nov 2026 by searching every installed mod's Def labels: More Injuries already has the defibrillator, airway suction, CPR, saline,
blood bags, splints and tourniquets; VFE Medical has the IV stand; Hemogen Pack covers transfusion. MediStation's "Go-juice Nebulizer" is
a recreational drug dispenser, and Rimefeller's autoclave is a chemical refinery, so neither covers what is proposed here. Atmosphere
Overhaul (installed, switched off) and Vanilla Gravship Expanded both have oxygen tanks, which the existing-effects rule says to use where
they are active.

| Equipment | Kind and size | What it would treat, in our mods | Art brief | Local model? |
| --- | --- | --- | --- | --- |
| **Ventilator** | Bedside facility, 1x1 | A closing airway or smoke-damaged lungs (Burns and Smoke). Organ Failure's ventilator can't appear without Questionable Ethics, and the owner wants our own | A wheeled cart with a screen, an accordion bellows in a clear cylinder, and corrugated tubing. South, east and north | Yes |
| **Safety shower and eyewash** | Building, 1x1, stands against a wall | First aid for chemical and acid burns and caustic gas (Burns and Smoke). Lab practice is long flushing with water (the usual figure is 15 minutes, from memory, unverified). Dubs Bad Hygiene's water could feed it as an optional tie-in | A green pipe stand with a wide shower head, a triangular pull handle and an eyewash bowl with a push flag. South, east and north | Yes |
| **Carbon monoxide and smoke alarm** | Wall-mounted, 1x1 | Warns before carbon monoxide builds up in a room (Burns and Smoke) | A small round white alarm with a test button and a red light. Wall-mounted, so each view sits against a wall | Yes, small; a scripted version as a backup |
| **Medical oxygen cylinder** | Item, and a cylinder trolley as a 1x1 building | Our own oxygen when neither Vanilla Gravship Expanded nor Atmosphere Overhaul is active (already in the design) | A tall cylinder with a regulator and a contents gauge. Colour code to check: Australian medical oxygen cylinders are, from memory, black with a white shoulder, unverified | Yes |
| **Nebuliser** | Bedside facility, 1x1 | Eases airway spasm after smoke (Burns and Smoke), delivering a drug from Emergency Dosing | A small compressor box with a clear medicine cup and mask on a short tube | Yes |
| **Anaesthesia machine** | Facility beside a surgery bed, 1x1 | Safer surgery, tying in Emergency Dosing's anaesthetics | A cart with coloured vaporisers on top, glass flow tubes, a bellows and a breathing circuit. South, east and north | Yes |
| **Medical steriliser** | Building, 1x1 or 2x1 | Lower infection risk for surgery in its room; overlaps with room cleanliness, so the lowest priority (agent judgement) | A steel chamber with a round pressure door and a gauge, steam vent on top | Yes |

**Recommendation (agent):** the ventilator, the safety shower and the alarm first. Each fills a gap nothing else in the owner's mods covers,
and each answers a condition Burns and Smoke already gives. The cylinder follows from the oxygen design. The nebuliser and anaesthesia
machine are good tie-ins once Emergency Dosing has a way for machines to give drugs. The steriliser is the weakest case.

### Phobos Radio Framework

Five textures are the owner's picks (7 Nov 2026); the base station and the two feedline atlases are still flat placeholders from `assets/phobos-radio/make_textures.py` (19 Oct 2026). **Since 12 Nov 2026 the base station is the owner's pick too** (Radio 0.1.7); the two atlases stay scripted.

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Hand-held radio (worn item) | Shipped pick: handheld-2 (7 Nov) | | |
| Base station (2x2) | **Shipped pick: base-station-remake-2-13 from night 3 (12 Nov 2026, Radio 0.1.7)**; was a placeholder | Yes | |
| Whip antenna (drawn 1x1.5) | Stand-in: mast-remake-12, a thin lattice mast (owner, 7 Nov) | Yes | A real whip still to pick: tall and thin, drawn upright on a tall canvas |
| Mast (drawn 1x2.5) | Shipped pick: klein-mast-flat-63, a guyed lattice tower (7 Nov) | | |
| Coax feedline atlas and its blueprint atlas | Placeholder | Script | A 4x4 linked atlas whose edges must line up exactly |
| Coax build icon and signal gizmo icon | Shipped picks: coax-icon-12 and signal-icon-12 (7 Nov) | | |
| Workshop preview | Shipped pick: workshop-preview-3, titled with the current name by script (7 Nov) | | Remade in one command if the public name changes |

### D'thorga Water Tap

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Bottle tap (1x1) | Shipped pick: bottle-tap-redraw-12 (7 Nov), the hand-drawn layout redrawn | | |
| Workshop preview | Shipped pick: hero-watertap-2 (10 Nov, replacing preview-water-tap-201) | | |

### D'thorga Factory Additions

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Deep factory hopper and automated fabrication bay | Inherit Vanilla Expanded's art through their parent Defs (none of it copied) | Yes | Optional: own art would set them apart from the buildings they extend. Recommendation (agent): low priority |
| Workshop preview | Shipped pick: hero-factory-2 (10 Nov, replacing preview-factory-additions-202) | | |

### D'thorga Vehicle Patches

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Mk. 82 and Mk. 84 bomb cases (items, 0.6.0) | **Shipped picks from night 3, seed 12 each (12 Nov 2026, Patches 0.16.9).** Were scripted stand-ins, drawn by `assets/vehicle-patches/draw_standins.py` (owner's direction, 7 Nov) | Yes | Two empty olive-drab bomb bodies from above, the Mk. 84 fatter: open nose ring, open tail, two lugs, no fins and no yellow band (the band marks a filling). The stand-ins served as layout references |
| Mk. 84 bomb fills: incendiary, smoke and tox fill canisters, EMP coil pack (items, Patches 0.20.0) | **Scripted stand-ins** (17 Nov 2026), drawn by `assets/vehicle-patches/draw_standins.py`, which also serve as layout references | Yes | Three sealed canisters from the side, tilted like the cases: olive with a light red band (incendiary), olive with a light green band (smoke), grey with a dark green band (tox), each with a filler cap and a handle; and a steel frame holding three copper coils (EMP). Waits for the owner's go for GPU work |
| Workshop preview | Shipped pick: hero-patches-1 (10 Nov, replacing the 7 Nov garage) | | |

### Button icons (owner, 6 Nov 2026)

| Button | Mod | Now | Needs | Local model? |
| --- | --- | --- | --- | --- |
| "Doctors may operate" | D'thorga Triage (button from D'thorga Framework) | Was a giant red X: the code asked for a game texture that doesn't exist (`UI/Icons/Medical/NormalCare`). Fixed in D'thorga Framework 0.3.1. Triage 0.1.2 ships the owner's pick, a scalpel over a red cross (not yet tested in game) | Nothing | Yes: the model did well (7 Nov) |
| Radio signal sheet | Phobos Radio Framework | Shipped pick: signal-icon-12 (7 Nov) | Nothing | Yes |
| Coax build icon | Phobos Radio Framework | Shipped pick: coax-icon-12 (7 Nov) | Nothing | Yes |
| Belt access toggle | D'thorga Factory Additions | Vanilla Expanded's conveyor icon, looked up at run time | Nothing | |
| Bottle tap's four arrows | D'thorga Water Tap | The game's own plus and minus buttons | Nothing | |
| Vehicle buttons: service, headlights, heaters, air conditioner, climate control, field fix, caravan repair, fittings, dive, surface, stay down, settle, the depot bay and station buttons, and the tanker's ten | D'thorga Framework | Shipped picks from the night of 10 Nov 2026, chosen by the owner, each with a record; the masters are on `art-masters` | Nothing: the bulk order's redraw with a stack of coins (gizmo-bulkorder-coins-13) shipped the same day | Yes: the model did well on simple objects |
| Load switch | D'thorga Framework | Stand-in drawn by `assets/vehicle-systems/draw_icons.py`, kept by the owner (10 Nov 2026) | Every generated version drew a medical cross or a blank plate, likely from the medicine references; try neutral references if it is redrawn | Yes |
| Dashboard warning lights (15) | D'thorga Framework | Shipped picks from the night of 10 Nov 2026: white shapes the dashboard tints, exported with `export_sprite.py --silhouette` | Nothing | Yes |

**Fitting icons, 16 Nov 2026:** the owner picked local klein-base pictures for the snorkel (head 12), the recovery winch and the all-terrain
tyres (Framework 0.113.0). **The work lamps fitting keeps its code-drawn stand-in, to be regenerated later** (owner, 16 Nov 2026: the new
attempt looked like headlights; compare it with the headlights button when redrawing). Still drawn in code: the auxiliary fuel tank, ferry
tank, litter kit and the cold-weather family icon.

**Countermeasure cartridge, 17 Nov 2026:** the owner picked a local klein-base picture (the square box, seed 11) for the cartridge item
(Framework 0.119.0); it is also the icon of the Harrier's Chaff and flares button, so that button needed no picture of its own.

**Any future button gets its icon in the same change**, or reuses a game or required-mod icon that is confirmed to exist; a lookup that
can fail falls back to a visible stand-in of our own, never silently to the red X (owner, 7 Nov 2026; in AGENTS.md).

### Mods with no preview yet

None since 10 Nov 2026, when D'thorga Vehicles got hero-vehicles-2 and Framework hero-framework-1 (owner's picks). Since 7 Nov 2026 the merge into D'thorga Medical and D'thorga Patches retired the Burns and Smoke, Medical Save Bridge, Emergency
Dosing, Triage and Missile Patches previews: D'thorga Medical uses Medical Equipment's picked background and D'thorga Patches Vehicle
Patches', both re-titled by script. The earlier backgrounds stay on the `art-masters` branch.

### Needing no art

- Burns and Smoke's conditions: conditions have no sprites.
- The water tap's arrows use the game's plus and minus buttons.
  Reusing the game's own UI icons in place is allowed; copying them into our files is not.

## Map sizes (agent proposal)

Proposed 6 Nov 2026 for the owner to confirm. Generation doesn't need them; exporting each pick to game size does.

| Building | Size | Why |
| --- | --- | --- |
| Electrolyser | 2x2 | The owner's pick is drawn square, so it exports without a redraw. A real stack with its water and gas plumbing is about a cabinet a metre or two across (our estimate, unsourced) |
| Liquid oxygen store | 2x2 | Also a square pick. Real stores are an upright insulated tank beside finned vaporisers, about that footprint at small hospital scale (our estimate, unsourced) |
| Hyperbaric chamber | 1x3 (built 7 Nov; first proposed 2x3) | A one-patient chamber is about a bed long. A bed holds one patient per tile of width, so 2 wide would hold two; the pedestal moved to the foot |

## Suggested order (agent proposal)

1. **No GPU, now:** steps 4 and 5, and the agent half of step 6.
2. **Night 1, objects and icons:** the three button icons, by model and by script side by side; concentrator redraw, hyperbaric chamber from its stand-in, the four radio objects, the bottle tap. Two
   seeds each to start with (11 and 12 have done best), references plus the style model.
3. **Night 2, previews:** one background per mod, with the names added by script, in one house layout.
4. **When the Defs exist:** export the electrolyser, store, mask and frost, and make their east and north views.
5. **Later:** retrain the style model on our own approved art instead of the game's sprites, once enough picks exist. Two have shipped
   and three more are picked; the number to wait for is an open question.

## Style model: v1 kept, v3 next (owner, 7 Nov 2026)

- **v1 stays the model in use.** v2 (2500 steps at 768 and 1024, 6 to 7 Nov) showed no gain: without references both give soft,
  product-render images, and v2 drifts more into the angled view the owner dislikes.
- **v3, on the owner's go and a stop time:** a larger private dataset of the game's own art, **buildings and furniture, items and
  button icons** (no plants, animals or pawns), with **a sharper upscale**: enlarge pixel-exact first, then a light clean-up, because
  the smooth upscale of tiny sprites may be what teaches the soft look (agent's reading, untested). Then v1, v2 and v3 on the same
  prompts and seeds, with references and without.
- **The cost:** more training images, more chances of a near-copy, and a slower check; everything stays on the owner's PC and in `.local/`, never committed.
- **Caption the view** (agent proposal, 7 Nov 2026): every v3 training caption and every request carries one fixed phrase for the view,
  "seen from directly above, south face hinted", so the view becomes a word the model can be asked for. Why, and the other ways to fix
  the angle, are in [the view-angle models record](image-routes-research.md#view-angle-models-research-record-7-nov-2026).

### Training: what a run needs (agent's preparation, 20 Nov 2026, for the owner's decision)

**Decided (owner, 20 Nov 2026): train on our own local picks, tonight, starting when the owner goes to bed** (a watcher starts it once
RimWorld has been closed ten minutes). The set is **house-v1**: the 151 local Klein pictures on `art-masters`, byte for byte, each
captioned with its request's subject and the trigger word `phobosdthorga`, built by `lora_experiment.py picks` (new, 20 Nov 2026). The
fifteen dashboard glyphs carry "flat solid black silhouette, no shading" in their captions, so that look is tied to those words
(agent's choice). Plan `.local/art/night-9.json`, dry run passed: 3,000 steps at 768 and 1024 with the text encoder unsqueezed
(`low_vram` off), the same training squeezed as the fallback step, install as `phobos-private-house-v1`, then the four redraws (paddock,
standard grain front matched to the wide front's pick, heavy-duty brakes, clutch kit), each wording drawn three ways: the old model
with references, the new one with references, the new one alone. About five hours. **A caveat, said plainly:** these pictures were
themselves made with v1 and references, so the new model learns the house style through v1's eyes and may also learn its habits; the
"alone" route shows how much it took in. Night 8's picks stay final (owner: "pick now, keep them").

**Night 9 ran on 21 Nov 2026 (06:02 to 11:01, stop time 12:30; the owner's go at bedtime: "queue up what we had planned for the
GPU")**: `.local/art/night-9.json`, every step finished. The service yard's fuel pump was added first and last (agent's choice:
`assets/vehicle-systems/requests/fuel-pump.json` on v1 with our depot bay and parts bench as style references, and
`fuel-pump-house.json` on the new model), since Framework 0.210.0 cannot be installed without it. **The training finished all 3,000
steps in 158 minutes** with `low_vram` off (no caption-caching stall this time); the squeezed fallback only re-saved the finished
model; installed as `phobos-private-house-v1`. 58 pictures in all, about 2.5 minutes each; graphics memory freed after.
**What the comparison shows (agent's reading, owner not yet judged):** the new model alone draws the house look (thick near-black
outline, flat shading) without any reference, which v1 and v2 never did; it still mistakes objects when a wording is vague (the
heavy-duty brakes' "c" wording drew a bearing). 14 pictures are flagged: nine only against our own pictures in the training set
(archived since the check tells our own art apart, 21 Nov 2026), five against the game's art, kept local by the rule (three against
`IconMorbidA`, one each against `FleshmassPieceC` and `MedicineUltratech_a`; first miscounted as three). The standard grain front's
right-looking pictures are flagged at 0.97 against the wide front's pick: they are the wide front again, so the agent suggests cutting a
shorter header from that pick by script. **Picked 21 Nov 2026** (`assets/phobos-vehicles/requests/night-9-picks-record.md`): the
fuel pump ships in Framework 0.210.1 and the paddock button in Farming 0.7.3, both house-v1 pictures. **The rest went to OpenAI's
image API** (the owner's choice, to match the parts pictures; about US$0.42 for eight): the heavy-duty brakes, the clutch kit and the
charging cable's overlay (an edit of the pump's pick, cut by the new `scripts/art/export_overlay.py`) in Framework 0.210.2, and the
standard grain front in Farming 0.7.4. Nothing from the night-8 and night-9 redraw list is left.

**In short: v3 is half-prepared and needs one night of about five hours. Before that, the owner decides whether to train on the game's
art (v3 as planned) or on our own shipped picks.** Read from the files in `C:/AI/lora-experiments/` with no GPU work; nothing changed.

- **What stalled on 8 Nov:** not the training. Before training, ai-toolkit stores what each caption means (its "text embeddings").
  v1 did that at about 8.5 s a caption; v2 slowed from 10 s to 25 s or more; v3 started at 17 s and settled near 30 s, so the 57 minutes
  covered 122 of 240 captions and training never began.
- **What survived:** the 122 stored captions (244 files, 1.8 GB, in `vanilla-v3/dataset/_t_e_cache`) and all 240 stored pictures.
  ai-toolkit reuses them, so a new run starts at caption 123.
- **The likely cause (agent's reading, unconfirmed):** the settings run the text encoder squeezed for a small card (`low_vram`, with the
  encoder in float8), and it slows as memory fills. The three configs have identical settings, so the size of the job is the difference.
  **Proposed fix, to try first:** turn `low_vram` off (on a 12 GB card the 4B model and the encoder in float8 should both fit) and watch
  the first five captions; if a caption still takes more than about 10 s, put it back and simply let the caching finish.
- **Time, from v2's figures:** about 1 hour to store the remaining 118 captions (at the slow rate), about 2.3 hours for 3,000 steps at 768
  and 1024, and about 1.5 hours for the comparison (36 images). **About 5 hours; one night with a stop time.** The runner already holds
  the plan (`.local/art/night-3.json`, steps 7 to 10).
- **The owner's decisions:**
  1. **Train at all?** v1 with references is doing well (132 shipped pictures are local Klein picks). v3's aim is a sharper look and a
     straight-down view; whether it delivers is untested.
  2. **Which art to learn from:** v3 as planned (240 of the game's own sprites, private, never shipped), or **our own shipped picks**
     (the "later" step in the suggested order above: there are now 132 local picks plus 143 paid pictures in the mods). Our own art
     teaches the house style and has no question about learning from another studio's art; the game's art teaches the vanilla look more
     directly. **Agent's lean: our own picks**, compared with v1 on the same prompts and seeds, but that needs a new dataset built and
     captioned first (CPU work, done beforehand), and the 118 captions still to store for v3 would go unused.
  3. **Which night**, and a stop time.

## Night 3 queue (owner's choices, 7 Nov 2026)

**Ran on 8 Nov 2026 (04:57 to 09:20), on the owner's go; the owner judged it on 12 Nov** ([the night's record](../../assets/phobos-vehicles/requests/night-2026-11-12-record.md)). Steps 1 to 6 finished. Step 7, training v3, was cut off by the stop time after 57 minutes with no model saved, so steps 8 to 10 never started. As prepared on 7 Nov: plan `.local/art/night-3.json`; one command:
`python scripts/art-run.py night .local/art/night-3.json --stop-at HH:MM`. The dry run passed every step. Four seeds a piece (11 to 14),
references plus the v1 style model, and the fixed view phrase "seen from directly above, south face hinted" in every prompt.

| Step | What | Images | Estimate (night 2's rate, about 3 to 4 minutes an image) |
| --- | --- | --- | --- |
| 1 | Fetch the depth-control add-on (RefControl, thedeoxen, Apache 2.0, hash recorded) | | a minute |
| 2 | The second set of medical equipment (11 pieces), the electrolyser and the chamber's top-down 1 by 3 redraw, each from its stand-in drawn large | 52 | about 3 hours |
| 3 | Depth-control trial: the electrolyser and chamber with depth maps drawn from their stand-ins | 8 | about 30 minutes |
| 4 | The liquid oxygen store's frost, from the shipped pick (a script cuts it out later) | 4 | about 15 minutes |
| 5 | The radio base station and a real whip | 8 | about 30 minutes |
| 6 | The Mk. 82 and Mk. 84 bomb cases, from their stand-ins | 8 | about 30 minutes |
| 7 | Train style model v3: 240 private game sprites (126 buildings and furniture, 83 items, 31 command buttons; no plants, animals, pawns, ruins, fleshmass or void art), enlarged pixel-exact, captions carrying the view phrase, 3000 steps at 768 and 1024 | | about 3.5 hours |
| 8 to 10 | Install v3 and draw the first batch's 9 subjects through it (style model alone, and with references) on seeds 11 and 12, beside v1's and v2's images from earlier nights | 36 | about 2 hours |

**About 10 hours in all**: everything but training and the comparison is done in the first 5. A stop time cuts the latest steps first.

## Next requests (7 Nov 2026)

The store's frost (its code waits for it, owner), the electrolyser, the chamber's top-down 1 by 3 redraw, a real whip, the radio base
station, the second set of medical equipment
([its record](medical-equipment-2.md)) and the four vents ([ventilation and hydrogen](ventilation-and-hydrogen.md)). Every new button
gets its icon in the same change (owner rule, 7 Nov 2026).

**Since then (12 Nov 2026):** every item on this list shipped or was settled, except the whip (held until the radio resumes) and the
four vents (ventilation is not built).

**Fixing the angle first:** the chamber redraw and the electrolyser are the test pieces for the depth-control trial in
[the view-angle models record](image-routes-research.md#view-angle-models-research-record-7-nov-2026), on the owner's go; the rest of the list runs through the current route.
