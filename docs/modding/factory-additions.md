# D'thorga Factory Additions for modders

How your own mod can add factory bills, automate a bench, feed belts from storage and give bills piped water, with D'thorga
Factory Additions. Players start at [the mod's README](../../mods/PhobosFactoryAdditions/README.md) and [the player
guide](../player-guide/factory.md). The reasons behind each choice, and how bills, hoppers and the output step work in Pipe
System's code, are in [VFE Factory additions: research and decisions](../development/archive/vfe-factory-additions.md); the belt
code is in [VFE Factory conveyors](../development/archive/vfe-factory-conveyors.md).

## Bills, storage, benches and water

Every bill is a Pipe System `ProcessDef` (Vanilla Expanded Framework). The human-meat bill, defName
`PhobosFactory_SynthesizeNeutroamineFromHumanMeat`, is in `Common/Defs/ProcessDefs/`; every tie-in bill is in
`Mods/<SourceMod>/Defs/ProcessDefs/`, beside the patch that adds it to its machine, and that folder loads only while the source
mod is active (`LoadFolders.xml`). Change a yield by patching the result count:

```xml
<Operation Class="PatchOperationReplace">
  <xpath>Defs/PipeSystem.ProcessDef[defName="PhobosFactory_SynthesizeNeutroamineFromHumanMeat"]/results/li[thing="Neutroamine"]/count</xpath>
  <value><count>5</count></value>
</Operation>
```

`Common/Patches/NeutroamineSynthesizer.xml` adds the human-meat bill to the synthesizer's process list and puts `Meat_Human` on
the stock bill's `disallowedThingDefs`. Remove that second operation to let the stock bill take human meat again.

Rules a new bill follows here, from reading Pipe System's code (28 October 2026): one result per bill (only the first is checked
against the output hopper's filter); `maxOutputCount` set, and at most the result's stack limit minus the batch count (in Pipe
System alone, above that the surplus of a finished batch is destroyed, and at zero a blocked output is dropped beside the machine;
this mod's output guard makes the machine wait instead of losing anything, but a limit within the stack still keeps the machine
from trying); `researchPrerequisites` hides the bill from the Add menu until done; any ingredient may come from any input slot,
but a hopper cell holds one stack. The offline checker `scripts/check-factory-patches.py` enforces the first three against the
installed game.

**Give every file in a `Mods/<SourceMod>/` folder that folder's name** (`MedicineGranulator_MedicinesPlus.xml`, not
`MedicineGranulator.xml`). RimWorld keys a mod's Defs and Patches files by their path inside each load folder and loads only the
copy in the folder listed last; the others are skipped without a message. That once hid most of this mod's tie-in bills.
`scripts/check-mod-layout.py` refuses a shared path.

Every `Building_Storage` type gets the player's **Belts may take from here** switch at load
(`Phobos.Factory.CompProperties_BeltAccess`, added in code; the saved key is `PhobosFactory_BeltsMayTake`). To make a storage
building feed belts and machines **by default**, add Pipe System's extension to its Def instead. Its switch then starts on, as a
factory hopper's does, and the player can turn belts off:

```xml
<Operation Class="PatchOperationAddModExtension">
  <xpath>Defs/ThingDef[defName="YourStorageDef"]</xpath>
  <value>
    <li Class="PipeSystem.FactoryHopperExtension">
      <isfactoryHopper>true</isfactoryHopper>
    </li>
    <!-- Optional: belts take only what this storage's filter allows. -->
    <li Class="Phobos.Factory.BeltFilterExtension" />
  </value>
</Operation>
```

It works only for storage whose items stay spawned on its cells (vanilla shelves, Adaptive Storage buildings); the belt reads the
map's thing grid. Without `BeltFilterExtension` a belt takes whatever lies there, VFE Factory's own rule for hoppers. Add the
`BeltFilterExtension` line only from a folder that loads while this mod is active
(`IfModActive="PhobosDthorga.FactoryAdditions"`); without this mod the game doesn't know the class. The full reading is in [VFE
Factory conveyors](../development/archive/vfe-factory-conveyors.md).

**Automating another bench.** Add a `Phobos.Factory.BenchBillTemplateDef` naming the bench and a factory machine; the assembly
turns every item that bench makes into a bill on that machine at load. The fabrication bay's templates are in
`Common/Defs/BenchBillTemplateDefs/` and `Mods/Rimatomics/Defs/`:

```xml
<Phobos.Factory.BenchBillTemplateDef>
  <defName>YourMod_SmithyBills</defName>          <!-- part of every bill's defName; saved, never rename -->
  <machine>PhobosFactory_AutomatedFabricationBay</machine>
  <sourceBenches><li>ElectricSmithy</li><li>FueledSmithy</li></sourceBenches>
  <researchPrerequisites><li>Smithing</li></researchPrerequisites>
  <billLabel>Make {0}</billLabel>                  <!-- {0}: the item's label -->
  <billDescription>Make {0} as a smithy would.</billDescription>
  <firstPriorityInBillList>400</firstPriorityInBillList>
  <autoInputSlots><li>(0,0,3)</li><li>(-3,0,0)</li><li>(3,0,0)</li></autoInputSlots>
  <sustainerDef>VFEFactory_AutomatedMachiningSustainer</sustainerDef>
  <effecterDef>VFEFactory_Default</effecterDef>
</Phobos.Factory.BenchBillTemplateDef>
```

Optional fields: `excludedThings` (items to leave at the bench), `workMultiplier` (default 4), `maxOutputCount` (default 75; never
above the item's stack limit), `spawnOnInteractionCell` (default true).

**Piped water on your own bills.** While Dubs Bad Hygiene is active, a factory bill uses water if its ProcessDef has this
extension and its machine has the comp. Add both from a patch guarded with `IfModActive` or `PatchOperationFindMod`, because the
classes exist only while Dubs Bad Hygiene is active. Both poison chances are optional and run from 0 to 1. An item without the
game's `CompProperties_FoodPoisonable` ignores them.

```xml
<li Class="Phobos.Factory.Hygiene.WaterUseExtension">
  <litresPerBatch>20</litresPerBatch>
  <poisonChanceUntreated>0</poisonChanceUntreated>
  <poisonChanceContaminated>0.25</poisonChanceContaminated>
</li>
```

```xml
<li>
  <compClass>Phobos.Factory.Hygiene.CompFactoryWater</compClass>
</li>
```

## Checks done

Each version was read in VFE Factory's, Pipe System's and the game's code, built with no warnings where it has C#, unit-tested
where it has rules, and checked against the owner's installation with `scripts/check-factory-patches.py`. Each version's checks
are in the [changelog](../../mods/PhobosFactoryAdditions/CHANGELOG.md), and the dated list the README kept until 17 November 2026
is in [the design record](../development/archive/vfe-factory-additions.md#checks-moved-from-the-readme-17-nov-2026). What the
owner has passed in game is listed at the end of [the guide's Factory chapter](../player-guide/factory.md#tested-in-game). **Not
yet tested in game:** everything else, starting with each new bill run to the end, the corner input cells, a belt taking from a
chemfuel tap or the corpse fridge, and removal.
