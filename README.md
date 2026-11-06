# Art references

Images the Phobos RimWorld mods do not use at all, kept only as references or for comparison (owner, 7 Nov 2026). The drawings under `drawings/` are ours, drawn with Pillow by scripts on `main` (`assets/phobos-medical/draw_placeholders.py`, `assets/water-tap/draw_tap.py --reference`), with no image generator, game art or other author's art; those scripts can draw them again. The rest are generated.

| File | What it is for |
| --- | --- |
| `drawings/OxygenConcentrator-large.png` | Our own Pillow drawing; the layout the generated concentrator was redrawn from |
| `drawings/HyperbaricChamber-large.png` | Our own Pillow drawing; the flat top-down layout for the hyperbaric chamber |
| `drawings/BottleTap-large.png` | Our own Pillow drawing; the layout for the bottle tap redraw |
| `medical/hyperbaric-chamber-102.png` | Picked 7 Nov for its look; drawn at an angle, so it guides a top-down redraw and is not shipped |
| `previews/preview-medical-equipment-202.png` | Picked 7 Nov from the previews page; the owner kept workshop-preview-204 for the mod, so this is unused |
| `medical/superseded-5nov/bedside-oxygen-both-11.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |
| `medical/superseded-5nov/bedside-oxygen-both-12.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |
| `medical/superseded-5nov/electrolyser-both-11.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |
| `medical/superseded-5nov/liquid-oxygen-store-both-11.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |
| `medical/superseded-5nov/liquid-oxygen-store-both-12.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |
| `medical/superseded-5nov/oxygen-concentrator-both-11.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |
| `medical/superseded-5nov/oxygen-mask-both-11.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |
| `medical/superseded-5nov/oxygen-mask-both-12.png` | A 5 Nov pick or alternate, superseded by the 7 Nov picks; kept to compare |

**AI-generated images are marked as such.** Each image's JSON sidecar names the generator, model and its SHA-256, the prompt,
the seed and the reference images. Models: FLUX.2 Klein 4B base (Black Forest Labs, Apache 2.0) and Z-Image base (Alibaba Tongyi lab,
Apache 2.0), run locally in ComfyUI. Where a sidecar names `phobos-private-vanilla.safetensors`, the image was made with a private style
model trained on upscaled vanilla RimWorld sprites; that model and its dataset are not published anywhere. Ludeon's EULA allows its
resources "as a basis or reference for a Mod" and does not mention AI training, so whether that covers such images is unsettled; the
owner chose to publish them (5 Nov 2026). Paths in a sidecar that start with `.local/references/` name the game's own sprites, given to
the generator as references: those sprites are not in this branch or anywhere in the repository.

Every generated image here passed the near-copy check against the game sprites it could have copied (perceptual hash and structural
similarity, flagged at a hash distance of 12 or less or structure of 0.6 or more). A slightly tilted copy can still pass.

Portions of the materials used to create this content are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights
reserved by Ludeon. This content is not official and is not endorsed by Ludeon.

Added with `scripts/art/archive_branch.py` on `main`, which only ever adds commits and refuses game art and model files.
