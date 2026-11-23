# EngineIdle.ogg

**Tipper: engine idling (blended by load, pitched by rpm).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-tipper-idle/p1-tipper-idle-32.flac (sha256 7913b00b6b3dbf2c5a1bd68eb20ec0d7cdf0ef680294b9b86d614ee49185d095) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 32, 8 steps, guidance 1.0 |
| Prompt | A heavy tipper dump truck with a rough, gravelly straight-six diesel engine and a rattling exhaust idling steadily, even and unchanging, no revving, continuous. Outdoors, a few metres away. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 5.5344], loop_match 0.162, loop_crossfade_s 0.5, loop_curve equal power, loop_seam_dip_db 0.65, levelled_by loudness, to -18.94 LUFS, gain_db -1.54 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +4 dB, through volume 21 |
| Result | -19.1 LUFS, true peak -1.8 dBTP, 4.83 s; flags: none |
| File | sha256 f1e6f8a1768730edd4ad19c3889a4a0c893bc0bfb790c611983f978f33e14e90, made 2026-11-13 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
