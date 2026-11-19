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

The owner's picks of 19 Nov 2026, fitted by `assets/phobos-vehicles/export_armour_kits.py` on this body's transform, so they register with it. **The armoured cab and heavy cab plates are AI-generated by paid editing**: fal's Nano Banana Pro edit (Google's model, through fal.ai) of this truck's own cab, cut from the picked body (`assets/phobos-vehicles/draw_kit_crops.py`), one try each, US$0.15 a picture (`assets/phobos-vehicles/requests/truck-cab-kits-paid-record.md`); the kit is the changed area, made solid and laid back at the cut's place. **The ram bar and the body skirts are AI-generated on the local model** (FLUX.2 Klein 4B base with our private style model, night 7; `assets/phobos-vehicles/requests/truck-kit-parts.json`, ram bar 11, skirt 14), drawn alone and fitted by the script: the skirt repeated end to end down each side and mirrored for the right. The background AI looked at every original first. Placement of the ram bar and skirts is the agent's, to revise once seen in game. Only our own art was used.

- `SurveyTruck_ArmouredCab_north.png`: SHA-256 `0f2c4e26476ca70a3846aef5b123ebc5bcb39d821a31c668d639b91345befa6d`
- `SurveyTruck_ArmouredCab_east.png`: SHA-256 `f636f54b74f3af7a97717a7fa1499ab1148180ab220bef8d1dce7e56d2b65553`
- `SurveyTruck_ArmouredCab_south.png`: SHA-256 `76d7fd65537d4b7769c96f5d3cdeb6da66501426272b2ea21d33d65cd5c63347`
- `SurveyTruck_HeavyCabPlates_north.png`: SHA-256 `9a18e6a6d8e2d3fca5d8fe28f62a54a83809901d4190e8cdcbe4198f30e0c1d6`
- `SurveyTruck_HeavyCabPlates_east.png`: SHA-256 `aeb9e8f4aaa82fdd4a2a7c99c38b9ade1486fb61dd89facc35419b9e450ccda1`
- `SurveyTruck_HeavyCabPlates_south.png`: SHA-256 `53520030f810048d5d019214ad1b68d5f54f8c75e06f84bf971cd42b0f30ef1c`
- `SurveyTruck_RamBar_north.png`: SHA-256 `6e4565c18598e07b6891538a3bad91dd169ca318c128ba3b5bcc506a0ef9e76a`
- `SurveyTruck_RamBar_east.png`: SHA-256 `82eda56268f70866a362d83b8cb7a59ca0c9a73c7dc4f89a968a35ee4761d995`
- `SurveyTruck_RamBar_south.png`: SHA-256 `47ca82420aa4a6ef744356587d67bc6d4c7b26e25200165a30065b96e9b6fa61`
