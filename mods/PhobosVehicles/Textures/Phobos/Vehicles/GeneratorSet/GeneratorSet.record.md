# GeneratorSet textures: asset record

Made on 12 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated, picked by the owner on 12 November 2026** from cards in the session. Each part was drawn alone on white with the picked body as its only reference (our own art, for style), placed unscaled on the body master's canvas, then moved and scaled into its place by `assets/phobos-vehicles/export_batch_b.py` and `scripts/art/export_vehicle.py`. Mirrored parts are left-right flips of the pick, new files that never replace it. No game or other author's art was uploaded or used as a reference, and each pick passed the near-copy check against the game's art. The prompts are in `assets/phobos-vehicles/requests/` (batch-b-pilots.json, batch-b-family.json, seabed-crawler-overlays.json, survey-truck-overlays.json); the review, spend and picks are in [the night's record](../../../../../../assets/phobos-vehicles/requests/night-2026-11-12-record.md). Placements are the agent's, for the owner to revise once seen in game.

**Body:** `generator-set-body-north-3.png`, SHA-256 `d5002f5b6a9aa11cbb3a5699e751fa991190b76e2f7923fdf3e5ecdce0eb8dcb`, exported by `scripts/art/export_vehicle.py body` with its colour masks (suffix m) and icon. East and south are the north turned by the script, so they never drift from it.

| Texture | Part master | Fit box in the body master | Master SHA-256 |
| --- | --- | --- | --- |

## Damage pictures (12 Nov 2026)

Made free from our own bulk tanker damage masters (`bulk-tanker-damage-body-1`, `bulk-tanker-damage-tank-1`, OpenAI gpt-image-2, 8 Nov 2026): a different crop for each place, fitted with `scripts/art/export_vehicle.py overlay --marks` (darkness only) and clipped to the vehicle's outline, by `assets/phobos-vehicles/export_damage.py`, which lists every crop and box. Shown by D'thorga Framework's damage looks below 60% part health. No game or other author's art was used.

| Texture | North SHA-256 |
| --- | --- |
| GeneratorSet_DamageCab | `59873bf4c92163096119f34fde254817564139437c476f9fa5863f6f6312bab0` |
| GeneratorSet_DamageHousing | `9ee28548d3ba41cec5017cc488625315984df742b3580ec124e9ee0760dd763b` |

## Re-exported at 3 x 8 (14 Nov 2026, Vehicles 0.51.0)

