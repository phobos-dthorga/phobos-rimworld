# Bulk tanker textures: asset record

Made on 8 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, picked or accepted by the owner. Exported by `scripts/art/export_vehicle.py` from the masters below; prompts are in `assets/phobos-vehicles/requests/`, the masters and their records on the `art-masters` branch, and the review in `assets/phobos-vehicles/README.md`. No game or other author's art was uploaded or used as a reference.

The icons under `Upgrades/` are cut from the north overlays by script. The damage vessel overlay is the tank damage master fitted to the vessel's box.

| Texture | Master | Master SHA-256 |
| --- | --- | --- |
| BulkTanker | bulk-tanker-body-north-2 | `3c7b752f08adb7eeb84a1f161cdec1d8686765dd289322165fe54fa72c7c07ce` |
| LiquidTank | bulk-tanker-liquid-tank-1 | `790ff78afc91f33b2c4b9c17238655266d2a4ba102610352172042bc2d5fa807` |
| LiquidTank_B | bulk-tanker-liquid-tank-2 | `88b9b1305edf10b4126381fcd1111af3a3fc97147c4be95ff9d98e93c5e49298` |
| LiquidTank_C | bulk-tanker-liquid-tank-3 | `9e357e64199fcd73dc6a0a363035ec3fe6c8c9713b584dc6fae14e6436000a03` |
| PressureVessel | bulk-tanker-pressure-vessel-2 | `4eacfe72d228bc925efc50a3b8ee369502dc869485ba5413b508eee47d98eb53` |
| PressureVessel_B | bulk-tanker-pressure-vessel-1 | `28d93b3b27d591c9af3c9929fa611f037459fb8a627234f868580455706059dc` |
| PressureVessel_C | bulk-tanker-pressure-vessel-3 | `00d19878d9d9fc986c5a50594b93126b66f05ee451cd3559f19ada25e9a39283` |
| HoseReel | bulk-tanker-hose-reel-2 | `c74565ffc829b69eca4c877e8714299dfe46a5a246f32c2bbb8f3dcc3c19685a` |
| HoseOut | bulk-tanker-hoses-out-1 | `130aa11c43aed2eb4313c8f78839f9d411f9aa2c0862a2a4ede70cd5ce0e9610` |
| DamageTank | bulk-tanker-damage-tank-1 | `378db5a2215e37af2d6480e43cbb5c72cbcab7d41776b4f0f1b58d2204851632` |
| DamageVessel | bulk-tanker-damage-tank-1 | `378db5a2215e37af2d6480e43cbb5c72cbcab7d41776b4f0f1b58d2204851632` |
| DamageBody | bulk-tanker-damage-body-1 | `2a592c869523356118d30946e463b285b1ad3dd3367d8a1ce77aa09aba45ba7e` |

## Fitted pictures, round three (12 Nov 2026, evening)

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from a labelled sheet at in-game size. Each part was drawn alone on white with one of our own picked bodies as its only reference (for style), then placed by `assets/phobos-vehicles/export_round_three.py` and `scripts/art/export_vehicle.py overlay`. The winch is the pick turned half a turn so its hook faces forward, a new file that never replaces it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art (the check as fixed that evening; [the record](../../../../../../assets/phobos-vehicles/requests/round-three-record.md)). The prompts are in `assets/phobos-vehicles/requests/fittings-overlays.json` and `trailer-overlays.json`. Placements are the agent's, for the owner to revise once seen in game.

| Texture | Shown when | Part master | Master SHA-256 | North SHA-256 |
| --- | --- | --- | --- | --- |
| BulkTanker_Winch | the recovery winch is fitted | `fitting-recovery-winch-overlay-1-turned-180.png` | `12d0883ccab36557e61075786c3e331159fe8cf1d3222aaa09ee72b78b60eb7e` | `e096f58323713884ca6a2af9985f108c63597e9b82c2a898d561bdd1db571353` |
| BulkTanker_AuxFuelTank | the auxiliary fuel tank is fitted | `fitting-aux-fuel-tank-overlay-2.png` | `efb2446cd2635534963dd8844e942263c4d93be8dc0ee45b6ddb668d413a0e2f` | `b77b328070ea2db93dd4a56d6393bcd6eb8c52c6c8aa2efa0243239f739f45f0` |
