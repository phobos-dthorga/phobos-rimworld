# BatteryVenting.ogg

**S16: a traction battery in thermal runaway.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-10 |
| Master | assets/phobos-sounds/generated/s16-battery-venting/s16-battery-venting-22.flac (sha256 46bd84566baa25bf935cdf85bd47e8c5cba893e2d3589acfe1d9b812c8e2708d) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 22, 8 steps, guidance 1.0 |
| Prompt | A large lithium battery pack failing: a sharp pop, then a fierce pressurised hiss of gas venting from the cells with crackling, growing stronger. Outdoors. Length: 5 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; filter lowpass=f=12000,lowpass=f=12000,lowpass=f=12000,lowpass=f=12000, levelled_by loudness, to -9.14 LUFS, gain_db 1.0 |
| Levelled against | the game's HissJet (effective -17.1 LUFS) +0 dB, through volume 40 |
| Result | -29.3 LUFS, true peak -1.1 dBTP, 5.35 s; flags: highs, spiky |
| File | sha256 b3f6df341ef77340403d48b56fe3033f5ac572dccbf9dc8bb71772205df086cb, made 2026-11-10 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
