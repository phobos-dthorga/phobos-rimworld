# BeltSnap.ogg

**A drive belt squealing and snapping under the bonnet (S111, Framework 0.270.0).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-23 |
| Master | assets/phobos-sounds/generated/n12-belt-squeal/n12-belt-squeal-122.flac (sha256 4eb2e6d291746aad130e2dbd360e1de7ac897945631c33fa6aa2e66fbcf373a7) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 122, 8 steps, guidance 1.0 |
| Prompt | A worn rubber drive belt on an engine slipping: a sharp rising squeal for two seconds, then a dull slap as the belt snaps and flaps loose, and the squeal stops. Under the bonnet, a few metres away. Not loud, not alarming. Length: 3 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -26.73 LUFS, gain_db -7.53 |
| Levelled against | the game's Interact_Repair_Start (effective -42.9 LUFS) -3 dB, through volume 11 |
| Result | -27.0 LUFS, true peak -10.8 dBTP, 0.86 s; flags: highs |
| File | sha256 547b10d41e042cc70f503582cdcb83b908112d9d0d24aa1fd177a673013797f3, made 2026-11-23 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
