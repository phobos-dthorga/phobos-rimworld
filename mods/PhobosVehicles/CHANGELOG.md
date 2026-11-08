# D'thorga Vehicles changelog

Maintained from 8 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

## [0.7.0] - 2026-11-09 - Draft

The cab, with D'thorga Framework 0.16.0. **Not yet tested in game.**

### Added

- Every vehicle named here gets the cab system: a closed cab with a heater and an air conditioner, and the night heater for caravans.
- Vanilla Vehicles Expanded's Dirtbike, Charley and Dinghy are marked open: they shelter nobody.

### Dependencies

- Needs D'thorga Framework 0.16.0 or later.

## [0.6.0] - 2026-11-09 - Draft

The owner's answers of 9 November 2026. **Not yet tested in game.**

### Balance

- The L-ATV's engine is now 370 hp (276 kW), the middle of the published 340 and 400 hp (owner's choice), up from 340.
- Water stops draw a tenth of the steam locomotives' real tender and tank capacities by default: 6,800 litres for the 4-8-4 and 496 for the tank engine. The realistic feed water setting restores 68,000 and 4,955.

### Changed

- The 4-8-4 carries no fusible plug, as US rules never required one, so with the boiler explosions setting on, letting its water run below the crown explodes the boiler. The tank engine keeps its plug.

### Dependencies

- Needs D'thorga Framework 0.14.0 or later.

## [0.5.0] - 2026-11-09 - Draft

Steam boilers: part 3 of the owner's approved proposals of 9 November 2026. **Not yet tested in game.**

### Added

- Trains of the Rim's Frisco 4-8-4 and Tommy 0-6-0T, chemfuel and wood alike, get a boiler: the Frisco with the 4500 class's 68,000 L tender and a big engine's slow start, the tank engine with the LB&SCR E2's 4,955 L of tank water and a quicker one. Their brake pipes are their brakes.

### Dependencies

- Needs D'thorga Framework 0.13.0 or later.

### Save compatibility

- Safe to add to and remove from a running colony: the locomotives gain or drop the boiler as their loadout says.

## [0.4.0] - 2026-11-09 - Draft

Real figures for named vehicles: part 2 of the owner's approved proposals of 9 November 2026, the L-ATV first. **Not yet tested in game.** Design: docs/development/vehicle-real-figures-design.md.

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

## [0.3.0] - 2026-11-09 - Draft

Power and lift: the owner's approved proposals of 9 November 2026, part 1. **Not yet tested in game.**

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

## [0.2.0] - 2026-11-09 - Draft

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
