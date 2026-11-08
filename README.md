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
| `water-tap/bottle-tap-redraw-12.png` | Bottle tap texture, Water Tap (Unreleased) |
| `radio/handheld-2.png` | Hand-held radio texture, Radio Framework (Unreleased) |
| `radio/klein-mast-flat-63.png` | Mast texture, Radio Framework (Unreleased) |
| `radio/mast-remake-12.png` | Whip antenna texture, a stand-in until a whip is picked, Radio Framework (Unreleased) |
| `radio/signal-icon-12.png` | Signal button icon, Radio Framework (Unreleased) |
| `radio/coax-icon-12.png` | Feedline build icon, Radio Framework (Unreleased) |
| `previews/workshop-preview-3.png` | Background of the Radio Framework Workshop preview (Unreleased) |
| `previews/preview-burns-and-smoke-202.png` | Background of the Burns and Smoke Workshop preview, a stand-in (Unreleased) |
| `previews/preview-emergency-dosing-202.png` | Background of the Emergency Dosing Workshop preview (Unreleased) |
| `previews/preview-factory-additions-202.png` | Background of the Factory Additions Workshop preview (Unreleased) |
| `previews/preview-framework-202.png` | Background of the Framework Workshop preview (Unreleased) |
| `previews/preview-triage-202.png` | Background of the Triage Workshop preview (Unreleased) |
| `previews/preview-vehicle-patches-202.png` | Background of the Vehicle Patches Workshop preview (Unreleased) |
| `previews/preview-water-tap-201.png` | Background of the Water Tap Workshop preview (Unreleased) |

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

## Vehicles

- `vehicles/tanker/`: the bulk tanker's north base body, **attempt 2 of the pilot, the owner's pick (8 Nov 2026)**, exactly as OpenAI gpt-image-2 returned it (768 x 1536, medium quality, US$0.0295), with its JSON record. The prompt is in `assets/phobos-vehicles/requests/tanker-pilot.json` on the working branch.
