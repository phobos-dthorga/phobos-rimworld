# Workshop preview backgrounds

Generated backgrounds for each mod's `About/Preview.png`. The prompts are agent-authored and held for the owner's review. The
model draws the picture only: **the names are added by script, never generated**, with
[`scripts/art/make_preview.py`](../../scripts/art/make_preview.py):

```text
python scripts/art/make_preview.py assets/phobos-previews/generated/preview-water-tap/preview-water-tap-201.png "Water Tap" "Bottled water on tap" mods/PhobosWaterTap/About/Preview.png
```

Generate only through the runner, and only on the owner's go:

```text
python scripts/art-run.py generate assets/phobos-previews/requests/backgrounds.json --stop-at HH:MM
```

Candidates stay in the ignored `generated/` folder until the owner picks one. A picked preview gets an asset record beside it in the
mod's `About/` folder, as every shipped image does. See the [art backlog](../../docs/development/art-backlog.md).
