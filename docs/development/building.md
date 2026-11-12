# Building, testing and deploying

**Status (8 Nov 2026): fourteen C# projects, RimWorld 1.7 only** (owner, 8 Nov 2026). Every project builds one assembly,
`net10.0` against the 1.7.4998 reference (Harmony 3 where a mod patches), into `mods/<Mod>/1.7/Assemblies/`. A tie-in assembly
builds into `mods/<Mod>/Mods/<Other>/1.7/Assemblies/` instead. Other mods that still ship only a 1.6 build (Vehicle Framework,
VFE Factory, InterRim Ballistic Missile) are referenced from that folder, which the 1.7 game loads too.
`Directory.Build.targets` at the root turns the target into the game version, the output folder, the game and Harmony
references and the Workshop folders of other mods' assemblies, so a project file only names its mod folder (`ModFolder`), its
tie-in folder if any (`TieInFolder`) and whether it patches (`UsesHarmony`). `python scripts/deploy.py --build --apply --all`
builds every project and installs every mod, with RimWorld closed.

**Shared tools come from a submodule.** [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit) is pinned at
`external/phobos-modkit`; after cloning, run `git submodule update --init` once. `scripts/triage-log.py` uses it. A fix to a
shared tool goes to phobos-modkit first; then the pin moves here with `git -C external/phobos-modkit checkout <tag>` and a
commit. Its Rust programs build with `python scripts/query-save.py --build-indexer` into `%LOCALAPPDATA%\phobos-modkit\target`,
outside this cloud-synced folder, and save indexes stay under the system's temp folder for the same reason: they hold data
from the owner's saves.

## The projects

| Project (`src/`) | Builds into (`mods/`, per version) | References besides the game | Harmony | Rule tests (`tests/`) | On GitHub |
| --- | --- | --- | --- | --- | --- |
| `PhobosFramework` | `PhobosFramework/` | nothing | no | `PhobosFramework.Rules.Tests` | builds and tests |
| `PhobosMedical` | `PhobosMedical/` | the framework, VEF's `PipeSystem.dll` (Vanilla Gravship Expanded through Defs only; More Injuries by name at run time) | yes | `PhobosMedical.Rules.Tests` | builds and tests |
| `PhobosFactory` | `PhobosFactoryAdditions/` | VEF's `PipeSystem.dll`, VFE Factory's `VanillaFurnitureExpandedFactory.dll` | yes | `PhobosFactory.Rules.Tests` | tests only |
| `PhobosFactory.Hygiene` | `PhobosFactoryAdditions/Mods/DubsBadHygiene/` | the factory core, the framework's hygiene tie-in, `PipeSystem.dll`, Dubs Bad Hygiene's `BadHygiene.dll` | yes | none | no |
| `PhobosFramework.Hygiene` | `PhobosFramework/Mods/DubsBadHygiene/` | Dubs Bad Hygiene's `BadHygiene.dll`: the shared plumbing reader | no | none | no |
| `PhobosMedical.UsefulMarks` | `PhobosMedical/Mods/UsefulMarks/` | the medical core, Useful Marks' `UsefulColonistBar.dll` | no | none | no |
| `PhobosMedical.Hygiene` | `PhobosMedical/Mods/DubsBadHygiene/` | the medical core, the framework's hygiene tie-in, `BadHygiene.dll` | no | none | no |
| `PhobosFramework.Vehicles` | `PhobosFramework/Mods/VehicleFramework/` (1.7 only) | the framework, Vehicle Framework's `Vehicles.dll` and `SmashTools.dll`: the vehicle systems | yes | `PhobosFramework.Vehicles.Rules.Tests` | tests only |
| `PhobosFramework.Vehicles.Outposts` | `PhobosFramework/Mods/VehiclesOutposts/` (with Vehicle Framework and Vanilla Expanded Framework both active) | the vehicle systems, the framework, VEF's `Outposts.dll`, Vehicle Framework's `Vehicles.dll` and `SmashTools.dll` | no | `PhobosFramework.Rules.Tests` | tests only |
| `PhobosFramework.Vehicles.Hygiene` | `PhobosFramework/Mods/VehiclesHygiene/` (with Vehicle Framework and Dubs Bad Hygiene both active) | the vehicle systems, the framework's hygiene tie-in, `Vehicles.dll`, `BadHygiene.dll`: vehicles' water from plumbing | no | none | no |
| `PhobosPatches.Harrier` | `PhobosPatches/Mods/HarrierVtol/` | the framework and its vehicle systems, Vehicle Framework's `Vehicles.dll`, `SmashTools.dll`, `UpdateLogTool.dll` | yes | none | no |
| `PhobosPatches.Outposts` | `PhobosPatches/Mods/Outposts/` (with Vanilla Expanded Framework and Vehicle Framework both active) | the framework, VEF's `Outposts.dll`, Vehicle Framework's `Vehicles.dll` and `SmashTools.dll` | yes | none | no |
| `PhobosPatches.PrisonLabor` | `PhobosPatches/Mods/PrisonLabor/` (temporary, while Prison Labor needs the guard) | the framework; finds Prison Labor by name | no | none | none |
| `PhobosPatches.VehicleFramework` | `PhobosPatches/Mods/VehicleFramework/` (to be removed when the framework handles fires itself) | the framework, Vehicle Framework's `Vehicles.dll` and `SmashTools.dll` | yes | `PhobosPatches.Rules.Tests` | tests only |
| `PhobosPatches.CombatExtended` | `PhobosPatches/Mods/CombatExtended/` (temporary, while an ammo set can hold empty entries) | the framework; finds Combat Extended by name | no | none | none |
| `PhobosPatches.Irbm` | `PhobosPatches/Mods/IRBM/` | the framework, InterRim Ballistic Missile's `IRBM.dll` | yes | `PhobosPatches.Rules.Tests` | tests only |
| `PhobosWaterTap` | `PhobosWaterTap/` | Dubs Bad Hygiene's `BadHygiene.dll` | no | `PhobosWaterTap.Rules.Tests` | tests only |
| `PhobosRadio` | `PhobosRadioFramework/` | VEF's `PipeSystem.dll` | no | `PhobosRadio.Physics.Tests` | tests only |

