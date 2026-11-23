# Ventilator.ogg

**Ventilator breathing for a patient (S83).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/m-ventilator-steady/m-ventilator-steady-41.flac (sha256 3c821318530685eabfa0534e54632a85f5be855c95d45f25e10393697876e65c) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 41, 8 steps, guidance 1.0 |
| Prompt | A hospital mechanical ventilator running beside a bed: a constant soft electric hum with a gentle rhythmic bellows push and sigh over it, calm and steady, never silent, continuous. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 6.5628], loop_match 0.844, loop_crossfade_s 0.25, loop_curve equal power, loop_seam_dip_db 0.14, levelled_by loudness, to -34.08 LUFS, gain_db 0.42 |
| Levelled against | the game's Hemopump_Ambience (effective -37.1 LUFS) -3 dB, through volume 50 |
| Result | -34.0 LUFS, true peak -20.3 dBTP, 6.11 s; flags: none |
| File | sha256 9b59420ef8e314abb5f900a96698a50d88fbbd3f72e94ecde84e5d1af7e067cf, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
