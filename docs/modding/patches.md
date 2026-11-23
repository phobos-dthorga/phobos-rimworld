# D'thorga Patches for modders

Where to read how each of D'thorga Patches' parts works in the patched mod's code, and why it was built that way. Players start at
[the mod's README](../../mods/PhobosPatches/README.md) and [the player guide](../player-guide/patches.md). The patches themselves
sit in the mod's `Mods/` folders, one per part, each loading only while its mod is active; the records below are for authors of the
patched mods and for anyone taking a patch over.

## The records

- [Vehicle patches: research and decisions](../development/archive/vehicle-patches.md): the Harrier, bombs, fabricator, forger and
  turret hulls, with the code paths and the source files.
- [Missile patches: research and decisions](../development/archive/missile-patches.md) and [strategic
  strike](../development/archive/missile-patches.md#strategic-strike-design-and-decisions-7-nov-2026): the blast and its queue.
- [RimPacts patches](../development/archive/rimpacts-patches.md): the Harrier's load fix, sorties over battles, struck towns'
  assaults.
- [Visit Settlements patches](../development/archive/visit-settlements-patches.md): the battle map that closed, and the food
  drops.
- [Close Air Support's A-10 and Su-25](../development/archive/close-air-support-jets.md), [UH-60M Black
  Hawk](../development/archive/uh60-blackhawk.md) and [C-17 Globemaster III](../development/archive/c17-globemaster.md): each
  aircraft brought into the fold.
- [Soldiers keep their guns](../development/soldiers-keep-their-guns.md), [mech rechargers that draw real
  power](../development/archive/mech-recharging-real-power.md) and [drones called
  home](../development/archive/drone-recall-design.md).
- [The register of temporary patches](../development/temporary-patches.md): what would make each temporary part unnecessary.

## Checks done

Every part was read in the patched mods' code and files and checked offline (builds, and `scripts/check-factory-patches.py` for
the recipe patches). Each version's checks are in the [changelog](../../mods/PhobosPatches/CHANGELOG.md), and the dated list the
README kept until 17 November 2026 is in [Vehicle
patches](../development/archive/vehicle-patches.md#checks-moved-from-the-readme-17-nov-2026). What the owner has passed in game
ends [the guide's Patches chapter](../player-guide/patches.md#tested-in-game); everything else is **not yet tested in game**.
