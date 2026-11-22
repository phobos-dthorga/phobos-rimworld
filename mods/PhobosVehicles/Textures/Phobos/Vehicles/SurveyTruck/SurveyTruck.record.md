# Survey truck textures: asset record

Made on 9 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, picked by the owner. Exported by `scripts/art/export_vehicle.py` from the masters below; prompts are in `assets/phobos-vehicles/requests/survey-truck-pilot.json` and `survey-truck-boom.json`, the masters and their records on the `art-masters` branch (`vehicles/survey-truck/`), the other attempts on `art-references`. The only reference uploaded was our own approved tanker body and, for the boom, our approved truck body. No game or other author's art was uploaded or used.

East and south are the north turned by the export script. The boom master was placed unchanged on the body's 1024 x 2048 canvas and fitted to the box 458 1118 564 1850 in master pixels, onto the deck's channel.

Pilot: 4 images, about US$0.189; boom: 3 images, about US$0.110 (run reports beside the masters).

| Texture | Master | Master SHA-256 |
| --- | --- | --- |
| SurveyTruck | survey-truck-body-north-1 | `1785df1c0645f4fd345cc1dccfabb2d53f3a011932bd45e13fa42630f057da01` |
| SurveyTruck_Boom | survey-truck-boom-1 | `76ca858b0e69d5e0d09e72b7fa08aa3708c18fadf748e8c5b225ebfbb64be689` |

## Overlays of 12 November 2026

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from cards in the session. Each part was drawn alone on white with the picked body as its only reference (our own art, for style), placed unscaled on the body master's canvas, then moved and scaled into its place by `assets/phobos-vehicles/export_batch_b.py` and `scripts/art/export_vehicle.py`. Mirrored parts are left-right flips of the pick, new files that never replace it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art. The prompts are in `assets/phobos-vehicles/requests/` (batch-b-pilots.json, batch-b-family.json, seabed-crawler-overlays.json, survey-truck-overlays.json); the review, spend and picks are in [the night's record](../../../../../../assets/phobos-vehicles/requests/night-2026-11-12-record.md). Placements are the agent's, for the owner to revise once seen in game.

| Texture | Part master | Fit box in the body master | Master SHA-256 |
| --- | --- | --- | --- |
| SurveyTruck_SetUp | `survey-truck-outrigger-1.png` | 790, 1120, 950, 1190 | `199ef540c1bea549...` |
| SurveyTruck_SetUp | `survey-truck-outrigger-1-mirrored.png` | 74, 1120, 234, 1190 | `782716328c441b1f...` |
| SurveyTruck_SetUp | `survey-truck-outrigger-1.png` | 790, 1850, 950, 1920 | `199ef540c1bea549...` |
| SurveyTruck_SetUp | `survey-truck-outrigger-1-mirrored.png` | 74, 1850, 234, 1920 | `782716328c441b1f...` |
| SurveyTruck_RadiometricProbe | `survey-truck-probe-1.png` | 590, 1290, 760, 1600 | `59c074dee29d4f91...` |
| SurveyTruck_LongRangeArray | `survey-truck-dish-2.png` | 400, 800, 624, 1024 | `690851f20d4957be...` |
| SurveyTruck_PoweredArray | `survey-truck-powered-array-1.png` | 285, 1280, 455, 1820 | `9a80fc4cc46a9adc...` |
| SurveyTruck_SurveyBattery | `survey-truck-battery-box-1.png` | 270, 1130, 460, 1262 | `788d8adea4dd5450...` |
| SurveyTruck_WorkLights | `survey-truck-work-lamps-2.png` | 300, 745, 724, 805 | `7431683b8cd15fa8...` |

## Damage pictures (12 Nov 2026)

Made free from our own bulk tanker damage masters (`bulk-tanker-damage-body-1`, `bulk-tanker-damage-tank-1`, OpenAI gpt-image-2, 8 Nov 2026): a different crop for each place, fitted with `scripts/art/export_vehicle.py overlay --marks` (darkness only) and clipped to the vehicle's outline, by `assets/phobos-vehicles/export_damage.py`, which lists every crop and box. Shown by D'thorga Framework's damage looks below 60% part health. No game or other author's art was used.