- **D'thorga Medical is one project with a folder per former mod** (`BurnsAndSmoke/`, `Dosing/`, `Triage/`, `Equipment/`, `SaveBridge/`), each keeping its namespace, because saves name classes by namespace (7 November 2026, [why](mod-consolidation.md)).
- **No project's DLL is copied beside another mod's**: `Directory.Build.targets` makes every project reference `Private=false` by default, transitive ones included. Before 7 November 2026 a transitive copy of `PhobosFramework.dll` shipped in Medical Equipment's Dubs Bad Hygiene folder.
- **Other mods' assemblies are referenced from the installed mod and never copied** (`Private=false`): their licences do not
  let us redistribute them (VEF is CC BY-NC-ND 4.0; VFE Factory and Dubs Bad Hygiene publish none). That is also why the
  projects that reference them cannot build on GitHub; their game-free rules still run there.
- **The medical projects reference D'thorga Framework with `ProjectReference ... Private="false"`**, so building one builds the
  framework first and the framework's DLL ships only in its own mod.
- **Build before deploying**: `deploy.py` copies whatever is in the mod folder and says "no assembly built" when a DLL is missing.
  `--build` builds every project, tie-ins included.
- **Rule tests compile only `src/<Project>/Rules/*.cs`** (the physics project its own folder), so they need neither the game nor
  any mod and run anywhere the .NET SDK does.
- Water Tap's texture and preview are generated art the owner picked (7 Nov 2026); `assets/water-tap/draw_tap.py` draws only the tap's layout reference.

### History

- **18 Oct 2026**: the 1.6 setup was proven by a throwaway probe mod (the tables below come from it).
- **19 Oct**: the radio framework's Phase 1 built (0.1.0 Draft). **29 Oct**: Factory Additions' assembly, the first with
  Harmony. **2 Nov**: the Dubs Bad Hygiene tie-in and Vehicle Patches' Harrier tie-in, the first tie-in assemblies. **3 Nov**:
  Medical Save Bridge. **4 Nov**: Water Tap, D'thorga Framework, Burns and Smoke, and the switch to **1.7 leading** with every
  project built for both versions. **5 Nov**: Emergency Dosing and Medical Equipment. **6 Nov**: Triage. **8 Nov**: the owner
  retired 1.6 support; everything new is 1.7 only. The same night: D'thorga Framework's vehicle systems, the first single-target (1.7)
  project. **8 Nov**: every project and mod dropped 1.6 (owner): one target each, no PolySharp, no `1.6` load folders.
