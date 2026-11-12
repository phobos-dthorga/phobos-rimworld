# Tipper textures: asset record

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from cards in the session. Each part was drawn alone on white with the picked body as its only reference (our own art, for style), placed unscaled on the body master's canvas, then moved and scaled into its place by `assets/phobos-vehicles/export_batch_b.py` and `scripts/art/export_vehicle.py`. Mirrored parts are left-right flips of the pick, new files that never replace it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art. The prompts are in `assets/phobos-vehicles/requests/` (batch-b-pilots.json, batch-b-family.json, seabed-crawler-overlays.json, survey-truck-overlays.json); the review, spend and picks are in [the night's record](../../../../../../assets/phobos-vehicles/requests/night-2026-11-12-record.md). Placements are the agent's, for the owner to revise once seen in game.

**Body:** `tipper-body-north-3.png`, SHA-256 `2243b95387beb7ea5f418c2f240554dd65d352e41a9e0765457155d9663ad00e`, exported by `scripts/art/export_vehicle.py body` with its colour masks (suffix m) and icon. East and south are the north turned by the script, so they never drift from it.

| Texture | Part master | Fit box in the body master | Master SHA-256 |
| --- | --- | --- | --- |

## Damage pictures (12 Nov 2026)

Made free from our own bulk tanker damage masters (`bulk-tanker-damage-body-1`, `bulk-tanker-damage-tank-1`, OpenAI gpt-image-2, 8 Nov 2026): a different crop for each place, fitted with `scripts/art/export_vehicle.py overlay --marks` (darkness only) and clipped to the vehicle's outline, by `assets/phobos-vehicles/export_damage.py`, which lists every crop and box. Shown by D'thorga Framework's damage looks below 60% part health. No game or other author's art was used.

| Texture | North SHA-256 |
| --- | --- |
| Tipper_DamageBody | `13b06e2882a93c0cce51ca6558944d18bb33e23b29fead8c4d8caa3ed9d46cc4` |
| Tipper_DamageCab | `8f9057778555caaa03d984dc85d820b3a08422d504d82f095acfb8aa122bf6e8` |

## Fitted pictures, round three (12 Nov 2026, evening)

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from a labelled sheet at in-game size. Each part was drawn alone on white with one of our own picked bodies as its only reference (for style), then placed by `assets/phobos-vehicles/export_round_three.py` and `scripts/art/export_vehicle.py overlay`. The winch is the pick turned half a turn so its hook faces forward, a new file that never replaces it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art (the check as fixed that evening; [the record](../../../../../../assets/phobos-vehicles/requests/round-three-record.md)). The prompts are in `assets/phobos-vehicles/requests/fittings-overlays.json` and `trailer-overlays.json`. Placements are the agent's, for the owner to revise once seen in game.

| Texture | Shown when | Part master | Master SHA-256 | North SHA-256 |
| --- | --- | --- | --- | --- |
| Tipper_Winch | the recovery winch is fitted | `fitting-recovery-winch-overlay-1-turned-180.png` | `12d0883ccab36557e61075786c3e331159fe8cf1d3222aaa09ee72b78b60eb7e` | `4bde10a456d345da9fec695ec3244da99d08a6805840aadca90b6c9887bd0206` |
| Tipper_AuxFuelTank | the auxiliary fuel tank is fitted | `fitting-aux-fuel-tank-overlay-2.png` | `efb2446cd2635534963dd8844e942263c4d93be8dc0ee45b6ddb668d413a0e2f` | `cb02c12778c46d5ff62bbcb2293c9d3e292001846232c8658e75d78f42e34da6` |
| Tipper_LoadingGrab | the loading grab upgrade is fitted | `tipper-loading-grab-3.png` | `d5357d1c901129d8cebd2cf0a8eca7ef525d8694361600eb6ad324cc047b6551` | `571db27cfb0df32f0a4f54d599f5ae9bb9c00799347bc01ab061276eaa08052c` |
