# EngineIdle.ogg

**DrillRig: engine idling (blended by load, pitched by rpm).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-drillrig-idle/p1-drillrig-idle-31.flac (sha256 1f5245dc85b1c7364dddab96c721e13bd262a8178de143d8ff539d36922e171b) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 31, 8 steps, guidance 1.0 |
| Prompt | A heavy drill rig truck with a big low-revving diesel engine and a whining hydraulic pump on its power take-off idling steadily, even and unchanging, no revving, continuous. Outdoors, a few metres away. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 6.1852], loop_match 0.26, levelled_by loudness, to -18.78 LUFS, gain_db 2.62 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +0 dB, through volume 13 |
| Result | -18.8 LUFS, true peak -2.6 dBTP, 5.74 s; flags: none |
| File | sha256 7f668dcb99951bce6a8bc7fa2d9531630a7d6d5ee657bd0a8230a5ec26c7d490, made 2026-11-11 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
