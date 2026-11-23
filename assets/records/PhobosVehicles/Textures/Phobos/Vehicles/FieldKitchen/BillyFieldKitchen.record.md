# BillyFieldKitchen textures: asset record

**The Billy field kitchen's own picture since D'thorga Vehicles 0.59.0.** Made on 17 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 17 November 2026** (attempt 3 of 3, "kitchen box van", the owner's choice of look that day) from a sheet showing the three attempts beside the bowser. Until then Billy borrowed the bowsers' picture. Every reference uploaded was our own picked art: the Pintle bowser body (`pintle-bowser-body-north-1.png`, for the shared chassis and style) and the Busbar generator set body (`generator-set-body-north-3.png`, for how a closed, vented body is drawn from above). No game or other author's art was uploaded or used. The prompt is in `assets/phobos-vehicles/requests/billy-pilot.json`; the master goes to the `art-masters` branch, the attempts not picked to `art-references`.

**Spend: US$0.166 for 3 images** under a US$0.50 cap (the sitting's cap was US$1.50). Nothing refused by moderation.

**Sizing (rule of 9 Nov 2026):** the bowsers' chassis and canvas, 3 x 8 cells, drawSize (5, 9): requested at 896 x 1792, as the bowsers were (1.53 times their in-game drawn size). Exported by `scripts/art/export_vehicle.py body --cells-long 160/21 --draw 5 9` with the bowser's five `--hole` points (the drawbar frame's four openings and the towing eye, measured again on this master: 446 77, 424 198, 468 198, 413 254, 479 254), after the background AI's mask (`clean_backgrounds.py masks --gpu`). 2.34 cells wide at 128 px a cell.

**Paint (repaired, `scripts/art/repaint_vehicles.py`, after `paint_segments.py`):** the body takes the main paint, olive as drawn ((145, 141, 109) reproduces the master); the canvas shutters stay as drawn. Their saturation (0.16) sits just below the body's (0.23 to 0.26) and their hue within a few degrees of it, so they are kept apart by a saturation floor of 0.21, not by hue. `paint_check.py` finds **6 flecks**: five on the roof vents' corner bolt heads (a few pixels each, which the checker notes may be drawn in their own colour) and one on the right mudguard; 13 at a floor of 0.18, 7 at 0.20, 9 at 0.17; the regions and parts methods gave 22 and 17. No overlays: Billy's upgrades are set-out pieces, not body parts.

- Master: `billy-field-kitchen-body-north-3` SHA-256 `1984aa14290c46f9c53b3a0dd8507c2f42711aa141f1573cd1bb0a5b071a1c1b`
- `BillyFieldKitchen_east.png`: SHA-256 `27ff3437007dd8acac7840947d7ecfeaa9f6c50ac3be02e048dd377c6bdc48ba`
- `BillyFieldKitchen_eastm.png`: SHA-256 `7fc48c63944a9216af05b04ce5e5c28a1d750e16c27a154be7ffc53fb2fc5308`
- `BillyFieldKitchen_icon.png`: SHA-256 `d4d0826c41daeb73368368425827d9c02889a8b4b049000d07237c4acc35a1a6`
- `BillyFieldKitchen_north.png`: SHA-256 `ec94355f92aa85681a11b6df5e9f2b852b6e642c2bd7713732cbe17e0c7d7074`
- `BillyFieldKitchen_northm.png`: SHA-256 `854531814bb44714a5cfef582a1f4dc69c595dc210b2b77602782a5618847e32`
- `BillyFieldKitchen_south.png`: SHA-256 `6aa3217fdcdeeaf248adb9c65fad8474ad9442e7b480f2eb022806d030e190ab`
- `BillyFieldKitchen_southm.png`: SHA-256 `c647b48bfe4203cd7c7448f565596314121d1ae0ad5d6a9e43aee563a4d1e71e`

## Camp gear drawn on the vehicle (22 Nov 2026)

Shown while the gear is fitted (D'thorga Vehicles 0.103.0; the owner's "everything on ours", 22 Nov 2026). **AI-generated**: OpenAI gpt-image-2
through the image API, medium quality, each an edit of a crop of this vehicle's own tinted game picture at twice its size (paid rule 13; our
own art the only reference), two attempts each; lifted out with `scripts/art/sam_lift.py --cpu` (Segment Anything 2.1, Meta, Apache 2.0; the
roof tents with only what is darker than the cab kept, so no patch of the cab's paint comes with them), exported by
`scripts/art/export_vehicle.py overlay` at the picture's own scale. Where the lift failed twice on dark gear over dark metal, the picture was
made by script instead (paid rules 7 and 11). Request and review: `assets/phobos-vehicles/requests/camp-gear-overlays.json`,
`camp-gear-overlays-record.md`.

- `BillyFieldKitchen_SideRoll` (billy-side-roll): made by script from the Billy's lifted roll (attempt 2), laid along this vehicle's left rail. Edit `camp-gear-billy-side-roll-1`, SHA-256 `d7a3d8a20ede7594ee18b613f8f1fb9801ff69553b4687fd3335cc2cc64d2a28`; north `10d6989bbe81634a5dd09cbf2d00896ca0b5dac25267b91fc1b08b1d3abb899b`

**The owner's follow-up, 22 Nov 2026** (D'thorga Vehicles 0.105.0): the rolled bag on whichever side the camp is pitched, and the rear tent drawn.

- `BillyFieldKitchen_SideRollRight`: the left roll mirrored across the vehicle's centre line by script, for a camp on the right; north `a1bb36bb9ed4c266090f7ff4df97765760349e329509b5b5aff0a4b880d5af7b`

**Every rolled bag redrawn (22 Nov 2026, Vehicles 0.105.0):** the roll lifted from the Billy carried a fringe of the Billy's body along one edge, so the bag is now drawn alone on white (`awning-roll-bag-2`, OpenAI gpt-image-2, medium; references our awning roll and camp heater; exported by `export_sprite.py` after the background AI) and laid by script on the left rail, mirrored on the right, and across the roof for a packed rear tent. North pictures' SHA-256:

- `BillyFieldKitchen_SideRoll`: 7210bbd64a84811b652a5bd59b16ef4ff781f4dabd945d3219cd535d6ad88927
- `BillyFieldKitchen_SideRollRight` (mirrored): d2ae623b9ff2216f3b68303f0560911c4ecd2be721fe4ed2113e7709ba9ced3c

## Own colours, no paint (23 Nov 2026)

**Facings exported again in their own colours from the same master on the same transform, and the paint masks removed**, D'thorga
Vehicles 0.106.3 (the owner's choice of 23 Nov 2026: repainted, the body stayed patchy after the trucks' fixes, so it is not offered
paint). `python scripts/art/repaint_vehicles.py stage --paint repaired` (this body is in its `UNPAINTED` list), then `apply`; the Def's
shader is plain Cutout. The drawing, its outline and every overlay's fit are unchanged. No game or other author's art was used.

| Texture | SHA-256 |
| --- | --- |
| BillyFieldKitchen_north | `ec94355f92aa85681a11b6df5e9f2b852b6e642c2bd7713732cbe17e0c7d7074` |
| BillyFieldKitchen_east | `27ff3437007dd8acac7840947d7ecfeaa9f6c50ac3be02e048dd377c6bdc48ba` |
| BillyFieldKitchen_south | `6aa3217fdcdeeaf248adb9c65fad8474ad9442e7b480f2eb022806d030e190ab` |
