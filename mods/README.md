# Mods

Each folder here is one mod, laid out exactly as players receive it, minus its built assemblies (DLLs),
which are build products and never committed.

- **What each mod does, and what it needs:** [the player guide's table and diagram](../docs/player-guide/README.md#the-mods).
- **How to play with them:** [the player guide](../docs/player-guide/README.md).

A mod folder holds:

- `About/About.xml`: `packageId` of the form `PhobosDthorga.<ModName>`, `name`, `author`, `description`,
  `supportedVersions`, `modVersion` (MAJOR.MINOR.PATCH), dependencies and load order;
- `About/Preview.png`, and `About/PublishedFileId.txt` once the game has uploaded it;
- `LoadFolders.xml`, `Common/` and the version folder `1.7/` (built assemblies, never committed; RimWorld 1.7 only);
- `README.md`, opening with **Read this first (gotchas)**, and `CHANGELOG.md`.

`python scripts/check-mod-layout.py` checks all of this. The full layout, and where C# source goes, is in
[building, testing and deploying](../docs/development/building.md).
