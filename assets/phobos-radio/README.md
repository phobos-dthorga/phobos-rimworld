# Radio framework artwork records

Every image the mod ships is listed here with how it was made. Nothing is taken from the game or from other mods. Since
7 Nov 2026 some are generated art the owner picked; the record beside each says how it was made.

| File | Made by | How | Date |
| --- | --- | --- | --- |
| `mods/PhobosRadioFramework/About/Preview.png` (640 × 360) | owner's pick, 7 Nov 2026 | `workshop-preview-3` (Flux.1-schnell, 19 Oct 2026), the name added by `scripts/art/make_preview.py`; see `Preview.record.md` | 7 Nov 2026 |
| `Common/Textures/Phobos/Radio/Things/BaseStation.png` (256), `Whip.png` (192 x 288), `Mast.png` (384 x 576) | owner's picks, 24 Nov 2026 (Radio 0.4.0) | paid (OpenAI gpt-image-2) from [requests/paid-1.json](requests/paid-1.json) and [requests/paid-2-antennas.json](requests/paid-2-antennas.json), exported by `scripts/art/export_sprite.py`; [batch record](requests/paid-1-record.md), a record beside each | 24 Nov 2026 |
| `Common/Textures/Phobos/Radio/Things/Handheld.png`, `UI/Signal.png`, `UI/CoaxIcon.png` | owner's picks, 7 Nov 2026 | generated, exported by `scripts/art/export_sprite.py`; a record beside each | 7 Nov 2026 |
| `Common/Textures/Phobos/Radio/Things/BaseStation.png` (256), **replaced 24 Nov 2026** by the paid pick above | owner's pick, 12 Nov 2026 (Radio 0.1.7) | generated in night 3 (`base-station-remake-2-13`, from [requests/remake-2.json](requests/remake-2.json)), exported by `scripts/art/export_sprite.py`; see `BaseStation.record.md` | 8 Nov 2026 |
| `Common/Textures/Phobos/Radio/Linked/Coax_Atlas.png` and `Coax_Blueprint_Atlas.png` (256, 4 × 4 linked tiles) | agent | `make_textures.py` in this folder: flat shapes in the mod's palette, pure Python, reproducible | 19 Oct 2026 |

Placeholders are marked as such in the changelog and replaced before the public beta.
