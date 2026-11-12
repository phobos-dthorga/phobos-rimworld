# Background clean-up: white left where background should be (12 Nov 2026)

**Owner's report (12 Nov 2026):** "a lot of literal white-space in our assets, where instead there should be transparency, especially all
of the ones that were just generated through the ChatGPT pipeline ... It's usually either on the borders, or inside the picture, like
with the hook lift. The Pintle trailer is another one." The owner asked for an AI tool, having had poor results from non-AI methods, and
gave the go for a processor run first and the graphics card overnight if still needed.

## Read this first

- **Nothing in the mods changes until the owner has looked.** The tool re-exports into `.local/art/clean-stage`, writes a dark review
  page to `.local/art/clean-review/index.html`, and only `apply` copies textures into the mods.
- **To review:** open `.local/art/clean-review/index.html`. Before is on the left, after on the right, both on dark ground. Flagged
  textures are first and are never applied unless the owner says so.
- **To apply:** `python scripts/art/clean_backgrounds.py apply` (add `--skip CargoHook.png` for each one to leave as it is). It updates
  the hashes in the texture records itself. Then the gate, a version and a changelog entry, as for any change.
- **The AI never runs on the graphics card while the game is open**, and its processor runs hide the card (the guard is tested).

## What was wrong: three causes

| Where it shows | Cause | Fix |
| --- | --- | --- |
| **Inside the picture** (the cargo hook's opening, the sling set's triangle and ring, the trailer's drawbar) | Our cut-out cleared only white it could reach from the picture's edge. White the object encloses looks the same as white paint (a bed sheet) to any rule | **AI**: BiRefNet marks what is background; enclosed white is cleared where it is sure, spreading only through white it does not take for the object |
| **On the borders, icons and items** | `export_sprite.py` shrank the cut-out without premultiplying, so the clear background's white could bleed into edge pixels (measured small: see below) | Shrink premultiplied, as the vehicle exporter already did |
| **On the borders, everything** | Soft edge pixels kept the white they were blended with | Take the white back out of each soft edge pixel: it showed alpha times the object plus the rest white, so the object's colour comes back exactly (`unmixed`) |

The second and third need no AI: the background was pure white, so the white is subtracted, not guessed. That is why they are safe where
"magic wand" tools are not, which guess where the background is.

## The AI

- **BiRefNet** (Zheng Peng and colleagues), MIT licence, through ComfyUI's own support (ComfyUI ships the code; we fetched only the
  weights, Comfy-Org's single file, 444 MB, SHA-256 checked by `scripts/art/fetch_models.py`). BRIA's RMBG-2.0 was set aside: its weights
  are for non-commercial use only. Sources: [ComfyUI's guide](https://docs.comfy.org/tutorials/utility/remove-background-birefnet),
  [Comfy's model page](https://comfy.org/p/supported-models/birefnet), [RMBG-2.0's card](https://huggingface.co/briaai/rmbg-2.0).
- **It looks at the full-size original, never the shipped texture.** Measured 12 Nov 2026: on 128 px icons it called the sling set's
  triangle part of the object; on the 1024 px originals every leftover white patch scored 0.00 to 0.06 (background) and the hook's white
  metal highlight 1.00 (object).
- **It can be wrong about white paint shaped like a hole.** It took the gas alarm's white ring (paint between two dark outlines) for
  background, and the first staging cleared 26% of that picture. So **a texture that loses more than 12% of its picture is flagged and
  held back**; `apply --include NAME` applies one after the owner has looked. Real openings (the sling set's triangle) may trip the same
  flag, which is the price of never clearing paint unseen.
- **A patch is cleared whole or not at all.** Where the AI is right (a hook's opening, gear holes, the sling set's triangle) it clears
  the whole white patch; where it is wrong (the Diagnose clipboard's paper, gauge faces, the survey dish, a thermometer's tube) it is
  unsure and clears blotches. So an enclosed patch of near-pure white is cleared only if the AI would clear 90% of it or more, and a
  patch put back takes its soft rim with it (authored, with tests). One wrong clear still passed: a small wedge of the stock engine
  gauge's face, which the dial's arc cuts off as a patch of its own; it was skipped by hand.
- **Measured before tuning:** on dark ground the edges of our icons and vehicles were already clean (the edge changes touched 38 pixels
  of the cargo hook and a few hundred of a truck). What the owner saw is enclosed white, which is the AI's part.
- **Its answers are kept** in `.local/art/bgmasks`, one mask per original, named by the original's SHA-256. The exporters read them
  (`export_sprite.ai_background`), so every export from then on clears what the AI calls background; a new original needs `masks` first.
- **Speed:** about 15 s a picture on 8 processor threads beside the game; the 305 originals behind our textures took about 76 minutes.

## The commands

```text
python scripts/art/clean_backgrounds.py plan                          which originals, and how many the AI has not seen
python scripts/art/clean_backgrounds.py masks --cpu --stop-at 02:00  the AI on the processor, below-normal priority, away from the game
python scripts/art/clean_backgrounds.py masks --gpu --stop-at 07:00  the same on the graphics card: refused while the game runs
python scripts/art/clean_backgrounds.py stage                         re-export everything into staging and write the review page
python scripts/art/clean_backgrounds.py apply [--skip NAME ...]       copy the reviewed, unflagged textures into the mods
```

## What it can and cannot rebuild

- **Icons and items:** from their 141 export records, which hold every setting.
- **Vehicle overlays:** from their export records and the two placement scripts (`export_batch_b.py`, `export_round_three.py`).
- **Vehicle bodies:** from their records, or for the tipper, drill rig and generator set (whose records were not kept) from their
  transforms. Their paint hue settings were not always recorded, so **any body whose paint mask moves is flagged and not applied**: its
  settings were guessed, and the paint would change.
- **Not rebuilt:** the damage pictures (cut by darkness, not by white), Pillow-drawn icons (no white background), and any texture whose
  original is not on this PC; `stage` lists them.

## Safeguards found and fixed on the way

- **The first processor trial loaded the model on the graphics card** while the owner played (about 20 seconds, 0.4 GB), because an
  empty `CUDA_VISIBLE_DEVICES` does not hide the card on Windows. **The art runner's processor mode had the same fault** since it was built;
  only ComfyUI's own `--cpu` had kept those runs off the card. Both now use `-1`, which hides it, and the worker refuses to start if a card
  is still visible (`tests/test_art_run_rehearsal.py`).

## Who chose what

- **Owner:** the AI route, processor first, the graphics card overnight if needed, and the go for everything it takes (12 Nov 2026).
- **Agent:** BiRefNet over RMBG-2.0 (licence); the hybrid rule (the AI decides which white is background, our cut-out keeps the outline);
  the thresholds (seed below 64, never into 192 or more, of 255), authored; staging and review before any change to the mods.

## The first run (12 to 13 Nov 2026)

- **The AI:** 66 originals on the processor beside the game (about 15 s each), then, with the game closed at the owner's word, the
  other 239 on the graphics card in 3.7 minutes (0.3 s each).
- **The review:** 64 textures changed; 3 were flagged (the gas alarm's ring, wrong; the sling set's triangle, right; the hyperbaric
  chamber's south view, whose export record no longer matches how it was made).
- **Applied (owner, 13 Nov 2026, as the agent recommended):** 61 textures in D'thorga Framework 0.105.1, Vehicles 0.49.1, Medical
  0.15.1 and the radio framework 0.1.8, leaving out the stock engine gauge and including the sling set. The gas alarm and the chamber's
  south view stay as they were.
