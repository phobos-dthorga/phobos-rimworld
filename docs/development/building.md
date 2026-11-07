# Building, testing and deploying

**Status (6 Nov 2026): eleven C# projects, RimWorld 1.7 leads.** Every project builds two assemblies from one project file:
`net10.0` against the 1.7.4998 reference (Harmony 3 where a mod patches) into `mods/<Mod>/1.7/Assemblies/`, and `net472`
against the 1.6 reference (Harmony 2, PolySharp) into `mods/<Mod>/1.6/Assemblies/`, the minimum backwards support until 1.6
is retired (owner, 4 Nov 2026). A tie-in assembly builds into `mods/<Mod>/Mods/<Other>/<version>/Assemblies/` instead.
`Directory.Build.targets` at the root turns the target into the game version, the output folder, the game and Harmony
references and the Workshop folders of other mods' assemblies, so a project file only names its mod folder (`ModFolder`), its
tie-in folder if any (`TieInFolder`) and whether it patches (`UsesHarmony`). `python scripts/deploy.py --build --apply --all`
builds every project and installs every mod, with RimWorld closed.

## The projects

| Project (`src/`) | Builds into (`mods/`, per version) | References besides the game | Harmony | Rule tests (`tests/`) | On GitHub |
| --- | --- | --- | --- | --- | --- |
| `PhobosFramework` | `PhobosFramework/` | nothing | no | `PhobosFramework.Rules.Tests` | builds and tests |
| `PhobosMedical` | `PhobosMedical/` | the framework, VEF's `PipeSystem.dll` (Vanilla Gravship Expanded through Defs only; More Injuries by name at run time) | yes | `PhobosMedical.Rules.Tests` | builds and tests |
| `PhobosFactory` | `PhobosFactoryAdditions/` | VEF's `PipeSystem.dll`, VFE Factory's `VanillaFurnitureExpandedFactory.dll` | yes | `PhobosFactory.Rules.Tests` | tests only |
| `PhobosFactory.Hygiene` | `PhobosFactoryAdditions/Mods/DubsBadHygiene/` | the factory core, the framework's hygiene tie-in, `PipeSystem.dll`, Dubs Bad Hygiene's `BadHygiene.dll` | yes | none | no |
| `PhobosFramework.Hygiene` | `PhobosFramework/Mods/DubsBadHygiene/` | Dubs Bad Hygiene's `BadHygiene.dll`: the shared plumbing reader | no | none | no |
| `PhobosMedical.Hygiene` | `PhobosMedical/Mods/DubsBadHygiene/` | the medical core, the framework's hygiene tie-in, `BadHygiene.dll` | no | none | no |
| `PhobosPatches.Harrier` | `PhobosPatches/Mods/HarrierVtol/` | the framework, Vehicle Framework's `Vehicles.dll`, `SmashTools.dll`, `UpdateLogTool.dll` | yes | none | no |
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
  project built for both versions. **5 Nov**: Emergency Dosing and Medical Equipment. **6 Nov**: Triage.
- Vehicle Framework targets .NET Framework 4.8 and Vehicle Patches 4.7.2, so that project sets
  `ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch`; the game loads both whatever they declare.

## The checks (run before every commit)

```text
python scripts/check-doc-links.py
python scripts/check-mod-layout.py
python -m unittest discover -s tests
dotnet test tests/<Project>             each rule-test project whose rules you changed
python scripts/check-factory-patches.py (needs the installed game; for Factory Additions and its tie-ins)
python scripts/deploy.py --build --apply --all   (with RimWorld closed)
```

