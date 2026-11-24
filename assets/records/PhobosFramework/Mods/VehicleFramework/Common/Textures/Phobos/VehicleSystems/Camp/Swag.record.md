# Swag

Made with ComfyUI, local, FLUX.2 Klein 4B base (Black Forest Labs, Apache 2.0) with our private house style model (house-v1, trained on our own picks), on 21 November 2026: **AI-generated**, one try, picked by the owner the same evening. The prompt is in `assets/vehicle-systems/requests/camp-gear-house.json`; the batch report is `assets/vehicle-systems/generated/run-20261121-2142.md`. Only our own shipped art was used as references, for style; no game or other author's art. Exported by `scripts/art/export_sprite.py` at 128 px a cell (128 x 256 for its 1 x 2 footprint), the background cleared with the background AI's mask. The east and south facings are the north one turned by script (90 degrees clockwise and 180 degrees); the game mirrors east for west. Re-exported 23 November 2026 with the soft-rim fix (no pale ring outside the outline), the east and south turned again from the new north.

**Facings fixed 25 November 2026 (Framework 0.280.1):** a bed faces from its head to its foot, as the game's own do (HospitalBed_south has its pillow at the top), so the picture first shipped as north, its head at the top, is the south facing. The files were swapped, not exported again: the old north is now south (its export.json follows it), the old south is north, and east is the old east turned 180 degrees, its head on the left; the game mirrors east for west.

- Master: `camp-swag-11.png`, SHA-256 `b77942f0c40b6fd5fadbf22bb4199173e6bfcfe1bd26684622654a12d9201ba6`
- `Swag_south.png`: SHA-256 `935379ccab4eee5eca092fdf1ae4e865ac07b22cfd92edc650df0ecf092c4ac9` (see its export.json)
- `Swag_north.png`: SHA-256 `f2ab909de7b3f9cbc113900737e6ce220a595b117df8acfa1c4b389c63830ea5`
- `Swag_east.png`: SHA-256 `5a0bcb9c4f257cbb8f81d9e4c0d762112cd9bd8fdbd105dceeb4117533014058`
