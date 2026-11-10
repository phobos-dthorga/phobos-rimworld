# EngineIdle.ogg

**SurveyTruck: engine idling (blended by load, pitched by rpm).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-survey-idle/p1-survey-idle-31.flac (sha256 b11ffa064401a351ddba7a19e81d982550f68d7a678fbbd8872cbe3e9577b54c) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 31, 8 steps, guidance 1.0 |
| Prompt | A light four-wheel-drive survey truck with a small four-cylinder diesel engine, tappety and lively idling steadily, even and unchanging, no revving, continuous. Outdoors, a few metres away. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 5.9241], loop_match 0.611, levelled_by loudness, to -18.78 LUFS, gain_db 2.02 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +0 dB, through volume 13 |
| Result | -18.8 LUFS, true peak -2.2 dBTP, 5.47 s; flags: none |
| File | sha256 af46a100e3f93f14589a9650a5fdf0e8f9732adea5fdba554062a0eac7054e93, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
