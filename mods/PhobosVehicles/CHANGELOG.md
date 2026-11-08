# D'thorga Vehicles changelog

Maintained from 8 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

### Fixed

- The bulk tanker no longer looks damaged when it is new: its damage marks now appear only while the part is really damaged. Found in the owner's first look. **Not yet tested in game.**

### Balance

- The bulk tanker is now 3 by 10 cells, the size of RimThunder's biggest truck, instead of 2 by 6, which looked tiny beside everything else (owner's first look). A tanker already built keeps its place on the map, so it may overlap what is beside it: move it clear first.

### Changed

- A leaking or burst tank now leaves the right mess: blood from hemogen, slime from nutrient paste, water from water and an oil smear from deepchem, as well as the chemfuel puddles it already left. **Not yet tested in game.**
- A note in the M1A2's loadout file now says where its oil's temperature comes from (D'thorga Framework 0.29.1 fixed it). Text only; nothing in game changes here.
- The mod description and the README's Coming later section now describe what 0.10.0 does (turbines, boilers, real figures, climate control, the electric Lightning) instead of 0.1.0, and name the five utility vehicles planned, each with an upgrade tree, with parts from RimThunder and Vanilla Vehicles Expanded where present (owner's directions, 8 November 2026; design record docs/development/utility-vehicles-design.md). Text only; nothing in game changes.
- Dates in this mod's records that were written a day ahead now read 8 November 2026. Text only; nothing in game changes.

### Compatibility and limits

- BMP-1 IFV (Vehicle Framework), by Turbo, is no longer on the Steam Workshop (Steam showed no public item on 8 November 2026: removed or made private). Its loadout here loads only while that mod is active, so it stays dormant and costs nothing; a player with their own copy still gets it.

## [0.15.0] - 2026-11-09 - Draft

Compartments for the bulk tanker (owner, 9 November 2026). **Not yet tested in game.** Needs D'thorga Framework 0.37.0.

### Added

- Two upgrades divide a liquid tank into two, then three compartments, one to each hatch, so it can carry different fluids on one trip. Pressure vessels stay whole.

### Balance

- Food cannot share a tank with anything else. Water, nutrient paste, hemogen and wort are listed as food by hand, since no item of theirs carries a nutrition value; beer and insect jelly count by their own.

## [0.14.0] - 2026-11-09 - Draft

Our vehicle parts moved to D'thorga Framework (owner, 9 November 2026). **Not yet tested in game.**

### Changed

- The engine assembly, drivetrain kit and electrical kit are now provided by D'thorga Framework 0.36.0 and always exist, not only when neither RimThunder nor Vanilla Vehicles Expanded is active. Their internal names are unchanged.

### Dependencies

- Needs D'thorga Framework 0.36.0.

## [0.13.0] - 2026-11-09 - Draft

Things the game keeps as items that are really fluids (owner, 9 November 2026). **Not yet tested in game.** Needs D'thorga Framework 0.35.0.

### Added

- Beer, wort and insect jelly out of the box.
- FSX and prometheum with Combat Extended. FSX may go off when the tank is hit. Prometheum travels only in a pressure vessel, and any spill burns.
- Toxic wastepacks as sludge with Biotech: they dissolve after 8 days unless frozen, as packs do, and a spill or a dissolved load pollutes the ground.
- Fecal sludge with Dubs Bad Hygiene, pumped from septic tanks or barrels and set down as barrels.
- Napalm and crude oil barrels with Rimefeller.

### Changed

- Pressure vessels now also appear with Combat Extended.

### Balance

- Ten units to one FSX or prometheum, thirty to a wastepack, twenty-five to a sludge barrel. A blast stops at 15 cells. All authored but the sludge barrel, which is Dubs' own figure.

## [0.12.0] - 2026-11-08 - Draft

The first vehicle of our own: the bulk tanker (owner, 8 November 2026). **Not yet tested in game.** Needs D'thorga Framework 0.34.0. Design: docs/development/utility-vehicles-design.md. Art: OpenAI gpt-image-2, picked and accepted by the owner, recorded in the textures folder.

