# Mods

Each folder here is one mod, laid out exactly as players receive it, minus its built assemblies (DLLs),
which are build products and never committed.

| Mod | Folder | What it is |
| --- | --- | --- |
| D’thorga Radio Framework | `PhobosRadioFramework/` | Radio with a real link budget; Phase 1 is local VHF and UHF |
| D’thorga Factory Additions | `PhobosFactoryAdditions/` | XML additions to Vanilla Furniture Expanded - Factory |

A mod folder holds:

- `About/About.xml`: `packageId` of the form `PhobosDthorga.<ModName>`, `name`, `author`, `description`,
  `supportedVersions`, `modVersion` (MAJOR.MINOR.PATCH), dependencies and load order;
- `About/Preview.png`, and `About/PublishedFileId.txt` once the game has uploaded it;
- `LoadFolders.xml`, `Common/` and version folders such as `1.6/`;
- `README.md`, opening with **Read this first (gotchas)**, and `CHANGELOG.md`.

`python scripts/check-mod-layout.py` checks all of this. The full layout, and where C# source goes, is in
[building, testing and deploying](../docs/development/building.md).
