# RimWorld modding reference

18 Oct 2026 · agent record

**The short version:** build for RimWorld 1.6 on .NET Framework 4.7.2 with Harmony 2, and keep 1.7 in view.
1.7 runs on Ludeon's new Waylith engine, wants .NET 10 and Harmony 3, and is only on Steam's unstable beta
branch. Ludeon says most 1.6 mods run on 1.7 unchanged.

Each fact names its source. **Observed** means read on the owner's PC on 18 Oct 2026; paths are given in
their generic form. The community wiki is a community source, not Ludeon's.

## Versions

| Fact | Source |
| --- | --- |
| The owner's game is **1.6.4871 rev590**, on Steam's main branch, with Royalty, Ideology, Biotech, Anomaly and Odyssey | Observed (`Version.txt`, `Data/`) |
| **1.7 moves RimWorld from Unity to the Waylith Engine**, Ludeon's own engine. It is on the unstable beta branch; the main branch stays on 1.6 "for now", with no stable date given | [Ludeon, 12 Oct 2026](https://ludeon.com/blog/2026/10/announcing-the-waylith-engine-and-update-1-7/) |
| Ludeon: "The vast majority of mods work on 1.7 without any changes from the authors"; a yellow "potentially unsupported" warning clears when an author tests and declares 1.7 | Ludeon, as above |
| Saves load both ways: 1.6 saves on 1.7, and 1.7 saves back on 1.6 | Ludeon, as above |
| Ludeon claims 2 to 4 times better performance in its tests (2.2 times on a late-game colony; a 1,500-mod load from 14 to 6 minutes); PCs over 10 years old may not run 1.7 | Ludeon, as above |

## Package IDs

| Content | `packageId` | Source |
| --- | --- | --- |
| Core | `Ludeon.RimWorld` | Observed |
| Royalty, Ideology, Biotech, Anomaly, Odyssey | `Ludeon.RimWorld.Royalty`, `.Ideology`, `.Biotech`, `.Anomaly`, `.Odyssey` | Observed |
| Harmony (Workshop item 2009463077) | `brrainz.harmony` | Observed: its `About.xml`, and 38 of the owner's mods name it with that Workshop link |

## About.xml

