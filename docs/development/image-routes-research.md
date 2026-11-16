# Image routes: research (19 Oct to 8 Nov 2026)

**Status (17 Nov 2026):** research, kept as a dated record; the routes in use today are in the [art pipeline](art-pipeline.md). **The Gemini recommendation below is superseded:** on 8 Nov 2026 the owner chose the OpenAI image API for vehicle bodies, run under the [paid generation rules](paid-image-generation-rules.md).

How we chose where our pictures come from, in three records merged on 17 Nov 2026, oldest first: the first routes and why not PixelLab (19 Oct 2026), models that lock or move the camera and their trial (7 to 8 Nov 2026), and paid image services compared with costs (8 Nov 2026). The OpenAI engine's operating manual, once part of the third, is now in the paid generation rules.

## Artwork direction

*Merged into this record on 17 Nov 2026 from `artwork-direction.md`; kept as written.*

19 Oct 2026 · owner decision with agent reasoning, for the owner's review

**Decision (owner, 19 Oct 2026): try local image generation before paying for anything.** The
placeholder sprites stay code-drawn until a generated candidate is approved.

### Why PixelLab is not the tool for RimWorld

Phobos Ostranauts uses PixelLab because Ostranauts is low-resolution pixel art. RimWorld is not: its
sprites are flat colour shapes with soft shading and thin dark outlines, drawn at 64 px per cell and
authored larger. A pixel-art sprite looks foreign in RimWorld, and upscaling pixel art with an AI
upscaler produces smears, not RimWorld's clean shading. The repository rule runs the other way: make
art at 2× or 4× and downscale deterministically.

### The three routes, cheapest first