The first three are the ones AGENTS.md requires before every commit; gate on their exit codes. **On GitHub**
(`.github/workflows/checks.yml`) every push runs those three, every rule-test project, and builds for 1.7 and 1.6 every
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
  LoadFolders.xml           which folders load for 1.7 and 1.6, and for optional mods
  Common/Defs, Patches, Languages, Textures, Sounds   shared by every version
  1.7/Assemblies/           built from src/ against the 1.7 reference, never committed
  1.6/Assemblies/           built from src/ against the 1.6 reference, never committed
  Mods/<OtherMod>/          optional tie-in content, loaded only when that mod is active
  Mods/<OtherMod>/1.7/, 1.6/   a tie-in's own assembly, one per game version
  README.md, CHANGELOG.md
src/<Project>/              C# source, building into mods/<Mod>/<version>/Assemblies/ for both versions
workshop/<Mod>/             Workshop page drafts and release notes, once publishing starts
```

The `Mods/<OtherMod>/` folder name is a house convention, not a game rule. **Name every file in it after
the folder** (`Patches/MedicineGranulator_MedicinesPlus.xml`): files are keyed by their path inside a load
folder, so two `Patches/MedicineGranulator.xml` in different folders means only one loads.

A starting `LoadFolders.xml`, one assembly folder per game version:

```xml
<loadFolders>
  <v1.6>
    <li>/</li>
    <li>Common</li>
    <li>1.6</li>
    <li IfModActive="CETeam.CombatExtended">Mods/CombatExtended</li>
  </v1.6>
  <v1.7>
    <li>/</li>
    <li>Common</li>
    <li>1.7</li>
    <li IfModActive="CETeam.CombatExtended">Mods/CombatExtended</li>
  </v1.7>
