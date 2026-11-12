# HookLatch.ogg

**S128: a helicopter's cargo hook latching onto a sling load.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the agent, provisionally, for the owner to confirm, 2026-11-12 |
| Master | assets/phobos-sounds/generated/s128-hook-latch/s128-hook-latch-51.flac (sha256 9fc69e61cf136229647d36b1bff7971223c44123f6824cb4d603e7ce824cc043) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 51, 8 steps, guidance 1.0 |
| Prompt | A heavy steel cargo hook under a helicopter snapping shut on a lifting ring: one solid spring-loaded metal latch clack with a short chain rattle as the strops take up, no engine noise. Close, outdoors, dry. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; cut_s [0.1, 1.05], levelled_by loudness, to -23.73 LUFS, gain_db -0.1 |
| Levelled against | the game's Interact_Repair_Start (effective -42.9 LUFS) +0 dB, through volume 11 |
| Result | -26.7 LUFS, true peak -1.7 dBTP, 0.87 s; flags: highs, spiky |
| File | sha256 a72239609af7c83468dcc006db6803430412c79cbd9966eb13dea96e1451550d, made 2026-11-12 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
