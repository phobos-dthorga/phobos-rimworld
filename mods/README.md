# Mods

Each folder here is one mod, laid out exactly as players receive it, minus its built assemblies (DLLs),
which are build products and never committed.

| Mod | Folder | What it is |
| --- | --- | --- |
| D’thorga Radio Framework | `PhobosRadioFramework/` | Radio with a real link budget; Phase 1 is local VHF and UHF |
| D’thorga Factory Additions | `PhobosFactoryAdditions/` | Additions and fixes for Vanilla Furniture Expanded - Factory: belts from any storage, an automated fabrication bay, other mods' drug labs as factory bills |
| D’thorga Vehicle Patches | `PhobosVehiclePatches/` | Patches for other authors' vehicle mods: the Harrier VTOL, RimThunder's heavy casting, and Spaceship Turret Hull under Combat Extended |
| D’thorga Medical Save Bridge | `PhobosMedicalSaveBridge/` | Adds More Injuries to a running colony, with MSE2 or without, by re-numbering saved body parts on the first load |
| D’thorga Water Tap | `PhobosWaterTap/` | A plumbing fixture for Dubs Bad Hygiene that keeps bottled water ready and refills itself from your water towers |
| D’thorga Framework | `PhobosFramework/` | The shared library: what a pawn breathes from the game's and other mods' gases, additive room gases of our own (carbon monoxide first), and the helpers every D'thorga mod shares |
| D’thorga Medical Equipment | `PhobosMedicalEquipment/` | Bedside oxygen (a concentrator, and a stand that burns Vanilla Gravship Expanded's canisters) for patients in linked beds, and a burn unit bed |
| D’thorga Emergency Dosing | `PhobosDosing/` | Doctors give painkillers and sedatives on their own, from any mod's drugs, found by their effects |
| D’thorga Burns and Smoke | `PhobosBurnsAndSmoke/` | Burned body fraction, burn shock, airway burns with a tracheotomy, smoke-damaged lungs and carbon monoxide, beside More Injuries and Combat Extended |
| D’thorga Triage | `PhobosTriage/` | Doctors queue the life-saving operations a patient needs, by what each condition does, and take the worst case first |

A mod folder holds:

- `About/About.xml`: `packageId` of the form `PhobosDthorga.<ModName>`, `name`, `author`, `description`,
  `supportedVersions`, `modVersion` (MAJOR.MINOR.PATCH), dependencies and load order;
- `About/Preview.png`, and `About/PublishedFileId.txt` once the game has uploaded it;
- `LoadFolders.xml`, `Common/` and the version folders `1.7/` and `1.6/` (built assemblies, never committed);
- `README.md`, opening with **Read this first (gotchas)**, and `CHANGELOG.md`.

`python scripts/check-mod-layout.py` checks all of this. The full layout, and where C# source goes, is in
[building, testing and deploying](../docs/development/building.md).
