# ClutchSlip.ogg

**A worn clutch slipping as the vehicle pulls away (S112, Framework 0.270.0).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-23 |
| Master | assets/phobos-sounds/generated/n12-clutch-slip/n12-clutch-slip-121.flac (sha256 617be781c8e3587262482b36d95cb1e27ec4fc1d0f10ef6fba870a53e821521f) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 121, 8 steps, guidance 1.0 |
| Prompt | A heavy truck's clutch slipping as it pulls away under load: the engine note flaring up while a dry friction grind and faint squeal rise, then catching and settling. Not loud, not alarming. Length: 3 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -26.73 LUFS, gain_db -8.93 |
| Levelled against | the game's Interact_Repair_Start (effective -42.9 LUFS) -3 dB, through volume 11 |
| Result | -26.7 LUFS, true peak -13.5 dBTP, 1.16 s; flags: whine |
| File | sha256 166e8cb33d5df1ba866bc6e6c880e9300cc005cc0f27b3e0d7abd80ce14ad6ba, made 2026-11-23 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
