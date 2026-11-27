# D'thorga Farming for modders

How your own mod's crops can be harvested by D'thorga Farming's machines. Players start at [the mod's
README](../../mods/PhobosFarming/README.md) and [the player guide](../player-guide/farming.md). How farming works, with every type
and field, is in [the farming system page](../development/systems/farming.md). **Not yet tested in game.**

## Putting a crop in a class

A front takes a crop through its **crop class**, never its name, so another mod's crop is harvested once it is placed in a class.
To let the grain fronts take your crop, add one patch:

```xml
<Operation Class="PatchOperationConditional">
  <xpath>Defs/ThingDef[defName="YourMod_Plant_Millet"]</xpath>
  <match Class="PatchOperationAddModExtension">
    <xpath>Defs/ThingDef[defName="YourMod_Plant_Millet"]</xpath>
    <value>
      <li Class="Phobos.Farming.PhobosCropExtension">
        <cropClass>PhobosCrop_Cereal</cropClass>
      </li>
    </value>
  </match>
</Operation>
```

| Class | Taken by |
| --- | --- |
| `PhobosCrop_Cereal`, `PhobosCrop_Pulse`, `PhobosCrop_Oilseed` | The grain fronts |
| `PhobosCrop_RowCrop` | The corn header |
| `PhobosCrop_Forage`, `PhobosCrop_Fibre`, `PhobosCrop_Root` | Harvesters still to come; hands for now |

Our own patches are in `Common/Patches/Crops.xml`. A combine cuts as a colonist would, so other mods that act when a plant is
harvested still see the harvest.

## A crop class of your own

