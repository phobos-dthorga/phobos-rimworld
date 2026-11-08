# D'thorga Vehicles

Vehicles that keep running only while they are looked after. It adds one vehicle of its own, **the bulk tanker**, and gives the
vehicles of other mods you already drive the systems that match what they really are, on top of the general systems D'thorga
Framework gives every vehicle.

Version 0.14.0, built 9 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **New in 0.11.0, with Vanilla Expanded Framework (not yet tested in game): garages.** A convoy that has stopped can set up a garage on
  the world map. Vehicles left in one stay with no time limit, and whoever is stationed there mends them from steel and components
  you bring by caravan. **Before removing this mod, take every vehicle out of every garage**, or they go with it.
- **Needs Vehicle Framework and D'thorga Framework 0.22.0 or later**, on **RimWorld 1.7 only**. Load it after both; the About file
  asks for that, so RimSort does it for you.
- **The general systems come from D'thorga Framework**, not from this mod: battery and alternator, coolant and oil, tyres or tracks,
  brakes, gearbox, wear and service. Their guide and settings are in
  [D'thorga Framework's README](../PhobosFramework/README.md#vehicle-systems-with-vehicle-framework). This mod only changes the
  vehicles below.
- **Safe to add to a running colony. Safe to remove only if you have no bulk tanker:** scrap any tanker first, as with any Vehicle
  Framework vehicle. The other vehicles below switch between their own systems and the general ones; a saved vehicle keeps or drops the
  air compressor as its design does.
- **From 0.3.0, a laden helicopter or pod may refuse to lift off** on a hot day or with engines out; the launch button says
  why. D'thorga Framework's hover-limit setting can allow it at a cost or just warn.
- **Open vehicles shelter nobody** (0.7.0): on Vanilla Vehicles Expanded's Dirtbike, Charley and Dinghy the crew feels the weather;
  every other vehicle here has a closed cab with a heater and an air conditioner.
- **The Harrier, the Hind, the Mongoose and the Cherokee run gas turbines** from 0.2.0. A Harrier too heavy for its engine to hover in
  the day's heat **lands on a runway instead of its pad** by default (with D'thorga Patches); D'thorga Framework's settings offer
  "allow it, at a cost to the engine" and "warn only". **Its injection water needs refilling** after a hard hover: a mechanic does
  it, from plumbing beside the jet when Dubs Bad Hygiene is active.
- **Heavy trucks with air brakes must idle before they drive** after standing for hours. Draft one and wait a few minutes; the move
  order says so while the air builds.
- **Vanilla Vehicles Expanded's vehicles weigh what real ones of their kind weigh** (0.8.0): bikes are lighter and tanks far heavier
  than before, so cargo matters on a tank and hardly at all on a bike. Change any vehicle's weight in Vehicle Framework's settings
  window ("Empty mass").
- **The Lightning can run on batteries** (0.9.0): turn on **"The Lightning runs on batteries"** in D'thorga Framework's settings and
  restart. It then charges parked over a powered conduit, from the network's stored energy (about five batteries' worth for a full
  charge), instead of taking chemfuel.
