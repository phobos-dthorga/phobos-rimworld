# Medical equipment art

Candidates for D'thorga Medical's equipment part (Medical Equipment until it joined D'thorga Medical on 7 Nov 2026). **What ships is
recorded beside each texture** in `mods/PhobosMedical/`, and the early picks in the design record. The prompts are agent-authored and
held for the owner's review. See the [design record](../../docs/development/art-automation-and-medical-equipment.md).

| Request file | What | Run, and what shipped |
| --- | --- | --- |
| [test-run.json](requests/test-run.json) | A short test of the engines and step counts | 5 Nov 2026 |
| [first-batch.json](requests/first-batch.json) | The oxygen machines, burn bed, chamber, field kit, dressings, resuscitator and the Workshop preview | 5 Nov 2026; picks in Medical Equipment 0.2.3 and 0.3.0 |
| [second-batch.json](requests/second-batch.json) | The concentrator redrawn, the chamber from its flat stand-in, Triage's button icon | 6 Nov 2026; the button icon's pick in Triage 0.1.2 |
| [third-batch.json](requests/third-batch.json) | The second set of equipment (11 pieces), the electrolyser and the chamber top-down | Night 3, 8 Nov 2026; picks in Medical 0.14.1 |
| [depth-trial.json](requests/depth-trial.json) | The electrolyser and chamber through the depth-control LoRA | Night 3, 8 Nov 2026; the chamber's pick in Medical 0.14.1 |
| [frost.json](requests/frost.json) | The liquid oxygen store's frost | Night 3, 8 Nov 2026; none used (the frost is drawn in code, Medical 0.15.0) |
| [radiation-icons.json](requests/radiation-icons.json) | The radiation control point and the zone tools (with Rimatomics) | 10 Nov 2026; picks in Medical 0.11.1 |

**Generate only through the runner, and only on the owner's go:**

```text
python scripts/art-run.py generate assets/phobos-medical/requests/test-run.json --stop-at HH:MM
python scripts/art-run.py generate assets/phobos-medical/requests/first-batch.json --stop-at HH:MM
python scripts/art-run.py generate assets/phobos-medical/requests/second-batch.json --stop-at HH:MM
```

The runner writes `generated/review.html`, a contact sheet per subject and a report. Because most references are game art, it also
checks every candidate against them: the report lists anything too close, and the review page marks it "do not pick". If the report
says the check did not run, pick nothing until it has. Our own art in `mods/` and `assets/` is left out of the check, because
resembling it is why we use it. To run it by hand:

```text
C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I scripts/art/near_copy.py assets/phobos-medical/generated .local/references/vanilla-medical
```

**Comparing with the stand-ins.** `baselines.json` names what the mod shows now for each subject. The review page puts it first in
that subject's section, and shows those candidates at 64 px too. The owner liked the concentrator stand-in (5 Nov 2026), so
`oxygen-concentrator-from-stand-in` uses its large drawing, `references/OxygenConcentrator-large.png`, as a reference, with the
same seeds as `oxygen-concentrator`. That makes "this design, redrawn" and "the model's own idea" directly comparable. It stays out
of the LoRA comparison (`"lora_comparison": false`).

**The second batch** (6 Nov 2026) redraws the concentrator without the first batch's hourglass shapes, redraws the hyperbaric chamber from a
flat top-down stand-in (`references/HyperbaricChamber-large.png`, drawn by `draw_placeholders.py HyperbaricChamber`), and tries an icon for
Triage's "Doctors may operate" button. All use references plus the private style model, the route the owner preferred.

**Our own reference drawings are not on `main`** (owner, 7 Nov 2026). `references/` is ignored; the `art-references` branch keeps a
copy of each with its record, and `draw_placeholders.py OxygenConcentrator HyperbaricChamber` draws them again on a fresh checkout.
Full-size masters of the picks are on the `art-masters` branch. Both are added with `scripts/art/archive_branch.py`.

**The game-art references are vanilla sprites in the ignored `.local/references/`, and are never committed.** The extractor that makes them
is private too (`.local/tools/art/extract_references.py`), so a fresh checkout has to rebuild both. The first batch makes south views only; the
east and north views come from the owner's picks, with the pick as the reference, so all three views match.
