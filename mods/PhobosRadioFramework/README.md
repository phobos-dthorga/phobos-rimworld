# D'thorga Radio Framework

Radio that works like radio: every contact is decided by a real link budget, and doubling the distance
costs 6 dB. This is **Phase 1**: local VHF and UHF on one map.

Version 0.1.0, in development since 19 October 2026. **Not yet tested in game.**

## Read this first (gotchas)

- **Requires Vanilla Expanded Framework** (its pipe network carries the coax feedline). Load this mod
  after Harmony, Core, the DLCs and Vanilla Expanded Framework.
- **Built for RimWorld 1.6.** On 1.7 it loads the same build and is untested there.
- **Decide before you start a colony:** the mod setting *Metres per cell* (default 1) changes every path
  and feedline loss. Changing it mid-save changes which links work.
- **Safe to add to a running save.** Removing it is safe once you have deconstructed every radio building
  and dropped every hand-held; the first load afterwards shows one harmless "could not find class" error.
- **The hand-held is worn on the belt slot**, so a colonist cannot wear a shield belt and a hand-held at
  the same time.
- **Nothing is published on the Steam Workshop yet.** Install from source with `scripts/deploy.py`.

## What it does

This version is the skeleton: it loads, and nothing in play changes yet. The next versions add, in order:

1. Hand-held radios, a base station with a built-in whip, the "In touch" thought, and signal bars.
2. Coax feedline, mast and whip antennas on Vanilla Expanded Framework's pipe network.
3. The link-budget window, the station's Net tab, the antenna siting preview, settings and debug tools.

## For modders

Bands, modes, hardware and the tuning values are Defs; other mods add radios and antennas in XML. The
Def types arrive with the next version and are documented here when they do.

## Rebuilding

```text
dotnet build src/PhobosRadio/PhobosRadio.csproj
python scripts/deploy.py --apply PhobosRadioFramework
```

See [building, testing and deploying](../../docs/development/building.md).

## Checks done

- 19 Oct 2026: the physics unit tests pass; the mod folder passes `scripts/check-mod-layout.py`. Not
  loaded in the game.
