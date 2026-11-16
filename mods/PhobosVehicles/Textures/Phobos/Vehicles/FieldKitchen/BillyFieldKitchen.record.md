# BillyFieldKitchen textures: asset record

**The Billy field kitchen's own picture since D'thorga Vehicles 0.59.0.** Made on 17 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 17 November 2026** (attempt 3 of 3, "kitchen box van", the owner's choice of look that day) from a sheet showing the three attempts beside the bowser. Until then Billy borrowed the bowsers' picture. Every reference uploaded was our own picked art: the Pintle bowser body (`pintle-bowser-body-north-1.png`, for the shared chassis and style) and the Busbar generator set body (`generator-set-body-north-3.png`, for how a closed, vented body is drawn from above). No game or other author's art was uploaded or used. The prompt is in `assets/phobos-vehicles/requests/billy-pilot.json`; the master goes to the `art-masters` branch, the attempts not picked to `art-references`.

**Spend: US$0.166 for 3 images** under a US$0.50 cap (the sitting's cap was US$1.50). Nothing refused by moderation.

**Sizing (rule of 9 Nov 2026):** the bowsers' chassis and canvas, 3 x 8 cells, drawSize (5, 9): requested at 896 x 1792, as the bowsers were (1.53 times their in-game drawn size). Exported by `scripts/art/export_vehicle.py body --cells-long 160/21 --draw 5 9` with the bowser's five `--hole` points (the drawbar frame's four openings and the towing eye, measured again on this master: 446 77, 424 198, 468 198, 413 254, 479 254), after the background AI's mask (`clean_backgrounds.py masks --gpu`). 2.34 cells wide at 128 px a cell.

**Paint (repaired, `scripts/art/repaint_vehicles.py`, after `paint_segments.py`):** the body takes the main paint, olive as drawn ((145, 141, 109) reproduces the master); the canvas shutters stay as drawn. Their saturation (0.16) sits just below the body's (0.23 to 0.26) and their hue within a few degrees of it, so they are kept apart by a saturation floor of 0.21, not by hue. `paint_check.py` finds **6 flecks**: five on the roof vents' corner bolt heads (a few pixels each, which the checker notes may be drawn in their own colour) and one on the right mudguard; 13 at a floor of 0.18, 7 at 0.20, 9 at 0.17; the regions and parts methods gave 22 and 17. No overlays: Billy's upgrades are set-out pieces, not body parts.

- Master: `billy-field-kitchen-body-north-3` SHA-256 `1984aa14290c46f9c53b3a0dd8507c2f42711aa141f1573cd1bb0a5b071a1c1b`
- `BillyFieldKitchen_east.png`: SHA-256 `d67e2c8f9bfe57cf96d92aecf438d048dd47afefbbe721c42b2f8e89e07569a2`
- `BillyFieldKitchen_eastm.png`: SHA-256 `7fc48c63944a9216af05b04ce5e5c28a1d750e16c27a154be7ffc53fb2fc5308`
- `BillyFieldKitchen_icon.png`: SHA-256 `d4d0826c41daeb73368368425827d9c02889a8b4b049000d07237c4acc35a1a6`
- `BillyFieldKitchen_north.png`: SHA-256 `6176362b61659b2c1570cc78ac81f10ec2da5d72bde75ce6f0fbc807805868e6`
- `BillyFieldKitchen_northm.png`: SHA-256 `854531814bb44714a5cfef582a1f4dc69c595dc210b2b77602782a5618847e32`
- `BillyFieldKitchen_south.png`: SHA-256 `56f805ddeafcd4facabb7fc1fcb5fa0b6fcfcfd380f07206e9e14865479b0cb8`
- `BillyFieldKitchen_southm.png`: SHA-256 `c647b48bfe4203cd7c7448f565596314121d1ae0ad5d6a9e43aee563a4d1e71e`
