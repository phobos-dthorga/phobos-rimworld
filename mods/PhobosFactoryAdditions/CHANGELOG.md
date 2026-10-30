# D'thorga Factory Additions changelog

Maintained from 28 October 2026. Draft entries record preparation, not Steam publication; nothing is
published yet. Not yet tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.7.0] - 2026-10-30 - Draft

Stuck conveyor belts start again by themselves, and belts take only what the corpse fridge's or a switched-on
storage's filter allows. Found in the owner's colony, where the human-meat neutroamine line had stopped. Not
yet tested in game.

### Added

- For modders: Phobos.Factory.BeltFilterExtension, a marker for a storage Def that VFE Factory treats as a
  factory hopper. Belts then take only what that storage's filter allows. The corpse fridge carries it.
### Changed

- Belts take only what a switched-on storage building's own filter allows. Before, they took everything on it.
  The owner's one switched-on building and all 25 Rimefeller unloading bays allow what they hold (checked in the
  save). Agent choice while scoping, held for the owner's review.
- Factory hoppers, deep factory hoppers and chemfuel taps are unchanged: belts take whatever is on them. A
  machine puts its output on a hopper whatever the hopper's filter says, and the owner's chemfuel tap allows
  nothing yet still makes chemfuel, so obeying those filters would stop working lines. Agent choice, held for
  the owner's review.
### Fixed

- A VFE Factory belt stuck empty but waiting now wakes and carries on. Such a belt skipped its whole update,
  never took from the hopper behind it again, and stayed stuck through a reload and Change direction, so the
  only fix was rebuilding it. The owner's save had nine, two of them between the mincer's hopper (75 human meat)
  and the neutroamine synthesizers. Belts already stuck in a save wake on their first tick. Covers underground
  entrances and exits too. What makes belts stick is not known yet: each wake is logged with the belt's
  position, up to 10 a session.
