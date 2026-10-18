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

## Mods named in the designs

The radio framework design plans optional tie-ins with other authors' mods, including
[Surveillance Shift](https://github.com/fernyrepos/Surveillance-Shift) by **ferny**, Combat Extended,
Vehicle Framework and the Vanilla Expanded mods. A tie-in calls another mod's public behaviour at runtime
only when that mod is active. It never copies the mod's code, art or data. Surveillance Shift's repository
has no licence file (checked 18 October 2026), so nothing from it may be copied. Each tie-in, when built,
records here what it uses and on what terms.
