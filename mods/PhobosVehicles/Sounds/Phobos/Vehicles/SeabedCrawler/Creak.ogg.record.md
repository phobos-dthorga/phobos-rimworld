# Creak.ogg

**Seabed Crawler: hull creaking while submerged (S76).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p2-crawler-creak-steady/p2-crawler-creak-steady-41.flac (sha256 0f18341e11cb309cfd7667e1b0ebb73e6879cd3873d860d7708c766d0db529c5) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 41, 8 steps, guidance 1.0 |
| Prompt | Inside a small steel submarine deep under water: a constant faint low rumble of the sea against the hull, with quiet distant creaks and soft groans of metal under pressure over it, calm, never silent, continuous. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.5, 5.5614], loop_match 0.689, loop_crossfade_s 0.25, loop_curve equal power, loop_seam_dip_db 0.0, filter dynaudnorm=f=200:g=11:p=0.9:m=60, levelled_by loudness, to -37.08 LUFS, gain_db -16.08 |
| Levelled against | the game's Hemopump_Ambience (effective -37.1 LUFS) -6 dB, through volume 50 |
| Result | -37.0 LUFS, true peak -22.7 dBTP, 4.81 s; flags: none |
| File | sha256 5f426e2fabd6e88218b74bd2a046203009984d76adb5a7649bcd7c748cd6dd39, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
