# D'thorga Radio Framework changelog

Maintained from 19 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

### Added

- The mod skeleton: About.xml (packageId PhobosDthorga.RadioFramework, RimWorld 1.6), load folders for
  1.6 and 1.7 (1.7 borrows the 1.6 build), a placeholder preview image, this changelog and the README.
- The game-free physics core (decibels, free-space path loss, noise floor, coax loss, ray clearance,
  the link-budget sheet and the margin outcomes), with unit tests that run without the game.
- The build project (RimWorld 1.6, .NET Framework 4.7.2, PolySharp) and scripts/deploy.py, which copies
  the mod into the game's Mods folder with RimWorld closed.

### Compatibility and limits

- Nothing in this version changes play yet: no Defs, no buildings, no radios. Safe to add and remove.
- Not yet tested in game.
