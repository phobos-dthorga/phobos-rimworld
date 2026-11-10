# EngineIdle.ogg

**Tipper: engine idling (blended by load, pitched by rpm).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-tipper-idle/p1-tipper-idle-32.flac (sha256 7913b00b6b3dbf2c5a1bd68eb20ec0d7cdf0ef680294b9b86d614ee49185d095) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 32, 8 steps, guidance 1.0 |
| Prompt | A heavy tipper dump truck with a rough, gravelly straight-six diesel engine and a rattling exhaust idling steadily, even and unchanging, no revving, continuous. Outdoors, a few metres away. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 5.8056], loop_match 0.205, levelled_by loudness, to -18.78 LUFS, gain_db -0.98 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +0 dB, through volume 13 |
| Result | -18.8 LUFS, true peak -1.5 dBTP, 5.36 s; flags: none |
| File | sha256 5ca3235be87e3743eedee05c84cb4fb3527b7fb30bb6371c45bb7a94bb36a22f, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
