# SquelchVehicle.ogg

**S164: a vehicle radio's gentle squelch from its small speaker as the vehicle comes into touch with a station.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-27 |
| Master | assets/phobos-sounds/generated/n16-squelch-mobile/n16-squelch-mobile-204.flac (sha256 dd005a5acc3c8704d73190b428bc981319104f9a31e20dff3f7b2fc3de1b3c72) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 204, 8 steps, guidance 1.0 |
| Prompt | A small mobile two-way radio's compact loudspeaker opening its squelch: a short, smooth burst of soft radio hiss that rises gently and closes again cleanly, slightly narrow and boxy in tone, as from a small metal speaker grille. Steady and even, no thumps, no knocks, no bumps, no engine, no road noise, no voice, no beep. Not loud, not alarming. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by true peak, to -5.72 dBTP, gain_db 8.18 |
| Levelled against | the game's Checkbox_TurnedOn (effective peak -18.8 dB) -4 dB, through volume 14 |
| Result | -31.0 LUFS, true peak -6.4 dBTP, 1.05 s; flags: highs, spiky |
| File | sha256 4ed856d13228aab2d6e749a74ac0384ec60a9d8a86f18be0144752a9a874408d, made 2026-11-27 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
