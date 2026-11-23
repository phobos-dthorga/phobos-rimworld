# FailedStartPetrol.ogg

**S10: a petrol engine that will not start.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-10 |
| Master | assets/phobos-sounds/generated/s10-failed-start-petrol/s10-failed-start-petrol-21.flac (sha256 7dbdb6a3e6e1beefc11392b59313dc1e85e1ed3510a034e6ec16049c644348b4) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 21, 8 steps, guidance 1.0 |
| Prompt | A petrol car engine failing to start: the starter motor cranks with a weak, slowing whirr, the engine sputters but never catches, then silence. Outdoors, close. Length: 5 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -18.94 LUFS, gain_db 7.8 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +4 dB, through volume 21 |
| Result | -21.9 LUFS, true peak -0.7 dBTP, 6.1 s; flags: spiky |
| File | sha256 095229999aa786e9d39f86f7a8611ec4065f7d606ee3fc731b8503801c24a4a8, made 2026-11-13 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
