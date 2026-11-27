# D'thorga Water Tap

A water bottle tap for **Dubs Bad Hygiene**: a small plumbing fixture that keeps a stack of bottled water ready and refills it by
itself from your water towers. Nobody has to stand at a basin filling bottles.

One of House D'thorga's mods (also spelt dthorga).

Version 0.3.0, built 28 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Not yet tested in game.** The owner has not passed any of it yet. It is built, unit-tested and read against the game's and
  Dubs Bad Hygiene's code, but not yet seen working in a colony.
- **Needs Dubs Bad Hygiene with its thirst option on, and D'thorga Framework.** Without thirst there are no water bottles, so the
  tap doesn't appear at all. **It isn't made for Dubs Bad Hygiene Lite**, or the full mod's lite mode: neither has plumbing. No
  Harmony needed.
- **Load it after Dubs Bad Hygiene and D'thorga Framework.** Its About file asks for that order, so RimSort sorts it for you.
- **RimWorld 1.7 only.**
- **It changes every water bottle in your game, not just the tap's.** A bottle split from an untreated stack stays untreated
  (before, it came out clean by mistake), so colonists on untreated water can get sick from bottles, as from a basin.
- **No settings to decide before a colony.** How many bottles a tap keeps, and the map's colony limit, are buttons on each tap.
- **Safe to add to a running colony.** Existing bottles keep working; any already split read as treated.
- **Safe to remove once you deconstruct every tap, and lay a pipe back where each stood**, or the plumbing is cut in two. A tap
  left standing vanishes with the mod and leaves a gap in the pipe. The first load afterwards logs two errors about a missing
  class (read from the game's code, not yet seen); the game goes on, and they stop once you save. Every mod's removal steps are in
  [Removing a mod](../../docs/player-guide/getting-started.md#removing-a-mod).

## What it adds

- **The water bottle tap**, under **Architect > Hygiene** after the plumbing research. Build it on a pipe or beside one, on a
  network with a water tower: it draws from towers only, like every Dubs Bad Hygiene fixture.
- **Colonists drink from it and pack bottles from it**, and caravans load from it. Bottles stay on the tap until someone takes
  them: its storage priority starts at Important.
- **A colony limit**, 50 bottles a map to start: once the map holds that many, every tap stops filling.
- **Water quality kept in every bottle**: a bottle holds the water that filled it, bottles of different water don't stack, and
  labels say "water (untreated)". The tap won't bottle contaminated water.
- **With Vanilla Furniture Expanded - Factory**, conveyor belts and factory machines can take bottles from the tap.

How to use it, its buttons and what its inspect pane says are in the player guide's
[Water tap chapter](../../docs/player-guide/water-tap.md).

| What | Value | Where it comes from |
| --- | --- | --- |
| Bottles kept ready | 0 to 10 a tap, starts at 10 | One stack, the bottle's stack limit |
| Colony limit | 0 to 1,000 a map, starts at 50, in steps of 10 | Authored |
| Fill rate | 1 bottle every 250 ticks, 240 a day at most | Authored |
| Water per bottle | 0.5 L | Dubs Bad Hygiene's own charge for packing a bottle, and the bottle's half-kilogram mass |
| Most water used | 120 L a day a tap | From the two above |
| Cost | 20 steel, 600 work | Authored |
| Power | None | The pumps supply the pressure: a gameplay simplification |

## Settings

None in the mod settings window. Each tap has its own buttons: **Fewer bottles** and **More bottles** (how many it keeps, 0 to
10), and **Colony limit -10** and **+10** (the map's limit, 0 to 1,000).

## More

- [The player guide's Water tap chapter](../../docs/player-guide/water-tap.md): using it, and what is tested in game.
- [The changelog](CHANGELOG.md): what changed in each version.
- [For modders](../../docs/modding/water-tap.md): building your own tap in XML, and the checks done.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
