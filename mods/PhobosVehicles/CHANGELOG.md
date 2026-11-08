# D'thorga Vehicles changelog

Maintained from 8 November 2026. Draft entries record preparation, not Steam publication; nothing is published yet. Not yet
tested in game unless an entry says the owner tested it.

## [Unreleased]

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
