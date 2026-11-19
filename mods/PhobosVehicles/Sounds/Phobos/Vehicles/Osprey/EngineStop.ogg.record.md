# EngineStop.ogg

**V-22 Osprey: engine shutdown.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-19 |
| Master | assets/phobos-sounds/generated/n7-osprey-shutdown/n7-osprey-shutdown-72.flac (sha256 51acf5a10438b0e90b73242d40b30484f1fe0b913c7d91e6ec8d9f59f4d64e5b) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 72, 8 steps, guidance 1.0 |
| Prompt | A large aircraft's two turbine engines shutting down: the turbine whine falling away slowly while big rotor blades swish slower and slower to a stop, ending in silence. Outdoors. Length: 10 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -16.94 LUFS, gain_db 1.3 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +6 dB, through volume 21 |
| Result | -18.8 LUFS, true peak -1.3 dBTP, 9.07 s; flags: none |
| File | sha256 d7719406dd9de27e623154f5e446b472923ae3a7285f1696d4beafdf67a14796, made 2026-11-19 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
