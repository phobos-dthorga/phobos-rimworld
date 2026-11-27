# SquelchStation.ogg

**S165: a base station's gentle squelch from its desk speaker as it comes into use.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-27 |
| Master | assets/phobos-sounds/generated/n16-squelch-station-low/n16-squelch-station-low-203d12.flac (sha256 7c97e0147266272e0edc2f07028de77d1237debcc03cd724b2b58026054be92d) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 203, 8 steps, guidance 1.0 |
| Prompt | A two-way radio base station on a desk opening its squelch as the operator starts a call: a soft, warm burst of radio hiss from a larger desk speaker with a little room around it, closing again cleanly. Mellow and low, no voice, no beep. Not loud, not alarming. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by true peak, to -5.72 dBTP, gain_db 18.48 |
| Levelled against | the game's Checkbox_TurnedOn (effective peak -18.8 dB) -4 dB, through volume 14 |
| Result | -24.3 LUFS, true peak -6.4 dBTP, 0.72 s; flags: none |
| File | sha256 1df346be22c8b8dcb8c03dc147612d72b5b472c6f00fd342c70fad2a4e34e5a9, made 2026-11-27 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
