# D'thorga Farming

Machines that work your fields: mark a growing zone as a paddock, and a combine harvester cuts it row by row by itself.

One of House D'thorga's mods (also spelt dthorga).

Version 0.6.0, built 20 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Needs Vehicle Framework and D'thorga Framework** (0.189.0 or later). Load it after both; the About file asks for this, so RimSort
  does it for you.
- **A combine has no front until you fit one.** Research **mechanised harvesting**, build the **Reaper combine harvester** (Architect >
  Motor pool), then fit the **standard grain front** from its upgrades. Without a front it refuses to harvest and says why.
- **Mark the field first.** Select a growing zone and press **Machine-worked**. Colonists then keep off it. **There is no seed drill
  yet**: to sow a paddock, turn **Hands may help** on, and off again once it is sown.
- **Each front cuts only the crops it really could.** The grain fronts take cereals, pulses and oilseeds: rice, and with Vanilla Cooking
  Expanded or Vanilla Plants Expanded their wheat, barley, oats, rye, sorghum, buckwheat, chickpeas, peas and canola. **The corn header
  takes corn and sunflowers.** **Potatoes, hay and cotton need harvesters that come later**, and stay for hands. Another mod's crop is
  taken once it is placed in a class: see [For modders](#for-modders).
- **A full combine needs somewhere to empty**: a **Garner chaser bin** parked near the paddock (any vehicle with a hitch tows it), a
  **field bin**, or a **grain silo**. The silo keeps grain four times as long, **but only while its fan has power**.
- **Settings to decide before you start:** Options > Mod settings > D'thorga Farming. **Hands may help on new paddocks** (off),
  **A machine's driver can fail a harvest, as by hand** (on), and **Hands use seed too** (off: on, colonists sowing grain, peas,
  oilseeds or corn by hand take a handful of it from a stockpile first, and will not sow without it).
- **Safe to add to a running colony.** **Removing it:** paddocks become plain growing zones; combines, chaser bins and the grain in
  them vanish with the mod. Empty them first.
- **Not yet tested in game.** Everything here is built and checked offline, and read from the game's and the mods' code.

## What it does

1. **Mark a paddock.** Machine-worked on any growing zone. Hands may help lets colonists sow and harvest it too.
2. **Fit a front and give the combine a driver.** Press **Harvest a paddock** and pick the paddock. With nobody aboard, it calls a
   colonist with Drive work to take the wheel.
3. **It works the rows by itself.** It lays the paddock out in rows along its longer side, starting from the corner nearest it, drives each
   row and cuts every ripe plant its front covers. Unripe plants are left standing.
4. **It cuts as a colonist would.** The driver's harvesting skill sets the yield and the chance of a failed cut. Other mods that act when a
   plant is harvested still see the harvest. Straw and other second products are left lying where each row ends, unless a straw chopper
   is fitted.
5. **When its tank is full** it drives to the nearest chaser bin, field bin or silo with room, pours the grain in through its auger,
   and goes back to the rows. With nowhere to empty, it stops and says so.
6. **Grain is held by weight, as one amount**, not in stacks. Pour it from a chaser bin onto the ground, where colonists haul it as
   usual, or into another bin. It goes off as it would in the open.

| Machine | What it is | Holds |
| --- | --- | --- |
| Reaper combine harvester | Self-propelled; 4 by 9 cells; a 6- or 8-cell grain front, or a 6-cell corn header | 9 tonnes of one crop at a time (12 with the tank extension) |
| Garner chaser bin | Towed; 3 by 7 cells | 12 tonnes, any crops |
| Field bin | A building, 3 by 3 cells | 25 tonnes, any crops |
| Grain silo | A building, 4 by 4 cells, 300 W | 60 tonnes, any crops; keeps four times as long while powered |

Every figure is authored, to tune in play.

## For modders

A crop is taken by a front through its **crop class**, never its name. To let the grain fronts take your crop, add one patch:

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

The classes are `PhobosCrop_Cereal`, `PhobosCrop_Pulse` and `PhobosCrop_Oilseed` (the grain fronts take these), `PhobosCrop_RowCrop`
(the corn header), and `PhobosCrop_Forage`, `PhobosCrop_Fibre` and `PhobosCrop_Root` for the harvesters to come. Our own patches are in
`Common/Patches/Crops.xml`.

## Credits and licence

The machines' pictures are AI-generated (OpenAI gpt-image-2) and picked by the owner; each has a record beside it. Code under the
repository's licence. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon
Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
