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
| SurveyTruck_Winch | the recovery winch is fitted | `fitting-recovery-winch-overlay-1-turned-180.png` | `12d0883ccab36557e61075786c3e331159fe8cf1d3222aaa09ee72b78b60eb7e` | `f7648254602dfe4a3af880bfed281b4bf9be388387ca4a72e6ea97141f3803d4` |
| SurveyTruck_AuxFuelTank | the auxiliary fuel tank is fitted | `fitting-aux-fuel-tank-overlay-2.png` | `efb2446cd2635534963dd8844e942263c4d93be8dc0ee45b6ddb668d413a0e2f` | `2adba34b8b67c06be05df011a5afc0b207ea592ef0070dead5e54310ce3fb47d` |
