# D'thorga Vehicles

Vehicles that keep running only while they are looked after. It adds one vehicle of its own, **the bulk tanker**, and gives the
vehicles of other mods you already drive the systems that match what they really are, on top of the general systems D'thorga
Framework gives every vehicle.

Version 0.26.0, built 10 November 2026 for House D'thorga. **Not yet tested in game.**

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

**Not yet tested in game.** A heavy truck, 3 by 10 cells, built with a bare deck. **Anything of ours that sets up can have its engine started while it stands, but cannot be driven until it packs up** (D'thorga Framework 0.54.0).

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

**Compartments:** two upgrades divide a liquid tank into two, then three compartments, so it can carry different fluids at once. They share the capacity equally; filled alike they hold as much as the whole tank. Use the **Compartment** button to choose which one the pump and the contents button work on. **Food can't share a tank with anything else**, and the contents menu says so. A setting in D'thorga Framework gives every liquid tank its compartments for free.

**Air brakes:** like the other heavy trucks, it must idle a few minutes to build air after standing for hours.

**Bulk buying:** take the tanker in a caravan to a settlement, pick the compartment, and press **Bulk order**. Choose how much with the same number box and arrows as the trade window. You pay the game's own price, with the silver in the caravan. What is on offer depends on what that settlement trades in, and the same fluid can't be bought there again for 15 days. Only fluids that exist as items can be bought. You can't do this from home: visiting traders don't sell by the tanker-load.

**Parts:** it is built from RimThunder's engine and running gear and Vanilla Vehicles Expanded's electrics when you have them, and from three parts of ours, made at a machining table, if you have neither.

## The generator set

**Not yet tested in game. The pictures are stand-ins.** A small diesel-electric truck: its engine turns a generator, electric motors drive the back axle, and when it is parked it can feed your grid.

- **Build it** from the **motor pool** tab of the Architect menu. It needs Machining and Electricity, and one engine assembly, drivetrain kit and electrical kit from the **vehicle parts bench** (D'thorga Framework).
- **Press Set up.** A cable end appears beside the truck; wire things to it, or touch a conduit to it. The truck cannot move until you press **Pack up**.
- **It gives 1,200 W at full health.** Fuel follows what the grid draws, from 1.5 chemfuel a day doing nothing to 9 a day flat out, or 9 a day whenever batteries on the grid have room. That is 60% of the game's chemfuel generator's economy: you are paying for being able to drive it away.
- **It stops if** the tank runs dry, the engine or the traction generator is destroyed, or damage takes it under 240 W. Its inspect pane says which.
- **Upgrades:** a larger generator (1,600 W), a cable reel (you pick where the cable end goes, up to 12 cells away), and a battery buffer that keeps the grid alive for a time when the engine cannot.
- **A vehicle of ours parked beside the cable end or a conduit on its grid counts as plugged in**, as with any grid. Vehicle Framework's own electric vehicles need a battery on the grid to charge.
- **If you remove the mod with one set up,** pack it up first, or a cable end is left behind that does nothing.

## The survey truck

**Tested in game by the owner on 9 November 2026 (Vehicles 0.23.0): the truck and its boom work well. Its upgrades are not yet tested in game.** A light four-wheel-drive truck that does a ground-penetrating scanner's job, wherever you can drive it.

- **Build it** from the **motor pool** tab. It needs Ground-penetrating scanner research, our four vehicle parts from the **vehicle parts bench**, and 10 uranium.
- **It needs room beside it.** When it sets up, its boom swings out to whichever side is clear, about three cells from the middle of the truck. If walls, buildings or other vehicles block both sides, it says so and will not set up.
- **Park it outdoors and press Set up.** A colonist on **Research** work then runs the survey, with their research speed. The engine idles while they work, at 8 chemfuel a day.
- **Finds are the game's own deposits,** with the game's ore odds and sizes. Select the set-up truck to see them on the ground.
- **A first find takes about 7 hours on average**, ten times quicker than the game's scanner; **each find on a map then takes longer than the last:** one and a half times, up to eight times.
- **Plugged in costs no fuel:** with a powered conduit or generator within 6 cells of its small power point, it runs off the grid at 700 W. The setting **Power points have the larger socket** (off) changes only that point's picture, for the next one a truck sets out.
- **With Rimefeller, a set-up survey truck counts as the scanner a deep well needs. With Dubs Bad Hygiene, a groundwater sensor** shows the water while the truck is set up and selected, and marks the richest spot within 15 cells.
- **Upgrades:** a geophone array (more steel) or a radiometric probe (more uranium, costs 40), not both; a long-range array that searches other lands for one ore, off the map's slowing; a powered array (three times as fast, 1,000 W, needs a grid or the battery); a large battery (runs with the engine off, and the engine starts itself to recharge it at 30%, **burning fuel with nobody at the wheel**); work lights.

## The drill rig

**Not yet tested in game. It looks like the bulk tanker until it has its own picture.** A heavy truck carrying a deep drill.

- **Build it** from the **motor pool** tab, after Deep drilling research.
- **It drills the cell behind its rear.** Reverse it up to a deposit a scanner has found and press **Set up**; a ring shows its reach. A colonist on **Mining** work then drills, with the game's portions and their Mining skill, and what comes up lands beside the hole.
- **A bare rig drills at ten times a deep drill:** a steel lump that takes a drill a month takes the rig about three days. A diamond bit doubles that to twenty times. A hammer bit is eighteen times on ore and thirty once only stone is left. It needs both, not one: pick by the job.
- **It burns 40 chemfuel a day while drilling,** unless you fit the electric power pack and have a powered grid within 6 cells.
- **Active drilling can bring insects,** by the game's own rule and your storyteller's odds, **and a faster rig meets them sooner:** the risk per lump is the same as a deep drill's, so speed is no way round them. Keep guards near it.
- **At night, with no work lights, it drills at 80%** of its rate.
- **Overdrive** (also on the survey truck): a toggle that doubles the pace while the engine runs flat out, fuel goes 2.5 times as fast and the working parts wear. It switches itself off at half health on any working part or an overheating engine.
- **With Rimefeller, the oil well kit** sinks a well at the hole when no ore is in reach, using steel from the rig's cargo and needing what a derrick needs (a working console, or a scanner or set-up survey truck for deep oil). It leaves a well head: **build a derrick over it to pump.**
- **With Vanilla Chemfuel Expanded, the deepchem head** raises deepchem into a 300-unit buffer at ten times a pumpjack's rate, though the buffer and the tanker's pump set the real pace. **Without the head, the rig leaves deepchem in the ground.** To empty the buffer, park a tanker with a deepchem tank within reach of its hose, set up and loading, or lay a deepchem pipe up to the rig. It never comes up as items.

## The tipper truck

**Not yet tested in game. It looks like the bulk tanker until it has its own picture.** A heavy truck that carries 2,400 kg and puts it all down in one go.

- **Build it** from the **motor pool** tab, after Machining and Electricity.
- **Load it** through its ordinary cargo window: 96 granite chunks, or 4,800 steel.
- **With a driver aboard and stopped, press Tip.** Everything in its cargo lands inside the outlined half-circle behind it, and **that includes any spare parts you loaded**. Whatever will not fit stays aboard, and it tells you how many.
- **Upgrades:** high sides (+600 kg), and either a rock body (tips half as fast again) or a bulk body (3,600 kg, tips slower).

## The submarine test hull

**Not a finished vehicle, and it cannot be built.** In dev mode, the debug action "Vehicle systems: spawn a submersible" places one, crewed, for trying out D'thorga Framework's diving, in deep or chest-deep water. It borrows the bulk tanker's picture, so it looks like a truck on the water. Not yet tested in game.

## How the trucks work together

The survey truck finds it, the drill rig brings it up, the tipper hauls the ore, the tanker carries the fluids and the generator set powers the camp ([the design](../../docs/development/utility-vehicles-design.md)).

## For modders

A loadout here is an XML file that names a vehicle and the systems and figures it gets. Write your own for any vehicle, in your own
mod: [vehicle systems for modders](../../docs/vehicle-systems-modding.md).
