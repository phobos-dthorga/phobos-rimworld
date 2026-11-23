# Records of what the mods ship

How each picture and sound in `mods/` was made: one `.record.md` per file (or per family of files), and the exporters'
`.export.json` (what was cut from which master, with its hashes), kept here in the same place
it would have under `mods/`, so `mods/PhobosVehicles/Textures/X/X.png` has its record at
`assets/records/PhobosVehicles/Textures/X/X.record.md`. They moved here from beside their files on 23 Nov 2026 (the owner's
choice in the docs rewrite), because deploy copies a mod's folder whole and the records went into the game's Mods folder with it.

- **Write a record with `scripts/art/art_paths.py`'s `record_path`**, given the path the record would have beside its file: it
  gives this folder's place and makes the folders. `records_under` and `shipped_records` find records wherever they are kept.
- **The art audit reads them here** (`python scripts/art-audit.py`; `config/art-audit.json`'s `records.mirrors`), and still finds
  a record left beside its file.
- The exporters' `.export.json` records followed the same day (the owner's word: "Move the export.json files too"), through
  the same helpers.
