# Art night of 12 November 2026: requests, spend, review and picks

The owner's go (12 Nov 2026, after the art audit): "proceed with #1 through #4, and get some art created with ChatGPT", against an
audit estimate of about US$8 with a suggested cap near US$15. Paid images go through the OpenAI image API (gpt-image-2, medium) under
[the paid generation rules](../../../docs/development/paid-image-generation-rules.md); each run had a dry run first and its own cap.
**Picked** means the owner chose it from cards in the session; nothing here is tested in game.

## The free half: night 3's local candidates (8 Nov 2026), judged at last

Made on the owner's GPU on 8 Nov 2026 (ComfyUI, FLUX.2 Klein 4B, the v1 style model; see
[the view-angle models record](../../../docs/development/view-angle-models.md)) and never judged until tonight. The owner's picks,
by seed:

| Subject | Pick | Agent's lean |
| --- | --- | --- |
| Ventilator | 12 | 12 |
| Safety shower and eyewash | 14 | 12 |
| Oxygen cylinder trolley | 12 | 12 |
| Nebuliser | 13 | 13 |
| Anaesthesia machine | 14 | 14 |
| Steriliser | 12 | 12 |
| Carbon monoxide and smoke alarm | 12 | 12 |
| Bronchodilator vials | 14 | 14 |
| Needle decompression kit | 12 | 14 |
| Fluids kit | 12 | 14 |
| Intubation kit | 13 | 12 |
| Electrolyser | top-down 12 | top-down 12 |
| Hyperbaric chamber | depth 11 | depth 13 (12 and 14 in both rows are tilted) |
| Mk. 82 bomb case | 12 | 12 |
| Mk. 84 bomb case | 12 | 12 |
| Radio base station | remake-2 13 | 12 |
| Radio whip | **held** until the radio framework resumes | |

**The liquid oxygen store's frost needs no picture** (owner, 12 Nov 2026: "the frost was a shader"). All four night-3 attempts redrew
the whole store at an angle, so none was usable anyway. The frost becomes code work: a shader, or a rime layer drawn by script from the
store's own outline. The agent's record of 27 Oct 2026 read DMS2's effect as a mote with a texture under the Mote shader, from its Defs;
the texture was never found as a loose file, so a custom shader in its asset bundle is plausible. Unverified either way.

## Paid run 1: batch B pilots (`batch-b-pilots.json`)

Dry run ceiling US$1.39; run with a US$1.50 cap; **12 images, US$0.528**; nothing flagged by the near-copy check. Every attempt came back
straight overhead at the size asked. Width against the footprint, measured on the non-white pixels:

| Vehicle | Canvas | 1 | 2 | 3 | 4 | Pick |
| --- | --- | --- | --- | --- | --- | --- |
| Generator set (2 x 6) | 768 x 1536 | 1.90 cells | 1.64, three axles where two were asked | 1.99 | 1.85 | **3** |
| Drill rig (3 x 10) | 1024 x 2304 | 2.68 | 2.97 | 2.76 | 2.80 | **2** (agent leaned 4) |
| Tipper (3 x 8) | 1024 x 2048 | 2.58 | 2.64 | 2.68 | 2.59 | **3** |

Widening (owner, 12 Nov 2026): only bodies under 2.8 cells, and not the tipper, whose box sets its visible width. **None of the picks
needs it.** The drill rig's canvas is 1024 x 2304 because 1024 x 2048 would have been only 1.45 times its 11-cell drawn length, under the
owner's 1.5 times rule.

## Paid runs 2 and 3: Crawler and survey truck overlays

`seabed-crawler-overlays.json`: ceiling US$1.82, cap US$2.00, **18 images, US$0.968**. `survey-truck-overlays.json`: ceiling US$1.21, cap
US$1.30, **12 images, US$0.667**, nothing flagged. Parts are drawn alone on white and fitted to the picked body by
`export_vehicle.py overlay --fit`.

**Near-copy flags, never to be picked and kept off the public branches:** hydrophone strip 2 (TableButcher_south), sonar dome 1
(DoubleSlabBed_south, FleshmassPieceC, GravlitePanels_a), radar mast 2 (DoubleSlabBed_south), radar mast 3 (DoubleSlabBed_south,
FleshmassPieceC).

| Part | Pick | Note |
| --- | --- | --- |
| Crawler wheel housing | 3 | mirrored by script for port |
| Crawler listening array strip | 1 | mirrored by script for port |
| Crawler sonar dome | 3 | |
| Crawler manipulator (node not built) | 1 | ships as a file until the node exists |
| Crawler radar mast (node not built) | 1 | the only clear attempt; fitted per hull |
| Crawler seabed drill (node not built) | 2 | agent leaned 3 |
| Survey truck probe | 1 | |
| Survey truck dish | 2 | agent leaned 1 |
| Survey truck powered array | 1 | |
| Battery box | 1 | also the generator set's BatteryBuffer, free |
| Roof lamp bar | 2 | also the drill rig's WorkLights, free; lit version by script |
| Outrigger | 1 | placed four times; also the generator set's and drill rig's, free |

