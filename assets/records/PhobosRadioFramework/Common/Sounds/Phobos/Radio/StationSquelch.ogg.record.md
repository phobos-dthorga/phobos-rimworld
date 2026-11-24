# StationSquelch.ogg

**S102: a radio station's squelch and static, quietly, while it is in use.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-25 |
| Master | assets/phobos-sounds/generated/n13-station-squelch/n13-station-squelch-131.flac (sha256 6c6752d92f8f9f3295646f7581c2cb59044f07832e4c0b2ea7e72a4e320d2dff) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 131, 8 steps, guidance 1.0 |
| Prompt | A two-way radio base station on a desk with its squelch open: a soft, even hiss of radio static with faint crackles, steady and unbroken, heard from across a quiet room. Not loud, not alarming. Length: 6 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 4.5], loop_match 0.459, loop_crossfade_s 0.25, loop_curve equal power, loop_seam_dip_db 0.0, levelled_by loudness, to -40.08 LUFS, gain_db 5.52 |
| Levelled against | the game's Hemopump_Ambience (effective -37.1 LUFS) -9 dB, through volume 50 |
| Result | -40.3 LUFS, true peak -17.5 dBTP, 4.05 s; flags: spiky |
| File | sha256 3ec791c0ae7f07bac724e728a956b5ae24cac1d68170c900929ac5764b01cab3, made 2026-11-25 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