### Added

- The bulk tanker: a 2 by 6 heavy truck whose tank is an upgrade. Three liquid tanks (general, insulated, high-volume), and three pressure vessels (standard, light, heavy-wall) where Vanilla Gravship Expanded gives it a gas to carry, plus a second hose reel.
- It carries chemfuel out of the box, refuelling vehicles and pumping stacks on the ground. Deepchem, chemfuel pipes, water, nutrient paste, hemogen, astrofuel and oxygen each come with the mod that gives them meaning.
- It is built from the parts your mods provide: RimThunder's engine and running gear, Vanilla Vehicles Expanded's electrics and cooling, or three parts of our own, made at the machining table, if you have neither.

### Compatibility and limits

- Not safe to remove from a save once a tanker is built: scrap it first, as with any Vehicle Framework vehicle.
- All health, cost, mass, capacity and pump figures are authored and untuned.
- Not built yet: RimThunder engine upgrades for the tree, the fire monitor, crude oil from Rimefeller, and helixien gas. The overlays are not repainted with the truck.

## [0.11.0] - 2026-11-08 - Draft

The garage, at the owner's request (8 November 2026). **Not yet tested in game.**

### Added

- With Vanilla Expanded Framework: the garage, a staffed outpost on the world map. It costs 200 steel and 10 components, needs Construction 6 among those who stay, and gives three bays per person up to twelve. Every three days it repairs from its stores, 40 points per skill level, a steel for each 10 points and a component for each 400. All of these are authored balance.

### Compatibility and limits

- The world-map icon is a stand-in, the game's caravan picture, until art is picked.

### Save compatibility

- Safe to add to a save. **Before removing it, take every vehicle out of every garage**: a garage and the vehicles in it go with the mod.

## [0.10.0] - 2026-11-08 - Draft

Automatic climate control by era, with D'thorga Framework 0.22.0 (owner, 8 November 2026: by technology and age). **Not yet tested in game.**

### Added

- Climate control on the modern vehicles: the L-ATV, the MaxxPro, the M1A2 SEP, the M10 Booker, the Harrier, the Mongoose and the Goat shunter, and Vanilla Vehicles Expanded's Tier 3 vehicles except the Springbok and the Louie. Older designs keep the heater and air conditioner by hand. An agent reading of each vehicle's era, for the owner to revise.

### Dependencies

- Needs D'thorga Framework 0.22.0 or later.

## [0.9.1] - 2026-11-08 - Draft

With D'thorga Framework 0.21.2. **Not yet tested in game.**

### Fixed

- The steam locomotives' cabs never warmed: the firebox now keeps the cab warm below 5 degrees while the locomotive is in steam, with a switch.

## [0.9.0] - 2026-11-08 - Draft

The Lightning as an electric van, with D'thorga Framework 0.20.0. **Not yet tested in game.** Design: docs/development/vehicle-electric-design.md.

### Added

- With D'thorga Framework's setting The Lightning runs on batteries (off by default, needs a restart), Vanilla Vehicles Expanded - Tier 3's Lightning runs on electricity: about 72 kWh, charged from a powered network with stored energy in about 10 hours, with the same range as before and 500 kg more weight. All figures authored.

### Dependencies

- Needs D'thorga Framework 0.20.0 or later.

### Save compatibility

- Turning the setting off again returns Lightnings to chemfuel on the next start. What a built Lightning's charge becomes then is Vehicle Framework's to decide and was not checked: empty its battery first if in doubt.

## [0.8.0] - 2026-11-08 - Draft

Empty masses for Vanilla Vehicles Expanded, with D'thorga Framework 0.19.0 (owner's choice, 8 November 2026). **Not yet tested in game.** Design: docs/development/vehicle-kerb-mass-check.md.

### Balance

- 33 Vanilla Vehicles Expanded vehicles weigh what a real vehicle of their class weighs, instead of 12 kg per Vehicle Framework Mass: the Dirtbike 110 kg instead of 360, the Bulldog 45 t instead of 5.4 t, the Trawler 8 t instead of 960 kg. Cargo now weighs on tanks and bikes as it should. All authored, approximate; three follow a real aircraft (Louie as a UH-1H, Cherokee as a CH-47D, Warbird as a P-51D, secondary sources).
- The Dirtbike and the Charley get a motorcycle's engine power (30 and 55 kW), since the light-car rule would have left a real-weight bike with 6.6 kW.

