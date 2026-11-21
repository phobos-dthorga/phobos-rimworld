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

## Re-exported at 3 x 8 (13 Nov 2026, Vehicles 0.51.0)

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
| GeneratorSet_east.png | `6a4ed6492dc1384d365a5ad1b2bd5d6d70dbf26b163eb4596918c226417dab26` |
| GeneratorSet_eastm.png | `aba9f076486581f438aaf39f73ed140bd51894100b7c5debd80fb527b90cad6a` |
| GeneratorSet_icon.png | `6eed5311b6324e54f80a8f7e7f500b4fde6fa2b6a9a0774162a0d979f8ef8ddf` |
| GeneratorSet_north.png | `1a6f7452cefe4e4e95bb41c5d08f969b045691030b02ae719f701d66fe07367b` |
| GeneratorSet_northm.png | `79ed79034539fc8b71d4c29fae1f566113f7c99cd11b5cc989881f6ec13abc03` |
| GeneratorSet_south.png | `063beb7f485307944431460cc1c9abf744e8f39ff4fa549c9de5d8c7954aa7d4` |
| GeneratorSet_southm.png | `1ebd50ad30bc50c3d29f3e7db3dc4aa1eb98a88789b500e5537b752f01c427c2` |

## The drawn armour kits (19 Nov 2026)

The owner's picks of 19 Nov 2026, fitted by `assets/phobos-vehicles/export_armour_kits.py` on this body's transform, so they register with it. **The armoured cab and heavy cab plates are AI-generated by paid editing**: fal's Nano Banana Pro edit (Google's model, through fal.ai) of this truck's own cab, cut from the picked body (`assets/phobos-vehicles/draw_kit_crops.py`), one try each, US$0.15 a picture (`assets/phobos-vehicles/requests/truck-cab-kits-paid-record.md`); the kit is the changed area, made solid and laid back at the cut's place. **The ram bar and the body skirts are AI-generated on the local model** (FLUX.2 Klein 4B base with our private style model, night 7; `assets/phobos-vehicles/requests/truck-kit-parts.json`, ram bar 11, skirt 14), drawn alone and fitted by the script: the skirt repeated end to end down each side and mirrored for the right. The background AI looked at every original first. Placement of the ram bar and skirts is the agent's, to revise once seen in game. Only our own art was used.

- `GeneratorSet_ArmouredCab_north.png`: SHA-256 `b01e48704a40bbd012baa62c41f54d52bfce6bad3d08d7cacab1793875da040e`
- `GeneratorSet_ArmouredCab_east.png`: SHA-256 `3674e882eb0566b36ab2b9a0f95af5f01546f11e3595e4299d0ef70560e825e5`
- `GeneratorSet_ArmouredCab_south.png`: SHA-256 `22f0d637a2e61f61812f74c60cc351ab9fd2d04e11cd3789f8fb0ca21c359455`
- `GeneratorSet_HeavyCabPlates_north.png`: SHA-256 `52ddccd8799dae3742182bc45df6f8fd8792669fe054155e5f22e4e8ce50e858`
- `GeneratorSet_HeavyCabPlates_east.png`: SHA-256 `ceeebebcb713c170b8429ff72276f8b3a2fb0fa0791d587f3cc94fb7450864d7`
- `GeneratorSet_HeavyCabPlates_south.png`: SHA-256 `b3c451f0b871a7eab6204b762f3bde7d4c6964b80a7e74d3d86b2d6d4b8db870`
- `GeneratorSet_RamBar_north.png`: SHA-256 `e3ac15ad0e62906049cb22670ea16463c506b9571432fe21afeb7b10d8e8e40e`
- `GeneratorSet_RamBar_east.png`: SHA-256 `7beeb8eb37b4052fa4e9fff31655b81f64afd24f1d3f2ccffc91ebb23fe2ae2d`
- `GeneratorSet_RamBar_south.png`: SHA-256 `428d0c5f5b3ef2d3db8e94edae8e0d5145437daf39f3f24cbf70f1df0a7daa6e`