- Vehicle Framework targets .NET Framework 4.8 and Vehicle Patches 4.7.2, so that project sets
  `ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch`; the game loads both whatever they declare.

## The checks (run before every commit)

```text
python scripts/check-doc-links.py
python scripts/check-mod-layout.py
python -m unittest discover -s tests
dotnet test tests/<Project>             each rule-test project whose rules you changed
python scripts/check-factory-patches.py (needs the installed game; for Factory Additions and its tie-ins)
python scripts/check-vehicle-schemas.py (offline; for the vehicle systems' systems and loadouts)
python scripts/deploy.py --build --apply --all   (with RimWorld closed)
```

The first three are the ones AGENTS.md requires before every commit; gate on their exit codes. **On GitHub**
(`.github/workflows/checks.yml`) every push runs those three, every rule-test project, and builds for 1.7 every
project that references only the game, D'thorga Framework and Harmony. `check-factory-patches` and `deploy` need the installed
game and run only on a machine with RimWorld.

- **check-doc-links** finds relative Markdown links whose target file is missing. It does not check `#heading` fragments.
- **check-mod-layout** checks every `mods/<Mod>/` folder:
  - `About/About.xml` has the required fields (including an author and `supportedVersions`), a `packageId` of the form
    `PhobosDthorga.<ModName>`, and **Ludeon's EULA disclaimer word for word** in the description;
  - package IDs are unique, and `modVersion` is MAJOR.MINOR.PATCH;
  - a `README.md` and `CHANGELOG.md` exist;
  - every folder `LoadFolders.xml` names exists and is not empty;
  - no two folders in one version block hold a Defs or Patches file at the same path (the game would load only the one in the
    folder listed last);
  - no Keyed string uses a numbered placeholder such as `{0}`: name it, as in `{COUNT}`;
  - no build products (DLLs, `bin/`, `obj/`) are present.
- **check-factory-patches** reads the installed game, the active mods (ModsConfig.xml) and the Workshop folder, never writing to
  them, and checks D'thorga Factory Additions (or `--mod <Name>`) against them: no file is skipped for sharing a path with a
  later folder's file, every patch xpath matches one node, every Def name a ProcessDef uses exists, every hopper-only bill has an
  output cap within its product's stack limit, defined bills match the bills patched onto machines, every ParentName resolves,
  and every bench bill template names a machine, benches, research, sound and effect that exist. Paths as for deploy.py
  (`--game`, `--workshop`, `--config`, the environment variables or `.local/install-settings.json`). An offline check is not an
  in-game test.
- **The unit tests** (`tests/test_*.py`) exercise every script in `scripts/` and `scripts/art/` against synthetic folders.

### Other scripts

