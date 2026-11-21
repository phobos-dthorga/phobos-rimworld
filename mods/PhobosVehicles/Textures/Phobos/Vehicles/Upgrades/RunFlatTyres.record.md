# RunFlatTyres

Made on 19 Nov 2026 with ComfyUI, local, FLUX.2 Klein 4B base (Black Forest Labs, Apache 2.0) with our private style model, night 7: **AI-generated**, picked by the owner on 19 November 2026 by question card. A deep-tread tyre cut open to show its orange run-flat insert: the upgrade icon of the service trucks' armour kit of that name, on all five trucks. Exported by `scripts/art/export_sprite.py` at 128 px, the background cleared with the background AI's mask. The prompt is in `assets/phobos-vehicles/requests/truck-kit-icons.json`. Only our own art was used as references; no game or other author's art.

**Re-exported 21 Nov 2026** with the owner's go: the tyre's middle had kept the master's background white, found by `export_sprite.py --sweep`. The export now names a hole there (`holes` in its `.export.json`), cleared as far as the outline, so the ground shows through; nothing else in the picture changed (`clean_backgrounds.py` stage, review and `apply --only`).

- Master: `truck-kit-runflattyres-12.png`, SHA-256 `2d053ee099120519ee64bf81227d7387de9a07d0dc089b4cdd6d2d0db9d939ee`
- `RunFlatTyres.png`: SHA-256 `d768a4db513d69e19c95bd13e932a25e0842714dd567321af8c941c9b7091f49`
