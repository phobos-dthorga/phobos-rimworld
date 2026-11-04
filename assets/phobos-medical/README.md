# Medical equipment art

Candidates for the planned medical equipment mod. **Nothing here is approved, and the mod does not exist yet.** The prompts are
agent-authored and held for the owner's review. See the [design record](../../docs/development/art-automation-and-medical-equipment.md).

**Generate only through the runner, and only on the owner's go:**

```text
python scripts/art-run.py generate assets/phobos-medical/requests/test-run.json --stop-at HH:MM
python scripts/art-run.py generate assets/phobos-medical/requests/first-batch.json --stop-at HH:MM
```

The runner writes `generated/review.html`, a contact sheet per subject and a report. Run this over any candidate before it can be
picked, because the references are game art:

```text
C:/AI/ComfyUI/ComfyUI_windows_portable/python_embeded/python.exe -I scripts/art/near_copy.py assets/phobos-medical/generated .local/references/vanilla-medical
```

**The references are vanilla sprites in the ignored `.local/references/`, and are never committed.** The extractor that makes them
is private too (`.local/tools/art/extract_references.py`), so a fresh checkout has to rebuild both. The first batch makes south views only; the
east and north views come from the owner's picks, with the pick as the reference, so all three views match.
