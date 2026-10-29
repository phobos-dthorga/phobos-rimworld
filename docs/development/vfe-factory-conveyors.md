# VFE Factory conveyors and hoppers: how belts pull and push (code read, 28 Oct 2026)

What decides where a Vanilla Furniture Expanded - Factory belt takes items from and puts them, and how to
let it take from another mod's storage. Read from the decompiled VFE Factory and Pipe System (Vanilla
Expanded Framework) assemblies in the ignored `.local/decompiled/` on 24, 26 and 28 Oct 2026, and from
Adaptive Storage Framework's published source (MIT, shipped in its Workshop folder). **None of it is tested
in game** unless a line says the owner saw it. Dated record; the mod README carries current behaviour.

## The one rule that matters: belts pull only from "factory hoppers"

**Since D'thorga Factory Additions 0.6.0 (29 Oct 2026)** any storage building with its "Belts may take from
here" switch on counts as well, and since 0.6.1 (30 Oct 2026) only for a belt that has it directly behind, not
at its sides; how that is done is in the
[factory additions record](vfe-factory-additions.md#belts-that-take-from-any-storage-060-29-oct-2026). The rule
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
- every belt touching it on the belt's back or sides will drain it, filter or not, so warn the player;
- a machine whose input cell lands on it will grab from it, which may be the point.

## Sources

- VFE Factory (Oskar Potocki, Sarg Bjornson, Taranchuk), Workshop item 3686924415, assembly read 24 to
  28 Oct 2026: `Building_Conveyor`, `Building_FactoryHopper`, the Harmony patches named above.
- Vanilla Expanded Framework, Pipe System (Workshop item 2023507013): `FactoryHopperExtension`,
  `Process`, `PipeSystem_Thing_PostMapInit_Patch`.
- Adaptive Storage Framework (bradson and others, Workshop item 3033901359, MIT): `Source/AdaptiveStorage/`
  `ThingClass.cs` and `HarmonyPatches/DeregisteredAtThingGridEvent.cs`.
- "[sbz] Fridge" (seobongzu, Workshop item 3486264784): `Defs/ThingDefs_Buildings/Buildings_Fridges.xml`.