### Dependencies

- Needs D'thorga Framework 0.19.0 or later.

### Compatibility and limits

- The three orbital pods (Frog, Toad, Goliath) keep the general rule: no real vehicle to weigh them by.

### Save compatibility

- Safe to add and remove; the figures live in the vehicle designs, not in saves.

## [0.7.0] - 2026-11-08 - Draft

The cab, with D'thorga Framework 0.16.0. **Not yet tested in game.**

### Added

- Every vehicle named here gets the cab system: a closed cab with a heater and an air conditioner, and the night heater for caravans.
- Vanilla Vehicles Expanded's Dirtbike, Charley and Dinghy are marked open: they shelter nobody.

### Dependencies

- Needs D'thorga Framework 0.16.0 or later.

## [0.6.0] - 2026-11-08 - Draft

The owner's answers of 8 November 2026. **Not yet tested in game.**

### Balance

- The L-ATV's engine is now 370 hp (276 kW), the middle of the published 340 and 400 hp (owner's choice), up from 340.
- Water stops draw a tenth of the steam locomotives' real tender and tank capacities by default: 6,800 litres for the 4-8-4 and 496 for the tank engine. The realistic feed water setting restores 68,000 and 4,955.

### Changed

- The 4-8-4 carries no fusible plug, as US rules never required one, so with the boiler explosions setting on, letting its water run below the crown explodes the boiler. The tank engine keeps its plug.

### Dependencies

- Needs D'thorga Framework 0.14.0 or later.

## [0.5.0] - 2026-11-08 - Draft

Steam boilers: part 3 of the owner's approved proposals of 8 November 2026. **Not yet tested in game.**

### Added

- Trains of the Rim's Frisco 4-8-4 and Tommy 0-6-0T, chemfuel and wood alike, get a boiler: the Frisco with the 4500 class's 68,000 L tender and a big engine's slow start, the tank engine with the LB&SCR E2's 4,955 L of tank water and a quicker one. Their brake pipes are their brakes.

### Dependencies

- Needs D'thorga Framework 0.13.0 or later.

### Save compatibility

- Safe to add to and remove from a running colony: the locomotives gain or drop the boiler as their loadout says.

## [0.4.0] - 2026-11-08 - Draft

Real figures for named vehicles: part 2 of the owner's approved proposals of 8 November 2026, the L-ATV first. **Not yet tested in game.** Design: docs/development/vehicle-real-figures-design.md.

### Added

- GTek's L-ATV gets the real vehicle's engine power, the Allison 2500's six ratios, its weight, tyres and four-wheel drive, and its 740 A alternator. Its power was never published, so it uses the transmission's published ceiling, and says so.
- Real engine power, weights and gearboxes, where published, for the MaxxPro, the Humvee, the Land Cruiser J70, the T-54, the Wiesel, the M113 and M113(M), the M48H, the M10 Booker, the M2A3 Bradley, the four BMP-1s and both M1A2s.

### Changed

- RimThunder's "M1075 HEMTT" is the PLS truck, so it now has the PLS's engine, gearbox, weight and 260 A electrics.
- The Humvee's alternator now cites the Army manual (60 to 400 A at 28 V); it keeps 60 A.

### Compatibility and limits

- Unpublished figures (several gear ratios, some rated speeds, every torque curve) are authored and labelled. Combat weights include crew and ammunition the model adds again (small).

### Save compatibility

- Safe to add to and remove from a running colony: nothing new is saved.

## [0.3.0] - 2026-11-08 - Draft

Power and lift: the owner's approved proposals of 8 November 2026, part 1. **Not yet tested in game.**

### Added

