# D'thorga Farming

Machines that work your fields: mark a growing zone as a paddock, and tractors plough, cultivate and sow it, and a combine
harvester cuts it, row by row by themselves.

One of House D'thorga's mods (also spelt dthorga).

Version 0.8.1, built 28 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Not yet tested in game.** The owner has not passed any of it yet: it is built and checked offline, and read from the game's
  and the mods' code.
- **Needs Vehicle Framework and D'thorga Framework** (0.202.0 or later). Load it after both; its About file asks for that order,
  so RimSort sorts it for you.
- **RimWorld 1.7 only.**
- **Mark the field first**: select a growing zone and press **Machine-worked**. Colonists then keep off it, unless **Hands may
  help** is on.
- **A combine has no front until you fit one**, and refuses to harvest without it. Fit the **standard grain front** from its
  upgrades.
- **Tractors carry or pull an implement; they do nothing alone**, and an implement or a cart moves only on a hitch. Hitch one, and
  the tractor's **Plough**, **Cultivate** or **Sow a paddock** order appears.
- **The Thane T400 is built in two halves: leave room behind its blueprint**, where the rear half is set down when it is finished.
- **A drill needs seed in its hopper**: the crop's own grain kept back from a harvest, or **seed grain** bought from bulk goods
  traders.
- **Each front cuts only the crops it really could.** The grain fronts take cereals, pulses and oilseeds; the corn header takes
  corn and sunflowers. Potatoes, hay and cotton stay for hands. **With Vanilla Cooking Expanded, wheat, barley, oats, rye and
  sorghum stay for hands too**: they give flour straight from the plant, which no combine pours out (a drill still sows them from
  bought seed grain).
- **A combine's tank holds one kind of grain at a time**: with another crop's grain still in it, it refuses the paddock and says
  so.
- **A full combine needs somewhere to empty**: a chaser bin near the paddock, a field bin or a grain silo. **The silo keeps grain
  four times as long only while its fan has power.** **Empty a bin or silo before taking it down**: full, at most 40 heaps are
  saved, and it warns you first.
