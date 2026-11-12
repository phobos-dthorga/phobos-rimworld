# Sputter.ogg

**SurveyTruck: out of fuel.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-survey-sputter/p1-survey-sputter-31.flac (sha256 3151273afe97928865f9cc3cb2b774e1fb744399d5336bbad66e4d8793e00c6b) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 31, 8 steps, guidance 1.0 |
| Prompt | A light four-wheel-drive survey truck with a small four-cylinder diesel engine, tappety and lively running out of fuel: it stumbles, coughs and sputters, then dies. Outdoors, close. Length: 4 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -18.94 LUFS, gain_db 1.9 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +4 dB, through volume 21 |
| Result | -21.8 LUFS, true peak -1.4 dBTP, 4.66 s; flags: spiky |
| File | sha256 dfa75a37b31d84773b1e54b289e283d4456c9dfe6d6cb127105b8d3cfecf6c3c, made 2026-11-13 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
