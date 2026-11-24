# CamperBed

Made on 22 Nov 2026 with OpenAI gpt-image-2 through the image API, at medium quality: **AI-generated**, the fold-out camper's double bed, 2 x 2, picked by the owner from two attempts (attempt 2). Exported by `scripts/art/export_sprite.py` at 256 px on its shorter side (1:1) after the background AI had looked at the master. The prompt is in `assets/phobos-vehicles/requests/camp-tents-kitchen.json` and the review in `assets/phobos-vehicles/requests/camp-tents-kitchen-record.md`; the master and its record are on the `art-masters` branch, the other attempt on `art-references`. The only references uploaded were our own approved camp art, for style only (the roof tent kit and awning roll for the kits; the swag and roof tent for the pieces). No game or other author's art was uploaded or used.
The east and south facings are the north one turned by script (90 degrees clockwise and 180 degrees); the game mirrors east for west.

**Facings fixed 25 November 2026 (Framework 0.280.1):** a bed faces from its head to its foot, as the game's own do (HospitalBed_south has its pillow at the top), so the picture first shipped as north, its head at the top, is the south facing. The files were swapped, not exported again: the old north is now south (its export.json follows it), the old south is north, and east is the old east turned 180 degrees, its head on the left; the game mirrors east for west.

- Master: `camp-camper-bed-north-2`, SHA-256 `77d79e39bc09a1157bc88519c8ff6093a50bba30221ca841311487133cf651d2`
- `CamperBed_south.png`: SHA-256 `d0d6ae108d025f3d7764d7765d98d840b261a8804e121a88f36e40dd8fba1bf5` (see its export.json)
- `CamperBed_north.png`: SHA-256 `f450af6e0ff52e467539e719da7eda6c1381168231bd4f6179db721f3e50c380`
- `CamperBed_east.png`: SHA-256 `2958986fc5f8e0011e4aff8be8e17a9349dcdef32ce165d83ecdbdc805979ccd`
