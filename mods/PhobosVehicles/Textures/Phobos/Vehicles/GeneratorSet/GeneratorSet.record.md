# GeneratorSet textures: asset record

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from cards in the session. Each part was drawn alone on white with the picked body as its only reference (our own art, for style), placed unscaled on the body master's canvas, then moved and scaled into its place by `assets/phobos-vehicles/export_batch_b.py` and `scripts/art/export_vehicle.py`. Mirrored parts are left-right flips of the pick, new files that never replace it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art. The prompts are in `assets/phobos-vehicles/requests/` (batch-b-pilots.json, batch-b-family.json, seabed-crawler-overlays.json, survey-truck-overlays.json); the review, spend and picks are in [the night's record](../../../../../../assets/phobos-vehicles/requests/night-2026-11-12-record.md). Placements are the agent's, for the owner to revise once seen in game.

**Body:** `generator-set-body-north-3.png`, SHA-256 `d5002f5b6a9aa11cbb3a5699e751fa991190b76e2f7923fdf3e5ecdce0eb8dcb`, exported by `scripts/art/export_vehicle.py body` with its colour masks (suffix m) and icon. East and south are the north turned by the script, so they never drift from it.

| Texture | Part master | Fit box in the body master | Master SHA-256 |
| --- | --- | --- | --- |

## Damage pictures (12 Nov 2026)

Made free from our own bulk tanker damage masters (`bulk-tanker-damage-body-1`, `bulk-tanker-damage-tank-1`, OpenAI gpt-image-2, 8 Nov 2026): a different crop for each place, fitted with `scripts/art/export_vehicle.py overlay --marks` (darkness only) and clipped to the vehicle's outline, by `assets/phobos-vehicles/export_damage.py`, which lists every crop and box. Shown by D'thorga Framework's damage looks below 60% part health. No game or other author's art was used.

| Texture | North SHA-256 |
| --- | --- |
| GeneratorSet_DamageCab | `849496980bcafe8f913a727e426020299a57ae69ce6b0f4c3f5860c73a0d492e` |
| GeneratorSet_DamageHousing | `b6063996cbbd7401275e52cad11cd9a954db258350a7e38044c1931d0ec2fc92` |
