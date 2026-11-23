# Leak.ogg

**Tanker: tank leaking (S29).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p2-tanker-leak/p2-tanker-leak-41.flac (sha256 ac702a5bf516d267b8cf46ecedde80d6ceb8ba03869e7b7af8d174b78eafe9e7) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 41, 8 steps, guidance 1.0 |
| Prompt | Liquid leaking steadily from a punctured steel tank onto gravel, a continuous gentle trickle and drip, unchanging. Outdoors. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 6.9612], loop_match 0.278, loop_crossfade_s 0.25, loop_curve equal power, loop_seam_dip_db 0.0, levelled_by true peak, to -4.72 dBTP, gain_db 7.78 |
| Levelled against | the game's HissSmall (effective peak -18.2 dB) -3 dB, through volume 15 |
| Result | -28.5 LUFS, true peak -4.9 dBTP, 6.51 s; flags: highs, spiky |
| File | sha256 08cc8ec6b46a581e308b2072528a2c7d54921a27772f81b3b007b63ba3b6962b, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