- Vanilla Vehicles Expanded's Frog and Toad and Tier 3's Goliath fly on their own thrusters (three, four and four): each thruster lost takes lift away, and a pod that cannot lift its load will not launch. The Goliath is built heavy, as the mod describes it.
- The Mosquito and the Louie get a turboshaft and their rotor masts as a rotor drive; the Louie is the mod's nod to the Huey, and like the real UH-1H its transmission, not its engine, limits its lift.
- The Hind and the Mongoose gain a main rotor gearbox part and a rotor drive limit.
- The Cherokee's transmissions and rotor masts become its rotor drive, and its lift is checked before it launches.
- Trains of the Rim's Goat DE2 runs as a diesel-electric: no gearbox, traction motors that heat when pulling hard, rail wheels, and air brakes from its brake pipe with a compressor.

### Dependencies

- Needs D'thorga Framework 0.12.0 or later. Vanilla Vehicles Expanded's loadouts load only while it is active.

### Compatibility and limits

- The pods' lift figures are authored for fictional craft. The Louie's drive limit follows the UH-1H's transmission limit and the Mongoose's the A129's gearbox; the Cherokee's, the Hind's and the Mosquito's are authored, as no figure was found.
- The Harrier's engine figures are labelled as the Pegasus 11-21's, the early AV-8B's engine.

### Save compatibility

- Safe to add to and remove from a running colony: these vehicles gain or drop the new systems and an air compressor part as their loadouts say.

## [0.2.0] - 2026-11-08 - Draft

D'thorga Framework 0.11.0's engines, drivetrains and gas turbines, fitted to named vehicles. **Not yet tested in game.**

### Added

- The Harrier gets its Pegasus engine: lift ratings from maximum continuous to the short wet rating, a 227 litre water injection tank, the jump jet's flight phases and a hover check at its real empty mass. Too heavy or too hot to hover onto its pad, it uses a runway by default (the setting is D'thorga Framework's; the gate is D'thorga Patches').
- The Hind and the Mongoose get two turboshafts each, bound to their starboard and port engine parts, and the helicopter's flight phases.
- Vanilla Vehicles Expanded - Tier 3's Cherokee (its tandem-rotor transport) gets two turboshafts, bound to its left and right turbo shaft parts; it flies through Vehicle Framework's own launch, so its phases are taxiing and cruising.
- RimThunder's M1A2 and M1A2 SEP get the gas turbine system and a tracked turbine drivetrain: high fuel use at idle, a hot start on a weak battery.

### Dependencies

- Needs D'thorga Framework 0.11.0 or later. The Harrier, Hind, Mongoose and Cherokee loadouts load only while their mods are active.

### Compatibility and limits

- The turbine temperature limits are authored placeholders, labelled in the files. The M1A2's auxiliary power unit is not built (it needs a patch to RimThunder's upgrade trees).

### Save compatibility

- Safe to add to and remove from a running colony: these vehicles gain or drop the turbine and water systems as their loadout says. Nothing the engine model works out is saved.

## [0.1.0] - 2026-11-08 - Draft

The first version, a stand-in while D'thorga Framework's vehicle systems are proved (owner, 8 November 2026: the framework before
any vehicle of ours). No vehicles of its own yet. **Not yet tested in game.**

### Added

- Vanilla Vehicles Expanded - Tier 3's Big Rig and RimThunder's HEMTT get air brakes: after standing long, they idle a few minutes before they can drive.
- The Humvee runs a 24-volt system from two batteries, with a smaller charging output, as the real HMMWV does.
- RimThunder's M1A2 and M1A2 SEP have gas-turbine engines, so they get no coolant system.
- Trains of the Rim's steam locomotives keep no vehicle systems, since a boiler is not a car engine; its diesel shunter gets the general ones.

### Dependencies

- Needs Vehicle Framework and D'thorga Framework 0.9.0 or later. Each mod named above is optional: its vehicles' loadout loads only while it is active.

### Compatibility and limits

- RimWorld 1.7 only.
- Every vehicle not named here gets D'thorga Framework's general systems.

### Save compatibility

- Safe to add to and remove from a running colony: the named vehicles switch between their own systems and the general ones, and saved vehicles keep or drop the air compressor as their design does.
