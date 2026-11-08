# Paid image generation: rules and lessons from Phobos Ostranauts (8 Nov 2026)

The rules for every picture we buy from the OpenAI image API (the owner's choice for vehicle bodies, 8 Nov 2026), and
where each rule comes from. The lessons are taken from the Phobos Ostranauts repository
([phobos-dthorga/phobos-ostranauts](https://github.com/phobos-dthorga/phobos-ostranauts)), whose art was made between
25 September and 7 October 2026 with ChatGPT's built-in image generation, one gpt-image-2 API call and about a hundred
PixelLab generations, all recorded request by request. That is our most recent experience of paid generation, and most
of its mistakes were made once and written down. This record carries them over so we do not make them again.

**Scope.** Paid generation only: the `openai` engine of `scripts/generate-art.py` and `scripts/art-run.py`
([how to run it](vehicle-art-services.md#running-the-openai-engine)). The local ComfyUI routes keep their own records
([artwork direction](artwork-direction.md), [art backlog](art-backlog.md)). Where a lesson is PixelLab-specific it is
marked and left behind. **No paid picture has been made for this project yet**; everything here is a rule or a plan,
not an observation of gpt-image-2 on our vehicles.

## Read this first (the rules)

1. **Only our own art is ever uploaded.** References sent to a provider come from `assets/`, `mods/<Mod>/Textures` or
   the owner's picks in `.local/art/`. The game's extracted textures (`.local/references/`), other authors' sprites,
   screenshots and web photographs stay on this PC. **The engine refuses any other reference before sending anything.**
   (Ostranauts: every record states "no game texture or screenshot was supplied"; the local model here has used vanilla
   textures as references, which is fine locally and forbidden for a provider.)
2. **One pilot before a family.** Make one picture, inspect it at native size and at an integer enlargement, and only
   then ask for the rest of the set (the other facings, the other vehicles, the overlays). A family is derived from the
   pilot by script or by reference edits; it is never generated independently.
3. **Every paid run has a cap and a dry run.** `--dry-run` first (free: it prints the cost ceiling and anything the API
   would refuse), then `--max-usd` on the real run. **Agents never buy credit, raise the owner's account limit or move to
   a dearer quality without being asked.** The owner's account limit is the second guard.
4. **The camera goes first in the prompt**, as an instruction, before the object: Vehicle Framework bodies are seen from
   directly above, so the Ostranauts overhead prefix applies to them word for word. Prompt words are guidance, not a
   guarantee: inspect the output for a tilted footprint, a visible side face or a diamond silhouette and reject it
   however good its paint is.
5. **Every reference has a named role**, in the request (`{"image": ..., "role": ...}`) and in the prompt ("Image 1 is
   our approved truck: match its finish only; Image 2 is this vehicle turned a quarter: keep its layout"). The record
   carries the roles.
6. **Draw the empty vehicle.** No cargo, crew, turrets, raised gear, mud, damage, dust, text, logos, numbers, dials,
   screens or lamps in a body. Those are the framework's overlays and the game's own effects, or they are not drawn at all.
   (Ostranauts' ruling of 29 Sep 2026 against painted live-state instruments, and its rule that connections are separate
   objects.)
7. **Correct one defect once, then change approach.** A defect gets one targeted correction (a changed sentence, a
   reference edit). If it survives, change the route (a reference instead of words, a sketch, a different quality), rather
   than rerolling the same request. Never reroll merely to change a crop, a margin or a palette: scripts do those for free.
8. **Record everything the day it is made**: prompt, references with roles and hashes, requested and returned size,
   quality, token counts, cost, date, the provider's terms as read that day, and the review (what was checked at native
   size and what was decided). The engine writes most of this per image; the review and the decision are written by hand
   in the batch's request record. A deviation from the plan is written down with its reason.
9. **Rejected and unselected pictures leave `main` with their reason.** They go to the `art-references` branch through
   `scripts/art/archive_branch.py` (never deleted, never silently kept). Inputs of a selected picture (its sketch, the pick
   it was edited from) stay on `main`; a master shipped or picked goes to `art-masters`.
10. **Approved picks stay.** An approved picture is not regenerated for a hypothetical improvement or to match a newer
    sibling; a family is restyled only when the owner reopens it, and then as a repaint pass over the kept layout.
11. **Prefer a free derivation to a paid generation**: a turned copy, a recolour, a crop, a downscale, a mask drawn from
    the panel zones, an icon from the body. Pay only for what a script cannot produce.
12. **A timed-out call is never retried blindly**: the provider may have made and billed the picture. The engine counts
    the estimate against the cap and moves on; the owner's usage page shows whether it was charged.

## Where the lessons come from

| Ostranauts record | What it holds |
| --- | --- |
| `docs/development/asset-generation-policy.md` | The owner's memoranda of 25 and 27 Sep 2026: layered bases, the overhead-first rule, the small cost-conscious workflow, the rejected-art archive |
| `docs/development/artwork-resolution-policy.md` | Masters at 2x the display size (4x when the short side is 32 px or less), integer nearest-neighbour exports |
| `docs/development/ship-equipment-art-study.md` | The study of 22 vanilla sprites and six owner screenshots that set the style brief; v1 to v2 of the Shipbreaker fixture (owner: "This looks GOOD.") |
| `docs/development/medical-art-handoff.md`, `assets/phobos-medical/requests.json` | The Ward-3 bed and Vigil-2 monitor: exact prompts, a direction change mid-task, a size revision, registration trials |
| `docs/development/oxsmith-art-handoff.md` | Two machines of one family from a pilot and a family reference; the review checklist |
| `assets/phobos-manufacturing/README.md` and its three `*-requests.json` | Seven reactors restyled to one approved finish, three support machines, six tanks: the restyle prompts with two referenced images each |
| `assets/workshop/README.md`, `prompts.json`, `composed.json` | Ten Workshop covers: a series layout held across generations by prompt and by reference; the one gpt-image-2 API call |
| `assets/rejected-artwork-archive.md` | About fifty rejected pictures with one-line reasons: projection, transparency, noise, indicators, near-copies, misrepresentation |
| `AGENTS.md`, section Artwork | The standing rules those records produced |

## The lessons, and the rule each one gives us

### Camera and projection

- **What happened.** The first Agriculture rack and cooker pilots came back in the wrong projection; coolant and reservoir
  work failed the same way; ingots and salt cake came back "with a darker lower band that reads as a side face" or as "an
  isometric slab". The fix that worked was an explicit camera instruction placed before the object description, repeated
  verbatim on every request, plus a reviewed overhead reference for later variants. Even so, the policy records that tool
  camera settings are "weak guidance, not a guarantee".
- **Rule for us.** Vehicle Framework draws bodies straight down, so the prefix carries over unchanged: *"ORTHOGRAPHIC
  VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking straight down at the object's TOP SURFACE ONLY. Rectangular
  edges run horizontally and vertically on the canvas. Show no vertical front or side faces."* Then say which edge is the
  front (north facing: the bonnet at the top). Reject a tilted footprint or a visible side face on sight. Our local
  models had the same trouble ([view-angle models](view-angle-models.md)); a paid model is not exempt.

### A pilot before a family

- **What happened.** Oxsmith's EC-4 was generated and inspected before the CR-4 was requested with the EC-4 as its family
  reference; both passed first time. The tank restyle inspected one 32 px pilot of each family and derived the larger
  sizes from the same source with no further generation. Where a family was generated all at once (the Agriculture
  stages), five of five were rejected together.
- **Rule for us.** The first paid run is **one vehicle's north body, a few attempts, under a small cap**. The owner picks
  one. East and south are derived from that pick (turned by script, then one reference edit each); the other vehicles
  take the pick as their family reference. `--only <request>` runs one request from a file.

### References and their roles

- **What happened.** The restyle prompts named each image's job: "Reference Image 1 is approved original Phobos artwork:
  use its coherent mechanical detail ... and strictly vertical overhead camera. Reference Image 2 is the old original
  Phobos sprite of THIS machine: retain its maker's colours and useful functional landmarks, but improve the design." The
  covers did the same ("Image 1: STYLE AND SERIES LAYOUT reference only. Do not draw its processing equipment or reuse its
  title."). Records kept the role beside each reference's hash. Where a role was not stated, the generator copied the
  wrong thing (a near copy of a flat start drawing; a logo added from nowhere).
- **Rule for us.** Each reference entry carries a `role`, and the prompt repeats it. Typical roles: *style only*, *this
  vehicle turned a quarter: keep its layout, fix the light*, *family reference: same maker, different vehicle*. The record
  shows the role beside the hash.

### Prompt discipline

- **What happened.** The prompts that passed first time shared a shape: camera first; "Create an ORIGINAL ..." with the
  object's job in one sentence; the canvas and what fills it ("fills the whole canvas edge to edge, no margin"); the
  orientation ("back edge at the TOP, crew service edge at the BOTTOM"); the maker's palette named as materials, not hex
  codes; the parts, placed ("UPPER LEFT ... LOWER RIGHT"); the style in the game's terms; then a long exclusion list. Small
  objects were told to keep "THREE dominant assemblies" so they stayed legible at their native size. Family prompts said
  "made by the same maker as ... and matching it exactly in style", and when a sibling started to look like the first
  machine, the next prompt said so ("Do not repeat reactor lids when this equipment needs different working shapes").
- **Rule for us.** Use the skeleton below. Name the vehicle's job, its facing, its maker palette and the three or four
  shapes that must read at the zoomed-out size. Put the exclusions at the end, and add to them whatever the last attempt
  got wrong. Keep the whole prompt in the request file, never only in a chat.

### What the provider does and does not honour

- **What happened.** The built-in tool returned 1254 x 1254 for 2048 x 2048 requests, and 971 x 1619 for a 3:5 request;
  the records kept the returned size and never upscaled. Painted indicators appeared "despite an explicit exclusion".
  Connection stubs asked for in a prompt "came out grey or vanished". Rivet and chipped-paint wording "scattered speckle
  noise over the lid". A correction pass sometimes "preserved the disliked old design almost unchanged". The gpt-image-2
  API call through a CLI exposed no seed or request id.
- **Rule for us.** The engine records the size that came back and says when it differs from the size asked. Do not ask
  for detail that will not survive the downscale (rivets, weld seams, small text), and do not ask the body to carry what
  the framework draws separately. Expect exclusions to be ignored sometimes; the review catches it, not the prompt. The
  API has no seed, so `seeds` are only attempt numbers; a rerun of the same attempt is a different picture.

### Working at the size the game shows

- **What happened.** Ostranauts' sprites are 16 px a tile, so every record carried a native-size preview beside a 4x or
  8x enlargement, and most decisions were made at native size: "narrower beds looked thin on their tiles, wider ones
  flattened the cartridges, and smaller palettes lost the teal" (Ward-3); every nearest-neighbour sample phase of the
  Vigil-2 master was compared and one chosen because it "keeps the vent dark". A generated master is a source, "not
  permission to ship a larger native footprint".
- **Rule for us.** RimWorld is not pixel art, so the palette and nearest-neighbour steps do not carry over; the habit does.
  Vehicle Framework wants 128 px per `drawSize` unit, drawn at the vehicle's cell footprint, so a 2 by 4 truck drawn at
  (3, 5) is 384 x 640 px north. Generate at 1024 x 1536 (the cheapest portrait size), downscale by script with area
  resampling and alpha clean-up, and **review at native size and at half size** (the zoomed-out view players use most).
  The master stays on `art-masters`; the mod ships only the native sizes.

### Records and provenance

- **What happened.** Every batch had a JSON record: date, owner direction quoted, scope, input provenance, usage (calls,
  cost or "not disclosed", allowance before and after), the provider's terms link, then one entry per request with the
  prompt, references, returned size, native and master sizes, the review and the decision. A superseded selection kept its
  entry with the archive commit. The Banking record quotes OpenAI's Services Agreement, sections 4.1 and 4.4: API customers
  own Output, OpenAI assigns any rights it has, and Output may not be unique.
- **Rule for us.** Each paid request file gets a sibling `<name>-record.md` or JSON with the owner's direction, the
  review of each attempt at native size, the pick, the deviations and the archive commits. The engine's per-image record
  supplies the mechanical half. The terms note in each record names the Services Agreement as read on the day; agents
  re-read it when it changes rather than copy an old summary.

### Cost discipline

- **What happened.** The owner "watches ChatGPT costs"; the rules were to read the allowance first, keep retries
  proportional to a specific defect, never silently buy credit or switch provider, and to "poll the original job after an
  uncertain response instead of submitting a duplicate charged request". Free derivations replaced generations wherever a
  script could do the job: the steel ingot is a recorded recolour of the aluminium one, the CO stores recolours of the
  dome stores, a propellant line a recolour of the water line, the Ward-3's damaged forms the game's own damage tint.
  Ten PixelLab submissions once hit a queue limit and were not retried; a 40-generation storyboard was rejected and not
  retried either.
- **Rule for us.** Script first: east and south from the north by rotation, masks by posterising panel zones, the icon by
  downscale, damage by the framework. Pay for the north body, the reference edits that fix the turned facings, and
  overlays. The cap, the dry run and the estimate are in the engine; the discipline is ours.

### Keeping the record honest

- **What happened.** Records separated "observed source evidence", "design inference" and "owner screenshot evidence"; a
  fictional machine was "not validated reactor engineering"; a cover was "an illustration, not a gameplay screenshot";
  approval of a picture was never claimed as approval in game ("owner in-game review pending" on every one).
- **Rule for us.** "Picked" means the owner chose it from the review page; "shipped" means it is in the mod; "tested in
  game" is said only when the owner says so, as everywhere in this repository.

## What is different for RimWorld vehicles

| | Phobos Ostranauts | Our vehicle bodies |
| --- | --- | --- |
| Look | Coarse pixel art, 16 px a tile, 96-colour palettes, nearest-neighbour exports | RimWorld's clean cel look: thick near-black outline, soft flat shading, pale panels, rounded corners; area resampling |
| Camera | Straight overhead for world sprites | The same: Vehicle Framework bodies are straight top-down |
| Facings | One sprite, the game rotates it | Three bodies (north, east, south; west mirrored), derived from one design |
| State | Colour, normal and damage maps, loose forms | Colour masks for the pattern shader; overlays for raised gear and damage; no normal maps |
| Upgrades | None | **The body is the bare platform, and every upgrade node that changes the look is its own overlay set** on the body's canvas (owner, 8 Nov 2026: every vehicle is upgradeable, and the art follows the tree). The request record lists the nodes before the first picture, so a later node never redraws the body |
| Background | Mostly opaque full-footprint squares (transparency kept failing) | Transparent bodies with a margin: generate on plain white and cut out by script (Ostranauts' edge flood fill), or use the API's transparent background, which is in preview on gpt-image-2 |
| Portrait | The world sprite served as the portrait | A 64 px icon, by downscale of the north body |
| Provider | ChatGPT's built-in tool (no cost or seed disclosed) | The gpt-image-2 API: token usage returned, cost recorded per picture, no seed |

## The prompt skeleton for a vehicle body

The pieces, in this order. Replace the bracketed parts; keep the camera prefix verbatim.

```text
ORTHOGRAPHIC VERTICAL OVERHEAD PLAN VIEW. Camera directly above, looking straight down at the
object's TOP SURFACE ONLY. Rectangular edges run horizontally and vertically on the canvas. Show
no vertical front or side faces.

Create an ORIGINAL game vehicle sprite for [the D'thorga survey truck]: [one sentence on what it
is and does, in the world of a hard-scrabble frontier colony]. Draw the BARE PLATFORM only: the
chassis, cab and empty deck. [The mast / tank / boom] is drawn separately as an overlay.

[If references: Image 1 is our approved [vehicle]: match its outline weight, shading and finish
only; do not copy its shape. Image 2 is this vehicle turned a quarter: keep its layout exactly,
correct the light to come from the top of the canvas, and redraw only the parts that differ end
to end.]

Portrait canvas. The vehicle fills the canvas with a small even margin on a plain pure white
background. North facing: the bonnet and cab at the TOP edge, the load bed at the BOTTOM edge.
[Maker palette as materials: dusty olive panels, dark rubber tyres, a pale grey cab roof, one
muted orange accent.]

Its roof plan is [three or four shapes placed: a short cab roof at the top with a windscreen
strip; a long open load bed with low sides; two wheel arches each side; a spare wheel on the
tailgate].

Style: a top-down sprite for the game RimWorld: a thick near-black outline around every shape,
soft flat cel shading with two or three tones per material, pale panels, rounded corners, a few
small clean details, no texture noise. Not a photograph, not a 3D render, not pixel art.

No cargo, people, animals, turrets, raised masts, dust, mud, damage, text, letters, numbers,
logos, dials, screens or lamps. No floor, ground, road, shadow outside the vehicle, perspective,
tilt or visible side faces. Top surfaces only.
```

The maker palette, the parts and the exclusions change per vehicle; after each attempt, add what went wrong to the
exclusions and change nothing else (rule 7). **An overlay request** uses the same skeleton with the approved body as Image 1
("match its outline weight, shading and finish; draw only the [tank] in its place on the deck; everything else pure white"), on
the body's canvas size, so the cut-out registers over the body without scaling.

## The pilot flow

1. Write the request file in `assets/phobos-vehicles/requests/`, one request for the north body of one vehicle,
   `"engine": "openai"`, `"quality": "medium"`, `1024 x 1536`, three or four attempt numbers, no references for a
   first design (or a `style only` reference to an approved pick, if one exists).
2. `python scripts/art-run.py generate <file> --dry-run`: read the cost ceiling and any refusal. Nothing is sent.
3. Tell the owner the ceiling and get the go.
4. `python scripts/art-run.py generate <file> --stop-at HH:MM --max-usd 0.50` (the game may be running: a paid-only run
   needs no graphics card).
5. Read the run report and the review page; check the near-copy result; write the review of each attempt into the
   batch record at native size and half size.
6. The owner picks, or asks for one correction. One correction, then a change of approach.
7. From the pick: turn by script for east and south, then one reference edit each with the roles named; masks and icon
   by script; archive the unpicked attempts with their reasons; master to `art-masters`.

## Review checklist (every attempt, before the owner sees it)

- Straight overhead: no visible side faces, no tilt, no diamond footprint, bonnet at the top edge.
- The silhouette is complete and inside the canvas; the background cuts out cleanly (no holes in pale panels).
- Reads at native size and at half size beside an approved vehicle: the same outline weight and shading.
- Nothing painted that the framework draws: no cargo, crew, turret, mast, damage, text, lamps.
- Not a near copy of a reference (the near-copy check ran and did not flag it).
- Returned size equals the requested size, or the record says otherwise.
- The record has the prompt, roles, hashes, cost, date and this review.

## What the engine enforces, and what stays a human rule

| Enforced by `scripts/art/openai_images.py` and the runner | A rule people keep |
| --- | --- |
| References outside our own art are refused before sending | Naming each reference's role in the prompt |
| Sizes, quality, background and moderation values the API would refuse | The camera prefix and the exclusion list |
| `--max-usd` required; a look-ahead estimate stops the run before the cap; the account limit is untouched | Starting with one pilot and a small cap; asking before raising a cap |
| Cost from returned token counts, written per image | Writing the review and the decision into the batch record |
| Returned size recorded; a mismatch noted | Reviewing at native and half size |
| Fatal refusals (no credit, bad key, account check) end the run at once | One correction per defect, then a change of approach |
| No retry of a timed-out call | Checking the usage page after a timeout |
| Key read from the environment only, blanked from errors | Never pasting a key into a file, chat or record |
| The near-copy check in `art-run.py` | Archiving rejects with reasons; keeping approved picks |

## Sources

- Phobos Ostranauts repository, the files named in the table above, read on 8 Nov 2026 from the owner's local clone
  (public at [phobos-dthorga/phobos-ostranauts](https://github.com/phobos-dthorga/phobos-ostranauts)). Owner directions
  quoted there are the owner's; the prompts are agent-written; the rules here are our adaptation and say so.
- OpenAI Services Agreement, sections 4.1 and 4.4 (ownership and non-uniqueness of Output), as recorded in Ostranauts'
  `assets/workshop/README.md` on 7 Oct 2026 ([page](https://openai.com/policies/services-agreement/); it refuses
  automated reads, so this is a dated secondary record until the owner confirms it).
- OpenAI image API reference and pricing, read 8 Nov 2026 ([art services research](vehicle-art-services.md)).
- Vehicle Framework sprite requirements: [utility vehicles design](utility-vehicles-design.md) and
  [art services research](vehicle-art-services.md#what-the-sprites-need).
