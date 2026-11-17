# Documentation

An index of the documents. **No mod is published on the Steam Workshop yet**; all seven install from source
([getting started](player-guide/getting-started.md)). What each mod does, and how they depend on one another, is in
[the project README](../README.md#the-mods).

## Where to look

| You want | Read | It is the authority for |
| --- | --- | --- |
| How to play | [The player guide](player-guide/README.md), a chapter per mod, starting with [getting started](player-guide/getting-started.md) | How to install, build, press and play |
| One mod's requirements, settings and figures | That mod's README, linked from the list below | Requirements, settings, figures and what is tested |
| What changed, and when | That mod's `CHANGELOG.md`, beside its README | Version history |
| Why something works as it does | [Development and research](development/README.md): a design record per mod and feature | Designs, research and decisions |

## For players

- **[The player guide](player-guide/README.md)**: start here.
- [The medical mods together](medical-mods.md): what happens when a colonist is hurt, and which part does what.
- [Help and troubleshooting](../SUPPORT.md): where the log is and what a useful report needs.

## For modders

- [D'thorga Framework for modders](framework-modding.md): the shared library's helpers outside vehicles, for your own mods.
- [Vehicle systems for modders](vehicle-systems-modding.md): give your Vehicle Framework vehicles their own batteries, coolant,
  air brakes or systems of your own, in XML, with D'thorga Framework.

## For contributors

Read [AGENTS.md](../AGENTS.md) and [CONTRIBUTING.md](../CONTRIBUTING.md) first. The [mods folder](../mods/README.md) explains how
a mod folder is laid out. [Development and research](development/README.md) indexes the design records, the project direction,
the build toolchain, the RimWorld modding reference and the writing guide.

## Mod versions

Every mod is RimWorld 1.7 only. These numbers are kept in step with each mod's About file by `scripts/release.py`.

| Mod | Version | README |
| --- | --- | --- |
| **D'thorga Framework** | 0.122.1 | [README](../mods/PhobosFramework/README.md) |
| **D'thorga Vehicles** | 0.61.1 | [README](../mods/PhobosVehicles/README.md) |
| **D'thorga Medical** | 0.17.0 | [README](../mods/PhobosMedical/README.md) |
| **D'thorga Factory Additions** | 0.13.0 | [README](../mods/PhobosFactoryAdditions/README.md) |
| **D'thorga Patches** | 0.21.1 | [README](../mods/PhobosPatches/README.md) |
| **D'thorga Water Tap** | 0.2.4 | [README](../mods/PhobosWaterTap/README.md) |
| **D'thorga Radio Framework** | 0.1.8 | [README](../mods/PhobosRadioFramework/README.md) |
