# Mods

Each folder here is one mod, laid out exactly as players receive it, minus its built assemblies (DLLs),
which are build products and never committed.

| Mod | Folder | What it is |
| --- | --- | --- |
| D’thorga Radio Framework | `PhobosRadioFramework/` | Radio with a real link budget; Phase 1 is local VHF and UHF |
| D’thorga Factory Additions | `PhobosFactoryAdditions/` | Additions and fixes for Vanilla Furniture Expanded - Factory: belts from any storage, an automated fabrication bay, other mods' drug labs as factory bills |
| D’thorga Patches | `PhobosPatches/` | Fixes and patches for other authors' mods: the Harrier VTOL, RimThunder's heavy casting, Spaceship Turret Hull under Combat Extended, InterRim Ballistic Missile's strategic blast. D'thorga Vehicle Patches and Missile Patches until 7 November 2026 |
| D’thorga Water Tap | `PhobosWaterTap/` | A plumbing fixture for Dubs Bad Hygiene that keeps bottled water ready and refills itself from your water towers |
| D’thorga Framework | `PhobosFramework/` | The shared library: what a pawn breathes from the game's and other mods' gases, additive room gases of our own (carbon monoxide first), the helpers every D'thorga mod shares, and with Vehicle Framework the vehicle systems (battery, coolant, oil, tyres, brakes, wear and service) |
| D’thorga Vehicles | `PhobosVehicles/` | Vehicles that need looking after. A stand-in for now: named loadouts that give other mods' vehicles the systems they really have (air brakes, 24-volt Humvee, gas-turbine tanks); our own utility vehicles come later |
| D’thorga Medical | `PhobosMedical/` | Burns and smoke, emergency dosing, triage, hospital and field equipment, and the save bridge that adds More Injuries to a running colony. Five mods until 7 November 2026, each now a part with its own section |

A mod folder holds:

- `About/About.xml`: `packageId` of the form `PhobosDthorga.<ModName>`, `name`, `author`, `description`,
  `supportedVersions`, `modVersion` (MAJOR.MINOR.PATCH), dependencies and load order;
- `About/Preview.png`, and `About/PublishedFileId.txt` once the game has uploaded it;
- `LoadFolders.xml`, `Common/` and the version folders `1.7/` and `1.6/` (built assemblies, never committed);
- `README.md`, opening with **Read this first (gotchas)**, and `CHANGELOG.md`.

`python scripts/check-mod-layout.py` checks all of this. The full layout, and where C# source goes, is in
[building, testing and deploying](../docs/development/building.md).
