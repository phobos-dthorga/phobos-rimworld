# D'thorga Factory Additions

Additions to **Vanilla Furniture Expanded - Factory**: a **"Belts may take from here" switch** on every storage building and factory
hopper; an **automated fabrication bay** for bionics, spacer gear and vehicle parts; a **fix for every factory machine**, which no
longer destroys a finished batch; a **deep factory hopper**; **piped water** for the conveyor oven with Dubs Bad Hygiene; and new
bills, from neutroamine out of human meat to other mods' drug lab recipes, each switched on only while its mod is active.

One of House D'thorga's mods (also spelt dthorga).

Version 0.14.0, built 17 November 2026 for House D'thorga. **Partly tested in game:** the human-meat bill (owner, 28 October 2026),
the drug lab bills appearing on their machines (29 October 2026), belts taking from switched-on storage only when they point away
from it (29 and 30 October 2026), and a bill with no recipe taken off a machine (10 November 2026). Everything else is not yet tested
in game.

**How to play with it is in the player guide's [Factory chapter](../../docs/player-guide/factory.md).** Each version's changes are in
the [changelog](CHANGELOG.md).

## Read this first (gotchas)

- **Requires Harmony, D'thorga Framework, Vanilla Expanded Framework and Vanilla Furniture Expanded - Factory.** Load it after all
  four, and after every mod it adds bills for (the About file asks for it, so RimSort does this for you). VFE Factory already needs
  Harmony.
- **Each tie-in loads only while its mod is active** (an `IfModActive` folder per mod): Medicines+ (with Deep Pills' infusions inside
  it, which need both), Ushankas Biological Warfare, Combat Extended, Dubs Rimatomics, Hemofungus, Complementary Odyssey, Glitterworld Medicine Production, Farmable
  Neutroamine, Passive Exoskeletons, Simple Washing Machine, Vanilla Cooking Expanded, Vanilla Chemfuel Expanded, Vanilla Races
  Expanded - Android, "[sbz] Fridge", The Dead Man's Switch and Dubs Bad Hygiene (whose part is a second small assembly). The vehicle
  parts bills need Vehicle Framework.
- **With The Dead Man's Switch, its feed hopper is a factory hopper** (0.14.0): belts take from it what its storage settings allow,
  whether or not its auger has power, and **factory hoppers link to its automatic benches**, which then draw from them with no colonist.
- **RimWorld 1.7 only.**
- **A machine draws only from its three input cells, and nobody stocks them for it.** Pawns never carry ingredients to a machine that
  feeds from hoppers, and a hopper at its default **Low** priority is never stocked from better storage. **Feed each ingredient by
  belt, or raise the input hoppers' priority** (Critical works), and tick the ingredient in the hopper's filter.
- **Machines wait at a full output hopper** instead of destroying the batch, so they may sit idle where they used to eat inputs
  silently: **run a belt out of every output hopper.** Bionics, weapons and armour leave one at a time.
- **A corpse gives its meat once.** A line that lived on the old endless meat, such as neutroamine from human meat, now needs real
  corpses.
- **The "Belts may take from here" switch starts off on storage** (including what you have already built) **and on for hoppers.**
  Only a belt pointing straight away from switched-on storage takes from it, and only what the storage's filter allows. A switched-off
  output hopper fills up and its machine waits. **A belt touching the corpse fridge empties it** unless the fridge's switch is off.
- **With Dubs Bad Hygiene, the conveyor oven's soup, mass-produced meal, baby food and pumpkin coffee bills pause until a pipe touching
  the oven reaches a water tower**, in colonies you already have too. Decide on the two settings below before you rely on it.
- **The stock "Synthesize neutroamine" bill stops taking human meat** once this mod is on: add the human-meat bill.
- **The fabrication bay and the deep factory hopper look exactly like the machining bay and the ordinary hopper** until they have art
  of their own: select them to tell.
- **Safe to add to a running save.** Bills whose recipe changed in an update bring themselves up to date.
- **Before removing it:** remove every bill this mod added from each machine's Processes tab and let running batches finish
  (otherwise the first load afterwards should log an error about a missing process and drop that bill; expected, not yet checked),
  and **deconstruct every deep factory hopper and fabrication bay**; items on a hopper stay where they are. Switch states and
  part-finished batches saved on bills are dropped by the game without a message (expected, not yet checked). Bills set aside for a
  missing recipe are lost. Pipes laid under ovens stay as ordinary hidden pipe.
