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

## Open

- [ ] The owner judges the first candidates (`assets/phobos-radio/requests/phase1-sprites.json`).
- [ ] `AGENTS.md`'s Artwork section still carries the Ostranauts wording about PixelLab; rewrite it once the
      first approved sprite shows which route works.
