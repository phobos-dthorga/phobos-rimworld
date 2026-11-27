# RepeaterCourtesy.ogg

**S166: a repeater station's courtesy tone and squelch tail as a call starts passing through it.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-27 |
| Master | assets/phobos-sounds/generated/n16-repeater-courtesy/n16-repeater-courtesy-202.flac (sha256 d426b34223955cfd349881d5a0ca689c018e5de94d8e0460ad6e38119d840792) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 202, 8 steps, guidance 1.0 |
| Prompt | An amateur radio repeater's tail after a transmission ends, heard on a small radio speaker: a moment of soft open hiss, one short low gentle courtesy tone, then the squelch tail closing cleanly. Mellow and low pitched, no voice, no shrill beep. Not loud, not alarming. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by true peak, to -4.72 dBTP, gain_db 30.28 |
| Levelled against | the game's Checkbox_TurnedOn (effective peak -18.8 dB) -3 dB, through volume 14 |
| Result | -16.7 LUFS, true peak -5.5 dBTP, 1.03 s; flags: none |
| File | sha256 c965a042b9f5493dc2d71f5c579a69e706fdfdb96c861f5eb24ac1decd5104f1, made 2026-11-27 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
