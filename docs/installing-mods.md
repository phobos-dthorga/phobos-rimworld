# Installing the mods

**Nothing is on the Steam Workshop yet.** The radio framework is in development and installs from source:
build it with the .NET SDK, then `python scripts/deploy.py --apply PhobosRadioFramework` with RimWorld
closed ([building](development/building.md)). **Code → Download ZIP** gives you source, not a built mod,
because the assembly is not committed.

## How it will work

When a mod is published:

1. **Subscribe on the Steam Workshop.** Steam downloads it, and RimWorld lists it in **Mods**.
2. **Subscribe to Harmony** if the mod needs it (most C# mods do). Its README says so.
3. **Turn it on in the Mods menu** (or in RimSort), below Harmony, Core and the DLCs, and below any mod it
   names as a requirement. Its README gives the load order.
4. **Read its "Read this first" section** before loading a save: some settings are best chosen before you
   start a colony, and the README says whether the mod is safe to add to a running save.

## Versions

- Mods are built for **RimWorld 1.6**, Steam's main branch.
- **RimWorld 1.7** (the Waylith engine) is on Steam's unstable beta branch. A mod declares 1.7 only once it
  has been tested there.

## Before you add or remove any mod

- **Back up your save first.** Saves are in
  `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\Saves`.
- Removing a mod that adds things to the world can break a save. Each README says whether removal is safe
  and what to do first.

Problems? See [help and troubleshooting](../SUPPORT.md).