| Texture | North SHA-256 |
| --- | --- |
| SurveyTruck_DamageCab | `c94c8495b0cf592a37d6a828170d216bb98ab536ab1ad76c0ee8dd722cfdd5d7` |
| SurveyTruck_DamageCabin | `4cb09a688c62c00f592c2c7f0d7c915c87e62c06cf2e391771d9a471faf0270c` |
| SurveyTruck_DamageDeck | `8be04f15aab27b0aa56a6bbf641684b6de891971f1c9b67cc26482e39359060c` |

## Boom extras (12 Nov 2026)

Made with OpenAI gpt-image-2 (medium): **AI-generated, picked by the owner on 12 November 2026** (geophones 1, reel 1), with the picked boom as the only reference, for style. Drawn alone, placed on the body master's canvas and fitted along the boom (geophones 492 1250 530 1680, reel 474 1150 548 1240, in body master pixels; the agent's placement). Shown as extras of the boom, swinging with it. The prompts are in `assets/phobos-vehicles/requests/survey-truck-boom-extras.json`.

| Texture | Master | Master SHA-256 |
| --- | --- | --- |
| SurveyTruck_Geophones | `survey-truck-geophones-1` | `04f119a52a71d8138a0e6047c9320e5c7e1ae237081082383aa9e1fa673077e7` |
| SurveyTruck_ResistivityReel | `survey-truck-resistivity-reel-1` | `c9d82befb734f869ff34f10133b6f38c309a73c3bfa1c8842b330b213c38c607` |

## Fitted pictures, round three (12 Nov 2026, evening)

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from a labelled sheet at in-game size. Each part was drawn alone on white with one of our own picked bodies as its only reference (for style), then placed by `assets/phobos-vehicles/export_round_three.py` and `scripts/art/export_vehicle.py overlay`. The winch is the pick turned half a turn so its hook faces forward, a new file that never replaces it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art (the check as fixed that evening; [the record](../../../../../../assets/phobos-vehicles/requests/round-three-record.md)). The prompts are in `assets/phobos-vehicles/requests/fittings-overlays.json` and `trailer-overlays.json`. Placements are the agent's, for the owner to revise once seen in game.

| Texture | Shown when | Part master | Master SHA-256 | North SHA-256 |
| --- | --- | --- | --- | --- |
| SurveyTruck_Winch | the recovery winch is fitted | `fitting-recovery-winch-overlay-1-turned-180.png` | `12d0883ccab36557e61075786c3e331159fe8cf1d3222aaa09ee72b78b60eb7e` | `c449289ed92f796db510723457da4acdb62aac2c36b9aff132e17d0224b3e4fb` |
| SurveyTruck_AuxFuelTank | the auxiliary fuel tank is fitted | `fitting-aux-fuel-tank-overlay-2.png` | `efb2446cd2635534963dd8844e942263c4d93be8dc0ee45b6ddb668d413a0e2f` | `2adba34b8b67c06be05df011a5afc0b207ea592ef0070dead5e54310ce3fb47d` |

## The drawn armour kits (19 Nov 2026)

The owner's picks of 19 Nov 2026, fitted by `assets/phobos-vehicles/export_armour_kits.py` on this body's transform, so they register with it. **The armoured cab and heavy cab plates are AI-generated by paid editing**: fal's Nano Banana Pro edit (Google's model, through fal.ai) of this truck's own cab, cut from the picked body (`assets/phobos-vehicles/draw_kit_crops.py`), one try each, US$0.15 a picture (`assets/phobos-vehicles/requests/truck-cab-kits-paid-record.md`); the kit is the changed area, made solid and laid back at the cut's place. **The ram bar and the body skirts are AI-generated on the local model** (FLUX.2 Klein 4B base with our private style model, night 7; `assets/phobos-vehicles/requests/truck-kit-parts.json`, ram bar 11, skirt 14), drawn alone and fitted by the script: the skirt repeated end to end down each side and mirrored for the right. The background AI looked at every original first. Placement of the ram bar and skirts is the agent's, to revise once seen in game. Only our own art was used. **Cleaned on 22 Nov 2026** (the owner's go, by card, after `scripts/art/edge_check.py` found loose outline strokes beside the cabs): the lift now drops strokes of the old cab left standing alone on the paper, takes the paper's pale rim off the kit's edge, and draws the kit a dark outline about one game pixel wide, so the body's pale roof no longer shows beside it; on 23 Nov the kit's edge was made hard before it is laid on white, as a softened edge had taken a pale rim outside that outline (the fringe check). Same edits, same place.

