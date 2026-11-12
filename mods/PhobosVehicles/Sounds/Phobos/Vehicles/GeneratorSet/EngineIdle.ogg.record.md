# EngineIdle.ogg

**GeneratorSet: engine idling (blended by load, pitched by rpm).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-11 |
| Master | assets/phobos-sounds/generated/p1-genset-idle/p1-genset-idle-32.flac (sha256 c0550adc5f88eb541dec868dcc3c9c1a8792f92ee55935018e92dc7648a0a0fa) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 32, 8 steps, guidance 1.0 |
| Prompt | A small diesel-electric truck: a modest diesel engine turning a generator, with a faint electric traction motor whine idling steadily, even and unchanging, no revving, continuous. Outdoors, a few metres away. Length: 8 seconds |
| Made into | loop, mono, 44100 Hz, Ogg Vorbis quality 5; loop_span_s [0.2, 5.5956], loop_match 0.275, loop_crossfade_s 0.25, loop_curve equal power, loop_seam_dip_db 1.04, levelled_by loudness, to -18.94 LUFS, gain_db -1.74 |
| Levelled against | the game's ChemfuelFiredGenerator_Ambience (effective -36.5 LUFS) +4 dB, through volume 21 |
| Result | -19.1 LUFS, true peak -4.4 dBTP, 5.15 s; flags: none |
| File | sha256 dabb4c28af551de32dd9ae2a78f651ea6bf2ecab34b9aa06cccec91b3c98f8c3, made 2026-11-13 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
