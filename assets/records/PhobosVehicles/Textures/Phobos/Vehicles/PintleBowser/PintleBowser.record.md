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
- `PintleBowser_east.png`: SHA-256 `848142918cdd51358484585b86a7fd60014f1efe5c590d1fb6f2a051ea29427c`
- `PintleBowser_eastm.png`: SHA-256 `4cd60e8f333562e728d87fd2e6354e8462030f8f6ca2c2b631385bdeaf3e9089`
- `PintleBowser_icon.png`: SHA-256 `a5c25ebeb4f1414eaefea23bdf37da7fd92597a8b9cb7db5866ec7dd49af67b6`
- `PintleBowser_north.png`: SHA-256 `c47174be8716b6945eb6e4891fdf49e22d7987d0730536992381314517dc7e44`
- `PintleBowser_northm.png`: SHA-256 `a6e152d1e6b285693913f94fb3c0381128a40c25f5aada2d80ffff669c074eed`
- `PintleBowser_south.png`: SHA-256 `ba8c9c7f2ab97bbdb7734f8ee767373cd2eed86ba0dd55ac965c5fe5b827ba1e`
- `PintleBowser_southm.png`: SHA-256 `e692886bf2cadb6a191bab1bc8c4d57a28dd15f569dae652d2b8d2a022afa308`

## Camp gear drawn on the vehicle (22 Nov 2026)

Shown while the gear is fitted (D'thorga Vehicles 0.103.0; the owner's "everything on ours", 22 Nov 2026). **AI-generated**: OpenAI gpt-image-2
through the image API, medium quality, each an edit of a crop of this vehicle's own tinted game picture at twice its size (paid rule 13; our
own art the only reference), two attempts each; lifted out with `scripts/art/sam_lift.py --cpu` (Segment Anything 2.1, Meta, Apache 2.0; the
roof tents with only what is darker than the cab kept, so no patch of the cab's paint comes with them), exported by
`scripts/art/export_vehicle.py overlay` at the picture's own scale. Where the lift failed twice on dark gear over dark metal, the picture was
made by script instead (paid rules 7 and 11). Request and review: `assets/phobos-vehicles/requests/camp-gear-overlays.json`,
`camp-gear-overlays-record.md`.

- `PintleBowser_SideRoll` (bowser-side-roll): made by script from the Billy's lifted roll (attempt 2), laid along this vehicle's left rail. Edit `camp-gear-bowser-side-roll-1`, SHA-256 `e3ae8b05fbeafc6c514b57527ef91f0d37277e3e922bd62a65766c53a9c400a4`; north `aa1b0a39ab5b0bd1412d9eb1536a5bceda90ef957c4c56c816acfa24fcfa7169`

**The owner's follow-up, 22 Nov 2026** (D'thorga Vehicles 0.105.0): the rolled bag on whichever side the camp is pitched, and the rear tent drawn.

- `PintleBowser_SideRollRight`: the left roll mirrored across the vehicle's centre line by script, for a camp on the right; north `50597ca07eb533f4b30f892678e66a85eca38741e3d3838c42097391e18e702d`

**Every rolled bag redrawn (22 Nov 2026, Vehicles 0.105.0):** the roll lifted from the Billy carried a fringe of the Billy's body along one edge, so the bag is now drawn alone on white (`awning-roll-bag-2`, OpenAI gpt-image-2, medium; references our awning roll and camp heater; exported by `export_sprite.py` after the background AI) and laid by script on the left rail, mirrored on the right, and across the roof for a packed rear tent. North pictures' SHA-256:

- `PintleBowser_SideRoll`: a16fcdcb1cb5b14723e9ebf45f60691d1bc1bd29ea75cdc84ad2a83f9f01ea7f
- `PintleBowser_SideRollRight` (mirrored): f1ca1e0e1c326c60acfe8b089599d07e0ce9bc8decd0b67cdf395fe97dea326c

## Paint made again (23 Nov 2026)

**Facings and paint masks exported again from the same master on the same transform**, D'thorga Vehicles 0.106.2 (the owner's review
of 23 Nov 2026: painted red, the tank's sides showed red dashes). `python scripts/art/repaint_vehicles.py stage --paint repaired`, then
`apply`, with this body's own settings there: its stripe's hues widened to 8-42 and no main paint above saturation 0.35 (the stripe is
drawn a strong orange beside the dull olive body), and `settle_faint`, which drops faint paint (within 0.03 of the floor) inside a
Segment Anything part carrying under 12 % of it, the tank's warm shaded edge. Also the paint's own lines joining the paint and the
game-size fleck mending of 0.106.1 (0 left). The drawing, its outline and every overlay's fit are unchanged. No game or other author's
art was used.

| Texture | SHA-256 |
| --- | --- |
| PintleBowser_north | `c47174be8716b6945eb6e4891fdf49e22d7987d0730536992381314517dc7e44` |
| PintleBowser_northm | `a6e152d1e6b285693913f94fb3c0381128a40c25f5aada2d80ffff669c074eed` |
| PintleBowser_east | `848142918cdd51358484585b86a7fd60014f1efe5c590d1fb6f2a051ea29427c` |
| PintleBowser_eastm | `4cd60e8f333562e728d87fd2e6354e8462030f8f6ca2c2b631385bdeaf3e9089` |
| PintleBowser_south | `ba8c9c7f2ab97bbdb7734f8ee767373cd2eed86ba0dd55ac965c5fe5b827ba1e` |
| PintleBowser_southm | `e692886bf2cadb6a191bab1bc8c4d57a28dd15f569dae652d2b8d2a022afa308` |
