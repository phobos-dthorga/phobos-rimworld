# Building, testing and deploying

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
  LoadFolders.xml           which folders load for 1.6 and 1.7, and for optional mods
  Common/Defs, Patches, Languages, Textures, Sounds   shared by every version
  1.6/Assemblies/           built from src/, never committed
  Mods/<OtherMod>/          optional tie-in content, loaded only when that mod is active
  README.md, CHANGELOG.md
src/<Project>/              C# source, building into mods/<Mod>/<version>/Assemblies/
workshop/<Mod>/             Workshop page drafts and release notes, once publishing starts
```

The `Mods/<OtherMod>/` folder name is a house convention, not a game rule. **Name every file in it after
the folder** (`Patches/MedicineGranulator_MedicinesPlus.xml`): files are keyed by their path inside a load
folder, so two `Patches/MedicineGranulator.xml` in different folders means only one loads.

A starting `LoadFolders.xml`, with 1.7 borrowing the 1.6 build until a native one exists:

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
    <li>1.6</li>
    <li IfModActive="CETeam.CombatExtended">Mods/CombatExtended</li>
  </v1.7>
</loadFolders>
```

Check each optional mod's real `packageId` in its `About.xml` before using it. Combat Extended's,
`CETeam.CombatExtended`, was read from its installed `About.xml` (Workshop item 2890901044) on 18 Oct 2026.

## The C# toolchain

| | RimWorld 1.6 (first) | RimWorld 1.7 (when the owner moves, or 1.7 goes stable) |
| --- | --- | --- |
| Target | `net472` | `net10.0` |
| Game references | `Krafs.Rimworld.Ref` 1.6.4871 | `Krafs.Rimworld.Ref` 1.7 build, or the game's Assembly-CSharp, UnityStubs and Waylith |
| Harmony | `Lib.Harmony` 2.4.x, compile-time only | Harmony 3.0 |
| Newer C# (records, `init`, `required`) | `PolySharp` 1.16.0, compile-time only, set once in `Directory.Build.props` | Not needed: .NET 10 has the types |
| Output | `mods/<Mod>/1.6/Assemblies/` | `mods/<Mod>/1.7/Assemblies/`, and `<v1.7>` in `LoadFolders.xml` switches from `1.6` to `1.7` |
| Tools | .NET SDK 10 builds both | Visual Studio 2026 or Rider 2026 to edit |

The sources and dates are in the [RimWorld modding reference](rimworld-modding-reference.md#building-c-mods).
`Directory.Build.props` already sets the shared options:

- the latest C# language version;
- nullable references;
- deterministic builds;
- warnings as errors;
- no target-framework subfolder;
- no `.deps.json` file;
- PolySharp for every `net472` project (owner decision, 18 Oct 2026).

A starting point for the first project. This is an agent sketch; a probe built from it on 18 Oct 2026 had 0
warnings and 0 errors, and its `Assemblies` folder held only the mod's own DLL:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <RootNamespace>Phobos.Radio</RootNamespace>
    <AssemblyName>PhobosRadio</AssemblyName>
    <OutputPath>../../mods/PhobosRadioFramework/1.6/Assemblies/</OutputPath>
    <DebugType>none</DebugType>
    <VefAssemblies Condition="'$(VefAssemblies)' == ''">C:\Program Files (x86)\Steam\steamapps\workshop\content\294100\2023507013\1.6\Assemblies</VefAssemblies>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.6.4871" />
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

When 1.7 support starts, the likely route is one project with `<TargetFrameworks>net472;net10.0</TargetFrameworks>`,
conditional references and an output path per framework. Try that before splitting into two projects.

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

A deploy script arrives with the first mod. It will:

- preview by default, and copy only with `--apply`;
- refuse while RimWorld is running;
- copy `mods/<Mod>/` and its built assemblies into `<game>\Mods\<Mod>\`;
- never touch `ModsConfig.xml` or RimSort's lists.

The game path is read from `.local/install-settings.json`, or the default Steam path. Until the script
exists, nothing is copied into the game.
