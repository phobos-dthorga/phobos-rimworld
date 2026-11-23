# SetUp.ogg

**Drill rig: outriggers down, mast raising (S47).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p2-drillrig-setup/p2-drillrig-setup-42.flac (sha256 63e66a1f93738831997353e12051efadf0b01c924b286a696d328838acb8ca63) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 42, 8 steps, guidance 1.0 |
| Prompt | A heavy drill rig truck setting up: hydraulic outriggers stomping down onto the ground one by one, then a tall steel mast raised slowly by whining hydraulic rams, creaking into place. Outdoors. Length: 8 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; filter lowpass=f=9000,lowpass=f=9000,lowpass=f=9000,lowpass=f=9000, levelled_by loudness, to -21.78 LUFS, gain_db 3.5 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) -3 dB, through volume 13 |
| Result | -22.7 LUFS, true peak -0.9 dBTP, 8.68 s; flags: spiky |
| File | sha256 25e5f6d52707fc9021a34bb21b76d24ad66f6a98644dea3e377c8152e5896d0f, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