## Camp gear drawn on the vehicle (22 Nov 2026)

Shown while the gear is fitted (D'thorga Vehicles 0.103.0; the owner's "everything on ours", 22 Nov 2026). **AI-generated**: OpenAI gpt-image-2
through the image API, medium quality, each an edit of a crop of this vehicle's own tinted game picture at twice its size (paid rule 13; our
own art the only reference), two attempts each; lifted out with `scripts/art/sam_lift.py --cpu` (Segment Anything 2.1, Meta, Apache 2.0; the
roof tents with only what is darker than the cab kept, so no patch of the cab's paint comes with them), exported by
`scripts/art/export_vehicle.py overlay` at the picture's own scale. Where the lift failed twice on dark gear over dark metal, the picture was
made by script instead (paid rules 7 and 11). Request and review: `assets/phobos-vehicles/requests/camp-gear-overlays.json`,
`camp-gear-overlays-record.md`.

- `GeneratorSet_RoofTent` (genset-roof-tent): attempt 2 of 2, the owner's pick. Edit `camp-gear-genset-roof-tent-2`, SHA-256 `9fc0ab216012e549a43047889b48e8b2e55ebcc99de1ce3c8f666b6494df7cb4`; north `0b8d43f36e1612a7073bdae92572dc2eaf884f9d10f846bb78bfc83f6ef07425`
- `GeneratorSet_SideRoll` (genset-side-roll): made by script from the Billy's lifted roll (attempt 2), laid along this vehicle's left rail. Edit `camp-gear-genset-side-roll-1`, SHA-256 `7c77917a05779d89ac073bca37f7f3c6f5e43fba912a96ad09e34ef5bfd3a91e`; north `0ac1c04c01b10241966bffb73f8891a49b31fae5a1252a4a6f6a4146265a946e`

**The owner's review, 22 Nov 2026** (D'thorga Vehicles 0.104.0): the lifted roof tents had "issues and dithering" (parts scrubbed out); the
roof tent is now drawn alone on white and laid by script, so the sprite exporter gives clean edges; both shells kept, by roof size.

- `GeneratorSet_RoofTent`, again: the closed roof tent drawn alone (`roof-tent-closed-2`) and laid on the cab by script, the compact shell for a smaller roof; north `ef88d1e0d91feadfcb31e27c1146ac3f1538adb57ca1bb822c9e050efe5cfeb8`

**The owner's follow-up, 22 Nov 2026** (D'thorga Vehicles 0.105.0): the rolled bag on whichever side the camp is pitched, and the rear tent drawn.

- `GeneratorSet_SideRollRight`: the left roll mirrored across the vehicle's centre line by script, for a camp on the right; north `01cc6e070c4127f7d7cd7cf5bc34c06e3413478fe9f1890d7897f6cb4a5ffc96`
- `GeneratorSet_RearTentRoof`: a rear tent packed on the roof or tent mount: the same roll turned across it by script; north `c5f5e3382328470964c493981f0ddbe0ba32faea8342bdd5bb0c1a0c1bd9a8a0`

**Every rolled bag redrawn (22 Nov 2026, Vehicles 0.105.0):** the roll lifted from the Billy carried a fringe of the Billy's body along one edge, so the bag is now drawn alone on white (`awning-roll-bag-2`, OpenAI gpt-image-2, medium; references our awning roll and camp heater; exported by `export_sprite.py` after the background AI) and laid by script on the left rail, mirrored on the right, and across the roof for a packed rear tent. North pictures' SHA-256:

- `GeneratorSet_SideRoll`: dfd71127911a59ba16a997bf59ddca6911462de653befff6563c2ec396f5fe52
- `GeneratorSet_SideRollRight` (mirrored): d20f9ba3e5254efadcd6d87e303fcc66ada324b91a2301eadb691a7415227e78
- `GeneratorSet_RearTentRoof` (across the roof): 46cc457be01a7beaf7263df4c03194fe6720c4924400337514fe34a69e86f99d
