# D'thorga Farming changelog

Maintained from 20 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.3.0] - 2026-11-20 - Draft

A corn header for the combine

### Added

- **The corn header, a third front for the combine**, for corn and sunflowers: snouts part the rows and chains pick the cobs. It cuts 6 cells wide and takes row crops only; fitting it takes off a grain front, as fronts fill the same place.

### Compatibility and limits

- Its picture was made again at high quality and twice the size after the first round came out blurry in the finer details. Not yet tested in game.

## [0.2.0] - 2026-11-20 - Draft

A field bin and a grain silo to pour a harvest into

### Added

- **The field bin holds 25 tonnes of grain at the edge of a paddock.** A combine with a full tank drives to it and empties itself, as it does to a chaser bin; a chaser bin within reach pours into it with its Pour button. Press Pour on the bin to empty it into a cart or onto the ground. Grain keeps in it as long as in the open.
- **The grain silo holds 60 tonnes, and keeps grain four times as long while its fan has power.** Without power it keeps no longer than a field bin. Its inspect text says which. After the new grain storage research.
- Both are built from the motor pool tab of the Architect menu.

### Balance

- The field bin: 220 steel, 25 tonnes, pouring 60 kg a second. The silo: 420 steel, 2 components and an electric motor, 60 tonnes, 300 W, pouring 80 kg a second, grain at a quarter of its spoiling rate while powered. All authored, to tune in play.

### Compatibility and limits

- Destroyed or deconstructed, a bin or silo spills what it held onto the ground beside it. Not yet tested in game.

### Save compatibility

- Safe to add. Removing the mod: bins and silos vanish with the grain in them; empty them first.

## [0.1.0] - 2026-11-20 - Draft

The first build: machine-worked paddocks, a combine harvester that cuts them row by row, a chaser bin to carry its grain

### Added

- **Mark any growing zone as a paddock for machines.** A Machine-worked button on every growing zone; while machines alone work a paddock, colonists keep off it, sowing and harvesting both. A Hands may help button lets them work it too. There is no seed drill yet, so a paddock is sown by hand with Hands may help on.
- **The Reaper combine harvester harvests a paddock row by row by itself.** Give it a driver and press Harvest a paddock: it lays out the rows, drives each one, and cuts every ripe plant its front takes into its grain tank. Unripe plants are left standing. It cuts by the game's own harvest rules, with the driver's harvesting skill setting the yield and the chance of a failed cut, as by hand; other mods that act when a plant is harvested still see it. With nobody aboard, pressing it calls a driver.
- **Grain fronts take only the crops they could really cut.** A combine has no front until one is fitted: the standard grain front cuts 6 cells wide, the wide grain front 8 (after research). Both take cereals, pulses and oilseeds; corn, potatoes, hay and cotton need harvesters that come later. Rice, and the cereals, pulses and canola of Vanilla Cooking Expanded and Vanilla Plants Expanded, are placed in their classes; other mods' crops can be with one patch each.
- **A full combine empties itself.** When its tank is full it drives to the nearest chaser bin or bin with room, pours through its auger, and goes back to the rows. With nowhere to empty, it stops and says so.
- **Straw is left in the field.** A crop's second product, such as barley's straw with Vanilla Expanded's cereals, is dropped where each row ends, as a combine leaves it; the straw chopper upgrade chops it into the field instead.
- **The Gleaner chaser bin**, a towed grain cart that any vehicle with a hitch can pull. It takes a combine's grain and pours it out through its own auger.
- **Grain is held by weight, as one amount, not in stacks**, and it weighs on the engine that carries it. Pour it into another bin or onto the ground with the Pour button. It keeps as long as it would in the open, and goes off if left too long.
- **Research:** mechanised harvesting and wide grain fronts, on the D'thorga research tab.
- **Upgrades:** the two grain fronts, a tank extension (a third more grain) and a straw chopper for the combine; a fast auger for the chaser bin.

### Balance

- The combine: 16 tonnes, 250 kW, a 9-tonne grain tank, 200 chemfuel; threshing takes 40% of its engine while it works a row. The chaser bin: 12 tonnes of grain, pouring 120 kg a second. All authored, to tune in play.

### Dependencies

- Needs Vehicle Framework and D'thorga Framework 0.189.0 or later.

### Compatibility and limits

- The combine and the chaser bin use the game's own vehicle sounds until their own are made.
- Their upgrade icons are cut from the machines' own pictures until their own are drawn.
- Not yet tested in game.

### Save compatibility

- Safe to add to a running colony. Removing it: paddocks become plain growing zones; combines and chaser bins and the grain in them vanish with the mod.
