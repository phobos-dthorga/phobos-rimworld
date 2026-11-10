# Pump.ogg

**Tanker: pump running off the power take-off (S27).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p2-tanker-pump/p2-tanker-pump-41.flac (sha256 7890eb2b59c33cd6594f6771ba8eab6bb6d43765cbb97f5db071c82a837d60e5) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 41, 8 steps, guidance 1.0 |
| Prompt | A bulk tanker truck's transfer pump running off its power take-off: a steady mechanical whine with liquid rushing through pipes and a big diesel idling underneath, continuous and unchanging. Outdoors. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 5.9053], loop_match 0.199, loop_crossfade_s 1.0, loop_curve equal gain, loop_seam_dip_db 0.0, levelled_by loudness, to -24.78 LUFS, gain_db -0.18 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) -6 dB, through volume 13 |
| Result | -24.9 LUFS, true peak -10.5 dBTP, 4.71 s; flags: none |
| File | sha256 3e8bd83dc33cf7eeae73efae70fd178461589222fa313a0819523399ef7a2d8c, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