The owner had the Busbar brought in line with the Talus tipper: footprint 3 x 8, drawn 4 x 9 (was 2 x 6, drawn 3 x 7). Every texture here
was exported again from the same originals, with no new generation: the body by `scripts/art/export_vehicle.py body --cells-long 8 --draw
4 9` (scale 0.72 of the original, down, never up; the same paint settings, the background clean-up's saved answer for the original), the
overlays by `assets/phobos-vehicles/export_batch_b.py --only GeneratorSet` and `assets/phobos-vehicles/export_damage.py` from the new
transform. The originals are unchanged. SHA-256 of every texture now shipped:

| File | SHA-256 |
| --- | --- |
| GeneratorSet_Battery_east.png | `1eb6bd2f79c527b5e20e894423a48b3014a86e47276340457af586093faf04db` |
| GeneratorSet_Battery_north.png | `c4f9ea955b12a3152dd36b07612f900bb33e8a6b3ed0bc97970e93cc2895c503` |
| GeneratorSet_Battery_south.png | `47d334407b2d086df45734e3ff1eaf2b4510fef7779e38bfd6deefbe1683dfef` |
| GeneratorSet_BiggerGenerator_east.png | `071c6a6a3019517d55e79de5ddf407b0ff67c7de30e97a505117055b2638f84a` |
| GeneratorSet_BiggerGenerator_north.png | `214da0c34c081d592c07079a67e09c1a2659591051eb702d433307ce5afa30c9` |
| GeneratorSet_BiggerGenerator_south.png | `5f1d99213e58c495bb5423b5e4f068f6eac59eefabe64dd0364989c8a679ae55` |
| GeneratorSet_DamageCab_east.png | `7832fe7bbcef5df2bb70df90702783116a64a9ec02d4d95a2cd69a9b648bbf3f` |
| GeneratorSet_DamageCab_north.png | `59873bf4c92163096119f34fde254817564139437c476f9fa5863f6f6312bab0` |
| GeneratorSet_DamageCab_south.png | `dba42425cbf4535b9d4058a743ff19f66ea263ca6c51d73ecd293526612bdb26` |
| GeneratorSet_DamageHousing_east.png | `731ebb1c7748d8be2581a2d75ab049aa979e650015f06cb555f6ec19a6576adb` |
| GeneratorSet_DamageHousing_north.png | `9ee28548d3ba41cec5017cc488625315984df742b3580ec124e9ee0760dd763b` |
| GeneratorSet_DamageHousing_south.png | `441d71458c17a19777ab3ae3b3db62b0a3f2ad1f723d543c34da777cd21035c5` |
| GeneratorSet_FanHeavy_east.png | `5378a44eb6e3fca5881781e89195df4661e4a44f460130eff4e62b9b5f6fc7fd` |
| GeneratorSet_FanHeavy_north.png | `c2a33a89f39adc435431bfb478762217a5eb38e6da73ecca117ea6ce8c314992` |
| GeneratorSet_FanHeavy_south.png | `48a39ddc845fa2636122b7bc8be5bbb86c434d6fda55df606d11fec56d2a684e` |
| GeneratorSet_Fan_east.png | `8eb7daf8aeda65790524ae3ab5a32d2d2ddfe8080fc8afa4c993ba16e84b5c69` |
| GeneratorSet_Fan_north.png | `e8fe88e5829c2f57adf2df507a8e77a0e3c2b7d6c3ac9c25519d1df96c2b700a` |
| GeneratorSet_Fan_south.png | `b8d51fa5ae8eb2b14a8e14140621430487843f144423cf1d7341bd03a39dfc32` |
| GeneratorSet_HeatExchanger_east.png | `562054ed3edd383fec86a853b0bb94b014ca2adf2cbe69a971e445e736081a51` |
| GeneratorSet_HeatExchanger_north.png | `6b57da4b6f40658f72c7ffb7dc8ce91b7c3f907c56889098ec9dbf599e65d948` |
| GeneratorSet_HeatExchanger_south.png | `845405e6981fbf52e560270081500080bd59157b53179af5fe63313fc2e285d5` |
| GeneratorSet_Reel_east.png | `e8fe5085dec488764287d763ec4e58a87302d6a82dde31dfa08384ea540399f8` |
| GeneratorSet_Reel_north.png | `adc519c0c6e5ddb62cfbcfa977d57bb1aa44e32c8ce355bee3260bd36c3ed929` |
| GeneratorSet_Reel_south.png | `59947b1ca02409b09a9fba6b6ca727ee4c0bd733f1977ac8bcfbfebc8080520b` |
| GeneratorSet_SetUp_east.png | `82d496a96e70ecbec007fa56804cca36ed75a5757636b3edc7323e2cebd23046` |
| GeneratorSet_SetUp_north.png | `9764459b18159c0dbdf4134aff62715803ded6340661ac5989d1d7fb92fe01d2` |
| GeneratorSet_SetUp_south.png | `c93e8a6e340d841312e349a1723425ca9c7985111df8c0ded8ae234bb50416d5` |
| GeneratorSet_east.png | `cb253070b4f89bc3aefe59c658c0e4b0a5e5b713ba8d17656926c684efac8e7f` |
| GeneratorSet_eastm.png | `6f46a88c055224d7e32de7d88d4c248bbf0b6684fa338cb2c33f2fa1a58cfa9f` |
| GeneratorSet_icon.png | `6eed5311b6324e54f80a8f7e7f500b4fde6fa2b6a9a0774162a0d979f8ef8ddf` |
| GeneratorSet_north.png | `94dac4f0bab336c4b7166774cd23a35764a0aae97f466359c5c4f695cd6b1d9b` |
| GeneratorSet_northm.png | `0e6dae39f3c182605edf663fc78392f956c10739d8152df04429e4bd9cdc3c1a` |
| GeneratorSet_south.png | `57bb372b5269247b992667cae17a99b088d97320fcb9660cd7ecd592db6fac63` |
| GeneratorSet_southm.png | `bb6e1dc80c0ee158b27632cff60ec2c3ae4647a1ad2d7b8147c445dc22a5ea06` |
