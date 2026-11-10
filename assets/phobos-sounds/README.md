# Phobos sounds: generation requests and records

Sound effects for any of our mods, made locally with Stable Audio 3 (Stability AI) through ComfyUI. How it works, the licence
and the review aids: [sound generation](../../docs/development/sound-generation.md).

- `requests/`: request files for `python scripts/art-run.py generate <file> --stop-at HH:MM`.
- `generated/`: candidates (FLAC masters with a JSON record each), review copies and the listening page `sound-review.html`.
  Git ignores it; nothing there is in a mod until the owner picks it.

| Request file | What | Status |
| --- | --- | --- |
| [pilot.json](requests/pilot.json) | Generator loop, oxygen concentrator hum, sonar ping, switch click; full-steps and distilled engines, seeds 11 and 12 | Run 10 Nov 2026 on the processor (16 sounds, 8 minutes). **The owner picked** the distilled concentrator hum (12; the full-steps seed 12 with an 11 kHz low-pass is the runner-up), the distilled generator loop (11), sonar ping (11) and switch click (seed open): [picks.json](picks.json). Findings in the [design record](../../docs/development/sound-generation.md#the-pilot-10-nov-2026) |

Every sound shipped from here keeps its record (engine, model hash, prompt, seed, date) and says it was made with generative AI.
