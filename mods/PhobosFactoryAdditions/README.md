# D'thorga Factory Additions

Additions to **Vanilla Furniture Expanded - Factory**: a **"Belts may take from here" switch** on every storage building and
factory hopper; an **automated fabrication bay** for bionics, spacer gear and vehicle parts; a **fix for every factory machine**,
which no longer destroys a finished batch; a **deep factory hopper**; **piped water** for the conveyor oven with Dubs Bad Hygiene;
and new bills, from neutroamine out of human meat to other mods' drug lab recipes, each switched on only while its mod is active.

One of House D'thorga's mods (also spelt dthorga).

Version 0.16.0, built 28 November 2026 for House D'thorga. **Partly tested in game:** the human-meat bill (owner, 28 October 2026),
the drug lab bills appearing on their machines (29 October 2026), belts taking from switched-on storage only when they point away
from it (29 and 30 October 2026), a bill with no recipe taken off a machine (10 November 2026), and a deep hopper taking four
stacks of output. Two of these passed only in part. Everything else is not yet tested in game.

## Read this first (gotchas)

- **Partly tested in game**: the five items on the version line above, a deep hopper taking four stacks of output among them.
  Two passed only in part; the list, with dates, ends the guide's [Factory chapter](../../docs/player-guide/factory.md#tested-in-game).
  **Everything else is not yet tested in game.**
- **Needs Harmony, D'thorga Framework, Vanilla Expanded Framework and Vanilla Furniture Expanded - Factory.** Load it after all
  four, and after every mod it adds bills for. Its About file asks for that order, so RimSort sorts it for you.
- **RimWorld 1.7 only.**
- **Each tie-in loads only while its mod is active**: without a mod you see none of its bills. The list is under [What it
  adds](#what-it-adds).
- **A machine draws only from its three input cells, and nobody stocks them for it.** Pawns never carry ingredients to a machine
  that feeds from hoppers, and a hopper at its default **Low** priority is never stocked from better storage. **Feed each
  ingredient by belt, or raise the input hoppers' priority** (Critical works), and tick the ingredient in the hopper's filter.
- **Machines wait at a full output hopper** instead of destroying the batch, so they may sit idle where they used to lose items
  silently: **run a belt out of every output hopper.**
- **The "Belts may take from here" switch starts off on storage** (including what you have already built) **and on for hoppers.**
  Only a belt pointing straight away from switched-on storage takes from it, and only what the storage's filter allows. **A belt
  touching the corpse fridge empties it** unless the fridge's switch is off.
- **A corpse gives its meat once.** A line that lived on endless meat, such as neutroamine from human meat, needs real corpses.
- **The stock "Synthesize neutroamine" bill stops taking human meat** once this mod is on: add the human-meat bill if you want it.
- **The fabrication bay and the deep factory hopper look exactly like the machining bay and the ordinary hopper** until they have
  art of their own: select them to tell them apart.
- **Settings to decide before a colony, with Dubs Bad Hygiene:** the conveyor oven's soup, mass-produced meal, baby food and
  pumpkin coffee bills **pause until a pipe touching the oven reaches a water tower**, in colonies you already have too, and food
  cooked with contaminated water can poison. Both are settings, on by default ([Settings](#settings)).
- **Safe to add to a running colony.** Bills whose recipe changed in an update bring themselves up to date.
- **Before removing it:** take every bill this mod added off each machine's **Processes** tab, let running batches finish, and
  **deconstruct every deep factory hopper and fabrication bay**; items on a hopper stay where they are. Otherwise the first load
  afterwards should log an error about a missing process and drop that bill (expected, not yet checked). Bills set aside for a
  missing recipe are lost, and pipes laid under ovens stay as ordinary hidden pipe. Every mod's steps are in [Removing a
  mod](../../docs/player-guide/getting-started.md#removing-a-mod).

## What it adds

How to use each part, with its tables, is in the player guide's [Factory chapter](../../docs/player-guide/factory.md).

- **Belts that take from any storage**: a switch on every storage building, from any mod (shelves, pallets, Rimefeller's unloading
  bays, Adaptive Storage buildings), and on factory hoppers, corpse fridges, chemfuel taps and the bottle tap. Stuck empty belts
  start again by themselves.
- **The output guard**: every factory machine waits at a full output hopper instead of losing its batch, and a batch bigger than
  one stack comes out in parts.
- **The machining bay fills a whole stack** of anything that stacks (hand-held radios, for one), where VFE Factory stopped it at two.
- **The automated fabrication bay**: what the fabrication bench, bioferrite shaper, Rimatomics machining table and vehicle parts
  bench make, built as bills when the game loads. The vehicle parts bills need Vehicle Framework.
- **The deep factory hopper**: four stacks on one cell.
- **New bills**: neutroamine from human meat or neutroaleaves, medicine from synthread, soup on the conveyor oven (Vanilla Cooking
  Expanded), and other mods' drug lab recipes on the granulator and refinery.
- **With The Dead Man's Switch**, its feed hopper works as a factory hopper, and factory hoppers link to its automatic benches.
- **Tie-ins, each only while its mod is active**: Medicines+ (with Deep Pills' infusions, which need both), Ushankas Biological
  Warfare, Combat Extended, Dubs Rimatomics, Hemofungus, Complementary Odyssey, Glitterworld Medicine Production, Farmable
  Neutroamine, Passive Exoskeletons, Simple Washing Machine, Vanilla Cooking Expanded, Vanilla Chemfuel Expanded, Vanilla Races
  Expanded - Android, [sbz] Fridge, The Dead Man's Switch and Dubs Bad Hygiene.

**Is it working?** The startup log has lines starting "[D'thorga Factory Additions]": how many storage types got the switch, and
how many bills the fabrication bay got from each bench. If a Vanilla Expanded update changes code a part relies on, the log names
the part that switched off, and everything else carries on.

## Settings

In **Options > Mod settings > D'thorga Factory Additions**. The page appears only while Dubs Bad Hygiene is active; both take
effect at once.

| Setting | What it does | Default |
| --- | --- | --- |
| Factory bills need piped water | On: the conveyor oven's wet bills draw water from your plumbing and pause while there is none. Off: they need no water. Lite mode never needs water | On |
| Contaminated water can poison food | On: food cooked with water from a contaminated tower may give food poisoning. Off: water quality makes no difference. Food already poisoned stays poisoned | On |

The **Belts may take from here** switch is not a mod setting: it is a button on each storage building, saved with it.

## More

- [The player guide's Factory chapter](../../docs/player-guide/factory.md): using each part, and what is tested in game.
- [The changelog](CHANGELOG.md): what changed in each version.
- [For modders](../../docs/modding/factory-additions.md): bills, hoppers and belts in your own mod, and the checks done.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