`About/About.xml` (folder name case-sensitive) has the root `<ModMetaData>`. Source:
[RimWorld Wiki, About.xml](https://rimworldwiki.com/wiki/Modding_Tutorials/About.xml).

- **Required:** `packageId`, `name`, `author` or `authors`, `description`, `supportedVersions`.
- **`packageId` rules:** letters and digits in at least two parts separated by periods, no other special
  characters, no trailing period; globally unique (a duplicate makes only the first mod work);
  case-insensitive, because RimWorld lowercases it. Recommended form: `YourName.YourModName`.
- **Don't rename `name` lightly:** `PatchOperationFindMod` still matches mods by name.
- **Optional:** `modVersion`, `modIconPath` (`About/ModIcon.png` loads automatically from 1.5), `url`,
  `descriptionsByVersion`, `modDependencies` (each `packageId`, `displayName`, `steamWorkshopUrl`;
  `alternativePackageIds` from 1.6), `modDependenciesByVersion`, `loadBefore`, `loadAfter` (and their
  `ByVersion` forms), `forceLoadBefore`, `forceLoadAfter`, `incompatibleWith` (and `ByVersion`).
- `modDependencies` only warns players. **The Workshop does not enforce it**: set the item's Required
  Items on Steam as well. RimSort and other managers may ignore `forceLoad*`.

## Folders and LoadFolders.xml

Source: [RimWorld Wiki, Mod Folder Structure](https://rimworldwiki.com/wiki/Modding_Tutorials/Mod_Folder_Structure).

- `About/` is the only required folder (`About.xml`, `Preview.png` for the mod list and the Workshop,
  optional `ModIcon.png`). Content folders: `Assemblies`, `Defs`, `Patches`, `Languages`, `Textures`,
  `Sounds`.
- **Without `LoadFolders.xml`:** the root and `Common/` load for every version, plus the first version
  folder compatible with the running game (a mod with `1.4` and `1.2` folders loads `1.2` on 1.3).
- **With `LoadFolders.xml`** (mod root): it replaces those rules, so list `/` and `Common` explicitly.
  Entries sit under version tags such as `<v1.6>`. `IfModActive` (any of a comma list),
  `IfModNotActive`, and from 1.6 `IfModActiveAll` gate a folder on other mods or DLCs. When the same
  relative file appears in several loaded folders, the last one wins.
- Textures and sounds are found by path, so a same-path file in a later mod overrides an earlier one. Keep
  ours under a `Phobos` subfolder.
- Observed patterns: Harmony lists `/` plus one folder per version, 1.2 to 1.7. Several of the owner's
  mods point `<v1.7>` at their `1.6` folder, shipping 1.6 assemblies to 1.7 until they make a native build.

## Building C# mods

| | RimWorld 1.6 | RimWorld 1.7 |
| --- | --- | --- |
| Target framework | `net472` | `net10.0` ("the .0 is important") |
| Reference assemblies | [Krafs.Rimworld.Ref](https://www.nuget.org/packages/Krafs.Rimworld.Ref) 1.6.4871 (1 Jul 2026) | Krafs.Rimworld.Ref 1.7.4979-alpha (15 Oct 2026), or Assembly-CSharp, UnityStubs and Waylith from the game |
| Harmony | [Lib.Harmony](https://www.nuget.org/packages/Lib.Harmony) 2.4.3 (11 Oct 2026), compile-time only; players get it from the Harmony mod | Harmony 3.0 (NuGet 3.0.0-rc.3, 15 Oct 2026) |
| Project format | SDK-style | SDK-style required; remove Unity references; `using UnityEngine;` may stay |
| IDE | Any current one | Visual Studio 2026 or Rider 2026; VS2022 and older Rider do not support .NET 10 |

The 1.7 column comes from the community page
[RimWorld 1.7 Mod Updates](https://rimworldwiki.com/wiki/Modding_Tutorials/RimWorld_1.7_Mod_Updates)
(last edited 16 Oct 2026), except the package dates, which come from NuGet. Its other points:

- "99% of mods built for 1.6 should work out of the box", even without a rebuild. Mods that use
  **Prepatcher** stop working until Prepatcher is updated.
- **Harmony 3 type-checks injected parameters** such as `__instance` and `__result`, and rejects a patch
  whose types don't match.
- 1.7 translates Unity calls through a shim (UnityStubs); switching to native Waylith methods should be
  faster.
- **Shaders:** compiled 1.6 shaders in asset bundles still work, but new shaders are GLSL source, compiled
  by the game at load. 1.7 renders with Vulkan and loads the Linux shader variants, so Windows-only
  shaders fail. Asset bundles still work but are "no longer really necessary".
- **Debugging:** Doorstop is no longer needed; attach the IDE to the game process. Developer mode adds a
  Frame Profiler, and the `-mod=` launch argument loads a chosen set of mods (IDs separated by `,` or `;`).
- `Player.log` moves into a `Logs` subfolder.
- Ludeon has an official Modder Primer (a Google Doc linked from that page). It was not read for this record.

## Paths (Windows, Steam)

| What | Where |
| --- | --- |
| Game | `C:\Program Files (x86)\Steam\steamapps\common\RimWorld` by default |
| Local mods | `<game>\Mods\` |
| Workshop mods | `<Steam>\steamapps\workshop\content\294100\<item id>\` |
| Saves, settings, `ModsConfig.xml` | `%USERPROFILE%\AppData\LocalLow\Ludeon Studios\RimWorld by Ludeon Studios\` (`Saves\`, `Config\`) |
| Log | `Player.log` in that folder (`Player-prev.log` is the run before); `Logs\Player.log` on 1.7 |

## Ludeon's EULA on mods

Source: [RimWorld EULA](https://rimworldgame.com/eula/). Quotes are exact.

- "You may create and distribute Mods." A mod must not contain "a substantial part of our Software".
- "Your Mods must be distributed for free, period." Donations may be accepted for modding generally, but
  are not tied to a specific mod and are never advertised inside a mod.
- "Your Mods must not contain any ads or other calls to action of any kind."
- Mods must be clearly presented as community-created, and carry a disclaimer that they are not official
  and not endorsed by Ludeon. They may not be distributed with the base game or its expansions.
- Game resources may be used "as a basis or reference for a Mod", but not ripped out and passed around.
- The author owns the mod, and grants Ludeon a perpetual, irrevocable licence to use and distribute it.