Left for a later round: the geophone spikes and resistivity reel, which ride on the moving boom (the code has no per-node look for a
moving part yet), the dish's turning code, and the damage overlays.

## Paid run 4: batch B family (`batch-b-family.json`)

Parts drawn against the three picked pilots, so the bodies can ship: the generator set's fan, cable out, cable reel, bigger alternator
and heat exchanger; the drill rig's folded mast, rotary head, power pack, casing rack, deepchem tank, diamond bit and hammer; the tipper's
plain box, rock body and bulk body. Dry run ceiling US$3.23 (32 images); cap US$3.30. **29 images, US$1.611.**

**Refused by OpenAI's moderation as "violence", not billed:** both casing-rack attempts and the second bigger-alternator attempt. "Casing"
and open round pipe ends most likely read as ammunition. One correction (`batch-b-family-retry.json`, "water-well liner pipes" with
threaded collars, US$0.25 cap) was refused twice more, so under rule 7 the rack changes route: the owner's GPU, or a drawing in code.

**Near-copy flags, as first run:** both cable-out attempts (DoubleSlabBed_south, FleshmassPieceC, structure 0.61 to 0.64, hash 29 to 32),
heat exchanger 2 (DoubleSleepSpot_south, hash 9) and folded mast 1 (Altar2x3_south, hash 11). **The owner disputed the cable flag** ("there
is no pre-existing cable like that in the vanilla game"), rightly: windows blank in both pictures counted as perfect agreement, so a thin
subject on white matched any sprite with empty corners. The checker was fixed to compare only where something is drawn (commit 9789879;
an exact paste of a game sprite still scores 0.999, and a copy tilted 4 degrees is still flagged). Re-checked, both cables and radar masts
2 and 3 pass; every hash-based flag and sonar dome 1 stand. The fixed report is `.local/art/near-copy-recheck/2026-11-12-fixed.json`.

| Part | Pick | Note |
| --- | --- | --- |
| Generator set fan | 1 stock, 2 the upgrade | owner: "#1 is the default, #2 is the upgrade", as its own node (`HeavyFan`, a parked radiator cooled as if driving; agent's proposal for what it does) |
| Generator set cable out | 1 | cleared by the fixed check |
| Generator set cable reel | 2 | |
| Generator set bigger alternator | 1 | the only one made |
| Generator set heat exchanger | 1 | the only clear one; also its node icon |
| Drill rig folded mast | 2 | agent leaned 3; the tall mast is 2 stretched 1.35 times by script |
| Drill rig rotary head | 2 | agent leaned 1 |
| Drill rig power pack | 1 | |
| Drill rig deepchem tank | 1 | |
| Drill rig diamond bit | 1 | |
| Drill rig hammer bit | 1 | agent leaned 2 |
| Tipper box | 3 | |
| Tipper rock and bulk bodies | rock 1, bulk 2 | |

## Shipped

- **Free picks:** commit 3250b91 (Medical 0.14.1, Patches 0.16.9, Radio 0.1.7). The owner's vial pick 14 was flagged (MedicineUltratech_a,
  hash 12), as was 12; attempt 11 ships, the owner's second choice.
- **Vehicles 0.43.0 and Framework 0.98.0:** the three bodies, every picked part fitted by `assets/phobos-vehicles/export_batch_b.py` (its
  boxes are the agent's placements), the new upgrade icons cut from the picks, and D'thorga Framework's node looks for pictures that
  replace a stock one. Records beside each vehicle's textures.

## Spend

| Run | Images | Spent |
| --- | --- | --- |
| Batch B pilots | 12 | US$0.528 |
| Crawler overlays | 18 | US$0.968 |
| Survey truck overlays | 12 | US$0.667 |
| Batch B family | 29 (3 refused) | US$1.611 |
| Pipe rack retry | 0 (2 refused) | US$0 |
| Survey truck boom extras | 4 | US$0.193 |
| **Night total** | **75** | **US$3.967**, against the audit's estimate of about US$8 for the whole backlog |

## Later the same day: the rest of the outstanding list (owner: "proceed with what you mentioned as outstanding except for the GPU work")

- **Boom extras** (`survey-truck-boom-extras.json`, dry run US$0.40, cap US$0.45, **4 images, US$0.193**, nothing flagged by the fixed
  check): the owner picked **geophones 1** and **reel 1**, after asking to see them (a labelled sheet opened on their screen). The owner first
  took them for game art; they are new pictures from OpenAI's image model, and the record says so. Fitted onto the boom's canvas at the
  agent's places and wired as extras that swing with the boom.
- **Damage pictures** for the five vehicles, free: the bulk tanker's two damage masters, cropped (new files in `derived-2026-11-12`), fitted
  with `--marks` and clipped to each vehicle's outline (`assets/phobos-vehicles/export_damage.py`).
- **The store's frost**, free: drawn in code by `assets/phobos-medical/draw_frost.py`. DMS2's own frost, read 12 Nov 2026, is a loose frost
  picture under the game's Mote shader, faded in and out; no custom shader and no asset bundle.
