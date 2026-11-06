# Art masters

Full-size masters (1024 px, the preview 1280 x 720) of the art the Phobos RimWorld mods ship, kept apart from `main` (owner, 7 Nov 2026). The mods ship scaled-down exports of these; the record beside each texture on `main` gives its export settings and hashes.

| File | What it is for |
| --- | --- |
| `medical/oxygen-concentrator-from-stand-in-103.png` | Oxygen concentrator texture, Medical Equipment 0.2.3 |
| `medical/bedside-oxygen-103.png` | Bedside oxygen stand texture, Medical Equipment 0.2.3 |
| `medical/burn-unit-bed-both-11.png` | Burn unit bed textures, Medical Equipment 0.2.0 |
| `medical/liquid-oxygen-store-103.png` | Liquid oxygen store texture, Medical Equipment 0.3.0 |
| `medical/oxygen-mask-104.png` | Field oxygen kit texture, Medical Equipment 0.3.0 |
| `medical/burn-dressing-101.png` | Burn dressing texture, Medical Equipment 0.3.0 |
| `medical/manual-resuscitator-103.png` | Manual resuscitator texture, Medical Equipment 0.3.0 |
| `triage/triage-authority-icon-14.png` | The care-choice button icon, Triage 0.1.2 |
| `previews/workshop-preview-204.png` | Background of the Medical Equipment Workshop preview, 0.2.3 |

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