- **Settings to decide before you start**: **Hands may help on new paddocks** (off), **A machine's driver can fail a harvest, as
  by hand** (on) and **Hands use seed too** (off), and, with The Dead Man's Switch - Legion, **Agricultural Frame: hay only from
  straw crops** (on). What each does is under [Settings](#settings).
- **Safe to add to a running colony.**
- **Removing it**: paddocks become plain growing zones, and every combine, tractor, implement, chaser bin, field bin and silo
  vanishes with the mod, with the grain and seed in them. **Empty them first.** Every mod's steps are in [Removing a
  mod](../../docs/player-guide/getting-started.md#removing-a-mod).

## What it adds

How to plough, sow and harvest, step by step, is in the player guide's [Farming chapter](../../docs/player-guide/farming.md).

- **Paddocks**: any growing zone marked **Machine-worked**.
- **Thane tractors** in three sizes, with a turnover plough, cultivators and a seed drill to hitch, and **Lower** to work an
  implement by hand anywhere the tractor drives.
- **The Reaper combine harvester**: it lays the paddock out in rows, cuts every ripe plant its front covers as a colonist would
  (the driver's harvesting skill sets the yield), calls a driver when nobody is aboard, and empties a full tank into the nearest
  bin or silo with room.
- **Grain held by weight**, as one amount, in a chaser bin, field bin or grain silo. Pour it onto the ground, where colonists haul
  it as usual. It goes off as it would in the open.
- **Upgrades** on every machine (below).

| Machine | What it is | Holds |
| --- | --- | --- |
| Reaper combine harvester | Self-propelled; 4 by 9 cells; a 6- or 8-cell grain front, or a 6-cell corn header | 9 tonnes of one crop at a time (12 with the tank extension) |
| Garner chaser bin | Towed by any vehicle with a hitch; 3 by 7 cells | 12 tonnes, any crops |
| Field bin | A building, 3 by 3 cells | 25 tonnes, any crops |
| Grain silo | A building, 4 by 4 cells, 300 W | 60 tonnes, any crops; keeps four times as long while powered |
| Thane T60 and T60 F | Small tractor, 60 kW, 3 by 5 cells; the F on wide tyres | Linkage and drawbar |
| Thane T130 and T130 RC | Medium tractor, 130 kW, 3 by 6 cells; the standard ballasted, the RC lighter | Linkage, drawbar and pick-up hitch |
| Thane T400 and T400 B | Large articulated tractor, 400 kW, two halves of 4 by 4 cells; the B ballasted | Linkage, drawbar and pick-up hitch on its rear half |
| Sillion turnover plough | On the linkage, 1 cell wide (2 with the wing extension) | |
| Tillman cultivator and light cultivator | On the linkage, 4 cells wide (7 with the wing extension); the light one pulls easier | |
| Coulter seed drill | On the drawbar, 4 cells wide (7 with the wing extension) | 1.5 tonnes of seed (half as much again with the hopper extension) |

**Upgrades**: liquid tyre ballast and front weights (more grip, a little more fuel), dual rear wheels (more grip on soft ground)
and the framework's auxiliary fuel tank (drawn as a saddle tank) on the tractors; wing extensions on every implement and a hopper
extension on the drill; a tank extension and a straw chopper on the combine; a fast auger on the chaser bin. Liquid ballast, the
tank extension, the straw chopper and the fast auger are not drawn on the machine; the rest are.

Every figure is authored, to tune in play.

## Settings

In **Options > Mod settings > D'thorga Farming**.

| Setting | What it does | Default |
| --- | --- | --- |
| Hands may help on new paddocks | On: colonists may still sow and harvest a paddock by hand. Off: machines alone work a new paddock until you turn **Hands may help** on for it | Off |
| A machine's driver can fail a harvest, as by hand | A combine's driver loses some plants to a failed harvest by the same chance a colonist harvesting by hand would: a skilled driver loses fewer | On |
| Hands use seed too | On: a colonist sowing a crop whose harvest is its own seed (grains, peas, oilseeds, corn) first takes a handful of that crop from a stockpile, about 3% of what each cell will yield, and won't sow it without one. Off: sowing by hand is free, as in the game. Cotton, hay and potatoes are always free | Off |
| Agricultural Frame: hay only from straw crops | With The Dead Man's Switch - Legion. On: its Agricultural Frame's extra hay comes only from crops that leave straw (grains, peas, chickpeas, canola, corn, sunflowers, haygrass). Off: hay from every plant, as its author made it | On |

**Rain soaks open fields**, which helps any outdoor growing zone, is a D'thorga Framework setting (under **Fields**, off by
default).

## More

- [The player guide's Farming chapter](../../docs/player-guide/farming.md): ploughing, sowing and harvesting, and what is tested
  in game.
- [The changelog](CHANGELOG.md): what changed in each version.
- [For modders](../../docs/modding/farming.md): putting another mod's crop in a class, so a front takes it.

## Credits and licence

The machines' pictures are AI-generated (OpenAI gpt-image-2) and picked by the owner; the record of how each was made is kept in
the repository. Code under the repository's licence.

**The tractors' engine sounds are real recordings** of farm tractors, cut and levelled, each with a record naming its source:

- A Valmet tractor working a harrow (1985), by **Yle, the Finnish Broadcasting Company's archive (YleArkisto on Freesound)**, [CC
  BY 4.0](https://creativecommons.org/licenses/by/4.0/): [the recording](https://freesound.org/people/YleArkisto/sounds/329643/).
- A Case IH CX90's engine stopping, by **Soundholder on Freesound**, [CC BY 3.0](https://creativecommons.org/licenses/by/3.0/):
  [the recording](https://freesound.org/people/Soundholder/sounds/425859/); also the root of the large tractor's stop.
- With thanks to **mickyman5000**, **kyles** and **bruno.auzet** on Freesound, whose recordings are CC0.
- The large tractor's sounds are built from these, lowered and layered; the medium and large tractors' starts are generated.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