| Route | Cost | Fits RimWorld's look | Notes |
| --- | --- | --- | --- |
| Code-drawn vector-style sprites (`assets/phobos-radio/make_textures.py`) | None | Yes, when done with care | Reproducible, no AI disclosure needed, exact geometry for linked atlases. The current ones are crude placeholders. **Since 12 Nov 2026 only the two coax atlases are:** the owner picked generated art for the rest (7 and 12 Nov). |
| **Local generation: ComfyUI with Flux.1-schnell** on the owner's RTX 4070 Ti (12 GB) | None beyond disk and time | Good for item and building bases and for the Workshop preview | Set up 19 Oct 2026 under `C:\AI\` (not in the repository). `scripts/generate-art.py` drives it and writes a record beside every image. **Since 5 Nov 2026** Flux.1-schnell makes only the Workshop previews; sprites and icons come from FLUX.2 Klein 4B base with references and the private style model ([art automation](art-pipeline.md#models)), and vehicle bodies from the OpenAI image API. |
| ChatGPT Imagegen (a paid plan) | Subscription | Good; the Ostranauts workflow exists | Only if local generation disappoints, and then for one month at the public-beta gate. **Revised 8 Nov 2026:** the paid route is a pay-per-image API, not a subscription; see [art services for vehicle sprites](#art-services-for-vehicle-sprites-research-8-nov-2026). |

### Licences and disclosure

- **Flux.1-schnell** is Apache 2.0 (Comfy-Org fp8 packaging, SHA-256 recorded in each image's record).
  Generated images may ship in a free mod; the mod and the Workshop page say the art is AI-assisted.
- Every generated candidate carries a JSON record: prompt, seed, steps, size, model file and hash, server
  version and date. Approved masters are kept at generation size under `assets/phobos-radio/generated/`;
  rejected candidates are deleted and listed in the asset README with the reason.
- The export to game size is deterministic (a script, recorded), so an approved master can be re-exported.

### What generation is for, and what it is not

- **For:** the station, hand-held and antenna bases; the Workshop preview; later, QSL card art.
- **Not for:** the coax linked atlas (16 tiles in a fixed geometric layout, scripted), UI icons (scripted),
  anything that must line up cell-exactly.

### Findings from the first night (19 to 20 Oct 2026)

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

### Open

- [x] The owner judges the candidates (`assets/phobos-radio/generated/review.html`) and names the station,
      mast and whip to export; rejected batches are then deleted and listed with reasons. **Done:** the mast and a stand-in whip were
      picked on 7 Nov 2026, the base station on 12 Nov (Radio 0.1.7); a real whip is held until the radio resumes.
- [x] `AGENTS.md`'s Artwork section no longer names PixelLab (checked 6 Nov 2026).
- [ ] Two copies of the review scripts exist: the shared ones in `scripts/art/` are current (5 Nov 2026); the older
      `contact_sheet.py`, `export_sprite.py` and `review_page.py` in `assets/phobos-radio/` stay until the radio art is redone with
      the shared ones. The radio asset record is [its README](../../assets/phobos-radio/README.md).
- [x] Whether to study vanilla sprites: decided on 5 Nov 2026 in the
      [art automation design](art-pipeline.md#vanilla-sprites). Newer engines, the unattended runner and
      the shared review tools (now in `scripts/art/`) are recorded there too.

## View-angle models: research record (7 Nov 2026)

*Merged into this record on 17 Nov 2026 from `view-angle-models.md`; kept as written.*


**Status (17 Nov 2026):** route 2 (depth control) was downloaded and tried in night 3 (8 Nov 2026), and on 12 Nov the owner
picked the depth route's chamber and the plain redraw's electrolyser (both ship in Medical 0.14.1). Routes 1, 3 and 4 are
not tried: style model v3, which route 1 needs, never finished training.

The owner asked (7 Nov 2026) whether there is a model, or models, we could download that would suit RimWorld's own art style better,
because a lot of the local model's assets come out at odd angles. **Written as a research record only**: when it was written, nothing
here had been downloaded, installed, trained or generated, and no candidate had been tried. Every claim about a model comes from its own model card or
repository, read on 7 November 2026; everything marked *agent inference* is ours. **Update, 8 Nov 2026:** route 2 has had its
first trial; see "First trial: night 3" near the end.

### Read this first (gotchas)

- **The odd angles are a camera problem, not a style problem.** No published model draws "RimWorld top-down". The models below
  either lock the camera to a drawing we supply, or move the camera of an image we already have. The style still comes from the
  references and our private style model, as now.
- **The first trial costs no new base model.** A depth-control LoRA exists for the exact FLUX.2 Klein 4B Base and text encoder already
  installed, under Apache 2.0. The depth map it wants can be drawn by script from the stand-in drawings we already make.
- **One strong-looking route is ruled out by licence.** Hunyuan3D 2.1's licence bars using or displaying its outputs outside its
  territory, which excludes the EU, the UK and South Korea; a Workshop mod is displayed everywhere, so nothing made with it can ship.
- **RimWorld's angle is not plain "top-down"** (agent observation from the game's own sprites, extracted privately in
  [the art automation design](art-pipeline.md#vanilla-sprites)): buildings and furniture are seen from
  directly above with the south face hinted, beds show the pillow, and pawns are drawn front-on. Prompts, stand-ins and training
  captions should say "seen from directly above, south face hinted" rather than "top-down", which the models read as a photograph
  from a high angle.
- **Every trial needs the owner's go and a stop time** (owner rule, [art backlog](art-backlog.md)); nothing runs while the game is open.
- **Civitai is unreachable from here** (its pages answered 403 and its API 451 on 7 Nov 2026), so the two "RimWorld art style" LoRAs
  there are listed from search results only, with their licences unknown.

### What we have, and what Night 2 showed

The machine: an RTX 4070 Ti with 12 GB of video memory, 64 GB of system memory, ComfyUI 0.41.0 with no custom nodes installed,
and about 100 GB free on C: (checked 7 Nov 2026). Installed models: FLUX.2 Klein 4B distilled and Base (fp8), its Qwen3-4B text
encoder, Z-Image, fal's sprite-sheet LoRA, Real-ESRGAN, and our private style model v1 and v2 (the design is in
[the art automation design](art-pipeline.md#models)).

Night 2 (6 to 7 Nov 2026) already showed the cure in miniature: given a flat stand-in drawing as the reference, Klein redrew the
hyperbaric chamber from directly above, and the concentrator, the bottle tap and the radio pieces came out on-style
([the backlog](art-backlog.md)). Two things are still missing:

1. A way to lock the angle **and** keep the look of a picked image at the same time. Klein given a sketch and a style reference
   follows the style and drops the sketch's layout (first night's finding, [artwork direction](#artwork-direction)).
2. A way to **re-angle a pick we already like**, such as hyperbaric-chamber-102, which is drawn at an angle and cannot be fixed by
   rotating it (tried 7 Nov).

### The candidates

| # | Route | Model and author | Licence | Base model and memory | What it fixes | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Caption the view into style model v3 | Our own training, no download | n/a | The installed Klein 4B Base | Makes the view a trigger word, so plain prompts stop drifting | **Do with v3**; costs nothing; untested |
| 2 | Reference plus depth control | RefControl FLUX.2 Klein 4B Reference Depth LoRA, thedeoxen ([model card, primary](https://huggingface.co/thedeoxen/refcontrol-FLUX.2-klein-4B-reference-depth-lora)) | Apache 2.0 | The installed `flux-2-klein-base-4b-fp8` and `qwen_3_4b`; one LoRA file | Locks the layout and angle to a drawing while taking the look from a reference pick | **First trial** |
| 3 | Move the camera of an existing image | Qwen-Edit-2509 Multiple-angles, dx8152 ([model card, primary](https://huggingface.co/dx8152/Qwen-Edit-2509-Multiple-angles)); pre-merged GGUF build by ChrisColeTech ([model card, primary](https://huggingface.co/ChrisColeTech/qwen-image-edit-multiple-angles-GGUF)) | Apache 2.0 (LoRA, the GGUF build and Qwen-Image-Edit-2509 itself) | Qwen-Image-Edit-2509, a 20B model: on 12 GB only as a 4-bit GGUF with offloading to system memory, through the ComfyUI-GGUF custom node; slow | Re-angles a pick we already like ("Turn the camera to a top-down view") | **Second trial**, if the depth route disappoints |
| 4 | Draw in 3D, render from above | TripoSG, VAST AI ([repository, primary](https://github.com/VAST-AI-Research/TripoSG)), then Blender | MIT (TripoSG); Blender is GPL, which covers the tool, not its renders | About 8 GB; shape only, no texture | True orthographic views, and south, east and north from one mesh, as depth maps for route 2 | **Later experiment**: the biggest set-up |

#### Route 1: caption the view into style model v3

v3 is already planned ([backlog](art-backlog.md#style-model-v1-kept-v3-next-owner-7-nov-2026)): a larger private set of the game's
buildings, furniture, items and button icons with a sharper upscale. The addition is cheap: **every training caption carries one fixed
phrase** for the view ("seen from directly above, south face hinted"), and every request that uses the style model carries the same
phrase. The model then learns the view as a word it can be asked for, instead of a tendency it may or may not pick up. v1 and v2 were
captioned without it, which may be part of why v2 drifted into the angled view without references (agent inference; untested).

#### Route 2: RefControl depth LoRA for Klein 4B Base (first trial)

From its model card (thedeoxen, updated 28 May 2026, Apache 2.0): a LoRA for FLUX.2 Klein 4B Base that fuses a reference image on
the left with a depth map on the right, keeping identity and style from the reference and pose and structure from the depth map.
Trigger word `refcontrol`. The Base model runs about 50 steps at guidance about 4. Its ComfyUI workflow loads exactly the files we
have (`flux-2-klein-base-4b-fp8.safetensors`, `qwen_3_4b.safetensors`, the FLUX.2 VAE) plus the LoRA, and makes its depth map with a
Depth Anything node from the `comfyui_controlnet_aux` pack.

What it would do for us (agent inference): the reference is the owner's pick, the depth map is drawn from the stand-in. A depth map
of a building seen from directly above is simple: the footprint is one level, raised parts are lighter, the floor is black. That is a
few more lines in `assets/phobos-medical/draw_placeholders.py`, which already draws each stand-in's layout, so the depth-estimator
custom node is not needed and nothing new goes into ComfyUI but one LoRA file. The chamber (pick 102, angled) and the electrolyser
(no pick) are the natural test pieces: the chamber's 1 by 3 layout drawing exists, and the electrolyser has a placeholder.

Risks: the LoRA was trained on photographs and characters (its examples), not flat game art, so it may fight the style reference;
the Base model at 50 steps is slow, which the owner has said is fine ([slow generation](art-backlog.md)); and a drawn depth map is
cruder than an estimated one, which may be a help (flat, unambiguous) or a hindrance. All untested.

#### Route 3: camera-angle LoRAs on Qwen-Image-Edit (second trial)

Two published LoRAs move the camera of an existing image, and only one of them reaches the view we need:

- **dx8152's Multiple-angles for Qwen-Image-Edit-2509** (Apache 2.0, model card updated 21 Apr 2026): no trigger word; plain
  instructions such as "Turn the camera to a top-down view", "Rotate the camera 45 degrees to the left" and "Move the camera forward".
  It wants the Qwen-Image Lightning LoRA alongside it (model card). ChrisColeTech's build (Apache 2.0, Aug 2026) merges the Lightning
  and Multiple-angles LoRAs into quantised GGUF weights (Q4_K_M, Q6_K, Q8_0) with a quantised Qwen2.5-VL-7B text encoder, so a 12 GB card
  with 64 GB of system memory can run it through the ComfyUI-GGUF custom node, with offloading and slowly. Sizes and speed on our
  card are unverified.
- **fal's Multiple-Angles for Qwen-Image-Edit-2511** (Apache 2.0, Jan 2026, 96 poses from Gaussian-splat renders): its four elevations
  are -30, 0, 30 and 60 degrees ("high-angle shot"), so **it never looks straight down**. Not for us.

What it would do for us: turn an angled pick into a straight-down one while keeping its design, which is exactly the chamber-102
problem. Costs: a second 20B base model on disk (roughly 12 GB for the 4-bit weights and 5 GB for the encoder, from the file list;
exact sizes unverified), a custom node, and a slower run; and "top-down" to a model trained on photographs means a high camera, so it
may still need a second pass through route 2 to flatten it (agent inference).

#### Route 4: draw in 3D, render from above (later)

A mesh rendered by an orthographic camera pointing straight down is the one way to get a view that is exactly from above, and the
same mesh gives south, east and north views that agree with each other, which no 2D route guarantees. The render would be a depth
map or a flat-shaded picture for route 2 to finish in the house style.

- **TripoSG** (VAST AI, MIT, March 2025): image to mesh, about 8 GB of video memory (its README). Shape only, no texture, which is all
  a depth map needs.
- **TRELLIS.2** (Microsoft, MIT, Dec 2025): textured meshes with PBR materials; its README and model card say an NVIDIA GPU with at least
  24 GB is necessary (verified on A100 and H100). Community guides claim 6.5 to 7.8 GB in a low-memory mode at lower resolution;
  unverified, and not from the authors.
- **Hunyuan3D 2.1** (Tencent): 10 GB for shape and 21 GB for texture (its README), and **ruled out by licence** whatever the memory:
  the Tencent Hunyuan 3D 2.1 Community License applies only in its territory, which excludes the European Union, the United Kingdom
  and South Korea, and its acceptable-use terms forbid using, distributing or displaying its outputs outside that territory. Art in
  a public Workshop mod is displayed in all three.

The cost is a new Python environment for the 3D model, Blender for the render, and a script to drive both; nothing of it exists yet.
Worth it only if route 2 works and the three-view consistency of buildings becomes the next problem (agent inference).

### Noted and set aside

| Model | Why not |
| --- | --- |
| "RimWorld Art Style" ([Civitai 411781](https://civitai.com/models/411781/rimworld-art-style)) and "RimWorld v1.0" ([Civitai 412891](https://civitai.com/models/412891/rimworld-v10), described as the storytellers' portrait style) LoRAs, SDXL, from search results only; also "Archotech style" ([Civitai 472077](https://civitai.com/models/472077/archotech-style-rimworld), SD 1.5) and a textual inversion ([sd-concepts-library](https://huggingface.co/sd-concepts-library/rimworld-art-style), SD 1.5, MIT) | Civitai is unreachable from here (403 and 451 on 7 Nov 2026), so their licences, datasets and samples are unseen. SDXL is an older base than Klein with a weaker editor; a style trained on the game's art carries the same EULA question as our own private model, with a dataset we cannot inspect. Nothing suggests they fix the angle |
| Kontext-Top-Down-View, prithivMLmods ([model card](https://huggingface.co/prithivMLmods/Kontext-Top-Down-View)) | FLUX.1 Kontext dev under its non-commercial licence; trained on 400 photo-scene pairs for high-angle photographs, not objects on a plain ground |
| Flux-2-Multi-Angles-LoRA-v2, lovis93 | For FLUX.2-dev (32B, non-commercial licence), which does not fit the card; its nearest elevation is "overhead" |
| FLUX.2 Klein 9B and its RefControl LoRAs (thedeoxen has depth, canny, lineart, normal and pose for 9B) | Klein 9B is gated (a click-through: Hugging Face shows the licence and lets the download through once you agree; checked 7 Nov 2026) under the FLUX Non-Commercial License: the owner's decision, as with FLUX 3 Image Klein, and outside the Apache pipeline. The 4B depth LoRA covers the first trial |
| "Top-down pixel art" and other pixel-art LoRAs | Pixel art is the wrong look for RimWorld ([artwork direction](#artwork-direction)) |
| fal's sprite-sheet LoRA (installed) | Already in use; its sheets include a top-down tile, but it draws flat outlined props rather than the game's soft-shaded buildings |

### What a trial needs (not built in this change)

For route 2, in one change on the owner's go:

1. One entry in `scripts/art/models.json` for `flux2_klein_4b_refcontrol_depth.safetensors` (folder `loras`, Apache 2.0), fetched by
   `scripts/art/fetch_models.py`, which checks the Hub's licence and hash.
2. A `klein-depth` engine in `scripts/generate-art.py` beside `klein_workflow`: Base model, reference and depth map stitched side by side
   as the LoRA expects, `refcontrol` in the prompt, about 50 steps and guidance 4, and the LoRA strength as a request setting.
3. A depth-map drawer in `assets/phobos-medical/draw_placeholders.py` for the chamber and the electrolyser, from their existing layouts.
4. A request file with the two test pieces at seeds 11 and 12, with and without the style model, so the review page shows the four
   routes side by side; and a stop time.

Route 3 adds the ComfyUI-GGUF custom node, two GGUF files and a `qwen-angles` engine; route 4 adds a 3D environment and Blender. Neither
is planned until route 2 has been judged.

### First trial: night 3 (8 Nov 2026)

Route 2 ran in night 3, on the owner's go (8 Nov 2026), beside a plain redraw of the same two pieces from a top-down stand-in.
As prepared, no new engine was needed: the depth map went in as the first reference image, the design as the second, with
`refcontrol` in the prompt. **The owner had not judged these images when this was written; everything below is agent observation.**
**Judged 12 Nov 2026:** the owner picked the depth route's chamber (seed 11) and the plain redraw's electrolyser (seed 12); both ship
in Medical 0.14.1 ([the night's record](../../assets/phobos-vehicles/requests/night-2026-11-12-record.md)).

| Test piece | Plain top-down redraw (stand-in as reference) | Depth route (depth map, then the pick) |
| --- | --- | --- |
| Electrolyser | Straight down, thick near-black outline, control box kept | Straight down, thick outline, control box lost its detail |
| Hyperbaric chamber | Straight down, thick outline, rounded capsule | Straight down, keeps pick 102's look closely, thinner outline |

- **Both routes cured the angle on both pieces** in the images looked at (seed 11 of each; the review page has all four seeds).
- **The depth route keeps the pick's look; the plain redraw keeps the vanilla outline better.** Which matters more is the owner's call.
- **Near-copy check:** the depth chamber images sit very close to pick 102 by design. Before 9d8f6af the check counted our own
  picks as game art and flagged them; after the fix, nothing in either route is flagged.
- **Route 3 and route 4 stay unplanned** unless the owner finds both of these wanting.

### Sources

- thedeoxen, RefControl FLUX.2 Klein 4B Reference Depth LoRA, model card and workflow (Hugging Face, read 7 Nov 2026).
- dx8152, Qwen-Edit-2509 Multiple-angles, model card (Hugging Face, read 7 Nov 2026).
- ChrisColeTech, Qwen-Image-Edit 2509 GGUF with Turbo and Multiple-Angles merged, model card (Hugging Face, read 7 Nov 2026).
- fal, Qwen-Image-Edit-2511 Multiple-Angles LoRA, model card (Hugging Face, read 7 Nov 2026).
- Black Forest Labs, FLUX.2 Klein 4B (Apache 2.0) and Klein 9B (FLUX Non-Commercial License, gated), model pages (Hugging Face, read 7 Nov 2026).
- Microsoft, TRELLIS.2 README and TRELLIS.2-4B model card (GitHub and Hugging Face, read 7 Nov 2026).
- Tencent, Hunyuan3D 2.1 README and LICENSE (GitHub and Hugging Face, read 7 Nov 2026).
- VAST AI Research, TripoSG README (GitHub, read 7 Nov 2026).
- prithivMLmods, Kontext-Top-Down-View, model card (Hugging Face, read 7 Nov 2026).
- Community memory figures for TRELLIS.2 and Klein 9B on 12 GB cards come from third-party guides and forum posts and are marked unverified above.

## Art services for vehicle sprites: research (8 Nov 2026)

*Merged into this record on 17 Nov 2026 from `vehicle-art-services.md`; kept as written.*


A research record behind the art plan in [utility vehicles](utility-vehicles-design.md). Prices and terms were read on
**8 Nov 2026** and move; re-read before paying. Where a service's own page refused the fetch (403), the row relies on
a dated third-party summary and says so ("3P"). AUD figures use about US$0.70 per A$1 and exclude the 10% GST that
overseas digital services add. The owner is on a pension and would rather pay per image than renew a monthly plan; the
earlier decision to try local generation first ([artwork direction](#artwork-direction), 19 Oct 2026) still stands
for items and icons, and this record replaces that record's paid-route row.

### Read this first

- **The owner's pick (8 Nov 2026): the OpenAI image API for vehicle bodies**, which they already know and their own sources rate
  best for RimWorld's look; **icons stay on the owner's local generation**, which already drew on-style icons for free. The owner
  is setting up an API account (ID verification, prepaid credit with automatic top-up off). The comparison below is kept as read.
- **Rotate, then edit.** A true top-down east facing is the north facing turned a quarter, and south is north turned a
  half. A script does the turns for free; the model then only fixes lighting and the parts that differ end to end.
  Facings that start from the same pixels match far better than three separate generations.
- **Not chosen (the owner picked OpenAI, above).** As read: **best value, the Gemini image API** (Nano Banana 2.1, about US$0.034 an image at 1K, half in batch). **Good
  fallback: the OpenAI image API** (gpt-image-2, US$0.041 at medium quality, prepaid, no ChatGPT subscription needed).
  **Backup for reference edits: fal.ai** (FLUX.2 pro edit, about US$0.045).
- **PixelLab does not fit the bodies.** Its rotation tools stop at 128 to 256 px and are trained on characters; vehicle
  bodies are 384 by 640 px. It owns none of its outputs on any tier, so 64 px icons would be fine if the owner wants it.
- **Free tiers with ownership traps:** Leonardo (the service owns free-plan outputs), Recraft (free images are public and
  Recraft's), Scenario (personal and evaluation use only), Midjourney (paid users own outputs; free or trial images are
  non-commercial), Meshy and Tripo (free outputs are CC BY, so the mod would credit them). A free mod is still public
  redistribution: avoid these tiers for anything that ships.
- **No service here blocks Australia.** The Gemini API lists Australia; fal blocks only embargoed countries; Google's
  paid-only rule covers the EEA, Switzerland and the UK.
- **Four vehicles cost a few dollars**, not a subscription (table at the end).

### What the sprites need

Per Vehicle Framework vehicle: a north, an east and a south body (west is mirrored by the framework) at 128 px per
drawSize unit, so a 2 by 4 cell truck drawn at (3, 5) is 384 by 640 px north and 640 by 384 px east; a colour mask for
each (flat colour zones for the three-colour pattern shader); overlays for a raised mast or a tipped body and for
damage; and a 64 px icon. The look is the game's: thick near-black outline, soft cel shading, pale panels, rounded
corners, straight top-down, no perspective. Outputs ship in a free public Workshop mod, so the terms must allow it.

### The comparison

| Service | Best use for us | Facing consistency | Max output | Price model and price (USD) | Free tier | API | Output terms | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| **PixelLab** | 64 px pixel-art icons at most | Rotate (one image in, eight directions out); "8-direction (Pro)" from one reference | Rotate 128 px; 8-rotations v3 256 px; Pro image 512 px a side ([API](https://www.pixellab.ai/pixellab-api), [docs](https://www.pixellab.ai/docs/tools/create-8-rotations-pro)) | Plans about $12, $24, $50 a month for 2k, 5k, 10k generations (3P, [respawnstack](https://respawnstack.com/tools/pixellab)); API per call: Rotate $0.011, Pro image $0.15 | 40 fast generations, then 5 a day (3P) | Yes | You own outputs on any tier; commercial use allowed; no training on them ([terms, 30 Oct 2026](https://www.pixellab.ai/termsofservice)) | Poor fit: built for pixel art, rotation capped far below 640 px |
| **OpenAI image API** (gpt-image-2; 2.5 and 2.6 also listed) | North body; reference-guided edits | Edits with several reference images | Long edge to 3840 px ([guide](https://developers.openai.com/api/docs/guides/image-generation)) | Prepaid API, no ChatGPT Plus: 1024 by 1536 at **medium $0.041, high $0.165**, plus reference input at $8 per million tokens; gpt-image-1-mini $0.015 / $0.052 but deprecated; gpt-image-1 shut down 23 Oct 2026 ([pricing](https://developers.openai.com/api/docs/pricing)) | None on the API | Yes | You own outputs, which may not be unique; no building competing models (3P summary of the 1 Jan 2026 terms, [ConductAtlas](https://conductatlas.com/platform/openai/terms-of-use-row/intellectual-property-ownership-of-outputs/); the openai.com page returned 403) | Good: follows instructions well; medium quality is cheap |
| **Google Gemini image API** (Nano Banana family) | North body, edits, masks | Several reference images in one prompt | 0.5K to 4K | Per image ([pricing](https://aistudio.google.com/docs/pricing)): **Nano Banana 2.1 $0.0336 at 1K, $0.050 at 2K, half in batch**; 3.1 Flash $0.067; 3 Pro $0.134; 2.5 Flash $0.039 (shuts down 15 Mar 2027) | No image models on the free API tier; the Gemini app is free with Nano Banana 2 at standard limits, watermarked, and Google trains on unpaid use ([help](https://support.google.com/gemini/answer/16275805)) | Yes | Google claims no ownership; unpaid use feeds its products; 18+ ([terms, 23 Mar 2026](https://aistudio.google.com/docs/terms)); Australia listed ([regions](https://aistudio.google.com/docs/available-regions)) | **Best value** |
| **Midjourney** | Concept art only | Omni-reference (V7, double cost), "Edit" in V8.2, style reference (3P) | 1024 px, 2048 px HD (3P) | Subscription only, $10 to $120 a month, no free trial (3P, [costbench](https://costbench.com/software/ai-image-generators/midjourney/)) | None | No | Paid users own outputs, kept after cancelling; companies over $1M revenue need Pro (official page 403; 3P [terms.law](https://terms.law/ai-output-rights/midjourney/)) | Skip: subscription, no API, weak at exact top-down |
| **Leonardo.ai** | General | Image guidance, character reference | about 1 to 2K | $12 to $60 a month (3P) | 150 tokens a day, all public | Separate | **Leonardo owns free-plan outputs**; paid users own theirs (3P, [terms.law](https://terms.law/ai-output-rights/leonardo/); official page 403) | Avoid the free tier |
| **Scenario** | Training a custom style | Custom models, editing, 3D | n/a | $15 to $75 a month ([pricing](https://www.scenario.com/pricing)) | 50 credits a day, **personal and evaluation only** | Yes | Paid plans carry a commercial licence | Good tool; the free tier rules out a published mod |
| **Layer.ai** | Studio style training | Style training | n/a | From $10 a month for 300 units ([pricing](https://www.layer.ai/pricing)) | Not stated | Yes | Commercial licence; ownership not stated | Aimed at studios |
| **Recraft** | Style-locked flat art, cheap edits | Custom style from references ($0.005), image to image | 2K on Pro | API: V4.1 $0.035, Flash $0.007, image to image $0.04, prepaid with no expiry ([API pricing](https://www.recraft.ai/docs/api-reference/pricing)); web plans about $12 to $20 a month (3P) | Daily credits, but **free images are public and Recraft's** ([pricing](https://www.recraft.ai/pricing)) | Yes | Paid and API: you own the images | Worth a test through the API |
| **Krea** | Many models in one place | Runs Nano Banana, FLUX Kontext, GPT Image and others | By model | Basic $9 a month for 5,000 units, about 200 Nano Banana 2.1 images ([pricing](https://www.krea.ai/pricing)) | 100 units a day; its own FAQ contradicts its free commercial licence | Yes | As left | One $9 month would cover the job |
| **Ideogram** | Text in images | Character reference | about 2K | API v4.5 $0.03 to $0.10 (3P, [eesel](https://www.eesel.ai/blog/ideogram-pricing)) | 30 credits a week, public | Yes | Free use reportedly commercial (3P) | Not needed |
| **Adobe Firefly** | Commercially safe images | Illustrator "Turntable" rotates 2D vector objects (3P) | 2K | $9.99 a month for 2,000 credits (3P) | Limited daily generations (3P; the Adobe FAQ returned 403) | Enterprise only | Commercial use allowed | Subscription; skip |
| **fal.ai** | Cheap reference edits | FLUX.2 pro edit with up to nine references; Qwen-Image-Edit-2511 Multiple Angles (camera azimuth and elevation); FLUX Kontext | 4K and above | Prepaid: FLUX.2 pro edit $0.03 for the first megapixel plus $0.015 per extra input or output megapixel, about $0.045 with one reference ([page](https://fal.ai/models/fal-ai/flux-2-pro/edit)); Kontext pro $0.04 (3P); Multiple Angles $0.035 per megapixel ([page](https://fal.ai/models/fal-ai/qwen-image-edit-2511-multiple-angles)); Nano Banana Pro $0.15 ([page](https://fal.ai/models/fal-ai/nano-banana-pro/edit)) | Occasional promo credits | Yes | No express assignment of outputs; each model's licence applies; models tagged for commercial use ([terms, 8 Sep 2026](https://fal.ai/terms)); only embargoed countries blocked | Good backup |
| **Replicate** | The same models | The same | The same | Pay per use: flux-1.1-pro $0.04, recraft-v3 $0.04 ([pricing](https://replicate.com/pricing)) | None stated | Yes | Assigns you all rights it has in outputs, commercial use allowed, subject to each model's licence ([terms, 1 Apr 2026](https://replicate.com/terms)) | As fal |
| **Meshy** (3D) | A model to re-render straight down | True 3D | Mesh | Pro A$32 a month, A$16 the first month ([pricing](https://www.meshy.ai/pricing)) | 100 credits a month, CC BY 4.0 | Pro and above | Paid: private, yours | Overkill; renders still need restyling |
| **Tripo** (3D) | As Meshy | As Meshy | Mesh | Pro about $20 a month (3P) | 200 credits a month, public, CC BY 4.0 by Tripo's own blog ([blog](https://www.tripo3d.ai/blog/commercial-use-ai-3d-models)) | Yes | Paid: private, commercial | As Meshy |

### The pipeline this points to (agent recommendation)

**Superseded (8 Nov 2026): the owner chose the OpenAI image API for vehicle bodies, not Gemini.** The pipeline in use is in
[Running the OpenAI engine](paid-image-generation-rules.md#running-the-openai-engine) and the [paid generation rules](paid-image-generation-rules.md). The
recommendation is kept as written.

1. **North body:** Nano Banana 2.1 through the Gemini API with a short style prompt and one to three style references
   (our own approved sprites, never the game's art in anything that ships); generate at 1K or 2K, downscale by script
   with area resampling and alpha clean-up to the exact size.
2. **East and south:** turn the finished north by script, then send the turned image back as a reference edit ("same
   vehicle, light from the top, keep the outline and colours"); where the ends differ, edit from the north plus the
   turned draft as two references.
3. **Overlays:** reference edits of the body, cut out by script against the base. **A part that moves (a mast, a boom, a dish, a rotary head, a tipper
   body) is its own picture at rest on the body's canvas, with the body drawn without it; never a frame sheet** ([the animation research](vehicle-animation-research.md)).
4. **Masks:** by script, posterising the panel zones into flat colour regions, or hand-painted over a faded copy; a
   model pass at the same price where the script cannot separate them. **Superseded (16 Nov 2026):** paint masks are made by region
   and repaired with Segment Anything's parts map ([vehicle paint masks](vehicle-paint-masks.md#paint-masks-by-region-16-nov-2026)).
5. **Icon:** downscale the north or east body and sharpen the outline, by script.
6. **Fallbacks:** gpt-image-2 medium if Gemini's style drifts; FLUX.2 pro edit on fal for the turned-image edits.
7. **Records:** every image through the near-copy check (`scripts/art/near_copy.py`) with a record naming the tool,
   model, prompt, date, SHA-256 and AI disclosure, plus the service's output terms as read that day; masters on the
   `art-masters` branch through `scripts/art/archive_branch.py`.
8. **Who does what:** a new engine in `scripts/art-run.py` calls the API with the owner's key from `.local/`; the owner
   buys the credit and makes every pick; agents never buy credit or switch to a costlier model unasked.

### Running the OpenAI engine

*Moved on 17 Nov 2026 to [paid image generation rules](paid-image-generation-rules.md#running-the-openai-engine).*

### Cost for four vehicles

Nine images a vehicle (three bodies, three masks, two overlays, one icon), four attempts each: 144 generations, of
which 80 are bodies and overlays.

| Scenario | Generations | USD | AUD before GST |
| --- | --- | --- | --- |
| Recommended: Nano Banana 2.1 bodies and overlays; masks and icons by script | 80 at about $0.036 with reference input | about $2.90 | about A$4.15 |
| The same in batch (half price, slower) | 80 | about $1.50 | about A$2.15 |
| Everything by Nano Banana 2.1 | 144 | about $5.20 | about A$7.40 |
| Everything by gpt-image-2 medium plus reference input | 144 | about $7.30 | about A$10.40 |
| Everything by FLUX.2 pro edit on fal | 144 at $0.045 | about $6.50 | about A$9.30 |
| Everything by Nano Banana Pro | 144 at $0.134 | about $19.30 | about A$27.60 |
| Everything by gpt-image-2 high | 144 at $0.165 or more | $24 or more | A$34 or more |

The smallest prepaid top-up on OpenAI, fal or Recraft (US$5 to $10) is larger than the job itself.

### Caveats

- **Exchange rate and GST** move the AUD figures; add 10% GST.
- **Pages that refused the fetch:** OpenAI's terms, Midjourney's pricing and terms, Leonardo, Firefly and Tripo's
  pricing; those rows rest on dated third-party summaries and are marked 3P.
- **Model names:** Google's page lists Nano Banana 2.1 as `gemini-nano-banana-2.1`; confirm in AI Studio before the
  engine is written. gpt-image-1-mini and Gemini 2.5 Flash Image are deprecated; build on neither.
- **Terms change.** Record the terms read on the day beside every shipped image.

### Sources

Every price and terms link is in the table, read 8 Nov 2026. The exchange rate is from
[poundsterlinglive](https://www.poundsterlinglive.com/history/AUD-USD-2026). Steam's AI-content disclosure is Valve's
rule and separate from any vendor's terms.
