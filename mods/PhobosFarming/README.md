# D'thorga Farming

Machines that work your fields: mark a growing zone as a paddock, and tractors plough, cultivate and sow it, and a combine harvester
cuts it, row by row by themselves.

One of House D'thorga's mods (also spelt dthorga).

Version 0.7.4, built 21 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Needs Vehicle Framework and D'thorga Framework** (0.202.0 or later). Load it after both; the About file asks for this, so RimSort
  does it for you.
- **A combine has no front until you fit one.** Research **mechanised harvesting**, build the **Reaper combine harvester** (Architect >
  D'thorga > Farming), then fit the **standard grain front** from its upgrades. Without a front it refuses to harvest and says why.
- **Mark the field first.** Select a growing zone and press **Machine-worked**. Colonists then keep off it, unless **Hands may help** is
  on.
- **Tractors carry or pull an implement; they do nothing alone**, and an implement or a cart moves only on a hitch: it cannot be
  driven by itself. Research **tillage and sowing** (and **articulated tractors** for the T400), build a **Thane** tractor and an
  implement, and hitch the implement: the **Sillion plough** and the **Tillman cultivators** on the three-point linkage, the **Coulter
  seed drill** on the drawbar. Then the tractor's **Plough**, **Cultivate** or **Sow a paddock** order appears.
- **The Thane T400 is built in two halves.** Its blueprint is the front half: **leave room behind it**, where its rear half is set down
  when it is finished. The halves never come apart.
- **A drill needs seed in its hopper**: the crop's own grain kept back from a harvest, or **seed grain** bought from bulk goods traders.
  Pour it in from a bin or cart, or haul the stacks next to the drill and press **Load from the ground** (it takes crops and seed only).
- **Each front cuts only the crops it really could.** The grain fronts take cereals, pulses and oilseeds: rice, and with Vanilla Cooking
  Expanded or Vanilla Plants Expanded their buckwheat, chickpeas, peas and canola. **The corn header takes corn and sunflowers.**
  **Potatoes, hay and cotton need harvesters that come later**, and stay for hands.
- **Wheat, barley, oats, rye and sorghum stay for hands too.** With Vanilla Cooking Expanded they give flour straight from the plant,
  which no combine pours out and no drill sows from. A drill still sows them from bought seed grain.
- **A combine's tank takes one kind of grain at a time.** With another crop's grain still in it, it refuses the paddock and says so:
  pour it out first. Another mod's crop is
  taken once it is placed in a class: see [For modders](#for-modders).
- **A full combine needs somewhere to empty**: a **Garner chaser bin** parked near the paddock (any vehicle with a hitch tows it), a
  **field bin**, or a **grain silo** (after **grain storage** research). The silo keeps grain four times as long, **but only while its
  fan has power**. **Empty a bin or silo before taking it down**: full, at most 40 heaps are saved, and it warns you first.
- **Settings to decide before you start:** Options > Mod settings > D'thorga Farming. **Hands may help on new paddocks** (off),
  **A machine's driver can fail a harvest, as by hand** (on), and **Hands use seed too** (off: on, colonists sowing grain, peas,
  oilseeds or corn by hand take a handful of it from a stockpile first, and will not sow without it). With **The Dead Man's Switch -
  Legion**, also **Agricultural Frame: hay only from straw crops** (on: its mech's extra hay comes only from grains, pulses, canola, corn,
  sunflowers and haygrass, as baled straw; off: from every plant, as its author made it).
- **Safe to add to a running colony.** **Removing it:** paddocks become plain growing zones; every combine, tractor, implement, chaser
  bin, field bin and silo vanishes with the mod, with the grain and seed in them. Empty them first.
- **Not yet tested in game.** Everything here is built and checked offline, and read from the game's and the mods' code.

## What it does

1. **Mark a paddock.** Machine-worked on any growing zone. Hands may help lets colonists sow and harvest it too.
2. **Prepare and sow it with a tractor.** Hitch a plough or cultivator, press **Plough** or **Cultivate a paddock**: weeds and wild plants
   go, a sown crop never does. The turnover plough rolls over at each row's end. Then hitch a seed drill with seed in it and press **Sow a
   paddock**. A cart can follow behind any implement. **Lower** works an implement by hand anywhere the tractor drives.
3. **Fit a front and give the combine a driver.** Press **Harvest a paddock** and pick the paddock. With nobody aboard, it calls a
   colonist with Drive work to take the wheel.
4. **It works the rows by itself.** It lays the paddock out in rows along its longer side, starting from the corner nearest it, drives each
   row and cuts every ripe plant its front covers. Unripe plants are left standing.
5. **It cuts as a colonist would.** The driver's harvesting skill sets the yield and the chance of a failed cut. Other mods that act when a
   plant is harvested still see the harvest. Straw and other second products are left lying where each row ends, unless a straw chopper
   is fitted.
6. **When its tank is full** it drives to the nearest chaser bin, field bin or silo with room, pours the grain in through its auger,
   and goes back to the rows. With nowhere to empty, it stops and says so.
7. **Grain is held by weight, as one amount**, not in stacks. Pour it from a chaser bin onto the ground, where colonists haul it as
   usual, or into another bin. It goes off as it would in the open.

| Machine | What it is | Holds |
| --- | --- | --- |
| Reaper combine harvester | Self-propelled; 4 by 9 cells; a 6- or 8-cell grain front, or a 6-cell corn header | 9 tonnes of one crop at a time (12 with the tank extension) |
| Garner chaser bin | Towed; 3 by 7 cells | 12 tonnes, any crops |
| Field bin | A building, 3 by 3 cells | 25 tonnes, any crops |
| Grain silo | A building, 4 by 4 cells, 300 W | 60 tonnes, any crops; keeps four times as long while powered |
| Thane T60 and T60 F | Small tractor, 60 kW, 3 by 5 cells; the F on wide tyres | Linkage and drawbar |
| Thane T130 and T130 RC | Medium tractor, 130 kW, 3 by 6 cells; the standard ballasted, the RC lighter | Linkage, drawbar and pick-up hitch |
| Thane T400 and T400 B | Large articulated tractor, 400 kW, two halves of 4 by 4 cells; the B ballasted | Linkage, drawbar and pick-up hitch on its rear half |
| Sillion turnover plough | On the linkage, 1 cell wide (2 with the wing extension) | |
| Tillman cultivator and light cultivator | On the linkage, 4 cells wide (7 with the wing extension); the light one pulls easier | |
| Coulter seed drill | On the drawbar, 4 cells wide (7 with the wing extension) | 1.5 tonnes of seed (half as much again with the hopper extension) |

**Upgrades:** liquid tyre ballast and front weights (more grip, a little more fuel), dual rear wheels (more grip on soft ground) and
the framework's auxiliary fuel tank (drawn as a saddle tank) on the tractors; wing extensions on every implement and a hopper extension
on the drill; a tank extension and a straw chopper on the combine, and a fast auger on the chaser bin. The front weights, dual wheels,
saddle tank, wings, hopper extension and the combine's fronts are drawn on the machine once fitted; liquid ballast, the tank extension,
the straw chopper and the fast auger are not.

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
repository's licence.

**The tractors' engine sounds are real recordings** of farm tractors, cut and levelled, each with a record beside it naming its source:
- A Valmet tractor working a harrow (1985), by **Yle, the Finnish Broadcasting Company's archive (YleArkisto on Freesound)**,
  [CC BY 4.0](https://creativecommons.org/licenses/by/4.0/): [the recording](https://freesound.org/people/YleArkisto/sounds/329643/).
- A Case IH CX90's engine stopping, by **Soundholder on Freesound**, [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/):
  [the recording](https://freesound.org/people/Soundholder/sounds/425859/); also the root of the large tractor's stop.
- With thanks to **mickyman5000**, **kyles** and **bruno.auzet** on Freesound, whose recordings are CC0.
- The large tractor's sounds are built from these, lowered and layered; the medium and large tractors' starts are generated.

Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights
reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