- **Nothing is published on the Steam Workshop yet.** Install from source with `scripts/deploy.py`.

## What it does

Each part in a line or three; the guide chapter has the tables and how to use each.

- **Belts that take from any storage**: a switch on every storage building (shelves, pallets, Rimefeller's unloading bays, every
  Adaptive Storage building) and on factory hoppers, corpse fridges, chemfuel taps and the bottle tap
  ([belts](../../docs/player-guide/factory.md#belts-that-take-from-any-storage),
  [hoppers](../../docs/player-guide/factory.md#factory-hoppers-the-same-switch-starting-on)).
- **Stuck belts start again** by themselves, and belts take only what a storage's filter allows; the corpse fridge and chemfuel taps
  feed belts and machines ([stuck belts and the fridge](../../docs/player-guide/factory.md#stuck-belts-and-the-corpse-fridge)).
- **The output guard**: every factory machine waits at a full output hopper instead of losing its batch, a batch bigger than one
  stack comes out in parts, and bills whose recipe changed or vanished are repaired or set aside
  ([machines and their output](../../docs/player-guide/factory.md#machines-and-their-output)).
- **The automated fabrication bay**: bills built at load from the fabrication bench, bioferrite shaper, Rimatomics machining table
  and vehicle parts bench ([fabrication bay](../../docs/player-guide/factory.md#the-automated-fabrication-bay)).
- **The deep factory hopper**: four stacks on one cell ([deep hopper](../../docs/player-guide/factory.md#the-deep-factory-hopper)).
- **New bills**: neutroamine from human meat or neutroaleaves, medicine from synthread, cooked soup on the conveyor oven, and other
  mods' drug lab recipes on the granulator and refinery ([new bills](../../docs/player-guide/factory.md#new-bills)).
- **Piped water and food poisoning** with Dubs Bad Hygiene
  ([piped water](../../docs/player-guide/factory.md#piped-water-and-food-poisoning-dubs-bad-hygiene)).

**Is it working?** The startup log has lines starting "[D'thorga Factory Additions]": how many storage types got the switch, and per
bench how many bills the fabrication bay got and what was skipped. If a Vanilla Expanded update changes the code a part relies on,
the log names the part that switched off and everything else carries on.

## Settings

In **Options, Mod settings, D'thorga Factory Additions**. The page appears only while Dubs Bad Hygiene is active; both take effect at
once.

| Setting | What it does | Default |
| --- | --- | --- |
| Factory bills need piped water | On: the conveyor oven's wet bills draw water from your plumbing and pause while there is none. Off: they need no water. Lite mode never needs water | On |
| Contaminated water can poison food | On: food cooked with water from a contaminated tower may give food poisoning. Off: water quality makes no difference. Food already poisoned stays poisoned | On |

The **Belts may take from here** switch is not a mod setting: it is a button on each storage building, saved with it.

## For modders

- [VFE Factory additions: research and decisions](../../docs/development/vfe-factory-additions.md): how bills, hoppers and the output
  step work in Pipe System's code, every decision and who made it, and
  [the README's modder notes](../../docs/development/vfe-factory-additions.md#for-modders-moved-from-the-readme-17-nov-2026) (bill
  rules, the `Mods/<SourceMod>/` file naming rule, `BenchBillTemplateDef`, `BeltFilterExtension`, piped water on your own bills).
- [VFE Factory conveyors](../../docs/development/vfe-factory-conveyors.md): the belt code, and how to make storage feed belts by
  default.

## Checks done

Each version was read in VFE Factory's, Pipe System's and the game's code, built with no warnings where it has C#, unit-tested where
it has rules, and checked against the owner's installation with `scripts/check-factory-patches.py`; each version's checks are in the
[changelog](CHANGELOG.md), and the dated list the README kept until 17 November 2026 is in
[the design record](../../docs/development/vfe-factory-additions.md#checks-moved-from-the-readme-17-nov-2026). **Tested in game by
the owner:** the four items in the version line above. **Not yet tested in game:** everything else, starting with each new bill run
to the end, the corner input cells, a belt taking from a chemfuel tap or the corpse fridge, and removal.
