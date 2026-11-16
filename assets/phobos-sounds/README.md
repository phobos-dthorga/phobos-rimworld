# Phobos sounds: generation requests and records

Sound effects for any of our mods, made locally with Stable Audio 3 (Stability AI) through ComfyUI. How it works, the licence
and the review aids: [sound generation](../../docs/development/sound-generation.md).

- `requests/`: request files for `python scripts/art-run.py generate <file> --stop-at HH:MM`.
- `generated/`: candidates (FLAC masters with a JSON record each), review copies and the listening page `sound-review.html`.
  Git ignores it; nothing there is in a mod until the owner picks it.

| Request file | What | Status |
| --- | --- | --- |
| [pilot.json](requests/pilot.json) | Generator loop, oxygen concentrator hum, sonar ping, switch click; full-steps and distilled engines, seeds 11 and 12 | Run 10 Nov 2026 on the processor (16 sounds, 8 minutes). **The owner picked** the distilled concentrator hum (12; the full-steps seed 12 with an 11 kHz low-pass is the runner-up), the distilled generator loop (11), sonar ping (11) and switch click (11), all on 10 Nov 2026: [picks.json](picks.json). Findings in the [design record](../../docs/development/sound-generation.md#the-pilot-10-nov-2026) |
| [generic-library.json](requests/generic-library.json) | The generic library, S3 to S17 (fuel nozzle, jump-start kit, warning chime, tyre burst, knock, brakes, oxygen regulator, cargo and more) | Run 10 Nov 2026; shipped in Framework 0.75.0, Medical 0.13.0, Vehicles 0.34.0 |
| [phase1-vehicles.json](requests/phase1-vehicles.json) | Phase 1: engines, doors, air brakes and sputter for our vehicles | Run 10 to 11 Nov 2026; shipped in Framework 0.76.0, Vehicles 0.35.0 |
| [phase2-vehicles-at-work.json](requests/phase2-vehicles-at-work.json) | Phase 2: our vehicles at work (pumping, tipping, drilling, surveying, diving) | Run 11 Nov 2026; shipped in Framework 0.82.0, Vehicles 0.39.0 |
| [medical.json](requests/medical.json) | Medical machines, devices and coughs (S83 to S100) | Run 11 Nov 2026; shipped in Medical 0.14.0 |
| [night-reruns.json](requests/night-reruns.json) | Fresh seeds or steadier wordings where every seed of a phase 2 or medical sound failed | Run 11 Nov 2026; picks shipped with phase 2 and medical |
| [sling-latch.json](requests/sling-latch.json) | The cargo hook latching onto a sling load (S128) | Run 12 Nov 2026; agent's provisional pick in Framework 0.101.0 |
| [sling-latch-2.json](requests/sling-latch-2.json) | A fresh set for the hook latch, two new wordings | Run 16 Nov 2026; the owner's pick, r2d-94, in Framework 0.114.4 |

**Shipped (not yet tested in game):** the four pilot picks on 10 Nov 2026, and every later pick, written into D'thorga Framework, Medical and Vehicles by
`python scripts/art/export_sound.py exports.json` ([exports.json](exports.json) says where each goes and which game sound it sits
beside; [exported.json](exported.json) holds every figure). Measure the game's own sounds with `python scripts/art/game_sounds.py`.

Every sound shipped from here keeps its record (engine, model hash, prompt, seed, date) and says it was made with generative AI.
