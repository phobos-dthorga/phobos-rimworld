# SwitchClick.ogg

**A physical switch on a vehicle or rig turned on or off.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-10 |
| Master | assets/phobos-sounds/generated/switch-click-fast/switch-click-fast-11.flac (sha256 60ebfe1d7c3f7df2efffe7da21cebbe18d2640325d88eee376a1a99953a440c4) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 11, 8 steps, guidance 1.0 |
| Prompt | One soft toggle switch click on a metal control panel, a short dry close-up click with a small muffled thunk, quiet, dead room with no echo. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; cut_s [0.98, 1.3], levelled_by true peak, to -3.7 dBTP, gain_db 9.1 |
| Levelled against | the game's Checkbox_TurnedOn (effective peak -18.8 dB) -2 dB, through volume 14 |
| Result | -70.0 LUFS, true peak -4.0 dBTP, 0.13 s; flags: highs, spiky |
| File | sha256 7da16829eb6eab746c879370bcb6af363b109a30002610570cc526c09a00c8d4, made 2026-11-10 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
