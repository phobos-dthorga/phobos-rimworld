# VFE Factory conveyors and hoppers: how belts pull and push (code read, 28 Oct 2026)

What decides where a Vanilla Furniture Expanded - Factory belt takes items from and puts them, and how to
let it take from another mod's storage. Read from the decompiled VFE Factory and Pipe System (Vanilla
Expanded Framework) assemblies in the ignored `.local/decompiled/` on 24, 26 and 28 Oct 2026, and from
Adaptive Storage Framework's published source (MIT, shipped in its Workshop folder). **None of it is tested
in game** unless a line says the owner saw it. Dated record; the mod README carries current behaviour.

**Merged here on 17 Nov 2026** from [VFE Factory additions](vfe-factory-additions.md), each kept as written:
[how machines take from hoppers](#how-machines-take-from-hoppers-and-the-deep-factory-hopper-29-oct-2026); [belts that take from any storage](#belts-that-take-from-any-storage-060-29-oct-2026);
[stuck belts and storage filters](#stuck-belts-and-belts-that-obey-storage-filters-070-30-oct-2026); [a belt switch on factory hoppers](#a-belt-switch-on-factory-hoppers-0110-2-nov-2026).

**Status (17 Nov 2026):** what this record found is built in D'thorga Factory Additions: the deep factory hopper (0.4.0), belts
taking from switched-on storage (0.6.0, only the belt behind it since 0.6.1), stuck belts woken and storage filters obeyed (0.7.0),
the belt switch on factory hoppers (0.11.0) and stale bills rebuilt (0.11.4). **Tested in game by the owner:** 0.6.0 and 0.6.1, belts
taking from a switched-on fridge (29 and 30 Oct 2026). Everything else is not yet tested in game.

## The one rule that matters: belts pull only from "factory hoppers"

**Since D'thorga Factory Additions 0.6.0 (29 Oct 2026)** any storage building with its "Belts may take from
here" switch on counts as well, and since 0.6.1 (30 Oct 2026) only for a belt that has it directly behind, not
at its sides; how that is done is in the
[0.6.0 section below](#belts-that-take-from-any-storage-060-29-oct-2026). The rule
below is stock VFE Factory.

`Building_Conveyor.TryPullFromDir` (VFE Factory) looks at the building in one neighbouring cell and pulls
only if **all** of these hold:

1. the building is a `Building_Storage` (vanilla shelves and stockpile buildings, Adaptive Storage
   buildings, hoppers; **not** a stockpile zone, a Rimefeller bay or a pawn);
   **Correction, 29 Oct 2026:** Rimefeller's refinery unloading bay (`RefineryLoadingBay`) *is* a plain
   `Building_Storage`, and its refiners place products on its cell, so it fails only test 2, the flag;
2. it is VFE Factory's own `Building_FactoryHopper`, **or** its Def carries Pipe System's
   `FactoryHopperExtension` with `isfactoryHopper` true;
3. one of its slot cells holds a spawned thing of category Item, read from the map's thing grid
   (`GridsUtility.GetThingList`).

It then takes the **whole stack** (`SplitOff(stackCount)`), despawns it and puts it in the belt. It does not
check the belt's own storage filter, the storage building's priority, or whether the item is forbidden.

Order and timing (`ProcessMovement`, `TryPullItem`): a belt tries to pull on every movement tick while it
has room (`innerContainer.Count < itemsPerCell`, which is 1 for the stock belt) and is not Waiting. It tries
the cell **behind** it first, then its **right**, then its **left**. `hasAdjacentHopper` is cached from a
neighbour scan that applies the same two tests, so a belt with no flagged storage next to it never scans.

Corpses count: VFE Factory's own spawn patch puts a corpse that spawns on a belt cell onto the belt, and
the automated mincer's processes take corpses from hoppers by the same thing-grid read.

## Where belts push

`GetConveyorOrStorageAt` picks, in the cell ahead, a conveyor first, then any `Building_Storage` or
`ISlotGroupParent`, then any other building. Into storage, the belt offers its item only if that storage's
filter accepts it; otherwise it waits for ever ("Waiting"). This needs **no** hopper flag: a belt can already
deliver into a shelf, a fridge or a hopper. Several belts into one belt make a merger; a belt with two or
three belt outputs is a splitter with a Filter tab (details in the memory of 24 and 26 Oct 2026: forward,
left and right outputs only; round robin; a narrowed filter sends allowed items straight ahead only).

**Added 17 Nov 2026** (code read for [universal hoppers](../universal-hoppers.md); not tested in game): a belt whose cell ahead holds
a building with `CompRefuelable` refuels it directly, taking only fuel its filter allows and only what it needs
(`CompleteTransfer`, `Building_Conveyor.cs:1810-1852`; `GetDumpCapability`, `1408-1429`). Ahead of any other building that is
neither a belt nor storage, the belt waits for ever. On open ground it drops one stack per cell, then waits.

## Machines use the same flag

Pipe System's `Process.InputFactoryHopperDetected` returns true for a cell holding any thing whose Def
carries `FactoryHopperExtension` with `isfactoryHopper` true. A process with
`onlyGrabAndOutputToFactoryHoppers` (every VFE Factory process) grabs ingredients only from such cells,
through `GetThingsAndContainedItems`: the cell's spawned things, plus the inner container of anything there
that implements `IFactoryExposedThingHolder` (belts do, so a belt in an input slot feeds the machine).

Pipe System's one other use of the flag: a thing spawned over its stack limit is **not** truncated on map
load if a flagged building shares its cell.

## What the flag does not change

VFE Factory's Harmony patches on forbidding (`ForbidUtility.IsForbidden`, for "allow taking"), on store
cells (`StoreUtility.IsGoodStoreCell`, for "allow inserting"), and the hopper's two toggle gizmos all test
for the `Building_FactoryHopper` **class**, not the extension. A flagged storage building keeps its normal
hauling, filters and priority, and gains no toggles. The item-on-belt drawing also tests the class, so an
item drawn leaving a flagged storage may start at the belt's centre rather than slide out of the building
(cosmetic, untested).

## Adaptive Storage Framework buildings qualify

Adaptive Storage's `ThingClass` derives from `Building_Storage` and keeps stored items **spawned on its
cells**: its code listens to `ThingGrid.DeregisterInCell` to learn when an item leaves, and comments that
"Spawned is always true here". It despawns stored things only when the building is minified with
"pack contents". So the belt's thing-grid read sees them. A 2 x 2 fridge with `maxItemsInCell` 5 holds 20
corpses across its four cells; the belt takes them one at a time (a corpse is a stack of 1).

## A belt that stops for good: empty and Waiting (code read 30 Oct 2026)

`Building_Conveyor.Tick` returns at once while the belt holds nothing and its `state` is `Waiting`, and the
belt pulls from a hopper only while it is not `Waiting`. Only an item pushed in by another belt wakes it, so a
belt fed only by a hopper stays stopped. The state is saved, and "Change direction" doesn't reset it: in stock
VFE Factory the fix is to rebuild the belt. VFE Factory sets `Waiting` only while a belt holds something, so
something outside the belt's code empties these; what it is is not known yet. D'thorga Factory Additions 0.7.0
wakes such belts (details in [the 0.7.0 section below](#stuck-belts-and-belts-that-obey-storage-filters-070-30-oct-2026)).

A belt holding an item that the storage ahead refuses waits for ever as well, and every belt behind it fills
up. VFE Factory's own "Haul from conveyor" right-click (offered while a belt waits with an item) clears it.

## Recipe: let belts and machines take from another storage building

One patch operation on the storage's Def, in a folder loaded only while that mod is active:

```xml
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/ThingDef[defName="sbz_CorpseFridge"]</xpath>
  <value>
    <li Class="PipeSystem.FactoryHopperExtension">
      <isfactoryHopper>true</isfactoryHopper>
    </li>
  </value>
</Operation>
```

VFE Factory does exactly this to Vanilla Chemfuel Expanded's chemfuel tap. The D'thorga Factory Additions
mod does it to the corpse fridge of "[sbz] Fridge" (`Mods/SbzFridge/`). Checks before flagging a building:

- it must be a `Building_Storage` whose items stay spawned on its cells (vanilla shelves and Adaptive
  Storage: yes; a container that moves items into a `ThingOwner`, such as a belt or a pod: no);
- every belt touching it on the belt's back or sides will drain it, filter or not, so warn the player (with D'thorga Factory
  Additions 0.11.0 the player can switch "Belts may take from here" off on it to stop that). With D'thorga Factory Additions
  0.7.0, add `<li Class="Phobos.Factory.BeltFilterExtension" />` beside the flag
  to make belts take only what the storage's own filter allows (the corpse fridge has it);
- a machine whose input cell lands on it will grab from it, which may be the point.

## How machines take from hoppers, and the deep factory hopper (29 Oct 2026)

*Moved here on 17 Nov 2026 from `vfe-factory-additions.md`; kept as written.* Where it says "the next section", it means
[the fabrication bay and the lost-item fix](vfe-factory-additions.md#the-fabrication-bay-and-the-lost-item-fix-29-oct-2026) in that record.


Code read in Pipe System 1.7 and VFE Factory, not tested in game:

- **Machines.** While a bill waits, `Process.CheckInputSlots` visits each `autoInputSlots` cell (rotated with
  the machine), skips it unless a building there carries `FactoryHopperExtension` (for hopper-only bills),
  and offers **every item on the cell** to every ingredient. `ThingAndResourceOwner.AddFromThing` takes only
  what that ingredient still needs (splitting the stack) and keeps a running count inside the machine; the
  batch starts when every count is full. So an input cell is not tied to an ingredient.
- **Why a shared ordinary hopper clogs.** It holds one stack. If the stack on it is something the machine
  already has enough of, nothing else can land there, and the next ingredient waits behind it on the belt.
- **Belts.** Pushing into storage, a belt counts the items on each cell against
  `GetMaxItemsAllowedInCell` (the building's `maxItemsInCell`) and also allows merging into a stack of the
  same item with room; pulling, it takes the first item stack it finds, whole.
- **Output.** A finished batch merges into the first matching stack on the output cell, and waits when that
  stack is above the bill's cap; items made with a stuff (`useFirstIngredientAsOutputStuff`, such as the
  machining bay's weapons) are placed as new things instead, so a multi-stack output cell holds several.
  **Corrected later on 29 Oct 2026:** most machining bay weapons are *not* made with a stuff (VFE sets the flag
  only when the item has a stuff cost), and for them the merge loses the new item; see the next section.
- **VFE's hopper Def** (`VFEFactory_FactoryHopper`) sets `maxItemsInCell` 1 and uses
  `Building_FactoryHopper`, which replaces the Def's graphic with three fixed textures (plain, input,
  output); only the autofarmer's placement preview names that Def directly.
- **The deep hopper** (`PhobosFactory_DeepFactoryHopper`) inherits that Def through a Name added by
  `PatchOperationAttributeAdd` (inheritance resolves after patches), overriding `maxItemsInCell` (4), cost
  and work. Because the class draws fixed textures, it looks the same as the ordinary hopper. A custom look
  would need a small C# subclass and new art: not done.

## Belts that take from any storage (0.6.0, 29 Oct 2026)

*Moved here on 17 Nov 2026 from `vfe-factory-additions.md`; kept as written.*


The owner asked for one generic way for VFE Factory's belts to pull from any container, instead of a patch per
building and mod (the corpse fridge in 0.2.0, the chemfuel taps in 0.3.2), and stressed the cost to run.

**What the code says (read 29 Oct 2026 in the decompiled VFE Factory, Pipe System 1.6 and 1.7, Adaptive Storage
Framework and Rimefeller assemblies; not tested in game).**

- `Building_Conveyor.PeriodicCacheCheck` runs about once a second per belt (`(TicksGame + thingIDNumber) % 60`)
  and sets the private `hasAdjacentHopper` when a neighbouring `Building_Storage` is VFE's `Building_FactoryHopper`
  or carries `FactoryHopperExtension`. Only then does `ProcessMovement` call `TryPullItem` on each idle tick,
  which tries `TryPullFromDir` behind, then clockwise, then counter-clockwise. That method takes the first
  spawned Item on the storage's slot cells, whole stack, and puts it on the belt. Nothing else sets the flag.
- `Building_Conveyor.ReceiveBypass(Thing)` (public) is VFE's own hand-over: it takes the item from any holder,
  adds it to the belt and sets the belt moving. `HasSpace()` is public too.
- `Process.InputFactoryHopperDetected(IntVec3)` (public, Pipe System) decides whether a hopper-only bill reads
  an input cell, by the same Def flag. Machines poll inputs every 100 ticks (`CompTickInterval`). Output uses a
  separate `FactoryHopperDetected()`, left alone.
- The flag is per Def, so no XML or Def change can make it depend on one building: a per-building switch needs
  code at those two decision points.
- Adaptive Storage's `AdaptiveStorage.ThingClass` derives from `Building_Storage` and spawns its stored items on
  the map. Rimefeller's "refinery unloading bay" (`RefineryLoadingBay`, parent `ChemfuelHopperBase`) is a plain
  `Building_Storage`, and `CompRefinery` places its products on the bay's cell with `GenPlace.TryPlaceThing`.
  Both can therefore feed belts. (The 28 Oct conveyor record said a Rimefeller bay is not `Building_Storage`;
  that was wrong, and a correction note is added there.)
- Kimoju's "VFE Factories: Buffs and tweaks" patches only the autofarmer and the fish farm, not belts.

**Survey (read-only, the owner's 300 active mods, by each ThingDef's resolved `thingClass`).** 105 Adaptive
Storage buildings (Warehouse Storage, the [sbz] mods, Cargo Holds, Adaptive Ideology Storage, [kyzy] Ship Wall
Enhanced), 30 plain `Building_Storage` (vanilla shelves and hopper, VFE furniture, Rimefeller bays, VCHE hoppers
and siphons, DMS, VGE, VNPE and others), and a scatter of single classes. The containers that are not
`Building_Storage` hold their contents in an inner container rather than on the map: graves and sarcophagi,
ancient and DMS crates, bookcases, growth vats, biosculpter pods, outfit stands, gibbet cages. Belts should not
empty those, and VFE's pull could not read them anyway.

**The three designs put to the owner.** Flag every storage type at load (no running cost, no Harmony, but any
belt beside any shelf drains it, in an existing colony too, which the project's rule against moving the
player's things forbids without consent); a switch per storage (chosen); a loader belt that pulls from
whatever is behind it (no VFE patches, but identical-looking until it has art, and no help for input cells).

**What 0.6.0 does.**

- `BeltAccessSetup.AddSwitches`, called from the existing postfix on `DefGenerator.GenerateImpliedDefs_PreResolve`
  (so the game resolves and checks the comp like XML), adds `CompProperties_BeltAccess` to every ThingDef whose
  `thingClass` is a `Building_Storage`, skipping Defs that already carry `FactoryHopperExtension`. It logs the
  count.
- `CompBeltAccess` holds one saved bool (`PhobosFactory_BeltsMayTake`, default off), shows a `Command_Toggle` on
  player-owned storage and an inspect line while on. The toggle delegates to `BeltAccess.SetBeltsMayTake`, which
  sets it and sends one silent message (no look target, so the game merges a multi-select into one).
- A postfix on `PeriodicCacheCheck` also sets `hasAdjacentHopper` when switched-on storage is behind or at
  either side (three `GetFirstThing<Building_Storage>` lookups, once a second, only while the flag is false).
  VFE recomputes the flag from scratch each time, so switching off takes effect within a second.
- A postfix on `TryPullFromDir`, only when VFE's own pull found nothing on that side: every 10 ticks
  (`IsHashIntervalTick`, VFE's own rhythm for items on a belt cell) it takes the first Item on the storage's
  cached slot cells (`AllSlotCellsList`, no allocation) with `SplitOff` and hands it over with `ReceiveBypass`.
  Hoppers keep priority, and the order of sides stays VFE's.
- A postfix on `InputFactoryHopperDetected` also accepts switched-on storage on the cell; Pipe System then reads
  every item there, as for a hopper.
- All three are postfixes. Each patch class is applied on its own, so a VFE rename switches off only that part.
  The assembly now references VFE Factory's installed `VanillaFurnitureExpandedFactory.dll` with
  `Private=false`; VFE Factory publishes no licence (its GitHub repository and Workshop folder checked
  29 Oct 2026), so nothing of it is copied ([third-party notices](../../../THIRD_PARTY_NOTICES.md)).

**Cost (estimate from the code, not measured).** Switched-off storage adds nothing while the game runs.
Belts add the neighbour check about once a second each; the storage scan runs only beside switched-on storage
and only every 10 ticks, and an idle belt beside an empty hopper already does more work than that every tick in
stock VFE. The owner runs Dubs Performance Analyzer and can measure it in play.

**Behaviour to watch in play.** A belt beside switched-on storage takes everything, forbidden items included and
whatever its filter says (the hopper rule). A belt that delivers back into storage it takes from loops for ever.
Pulling from storage runs at most once per 10 ticks per belt, a little slower than from a hopper.

**0.6.1 (30 Oct 2026): only the belt behind.** The owner's first test (29 Oct 2026, RimWorld 1.7) showed belts
taking stacks from a switched-on Adaptive Storage fridge, and also belts that merely ran along its sides: a
line wrapped around the fridge emptied it. 0.6.0 had copied the hopper rule (behind and both sides), which
suits a hopper placed to feed one belt but not storage that belts pass. Now `BeltAccess.HasStorageToTakeFrom`
looks only at the cell behind the belt (`Rotation.Opposite`; `TryPullFromDir`'s argument is the direction
from the belt to the neighbour, read in `TryPullItem`), and `TryTakeFrom` returns at once for any other side.
The neighbour check falls from three lookups to one. Factory hoppers and Def-flagged storage keep VFE's own
rule. The 0.6.0 notes also said a hopper beside the same belt goes first; that holds only per side. VFE tries
behind first, so switched-on storage behind a belt goes before a hopper at its side.

## Stuck belts and belts that obey storage filters (0.7.0, 30 Oct 2026)

*Moved here on 17 Nov 2026 from `vfe-factory-additions.md`; kept as written.*


The owner reported that the human-meat neutroamine line had stopped: "the hopper and conveyor belts don't seem
to be working". Diagnosed from a read-only copy of the owner's PG6 save (RimWorld 1.7) and VFE Factory's
decompiled assembly; the owner asked for both fixes ("apply whatever fixes are necessary").

**What the save showed.** Both synthesizers were starved of meat (0 and 11 of 20 loaded; they had run the
human-meat bill 183 and 85 times, so the line had worked). The mincer had left 75 human meat in its output
hopper, and the storage group filter allowed it. The two belts that carry it on were **empty but in the Waiting
state**. Separately, the belt in front of the top mincer's corpse hopper held a steel slag chunk the corpse-only
hopper refused, and every corpse belt behind it was full and waiting. The chunk's last recorded cell was on the
corpse fridge. Across the map, 9 of 579 belts were empty and Waiting (the other 68 waiting belts held items:
ordinary queues).

**What the code says** (VFE Factory, read 30 Oct 2026; not tested in game):

- `Building_Conveyor.Tick` returns at once while `innerContainer` is empty and `state` is `Waiting`, and
  `ProcessMovement` pulls from a hopper only while the state is not `Waiting`. Only an item pushed in by
  another belt (`ReceiveBypass`, a belt-to-belt transfer) changes the state. The state is saved, so a reload
  keeps it, and `TrySetDirection` ("Change direction") doesn't touch it. In game the only fix was to rebuild
  the belt.
- Every place VFE Factory sets `Waiting` runs only while the belt holds something, and every place it empties a
  belt resets it (`ResetConveyorState`, or `Empty` in the "Haul from conveyor" job). **What empties these belts
  is not known.** Ruled out by reading: rotting (a belt doesn't tick its contents), weather damage (resets),
  machines' input cells (none on these belts), the haul job, and our 0.6 storage pull (the game's
  `Thing.SplitOff` of a whole stack despawns it, so nothing is held twice). Neither log had errors from belts.
- `TryPullFromDir` takes the first item on a hopper-flagged storage's cells, whole stack, with no filter check,
  so anything lying on the corpse fridge rides out with the corpses.

**What 0.7.0 does.**

- **Stuck belts wake.** A prefix on `Building_Conveyor.Tick` (`ConveyorTickPatch`, `StuckBelts.WakeIfStuck`)
  calls VFE Factory's own private `ResetConveyorState` when `BeltRule.IsStuck` holds, so the same tick runs
  normally and the belt pulls again. Underground entrances and exits tick through the same method. The cost is
  two field reads per belt per tick. `Prepare` looks the reset up once; if an update renames it, the patch stays
  off and the log says so. The rule has unit tests.
- **Each wake is logged** with the belt's position, up to 10 a session (agent default), so play can show what
  empties belts without flooding the log.
- **Belts obey the corpse fridge's filter.** A new marker, `Phobos.Factory.BeltFilterExtension`, on a
  hopper-flagged storage Def makes belts take only stacks its storage filter allows (`StorageSettings.
  AllowedToAccept`, the group's settings when the building is in a storage group; a pure filter check, no room
  check). A prefix on `TryPullFromDir` handles such storage and skips the original for it alone, because VFE
  chooses the item inside that method. The corpse fridge patch adds the marker. Rejected stacks stay; the
  storage doesn't accept them, so haulers move them on.
- **Switched-on storage obeys its filter too** (both 0.7.0 rules through one `BeltAccess.TakeFirstAllowed`).

**Agent choices while scoping (owner to revise).**

- **VFE Factory's own hoppers and taps keep VFE's rule**: belts take whatever is on them. A machine places its
  output on its hopper whatever the hopper's filter says, and the owner's chemfuel tap is saved with a filter
  that allows nothing yet still makes chemfuel, so obeying those filters would quietly stop working lines.
- **Switched-on storage obeys its filter.** Checked in the PG6 save first: the one switched-on building (a
  thermostatic fridge cabin holding psychoid leaves) allows what it holds, and all 25 Rimefeller unloading bays
  allow FSX. Gotcha for other players: Rimefeller adds FSX to the bay's list of allowed items under Combat
  Extended but not to a new bay's default filter, so a new bay may need FSX ticked before belts take it.

**Version.** 0.7.0: a fix (stuck belts) with new behaviour and a new Def extension for modders (the filter rule),
so the middle number moves.

## A belt switch on factory hoppers (0.11.0, 2 Nov 2026)

*Moved here on 17 Nov 2026 from `vfe-factory-additions.md`; kept as written.*


The owner asked for an option on VFE Factory's hoppers that stops conveyor belts taking from them, if the player chooses.

**What the code says** (read 2 Nov 2026; not tested in game):

- `Building_Conveyor.TryPullFromDir` is the only place a belt takes from storage. It reads `GetCachedNeighborBuilding(dir)`,
  and returns false unless that is a `Building_FactoryHopper` or carries `FactoryHopperExtension` with `isfactoryHopper`. **The
  decompiled C# shows this check as an empty block**; the IL shows `brfalse` to the method's `return false`. Read the IL when a
  decompiled branch looks empty.
- `PeriodicCacheCheck` sets `hasAdjacentHopper`, once a second and for all sides together, so it can't express "not this hopper".
- Delivering into a hopper is separate code, and machines read input cells through Pipe System, so neither passes through this.

**What 0.11.0 does.** `CompBeltAccess` gains `Props.startsOn`, true where `BeltAccess.IsHopperDef` says VFE Factory feeds belts by
itself, and `BeltAccessSetup` now gives every storage type the switch. `BeltRule` states the meaning: on ordinary storage, on adds
it to what belts take from; on a hopper, off removes it. The `TryPullFromDir` prefix refuses for a hopper switched off. It now
reads VFE Factory's own cached neighbour, the same building the original would read, in place of the 0.7.0 thing-grid lookup and
its "any filtered Def loaded" shortcut. A belt beside a switched-off hopper keeps asking every tick while it has room, as it already
does beside an empty hopper.

**Agent choices (owner to revise).**

- **Belts only.** Machines and colonists still take from a switched-off hopper, because the owner asked about belts, and
  Allow taking already covers colonists.
- **The same switch and label as other storage**, rather than a second button with an opposite meaning. On means belts may take,
  everywhere.
- **Flagged storage too** (chemfuel taps, the corpse fridge): one rule for everything that feeds belts, and it answers the corpse
  fridge's "any belt touching it empties it".
- **Each hopper has its own switch**, not shared across a linked group as VFE Factory's Allow taking is. A linked group shares
  storage settings, but which hopper a belt may take from is about where the hopper stands. Multi-select sets several.
- **Inspect text speaks only when the switch is off** on a hopper, so ordinary hoppers stay uncluttered.

## Bills that outlive their recipe (7 Nov 2026)

The owner reported an automated machining bay reading only the first stack in a deep factory hopper (components, with FSX behind them
that the bill said it lacked). Read from Pipe System 1.7 and the owner's Autosave-12 of that evening:

- **The scan reads every stack.** `Process.CheckInputSlots` walks each input cell's full thing list (and a container's inner items)
  and offers every item to every ingredient; the save showed FSX 20/20 counted from the second stack of the hopper in the screenshot.
- **A bill's ingredient owners are built once** (`Process.Setup`, when the bill is added), saved, and never compared with the recipe
  again. After a recipe change the owners stay old while the scan matches cell items against the new recipe; an item the old owners
  cannot take reaches `AdvancedProcessorsManager.AddIngredient` with a null owner and throws, ending the scan before the items behind
  it. The owner's bays had run two recipes (heavy castings, then bomb cases: both are in their cached ingredient lists).
- **Machines draw only from their input cells** (the machining bay's three, at offsets (0,3), (-3,0), (3,0) rotated with it), and a
  hopper-fed bill is never put in the list pawns deliver to (`SetAwaitingIngredients` is skipped when `autoGrabFromHoppers` and input
  slots are set). In the save, one bay waited on 2 components with three empty input hoppers (29 components sat in the next bay's
  hopper), and the other on a bomb case with its bomb-case owner stuck as "being filled": the scan marks the owner it was called for
  whenever it takes any item, and unmarks only the item's own owner.
- **Fixes in D'thorga Factory Additions 0.11.4:** stale bills rebuilt from the recipe on their first tick after a load (unstarted) or
  on their next reset (running), counted ingredients set down beside the machine, target count kept, a player message; a null-owner
  guard that skips the item instead of throwing; the stuck mark cleared after each scan. The README now states the input-cell and
  priority rules. Not yet tested in game.

## Sources

- VFE Factory (Oskar Potocki, Sarg Bjornson, Taranchuk), Workshop item 3686924415, assembly read 24 to
  28 Oct 2026: `Building_Conveyor`, `Building_FactoryHopper`, the Harmony patches named above.
- Vanilla Expanded Framework, Pipe System (Workshop item 2023507013): `FactoryHopperExtension`,
  `Process`, `PipeSystem_Thing_PostMapInit_Patch`.
- Adaptive Storage Framework (bradson and others, Workshop item 3033901359, MIT): `Source/AdaptiveStorage/`
  `ThingClass.cs` and `HarmonyPatches/DeregisteredAtThingGridEvent.cs`.
- "[sbz] Fridge" (seobongzu, Workshop item 3486264784): `Defs/ThingDefs_Buildings/Buildings_Fridges.xml`.
