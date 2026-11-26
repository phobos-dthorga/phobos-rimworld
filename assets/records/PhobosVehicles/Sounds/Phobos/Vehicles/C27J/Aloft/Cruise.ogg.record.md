# Cruise.ogg

**S155: C-27J Spartan in the air, heard from the ground (one of two, played in turn).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-26 |
| Master | assets/phobos-sounds/generated/n17-c27j-flyover/n17-c27j-flyover-171.flac (sha256 e96296638aca2c98990986ca04107327419e3f55a0c974bfd86df077bb8efdd5) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 171, 8 steps, guidance 1.0 |
| Prompt | A twin-engine turboprop transport aircraft cruising high overhead, heard from the ground: a soft, distant, even propeller drone with a gentle low hum, steady and unchanging, continuous, with faint wind. Outdoors. Not loud, not alarming. Length: 8 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -16.94 LUFS, gain_db 8.9 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +6 dB, through volume 21 |
| Result | -17.7 LUFS, true peak -1.1 dBTP, 6.55 s; flags: none |
| File | sha256 55f7a0a3f61f41949e00cf21392affb2f2aeb2004c1ce54ac928b30f18f2ec3b, made 2026-11-27 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
