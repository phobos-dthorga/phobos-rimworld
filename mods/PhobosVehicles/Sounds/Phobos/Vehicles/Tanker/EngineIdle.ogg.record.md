# EngineIdle.ogg

**Tanker: engine idling (blended by load, pitched by rpm).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-tanker-idle/p1-tanker-idle-33.flac (sha256 d9129bc3d4c72f268fd4188e63d730a84a7753736d9b3e2eddd2ef4f42f85581) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 33, 8 steps, guidance 1.0 |
| Prompt | A large bulk tanker truck with a big turbocharged six-cylinder diesel engine, deep and heavy idling steadily, even and unchanging, no revving, continuous. Outdoors, a few metres away. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.8, 5.1761], loop_match 0.19, loop_crossfade_s 0.25, loop_curve equal power, loop_seam_dip_db 0.0, levelled_by loudness, to -18.94 LUFS, gain_db -3.04 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +4 dB, through volume 21 |
| Result | -19.0 LUFS, true peak -4.6 dBTP, 4.13 s; flags: none |
| File | sha256 b68d7aa414e4f6c53275e1ff0f44917e5d11fc97e333060896d69cb97b8e49e2, made 2026-11-13 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
