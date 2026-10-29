# Third-party provenance

**As of 18 October 2026, the repository contains no third-party code, art, sound or data.** It holds only
original Phobos documentation, scripts and tests. This file records what each mod takes from elsewhere,
starting with the first mod that does.

## RimWorld

[RimWorld](https://rimworldgame.com/) and its expansions are made by **Ludeon Studios**. Nothing from the
game is redistributed here. Our records cite facts read from the installed game's Defs, such as the comms
console's power draw in the [radio reference checks](docs/development/radio-reference-checks.md); citing a
value confers no ownership of the game's files. Mods follow [Ludeon's EULA](https://rimworldgame.com/eula/):
free, clearly community-created, and not official or endorsed by Ludeon.

## Harmony

Planned C# mods depend on **Harmony** by **Andreas Pardeike (brrainz)**:
[GitHub](https://github.com/pardeike/Harmony) (MIT licence), Workshop item 2009463077. Players get it from
the Harmony mod. Our mods reference it at build time only and will not ship `0Harmony.dll`.

## Vanilla Expanded Framework

The radio framework's feedline runs on the **PipeSystem** module of
[Vanilla Expanded Framework](https://github.com/Vanilla-Expanded/VanillaExpandedFramework) by **Oskar
Potocki and the Vanilla Expanded team** (Workshop item 2023507013, `packageId`
`OskarPotocki.VanillaFactionsExpanded.Core`). VEF is a declared dependency: players must have it, and our
assembly is compiled against the installed `PipeSystem.dll` without bundling it.

VEF's licence (its `LICENSE` file, read 19 October 2026) is **Creative Commons BY-NC-ND 4.0** with riders.
The relevant rider: "Modders that create mods that depend on this framework (through marking this mod as
a dependency) are expressly permitted to create commissioned mods with commercial advantages or monetary
compensation, as well as accept donations on their mods, provided all other terms of the Licence are met."
The NoDerivatives term means we **never copy or adapt** VEF's code, DLLs, art or XML, and never
redistribute them; we only reference the installed assembly and subclass or call its public types at
runtime. Nothing from VEF is in this repository. Our own code remains MIT.

## Vanilla Furniture Expanded - Factory

D'thorga Factory Additions (`mods/PhobosFactoryAdditions/`) exists to extend
[Vanilla Furniture Expanded - Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3686924415) by
**Oskar Potocki and the Vanilla Expanded team** (`packageId` `VanillaExpanded.VFEFactory`), a declared
dependency. Its [GitHub repository](https://github.com/Vanilla-Expanded/VanillaFurnitureExpanded-Factory) and
its Workshop folder carry **no licence** (checked 29 October 2026), so nothing from it may be copied. We patch
its Defs by name in XML, read its decompiled assembly for research (kept in the ignored `.local/`), and since
0.6.0 compile against its installed `VanillaFurnitureExpandedFactory.dll` without bundling it, to patch and call
its conveyor belt at runtime. At runtime the storage switch also shows VFE Factory's own belt icon, read from its
installed Def; the image is never copied. Nothing from VFE Factory is in this repository.

## Mods named in the designs

The radio framework design plans optional tie-ins with other authors' mods, including
[Surveillance Shift](https://github.com/fernyrepos/Surveillance-Shift) by **ferny**, Combat Extended,
Vehicle Framework and the Vanilla Expanded mods. A tie-in calls another mod's public behaviour at runtime
only when that mod is active. It never copies the mod's code, art or data. Surveillance Shift's repository
has no licence file (checked 18 October 2026), so nothing from it may be copied. Each tie-in, when built,
records here what it uses and on what terms.
