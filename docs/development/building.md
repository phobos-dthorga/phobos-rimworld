# Building, testing and deploying

**Status (4 Nov 2026): RimWorld 1.7 leads.** Every C# project builds two assemblies from one project file:
`net10.0` against the 1.7.4998 reference (Harmony 3 where a mod patches) into `mods/<Mod>/1.7/Assemblies/`, and `net472`
against the 1.6 reference (Harmony 2, PolySharp) into `1.6/Assemblies/`, the minimum backwards support until 1.6 is
retired (owner, 4 Nov 2026). `LoadFolders.xml` gives each game version its own folder; a tie-in assembly sits in
`Mods/<Other>/1.7/` and `Mods/<Other>/1.6/`. `Directory.Build.targets` at the root turns the target into the game version,
the output folder, the game and Harmony references and the Workshop folders of other mods' assemblies, so a project file only
names its mod folder (`ModFolder`), its tie-in folder if any (`TieInFolder`) and whether it patches (`UsesHarmony`). All eight
projects built both ways on 4 Nov 2026 with 0 warnings; **no 1.7 assembly is tested in game yet.** `python scripts/deploy.py
--build --apply --all` builds everything and installs every mod. The per-mod notes below are kept as written; where one
says a 1.6 build is "loaded on 1.7 too", that was true until 4 Nov 2026.

**Status (19 Oct 2026): Phase 1 of the radio framework is built** (0.1.0 Draft) in
`mods/PhobosRadioFramework/` and `src/PhobosRadio/`, and awaits the owner's play test. The checks below
run today; `dotnet build src/PhobosRadio/PhobosRadio.csproj` builds the mod where VEF is installed. The 1.6 setup was proven on 18 Oct 2026 by building a
throwaway probe mod; the 1.7 column is untried.

