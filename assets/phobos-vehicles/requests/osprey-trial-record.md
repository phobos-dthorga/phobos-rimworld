# The V-22 Osprey's body: a trial across four services (18 Nov 2026)

**Status (18 Nov 2026):** four pictures made, one from each service; the owner's pick is pending. Nothing is exported or shipped.

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

## Next, after the owner's pick

1. The pick goes to `art-masters` and the other three to `art-references` with `scripts/art/archive_branch.py`; none is truly fugly.
2. The background AI and Segment Anything on the pick, on the owner's go with the game closed: the parts for damage looks, paint
   regions and the rotor hubs.
3. The overlays, as reference edits of the pick on the same service.
