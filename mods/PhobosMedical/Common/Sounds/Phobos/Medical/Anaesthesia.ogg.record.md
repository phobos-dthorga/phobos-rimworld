# Anaesthesia.ogg

**Anaesthesia machine during surgery (S85).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/m-anaesthesia/m-anaesthesia-42.flac (sha256 f8f4c9b05d1f9269e0784adc393f6f89cf3422f5c93566a367b3223478ad8b2d) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 42, 8 steps, guidance 1.0 |
| Prompt | An anaesthesia machine during surgery: a slow soft bellows rising and falling with quiet gas flow, calm and continuous. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 6.2155], loop_match 0.159, loop_crossfade_s 1.0, loop_curve equal gain, loop_seam_dip_db 0.86, levelled_by loudness, to -37.08 LUFS, gain_db -9.38 |
| Levelled against | the game's Hemopump_Ambience (effective -37.1 LUFS) -6 dB, through volume 50 |
| Result | -37.3 LUFS, true peak -25.6 dBTP, 5.02 s; flags: highs |
| File | sha256 ebd53e0f5c3e572f0b815a160e3b6cb9c297abb04bf843ca83d707bf2ef3c337, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