- A stray item on the corpse fridge no longer jams the corpse line. Belts took the first thing on the fridge,
  whatever it was; in the owner's colony a steel slag chunk rode out with the corpses, the mincer's corpse-only
  hopper refused it, and every corpse belt behind it waited for ever. Now belts take only what the fridge's
  storage filter allows (its storage group's filter when linked), and haulers take the rest away.
### Save compatibility

- Nothing new is saved. Safe to add to and remove from a save; belts woken stay woken.

### Compatibility and limits

- A belt holding an item the storage ahead refuses still waits, as in VFE Factory: right-click it with a
  colonist and pick Haul from conveyor.
- Under Combat Extended, Rimefeller lets an unloading bay hold FSX but doesn't tick FSX in a new bay's filter.
  Tick it before switching a bay on, or its belt leaves the FSX.
- Cost, estimated from the code and not measured: two field checks per belt per tick for stuck belts; the filter
  check only when a belt takes from the corpse fridge or switched-on storage; nothing for the fridge check when
  [sbz] Fridge isn't loaded.
- Two new Harmony patches on VFE Factory's belt: a prefix on its tick, and a prefix on its pull that replaces the
  pull for filter-marked storage only. If an update renames what they use, the log says which part switched off.
## [0.6.1] - 2026-10-30 - Draft

Only a belt that points away from switched-on storage takes from it; belts that run past it leave it alone.
Tested in game by the owner on 30 October 2026 (RimWorld 1.7, 0.6.1 installed): "The fix works brilliantly".
Machine input cells on switched-on storage and the switch surviving a save and load are not yet reported.

### Fixed

- A belt that runs past a switched-on storage building no longer takes from it. Only a belt on a cell next to
  the storage that points straight away from it takes, so belts can pass or wrap around switched-on storage
  without emptying it. The owner found this in game on 29 October 2026: belts running along a thermostatic
  fridge cabin were all taking its psychoid leaves. 0.6.0 had copied the factory hopper's rule, which feeds
  belts that have the hopper behind them or at either side.
- Corrected 0.6.0's notes: a factory hopper at a belt's side does not go before switched-on storage. VFE Factory
  tries the cell behind a belt first, so storage behind a belt goes first.

### Compatibility and limits

- Factory hoppers, VCHE chemfuel taps and the sbz corpse fridge keep VFE Factory's own rule: a belt with one
  behind it or at either side takes from it.
- A little cheaper: each belt now looks at one cell for switched-on storage about once a second, instead of
  three. An estimate from the code, not measured.

### Save compatibility

- Nothing saved changes. Storage switched on under 0.6.0 stays on.

## [0.6.0] - 2026-10-29 - Draft

Belts and factory machines can take from any storage building once the player switches it on. Tested in game
by the owner on 29 October 2026 (RimWorld 1.7): the switch and its inspect line appear on an Adaptive Storage
fridge, and belts take whole stacks from it, but belts at its sides took from it too (fixed in 0.6.1). Machine
input cells and the switch surviving a save and load were not reported.

### Added

- A Belts may take from here switch on every storage building whose items lie on its cells, from any mod:
  vanilla shelves, VFE furniture storage, Rimefeller's refinery unloading bays, every Adaptive Storage building
  (Warehouse Storage, the sbz mods, Cargo Holds, Adaptive Ideology Storage) and more, about 135 building types
  on the owner's mod list. One rule in code instead of a patch per building. Owner's choice of design (a switch
  per storage, over flagging every storage or a separate loader belt), 29 October 2026.
- While it is on, a VFE Factory belt with the storage directly behind it or at either side takes one whole
  stack at a time whenever it has room, as it would from a factory hopper, and a factory machine takes
  ingredients from it when it stands on one of the machine's input cells. A factory hopper beside the same belt
  still goes first.
- The switch says what it did: a message when it changes, and a line in the storage's inspect text while it is
  on. Its text is agent-written, held for the owner's review.

### Compatibility and limits

- Off for every storage, new or already built, so nothing moves until the player switches it on. Everything on
  a switched-on storage can go, whatever the belt's filter says.
- A belt fed from storage looks for a stack every 10 ticks, so it is a little slower than one fed from a factory
  hopper. Storage with the switch off costs nothing; each belt adds a look at three cells about once a second.
  An estimate from the code, not measured.
- Machines still put finished goods only into factory hoppers; the switch is for taking.
- Graves, sarcophagi, growth vats, biosculpter pods, outfit stands, bookcases and sealed crates get no switch:
  they keep their contents inside themselves. Factory hoppers, VCHE chemfuel taps and the sbz corpse fridge
  already feed belts all the time and get no switch.
- Three Harmony postfixes: two on private VFE Factory belt methods, one on Pipe System's input-cell check. If an
  update renames one, only that part switches off, with an error in the log.

### Dependencies

- The assembly now also compiles against VFE Factory's own assembly, referenced from the installed mod and
  never shipped. VFE Factory was already required.

### Save compatibility

- Safe to add mid-game. Each switched-on storage saves one setting (key PhobosFactory_BeltsMayTake); removing
  the mod should drop it without a message (expected, not yet checked) and leaves the items where they are.

## [0.5.1] - 2026-10-29 - Draft

Factory machines stop destroying finished batches, on every machine instead of only the machining bay. Not
yet tested in game.

### Fixed

- No factory machine destroys a finished batch any more; it waits until the output hopper has room. A
  read-only scan of the owner's mods found 74 of the 134 bills that output to factory hoppers at risk, nearly
  all of them VFE Factory's own (smelter, biofuel refinery, alloy forge, assembler, ammunition press,
  distillery, cannery, autoloom, conveyor oven, masonry saw, mincer, drill platform, medicine granulator,
  neutroamine synthesizer), plus Kimoju's and Munitions Industries' bills and every machining bay bill VFE
  generates. Pipe System adds a batch to a matching stack while that stack holds no more than the bill's
  output limit, but most limits equal the stack limit (150 chemfuel, 75 steel), so a full stack swallowed
  every later batch, and a nearly full one the part that did not fit. Owner's go-ahead, 29 October 2026.