| Script | What it does | Flags |
| --- | --- | --- |
| `scripts/triage-log.py` | Summarises `Player.log` against `Player-prev.log`: messages grouped into signatures, one exception worded several ways shown together, the mods involved named, and notices for the save loaded and the log limit. Reads only; works while the game runs. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-log tool | `--show ID`, `--previous`, `--no-previous`, `--top`, `--levels`, `--json`, `--logs`, `--game`, `--workshop`, `--no-mods`, `--no-redact` |
| `scripts/workshop-research.py` | What players report about mods on the Workshop lately: `report <mods>` (update date, declared versions, recent comments, the uploader's last reply, the newest comments that mention problems), `versions` (active Workshop mods not declaring the game's version), `details`, `comments`, `sql`. Reads Steam's public pages only, paced 8 s apart for comments and cached for 24 hours. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-workshop tool | report name first, then mods by name, package ID or Workshop ID; `--max-age`, `--refresh`, `--offline`, `--days`, `--top`, `--json` |
| `scripts/query-defs.py` | Which active mods define and patch each Def, from an index of the installed game: `summary`, `who <defName>` (every definition, the one the game keeps, every patch naming it), `show` (XML and parents), `list`, `patches`, `mods`, `sql`. Indexes about 340 mods in about 8 s and rebuilds itself when the mod list, a Workshop item, the game or a local mod changes; reports then take about 0.3 s. Defs are shown as written, without other mods' patches. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-defs tool | report name first, then `--top`, `--json`, `--rebuild`, the report's filters (`--help` on each report) |
| `scripts/query-save.py` | Answers questions about a save from an index: `summary`, `pawns`, `hediffs`, `things`, `find <ID>`, `grid` (terrain, roofs, gas, fog, snow, pollution and deep resources per map, a text picture, or one cell), `world` (settlements, sites and caravans by faction; `--id` for one object's values, such as an aircraft's flight path), `components` (the game's, world's and maps' components, often mods' saved state, value by value), `diff <other save>` (with `--component` to compare one mod's state), `schema`, `sql`. The first question indexes a frozen copy of the save (about 2.5 s for 300 MB), deleted straight afterwards; later ones reuse the index until the save changes. Build its Rust indexer once with `--build-indexer`. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-save tool | report name first, then `--save`, `--top`, `--json`, the report's filters (`--help` on each report) |
| `scripts/refresh-mod-list.py` | Rewrites [the owner's mod list](owner-mod-list.md) from the game's files, reading only | `--online` (re-read the Vanilla Expanded collection), `--check` (report, write nothing), `--game`, `--workshop`, `--config`, `--doc` |
| `scripts/generate-art.py` | Runs a texture request through the local ComfyUI ([artwork direction](artwork-direction.md)) | `request_file`, `--only`, `--start`, `--comfy` |
| `scripts/art-run.py` | Generates, trains or runs an overnight art batch, stopping safely | `generate`, `train` or `night`, then `source`; `--stop-at`, `--only`, `--comfy`, `--min-free-gb`, `--also-stop-on`, `--near-copy-against`, `--dry-run` |
| `scripts/art/` | The review tools: contact sheets, sprite export, the review page, near-copy checks, model fetching | see each script's `--help` |

## Where things go

```text
mods/<Mod>/                 the mod exactly as players get it, minus built DLLs
  About/About.xml           identity, dependencies, supported versions, modVersion
  About/Preview.png         mod list and Workshop image
  About/PublishedFileId.txt written by the game at first upload; committed then
  LoadFolders.xml           which folders load on 1.7, and for optional mods
  Common/Defs, Patches, Languages, Textures, Sounds
  1.7/Assemblies/           built from src/ against the 1.7 reference, never committed
  Mods/<OtherMod>/          optional tie-in content, loaded only when that mod is active
  Mods/<OtherMod>/1.7/       a tie-in's own assembly
  README.md, CHANGELOG.md
src/<Project>/              C# source, building into mods/<Mod>/1.7/Assemblies/
workshop/<Mod>/             Workshop page drafts and release notes, once publishing starts
```

The `Mods/<OtherMod>/` folder name is a house convention, not a game rule. **Name every file in it after
the folder** (`Patches/MedicineGranulator_MedicinesPlus.xml`): files are keyed by their path inside a load
folder, so two `Patches/MedicineGranulator.xml` in different folders means only one loads.

A starting `LoadFolders.xml`:

```xml
<loadFolders>
  <v1.7>
    <li>/</li>
    <li>Common</li>
    <li>1.7</li>
    <li IfModActive="CETeam.CombatExtended">Mods/CombatExtended</li>
  </v1.7>
</loadFolders>
```

A tie-in with its own assembly lists both its content folder and its version folder
(`Mods/HarrierVtol` and `Mods/HarrierVtol/1.7`); `check-mod-layout` treats any folder whose last part is a game version
as a build-product folder that need not be committed.

Check each optional mod's real `packageId` in its `About.xml` before using it. Combat Extended's,
`CETeam.CombatExtended`, was read from its installed `About.xml` (Workshop item 2890901044) on 18 Oct 2026.

## The C# toolchain

| | RimWorld 1.7 (the only version, owner, 8 Nov 2026) |
| --- | --- |
| Target | `net10.0` |
| Game references | `Krafs.Rimworld.Ref` 1.7.4998-unstable (matches the owner's game build) |
| Harmony | `Lib.Harmony` 3.0.0-rc.4, compile-time only |
| Output | `mods/<Mod>/1.7/Assemblies/` |
| Tools | .NET SDK 10 builds it; Visual Studio 2026 or Rider 2026 to edit |

The sources and dates are in the [RimWorld modding reference](rimworld-modding-reference.md#building-c-mods).
`Directory.Build.props` sets the shared options (the latest C# language version, nullable references, deterministic builds,
warnings as errors, no `.deps.json`), and `Directory.Build.targets` everything that
depends on the target: `GameVersion` (1.7), the output folder
`mods/$(ModFolder)/$(TieInFolder)$(GameVersion)/Assemblies/` (set as `OutDir`, `TargetDir` and `TargetPath`, because the SDK
has already derived those from `OutputPath` by the time the file is imported), the Krafs reference, Harmony
3 when a project sets `UsesHarmony`, and the Workshop folders `VefAssemblies` and `DbhAssemblies` (both mods ship 1.7
assemblies), `VfeFactoryAssemblies` and `VehicleFrameworkAssemblies` (1.6 only: neither ships a 1.7
build yet, and the game loads their 1.6 assemblies on 1.7). Override any of them with `-p:Name=...` or an environment
variable, or `WorkshopRoot` for all four.

A project file that references another mod (D'thorga Radio Framework's):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Phobos.Radio</RootNamespace>
    <AssemblyName>PhobosRadio</AssemblyName>
    <ModFolder>PhobosRadioFramework</ModFolder>
    <Version>0.1.1</Version>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="PipeSystem">
      <HintPath>$(VefAssemblies)\PipeSystem.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

`Private=false` keeps `PipeSystem.dll` out of our `Assemblies` folder: players get it from VEF, and its
licence forbids us shipping a copy. Set `VefAssemblies` (as an environment variable or `-p:VefAssemblies=…`)
when VEF lives elsewhere. Harmony is not referenced until a feature needs a patch; `UsesHarmony` then adds it with
`ExcludeAssets="runtime"`, so `0Harmony.dll` stays out too. A medical mod references the framework instead:

```xml
<ItemGroup>
  <ProjectReference Include="../PhobosFramework/PhobosFramework.csproj" Private="false" />
</ItemGroup>
```

One project, one target, the conditions in `Directory.Build.targets`. A project
that patches adds `<UsesHarmony>true</UsesHarmony>`; a tie-in assembly adds `<TieInFolder>Mods/HarrierVtol/</TieInFolder>`.
Harmony 3.0.0-rc.4 accepted every existing patch at build time; its nullable annotations are stricter than Harmony 2's, which
surfaced one unchecked dereference in Vehicle Patches.

## RimWorld 1.6, retired

From 18 Oct to 8 Nov 2026 every project also built `net472` against the 1.6 reference, with Harmony 2 and PolySharp (Sergio
Pedri's MIT source generator) for the C# features Unity's .NET Framework 4.7.2 libraries lack (`record`, `init`, `required`,
`[MemberNotNull]`). The owner retired 1.6 on 8 Nov 2026 and every mod dropped it on 8 Nov 2026. The git history before then has
the two-target setup, should it ever be wanted again.

## Building on Linux

The build needs only two packages: Krafs and Lib.Harmony. Because Krafs brings the whole reference set, the .NET 10 SDK compiles
every project on Linux with no Mono and no Windows targeting pack. **GitHub's `ubuntu-latest` runner
proves it on every push** for the projects that reference no other mod's assembly (the table above). Code is exercised in the game.

## Deploying to the game

`python scripts/deploy.py [--build] [--apply] <Mod>...` or `--all` installs mods. It:

- previews by default, and copies only with `--apply`;
- with `--all` takes every folder under `mods/` that has an `About/About.xml`; with `--build` (whether mods are named or `--all`)
  runs `dotnet build` on every project under `src/` first, stopping at the first failure before anything
  is copied;
- refuses while RimWorld is running, found by its process name (`RimWorld.exe` for 1.7 on Windows, `RimWorldWin64.exe` and
  `RimWorldWin.exe` for 1.6, and the Linux and Mac names);
- with `--apply`, first renames every named mod's installed folder aside and straight back, so a folder something holds open
  stops the whole run before **any** mod changes, and the refusal names any File Explorer window open inside it (otherwise it
  names the usual holders: a terminal or editor open in it, RimSort). **On 8 Nov 2026 an Explorer window open in
  `PhobosVehicles\Mods\TrainsOfTheRim\Defs` stopped every install at D'thorga Vehicles**, after earlier mods had updated and
  before D'thorga Water Tap; Windows reports an open folder as "Access is denied", not "in use";
- moves the previous install to `.local/deploy-backups/`, renaming it on the game's own drive first: Windows
  refuses that rename while the game holds any file in it open, so an unrecognised running game still stops it
  with nothing changed;
- copies `mods/<Mod>/` and its built assemblies into `<game>\Mods\<Mod>\`;
- **encodes each installed mod's textures to DDS with RimSort's todds** (13 Nov 2026), so RimSort's texture step is not needed for
  our mods: a fresh install replaces the mod folder and so wiped the DDS files RimSort had made, and the owner ran todds over every
  mod after each update. It uses RimSort's own "optimized" preset exactly (`-f BC1 -af BC7 -on -vf -fs -ss Textures -t -p <folder>`,
  read from RimSort's source, `app/utils/todds/wrapper.py` at commit c02f2b3; the vertical flip matters), only on the mods just
  deployed and only PNG files with `Textures` in their path. todds comes from `--todds`, `RIMSORT_TODDS`,
  `.local/install-settings.json` (key `toddsPath`) or RimSort's default install; without it, or if it fails, the mods still work
  from their PNG files and the run says so. `--no-dds` skips the step. **Confirmed working by the owner in their own install, 13 Nov 2026**;
- never touches `ModsConfig.xml` or RimSort's lists or settings.

The game path comes from `--game`, the `RIMWORLD_PATH` environment variable, `.local/install-settings.json` (key
`rimworldPath`; `check-factory-patches` and `refresh-mod-list` also read `workshopPath` and `rimworldConfigPath` there, and
`triage-log` reads `rimworldLogsPath`, `query-save` reads `rimworldSavesPath` and `modkitCachePath`, and `query-defs` reads
`workshopPath`, `rimworldConfigPath` and `modkitCachePath`), or the default Steam path. **Until 30 Oct 2026 the script knew only 1.6's process name**: one deploy that day
began while the owner's 1.7 game ran and stopped at the first locked file, with the install intact; the rename
step and the 1.7 name come from that.

## Cleaning up

`python scripts/cleanup.py` lists what the tools, builds and tests leave behind, with each group's size and why it may go;
`--apply` deletes it (the owner runs that). It calls phobos-modkit's `cleanup` (since 1.8.0, 11 Nov 2026; its guide is
`docs/cleanup.md` in the submodule), which clears:

| What | Where | Rule |
| --- | --- | --- |
| Save indexes and other derived data | The shared tools' cache, under the temporary folder | An index whose save has been saved again or has gone; anything unchanged for 14 days (`--older-than`); never the Workshop cache |
| The shared tools' development build | `%LOCALAPPDATA%\phobos-modkit\target\debug` | Only the kit's own tests use it; the release programs stay; skipped while cargo runs (`--keep-build` to keep it) |
| Old install backups | `.local/deploy-backups` | The newest of each current mod stays; merged mods keep none |
| Test repositories | The temporary folder, `archive-test-*` | All |
| .NET build logs | The temporary folder, `Microsoft.NET.Workload_*.log` | Older than a day |
| RimWorld crash dumps | `%LOCALAPPDATA%\CrashDumps` | All but the newest |
| Git's file-watcher daemons | Running processes | Stopped; a global `core.fsmonitor` starts one in every repository, and git starts one again when needed |

The repository's own rules are in `config/cleanup.json`: **something new that a script or test leaves behind gets a rule
there**. It reports, never deletes, a folder a failed deploy left in the game folder. **Tests that make a git repository set `core.fsmonitor false` in it and delete it
with read-only handling** (`remove_tree` in `tests/test_archive_branch.py`): git's object files are read-only, and
`shutil.rmtree(..., ignore_errors=True)` left 400 repositories and 220 daemons behind before 8 Nov 2026.
