# Art backlog: what to make with the local image model (6 Nov 2026, updated 7 Nov 2026)

Every texture and preview the Phobos RimWorld mods ship or still need, which of them suit the local image model, and what has to
happen before a run. A record only: nothing here has been generated for it. How the pipeline works is in the
[art automation design](art-automation-and-medical-equipment.md); the picks so far are in [its list of picks](art-automation-and-medical-equipment.md#the-owners-picks).

## Read this first (gotchas)

- **Every run needs the owner's go and a stop time** (owner rule). No GPU work while the owner plays; the runner refuses while
  RimWorld is open and frees the card's memory when it ends.
- **The model is good at one thing here: a single object seen from above**, a building or an item, on a plain background. It is poor
  at tiling textures and at any text, which stay scripted (agent judgement, from the runs so far). **Button icons are untested:** a
  game button icon is a simple painted object, so the model may do well; the first icon batch should try both routes side by side.
- **A giant red X on a button means a missing texture, not missing art.** It is the game's stand-in when a file isn't found, and
  it is a bug to fix in code straight away, before any new icon exists.
- **Text on Workshop previews is never generated.** The model draws a background or hero image; a script adds the mod's name.
- **Every picked machine and item has its Def since Medical Equipment 0.3.0.** Outstanding art: the electrolyser, the chamber's
  top-down redraw at 1 by 3, and the store's frost; each has a scripted placeholder or works without it.
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
| Electrolyser (2x2) | Scripted placeholder in 0.3.0; no pick: none suited (owner, 7 Nov) | A new batch with a fresh brief, top-down, square | Yes | Go |
| Liquid oxygen store (2x2) | Shipped pick, 0.3.0, one view (it doesn't rotate); the owner wants to see it in game before judging it | Nothing, unless the owner asks for a redraw after seeing it | | Owner's look in game |
| Liquid oxygen store's frost | Nothing; the store works without it (0.3.0) | One frost texture shown while it works, faded in and out by code, following DMS2's Pressure Chamber | Part: the model draws the rime, a script cuts it out | Go, then code to show it |
| Hyperbaric chamber (1x3) | Scripted placeholder in 0.3.0. Pick for its look: 102, drawn at an angle; rotating it doesn't fix that (tried 7 Nov) | A top-down redraw at 1 by 3 on a square canvas (the game draws beds on square textures), with 102 as the design reference; a new 1 by 3 layout drawing first | Yes | Go |
| Field oxygen kit (item) | Shipped pick, 0.3.0: oxygen-mask-104 | Nothing | | |
| Silver burn dressing (medicine) | Shipped pick, 0.3.0: 101 | Nothing | | |
| Manual resuscitator (item) | Shipped pick, 0.3.0: 103 | Nothing | | |
| Bedside oxygen stand | Shipped pick, 0.2.3 (replacing 0.1.1's) | Nothing | | |
| Burn unit bed | Shipped pick, 0.2.0. East and north are the top-down view turned | Nothing: a bed drawn from directly above needs no separate side views (agent judgement) | | |
| Ventilator (1x1) | Scripted placeholder in 0.7.0 | Generated art: a bedside cabinet from above with a breathing-wave screen, a bellows column and a tube to the bed | Yes | Go |
| Safety shower and eyewash (1x1) | Scripted placeholder in 0.7.0 | Generated art: a green drench head with a pull handle over a grated tray, and an eyewash bowl, from above | Yes | Go |
| Oxygen cylinder trolley (1x1, VGE) | Scripted placeholder in 0.7.0 | Generated art: a wheeled frame with two oxygen cylinders end-on, a regulator and a mask tube, from above | Yes | Go |
| Nebuliser (1x1) | Scripted placeholder in 0.7.0 | Generated art: a small compressor with a tube to a mask and a drug cup, from above | Yes | Go |
| Bronchodilator vial (item) | Scripted placeholder in 0.7.0 | Generated art: a strip of twist-top plastic vials | Yes | Go |
| Anaesthesia machine (1x1) | Scripted placeholder in 0.7.0 | Generated art: a trolley with a monitor, coloured vaporisers, flow meters and a breathing bag, from above | Yes | Go |
| Needle decompression kit (item, More Injuries) | Scripted placeholder in 0.7.0 | Generated art: a long needle with a green hub beside its sheath | Yes | Go |
| Fluids kit (item) | Scripted placeholder in 0.7.0 | Generated art: a clear bag of fluid with a label and a coiled giving set | Yes | Go |
| Steriliser (1x1) | Scripted placeholder in 0.7.0 | Generated art: a square autoclave with a round pressure door and locking wheel, from above | Yes | Go |
| Carbon monoxide and smoke alarm (ceiling) | Scripted placeholder in 0.7.0 | Generated art: a small white ceiling disc with a grille and a red light, seen from below | Yes | Go |
| Intubation kit (item) | Scripted placeholder in 0.7.0 | Generated art: a laryngoscope and a clear breathing tube with its cuff | Yes | Go |
| Workshop preview (640x360) | Shipped pick, 0.2.3 | Nothing | | |

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

Five textures are the owner's picks (7 Nov 2026); the base station and the two feedline atlases are still flat placeholders from `assets/phobos-radio/make_textures.py` (19 Oct 2026).

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Hand-held radio (worn item) | Shipped pick: handheld-2 (7 Nov) | | |
| Base station (2x2) | Placeholder | Yes | Candidates from 19 Oct exist (`assets/phobos-radio/generated`) |
| Whip antenna (drawn 1x1.5) | Stand-in: mast-remake-12, a thin lattice mast (owner, 7 Nov) | Yes | A real whip still to pick: tall and thin, drawn upright on a tall canvas |
| Mast (drawn 1x2.5) | Shipped pick: klein-mast-flat-63, a guyed lattice tower (7 Nov) | | |
| Coax feedline atlas and its blueprint atlas | Placeholder | Script | A 4x4 linked atlas whose edges must line up exactly |
| Coax build icon and signal gizmo icon | Shipped picks: coax-icon-12 and signal-icon-12 (7 Nov) | | |
| Workshop preview | Shipped pick: workshop-preview-3, titled with the current name by script (7 Nov) | | Remade in one command if the public name changes |

### D'thorga Water Tap

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Bottle tap (1x1) | Shipped pick: bottle-tap-redraw-12 (7 Nov), the hand-drawn layout redrawn | | |
| Workshop preview | Shipped pick: preview-water-tap-201 (7 Nov) | | |

### D'thorga Factory Additions

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Deep factory hopper and automated fabrication bay | Inherit Vanilla Expanded's art through their parent Defs (none of it copied) | Yes | Optional: own art would set them apart from the buildings they extend. Recommendation (agent): low priority |
| Workshop preview | Shipped pick: preview-factory-additions-202 (7 Nov) | | |

### D'thorga Vehicle Patches

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Mk. 82 and Mk. 84 bomb cases (items, 0.6.0) | Scripted stand-ins, drawn by `assets/vehicle-patches/draw_standins.py` (owner's direction, 7 Nov) | Yes | Two empty olive-drab bomb bodies from above, the Mk. 84 fatter: open nose ring, open tail, two lugs, no fins and no yellow band (the band marks a filling). The stand-ins can serve as layout references. Waiting on a go |
| Workshop preview | Shipped pick (7 Nov) | | |

### Button icons (owner, 6 Nov 2026)

| Button | Mod | Now | Needs | Local model? |
| --- | --- | --- | --- | --- |
| "Doctors may operate" | D'thorga Triage (button from D'thorga Framework) | Was a giant red X: the code asked for a game texture that doesn't exist (`UI/Icons/Medical/NormalCare`). Fixed in D'thorga Framework 0.3.1. Triage 0.1.2 ships the owner's pick, a scalpel over a red cross (not yet tested in game) | Nothing | Yes: the model did well (7 Nov) |
| Radio signal sheet | Phobos Radio Framework | Shipped pick: signal-icon-12 (7 Nov) | Nothing | Yes |
| Coax build icon | Phobos Radio Framework | Shipped pick: coax-icon-12 (7 Nov) | Nothing | Yes |
| Belt access toggle | D'thorga Factory Additions | Vanilla Expanded's conveyor icon, looked up at run time | Nothing | |
| Bottle tap's four arrows | D'thorga Water Tap | The game's own plus and minus buttons | Nothing | |
| Service, load switch, headlights, cab heater | D'thorga Framework (vehicle systems, 0.9.0) | Stand-ins drawn in code by `assets/vehicle-systems/draw_icons.py` (8 Nov 2026), each with a record | Generated icons, on the owner's go | Yes |
| Air conditioner, night heater, jump-start kit, booster pack | D'thorga Framework (vehicle systems, 0.16.0 and 0.18.0) | Stand-ins drawn by the same script, each with a record; the kit and pack are item textures too | Low priority: the owner rates the stand-ins well and wants them replaced only when little else is left (8 Nov 2026) | Yes |

**Any future button gets its icon in the same change**, or reuses a game or required-mod icon that is confirmed to exist; a lookup that
can fail falls back to a visible stand-in of our own, never silently to the red X (owner, 7 Nov 2026; in AGENTS.md).

### Mods with no preview yet

None since 7 Nov 2026. The merge into D'thorga Medical and D'thorga Patches retired the Burns and Smoke, Medical Save Bridge, Emergency
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
  the angle, are in [the view-angle models record](view-angle-models.md).

## Night 3 queue (owner's choices, 7 Nov 2026)

Prepared, not run: it waits for the owner's go and stop time. Plan `.local/art/night-3.json`; one command:
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

**Fixing the angle first:** the chamber redraw and the electrolyser are the test pieces for the depth-control trial in
[the view-angle models record](view-angle-models.md), on the owner's go; the rest of the list runs through the current route.
