# Art backlog: what to make with the local image model (6 Nov 2026)

Every texture and preview the Phobos RimWorld mods ship or still need, which of them suit the local image model, and what has to
happen before a run. A record only: nothing here has been generated for it. How the pipeline works is in the
[art automation design](art-automation-and-medical-equipment.md); the picks so far are in [its list of picks](art-automation-and-medical-equipment.md#the-owners-picks).

## Read this first (gotchas)

- **Every run needs the owner's go and a stop time** (owner rule). No GPU work while the owner plays; the runner refuses while
  RimWorld is open and frees the card's memory when it ends.
- **The model is good at one thing here: a single object seen from above**, a building or an item, on a plain background. It is poor
  at tiling textures, at tiny flat UI icons and at any text. Those stay scripted (agent judgement, from the runs so far).
- **Text on Workshop previews is never generated.** The model draws a background or hero image; a script adds the mod's name.
- **Five machines and items have no Def yet.** Their art can be generated now, but its export size waits for each one's size on
  the map.
- **Everything that ships keeps an asset record** (tool, model, prompt, seeds, hashes, date, AI disclosure) and passes the near-copy
  check first. Output of the private style model may ship, by the owner's choice (option A, 5 Nov 2026).

## Before the first run

| # | Step | Who | Blocks |
| --- | --- | --- | --- |
| 1 | **Give a go and a stop time** | Owner | Every run |
| 2 | **Decide what happens to the two radio images changed in the working tree since 19 Oct** (`Handheld.png` and `Preview.png`, each with an `.export.json`). They are an old session's uncommitted exports of candidates nobody picked | Owner: keep for review, or discard | The radio batch, so it starts from a clean tree |
| 3 | **Radio art: pick from the 19 Oct candidates, or remake them.** Recommendation (agent): remake them through the current runner with references and the style model, as they were made before that route existed | Owner | The radio batch |
| 4 | **Draw a flat stand-in for the hyperbaric chamber**, as was done for the concentrator, so the model redraws a layout instead of inventing angles | Agent, no GPU | The chamber |
| 5 | **Write the request files** for each batch below, and a preview template script that puts the mod's name on a generated background | Agent, no GPU | Batches 2 and 3 |
| 6 | **Set map sizes** for the electrolyser (2x1 or 2x2), the liquid oxygen store and the hyperbaric chamber (2x3 in the brief) | Agent proposes, owner confirms | Exporting those three, not generating them |
| 7 | Optional: **clean the PC first.** The card reached about 88 C with thermal slowdown during image generation on 5 Nov | Owner and Hiddensoul | Nothing; runs are only slower |
| 8 | Optional: Hiddensoul's ComfyUI workflow, and whether to accept FLUX 3 Image Klein's licence | Owner | Nothing; the current models work |

## The list

**Local model?** "Yes" means a single object or picture the model handles; "Script" means it stays drawn by code; "Part" means the
model draws the picture and a script does the rest.

### D'thorga Medical Equipment

| Asset | Now | Needs | Local model? | Waiting on |
| --- | --- | --- | --- | --- |
| Oxygen concentrator (1x1) | Hand-drawn stand-in, which the owner likes for now | A better redraw without the generated picks' hourglass shapes; the stand-in as reference | Yes | Go |
| Electrolyser | Pick: seed 11 | Export, then east and north views with the pick as reference | Yes | Its Def and map size |
| Liquid oxygen store | Pick: seed 12 (seed 11 kept) | Export and views, as above | Yes | Its Def and map size |
| Liquid oxygen store's frost | Nothing | One frost texture shown while it works, faded in and out by code, following DMS2's Pressure Chamber | Part: the model draws the rime, a script cuts it out | The store's Def |
| Hyperbaric chamber (2x3) | No pick: the owner disliked the angles | Stand-in (step 4), then a redraw from it | Yes | Steps 4 and 6 |
| Oxygen mask and regulator (item) | Pick: seed 11 (seed 12 kept) | Export at item size | Yes | Its Def |
| Burn dressings (item) | Nothing | A first batch | Yes | Owner's approval of the item itself (agent idea) |
| Manual resuscitator (item) | Nothing | A first batch | Yes | Owner's approval of the item itself (agent idea) |
| Bedside oxygen stand | Shipped pick, 0.1.1 | Nothing | | |
| Burn unit bed | Shipped pick, 0.2.0. East and north are the top-down view turned | Nothing: a bed drawn from directly above needs no separate side views (agent judgement) | | |
| Workshop preview (640x360) | None | A preview | Part | Step 5 |

### Phobos Radio Framework

All eight textures are flat placeholders from `assets/phobos-radio/make_textures.py` (19 Oct 2026), to be replaced before the public beta.

| Asset | Now | Local model? | Note |
| --- | --- | --- | --- |
| Hand-held radio (worn item) | Placeholder; an unpicked candidate sits uncommitted (step 2) | Yes | Small at game size, so it needs a bold silhouette |
| Base station (2x2) | Placeholder | Yes | Candidates from 19 Oct exist (`assets/phobos-radio/generated`) |
| Whip antenna (drawn 1x1.5) | Placeholder | Yes | Tall and thin: draw it upright on a tall canvas |
| Mast (drawn 1x2.5) | Placeholder | Yes | As above; 19 Oct mast candidates exist |
| Coax feedline atlas and its blueprint atlas | Placeholder | Script | A 4x4 linked atlas whose edges must line up exactly |
| Coax build icon and signal gizmo icon | Placeholder | Script | Tiny flat icons, clearer drawn by code |
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

### Mods with no preview yet

D'thorga Framework, D'thorga Burns and Smoke, D'thorga Emergency Dosing, D'thorga Triage, D'thorga Vehicle Patches and D'thorga
Medical Save Bridge ship no textures and no `About/Preview.png`. Each needs a preview before the Workshop (Part: generated background,
scripted name).

### Needing no art

- Burns and Smoke's conditions: conditions have no sprites.
- Triage's patient care button uses the game's own medical care icon; the water tap's arrows use the game's plus and minus buttons.
  Reusing the game's own UI icons in place is allowed; copying them into our files is not.

## Suggested order (agent proposal)

1. **No GPU, now:** steps 4 and 5, and the agent half of step 6.
2. **Night 1, objects:** concentrator redraw, hyperbaric chamber from its stand-in, the four radio objects, the bottle tap. Two
   seeds each to start with (11 and 12 have done best), references plus the style model.
3. **Night 2, previews:** one background per mod, with the names added by script, in one house layout.
4. **When the Defs exist:** export the electrolyser, store, mask and frost, and make their east and north views.
5. **Later:** retrain the style model on our own approved art instead of the game's sprites, once enough picks exist. Two have shipped
   and three more are picked; the number to wait for is an open question.
