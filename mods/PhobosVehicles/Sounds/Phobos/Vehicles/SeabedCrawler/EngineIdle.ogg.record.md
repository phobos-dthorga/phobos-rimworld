# EngineIdle.ogg

**SeabedCrawler: engine idling (blended by load, pitched by rpm).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-crawler-idle/p1-crawler-idle-31.flac (sha256 2bdc7f20f3cbeb76d4b2e46e0b4e538f45888acbb6fdcdb6b3534b2006b21716) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 31, 8 steps, guidance 1.0 |
| Prompt | A small research submarine driven by electric motors turning a propeller, heard from inside its steel hull, motors humming softly at slow speed with a gentle propeller swish, calm, continuous and unchanging. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.4, 6.7677], loop_match 0.454, levelled_by loudness, to -21.78 LUFS, gain_db 9.12 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) -3 dB, through volume 13 |
| Result | -21.8 LUFS, true peak -7.7 dBTP, 6.12 s; flags: none |
| File | sha256 629ce106b5a0c10c68a652ece279f0c226e450d5f41cd942ea05be53380705e9, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
