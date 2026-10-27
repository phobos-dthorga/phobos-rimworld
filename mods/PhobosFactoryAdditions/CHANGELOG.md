# D'thorga Factory Additions changelog

Maintained from 28 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.1.0] - 2026-10-28 - Draft

The first slice: human meat as a neutroamine feedstock, as its own bill. Not yet tested in game.

### Added

- The bill "Synthesize neutroamine from human meat" on VFE Factory's neutroamine synthesizer: 20 human meat
  and 3 chemfuel give 6 neutroamine in 4800 ticks, with the stock bill's input slots, sounds and effects.
  No Ideology thought fires (owner's choice).
- The mod skeleton: About.xml (packageId PhobosDthorga.FactoryAdditions, RimWorld 1.6 and 1.7), load folders,
  a placeholder preview image drawn by a script (record beside it), this changelog and the README.
- Bill and description text written by the agent, held for the owner's review.

### Changed

- The stock "Synthesize neutroamine" bill no longer takes human meat (it took any raw meat by category), and
  its description now says so. Human meat in a hopper feeding the stock bill waits until the new bill is added.

### Balance

- 6 neutroamine per batch against the stock bill's 5 is an authored value: the lore renders fat, and a human
  carcass is far fattier than lean game. The README gives the figures and their sources.

### Dependencies

- Vanilla Expanded Framework and Vanilla Furniture Expanded - Factory, both required.
- Optional: Vanilla Chemfuel Expanded, under which the new bill can draw chemfuel from the pipe network.

### Save compatibility

- Safe to add mid-save. Before removing, take the human-meat bill off every synthesizer and let batches
  finish; the first load without the mod is expected to log one missing-process error otherwise (not checked).

### Compatibility and limits

- Loads beside Kimoju's "VFE Factories: Buffs and tweaks" (insect and fish bills); both add to the same list.
- Another mod that replaces the stock bill's description wins or loses by load order; the exclusion is
  added to an existing exclusion list rather than duplicated.
- Not yet tested in game.
