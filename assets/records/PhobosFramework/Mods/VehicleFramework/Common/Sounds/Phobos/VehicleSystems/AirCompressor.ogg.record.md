# AirCompressor.ogg

**S13: air brakes recharging after a stop.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-10 |
| Master | assets/phobos-sounds/generated/s13-air-compressor/s13-air-compressor-21.flac (sha256 71e093aeb85d589eef41f8c1e1b0965086065e030412e003330760911c499afd) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 21, 8 steps, guidance 1.0 |
| Prompt | A truck's air brake compressor charging after stopping: a steady mechanical chugging pump for a few seconds, then the air dryer purging with one short sharp hiss. Outdoors, beside the truck. Length: 6 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -24.78 LUFS, gain_db -4.78 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) -6 dB, through volume 13 |
| Result | -25.0 LUFS, true peak -11.6 dBTP, 7.03 s; flags: highs |
| File | sha256 b28873f4feb492527b6f90b874b07592235c23d1ea5f0bb1b26a1479b4be89f3, made 2026-11-10 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
