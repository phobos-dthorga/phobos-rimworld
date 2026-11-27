# LinkLost.ogg

**S168: a remote control link dropping out, as a vehicle driven from afar loses its link.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-27 |
| Master | assets/phobos-sounds/generated/n16-link-lost/n16-link-lost-202.flac (sha256 13d7eb88eed0646a720301566587932e36aa853bd385c1ed6d701595e0c745d9) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 202, 8 steps, guidance 1.0 |
| Prompt | A radio control link dropping out: a short broken stutter of data tones fading into soft static, then quiet. Low and gentle, no voice, no alarm, no beeping. Not loud, not alarming. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; filter bandreject=f=7000:width_type=h:w=400, levelled_by true peak, to -4.72 dBTP, gain_db 18.28 |
| Levelled against | the game's Checkbox_TurnedOn (effective peak -18.8 dB) -3 dB, through volume 14 |
| Result | -21.1 LUFS, true peak -4.3 dBTP, 1.45 s; flags: highs |
| File | sha256 7da343a0865e0a6a745fd2b65804b8181cefbc93260d8263633925c442161feb, made 2026-11-27 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
