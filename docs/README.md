# Documentation

**No mod is published on the Steam Workshop yet.** Ten mods are built and install from source with `scripts/deploy.py`
([installing](installing-mods.md)).

## For players

- [Installing the mods](installing-mods.md): from source today, from the Workshop later, and what to back up first.
- [The medical mods together](medical-mods.md): what happens when a colonist is hurt, and which mod does what.
- [Help and troubleshooting](../SUPPORT.md): where the log is and what a useful report needs.

Each mod's README is its player guide, with the gotchas first. As of 6 November 2026:

| Mod | Version | What it does | Guide |
| --- | --- | --- | --- |
| **D'thorga Framework** | 0.3.0 | The shared library: what a pawn breathes, carbon monoxide from fires, the health layer and per-person care choices the medical mods share | [README](../mods/PhobosFramework/README.md) |
| **D'thorga Burns and Smoke** | 0.5.0 | Burned body fraction, burn shock, airway burns with a tracheotomy, smoke-damaged lungs, carbon monoxide, and fires that heat the air around them in the open | [README](../mods/PhobosBurnsAndSmoke/README.md) |
| **D'thorga Emergency Dosing** | 0.3.1 | Doctors give painkillers, sedatives and disease-fighting drugs on their own, found by their effects; colonists in pain take what their policy allows | [README](../mods/PhobosDosing/README.md) |
| **D'thorga Medical Equipment** | 0.2.2 | Bedside oxygen and a burn unit bed | [README](../mods/PhobosMedicalEquipment/README.md) |
| **D'thorga Triage** | 0.1.1 | Doctors queue the life-saving operations a patient needs, and take the worst case first | [README](../mods/PhobosTriage/README.md) |
| **D'thorga Medical Save Bridge** | 0.1.2 | Adds More Injuries to a running colony, with Medical System Expansion 2 or without | [README](../mods/PhobosMedicalSaveBridge/README.md) |
| **D'thorga Factory Additions** | 0.11.2 | Additions to Vanilla Furniture Expanded - Factory: belts from any storage, an automated fabrication bay, a deep hopper, other mods' drug labs as factory bills, and fixes | [README](../mods/PhobosFactoryAdditions/README.md) |
| **D'thorga Vehicle Patches** | 0.5.0 | The Harrier VTOL, RimThunder's heavy casting, and Spaceship Turret Hull under Combat Extended | [README](../mods/PhobosVehiclePatches/README.md) |
| **D'thorga Water Tap** | 0.2.1 | A Dubs Bad Hygiene fixture that keeps bottled water ready and refills itself | [README](../mods/PhobosWaterTap/README.md) |
| **D'thorga Radio Framework** | 0.1.2 | Hand-held VHF and UHF radios, a base station, coax and antennas; a real link budget decides every contact | [README](../mods/PhobosRadioFramework/README.md) |

## For contributors

The [mods folder](../mods/README.md) explains how a mod folder is laid out. [Development and research](development/README.md) has a design record per mod, the project direction, the radio design, the
build toolchain, the RimWorld modding reference and the writing guide. Read [AGENTS.md](../AGENTS.md) and
[CONTRIBUTING.md](../CONTRIBUTING.md) first.
