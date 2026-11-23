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

## Checks done

Each version's checks (builds, unit tests, readings of the game's and Vehicle Framework's code) are in [the
changelog](../../mods/PhobosFarming/CHANGELOG.md). **Nothing is tested in game yet.**
