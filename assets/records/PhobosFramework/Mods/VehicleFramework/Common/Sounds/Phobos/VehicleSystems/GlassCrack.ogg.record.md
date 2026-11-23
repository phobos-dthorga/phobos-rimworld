# GlassCrack.ogg

**A vehicle's glass cracking (S146, Framework 0.253.0).** Made with generative AI: ComfyUI 0.41.0, local, Stable Audio 3 Small SFX, distilled (Stability AI, Stability AI Community License; Comfy-Org packaging), run locally, prompted by the Phobos art scripts, then prepared by scripts/art/export_sound.py. The model's outputs belong to their maker under the Stability AI Community License; shipping them needs no notice (docs/development/sound-generation.md).

| | |
| --- | --- |
| Picked by | the owner, 2026-11-23 |
| Master | assets/phobos-sounds/generated/n11-glass-crack/n11-glass-crack-82.flac (sha256 037fd137321b91b8671f71936a3141adccf158d21593e17be5a0f5137d35bd3e) |
| Model | stable_audio_3_small_sfx.safetensors (sha256 ed9cf1b6172f1a8c2921a9560c21109ff3239524563ced9dce6dcdef41e2f515), seed 82, 8 steps, guidance 1.0 |
| Prompt | A stone chip striking a vehicle windscreen: a single sharp tick and a short crackle of laminated glass cracking, still in one piece. Close, quiet, not alarming. Length: 2 seconds |
| Made into | oneshot, mono, 44100 Hz, Ogg Vorbis quality 5; levelled_by true peak, to -13.66 dBTP, gain_db -7.46 |
| Levelled against | the game's Metal_Drop (effective peak -22.7 dB) -3 dB, through volume 25 |
| Result | -37.7 LUFS, true peak -13.7 dBTP, 0.73 s; flags: highs, spiky |
| File | sha256 d44c311866e4c78d11afae21bf743e14bf54054497842f501541178a752417db, made 2026-11-23 with ffmpeg version 9.0.2-full_build-www.gyan.dev Copyright (c) 2000-2026 the FFmpeg developers |

Not yet tested in game.
