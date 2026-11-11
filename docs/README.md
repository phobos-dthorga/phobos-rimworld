# Documentation

**No mod is published on the Steam Workshop yet.** Seven mods are built and install from source with `scripts/deploy.py`
([installing](installing-mods.md)).

## For players

- [Installing the mods](installing-mods.md): from source today, from the Workshop later, and what to back up first.
- [The medical mods together](medical-mods.md): what happens when a colonist is hurt, and which mod does what.
- [Help and troubleshooting](../SUPPORT.md): where the log is and what a useful report needs.

## For modders

- [Vehicle systems for modders](vehicle-systems-modding.md): give your Vehicle Framework vehicles their own batteries, coolant,
  air brakes or systems of your own, in XML, with D'thorga Framework.

Each mod's README is its player guide, with the gotchas first. **Every mod is RimWorld 1.7 only from 8 November 2026.** As of 7 November 2026, when seven mods became two ([why](development/mod-consolidation.md)):

| Mod | Version | What it does | Guide |
| --- | --- | --- | --- |
| **D'thorga Framework** | 0.89.0 | The shared library: what a pawn breathes, carbon monoxide from fires, the health layer and per-person care choices the medical mods share, and with Vehicle Framework the vehicle systems (battery, coolant, oil, engines and drivetrains, gas turbines, tyres, brakes, wear and service) | [README](../mods/PhobosFramework/README.md) |
| **D'thorga Vehicles** | 0.39.1 | Vehicles that need looking after: the bulk tanker, and named loadouts that give other mods' vehicles the systems they really have | [README](../mods/PhobosVehicles/README.md) |
| **D'thorga Medical** | 0.14.0 | Burns and smoke, emergency dosing, triage, hospital and field equipment, and a safe way to add More Injuries to a running colony: five mods in one | [README](../mods/PhobosMedical/README.md) |
| **D'thorga Factory Additions** | 0.12.2 | Additions to Vanilla Furniture Expanded - Factory: belts from any storage, an automated fabrication bay, a deep hopper, other mods' drug labs as factory bills, and fixes | [README](../mods/PhobosFactoryAdditions/README.md) |
| **D'thorga Patches** | 0.16.7 | The Harrier VTOL, RimThunder's heavy casting, Spaceship Turret Hull under Combat Extended, and InterRim Ballistic Missile's strategic blast | [README](../mods/PhobosPatches/README.md) |
| **D'thorga Water Tap** | 0.2.4 | A Dubs Bad Hygiene fixture that keeps bottled water ready and refills itself | [README](../mods/PhobosWaterTap/README.md) |
| **D'thorga Radio Framework** | 0.1.6 | Hand-held VHF and UHF radios, a base station, coax and antennas; a real link budget decides every contact | [README](../mods/PhobosRadioFramework/README.md) |

## For contributors

The [mods folder](../mods/README.md) explains how a mod folder is laid out. [Development and research](development/README.md) has a design record per mod, the project direction, the radio design, the
build toolchain, the RimWorld modding reference and the writing guide. Read [AGENTS.md](../AGENTS.md) and
[CONTRIBUTING.md](../CONTRIBUTING.md) first.