- `SurveyTruck_ArmouredCab_north.png`: SHA-256 `637747b33a00ea445ff6899d712927d8cf6302a8ce34fdd6a823f383c148e512`
- `SurveyTruck_ArmouredCab_east.png`: SHA-256 `d7ca19ed46457d2e9dc4600e612ea580b513a088f787a78084ea13a7f196939f`
- `SurveyTruck_ArmouredCab_south.png`: SHA-256 `d0e11edd9fe06a4c0ed901924724caba73ac1a287a2159ed76c4378c4fd45306`
- `SurveyTruck_HeavyCabPlates_north.png`: SHA-256 `ad55f6d51c44c88fe5710b9a191382d774d41099175461b421bc5a380842de22`
- `SurveyTruck_HeavyCabPlates_east.png`: SHA-256 `a9239ba4aa733ab4fd0efb8b5ec52e6b2f5c1909993146f89a4c1bb88074115d`
- `SurveyTruck_HeavyCabPlates_south.png`: SHA-256 `a9a775d26778c1f04860cb17da8ff48d14506f036886a53c30357d605b8e4466`
- `SurveyTruck_RamBar_north.png`: SHA-256 `6e4565c18598e07b6891538a3bad91dd169ca318c128ba3b5bcc506a0ef9e76a`
- `SurveyTruck_RamBar_east.png`: SHA-256 `82eda56268f70866a362d83b8cb7a59ca0c9a73c7dc4f89a968a35ee4761d995`
- `SurveyTruck_RamBar_south.png`: SHA-256 `47ca82420aa4a6ef744356587d67bc6d4c7b26e25200165a30065b96e9b6fa61`

## Camp gear drawn on the vehicle (22 Nov 2026)

Shown while the gear is fitted (D'thorga Vehicles 0.103.0; the owner's "everything on ours", 22 Nov 2026). **AI-generated**: OpenAI gpt-image-2
through the image API, medium quality, each an edit of a crop of this vehicle's own tinted game picture at twice its size (paid rule 13; our
own art the only reference), two attempts each; lifted out with `scripts/art/sam_lift.py --cpu` (Segment Anything 2.1, Meta, Apache 2.0; the
roof tents with only what is darker than the cab kept, so no patch of the cab's paint comes with them), exported by
`scripts/art/export_vehicle.py overlay` at the picture's own scale. Where the lift failed twice on dark gear over dark metal, the picture was
made by script instead (paid rules 7 and 11). Request and review: `assets/phobos-vehicles/requests/camp-gear-overlays.json`,
`camp-gear-overlays-record.md`.

- `SurveyTruck_RoofTent` (survey-roof-tent): attempt 2 of 2, the owner's pick. Edit `camp-gear-survey-roof-tent-2`, SHA-256 `49304c7579f705cfb03045c6c03a624434ed5ff10570df395a5c9fbaacf4a29e`; north `29729a6ef3b88f358eb7274662303174a7dda21d31117a798e4ec11e059bdb4d`
- `SurveyTruck_SideRoll` (survey-side-roll): made by script from the Billy's lifted roll (attempt 2), laid along this vehicle's left rail. Edit `camp-gear-survey-side-roll-1`, SHA-256 `fa698843041ab0d89fc29edcf54e792e2c1d725d573c7b716f33528bbc336d17`; north `6729499a0c31162dba6d988964ff165ffd356f7072024c21c0cd463543187eda`

**The owner's review, 22 Nov 2026** (D'thorga Vehicles 0.104.0): the lifted roof tents had "issues and dithering" (parts scrubbed out); the
roof tent is now drawn alone on white and laid by script, so the sprite exporter gives clean edges; both shells kept, by roof size.

- `SurveyTruck_RoofTent`, again: the closed roof tent drawn alone (`roof-tent-closed-1`) and laid on the cab by script, the bolder shell for a wider roof; north `1b2af9e4ce5a70e610bd40131d50f203783fc87448e10d059414085bf0b2d915`

**The owner's follow-up, 22 Nov 2026** (D'thorga Vehicles 0.105.0): the rolled bag on whichever side the camp is pitched, and the rear tent drawn.

