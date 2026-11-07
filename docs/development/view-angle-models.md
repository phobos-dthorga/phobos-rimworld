# View-angle models: research record (7 Nov 2026)

The owner asked (7 Nov 2026) whether there is a model, or models, we could download that would suit RimWorld's own art style better,
because a lot of the local model's assets come out at odd angles. **A research record only**: nothing here has been downloaded,
installed, trained or generated, and no candidate has been tried. Every claim about a model comes from its own model card or
repository, read on 7 November 2026; everything marked *agent inference* is ours.

## Read this first (gotchas)

- **The odd angles are a camera problem, not a style problem.** No published model draws "RimWorld top-down". The models below
  either lock the camera to a drawing we supply, or move the camera of an image we already have. The style still comes from the
  references and our private style model, as now.
- **The first trial costs no new base model.** A depth-control LoRA exists for the exact FLUX.2 Klein 4B Base and text encoder already
  installed, under Apache 2.0. The depth map it wants can be drawn by script from the stand-in drawings we already make.
- **One strong-looking route is ruled out by licence.** Hunyuan3D 2.1's licence bars using or displaying its outputs outside its
  territory, which excludes the EU, the UK and South Korea; a Workshop mod is displayed everywhere, so nothing made with it can ship.
- **RimWorld's angle is not plain "top-down"** (agent observation from the game's own sprites, extracted privately in
  [the art automation design](art-automation-and-medical-equipment.md#vanilla-sprites)): buildings and furniture are seen from
  directly above with the south face hinted, beds show the pillow, and pawns are drawn front-on. Prompts, stand-ins and training
  captions should say "seen from directly above, south face hinted" rather than "top-down", which the models read as a photograph
  from a high angle.
- **Every trial needs the owner's go and a stop time** (owner rule, [art backlog](art-backlog.md)); nothing runs while the game is open.
- **Civitai is unreachable from here** (its pages answered 403 and its API 451 on 7 Nov 2026), so the two "RimWorld art style" LoRAs
  there are listed from search results only, with their licences unknown.

## What we have, and what Night 2 showed

The machine: an RTX 4070 Ti with 12 GB of video memory, 64 GB of system memory, ComfyUI 0.41.0 with no custom nodes installed,
and about 100 GB free on C: (checked 7 Nov 2026). Installed models: FLUX.2 Klein 4B distilled and Base (fp8), its Qwen3-4B text
encoder, Z-Image, fal's sprite-sheet LoRA, Real-ESRGAN, and our private style model v1 and v2 (the design is in
[the art automation design](art-automation-and-medical-equipment.md#models)).

Night 2 (6 to 7 Nov 2026) already showed the cure in miniature: given a flat stand-in drawing as the reference, Klein redrew the
hyperbaric chamber from directly above, and the concentrator, the bottle tap and the radio pieces came out on-style
([the backlog](art-backlog.md)). Two things are still missing:

1. A way to lock the angle **and** keep the look of a picked image at the same time. Klein given a sketch and a style reference
   follows the style and drops the sketch's layout (first night's finding, [artwork direction](artwork-direction.md)).
2. A way to **re-angle a pick we already like**, such as hyperbaric-chamber-102, which is drawn at an angle and cannot be fixed by
   rotating it (tried 7 Nov).

## The candidates

| # | Route | Model and author | Licence | Base model and memory | What it fixes | Verdict |
| --- | --- | --- | --- | --- | --- | --- |
| 1 | Caption the view into style model v3 | Our own training, no download | n/a | The installed Klein 4B Base | Makes the view a trigger word, so plain prompts stop drifting | **Do with v3**; costs nothing; untested |
| 2 | Reference plus depth control | RefControl FLUX.2 Klein 4B Reference Depth LoRA, thedeoxen ([model card, primary](https://huggingface.co/thedeoxen/refcontrol-FLUX.2-klein-4B-reference-depth-lora)) | Apache 2.0 | The installed `flux-2-klein-base-4b-fp8` and `qwen_3_4b`; one LoRA file | Locks the layout and angle to a drawing while taking the look from a reference pick | **First trial** |
| 3 | Move the camera of an existing image | Qwen-Edit-2509 Multiple-angles, dx8152 ([model card, primary](https://huggingface.co/dx8152/Qwen-Edit-2509-Multiple-angles)); pre-merged GGUF build by ChrisColeTech ([model card, primary](https://huggingface.co/ChrisColeTech/qwen-image-edit-multiple-angles-GGUF)) | Apache 2.0 (LoRA, the GGUF build and Qwen-Image-Edit-2509 itself) | Qwen-Image-Edit-2509, a 20B model: on 12 GB only as a 4-bit GGUF with offloading to system memory, through the ComfyUI-GGUF custom node; slow | Re-angles a pick we already like ("Turn the camera to a top-down view") | **Second trial**, if the depth route disappoints |
| 4 | Draw in 3D, render from above | TripoSG, VAST AI ([repository, primary](https://github.com/VAST-AI-Research/TripoSG)), then Blender | MIT (TripoSG); Blender is GPL, which covers the tool, not its renders | About 8 GB; shape only, no texture | True orthographic views, and south, east and north from one mesh, as depth maps for route 2 | **Later experiment**: the biggest set-up |

### Route 1: caption the view into style model v3

v3 is already planned ([backlog](art-backlog.md#style-model-v1-kept-v3-next-owner-7-nov-2026)): a larger private set of the game's
buildings, furniture, items and button icons with a sharper upscale. The addition is cheap: **every training caption carries one fixed
phrase** for the view ("seen from directly above, south face hinted"), and every request that uses the style model carries the same
phrase. The model then learns the view as a word it can be asked for, instead of a tendency it may or may not pick up. v1 and v2 were
captioned without it, which may be part of why v2 drifted into the angled view without references (agent inference; untested).

### Route 2: RefControl depth LoRA for Klein 4B Base (first trial)

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

### Route 3: camera-angle LoRAs on Qwen-Image-Edit (second trial)

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

### Route 4: draw in 3D, render from above (later)

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

## Noted and set aside

| Model | Why not |
| --- | --- |
| "RimWorld Art Style" ([Civitai 411781](https://civitai.com/models/411781/rimworld-art-style)) and "RimWorld v1.0" ([Civitai 412891](https://civitai.com/models/412891/rimworld-v10), described as the storytellers' portrait style) LoRAs, SDXL, from search results only; also "Archotech style" ([Civitai 472077](https://civitai.com/models/472077/archotech-style-rimworld), SD 1.5) and a textual inversion ([sd-concepts-library](https://huggingface.co/sd-concepts-library/rimworld-art-style), SD 1.5, MIT) | Civitai is unreachable from here (403 and 451 on 7 Nov 2026), so their licences, datasets and samples are unseen. SDXL is an older base than Klein with a weaker editor; a style trained on the game's art carries the same EULA question as our own private model, with a dataset we cannot inspect. Nothing suggests they fix the angle |
| Kontext-Top-Down-View, prithivMLmods ([model card](https://huggingface.co/prithivMLmods/Kontext-Top-Down-View)) | FLUX.1 Kontext dev under its non-commercial licence; trained on 400 photo-scene pairs for high-angle photographs, not objects on a plain ground |
| Flux-2-Multi-Angles-LoRA-v2, lovis93 | For FLUX.2-dev (32B, non-commercial licence), which does not fit the card; its nearest elevation is "overhead" |
| FLUX.2 Klein 9B and its RefControl LoRAs (thedeoxen has depth, canny, lineart, normal and pose for 9B) | Klein 9B is gated (a click-through: Hugging Face shows the licence and lets the download through once you agree; checked 7 Nov 2026) under the FLUX Non-Commercial License: the owner's decision, as with FLUX 3 Image Klein, and outside the Apache pipeline. The 4B depth LoRA covers the first trial |
| "Top-down pixel art" and other pixel-art LoRAs | Pixel art is the wrong look for RimWorld ([artwork direction](artwork-direction.md)) |
| fal's sprite-sheet LoRA (installed) | Already in use; its sheets include a top-down tile, but it draws flat outlined props rather than the game's soft-shaded buildings |

## What a trial needs (not built in this change)

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

## Sources

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
