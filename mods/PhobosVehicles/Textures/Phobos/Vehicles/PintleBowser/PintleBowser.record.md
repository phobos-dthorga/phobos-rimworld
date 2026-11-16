# PintleBowser textures: asset record

**The Pintle water and fuel bowsers' own picture since D'thorga Vehicles 0.56.0**, shared by both (owner, 16 Nov 2026: one body, told apart by paint). Made on 16 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 16 November 2026** from a labelled sheet that showed each attempt large and at about in-game size on soil-coloured ground. Until then both bowsers borrowed the Pintle flatbed's picture, scaled up. Every reference uploaded was our own picked art: the Pintle flatbed body (`flatbed-trailer-body-north-2.png`, for its chassis and style) and the bulk tanker's liquid tank (`BulkTanker_LiquidTank_north.png`, for how a tank is drawn) for the body; the picked body for the two overlays. No game or other author's art was uploaded or used. The prompts are in `assets/phobos-vehicles/requests/bowser-pilot.json` and `bowser-overlays.json`; the masters are on the `art-masters` branch (`vehicles/bowser/`), the attempts not picked on `art-references` (`generated/vehicles/bowser/`).

**Spend: US$0.326 for 7 images** (body: 3 attempts, US$0.147 under a US$0.45 cap; overlays: 2 attempts each, US$0.179 under a US$0.60 cap). Nothing refused by moderation.

**Sizing (rule of 9 Nov 2026):** drawSize was (4.57, 9.14), 585 x 1170 px in game; requested at 896 x 1792, 1.53 times. No other trailer is active on the owner's list. Exported by `scripts/art/export_vehicle.py body --cells-long 160/21 --draw 5 9 --min-saturation 0.12 --accent-hue 8 37` with five `--hole` points (the drawbar frame's four openings and the towing eye). 160/21 is the flatbed's 20/3 scaled by 8/7, so the trailer is drawn 7.62 cells long, the same as before; the export takes whole cells only, so the picture is 5 x 9 (more empty margin) instead of 4.57 x 9.14. **Paint:** the walkway and hatches (saturation 0.15 to 0.18) sat on the default 0.16 threshold and came out speckled, so the threshold was lowered to 0.12 (the pale tank, 0.07, stays bare); the side stripe's hue (34 to 35 degrees) sat on the accent range's top, so it was widened to 37. 2.23 cells wide at 128 px a cell. Checked by eye on dark ground with both overlays fitted.

**Picks and reasons:** body attempt 1 of 3 (its axles sit about 0.7 cells behind the picture's middle, nearest the follow rule's 0.31; attempt 2's sat 1.35 behind); jacket 2 of 2 (heavier straps that survive at in-game size); reel 2 of 2 (squarer, more straight-down).

**Overlays** (`export_vehicle.py overlay --like PintleBowser.transform.json --fit ...`, boxes in master pixels): `PintleBowser_Lagging` over the tank's outline (292 306 604 1550), shown by the lagged tank node; `PintleBowser_SecondHose` across the front of the tank behind the headboard (353 318 543 500, agent placement: the rear holds the first reel and the pump box), shown by the second hose node. The jacket has its own orange edge, so a lagged water bowser shows orange there rather than its blue.

- Masters: `pintle-bowser-body-north-1` SHA-256 `9913eb43830a99172097ca3f92b61515dce5cacae43bf7d71ab94869b4be0676`; `pintle-bowser-lagged-tank-2` `deb58dd764168a0ca459bd0be462409234efddfd6e0f71b2bb6b9a209977517b`; `pintle-bowser-second-hose-2` `6180d5b34179d632a9bd67412ddccc5f7e5ca940aea02f1bcce98a110abadd36`
- `PintleBowser_Lagging_east.png`: SHA-256 `92b944c9115e9d07826f8eaa4dea30e798a1beff47484965a04bf91eea87cf93`
- `PintleBowser_Lagging_north.png`: SHA-256 `94f1b031627e1ccfa48487e5800a269be15271c3b72c104897a5f60d6e428116`
- `PintleBowser_Lagging_south.png`: SHA-256 `d013c04e59c97eceb6407e2414e3c8fe8cbac482505362581f129e249834a2d7`
- `PintleBowser_SecondHose_east.png`: SHA-256 `f472f3038309dd6343d54896792802ecf58fb4ee75a8dbf8c22e5dbed65710b3`
- `PintleBowser_SecondHose_north.png`: SHA-256 `545dd630f3b6999c29a810e3b3ad3130c51bd88cbbd57f356bbc78a410460dcd`
- `PintleBowser_SecondHose_south.png`: SHA-256 `99361b9cf7686eea9790aa6e8d4e5d48b684560f7e188875efd8ba0923822c2d`
- `PintleBowser_east.png`: SHA-256 `cb0dea8e99aa598fd931da1848a6714584fff378dcd67c521946036c9127055a`
- `PintleBowser_eastm.png`: SHA-256 `0ef736bb5795195329c126e0ffb0492a645f8e36ab6ea3f5a74582c30e5a4355`
- `PintleBowser_icon.png`: SHA-256 `a5c25ebeb4f1414eaefea23bdf37da7fd92597a8b9cb7db5866ec7dd49af67b6`
- `PintleBowser_north.png`: SHA-256 `506c0e5a1a06a45db3b7cefeaad1e6e89f3bfcdcb8b044e1481faaa143246fb2`
- `PintleBowser_northm.png`: SHA-256 `5ed9190dfb4a0b79cf46d919766f47b115335191906677b7d71342fbd7dfb283`
- `PintleBowser_south.png`: SHA-256 `1328e6f06af5508be844e9e94a0578ffb1b8a11336d5da84fa26955cf36123cb`
- `PintleBowser_southm.png`: SHA-256 `75f28e746093f2b327f6720ae1507df7a65aabfacb2b0a74632353558c233568`
