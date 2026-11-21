# D'thorga Farming changelog

Maintained from 20 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.7.3] - 2026-11-21 - Draft

The paddock button's own picture. Not yet tested in game.

### Changed

- **The button that marks a growing zone for machines has its own picture**: a green field split by a tyre track, drawn locally with our new house style model and picked by the owner. It borrowed the game's growing-zone picture before.

## [0.7.2] - 2026-11-21 - Draft

Farming's own Architect tab

### Changed

- **The farm machines and grain stores are in their own Farming tab, inside the Architect's D'thorga tab** (the owner's choice of 21 November 2026): the Thane tractors, the implements, the Reaper combine, the Garner chaser bin, the field bin and the grain silo, all together. They were in the Motor pool tab.

### Dependencies

- Needs D'thorga Framework 0.205.0, installed with it.

### Compatibility and limits

- A layout change only. Safe with a running save. Not yet tested in game: row 301 of the test pass.

## [0.7.1] - 2026-11-21 - Draft

Fixes from a fault-read of batches one and two, before their first test

### Changed

- **Wheat, barley, oats, rye and sorghum are left to hands** (the owner's choice of 21 November 2026). With Vanilla Cooking Expanded they give flour straight from the plant, which no combine pours out and no drill sows from, so no front cuts them now. A drill still sows them from bought seed grain, as it sows any crop. Rice, buckwheat, chickpeas, peas, canola, corn and sunflowers are as before.

### Fixed

- **The seed drill sows its whole width.** Its working line was its own rear row, and the game plants nothing in a cell a vehicle stands on, so about one cell in four was sown. It now sows the line just behind it, and a cart following the drill is not in the way.
- **The plough and cultivator drive each row once.** A tilled cell is empty, and an empty cell had counted as work still to do, so every row was driven a second time and a paddock was marked ploughed at half coverage.
- **A save and load in the middle of a tractor's run no longer leaves it driving on and working nothing.** The implement now saves its paddock and its tallies.
- **A combine with another crop's grain left in its tank says so and does not start.** It had driven the whole paddock twice and cut nothing, with no reason given.
- **The straw chopper chops only straw.** A second product a person can eat was destroyed with it.
- **Colonists harvesting beside a machine-only paddock no longer take its plants.**
- **A hopper with too little seed for a cell keeps it, and the run stops at the row's end.** The last few units had been thrown away.
- **A combine leaving the map partway along a row pours what it has cut into its tank.**
- **A paddock carried on a gravship keeps its mark** on the map it lands on.
- **The drill sows a heated greenhouse in winter, as hands do**, and its driver's count of plants sown goes up.
- **With Combat Extended, a loadout no longer drops the seed a colonist carries to sow by hand** (the Hands use seed too setting).
- **The Sillion plough no longer throws an error at every start-up with Combat Extended.** Its body picture had nothing in it for Combat Extended to measure.
- **The T400's rear halves no longer draw two warnings at every start-up**, and each sits where the two pictures meet: it was drawn about a fifth of a cell too far forward.
- **The T60 F and the T130 RC grip soft ground as their descriptions say.** No figure had given it.
- **Smaller fixes**: the Machine-worked button's description knew of no seed drill; the wide grain front no longer claims more load on the engine; a crop with nothing to weigh still takes a little bought seed; sowing by hand with no seed in reach asks once, not for every cell.

### Balance

- Soft ground grip: 0.12 on the T60 F and 0.06 on the T130 RC, against the 0.25 dual rear wheels add. Authored, to tune in play.

### Dependencies

- Needs D'thorga Framework 0.202.0, installed with it. The tractors no longer pay the combine's threshing load, which is fixed there.

### Save compatibility

- Safe with a running save. A tractor's run saved by 0.7.0 in the middle of a paddock should be stopped and started again.

### Compatibility and limits

- **Found by reading, not in play** (docs/development/fault-read-2026-11-21.md). Not yet tested in game: rows 272 to 274, 276, 277, 279 to 281, 285 and 291 to 293 of the test pass.
- Left as they were: two research lines drawn backwards on the research tab; the chaser bin, tractors and implements have no Combat Extended armour figures; a towed bin's auger takes no engine power.
- To look at in play: that a machine keeps to its row, and that nothing faint shows under the plough.

### Changed

- **Upgrade trees start at the top of the tab.** Each machine's upgrades sat four rows down under an empty band; they now start just under the tab's title, laid out like the trucks'. Only positions changed: no upgrade, link or cost, and fitted upgrades stay fitted. Not yet tested in game.

## [0.7.0] - 2026-11-20 - Draft

Batch two: tractors, implements, upgrades and bought seed

### Added

- **The Thane tractors, small and medium, in two models each** (the owner's picks of 20 November 2026), under one button each in the motor pool: the T60 and the T60 F on wide tyres, the ballasted T130 and the lighter T130 RC. Each carries an implement on its three-point linkage and pulls on its drawbar, and drives a paddock row by row with the implement working. Unlocked by the new research **Tillage and sowing**, which needs Mechanised harvesting and Machining; a second, **Articulated tractors**, follows it for the large tractor to come.
- **The Thane T400, the large articulated tractor, in two models** under one button: the standard T400 with a larger fuel deck and the ballasted T400 B. It bends in the middle, its rear half following on the pivot and carrying the linkage and drawbar, and all its weight grips. Built from one blueprint: leave room behind the front half for the rear. Unlocked by Articulated tractors. Needs D'thorga Framework 0.195.0.
- **The implements, ready to carry and pull**: the **Sillion turnover plough** (three furrows, one cell wide, as drawn), which rolls over at each row's end so every furrow is thrown the same way, its picture showing the side that is down; the two **Tillman cultivators** under one button, the standard and the light one that pulls easier; and the **Coulter seed drill** with a 1.5-tonne seed hopper you pour seed into. Each has a hitch of its own, so a cart can follow it. Their order buttons show the implement itself. Needs D'thorga Framework 0.196.0.
- **Upgrades for every machine** (the owner's picks of 20 November 2026), each drawn on the machine once fitted: liquid tyre ballast and front weights (more weight on the wheels, more grip, a little more fuel), dual rear wheels (more grip on soft ground), a bigger fuel tank (the framework's auxiliary fuel tank, drawn as a saddle tank), wing extensions that widen the plough to two cells and the cultivators and drill to seven (pulling harder as they widen), and a hopper extension that holds half as much seed again. Needs D'thorga Framework's soft ground grip (in its Unreleased changes).
- **Bought seed**: seed grain, sold by bulk goods caravans and orbital bulk traders at about three times grain's value, which a seed drill sows any crop with.
- **The tractors' engines sound like real tractors** (the owner's picks of 20 November 2026): recordings of real farm tractors for the small and medium sizes, starting, idling, working and stopping, rising in pitch with the engine and blending into the working sound under load; the large tractor's built from the medium ones, lowered and layered; generated starts where no recording had one. Credits are in the README.
- **A new setting, Agricultural Frame: hay only from straw crops, on by default** (the owner's request; Mod settings, D'thorga Farming, General, under Other mods, shown only with The Dead Man's Switch - Legion). Its Agricultural Frame drops hay with every plant it harvests, cabbages and berries included. On, only crops that leave straw or forage behind give it: grains, peas, chickpeas, canola, corn, sunflowers and haygrass. Vegetables, fruit, root crops, cotton, flowers and herbs give none. The frame's own amounts are kept. Off, it is as its author made it. Takes effect at once. Each crop class now says whether it leaves straw, in its XML, so other mods' crops follow the class they are put in.

### Compatibility and limits

- **Not yet in this batch:** the tractors' door, brake and fuel sounds; the liquid ballast node shows the dual wheels' icon until its own is drawn; an implement does not yet look different while lifted. Every figure is the agent's, in the ranges of real tractors.
- **Grain and seed reach a drill two ways**: poured from a bin or cart, or taken in from stacks hauled next to it (D'thorga Framework's Load from the ground).
- Safe to add to a running colony. Removing it removes every tractor and implement with the mod.
- Not yet tested in game: rows 276, 277, 279 to 281, 285 and 291 to 293 of the test pass.

### Dependencies

- Needs D'thorga Framework 0.200.0 with its changes since (soft ground grip, loading from the ground), installed with it.

### Fixed

- **The plough and cultivator now spare every sown crop, as their buttons say.** Before, they spared only the paddock's chosen crop: change a paddock from cabbage to wheat and plough it, and any cabbages still growing were destroyed with no harvest. Now any crop a colonist sowed is passed over, whatever its kind; weeds, stubble and wild plants are still cleared. Found in an audit, not yet tested in game.
- **Art records no longer hold a path from the developer's PC.** The export records beside some textures named the full folder they were made in; they now name the file within the project. Nothing a player sees changes.

## [0.6.0] - 2026-11-20 - Draft

Hands use seed too, a setting for more realism

### Added

- **A new setting, Hands use seed too, off by default** (the owner's choice, for players who want more realism; Mod settings, D'thorga Farming, General, under Seed). On, a colonist sowing by hand a crop whose harvest is its own seed (grains, peas, oilseeds, corn) first takes a handful of it from a stockpile into their inventory, about twenty cells' worth, and uses it up as they sow, at the same rate a seed drill uses: about three per cent of what each cell will yield, plus a tenth lost to cleaning. With none they can reach, they will not sow that crop, and the reason shows when you ask them to. Other crops are still sown free.
- **Each crop class now says whether its harvest is its own seed**, in its XML, so other mods' crops follow the class they are put in.

### Fixed

- **The seed drill no longer sows a crop from a harvest that is not seed**: as built, it would have sown cotton from cloth or haygrass from hay. Such crops now sow only from bought seed (not yet in the game). Nothing had used the drill yet.

### Compatibility and limits

- Root crops (potatoes) count as sowing free for now: seed potatoes are real, but take about a tenth of the crop, not the few per cent of grain, and are not modelled. An agent's choice, for the owner to review.
- What a colonist still owes below one unit of seed is not saved, so at most a unit's worth is forgotten at a load.
- Another mod that empties colonists' inventories (Combat Extended's loadouts can) may drop the handful; the colonist then fetches another.
- Not yet tested in game: row 281 of the test pass.

## [0.5.0] - 2026-11-20 - Draft

An implement lowered by hand, and the owner's decisions for batch two

### Added

- **Lower and Lift buttons on an implement** (the owner's choice of 20 November 2026). Lowered, it works wherever the tractor drives it, not only on a paddock run, and pulls hard while it does: tillage clears weeds and wild plants and never a sown crop, and a drill sows only a paddock it could sow on a run. A paddock run lifts it, then lowers it on each row as before. The buttons use the framework's set-up and pack-up pictures until drawn ones are picked.

### Changed

- **An implement behind another one is driven by the tractor at the front**: whoever drives the tractor counts as the driver for skill and records.
- The owner's decisions are in the batch-two record: draught kept at Iowa State's medium soil with a cell taken as 1.5 metres; a wider implement by a wing-extension upgrade rather than a second model; bought seed at about three times grain's value, from bulk goods traders.

### Dependencies

- Needs D'thorga Framework 0.193.0 (lines of tows; implements lowered by hand).

### Compatibility and limits

- **Nothing to press yet**: the plough, cultivator and drill are not built (their pictures wait for the owner's go), so the buttons have nothing to sit on.
- No new settings. Safe to add to and remove from a save as before. Not yet tested in game: row 280 of the test pass.

## [0.4.2] - 2026-11-20 - Draft

The owner's names for the machines

### Changed

- **The chaser bin is now the Garner chaser bin (Garner G1)**, the owner's name. The combine stays the Reaper; the tractors to come are Thanes, the plough a Sillion, the cultivator a Tillman and the seed drill a Coulter. A name only: nothing saved changes.

## [0.4.1] - 2026-11-20 - Draft

Drawn icons for the harvest order and five upgrades

### Changed

- **The Harvest button and five upgrades have drawn icons** (the owner's picks of 20 November 2026), in place of pictures cut from the machines themselves: the wide grain front, the corn header, the tank extension, the straw chopper and the fast auger.
- The standard grain front keeps its cut picture, and the paddock button the game's growing-zone picture, until their redraws are picked.

### Compatibility and limits

- Pictures only; no change in play. Safe with a running save.
- Not yet tested in game.

## [0.4.0] - 2026-11-20 - Draft

Fixes from a fault-read: no harvested grain is lost; and the plough, cultivator and seed drill ready for their tractors

### Fixed

- **Grain a combine has cut is never lost.** Plants go as they are cut, and their grain waited until the row's end: a save and load partway along a row, the combine leaving the map, or a stopped run lost it or dropped it all on the ground even with room in the tank. It now goes into the tank first, keeps across a save, and only what does not fit is left beside the combine.
- **Straw that finds no room on the ground is kept for the next row's end**, not lost.
- Colonists looking for harvesting work find paddocks a little faster.

### Added

- **The plough, the cultivator and the seed drill are written**, waiting for their tractors and pictures (farming batch two): the plough and cultivator clear weeds, stubble and wild plants and never a sown crop or a tree; the drill sows the paddock's crop by the game's own sowing rules, with the crop's own grain or bought seed. Nothing of them is in the game yet.
- A paddock remembers when it was last ploughed or cultivated, for the soil rules to come.

### Compatibility and limits

- One thing to watch in play, from the fault-read: whether colonists still harvest a paddock machines alone work. The patch that keeps them off it changes a very short method the game might fold into its caller; if they do, say so.
- Not yet tested in game.

### Save compatibility

- Safe. A combine's cut-but-not-poured grain and a paddock's tilling are new saved fields; older saves load without them.

## [0.3.3] - 2026-11-20 - Draft

The combine and the chaser bin no longer sell for more than they cost to build

### Balance

- **The combine's market value is 3,000, down from 4,200, and the chaser bin's 1,300, down from 1,500.** Their parts are close to the Talus tipper's and the flatbed's, so selling one had paid more than building it. Machines add cells, never yield: a harvest gives what hands would get from the same cells, and selling crops stays limited by traders, as in the plain game (the economy check, docs/development/farming-machines.md).

## [0.3.2] - 2026-11-20 - Draft

The chaser bin renamed

### Changed

- **The chaser bin is now the Auger chaser bin (Auger A1).** The name it was first given belongs to a real maker of combines, so it was changed before anything was installed or published; earlier entries here use the new name. A name only: nothing saved changes. The machines' names remain the owner's to choose.

## [0.3.1] - 2026-11-20 - Draft

The grain fronts redrawn sharper

### Changed

- **The standard and wide grain fronts have new, sharper pictures**: their knife guards, reels and augers were soft at the size the first ones were made. The same fronts, the same widths; nothing else changes in play.

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

- Destroyed or deconstructed, a bin or silo spills what it held onto the ground beside it, as many heaps as will go (40 at most), and names what was lost. Not yet tested in game.

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
- **The Auger chaser bin**, a towed grain cart that any vehicle with a hitch can pull. It takes a combine's grain and pours it out through its own auger.
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