- The fix is a check just before Pipe System's output step: when the batch would not fit on the stack it is
  about to join, the machine waits and its inspect text says the output hopper is too full, as Pipe System
  does when a hopper is above its limit. It covers batches whose size varies (mincing corpses, ovens,
  smelting, mechanoid shredding), which 0.5.0's fixed limits could not.
- Bills with no output limit at all (VFE's stock neutroamine bill, Kimoju's drill and insect and fish
  neutroamine bills) drop a batch that does not fit on a free cell beside the machine, Pipe System's own
  fallback for such bills, instead of losing it.

### Changed

- Replaces 0.5.0's machining-bay-only fix (lowered output limits, and the Phobos.Factory.SafeOutputLimitsExtension
  patched onto the bay), which is removed. Machining bay bills keep VFE's limit of 1, so the inspect text now
  reads "Output factory hopper too full (max 1 units)" instead of 0.5 units.
- The fabrication bay's bills set their output limit to the item's stack limit or 75, whichever is lower.
- Each Harmony patch is applied on its own, so if a Vanilla Expanded update changes the output step, only
  the guard switches off (with an error in the log) and the fabrication bay's bills still work.

### Compatibility and limits

- The check runs in Pipe System's output step for every mod's factory bills, only when a batch would
  otherwise be lost. It reads the output cell and makes one throwaway copy of the item; it does not change
  the game's random numbers (random outputs, such as the drill's metal, come out the same).

### Save compatibility

- Safe to install over 0.5.0 mid-game. Nothing is saved.

## [0.5.0] - 2026-10-29 - Draft

An automated fabrication bay for fabrication bench, bioferrite shaper and Rimatomics machining table items,
and a fix for VFE Factory's machining bay losing finished items. The mod's first code. Not yet tested in game.

### Added

- Automated fabrication bay (Architect, Factories; Complex factories and Fabrication research): a factory
  machine with a bill for every item made at a fabrication bench (bionic and other artificial body parts,
  spacer guns, spacer armour and helmets; about 135 on the owner's mod list), with Anomaly every item made at
  a bioferrite shaper (about 18), and with Dubs Rimatomics every item made at its machining table (27). The
  owner asked on 29 October 2026, after noticing that MSE2's bone replacements had machining bay bills and
  bionics did not (VFE Factory automates only machining table items). Owner's choices: these three benches
  and a separate building.
- The bills are built at load from each item's recipe after every mod's changes: its cost as ingredients
  (steel, any textile or any leather for a material, as VFE Factory does), four times the bench's work, the
  bench's research plus the item's own. A bill is offered only while its bench recipe is available, so
  Ideology memes and Rimatomics' own research still gate it. Chemfuel comes from the pipes with Vanilla
  Chemfuel Expanded. The startup log lists how many bills each bench gave and what was skipped.
- The building inherits VFE Factory's machining bay (size, input and output cells, power, heat,
  overclocking, facilities, piped chemfuel, graphics, its two mechanoid shredding bills), so it looks the
  same until it has art of its own.
- A bench bill template Def (Phobos.Factory.BenchBillTemplateDef), so another mod can automate another bench
  in XML; the README shows how.
- The first C# assembly (PhobosFactory.dll, built for 1.6, loaded on 1.7 too) and two Harmony patches: a
  postfix on the game's implied Def generation (where the bills are made, as VFE Factory makes its own) and a
  postfix on Pipe System's Add process menu (hides a bill whose bench recipe is not yet available; Pipe
  System offers no other hook).
- scripts/check-factory-patches.py checks the bench templates' references; game-free unit tests cover the
  output limit rule (tests/PhobosFactory.Rules.Tests).
