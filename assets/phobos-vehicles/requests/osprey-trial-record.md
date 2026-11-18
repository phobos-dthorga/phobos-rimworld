# The V-22 Osprey's body: a trial across four services (18 Nov 2026)

**Status (18 Nov 2026):** four pictures made, one from each service. **The owner picked Nano Banana Pro**, with Nano Banana 2 "a very
close call". The pick is on `art-masters` (`vehicles/osprey/`), the other three and the plan drawing on `art-references`
(`generated/vehicles/osprey/`). Nothing is exported or shipped.

The owner asked for the Osprey's body to come from another service than ChatGPT for its look ([the Osprey's record](../../../docs/development/vehicle-osprey.md#size)),
so the same brief went to four models through fal.ai (the `fal` engine, [its rules](../../../docs/development/paid-image-generation-rules.md#running-the-fal-engine)).
Requests: [osprey-trial.json](osprey-trial.json). The plan reference is drawn by [draw_osprey_plan.py](../draw_osprey_plan.py) from the
published dimensions; the finish reference is our approved tanker body (bulk-tanker-body-north-2). Both are our own art.

## What each made

At 4,096 x 4,096 px. Costs are the engine's table ceilings: fal's answers carry no price, and the key cannot read the account's
balance, so the true spend is on the owner's fal dashboard.

| Service (model) | Seconds | Cost ceiling | What it did, judged at full size and on dark ground (agent's review) |
| --- | --- | --- | --- |
| Google Nano Banana Pro (`fal-ai/nano-banana-pro/edit`) | 56 | US$0.30 | **Closest to the brief and to the game's look.** Thick outline, flat shading, many small clean details. Nacelles upright with round hub caps and two orange bands, as asked. It gave the wing a slight forward sweep, which the real V-22 has, though our plan drew it straight. Sponsons small, with stub fins. Pure white background, so it cuts out cleanly |
| Google Nano Banana 2 (`fal-ai/nano-banana-2/edit`) | 588 | US$0.24 | Rich detail (walkway strips, wing panels). **The nacelles are drawn as pointed pods lying flat, so it reads as forward flight**, not the upright nacelles asked for; the main wheels show beside the sponsons. Pure white background |
| ByteDance Seedream 4.5 (`fal-ai/bytedance/seedream/v4.5/edit`) | 65 | US$0.04, twice | **Drew the rotors, which the brief excluded**, in a pixel-art finish that matches nothing else of ours. Answered in JPEG, which the engine first refused after paying (US$0.04 lost); fixed the same evening, and the re-run's original is kept as `osprey-trial-seedream-45-1.original.jpg` |
| Black Forest Labs FLUX 3 Image (`blackforestlabs/flux-3/edit-image`) | 214 | US$1.19 | **Copied our plan drawing almost shape for shape**, with little detail added. **Its background is speckled grey, not white**, so it does not cut out. The dearest by far |

**Spent, by the ceilings:** US$1.81 (the four pictures, plus the Seedream picture lost to the engine's fault). The cap for the
first run was US$1.80 and for the re-run US$0.05.

## The agent's recommendation

**Nano Banana Pro**, as the pilot: it follows the brief, matches our finish and cuts out cleanly, at a mid price. FLUX 3 kept the
shape best but added the least; Nano Banana 2 drew the wrong nacelles; Seedream ignored the exclusions.

## The owner's pick (18 Nov 2026)

**Google Nano Banana Pro**, the agent's recommendation too: "Google Nano Banana Pro is my choice too but the Google Nano Banana 2 is a
very close call." Archived the same evening with `scripts/art/archive_branch.py`: the pick to `art-masters`, the rest to
`art-references`; none was truly fugly. Nano Banana 2's flat-lying nacelles are kept in mind as a reference for the forward
nacelle pictures the tilt needs ([the Osprey's record](../../../docs/development/vehicle-osprey.md#tilting-nacelles-18-nov-2026)):
it is our own picture now, so it may be sent as one.

## Next

1. ~~The background AI and Segment Anything on the pick~~ **Parts cut on the owner's go, 18 Nov 2026, with the game closed**, by the
   new `scripts/art/name_parts.py` from [osprey-parts.json](osprey-parts.json): twelve parts and three fuselage sections, in
   `.local/art/parts/` under the pick's SHA-256, with a review picture. Three runs: the first missed the right sponson and let parts
   overlap; the second clipped each part to its plan box and settled overlaps by priority; the third widened the nacelles' boxes
   (the pick draws them longer than the plan) and smoothed harder. Left as found, to tidy at export: a small patch of the right
   sponson on the right wing's root, a ragged edge where the tailplane meets the fuselage, and a stray speck in the left hub. The
   hubs' centres, the rotors' anchors, are (583, 1659) and (3507, 1668) in the pick's pixels. The background AI had nothing to
   look at: its tool covers only pictures behind shipped textures, so it runs on the pick at export.
2. The overlays, and the nacelle pictures at set angles, as reference edits of the pick on the same service: a dry run and the
   owner's spending limit first.
