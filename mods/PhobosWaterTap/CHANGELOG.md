# D'thorga Water Tap changelog

Maintained from 4 November 2026. Draft entries record preparation, not Steam publication; nothing is published
yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.0] - 2026-11-04 - Draft

First version, at the owner's request: a constant supply of bottled water for the colony, like Vanilla Chemfuel Expanded's chemfuel tap.
Not yet tested in game. The building's description and in-game text are agent-written, held for the owner's review.

### Added

- A water bottle tap under Architect, Hygiene, after researching plumbing: 20 steel, one cell, no power. It joins your pipes like a basin and keeps
  up to 10 bottles of water ready, making one every 250 ticks from the water towers on its network.
- Fewer bottles and More bottles commands, which say what they did. At 0 the tap stops filling.
- Inspect text that names what the tap is doing or the one thing it waits for.
- Each bottle holds the quality of the water that filled it. The tap refuses contaminated water.
- With Vanilla Furniture Expanded - Factory, belts and machines can take from the tap.

### Changed

- Every water bottle now keeps its water quality when its stack is split, and bottles of different water no longer stack. Dubs Bad Hygiene had no rules
  for this, so a bottle split off an untreated stack came out treated. Labels show untreated and contaminated water.

### Balance

- Fill rate, cost and work are authored values. Each bottle costs 0.5 L, Dubs Bad Hygiene's own charge for packing one and the water in a bottle of its
  0.5 kg mass.
- Colonists drinking bottled untreated water can now get sick, as they already can from a basin. Before, a split stack hid this.

### Dependencies

- Needs Dubs Bad Hygiene, with its thirst option on. Without thirst the tap isn't loaded, because the bottle doesn't exist. Vanilla Furniture Expanded - Factory
  is optional. No Harmony patches.

### Save compatibility

- Safe to add to a running colony. Bottles already split read as treated.
- Safe to remove once your taps are deconstructed. A tap left standing vanishes with the mod and leaves a gap in the pipe. The bottle rule saves nothing.

### Compatibility and limits

- Not for Dubs Bad Hygiene Lite or the full mod's lite mode, which have no plumbing.
- Holds one stack, because the tap holds one stack on its one cell. Build more taps for more.
- A tap holding bottles of other water waits until they are taken before it fills again, for example after a water filter loses power.
- If pawns or belts carry bottles to other storage, the tap refills until that storage is full. Its storage priority starts at Important so that this happens only if you set it up.
- The texture and Workshop preview are code-drawn placeholders.