**D'thorga Factory Additions has an assembly since 0.5.0** (29 Oct 2026): `dotnet build
src/PhobosFactory/PhobosFactory.csproj` builds `mods/PhobosFactoryAdditions/1.6/Assemblies/PhobosFactory.dll`,
which `LoadFolders.xml` loads on 1.6 and 1.7. It is the first project with Harmony (`Lib.Harmony` 2.4.3,
`ExcludeAssets="runtime"`, so `0Harmony.dll` never ships) and, like the radio framework, references the
installed `PipeSystem.dll`. Since 0.6.0 it also references VFE Factory's installed
`VanillaFurnitureExpandedFactory.dll` the same way (`VfeFactoryAssemblies`, default the Workshop item's `1.6\Assemblies`;
VFE Factory publishes no licence, so it is never copied). **Build before deploying**: `deploy.py` copies whatever is in the mod folder and
says "no assembly built" when the DLL is missing. Its game-free output rule is tested by
`dotnet test tests/PhobosFactory.Rules.Tests`, which also runs on GitHub.

**A second assembly since 0.10.0** (2 Nov 2026): `dotnet build src/PhobosFactory.Hygiene/PhobosFactory.Hygiene.csproj` builds
the core and then `mods/PhobosFactoryAdditions/Mods/DubsBadHygiene/Assemblies/PhobosFactory.Hygiene.dll`, the Dubs Bad Hygiene
tie-in, which the core assembly never references. It references the installed `BadHygiene.dll` the same way (`DbhAssemblies`,
default the Workshop item's `1.6\Assemblies`), never copied. **Build this project before deploying**, so both DLLs are current.

**D'thorga Vehicle Patches has an assembly since 0.1.0** (2 Nov 2026): `dotnet build src/PhobosVehiclePatches/PhobosVehiclePatches.csproj` builds
`mods/PhobosVehiclePatches/Mods/HarrierVtol/Assemblies/PhobosVehiclePatches.dll`, loaded only while the Harrier is active. It references the installed
`Vehicles.dll`, `SmashTools.dll` and `UpdateLogTool.dll` (`VehicleFrameworkAssemblies`, default the Workshop item's `1.6\Assemblies`), never copied. Vehicle
Framework targets .NET Framework 4.8 and this project 4.7.2, so the project sets `ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch`;
the game loads both whatever they declare.

**D'thorga Medical Save Bridge has an assembly since 0.1.0** (3 November 2026): `dotnet build
src/PhobosMedicalSaveBridge/PhobosMedicalSaveBridge.csproj` builds `mods/PhobosMedicalSaveBridge/1.6/Assemblies/PhobosMedicalSaveBridge.dll`,
loaded on 1.6 and 1.7. It references only the game and Harmony: More Injuries is reached by name at run time, and MSE2 not at all, so
it builds anywhere the .NET SDK is. Its game-free rules (the index re-numbering and the mod-list comparison) are tested by
`dotnet test tests/PhobosMedicalSaveBridge.Rules.Tests`, which also runs on GitHub.

**D'thorga Water Tap has an assembly since 0.1.0** (4 November 2026): `dotnet build src/PhobosWaterTap/PhobosWaterTap.csproj` builds
`mods/PhobosWaterTap/1.6/Assemblies/PhobosWaterTap.dll`, loaded on 1.6 and 1.7. It references the installed `BadHygiene.dll` (`DbhAssemblies`, default the Workshop item's
`1.6\Assemblies`), never copied, and has no Harmony. Its game-free rules are tested by `dotnet test tests/PhobosWaterTap.Rules.Tests`, which also runs on GitHub. Its texture and
preview are redrawn by `assets/water-tap/draw_tap.py`, with any Python that has Pillow.

**D'thorga Framework has an assembly since 0.1.0** (4 November 2026): `dotnet build src/PhobosFramework/PhobosFramework.csproj` builds
`mods/PhobosFramework/1.6/Assemblies/PhobosFramework.dll`, loaded on 1.6 and 1.7. It references only the game (Combat Extended is reached by
name) and has no Harmony, so it builds anywhere the .NET SDK is. Its game-free rules (door and vent exchange, venting, decay, the rebuild of a
room from its cells, mask and signal choice, the owner's source rule) are tested by `dotnet test tests/PhobosFramework.Rules.Tests`, which also
runs on GitHub. **Other projects reference it with `ProjectReference ... Private="false"`**, so the framework's DLL ships only in its own mod.

**D'thorga Triage has an assembly since 0.1.0** (6 November 2026): `dotnet build src/PhobosTriage/PhobosTriage.csproj` builds the framework and then
`mods/PhobosTriage/<version>/Assemblies/PhobosTriage.dll`. It references the game and the framework project only, and has no Harmony. Its game-free rules
(authority, the class of a condition by its effects, the choice of operation and its refusal, the triage tag and score) are tested by
`dotnet test tests/PhobosTriage.Rules.Tests`. The framework's health layer it builds on (0.3.0) is tested by the framework's own test project.

**D'thorga Burns and Smoke has an assembly since 0.1.0** (4 November 2026): `dotnet build src/PhobosBurnsAndSmoke/PhobosBurnsAndSmoke.csproj`
builds the framework and then `mods/PhobosBurnsAndSmoke/1.6/Assemblies/PhobosBurnsAndSmoke.dll`. It references the game and the framework
project only; More Injuries and Combat Extended are reached through the framework by name. Its game-free rules (burned fraction, the Baux score
and its treatment factor, carbon monoxide uptake and clearance, the airway curve) are tested by `dotnet test tests/PhobosBurnsAndSmoke.Rules.Tests`.

**One limit to know:** the mod references Vanilla Expanded Framework's `PipeSystem.dll`, which its licence
does not let us copy into the repository. So **GitHub Actions cannot build the mod**; it builds and runs
only the game-free physics tests in `tests/`. The mod itself builds on a machine with RimWorld and VEF
installed. The Linux-build claim in the modding reference therefore stands only for the physics project.

## The checks (run before every commit)

```text
python scripts/check-doc-links.py
python scripts/check-mod-layout.py
python scripts/check-factory-patches.py
python -m unittest discover -s tests
python scripts/deploy.py --build --apply --all   (with RimWorld closed)
```

- **check-doc-links** finds relative Markdown links whose target is missing.
- **check-factory-patches** reads the installed game, the active mods (ModsConfig.xml) and the Workshop
  folder, never writing to them, and checks D'thorga Factory Additions (or `--mod <Name>`) against them:
  no file is skipped for sharing a path with a later folder's file, every patch xpath matches one node, every
  Def name a ProcessDef uses exists, every hopper-only bill has an output cap within its product's stack
  limit, defined bills match the bills patched onto machines, every ParentName resolves (to a loaded
  Name or one the mod's patches add to a Def that has none yet), and every bench bill template names a
  machine, benches, research, sound and effect that exist. It reads only the files the game loads.
  Paths as for deploy.py (`--game`, `--workshop`, `--config`, the environment variables or
  `.local/install-settings.json`). An offline check is not an in-game test.
- **check-mod-layout** checks every `mods/<Mod>/` folder:
  - `About/About.xml` has the required fields and a `packageId` of the form `PhobosDthorga.<ModName>`;
  - package IDs are unique, and `modVersion` is MAJOR.MINOR.PATCH;
  - a `README.md` and `CHANGELOG.md` exist;
  - every folder `LoadFolders.xml` names exists;
  - no two folders in one version block hold a Defs or Patches file at the same path (the game would load
    only the one in the folder listed last);
  - no build products (DLLs, `bin/`, `obj/`) are present.
- **The unit tests** exercise both scripts against synthetic folders.

The same checks run on GitHub for every push (`.github/workflows/checks.yml`). None of them needs RimWorld.

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

A project file today (D'thorga Radio Framework's; the 18 Oct 2026 probe that proved the 1.6 setup used the longer single-target form):

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
when VEF lives elsewhere. Harmony is not referenced until a feature needs a patch; when it is, use
`ExcludeAssets="runtime"` so `0Harmony.dll` stays out too.

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

The probe needed only three packages: Krafs, Lib.Harmony and PolySharp. Because Krafs brings the whole
framework reference set, the .NET 10 SDK should compile 1.6 mods on Linux with no Mono and no Windows
targeting pack, so CI can prove a mod builds. Running net472 code on Linux would still need Mono, so code
is exercised in the game. Not yet tried on Linux: confirm it when the first project adds a build step to CI.

## Deploying to the game

`python scripts/deploy.py [--build] [--apply] <Mod>...` or `--all` installs mods. It:

- previews by default, and copies only with `--apply`;
- with `--all` takes every folder under `mods/` that has an `About/About.xml`, and with `--build` runs `dotnet build` on every
  project under `src/` first (both game versions), stopping at the first failure before anything is copied;
- refuses while RimWorld is running, found by its process name (`RimWorld.exe` for 1.7 on Windows,
  `RimWorldWin64.exe` for 1.6);
- moves the previous install to `.local/deploy-backups/`, renaming it on the game's own drive first: Windows
  refuses that rename while the game holds any file in it open, so an unrecognised running game still stops it
  with nothing changed;
- copies `mods/<Mod>/` and its built assemblies into `<game>\Mods\<Mod>\`;
- never touches `ModsConfig.xml` or RimSort's lists.

The game path comes from `--game`, the `RIMWORLD_PATH` environment variable, `.local/install-settings.json`, or
the default Steam path. **Until 30 Oct 2026 the script knew only 1.6's process name**: one deploy that day
began while the owner's 1.7 game ran and stopped at the first locked file, with the install intact; the rename
step and the 1.7 name come from that.
