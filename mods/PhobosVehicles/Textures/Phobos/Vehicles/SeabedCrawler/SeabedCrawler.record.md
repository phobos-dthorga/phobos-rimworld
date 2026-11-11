# Seabed Crawler textures: asset record

Made on 11 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, picked by the owner (11 Nov 2026: attempt 3 as the base hull, attempts 1 and 4 as the two refits). The prompt is in `assets/phobos-vehicles/requests/seabed-crawler-pilot.json` and the review in `docs/development/vehicle-seabed-crawler-art.md`. The only reference uploaded was our own approved bulk tanker body, for style; no game or other author's art was uploaded or used as a reference.

**Two steps by script, both free and both leaving the masters untouched:**

1. `assets/phobos-vehicles/widen_master.py` widened each master sideways about its own centre line, because the generator drew every hull slimmer than asked (1.35 times for attempts 1 and 3, 1.28 for attempt 4, bringing each to about 3.9 to 4 cells across on the 4-cell footprint). On attempt 3 it also painted out a few tiny glyph-like marks on the sail, with the colour around them.
2. `scripts/art/export_vehicle.py body ... --cells-long 9 --draw 5 10 --main-hue 185 240 --accent-hue 8 32 --min-saturation 0.05` cut the white away, scaled each to 9 cells long on a 5 x 10 canvas, and wrote the facings, the colour masks (red: the grey-blue hull; green: the orange band on the sail) and the icon. The hull's grey-blue is greyer than the tool's usual paint (saturation 0.06 to 0.12), hence the lower floor.

| Texture | Master | Master SHA-256 | Widened file SHA-256 |
| --- | --- | --- | --- |
| SeabedCrawler (base hull) | seabed-crawler-body-north-3 | `3ebfca1bbd305bb3fec32e47d277850c858c6992986903a4f6dda063add63ade` | `08867600867f5d0575ac49d9864ba202de8cc160c5ba7c199ba93eb648c26ad0` |
| SeabedCrawler_Research (laboratory refit) | seabed-crawler-body-north-1 | `da5ac0115db40401a631f3d01098bfb8d6fc4085e1f9a51329359e8360ab044e` | `55cfe76ff0f6ef7db6b79ff9bf2afec1771e89f1fc53518508ce52bd3b62f094` |
| SeabedCrawler_Sensor (sensor refit) | seabed-crawler-body-north-4 | `ffe01b7259c0cdf33f7ce8106a20d89399996155716b0a51bf72befbb5c89dcb` | `7d11b875e5a6b7ae89b5b5121db27188caccda46f5a8a9b668314efce6cb592b` |

The masters and the widened files go to the `art-masters` branch. Attempt 2 was flagged by the near-copy check against the game's own art and stays on the owner's PC only.

## Overlays of 12 November 2026

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from cards in the session. Each part was drawn alone on white with the picked body as its only reference (our own art, for style), placed unscaled on the body master's canvas, then moved and scaled into its place by `assets/phobos-vehicles/export_batch_b.py` and `scripts/art/export_vehicle.py`. Mirrored parts are left-right flips of the pick, new files that never replace it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art. The prompts are in `assets/phobos-vehicles/requests/` (batch-b-pilots.json, batch-b-family.json, seabed-crawler-overlays.json, survey-truck-overlays.json); the review, spend and picks are in [the night's record](../../../../../../assets/phobos-vehicles/requests/night-2026-11-12-record.md). Placements are the agent's, for the owner to revise once seen in game.

| Texture | Part master | Fit box in the body master | Master SHA-256 |
| --- | --- | --- | --- |
| SeabedCrawler_BottomingWheels | `seabed-crawler-wheel-housing-3.png` | 880, 870, 990, 1060 | `f0c5c6f1da792e47...` |
| SeabedCrawler_BottomingWheels | `seabed-crawler-wheel-housing-3-mirrored.png` | 34, 870, 144, 1060 | `001279eeb0c444f8...` |
| SeabedCrawler_Hydrophones | `seabed-crawler-hydrophone-strip-1.png` | 845, 1030, 895, 1340 | `d78b7cc28dde0088...` |
| SeabedCrawler_Hydrophones | `seabed-crawler-hydrophone-strip-1-mirrored.png` | 129, 1030, 179, 1340 | `f181682b3545d392...` |
| SeabedCrawler_ActiveSonar | `seabed-crawler-sonar-dome-3.png` | 340, 45, 684, 290 | `92c1a8c0a52d8a99...` |
| SeabedCrawler_Manipulator | `seabed-crawler-manipulator-1.png` | 190, 180, 370, 420 | `4676d50be2930718...` |
| SeabedCrawler_RadarMast | `seabed-crawler-radar-mast-1.png` | 470, 590, 554, 980 | `af3fbedfd0cee771...` |
| SeabedCrawler_SeabedDrill | `seabed-crawler-seabed-drill-2.png` | 400, 1150, 624, 1374 | `e6199ddfadc1a175...` |