- Bill names, descriptions and the building's description written by the agent, held for the owner's review.

### Fixed

- VFE Factory's automated machining bay no longer loses a finished item when an identical one is still in
  its output hopper; the machine waits instead (owner's go-ahead, 29 October 2026). Its bills set an output
  limit of 1, and Pipe System adds a finished item to a matching stack while that stack holds no more than
  the limit; the game counts two identical guns as stackable at a stack limit of 1, none of the new one fits,
  and the rest was never placed. Every bill on the machining bay now gets the largest limit that loses
  nothing (0.5 for an item that does not stack, so any identical item makes it wait). Read in Pipe System's
  and the game's 1.7 code; not seen in game. The two mechanoid shredding bills are unchanged: their output
  depends on the mechanoid's size.
- 0.4.0's note that a deep factory hopper holds four finished weapons as an output was wrong for identical
  items (see above); it holds four different ones.

### Balance

- Fabrication bay: 300 steel, 150 plasteel, 12 components, 10 advanced components, 7000 work (machining bay:
  440 steel, 18 components, 5 advanced components, 5000 work). Agent defaults, owner to tune.
- Bills take four times the bench's work, no skill, normal quality, as VFE Factory's machining bills do.

### Dependencies

- Harmony is now required (VFE Factory already requires it).

### Compatibility and limits

- Not automated: items some machine already makes, buildings (the hologram base), and items made of a
  material plus parts (Pandora's Exopack, ExoHelmet and ExoVisor; Anomaly's ceremonial hood): Pipe System
  gives a finished item the material of the last ingredient it took, so they could come out made of
  components.
- Many spacer items need four to six ingredients and the bay has three input cells: use deep factory hoppers.
- Items that do not stack leave one at a time: the inspect text then reads "Output factory hopper too full
  (max 0.5 units)" until the finished item is taken away.
- Bills follow the installed mods at each start: a mod update that changes a recipe changes its bill.
- The bill counts are estimates from the mods' files before patches; the startup log has the real ones.

### Save compatibility

- Safe to add mid-game. Before removing the mod, take the fabrication bay's bills off, deconstruct every
  fabrication bay, and expect queued bills to be dropped with an error otherwise (not checked). The
  machining bay fix stores nothing.

## [0.4.0] - 2026-10-29 - Draft

A deep factory hopper that holds four stacks. Not yet tested in game.

### Added

- Deep factory hopper (Architect, Factories, beside the factory hopper): holds up to four stacks on its one
  cell instead of one. A machine takes every ingredient it still needs from an input cell, so one deep
  hopper can feed several ingredients, and a belt carrying mixed items keeps filling it instead of stopping
  behind one surplus stack. Asked for by the owner on 29 October 2026, after sharing one ordinary hopper
  between two ingredients clogged.
- It inherits everything else from VFE Factory's own hopper (looks, input and output markings, the allow
  taking and allow inserting toggles, storage groups, research), so it follows VFE Factory's updates.
- scripts/check-factory-patches.py also checks that every ParentName in the mod resolves, and that a patch
  adding a Name does not meet one already there.

### Balance

- 35 steel and 1 component, 800 work (the stock hopper: 20 steel, 400 work). Four stacks, one per solid
  ingredient of a four-ingredient bill. Agent defaults, owner to tune.

### Compatibility and limits

- It looks exactly like the ordinary hopper on the map: VFE Factory's hopper class draws fixed textures.
  Tell them apart by the name when selected.
- Used as an output hopper, it holds up to four different finished items that do not stack. (Corrected in
  0.5.0: this entry first said four of any kind; an identical one was lost until the 0.5.0 fix, and now
  waits.) Items that stack still wait at the bill's output cap, as in an ordinary hopper.

### Save compatibility

- Safe to add mid-game. Before removing the mod, deconstruct every deep factory hopper; the items on it stay
  on the ground.

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
