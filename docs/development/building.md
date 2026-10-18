# Building, testing and deploying

**Status (18 Oct 2026): no mod and no C# project exists yet.** The checks below run today. The rest of this
page records the toolchain the first mod will use, so the first project starts from a decision rather than
a blank page. The C# sketch has not been built yet; correct this page when it is.

## The checks (run before every commit)

```text
python scripts/check-doc-links.py
python scripts/check-mod-layout.py
python -m unittest discover -s tests
```

- **check-doc-links** finds relative Markdown links whose target is missing.
- **check-mod-layout** checks every `mods/<Mod>/` folder:
  - `About/About.xml` has the required fields and a `packageId` of the form `PhobosDthorga.<ModName>`;
  - package IDs are unique, and `modVersion` is MAJOR.MINOR.PATCH;
  - a `README.md` and `CHANGELOG.md` exist;
  - every folder `LoadFolders.xml` names exists;
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

The `Mods/<OtherMod>/` folder name is a house convention, not a game rule.

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
| Output | `mods/<Mod>/1.6/Assemblies/` | `mods/<Mod>/1.7/Assemblies/`, and `<v1.7>` in `LoadFolders.xml` switches from `1.6` to `1.7` |
| Tools | .NET SDK 10 builds both | Visual Studio 2026 or Rider 2026 to edit |

The sources and dates are in the [RimWorld modding reference](rimworld-modding-reference.md#building-c-mods).
`Directory.Build.props` already sets the shared options:

- the latest C# language version;
- nullable references;
- deterministic builds;
- warnings as errors;
- no target-framework subfolder;
- no `.deps.json` file.

A starting point for the first project (agent sketch, **not yet built**):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net472</TargetFramework>
    <RootNamespace>Phobos.Radio</RootNamespace>
    <AssemblyName>PhobosRadio</AssemblyName>
    <OutputPath>../../mods/PhobosRadio/1.6/Assemblies/</OutputPath>
    <DebugType>none</DebugType>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Krafs.Rimworld.Ref" Version="1.6.4871" />
    <PackageReference Include="Lib.Harmony" Version="2.4.3" ExcludeAssets="runtime" />
  </ItemGroup>
</Project>
```

The names here are placeholders: the framework's public name and `packageId` are still open questions.
`ExcludeAssets="runtime"` keeps `0Harmony.dll` out of our `Assemblies` folder, because players get it from
the Harmony mod.

When 1.7 support starts, the likely route is one project with `<TargetFrameworks>net472;net10.0</TargetFrameworks>`,
conditional references and an output path per framework. Try that before splitting into two projects.

## Deploying to the game

A deploy script arrives with the first mod. It will:

- preview by default, and copy only with `--apply`;
- refuse while RimWorld is running;
- copy `mods/<Mod>/` and its built assemblies into `<game>\Mods\<Mod>\`;
- never touch `ModsConfig.xml` or RimSort's lists.

The game path is read from `.local/install-settings.json`, or the default Steam path. Until the script
exists, nothing is copied into the game.
