# D'thorga Factory Additions changelog

Maintained from 28 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.3.2] - 2026-10-29 - Draft

Restores two pieces of VFE Factory's own Vanilla Chemfuel Expanded support that a mod clash skips. Not
yet tested in game.

### Fixed

- With Vanilla Chemfuel Expanded: its chemfuel tap counts as a factory hopper again, so belts pull chemfuel
  from it and a machine whose input cell lands on it takes chemfuel straight from it. With Vanilla Gravship
  Expanded as well, the automated biofuel refinery's astrofuel bill draws chemfuel from the pipe network
  again. VFE Factory sets both up, but its patch stops at an earlier step when Combat Extended and Vanilla
  Factions Expanded - Deserters are also active (CE removes a shell bill that step edits). Each repair
  checks first and does nothing if the change is already there. Owner's go-ahead, 29 October 2026.

### Save compatibility

- Safe to add mid-game. Nothing saved changes. Chemfuel taps already built become factory hoppers.

## [0.3.1] - 2026-10-29 - Draft

Fixes 0.3.0's bills not appearing on the machines. Tested in game by the owner on 29 October 2026 (RimWorld
1.7, with 0.3.2 installed): the bills now appear on the granulator, the synthesizer (human meat included)
and the refinery.

### Fixed

- The 0.3.0 bills now reach their machines. Seven optional folders each had a patch file called
  Patches/MedicineGranulator.xml, and two had Patches/BiofuelRefinery.xml. RimWorld loads only one file per
  path inside a mod's load folders (the one in the folder listed last) and skips the rest without a message.
  So with the owner's mods only glitterworld medicine (still behind its research), the two detergents and
  leaf neutroamine were added; the Medicines+, Deep Pills, Biological Warfare, psycho, potassium iodide,
  hemogen, vacsop and polyamide bills existed but were on no machine. Reported by the owner on 29 October
  2026; cause read in the game's loader code the same day.
- The human-meat bill is back in the neutroamine synthesizer's Add process menu. With Farmable Neutroamine
  active, 0.3.0's Patches/NeutroamineSynthesizer.xml for leaf neutroamine replaced the Common one, which
  also dropped the stock bill's human-meat exclusion and its corrected description. Bills already queued
  kept working, because the bill's Def still loaded.
- Every patch file in an optional folder now carries that folder's name (MedicineGranulator_MedicinesPlus.xml
  and so on), so no two files share a path.

### Changed

- The repository checks now know the rule: scripts/check-mod-layout.py refuses any Defs or Patches file
  that shares a path with one in another load folder of the same version, and
  scripts/check-factory-patches.py checks only the files the game would load. Run against the 0.3.0 files,
  it reports the nine skipped patch files and the 41 bills on no machine; on 0.3.1, no errors.

### Save compatibility

- Safe to install over 0.3.0 mid-game. No saved data changes; bills already queued are unaffected.

### Compatibility and limits

- Separate from this mod: with Combat Extended, Vanilla Chemfuel Expanded and Vanilla Factions Expanded -
  Deserters together, VFE Factory's own chemfuel patch stops partway (CE removes a shell bill it patches),
  so its last two steps never run: astrofuel from piped chemfuel, and chemfuel taps as factory hoppers.
  Seen in the owner's log on 29 October 2026; not changed here.

## [0.3.0] - 2026-10-28 - Draft

Drug lab recipes from other mods as factory bills, 44 of them, each loaded only while its mod is active.
Not yet tested in game.

### Added

- Medicine granulator, with Medicines+: a bill for each of its 14 drugs (aniracetam, AntiTox, clozapine,
  cyanide, heroin, hydra, Joy, addictol, morphine, penicillin, smelling salts, ThornWeed serum, Tyrox,
  Upper), batch of four at four times the lab work, with the mod's own research.
- Medicine granulator, with Medicines+: a penicillin-from-fungus bill (40 raw fungus and 4 neutroamine
  give 4), an authored alternative path; the README gives the reasoning.
