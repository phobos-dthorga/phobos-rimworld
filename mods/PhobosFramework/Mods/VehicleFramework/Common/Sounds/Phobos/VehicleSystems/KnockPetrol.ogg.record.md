# KnockPetrol.ogg

**S11: a petrol engine knocking, oil pressure gone.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-10 |
| Master | assets/phobos-sounds/generated/s11-knock-petrol/s11-knock-petrol-24.flac (sha256 2f92b25f253ec73a36039984476e0cb5e2b95364b56f0f5bf05eea011668743b) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 24, 8 steps, guidance 1.0 |
| Prompt | A worn petrol engine idling with a light rapid tapping and knocking from loose valves, steady rough running, continuous and unchanging. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 6.5526], loop_match 0.235, levelled_by loudness, to -24.78 LUFS, gain_db -1.78 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) -6 dB, through volume 13 |
| Result | -24.9 LUFS, true peak -10.2 dBTP, 6.1 s; flags: none |
| File | sha256 09481d363a67732585c027bf32262edbff8e2cc4a9ffe4e8861f2031a25940a4, made 2026-11-10 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
