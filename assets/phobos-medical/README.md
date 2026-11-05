# Medical equipment art

Candidates for the planned medical equipment mod. **The owner's picks, and what ships, are listed in the design record.** The prompts are
agent-authored and held for the owner's review. See the [design record](../../docs/development/art-automation-and-medical-equipment.md).

**Generate only through the runner, and only on the owner's go:**

```text
python scripts/art-run.py generate assets/phobos-medical/requests/test-run.json --stop-at HH:MM
python scripts/art-run.py generate assets/phobos-medical/requests/first-batch.json --stop-at HH:MM
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

**The game-art references are vanilla sprites in the ignored `.local/references/`, and are never committed.** The extractor that makes them
is private too (`.local/tools/art/extract_references.py`), so a fresh checkout has to rebuild both. The first batch makes south views only; the
east and north views come from the owner's picks, with the pick as the reference, so all three views match.
