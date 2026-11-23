# SuppressionDischarge.ogg

**Automatic fire suppression going off in an engine bay or cab (S143, Framework 0.253.0).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-23 |
| Master | assets/phobos-sounds/generated/n11-suppression/n11-suppression-83.flac (sha256 5fddc120d246adbae324872b0fd36bfea429264eb497be07bec0146e45d4cf4c) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 83, 8 steps, guidance 1.0 |
| Prompt | An automatic fire suppression system going off inside a truck's engine bay: a muffled metallic click, then a steady rushing hiss of gas from several nozzles behind a closed panel, fading out. Heard from outside the vehicle, muffled, not alarming. Length: 4 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -15.14 LUFS, gain_db 4.8 |
| Levelled against | the game's HissJet (effective -17.1 LUFS) -6 dB, through volume 40 |
| Result | -17.2 LUFS, true peak -1.1 dBTP, 4.03 s; flags: none |
| File | sha256 c0529ac3e4eb86827baa02b5664c992543cc66455c040f94ef4787e732bb1ba1, made 2026-11-23 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
