# FuelNozzle.ogg

**S3: a vehicle refuelled.** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-10 |
| Master | assets/phobos-sounds/generated/s03-fuel-nozzle/s03-fuel-nozzle-23.flac (sha256 ca28563922e85f023bdb971312eb184e1652356870cdbe884e7f153074d64211) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 23, 8 steps, guidance 1.0 |
| Prompt | Refuelling a truck from a fuel pump nozzle: the nozzle clunks into the filler neck, then liquid fuel glugs and flows into a metal tank for a few seconds, ending with the nozzle clicking off and being pulled out. Close, outdoors, dry. Length: 6 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by loudness, to -26.01 LUFS, gain_db -0.71 |
| Levelled against | the game's Interact_Repair (effective -38.4 LUFS) -3 dB, through volume 17 |
| Result | -26.0 LUFS, true peak -4.1 dBTP, 7.03 s; flags: spiky |
| File | sha256 bfe4cd56e7944a2d2e156d25165848115168968efabe91fc4de3e876a06be531, made 2026-11-10 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
