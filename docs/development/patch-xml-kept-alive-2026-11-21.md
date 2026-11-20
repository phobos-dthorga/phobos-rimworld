# Finding: every mod's patch XML kept in memory (21 Nov 2026)

**Status (21 Nov 2026):** found and explained; **nothing is changed**, in our mods or anyone's. What to do (report it, patch it,
or leave it) waits for the owner's word.

## Read this first

- **About 453 MB of the owner's game memory is XML the game meant to let go**: 9.76 million XML objects in 10,499 documents, one
  per patch file of every active mod, live for the whole session.
- **The game clears it; Vanilla Expanded Framework reads it back.** After loading, the game sets every mod's patch list to nothing
  (`LoadedModManager.ClearCachedPatches`). Reading a mod's `ModContentPack.Patches` when the list is empty makes the game load
  that mod's patch files from disk again, and keep them. Vanilla Expanded Framework's settings code reads every running mod's.
- **Not our code, and not a leak that grows**: it is loaded once, at the first such read, and stays.
- **Found with the new heap reader** (phobos-modkit 1.65.0, `python scripts/perf.py look-why`), on the owner's colony, with the
  owner's yes before each snapshot. Not an in-game test of anything.

## Evidence

| Step | What was seen | How |
| --- | --- | --- |
| How much | 453 MB of `System.Xml.*` objects in a 4.8 GB heap; 3.48 million `XmlElement`, 2.84 million `XmlText`, 10,499 `XmlDocument` | Overview of a snapshot (27.4 million objects, 470 ms pause) |
| Live or garbage | **Live**: paths from a root found for both `XmlDocument` and `XmlElement` | `look-why` |
| What holds it | A static field → a mod's `ModContentPack` → its `patches` list (8,192 slots) → a `PatchOperationAdd` → its `XmlContainer.value` → the `XmlElement` → … → its `ownerDoc`, the whole document | `look-why`'s path. The static field the search reached first was Combat Extended's `Controller.content`; it is only a way in to CE's own content pack, not the cause |
| The game lets go | `LoadAllActiveMods` calls `ClearCachedPatches`, which calls `ClearPatchesCache` (`patches = null`) for every running mod | `python scripts/query-code.py show` on the 1.7 decompile |
| Not Loading Progress | Loading Progress transpiles `ClearCachedPatches`, but only to time each mod (ilyvion, [loading-progress](https://github.com/ilyvion/loading-progress), `StartupImpact/Patches/LoadedModManager_ClearCachedPatches_Patches.cs`); the clearing still runs | Its public source |
| Who reads it back | Every installed assembly calling `ModContentPack.get_Patches`, by metadata: the game, Loading Progress and XML Extensions (during loading), and **Vanilla Expanded Framework** after it | phobos-modkit's assembly reader (`calls`), over 713 installed assemblies |

## Where Vanilla Expanded Framework reads every mod's patches

From its public source (Oskar Potocki and the Vanilla Expanded team,
[Vanilla-Expanded/VanillaExpandedFramework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework), clone at `afb6a8f4`,
17 Nov 2026); the installed build's metadata shows the same methods calling it.

| Where | When it runs | Reads |
| --- | --- | --- |
| `ModSettingsFramework/ModSettingsFrameworkMod.cs`, `WriteSettings` (line 76, the loop at 88) | When the settings window of a mod using its settings framework closes | **Every running mod's** patches |
| `ModSettingsFramework/ModSettingsContainer.cs`, `ExposeData` (line 129, the loop at 141) | When those settings are read back (`PostLoadInit`) | **Every running mod's** patches |
| `VEF/Global/Settings/GlobalSettings.cs`, `ToggablePatchesSettings` (line 193) and `AddButtons` (line 249) | When Vanilla Expanded Framework's own "toggleable patches" settings tab is drawn | **Every running mod's** patches, at the first drawing |
| `ModSettingsFrameworkMod.SettingsCategory` (line 100) | When the mod settings list is built | That one mod's own patches |

One snapshot cannot say which of these ran in the owner's session; any one of the first three loads all 10,499 files again.

## What could be done (the owner's choice; nothing done)

| Option | What it gives | Cost and risk |
| --- | --- | --- |
| **Tell Vanilla Expanded's team** (draft for the owner to post) | A fix where it belongs: read the patches it needs once, early, or clear them again after reading | An active team (VEF updated 17 Nov 2026); their timing |
| **A temporary patch in D'thorga Patches** | After each of those reads, set the lists back to nothing, as the game does: about 450 MB back | Must not clear inside a drawing loop (it would load every file each frame); needs a row in `temporary-patches.md`. By our rule another author's bug is theirs unless it is extreme; 450 MB held is not a log storm or lost data |
| **Leave it** | Nothing to maintain | About 450 MB held per session, on a machine with plenty |
