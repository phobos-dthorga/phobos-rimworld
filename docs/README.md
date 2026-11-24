# Documentation

Where to start, by who you are. **No mod is published on the Steam Workshop yet**; all eight install from source.

## For players

**[The player guide](player-guide/README.md)** is where players start: what each mod does and needs, how to install, and how to
play, a chapter per mod. [Help and troubleshooting](../SUPPORT.md) says where the game's log is and what a useful report needs.

## For modders

- [D'thorga Framework for modders](framework-modding.md): the shared library's helpers outside vehicles, for your own mods.
- [Vehicle systems for modders](vehicle-systems-modding.md): give your Vehicle Framework vehicles their own batteries, coolant,
  air brakes or systems of your own, in XML, with D'thorga Framework.
- [For modders, mod by mod](modding/README.md): extending each of the other D'thorga mods, with its checks done.
- [The reference site](https://phobos-dthorga.github.io/phobos-rimworld/): D'thorga Framework's types, with XML examples.

## For contributors

- [Development and research](development/README.md): the design records, the project direction, the build toolchain, the
  RimWorld modding reference and the writing guide.
- [How the code is built](development/architecture.md): layers, ticks, tie-ins, saves and removal, settings and tests.
- Read [AGENTS.md](../AGENTS.md) and [CONTRIBUTING.md](../CONTRIBUTING.md) first; [the mods folder](../mods/README.md) explains
  how a mod folder is laid out.

## Mod versions

Every mod is RimWorld 1.7 only. These numbers are kept in step with each mod's About file by `scripts/release.py`.

| Mod | Version | README |
| --- | --- | --- |
| **D'thorga Framework** | 0.284.0 | [README](../mods/PhobosFramework/README.md) |
| **D'thorga Vehicles** | 0.118.0 | [README](../mods/PhobosVehicles/README.md) |
| **D'thorga Medical** | 0.26.1 | [README](../mods/PhobosMedical/README.md) |
| **D'thorga Factory Additions** | 0.14.2 | [README](../mods/PhobosFactoryAdditions/README.md) |
| **D'thorga Patches** | 0.41.0 | [README](../mods/PhobosPatches/README.md) |
| **D'thorga Farming** | 0.7.9 | [README](../mods/PhobosFarming/README.md) |
| **D'thorga Water Tap** | 0.2.4 | [README](../mods/PhobosWaterTap/README.md) |
| **D'thorga Radio Framework** | 0.16.1 | [README](../mods/PhobosRadioFramework/README.md) |
