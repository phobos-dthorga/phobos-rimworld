# Artwork direction

19 Oct 2026 · owner decision with agent reasoning, for the owner's review

**Decision (owner, 19 Oct 2026): try local image generation before paying for anything.** The
placeholder sprites stay code-drawn until a generated candidate is approved.

## Why PixelLab is not the tool for RimWorld

Phobos Ostranauts uses PixelLab because Ostranauts is low-resolution pixel art. RimWorld is not: its
sprites are flat colour shapes with soft shading and thin dark outlines, drawn at 64 px per cell and
authored larger. A pixel-art sprite looks foreign in RimWorld, and upscaling pixel art with an AI
upscaler produces smears, not RimWorld's clean shading. The repository rule runs the other way: make
art at 2× or 4× and downscale deterministically.

## The three routes, cheapest first

| Route | Cost | Fits RimWorld's look | Notes |
| --- | --- | --- | --- |
| Code-drawn vector-style sprites (`assets/phobos-radio/make_textures.py`) | None | Yes, when done with care | Reproducible, no AI disclosure needed, exact geometry for linked atlases. The current ones are crude placeholders. |
| **Local generation: ComfyUI with Flux.1-schnell** on the owner's RTX 4070 Ti (12 GB) | None beyond disk and time | Good for item and building bases and for the Workshop preview | Set up 19 Oct 2026 under `C:\AI\` (not in the repository). `scripts/generate-art.py` drives it and writes a record beside every image. |
| ChatGPT Imagegen (a paid plan) | Subscription | Good; the Ostranauts workflow exists | Only if local generation disappoints, and then for one month at the public-beta gate. |

## Licences and disclosure

- **Flux.1-schnell** is Apache 2.0 (Comfy-Org fp8 packaging, SHA-256 recorded in each image's record).
  Generated images may ship in a free mod; the mod and the Workshop page say the art is AI-assisted.
- Every generated candidate carries a JSON record: prompt, seed, steps, size, model file and hash, server
  version and date. Approved masters are kept at generation size under `assets/phobos-radio/generated/`;
  rejected candidates are deleted and listed in the asset README with the reason.
- The export to game size is deterministic (a script, recorded), so an approved master can be re-exported.

## What generation is for, and what it is not

- **For:** the station, hand-held and antenna bases; the Workshop preview; later, QSL card art.
- **Not for:** the coax linked atlas (16 tiles in a fixed geometric layout, scripted), UI icons (scripted),
  anything that must line up cell-exactly.

## Findings from the first night (19 to 20 Oct 2026)

Set-up: ComfyUI 0.41.0 portable under `C:\AI\ComfyUI`, Flux.1-schnell fp8 (16 GB) and FLUX.2 Klein 4B
distilled with its Qwen3-4B text encoder, VAE and fal's sprite-sheet LoRA (15 GB), all checksum-verified.
Four images take about a minute on the RTX 4070 Ti.

| Need | What worked | What did not |
| --- | --- | --- |
| Style target | The owner chose a crowded Surveillance Shift-style desk (pale kit on dark steel, soft shading, thin outlines) as the look to match, from a screenshot used as reference only | Flux.1-schnell's first prompts gave front views with desk legs; "straight down" alone did not fix it |
| Top-down buildings | **Klein with a style reference** ("redraw image 1 as a floor plan, no legs"): a crowded top-down desk on the first try (`klein-station-top`) | Klein given a sketch *and* a style reference followed the style and dropped the sketch's layout; the seeds barely varied |
| Layout control | Klein with the sketch as the *only* reference kept the layout exactly (`klein-station-sketch-only`); schnell image-to-image from a sketch did too | The sketch-only route adopts the sketch's crude style; it needs a second pass for finish |
| Tall props (mast, whip) | **The sprite-sheet LoRA on Klein**: flat, outlined, consistent four-view sheets; the top-left tile is a ready mast sprite (`klein-mast-sheet-61`) | Text prompts for "flat 2D sprite" still produced 3D-looking lattice renders, with or without a style reference |
| Items | Flux.1-schnell text-to-image (`handheld-1`, approved) and the sprite-sheet LoRA both give clean item icons | |
| The Workshop preview | Flux.1-schnell painterly prompt; the owner approved `workshop-preview-3` | |

Recipe for the rest of Phase 1, pending the owner's picks: buildings through Klein with the approved
station as the style reference; props and items through the sprite-sheet LoRA; linked atlases and UI
icons stay scripted. Every candidate carries its record; game-scale previews are in
`assets/phobos-radio/generated/exports-preview/`, and `review.html` beside them shows everything.

## Open

- [ ] The owner judges the candidates (`assets/phobos-radio/generated/review.html`) and names the station,
      mast and whip to export; rejected batches are then deleted and listed with reasons.
- [ ] `AGENTS.md`'s Artwork section still carries the Ostranauts wording about PixelLab; rewrite it with the
      recipe above once the owner has approved a building and a prop.
- [ ] Whether to fetch three vanilla sprites from the wiki for study (asked 19 Oct, not yet answered).
