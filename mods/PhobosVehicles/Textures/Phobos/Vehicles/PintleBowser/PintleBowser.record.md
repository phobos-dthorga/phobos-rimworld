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
- `PintleBowser_east.png`: SHA-256 `580a258441c30da12bbe3b1d6b3057c8d0a4919dce72b46b54ea2b8a59571145`
- `PintleBowser_eastm.png`: SHA-256 `9653c327d39c6c5ccbdc2238fc864c158bc2d1b4f73e2e5b1419091303288bbe`
- `PintleBowser_icon.png`: SHA-256 `a5c25ebeb4f1414eaefea23bdf37da7fd92597a8b9cb7db5866ec7dd49af67b6`
- `PintleBowser_north.png`: SHA-256 `3197ad41833eeec1edd24cb41e2c2b82f38595251e029832f91d7f55378802d8`
- `PintleBowser_northm.png`: SHA-256 `02bc27592b33780ab1dc077a0d554734bcc8b09362aa846717586897cb12c1e9`
- `PintleBowser_south.png`: SHA-256 `8ae50304f7b276d7e1ff2dfb6cc55178ee241c0c8dc02fcb11e2bd32d9661803`
- `PintleBowser_southm.png`: SHA-256 `c6e2a1db699e9450fee9725e844f430645a2374eea1f7eace12baed19898f76c`

## Camp gear drawn on the vehicle (22 Nov 2026)

Shown while the gear is fitted (D'thorga Vehicles 0.103.0; the owner's "everything on ours", 22 Nov 2026). **AI-generated**: OpenAI gpt-image-2
through the image API, medium quality, each an edit of a crop of this vehicle's own tinted game picture at twice its size (paid rule 13; our
own art the only reference), two attempts each; lifted out with `scripts/art/sam_lift.py --cpu` (Segment Anything 2.1, Meta, Apache 2.0; the
roof tents with only what is darker than the cab kept, so no patch of the cab's paint comes with them), exported by
`scripts/art/export_vehicle.py overlay` at the picture's own scale. Where the lift failed twice on dark gear over dark metal, the picture was
made by script instead (paid rules 7 and 11). Request and review: `assets/phobos-vehicles/requests/camp-gear-overlays.json`,
`camp-gear-overlays-record.md`.

- `PintleBowser_SideRoll` (bowser-side-roll): made by script from the Billy's lifted roll (attempt 2), laid along this vehicle's left rail. Edit `camp-gear-bowser-side-roll-1`, SHA-256 `e3ae8b05fbeafc6c514b57527ef91f0d37277e3e922bd62a65766c53a9c400a4`; north `aa1b0a39ab5b0bd1412d9eb1536a5bceda90ef957c4c56c816acfa24fcfa7169`
