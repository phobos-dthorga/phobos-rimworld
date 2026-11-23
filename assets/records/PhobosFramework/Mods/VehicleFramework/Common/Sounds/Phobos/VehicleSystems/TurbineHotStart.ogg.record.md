# TurbineHotStart.ogg

**A gas turbine lit hot by a weak battery (S123, Framework 0.270.0).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-23 |
| Master | assets/phobos-sounds/generated/n12-turbine-hot-start/n12-turbine-hot-start-121.flac (sha256 8a19cbb12dc175ae4ca5eaf8ce33910ccdecc49681b36dd9075a11726e5e306c) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 121, 8 steps, guidance 1.0 |
| Prompt | A gas turbine engine failing to start properly: the starter whines and the engine spools up slowly, a rough rumbling light-off with a coughing roar, then the spool sags and winds down. Heard from outside the aircraft. Not loud, not alarming. Length: 6 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -18.78 LUFS, gain_db 4.1 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +0 dB, through volume 13 |
| Result | -19.8 LUFS, true peak -1.2 dBTP, 6.09 s; flags: none |
| File | sha256 cb1d3c22392105d5d86b66a809d43c547320e3afada1eea55af13636c4e91914, made 2026-11-23 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