- **Modern vehicles have climate control** (0.10.0): the L-ATV, MaxxPro, M1A2 SEP, M10 Booker, Harrier, Mongoose, Goat shunter and
  Vanilla Vehicles Expanded's Tier 3 (not the Springbok or the Louie) hold the cab near 21 °C by themselves.
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
| Cherokee (Vanilla Vehicles Expanded - Tier 3) | Two turboshafts, bound to its left and right turbo shaft parts; one out, the other carries the load and runs hot; its transmissions and rotor masts are its rotor drive; its lift is checked before launch | A tandem-rotor heavy lifter in the Chinook's mould, with an engine pod on each side |
| Louie (VVE - Tier 3) and Mosquito (Vanilla Vehicles Expanded) | One turboshaft each, the rotor mast as the rotor drive, lift checked before launch | The Louie is the mod's Huey nod (Huey, Dewey and Louie), and the UH-1H had one turboshaft |
| Frog and Toad (Vanilla Vehicles Expanded), Goliath (VVE - Tier 3) | Their own three, four and four thrusters as engines: each one lost takes lift away, and a pod that cannot lift its load will not launch | Thrust-lifted pods; the Goliath is built heavy, as the mod describes it |
| **L-ATV** (GTek's Vehicles) | 370 hp (276 kW), the middle of the published figures (its power was never officially published), Allison six-speed ratios, weight, tyres and four-wheel drive, and its 740 A alternator | Oshkosh's and Allison's published figures; the power is your choice |
| MaxxPro (GTek's Vehicles), Humvee, Land Cruiser J70, T-54 and Wiesel (Frontline Workshop), M113, M48H and the "HEMTT" (Roaring Tiger), M10 Booker and M2A3 Bradley (Desert Sabre), the BMP-1 family (no longer on the Workshop since 8 November 2026; the loadout waits, dormant, for anyone with a copy), both M1A2s | Their real engines' power, weights and gearboxes where published, each labelled in the files; the "HEMTT" is really the PLS, so it gets the PLS's figures | So each one climbs, labours and bogs like itself rather than like a generic diesel |
| Goat DE2 (Trains of the Rim) | Diesel-electric: no gearbox, traction motors that heat when pulling hard, rail wheels, air brakes from its brake pipe with a compressor | Its description says diesel-electric, and it has a traction motor and a brake pipe of its own |
| Steam locomotives (Trains of the Rim) | A boiler: hours to raise steam, a water glass, a tender or tanks to refill at a water stop, a fusible plug that melts if the water runs low | The Frisco follows the real 4500 class (a 68,000 L tender) and the tank engine the LB&SCR E2, the class Thomas was drawn from |

Every source and simplification is in the loadout files under `Mods/`.

## The bulk tanker

**Not yet tested in game.** A heavy truck, 3 by 10 cells, built with a bare deck.

1. **Fit a tank** in its Upgrades tab: a general, insulated or high-volume liquid tank, or a pressure vessel if Vanilla Gravship Expanded is active. Only one tank at a time.
2. **Choose what it carries** with its Choose contents button.
3. **Park it** on or beside the pipes, the stacks or the vehicle you want, then press **Set up**. It cannot move until you press Pack up.
4. Press **Load** or **Unload**. The inspect pane says what the pump is waiting for.

| It carries | Pumped to and from | Temperature |
| --- | --- | --- |
| Chemfuel | Other vehicles' fuel tanks, stacks on the ground, Vanilla Chemfuel Expanded's pipes | Thickens and pumps slowly in deep cold, only with the fuel setting on |
| Deepchem | Vanilla Chemfuel Expanded's pipes | As chemfuel |
| Water | Dubs Bad Hygiene's plumbing | Freezes below 0 °C and cannot be pumped until it thaws |
| Nutrient paste, hemogen | Their pipes | Above 0 °C the whole load spoils after about 18 hours; the insulated tank doubles that |
| Astrofuel, oxygen | Vanilla Gravship Expanded's pipes | Oxygen vents in a fire or great heat |
| Beer, insect jelly | Stacks on the ground | None |
| Wort | Stacks on the ground | The whole load spoils after 5 days above 0 °C |
| FSX (Combat Extended) | Stacks on the ground | None, but a hit on the tank may set off the load |
| Prometheum (Combat Extended) | Stacks on the ground | Pressure vessel only; any spill catches fire |
| Toxic sludge (Biotech) | Wastepacks on the ground | Dissolves after 8 days above 0 °C; a spill or a dissolved load pollutes the ground |
| Fecal sludge (Dubs Bad Hygiene) | Septic tanks and barrels | None. It is what it is |
| Napalm, crude oil (Rimefeller) | Stacks and barrels on the ground | None |

**Gotchas:** an empty tank must be **purged** before it can carry something else: select a colonist and right-click the truck (four in-game hours; it takes water where Dubs Bad Hygiene is active). **Taking a tank off with a load in it spills the load.** A damaged tank leaks, and a destroyed tank holding something flammable explodes unless you turn that off in D'thorga Framework's settings. A full tank slows the truck.

**Parts:** it is built from RimThunder's engine and running gear and Vanilla Vehicles Expanded's electrics when you have them, and from three parts of ours, made at a machining table, if you have neither.

## Coming later

More utility vehicles of our own, each with a part for every job and an upgrade tree of its own: a survey truck, a mobile drill rig, a
tipper hauler and a generator set ([the design](../../docs/development/utility-vehicles-design.md)).

## For modders

A loadout here is an XML file that names a vehicle and the systems and figures it gets. Write your own for any vehicle, in your own
mod: [vehicle systems for modders](../../docs/vehicle-systems-modding.md).
