# ChannelKnob.ogg

**S169: a station's channel knob turned as it moves a radio to another channel.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-27 |
| Master | assets/phobos-sounds/generated/n16-channel-knob/n16-channel-knob-202.flac (sha256 00ed37544b45d86fd09228c25ae4cf0d07be619db4844805323d4ff57171fd60) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 202, 8 steps, guidance 1.0 |
| Prompt | The channel knob on a two-way radio turned a few steps: three or four small firm detented clicks of a rotary switch, close to the listener, dry and clean, nothing else. Not loud, not alarming. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by true peak, to -3.72 dBTP, gain_db 6.28 |
| Levelled against | the game's Checkbox_TurnedOn (effective peak -18.8 dB) -2 dB, through volume 14 |
| Result | -29.5 LUFS, true peak -3.9 dBTP, 1.39 s; flags: highs, spiky |
| File | sha256 2bccf2aa0f41e2d516e5d51f496570e2f00eeaa3dff2dfda5b86452a14a79e92, made 2026-11-27 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