- Medicine granulator, with Deep Pills and Medicines+: the three infusions (peyote, opium, thorn weed),
  batch of eight from the x8 lab recipe.
- Medicine granulator, with Ushankas Biological Warfare: a vaccine and an antiserum bill for each of its
  seven diseases, and the malaria and sleeping sickness pills (one at a time, since a sample does not stack).
- Medicine granulator: psycho (Combat Extended), potassium iodide (Dubs Rimatomics), hemogen packs from
  hemofungus (Hemofungus), vacsop from steel and vacsop from vacstone blocks (Complementary Odyssey), and
  glitterworld medicine (Glitterworld Medicine Production) with five input cells for its four ingredients.
- Neutroamine synthesizer, with Farmable Neutroamine: neutroamine from neutroaleaves, 25 leaves to 5.
- Automated biofuel refinery: polyamide (Passive Exoskeletons) and both laundry detergents (Simple Washing
  Machine).
- With Vanilla Chemfuel Expanded, the refinery bills draw chemfuel from the pipe network; with Vanilla
  Races Expanded - Android, the granulator bills that take neutroamine draw it from that network.
- A repository check, scripts/check-factory-patches.py, that reads the installed game and active mods and
  verifies every patch xpath, Def name and output cap in this mod (run against the owner's install: clean).
- Bill names and descriptions written by the agent, held for the owner's review.

### Fixed

- The human-meat bill now sets an output cap of 75. Without one, Pipe System destroys a finished batch
  whose output cell is blocked instead of waiting (code read 28 October 2026; the stock bill has the same
  gap).

### Balance

- Every port follows VFE Factory's own rule for its drug bills: inputs times four, lab work times four per
  unit, no skill requirement. Output caps are set below each product's stack limit (drugs 150, medicine 25,
  hemogen packs 10, vaccines and antiserums 15) so no batch is lost.
- The fungus penicillin ratio (10 raw fungus per unit) is authored, meant to be bulkier than the herbal
  path, not cheaper.

### Dependencies

- No new required mods. Optional: Medicines+, Deep Pills, Ushankas Biological Warfare, Combat Extended,
  Dubs Rimatomics, Hemofungus, Complementary Odyssey, Glitterworld Medicine Production, Farmable
  Neutroamine, Passive Exoskeletons, Simple Washing Machine, Vanilla Races Expanded - Android.

### Save compatibility

- Safe to add mid-save. Before removing, take every bill this mod added off each machine and let batches
  finish (as for 0.1.0).

### Compatibility and limits

- Luciferium Expansion's recipes are not ported (owner's choice). Pathogen samples, the disease sampler,
  flesh-breaker, the rumsuit and the medic bag stay at the drug lab.
- Values were read from the source mods as installed on 28 October 2026; a source mod update can change its
  lab recipe without changing these bills.
- The two extra corner input cells for glitterworld medicine are outside VFE Factory's own convention and
  untested; a belt into one of the three usual cells is the fallback.
- Not yet tested in game.

## [0.2.0] - 2026-10-28 - Draft

The corpse fridge as a source for belts and machines. Not yet tested in game.

### Added

- With "[sbz] Fridge" (seobongzu) active, its corpse fridge counts as a factory hopper: conveyor belts with
  the fridge behind them or at either side pull corpses out of it, and an automated mincer whose input cell
  lands on the fridge takes corpses straight from it. One Def extension, loaded only while that mod is on.

### Compatibility and limits

- Any belt touching the fridge on the belt's back or sides will empty it whenever the belt has room; the
  belt's own filter does not stop that (stock hopper behaviour).
- Only the corpse fridge is flagged; the mod's other fridges are unchanged.
- Nothing is written to the save; removing the mod only stops the pulling.
- Not yet tested in game.

## [0.1.0] - 2026-10-28 - Draft

The first slice: human meat as a neutroamine feedstock, as its own bill. Tested in game by the owner on
28 October 2026 (RimWorld 1.7): the human-meat bill works.

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
- The stock bill's exclusion of human meat and removal from a save are not yet tested in game.