</loadFolders>
```

A tie-in with its own assembly lists both its content folder and its version folder under each block
(`Mods/HarrierVtol` and `Mods/HarrierVtol/1.7`); `check-mod-layout` treats any folder whose last part is a game version
as a build-product folder that need not be committed.

Check each optional mod's real `packageId` in its `About.xml` before using it. Combat Extended's,
`CETeam.CombatExtended`, was read from its installed `About.xml` (Workshop item 2890901044) on 18 Oct 2026.

## The C# toolchain

| | RimWorld 1.6 (minimum backwards support) | RimWorld 1.7 (leads) |
| --- | --- | --- |
| Target | `net472` | `net10.0` |
| Game references | `Krafs.Rimworld.Ref` 1.6.4871 | `Krafs.Rimworld.Ref` 1.7.4998-unstable (matches the owner's game build) |
| Harmony | `Lib.Harmony` 2.4.3, compile-time only | `Lib.Harmony` 3.0.0-rc.4, compile-time only |
| Newer C# (records, `init`, `required`) | `PolySharp` 1.16.0, compile-time only, set once in `Directory.Build.props` | Not needed: .NET 10 has the types |
| Output | `mods/<Mod>/1.6/Assemblies/` | `mods/<Mod>/1.7/Assemblies/` |
| Tools | .NET SDK 10 builds both | Visual Studio 2026 or Rider 2026 to edit |

The sources and dates are in the [RimWorld modding reference](rimworld-modding-reference.md#building-c-mods).
`Directory.Build.props` sets the shared options (the latest C# language version, nullable references, deterministic builds,
warnings as errors, no `.deps.json`, PolySharp for every `net472` build), and `Directory.Build.targets` everything that
depends on the target: `GameVersion` (1.7 for `net10.0`, 1.6 for `net472`), the output folder
`mods/$(ModFolder)/$(TieInFolder)$(GameVersion)/Assemblies/` (set as `OutDir`, `TargetDir` and `TargetPath`, because the SDK
has already derived those from `OutputPath` by the time the file is imported), the Krafs reference for each version, Harmony
2 or 3 when a project sets `UsesHarmony`, and the Workshop folders `VefAssemblies` and `DbhAssemblies` (one per game version:
both mods ship 1.7 assemblies), `VfeFactoryAssemblies` and `VehicleFrameworkAssemblies` (1.6 only: neither ships a 1.7
build yet, and the game loads their 1.6 assemblies on 1.7). Override any of them with `-p:Name=...` or an environment
variable, or `WorkshopRoot` for all four.

A project file that references another mod (D'thorga Radio Framework's; the 18 Oct 2026 probe that proved the 1.6 setup used the longer single-target form):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net472;net10.0</TargetFrameworks>
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

That is the route taken on 4 Nov 2026: one project, both targets, the conditions in `Directory.Build.targets`. A project
that patches adds `<UsesHarmony>true</UsesHarmony>`; a tie-in assembly adds `<TieInFolder>Mods/HarrierVtol/</TieInFolder>`.
Harmony 3.0.0-rc.4 accepted every existing patch at build time; its nullable annotations are stricter than Harmony 2's, which
surfaced one unchecked dereference in Vehicle Patches.

## Newer C# on RimWorld 1.6

RimWorld 1.6 compiles against Unity's copies of the .NET Framework 4.7.2 libraries (April 2018), which
`Krafs.Rimworld.Ref` supplies in full. What that means for C# 14, from the probe build on 18 Oct 2026:

| Feature | Without PolySharp | With PolySharp |
| --- | --- | --- |
| Syntax only: pattern matching, switch expressions, file-scoped namespaces | Works | Works |
| `record`, `init` | Fails: `IsExternalInit` is not defined (CS0518) | Works |
| `required` members | Fails: three missing attributes (CS0656) | Works |
| `[NotNullWhen]`, `[NotNull]` and most nullable attributes | Works: Unity's `mscorlib` has them, public | Works |
| `[MemberNotNull]` | **Fails: Unity's `mscorlib` has it, but internal (CS0122)** | Works |
| Features needing runtime support, such as default interface methods | Not tested; expected to fail on Unity's runtime | No polyfill can add runtime support |

PolySharp is a source generator by Sergio Pedri (MIT, [NuGet](https://www.nuget.org/packages/PolySharp)).
It writes only the types a project is missing, as **internal** types inside the mod's own assembly. The probe's
26 generated types were all internal, so two mods carrying them cannot clash, and PolySharp itself never
ships.

## Building on Linux

The probe needed only three packages: Krafs, Lib.Harmony and PolySharp. Because Krafs brings the whole framework reference set,
the .NET 10 SDK compiles both targets on Linux with no Mono and no Windows targeting pack. **GitHub's `ubuntu-latest` runner
proves it on every push** for the projects that reference no other mod's assembly (the table above). Running `net472` code on
Linux would still need Mono, so code is exercised in the game.

## Deploying to the game

`python scripts/deploy.py [--build] [--apply] <Mod>...` or `--all` installs mods. It:

- previews by default, and copies only with `--apply`;
- with `--all` takes every folder under `mods/` that has an `About/About.xml`; with `--build` (whether mods are named or `--all`)
  runs `dotnet build` on every project under `src/` first (both game versions), stopping at the first failure before anything
  is copied;
- refuses while RimWorld is running, found by its process name (`RimWorld.exe` for 1.7 on Windows, `RimWorldWin64.exe` and
  `RimWorldWin.exe` for 1.6, and the Linux and Mac names);
- moves the previous install to `.local/deploy-backups/`, renaming it on the game's own drive first: Windows
  refuses that rename while the game holds any file in it open, so an unrecognised running game still stops it
  with nothing changed;
- copies `mods/<Mod>/` and its built assemblies into `<game>\Mods\<Mod>\`;
- never touches `ModsConfig.xml` or RimSort's lists.

The game path comes from `--game`, the `RIMWORLD_PATH` environment variable, `.local/install-settings.json` (key
`rimworldPath`; `check-factory-patches` and `refresh-mod-list` also read `workshopPath` and `rimworldConfigPath` there), or the
default Steam path. **Until 30 Oct 2026 the script knew only 1.6's process name**: one deploy that day
began while the owner's 1.7 game ran and stopped at the first locked file, with the install intact; the rename
step and the 1.7 name come from that.
