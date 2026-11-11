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