- `SurveyTruck_SideRollRight`: the left roll mirrored across the vehicle's centre line by script, for a camp on the right; north `e4b200cf748013bd4d7924b9ecdac23ca470edfdb26dd2a393ceb8589328604e`
- `SurveyTruck_RearTentRoof`: a rear tent packed on the roof or tent mount: the same roll turned across it by script; north `4c155a0aebd0f1848767f870c6cedfa4dca1610383306ceb87a3f81e2ba30e27`

**Every rolled bag redrawn (22 Nov 2026, Vehicles 0.105.0):** the roll lifted from the Billy carried a fringe of the Billy's body along one edge, so the bag is now drawn alone on white (`awning-roll-bag-2`, OpenAI gpt-image-2, medium; references our awning roll and camp heater; exported by `export_sprite.py` after the background AI) and laid by script on the left rail, mirrored on the right, and across the roof for a packed rear tent. North pictures' SHA-256:

- `SurveyTruck_SideRoll`: 51ee89ac72478b7054c80880b3350ad22d3ec29a65a49d934232a91858350280
- `SurveyTruck_SideRollRight` (mirrored): aa1529aa19f922961c9da67fe7565835c9c7d70612a8212a9405c7abf23653fe
- `SurveyTruck_RearTentRoof` (across the roof): 00cf8ffef585a6c502c945ed1793ff68c4e8291d84a8651fa5b87e88adf85be0

## Cracked glass (23 Nov 2026)

**Made free from one AI-generated master, picked by the owner on 23 November 2026**: `cracked-windscreen-1` (OpenAI gpt-image-2, medium
quality, US$0.022; SHA-256 `9c1f786172dbc87156f3f02e10b2fe2bbfd25b63a8975529858b2ea25d2b5bfd`; prompt in `assets/vehicle-systems/requests/cracked-windscreen-pilot.json`, our approved tipper
the only reference, for style). Its cracks are kept as light marks by `scripts/art/export_vehicle.py`'s `light_marks_to_alpha` (near-white
lines and stars, its own glass falling away), fitted to this vehicle's glass and clipped to its panes by
`assets/vehicle-systems/lay_cracked_glass.py`, so the vehicle's own glass, pillars and outline stay as drawn. East and south are north
turned. Shown by D'thorga Framework's damage looks while the glass part is below 70 %. No game or other author's art was used.

| Texture | SHA-256 |
| --- | --- |
| SurveyTruck_DamageGlass_north | `13e9e56e32dc880594252b88f62e5c24d759c0bdfded7efeee715fd0ad3e2354` |
| SurveyTruck_DamageGlass_east | `1acd5b299fe8c19c36a9db219d36fa41c80d5e8193f16f186881b6b0c70597cd` |
| SurveyTruck_DamageGlass_south | `519d929646c8f9ace15bfdff2fc94da5506592e8c6c56795e38945bf3a38a7f3` |

## Paint made again (23 Nov 2026)

**Facings and paint masks exported again from the same master on the same transform**, D'thorga Vehicles 0.106.1 (the owner's review
of 23 Nov 2026: repainted, the truck looked rusty). `python scripts/art/repaint_vehicles.py stage --paint repaired`, then `apply`:
`scripts/art/export_vehicle.py`'s `absorb_paint_coloured_edges` lets the paint's own darker lines and rims join the paint, so they tint
with it instead of keeping the master's olive, and `repaint_vehicles.py`'s `mend_flecks` mends what `paint_check.py` finds at game size
(0 left). The drawing, its outline and every overlay's fit are unchanged. No game or other author's art was used.

| Texture | SHA-256 |
| --- | --- |
| SurveyTruck_north | `93497c88fffdd9747f985782db0a8a3f57161fca2f94f2cd450b04246a3c07a5` |
| SurveyTruck_northm | `6c52cdc7604f9a1b776369319b479258ec5748fd30b62a832bd668e6be8cd628` |
| SurveyTruck_east | `144f6452940107ef858c5d450b874a483536f7b93670979d0995b0fa1f7452cc` |
| SurveyTruck_eastm | `a769f8d6fec00803f0d0d0497e88bfe1df4cf8be8f5441b4264aed87a9c4e1bf` |
| SurveyTruck_south | `d177307ca5e70e0bbfd532072a7a1f639877417ec1535a8043d2d9fd9061b6dd` |
| SurveyTruck_southm | `c114a12dbf326797376c8cbe23f1c73b879d07fb7269f80ddf1f6f44a6ff6295` |
