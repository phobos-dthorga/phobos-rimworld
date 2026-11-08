# D'thorga Vehicles

Vehicles that keep running only while they are looked after. **This first version adds no vehicles of its own.** It gives the
vehicles of other mods you already drive the systems that match what they really are, on top of the general systems D'thorga
Framework gives every vehicle.

Version 0.2.0, built 9 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Needs Vehicle Framework and D'thorga Framework 0.11.0 or later**, on **RimWorld 1.7 only**. Load it after both; the About file
  asks for that, so RimSort does it for you.
- **The general systems come from D'thorga Framework**, not from this mod: battery and alternator, coolant and oil, tyres or tracks,
  brakes, gearbox, wear and service. Their guide and settings are in
  [D'thorga Framework's README](../PhobosFramework/README.md#vehicle-systems-with-vehicle-framework). This mod only changes the
  vehicles below.
- **Safe to add to and remove from a running colony.** The vehicles below switch between their own systems and the general ones;
  a saved vehicle keeps or drops the air compressor as its design does.
- **The Harrier, the Hind and the Mongoose run gas turbines** from this version. A Harrier too heavy for its engine to hover in
  the day's heat **lands on a runway instead of its pad** by default (with D'thorga Patches); D'thorga Framework's settings offer
  "allow it, at a cost to the engine" and "warn only". **Its injection water needs refilling** after a hard hover: a mechanic does
  it, from plumbing beside the jet when Dubs Bad Hygiene is active.
- **Heavy trucks with air brakes must idle before they drive** after standing for hours. Draft one and wait a few minutes; the move
  order says so while the air builds.
- **Not yet tested in game.** Everything is read from the mods' files and checked offline.

## What it changes

| Vehicle (mod) | What changes | Why |
| --- | --- | --- |
| Big Rig (Vanilla Vehicles Expanded - Tier 3) | Air brakes and an air compressor | Semi-trailer trucks brake with air held off by springs |
| M1075 HEMTT (RimThunder - Roaring Tiger) | Air brakes and an air compressor | The real HEMTT has air brakes (its US Army maintenance manual has the chapter) |
| Humvee (HUMVEE) | A 24-volt system from two batteries; about 60 A of charging | The real HMMWV's 24 V system (US Army ordnance training text); the charging figure is from owners' reports |
| M1A2 and M1A2 SEP (RimThunder) | No coolant system; a gas turbine on a tracked drivetrain, which burns hard even at idle | A gas-turbine engine has no radiator, and the AGT1500 idles at a high share of its full-power fuel flow |
| Harrier (Harrier VTOL Attack Aircraft) | A vectored-thrust turbofan with its real lift ratings, water injection, its own flight phases and a hover check at its real empty mass | The Pegasus engine's published ratings and water injection (All-Aero); the jet's empty mass (Pima Air and Space Museum) |
| Hind (Hind Helicopter Gunship) and Mongoose (Mongoose Attack Helicopter) | Two turboshafts, bound to their own engine parts; one out, the other carries the load and runs hot | Both are twin-engine helicopters with an engine part on each side |
| Steam locomotives (Trains of the Rim) | No vehicle systems | A boiler is not a car engine; a boiler system would be its own design |

Every source and simplification is in the loadout files under `Mods/`.

## Coming later

Utility vehicles of our own, each with a part for every job: a survey truck, a mobile drill rig, a tipper hauler and a generator
set first ([the design](../../docs/development/utility-vehicles-design.md)).

## For modders

A loadout here is an XML file that names a vehicle and the systems and figures it gets. Write your own for any vehicle, in your own
mod: [vehicle systems for modders](../../docs/vehicle-systems-modding.md).
