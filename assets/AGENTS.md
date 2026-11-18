# Art and sound rules

Moved from the repository's `AGENTS.md` on 18 Nov 2026 (owner's go-ahead), text unchanged; that file points here. These are
standing rules for every picture and sound the mods ship or the tools generate. The pipeline itself is in
`docs/development/art-pipeline.md`.

- Match RimWorld's look. Record how each texture or sound was made (tool, prompt, date, source hashes) in
  an asset record beside it; disclose AI assistance; never ship the game's or another mod's art. Check
  generator allowance and cost first; never silently buy credit or switch to a costlier generator.
- **Every generated image is saved on an art branch unless it is truly fugly** (owner, 8 Nov 2026), exactly as it was generated: the original file byte for byte with its record, the pick on `art-masters` and the rest on `art-references`. Crops, cut-outs and resizes are new files and never replace an original. A truly fugly image is left out with a one-line reason in its batch record; when in doubt, keep it. An image the near-copy check flags against the game's or another author's art stays local, off these public branches.
- **No white where background should be** (owner, 12 Nov 2026: white "on the borders, or inside the picture"): before exporting a new
  original, let the background AI look at it (`python scripts/art/clean_backgrounds.py masks --cpu --stop-at HH:MM`); the exporters then
  clear white the object encloses where the AI is sure it is background, and take the white out of soft edges. Re-exports go through
  `stage`, a review page and `apply`. The AI runs on the graphics card only with the game closed (`docs/development/background-cleanup.md`).
- **The AI looks at every picture we ship, at every export, not only new ones** (owner, 16 Nov 2026: "so cheap to run that we may as well use
  it for everything"): run `masks` before any export, re-export, crop or overlay, and **never judge a texture from the raw file alone**: a
  vehicle's painted parts are stored grey for the game to tint, so look at it tinted on dark ground before showing it to the owner. A paint
  mask (the `m` files) is never shipped speckled: any pixel left unpainted inside a painted part, or painted outside it, is a fault to fix
  before the owner sees it (the Pintle bowser's, 16 Nov 2026). **Paint is decided by region, then flecks repaired by Segment Anything's parts
  map** (the owner's picks of 16 Nov 2026: regions over parts alone, then the repair): run `scripts/art/paint_segments.py` on a new master and
  repaint with `repaired`; `python scripts/art/repaint_vehicles.py` stages, reviews and applies
  every body's masks again, and **`python scripts/art/paint_check.py` counts the visible flecks** left before the owner sees them
  (`docs/development/vehicle-paint-masks.md`).
- **Cut every vehicle master into its named parts with Segment Anything wherever a part earns it** (owner, 18 Nov 2026: "if we
  can extract some kind of benefit like that"): a damage look on the part its design's parts table names, the anchor of anything
  that turns or sits on the body (a rotor hub, a turret ring, a hook), a paint region, or the part as its own overlay. Prompt each
  part at the place the design or its plan drawing gives, show the owner the parts before anything is exported, and use the
  graphics card only with the game closed and on the owner's go.
- **Art the mods don't ship stays off `main`** (owner, 7 Nov 2026): full-size masters of shipped and picked art on the `art-masters`
  branch, reference drawings and superseded or comparison picks on `art-references`, each with its sidecar or record. Add to them only
  with `scripts/art/archive_branch.py`, which adds commits on top, never rewrites, and refuses game art, the private style model and
  its dataset, and model files. `assets/*/references/` is ignored on `main`; the scripts that draw references draw them again.
- **Paid generation follows `docs/development/paid-image-generation-rules.md`** (owner, 8 Nov 2026: the OpenAI image API
  for vehicle bodies, with the rules learnt in Phobos Ostranauts; 18 Nov 2026: other services through fal.ai where the owner
  prefers their look, under the same rules). **Only our own art is ever uploaded** to a provider: the
  game's textures, other authors' art and screenshots stay on this PC, and the engine refuses them as references. Every
  paid run has `--dry-run` first and `--max-usd` on the real run; agents never buy credit, raise the owner's account limit
  or move to a dearer quality unasked. **Before the first picture of a vehicle, measure its planned size against the biggest comparable vehicle on the owner's list, and generate at 1.5 times its in-game drawn size or more** (owner, 9 Nov 2026), so a later change of size never scales art up. One pilot picture, inspected at native size, before a family or a facing set; the
  camera instruction first in every prompt; each reference with a named role; bodies drawn empty (no cargo, crew, gear,
  damage or text: those are overlays or nothing); one correction per defect, then a change of approach; rejects archived
  with their reason; approved picks stay unless the owner reopens them.
- Sounds are quiet, never alarming, and under the one volume setting. **Our vehicles get a full set** (owner, 10 Nov 2026: as rich as
  GTek's Oshkosh L-ATV, doors and all), and **every vehicle and machine of ours sounds like itself**: a sample is shared only where the same
  physical thing makes it (a switch, a fuel nozzle, a kit, a regulator, the dashboard). Engines, doors, brakes and working machinery are never
  shared, and other mods' vehicles keep their authors' sounds. The plan and every sound's ID are in `docs/development/sound-catalogue.md`.
- **Sounds are made locally and judged by more than the ear** (owner, 10 Nov 2026; `docs/development/sound-generation.md`): Stable Audio 3
  through `scripts/art-run.py`, under the same safeguards as images, and **ffmpeg for every audio job** (owner: "a much finer tool"), never
  Python audio libraries. A pick is made from the listening page `scripts/art/sound_review.py` writes, with its spectrograms, measured flags
  and lowered copy; masters stay byte for byte. `art-run.py --cpu` runs small sound models on the processor beside the game (owner's request).
  **A listening report from the owner is evidence**: their ears caught a loop seam every flag missed, so turn each such report into a check.
- **Answer how loud or how made a game sound is with `python scripts/art/game_sounds.py`** (owner's request for toolkits, 10 Nov 2026): `find`,
  `measure` and `sounddef` read RimWorld's own sounds inside its `Assets.zip` archives and its SoundDefs, keeping no game file. A SoundDef
  plays at `volumeRange / 100`, so compare sounds by effective loudness, never by the file alone.
- **Answer what art is still a stand-in, borrowed or unrecorded with `python scripts/art-audit.py`** (owner, 17 Nov 2026), before reading
  texture records or Defs by hand: every shipped texture paired with its record and classed by the record's first statement of how it was
  made, and every picture a Def uses that belongs to another thing or is not ours (`--all` adds the accepted ones, `--mod`, `--kind`;
  `check`, part of the gate, fails on a texture with no record). **A record opens with how its picture was made**, and a borrowing the
  owner keeps goes in `config/art-audit.json` with its reason.
- **Ship a sound only through `python scripts/art/export_sound.py assets/phobos-sounds/exports.json`**: it levels the pick against a named game
  sound, mixes to mono at 44.1 kHz as the game's are, refuses a clip, whine, seam dip or gap, and writes the record beside the file; `--check`
  verifies them, and a test runs it. Never copy a sound into a mod by hand. Every SoundDef of ours carries
  `Phobos.Framework.Sounds.PhobosSoundVolumeExtension`, so D'thorga Framework's one volume setting scales it.
