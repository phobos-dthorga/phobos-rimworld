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

### D'thorga Medical Equipment

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

All eight textures are flat placeholders from `assets/phobos-radio/make_textures.py` (19 Oct 2026), to be replaced before the public beta.

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Hand-held radio (worn item) | Placeholder; an unpicked candidate sits uncommitted (step 2) | Yes | Small at game size, so it needs a bold silhouette |
| Base station (2x2) | Placeholder | Yes | Candidates from 19 Oct exist (`assets/phobos-radio/generated`) |
| Whip antenna (drawn 1x1.5) | Placeholder | Yes | Tall and thin: draw it upright on a tall canvas |
| Mast (drawn 1x2.5) | Placeholder | Yes | As above; 19 Oct mast candidates exist |
| Coax feedline atlas and its blueprint atlas | Placeholder | Script | A 4x4 linked atlas whose edges must line up exactly |
| Coax build icon and signal gizmo icon | Placeholder | Untested | Listed under button icons below |
| Workshop preview | Placeholder | Part | Waits on the framework's public name, still an open design question |

### D'thorga Water Tap

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Bottle tap (1x1) | Pillow-drawn placeholder (4 Nov), with the spout and handle at the back so a stack of bottles doesn't hide them | Yes | Keep that layout; the current drawing makes a good reference |
| Workshop preview | Placeholder, "replace before publication" | Part | |

### D'thorga Factory Additions

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Deep factory hopper and automated fabrication bay | Inherit Vanilla Expanded's art through their parent Defs (none of it copied) | Yes | Optional: own art would set them apart from the buildings they extend. Recommendation (agent): low priority |
| Workshop preview | Placeholder | Part | |

### Button icons (owner, 6 Nov 2026)

| Button | Mod | Now | Needs | Local model? |
| --- | --- | --- | --- | --- |
| "Doctors may operate" | D'thorga Triage (button from D'thorga Framework) | Was a giant red X: the code asked for a game texture that doesn't exist (`UI/Icons/Medical/NormalCare`). Fixed in D'thorga Framework 0.3.1. Triage 0.1.2 ships the owner's pick, a scalpel over a red cross (not yet tested in game) | Nothing | Yes: the model did well (7 Nov) |
| Radio signal sheet | Phobos Radio Framework | Flat placeholder | A proper icon | As above |
| Coax build icon | Phobos Radio Framework | Flat placeholder | A proper icon | As above |
| Belt access toggle | D'thorga Factory Additions | Vanilla Expanded's conveyor icon, looked up at run time | Nothing | |
| Bottle tap's four arrows | D'thorga Water Tap | The game's own plus and minus buttons | Nothing | |

**Any future button gets its icon in the same change**, or reuses a game or required-mod icon that is confirmed to exist; a lookup that
can fail falls back to a visible stand-in of our own, never silently to the red X (agent proposal, for the owner to confirm).

### Mods with no preview yet

D'thorga Framework, D'thorga Burns and Smoke, D'thorga Emergency Dosing, D'thorga Triage, D'thorga Vehicle Patches and D'thorga
Medical Save Bridge have no `About/Preview.png` (backgrounds for each were generated 6 to 7 Nov, not yet picked). Each needs a preview before the Workshop (Part: generated background,
scripted name).

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