`Phobos.Farming.PhobosCropClassDef` is a class of crop by how it is harvested. A harvesting front takes a crop through its class, never
its name. Our seven classes are in the table under [Putting a crop in a class](#putting-a-crop-in-a-class).

**Gotchas first:**

- **A class no front takes is harvested by no machine.** Fronts list the classes they take; a new class needs a front of your own
  (see [A combine of your own](#a-combine-of-your-own)) or a patch adding it to one of ours.
- **Prefer one of our classes** when your crop is harvested like one of them: our fronts then take it with one patch.
- **`harvestIsSeed` decides what a drill sows the crop with.** True: the crop's own harvest, cleaned, or bought seed. False: bought
  seed only. With the setting that makes hands use seed on, it also decides whether colonists fetch seed to sow it by hand.
- **`leavesStraw` is read only with Dead Man's Switch - Legion's Agricultural Frame** (and our setting for its hay, on by default):
  only crops whose class leaves straw give the frame's hay.

From `mods/PhobosFarming/Common/Defs/Phobos/Farming/CropClasses.xml`:

```xml
<Phobos.Farming.PhobosCropClassDef>
  <defName>PhobosCrop_Cereal</defName>
  <harvestIsSeed>true</harvestIsSeed>
  <leavesStraw>true</leavesStraw>
  <label>cereal</label>
  <description>Small grains cut whole and threshed: rice, wheat, barley, oats, rye, sorghum, buckwheat. A combine's grain front takes them.</description>
</Phobos.Farming.PhobosCropClassDef>
```

To let one of our fronts take a new class, add it to that front's list. A sketch, not shipped XML:

```xml
<Operation Class="PatchOperationAdd">
  <xpath>Defs/Vehicles.VehicleDef[defName="Phobos_Combine"]/comps/li[@Class="Phobos.Farming.CompProperties_PhobosHarvester"]/fronts/li[node="PhobosCombineFrontStandard"]/takes</xpath>
  <value>
    <li>YourMod_CropClass_Buckwheat</li>
  </value>
</Operation>
```

`PhobosCropClassDef`:

| Field | What it does (default) |
| --- | --- |
| `harvestIsSeed` | The crop's harvest is its own seed (grain, pulses, oilseeds, corn kernels), so it can be kept back and sown again. False where it is not (hay, cotton lint), which then sows only from bought seed (false). |
| `leavesStraw` | Cutting the crop leaves straw or forage worth baling as feed (cereal and pulse straw, corn stover, grass), which the game's hay stands for. False for root crops and cotton (false). |

Which crop goes in which class, and both flags on our classes, are the agent's reading of real farming, held for the owner's review.

## A combine of your own

`Phobos.Farming.CompProperties_PhobosHarvester` makes a vehicle a combine: on a field run it cuts the ripe plants its strip covers, by
the game's own harvest steps with the driver standing in for a colonist, and pours the grain into its own tank. The front, fitted by
upgrade, decides how wide it cuts and which crop classes it takes.

**Gotchas first:**

- **It goes on a `Vehicles.VehicleDef`, with two more comps:** D'thorga Framework's `Phobos.VehicleSystems.CompProperties_PhobosFieldRun`
  (which drives) and `Phobos.VehicleSystems.CompProperties_PhobosBulkHold` (the grain tank). Without a hold it refuses every paddock
  and says it has no tank.
- **Fronts are fitted by upgrade.** Each front names a node key in the vehicle's Vehicle Framework upgrade tree
  (`Vehicles.CompProperties_UpgradeTree`). With no front fitted it refuses to harvest; if several are fitted, the first in the list
  counts.
- **It works only paddocks**: growing zones the player has marked **Machine-worked**.
- **It never cuts an unripe plant, nor a crop class its front does not take.** A tank that keeps to one product and still holds
  another refuses the paddock, with the reason.
- **Straw and other second products are left lying in the field**, as a combine leaves them, unless the straw chopper node is fitted.
- Other mods that act when a plant is harvested still see it: the plant's own harvest hook runs, with the vehicle as the harvester.

From `mods/PhobosFarming/Common/Defs/Phobos/Farming/Vehicle_Combine.xml`, trimmed (two of its three fronts; the tank is in
[Field runs and bulk holds](../vehicle-features-modding.md#field-runs-and-bulk-holds)):

```xml
<comps>
  <li Class="Vehicles.CompProperties_UpgradeTree">
    <def>Phobos_Combine_UpgradeTree</def>
  </li>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosFieldRun" />
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosBulkHold">
    <capacityKg>9000</capacityKg>
    <mixesProducts>false</mixesProducts>
  </li>
  <li Class="Phobos.Farming.CompProperties_PhobosHarvester">
    <strawChopperNode>PhobosCombineStrawChopper</strawChopperNode>
    <fronts>
      <li>
        <node>PhobosCombineFrontStandard</node>
        <label>standard grain front</label>
        <width>6</width>
        <takes>
          <li>PhobosCrop_Cereal</li>
          <li>PhobosCrop_Pulse</li>
          <li>PhobosCrop_Oilseed</li>
        </takes>
      </li>
      <li>
        <node>PhobosCombineFrontCorn</node>
        <label>corn header</label>
        <width>6</width>
        <takes>
          <li>PhobosCrop_RowCrop</li>
        </takes>
      </li>
    </fronts>
  </li>
</comps>
```

The front's picture is drawn by D'thorga Framework's node looks (`Phobos.VehicleSystems.CompProperties_PhobosNodeLooks`), one look
for each front's node, as the same file shows.

`CompProperties_PhobosHarvester`:

| Field | What it does (default) |
| --- | --- |
| `fronts` | The fronts it can be fitted with, one `li` each (empty: it can never harvest). |
| `driverExperienceShare` | The share of a hand harvester's Plants experience the driver earns for each plant cut (0.25, authored). |
| `strawChopperNode` | The upgrade node that chops straw into the field instead of leaving it lying (empty: never). |
| `orderIconPath` | The harvest order button's picture (`Phobos/Farming/UI/Harvest`). |

Each front:

| Field | What it does (default) |
| --- | --- |
| `node` | The upgrade node that fits it (empty). |
| `width` | How many cells wide its strip is (6). Ours: 6 and 8 for the grain fronts, the owner's figures. |
| `takes` | The crop classes it takes (empty: none). |
| `label` | Its name in the player's words, for messages (empty). |

## Implements behind a tractor

A plough, a cultivator or a seed drill is a vehicle of its own, held behind a tractor through D'thorga Framework's hitch and moved by
it. On a field run the tractor drives and the implement works the strip behind it. Its comp is one of two:
`Phobos.Farming.CompProperties_PhobosTillage` (a plough or cultivator) or `Phobos.Farming.CompProperties_PhobosSeedDrill` (a drill).
Both share the fields of `CompProperties_PhobosImplement`.

**Gotchas first:**

- **Never write `Phobos.Farming.CompProperties_PhobosImplement` in XML.** It is abstract: write its fields on the tillage or drill comp.
- **The implement is a `Vehicles.VehicleDef`**, towed through our hitch: it needs `Phobos.VehicleSystems.CompProperties_PhobosTrailer`
  and a `Phobos.VehicleSystems.PhobosCouplingsExtension` naming the coupling it is towed by. See [Hitches, couplings and
  trailers](../vehicle-features-modding.md#hitches-couplings-and-trailers). Unhitched, it does nothing.
- **The tractor needs `Phobos.VehicleSystems.CompProperties_PhobosFieldRun`** for the field order; the implement is the worker.
- **It works only paddocks on a run.** Its **Lower** button lets it work while the tractor drives anywhere: tillage works any cell it
  passes, a drill only a paddock it could sow.
- **No implement ever destroys a sown crop**, of any kind, nor a tree.
- **It pulls only while lowered.** Its draught is `widthCells` x `metresPerCell` x `kilonewtonsPerMetre`, and counts as load on the
  tractor's engine.
- **`wingWidthCells` replaces `widthCells`** once the `wingNode` upgrade is fitted; it does not add to it. The draught grows with it.

### Fields every implement shares

`CompProperties_PhobosImplement`, written on either comp:

| Field | What it does (default) |
| --- | --- |
| `widthCells` | How many cells wide it works (3, authored). |
| `kilonewtonsPerMetre` | Its draught per metre of working width, in kN (4.4: Iowa State University's tables for medium soil; approximate). |
| `metresPerCell` | The metres a cell stands for, for draught (1.5, the agent's scale). |
| `orderIconPath` | The order button's picture (`Phobos/Farming/UI/Harvest`). |
| `wingNode` | The upgrade node that unfolds its wings (none). |
| `wingWidthCells` | Its width with the wings unfolded (0: the wings change nothing). |

### A plough or cultivator

`CompProperties_PhobosTillage` clears weeds, stubble and wild plants from the cells it passes, never a sown crop and never a tree,
and marks the paddock ploughed or cultivated once a run has worked most of it. A cell holding a sown crop is passed over whole. The
mark shows in the run's end message and is kept for soil rules still to come; it does nothing else yet.

From `mods/PhobosFarming/Common/Defs/Phobos/Farming/Vehicle_Implements.xml` (the Sillion turnover plough), trimmed; its width,
draught and weight are authored [A], approximate:

```xml
<Vehicles.VehicleDef ParentName="PhobosFarming_MountedImplementBase">
  <defName>Phobos_PloughSillion</defName>
  <!-- ... -->
  <comps>
    <li Class="Phobos.Farming.CompProperties_PhobosTillage">
      <widthCells>1</widthCells>
      <kilonewtonsPerMetre>7.3</kilonewtonsPerMetre>
      <metresPerCell>1.5</metresPerCell>
      <orderIconPath>Phobos/Farming/Plough/Plough_icon</orderIconPath>
      <mark>ploughed</mark>
      <turns>true</turns>
      <wingNode>PhobosPloughWings</wingNode>
      <wingWidthCells>2</wingWidthCells>
    </li>
    <li Class="Vehicles.CompProperties_UpgradeTree">
      <def>Phobos_Plough_UpgradeTree</def>
    </li>
  </comps>
</Vehicles.VehicleDef>
```

`CompProperties_PhobosTillage` (plus the shared fields above):

| Field | What it does (default) |
| --- | --- |
| `mark` | `ploughed` or `cultivated`: what the paddock is marked as, and which order and messages it shows (`ploughed`). |
| `markShare` | The share of a paddock's cells a run must work for the paddock to be marked (0.9, authored). |
| `turns` | A reversible (turnover) plough: it rolls over at each row's end, so every furrow is thrown the same way (false). |

A plough that `turns` shows its sides through D'thorga Framework's node looks. It answers the look names `turned` (the other side
is down), `wings` (wings fitted, first side down) and `wings-turned` (wings fitted, other side down). The Sillion's looks in the same
file show how.

### A seed drill

`CompProperties_PhobosSeedDrill` sows the paddock's crop in the empty cells it passes, by the game's own checks for a hand (season,
light, roof, neighbours, the ground). It never cuts anything. It draws seed from its own hopper: the crop's own grain, cleaned as it
goes, or the bought seed named in `genericSeed`.

**Gotchas first:**

- **It needs its own `Phobos.VehicleSystems.CompProperties_PhobosBulkHold`** on the same vehicle: that is its hopper. With no seed in
  it, it refuses the paddock; short of seed partway, the run stops at that row's end and says so.
- **Own grain only for a crop whose class has `harvestIsSeed`.** Any other crop sows only from `genericSeed`.
- **Your `genericSeed` item needs `Phobos.Framework.Bulk.PhobosBulkGoodsExtension`** on its Def, or an auger will not take it in
  from the ground. Our `PhobosFarming_Seed` (`Items_Seed.xml`) carries it.
- Before a run it refuses a crop whose sowing research is unfinished, a crop out of season where the paddock is, and a crop above
  its driver's Plants skill.

From `Vehicle_Implements.xml` (the Coulter drill), trimmed; the hopper's figures are authored [A]:

```xml
<comps>
  <li Class="Phobos.VehicleSystems.CompProperties_PhobosBulkHold">
    <capacityKg>1500</capacityKg>
    <mixesProducts>false</mixesProducts>
    <pourKgPerSecond>40</pourKgPerSecond>
    <reachCells>2</reachCells>
  </li>
  <li Class="Phobos.Farming.CompProperties_PhobosSeedDrill">
    <widthCells>4</widthCells>
    <kilonewtonsPerMetre>1.0</kilonewtonsPerMetre>
    <metresPerCell>1.5</metresPerCell>
    <orderIconPath>Phobos/Farming/SeedDrill/SeedDrill_icon</orderIconPath>
    <wingNode>PhobosDrillWings</wingNode>
    <wingWidthCells>7</wingWidthCells>
    <genericSeed>PhobosFarming_Seed</genericSeed>
  </li>
</comps>
```

`CompProperties_PhobosSeedDrill` (plus the shared fields above):

| Field | What it does (default) |
| --- | --- |
| `seedShare` | The share of a cell's harvest, by weight, a cell takes as seed (0.03, about 3 %, from the Birchip Cropping Group and Teagasc figures; authored). |
| `cleaningLoss` | What cleaning takes off the crop's own grain used as seed: it sows at its weight divided by what is left (0.1, authored). Bought seed has none. |
| `genericSeed` | The bought seed that sows any crop (none: own grain only). |

Every cell takes at least 5 grams of seed, whatever the crop yields (a code constant, authored). Our implements are described in
full, with their figures, in [the farming system page](../development/systems/farming.md#implements).

---

## Checks done

Each version's checks (builds, unit tests, readings of the game's and Vehicle Framework's code) are in [the
changelog](../../mods/PhobosFarming/CHANGELOG.md). **Nothing is tested in game yet.**
