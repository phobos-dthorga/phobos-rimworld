# Art services for vehicle sprites: research (8 Nov 2026)

A research record behind the art plan in [utility vehicles](utility-vehicles-design.md). Prices and terms were read on
**8 Nov 2026** and move; re-read before paying. Where a service's own page refused the fetch (403), the row relies on
a dated third-party summary and says so ("3P"). AUD figures use about US$0.70 per A$1 and exclude the 10% GST that
overseas digital services add. The owner is on a pension and would rather pay per image than renew a monthly plan; the
earlier decision to try local generation first ([artwork direction](artwork-direction.md), 19 Oct 2026) still stands
for items and icons, and this record replaces that record's paid-route row.

## Read this first

- **The owner's pick (8 Nov 2026): the OpenAI image API for vehicle bodies**, which they already know and their own sources rate
  best for RimWorld's look; **icons stay on the owner's local generation**, which already drew on-style icons for free. The owner
  is setting up an API account (ID verification, prepaid credit with automatic top-up off). The comparison below is kept as read.
- **Rotate, then edit.** A true top-down east facing is the north facing turned a quarter, and south is north turned a
  half. A script does the turns for free; the model then only fixes lighting and the parts that differ end to end.
  Facings that start from the same pixels match far better than three separate generations.
- **Best value: the Gemini image API** (Nano Banana 2.1, about US$0.034 an image at 1K, half in batch). **Good
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

## What the sprites need

Per Vehicle Framework vehicle: a north, an east and a south body (west is mirrored by the framework) at 128 px per
drawSize unit, so a 2 by 4 cell truck drawn at (3, 5) is 384 by 640 px north and 640 by 384 px east; a colour mask for
each (flat colour zones for the three-colour pattern shader); overlays for a raised mast or a tipped body and for
damage; and a 64 px icon. The look is the game's: thick near-black outline, soft cel shading, pale panels, rounded
corners, straight top-down, no perspective. Outputs ship in a free public Workshop mod, so the terms must allow it.

## The comparison

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

## The pipeline this points to (agent recommendation)

1. **North body:** Nano Banana 2.1 through the Gemini API with a short style prompt and one to three style references
   (our own approved sprites, never the game's art in anything that ships); generate at 1K or 2K, downscale by script
   with area resampling and alpha clean-up to the exact size.
2. **East and south:** turn the finished north by script, then send the turned image back as a reference edit ("same
   vehicle, light from the top, keep the outline and colours"); where the ends differ, edit from the north plus the
   turned draft as two references.
3. **Overlays:** reference edits of the body, cut out by script against the base.
4. **Masks:** by script, posterising the panel zones into flat colour regions, or hand-painted over a faded copy; a
   model pass at the same price where the script cannot separate them.
5. **Icon:** downscale the north or east body and sharpen the outline, by script.
6. **Fallbacks:** gpt-image-2 medium if Gemini's style drifts; FLUX.2 pro edit on fal for the turned-image edits.
7. **Records:** every image through the near-copy check (`scripts/art/near_copy.py`) with a record naming the tool,
   model, prompt, date, SHA-256 and AI disclosure, plus the service's output terms as read that day; masters on the
   `art-masters` branch through `scripts/art/archive_branch.py`.
8. **Who does what:** a new engine in `scripts/art-run.py` calls the API with the owner's key from `.local/`; the owner
   buys the credit and makes every pick; agents never buy credit or switch to a costlier model unasked.

## Running the OpenAI engine

**The owner's pick (8 Nov 2026): the OpenAI image API for vehicle bodies, local generation for icons.** The engine was built
on 8 Nov 2026 (`scripts/art/openai_images.py`, used by `scripts/generate-art.py` and `scripts/art-run.py`); offline tests in
`tests/test_openai_images.py`. **No real image has been made with it yet.** The rules for using it, learnt in Phobos
Ostranauts, are in [paid image generation rules](paid-image-generation-rules.md): read them before writing a request.

- **The key** is read from the `OPENAI_API_KEY` environment variable only (the owner's user variable). It is never written to
  a file, record or report, and key-shaped text in an error is blanked. The owner's key is a restricted key: it may make
  images but not list models, which the engine never needs. On 8 Nov 2026 a deliberately invalid request (a 7 by 7 size)
  showed the key and `gpt-image-2` are accepted; an invalid request makes no image and costs nothing.
- **A request** uses `"engine": "openai"`, a prompt, a width and height (each divisible by 16, shape between 1:3 and 3:1,
  at most 3840 by 2160), optional `"quality"` (`low`, `medium` the default, `high`), `"background"`, `"moderation"`, and
  up to 16 `"references"` (PNG, JPEG or WebP), which turn the call into an edit. A reference is a path or
  `{"image": path, "role": "what it is for"}`; the role is recorded and belongs in the prompt too. **A reference must be our
  own art** (under `assets/`, `mods/` or `.local/art/`): the engine refuses the game's extracted textures and other authors'
  art before anything is sent. `"seeds"` are attempt numbers that name the
  files; the API has no seed, so a rerun differs. Local settings (`init`, `lora`, `steps`, `cfg`, `model`) are refused.
- **The cap:** `--max-usd` is required for any paid request (`"max_usd"` in a night step). Before each image the run checks
  that what is spent plus that image's highest estimate stays within the cap, and stops otherwise. The cost recorded is worked
  out from the token counts OpenAI returns, at the pricing page's rates; a call with no answer counts its estimate, because it
  may still have been billed. The account's own spending limit, which the owner has set, is the second guard.
- **Stops at once** on no credit, a bad key or an account check (organisation verification); a refused prompt fails that image
  only. Rate limits and server errors are retried twice; a timeout is never retried.
- **No graphics card:** a run with only paid requests never starts ComfyUI and does not stop for the game. The stop time and
  the disk floor still apply.
- **Each record** adds the quality, the endpoint, the token counts, the cost, the size that came back (and a note when it
  differs from the size asked), each reference's role, an AI disclosure and the terms note as read on the day, beside the
  usual prompt, size, date and SHA-256.

```text
python scripts/art-run.py generate <requests.json> --dry-run
python scripts/art-run.py generate <requests.json> --stop-at 23:00 --max-usd 0.50
```

The dry run sends nothing and prints the most the missing images could cost and anything the API would refuse.

## Cost for four vehicles

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

## Caveats

- **Exchange rate and GST** move the AUD figures; add 10% GST.
- **Pages that refused the fetch:** OpenAI's terms, Midjourney's pricing and terms, Leonardo, Firefly and Tripo's
  pricing; those rows rest on dated third-party summaries and are marked 3P.
- **Model names:** Google's page lists Nano Banana 2.1 as `gemini-nano-banana-2.1`; confirm in AI Studio before the
  engine is written. gpt-image-1-mini and Gemini 2.5 Flash Image are deprecated; build on neither.
- **Terms change.** Record the terms read on the day beside every shipped image.

## Sources

Every price and terms link is in the table, read 8 Nov 2026. The exchange rate is from
[poundsterlinglive](https://www.poundsterlinglive.com/history/AUD-USD-2026). Steam's AI-content disclosure is Valve's
rule and separate from any vendor's terms.
