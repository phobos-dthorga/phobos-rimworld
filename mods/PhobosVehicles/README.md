# D'thorga Vehicles

Vehicles that keep running only while they are looked after. It adds working vehicles of its
own, the **Talus** tipper truck, **Ullage** bulk tanker, **Kelly** drill rig, **Datum** survey truck, **Busbar** generator set and
**Fathom** seabed crawler, and gives the vehicles of other mods you already drive the systems that match what they really are, on top
of the general systems D'thorga Framework gives every vehicle.

One of House D'thorga's mods (also spelt dthorga).

Version 0.51.1, built 13 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Three helicopters take a cargo hook, and so a sling, a fire bucket and the litter kit** (not yet tested in game): the Cherokee
  (11,800 kg), the Louie (1,814 kg) and the Hind (2,400 kg), each from a real helicopter's figure, marked approximate. The Hind flies
  across the map by itself, so it needs D'thorga Framework 0.102.0 or later, and it cannot fly water drop runs. The Mosquito and the
  Mongoose take no hook.

- **New in 0.41.0 (not yet tested in game): our vehicles are built from parts.** A vehicle asks for an engine, a transmission, a fuel
  tank, tyres and the rest. **Where RimThunder or Vanilla Vehicles Expanded is installed, it asks for their parts**; otherwise for ours,
  made at the vehicle parts bench. **Finish or cancel any vehicle being built before you update**, or its frame keeps what was
  delivered and then asks for the new list. To build from our own parts whatever is installed, set **Parts for building** in D'thorga
  Framework's settings. **Needs D'thorga Framework 0.95.0 or later.**
- **New in 0.50.0 (not yet tested in game): every upgrade on our vehicles costs what D'thorga Framework's fittings and tunes do**: several hours' work and much more steel and components, more on a heavier vehicle, under the same two settings, *Time to fit and tune* and *Materials to fit and tune*. Other mods' upgrades are unchanged. Taking an upgrade off refunds a share of what it cost when it was fitted.
- **New in 0.39.1 (not yet tested in game): our vehicles have names**, such as the Talus tipper truck, and a few upgrades and tunes
  take the trade's own words (greedy boards, RC hammer, stock tune, tall final drive). **Only the names you read changed**: saves,
  upgrades already fitted and other mods' patches are unaffected. What each name means is [below](#our-vehicles-names).
- **New in 0.27.0 (not yet tested in game): each truck has its own research, on the D'thorga tab,** and truck upgrades now need the research they name. **An existing colony gets a truck's project free, once, if it had already done the research that project replaces.** New colonies research them normally.
- **New in 0.11.0, with Vanilla Expanded Framework (not yet tested in game): garages.** A convoy that has stopped can set up a garage on
  the world map. Vehicles left in one stay with no time limit, and whoever is stationed there mends them from steel and components
  you bring by caravan. **Before removing this mod, take every vehicle out of every garage**, or they go with it.
- **Needs Vehicle Framework and D'thorga Framework 0.95.0 or later**, on **RimWorld 1.7 only**. Load it after both; the About file
  asks for that, so RimSort does it for you.
- **The general systems come from D'thorga Framework**, not from this mod: battery and alternator, coolant and oil, tyres or tracks,
  brakes, gearbox, wear and service. Their guide and settings are in
  [D'thorga Framework's README](../PhobosFramework/README.md#vehicle-systems-with-vehicle-framework). This mod only changes the
  vehicles below.
- **Safe to add to a running colony. Safe to remove only once none of our own vehicles is left:** scrap every tipper, tanker,
  drill rig, survey truck, generator set, seabed crawler and trailer first, as with any Vehicle Framework vehicle, or they go with the mod.
  Other mods' vehicles switch between their own systems and the general ones; a saved vehicle keeps or drops the
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
- **New in 0.49.0 (not yet seen in game): more of what you fit shows.** The trailer has its own picture and its greedy boards show; the
  tipper's loading grab shows folded behind its cab; and on the tanker, survey truck, drill rig and tipper a **recovery winch** shows on the
  front bumper and an **auxiliary fuel tank** between the wheels while each is fitted. Placements are first guesses, to be adjusted once seen.
- **New in 0.45.0 (not yet tested in game): the Pintle flatbed trailer.** Any land vehicle can tow it through D'thorga Framework's hitch
  (0.101.0 or later); see [below](#the-pintle-flatbed-trailer). **From Framework 0.104.0 it goes on journeys** with the vehicle towing it (not yet tested in game).
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
| Cherokee (Vanilla Vehicles Expanded - Tier 3) | Two turboshafts, bound to its left and right turbo shaft parts; one out, the other carries the load and runs hot; its transmissions and rotor masts are its rotor drive; its lift is checked before launch; from 0.40.0 a hook mount for a cargo hook rated at 11,800 kg, to carry a vehicle on a sling (not with Cherokee Sling active); from 0.45.0 (not yet tested in game) its upholstery takes D'thorga Framework's litter kit, and with a hook it can fly the Framework's water drops | A tandem-rotor heavy lifter in the Chinook's mould, with an engine pod on each side |
| Louie (VVE - Tier 3) and Mosquito (Vanilla Vehicles Expanded) | One turboshaft each, the rotor mast as the rotor drive, lift checked before launch | The Louie is the mod's Huey nod (Huey, Dewey and Louie), and the UH-1H had one turboshaft |
| Frog and Toad (Vanilla Vehicles Expanded), Goliath (VVE - Tier 3) | Their own three, four and four thrusters as engines: each one lost takes lift away, and a pod that cannot lift its load will not launch | Thrust-lifted pods; the Goliath is built heavy, as the mod describes it |
| **L-ATV** (GTek's Vehicles) | 370 hp (276 kW), the middle of the published figures (its power was never officially published), Allison six-speed ratios, weight, tyres and four-wheel drive, and its 740 A alternator | Oshkosh's and Allison's published figures; the power is your choice |
| MaxxPro (GTek's Vehicles), Humvee, Land Cruiser J70, T-54 and Wiesel (Frontline Workshop), M113, M48H and the "HEMTT" (Roaring Tiger), M10 Booker and M2A3 Bradley (Desert Sabre), the BMP-1 family (no longer on the Workshop since 8 November 2026; the loadout waits, dormant, for anyone with a copy), both M1A2s | Their real engines' power, weights and gearboxes where published, each labelled in the files; the "HEMTT" is really the PLS, so it gets the PLS's figures | So each one climbs, labours and bogs like itself rather than like a generic diesel |
| Goat DE2 (Trains of the Rim) | Diesel-electric: no gearbox, traction motors that heat when pulling hard, rail wheels, air brakes from its brake pipe with a compressor | Its description says diesel-electric, and it has a traction motor and a brake pipe of its own |
| Steam locomotives (Trains of the Rim) | A boiler: hours to raise steam, a water glass, a tender or tanks to refill at a water stop, a fusible plug that melts if the water runs low | The Frisco follows the real 4500 class (a 68,000 L tender) and the tank engine the LB&SCR E2, the class Thomas was drawn from |

Every source and simplification is in the loadout files under `Mods/`.

## How they sound (new in 0.35.0; not yet tested in game)

**Every vehicle here has its own voice**: its own engine start and stop, its own door or hatch, and an engine that follows the
drive, rising as it revs, dropping at each gear change and growling harder under load (an idling and a labouring loop, blended).
The tanker, tipper and drill rig hiss their air brakes as they stop; the trucks splutter when the tank runs dry. The generator truck's
engine falls quiet while it is set up, where its own running sound plays. D'thorga Framework's **D'thorga sounds volume** setting
turns them all down or off. All were made with generative AI on the owner's PC and picked by the owner
([how](../../docs/development/sound-generation.md)).

**Their work sounds too** (new in 0.39.0; not yet tested in game): the tanker's hoses, pump, relief valve, leaks and purges; the
tipper's rams, load and tailgate (rock, bulk and standard bodies each their own); the drill rig setting up, drilling (the RC hammer
and shore power each their own), a portion down the chute and winding down; the survey truck's boom, console, array, dish and a soft
tone for a find; the generator truck setting up; and the Crawler diving, surfacing, settling and lifting off. **Under the water,
only your own crew hears their boat**: the creaks, the air scrubber, the laboratory and a contact never give a hidden enemy boat away.

## Our vehicles' names

Each name is a real word from engineering or earth science that says something true about the job (owner's choice, 11 November
2026). The description in the game opens with its designation: the name, its first letter and a mark number, so a bigger tipper
would one day be the Talus T2, and a refit adds a letter.

| Vehicle | Designation | The word means |
| --- | --- | --- |
| Talus tipper truck | Talus T1 | The heap of broken rock at the foot of a cliff: what it leaves when it tips |
| Ullage bulk tanker | Ullage U1 | The empty space a tank must keep above its load, measured at every fill |
| Kelly drill rig | Kelly K1 | The square bar that turns the drill string on a rotary rig |
| Datum survey truck | Datum D1 | The reference point every survey measures from |
| Busbar generator set | Busbar B1 | The bar that carries power into a grid, which is what the set becomes once it is set up |
| Fathom seabed crawler | Fathom F1, refits F1-S (sensor) and F1-L (laboratory) | The sailor's depth unit of six feet; to fathom something is to get to the bottom of it |
| Pintle flatbed trailer | Pintle P1 | The hook-and-ring coupling a drawbar trailer hangs from |
| X-1 submarine test hull | X-1 | Experimental hulls are numbered in order: X-1, then X-2, X-3 |

## The Ullage bulk tanker

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

## The Busbar generator set

**Not yet tested in game.** A medium diesel-electric truck, 3 by 8 cells like the Talus tipper since 0.51.0 (it was 2 by 6): its engine turns a generator, electric motors drive the back axle, and when it is parked it can feed your grid.

- **Build it** from the **motor pool** tab of the Architect menu. It needs Machining and Electricity, and one engine assembly, drivetrain kit and electrical kit from the **vehicle parts bench** (D'thorga Framework).
- **Press Set up.** A cable end appears beside the truck; wire things to it, or touch a conduit to it. The truck cannot move until you press **Pack up**.
- **It gives 2,000 W at full health** (1,200 W before 0.51.0). Fuel follows what the grid draws, from 2 chemfuel a day doing nothing to 15 a day flat out, or 15 a day whenever batteries on the grid have room. That is 60% of the game's chemfuel generator's economy: you are paying for being able to drive it away.
- **It stops if** the tank runs dry, the engine or the traction generator is destroyed, or damage takes it under 400 W. Its inspect pane says which.
- **Upgrades:** an uprated alternator (2,650 W), a cable reel (you pick where the cable end goes, up to 12 cells away), a battery buffer that keeps the grid alive for a time when the engine cannot, a heavy-duty fan that cools a parked engine as well as driving does, for hard work in hot weather, and a **service lead** (new in 0.45.0, not yet tested in game): with the set set up and running, **Run the service lead** plugs it into one of your vehicles within 6 cells to charge its battery, or start it at once if it is too flat, taking 300 W before the camp's grid.
- **New in 0.42.0 (not yet tested in game), an exhaust heat exchanger:** while the set makes power it warms the room its cable end stands in, up to 21 C, more as more power is drawn. **Put the cable end inside the room you want warm**; outdoors the heat is wasted. It costs 25 W of output.
- **A vehicle of ours parked beside the cable end or a conduit on its grid counts as plugged in**, as with any grid. Vehicle Framework's own electric vehicles need a battery on the grid to charge.
- **You can hear it running** (new in 0.31.0): while it is set up and its engine runs, it sounds like the game's chemfuel generator, from the truck, and falls quiet when it stops. D'thorga Framework's **D'thorga sounds volume** setting turns it down or off.
- **If you remove the mod with one set up,** pack it up first, or a cable end is left behind that does nothing.

## The Datum survey truck

**Tested in game by the owner on 9 November 2026 (Vehicles 0.23.0): the truck and its boom work well. Its upgrades are not yet tested in game.** A light four-wheel-drive truck that does a ground-penetrating scanner's job, wherever you can drive it.

- **Build it** from the **motor pool** tab. It needs Ground-penetrating scanner research, our four vehicle parts from the **vehicle parts bench**, and 10 uranium.
- **It needs room beside it.** When it sets up, its boom swings out to whichever side is clear, about three cells from the middle of the truck. If walls, buildings or other vehicles block both sides, it says so and will not set up.
- **Park it outdoors and press Set up.** A colonist on **Research** work then runs the survey, with their research speed. The engine idles while they work, at 8 chemfuel a day.
- **Finds are the game's own deposits,** with the game's ore odds and sizes. Select the set-up truck to see them on the ground.
- **A first find takes about 7 hours on average**, ten times quicker than the game's scanner; **each find on a map then takes longer than the last:** one and a half times, up to eight times.
- **Plugged in costs no fuel:** with a powered conduit or generator within 6 cells of its small power point, it runs off the grid at 700 W. The setting **Power points have the larger socket** (off) changes only that point's picture, for the next one a truck sets out.
- **With Rimefeller, a set-up survey truck counts as the scanner a deep well needs. With Dubs Bad Hygiene, a groundwater sensor** shows the water while the truck is set up and selected, and marks the richest spot within 15 cells.
- **Upgrades:** a geophone array (more steel) or a radiometric probe (more uranium, costs 40), not both; a long-range array that searches other lands for one ore, off the map's slowing; a powered array (three times as fast, 1,000 W, needs a grid or the battery); a large battery (runs with the engine off, and the engine starts itself to recharge it at 30%, **burning fuel with nobody at the wheel**); work lights.

## The Kelly drill rig

**Not yet tested in game.** A heavy truck carrying a deep drill.

- **Build it** from the **motor pool** tab, after Deep drilling research.
- **It drills the cell behind its rear.** Reverse it up to a deposit a scanner has found and press **Set up**; a ring shows its reach. A colonist on **Mining** work then drills, with the game's portions and their Mining skill, and what comes up lands beside the hole.
- **A bare rig drills at ten times a deep drill:** a steel lump that takes a drill a month takes the rig about three days. A diamond core bit doubles that to twenty times. An RC (reverse-circulation) hammer is eighteen times on ore and thirty once only stone is left. It needs both, not one: pick by the job.
- **It burns 40 chemfuel a day while drilling,** unless you fit the electric power pack and have a powered grid within 6 cells.
- **Active drilling can bring insects,** by the game's own rule and your storyteller's odds, **and a faster rig meets them sooner:** the risk per lump is the same as a deep drill's, so speed is no way round them. Keep guards near it.
- **At night, with no work lights, it drills at 80%** of its rate.
- **Overdrive** (also on the survey truck): a toggle that doubles the pace while the engine runs flat out, fuel goes 2.5 times as fast and the working parts wear. It switches itself off at half health on any working part or an overheating engine.
- **With Rimefeller, the oil well kit** sinks a well at the hole when no ore is in reach, using steel from the rig's cargo and needing what a derrick needs (a working console, or a scanner or set-up survey truck for deep oil). It leaves a well head: **build a derrick over it to pump.**
- **With Vanilla Chemfuel Expanded, the deepchem head** raises deepchem into a 300-unit buffer at ten times a pumpjack's rate, though the buffer and the tanker's pump set the real pace. **Without the head, the rig leaves deepchem in the ground.** To empty the buffer, park a tanker with a deepchem tank within reach of its hose, set up and loading, or lay a deepchem pipe up to the rig. It never comes up as items.

## The Talus tipper truck

**Not yet tested in game.** A heavy truck that carries 2,400 kg and puts it all down in one go.

- **Build it** from the **motor pool** tab, after Machining and Electricity.
- **Load it** through its ordinary cargo window: 96 granite chunks, or 4,800 steel.
- **With a driver aboard and stopped, press Tip.** Everything in its cargo lands inside the outlined half-circle behind it, and **that includes any spare parts you loaded**. Whatever will not fit stays aboard, and it tells you how many.
- **Upgrades:** greedy boards (taller sides, +600 kg), either a rock body (tips half as fast again) or a bulk body (3,600 kg, tips slower), and a **loading grab** (new in 0.45.0, not yet tested in game; from 0.49.0 it shows folded behind the cab): with a driver aboard and the truck stopped, **Work the grab** lifts loose chunks and stacks within 4 cells into the cargo, up to 500 kg a lift, with nobody carrying. Things in a stockpile or on a shelf are left; it cannot tip while the grab is out.

## The Pintle flatbed trailer

**Not yet tested in game.** A two-axle flatbed with low sides and a drawbar: 2,000 kg empty, 3,000 kg of cargo.

- **Build it** from the **motor pool** tab, after vehicle parts research.
- **Load it** through its cargo tab while it stands unhitched.
- **To tow it**, select a vehicle, press **Hitch a trailer** and pick it; a colonist couples it and it is set behind the vehicle. **Unhitch**
  leaves it where it stands, loaded, for later. A broken drawbar has to be repaired before it can be hitched.
- **Its weight is the towing vehicle's to pull**: a heavy load slows a small truck, and on soft ground or a hill only the vehicle's own
  wheels grip.
- **Upgrade:** greedy boards (+600 kg), which show on the trailer as taller side boards.
- **Its own picture since 0.49.0** (not yet seen in game): a sand-grey drawbar flatbed with a dark plank deck; repaint it as any vehicle.

## The Fathom seabed crawler (not yet tested in game)

**A submarine of 4 by 9 cells, built in shallow water. Research Submersible hull on the D'thorga: Submarines tab first.** It has its own picture since 0.38.0, a grey-blue hull with an orange band on the sail; repaint it as any vehicle. Bare, it can only dive, hide and come home. **It grew from 3 by 8 to 4 by 9 in 0.36.0**, so a Crawler placed by an earlier version keeps its old shape in the save: destroy it in dev mode and place a fresh one. Each upgrade takes its own research and its own part from the parts bench:

- **Bottoming wheels** let it rest on the bottom, where it uses half the air and charge and is far harder to find. It must lift off before it can move.
- **Air scrubber** gives six more hours under, and a crew told to stay down past empty suffers more slowly.
- **Wet laboratory** adds two scientist's seats. **Put colonists with an Intellectual skill of 7 or more in them** and they carry on the colony's current research project, at three quarters of a simple research bench's speed while the boat is still and half that while it moves. They need to be awake and on a work shift. It takes any project that needs no particular bench. The boat's inspect panel says who is at work, or why nobody is.

- **Advanced laboratory** comes after the wet laboratory and is made from one analyser bench. It takes on anything a hi-tech research bench can, at a hi-tech bench's speed.

**Research done only at sea.** Its research, **advanced marine science**, can only be done by scientists seated in a laboratory aboard. **Choose it in the research tab as usual: the benches at home keep working on the colony's own project**, and both show as in progress. If you use Nice Research Tab, choose it directly instead of queueing it ahead of other projects, or the queue waits for it.

**When watchers spot it under water, a soft ping sounds at the hull** (new in 0.31.0; the test hull too since 0.34.0), with the message saying it has been seen. D'thorga Framework's **D'thorga sounds volume** setting turns it down or off.

- **Listening array** (new in 0.32.0; research Hydrophones, one hydrophone array) hears hostile movement within 40 cells while the boat is still and 20 while it moves. Things on land are heard at half those distances. It finds other boats under the water by their noise. The inspect panel says how many are moving and where the nearest is, and **warns when the nearest is close enough to spot the boat**. Select the boat to see its range and a line to each contact. It never gives the boat away. Damage to the hydrophones at the bow shortens what it hears.

**A fire on the Crawler goes out once it is fully under** (new in 0.32.0), so diving is also the way to put out a burning boat.

- **Sonar set** (new in 0.38.0; research Active sonar, after Hydrophones; one sonar transducer) adds a **Sonar ping** button. **A ping finds everything hostile in the water within 60 cells, moving or still, and every boat under the water**, though one resting on the bottom only from half as far. **But every ping gives you away:** under the water the boat is followed for a minute afterwards, as if spotted, and anyone in the water hears the ping from twice as far. It needs 30 seconds between pings and someone awake aboard. Damage to the transducer under the bow shortens it.
- **Enemy pings are heard without a set of your own:** any of your boats in the water is told which direction a ping came from.

**Two hull refits, new in 0.38.0. Choose one: they exclude each other**, and each gives the Crawler a different hull picture. To change your mind, remove one in the Upgrades tab and fit the other.

- **Sensor refit**, the F1-S (research Faired sensor sail, after Active sonar; one faired sensor sail): a smooth faired sail. The listening array hears 20 cells further and the sonar reaches 30 further, but it carries 200 less.
- **Laboratory refit**, the F1-L (research Laboratory hull section, after Advanced marine science; one laboratory hull section; needs the advanced laboratory fitted): a longer hull. A third scientist's seat and faster research, but a little slower in the water.

More upgrades are designed, including radar, and are not built yet. The bottoming wheels, listening array and sonar dome show on the hull as they are fitted.

## The X-1 submarine test hull

**Not a finished vehicle, and it cannot be built.** In dev mode, the debug action "Vehicle systems: spawn a submersible" places one, crewed, for trying out D'thorga Framework's diving, in deep or chest-deep water. It wears the Crawler's base hull since 0.38.0 and is the Crawler's size, 4 by 9 cells since 0.36.0. Not yet tested in game.

## How the trucks work together

The survey truck finds it, the drill rig brings it up, the tipper hauls the ore, the tanker carries the fluids and the generator set powers the camp ([the design](../../docs/development/utility-vehicles-design.md)).

## For modders

A loadout here is an XML file that names a vehicle and the systems and figures it gets. Write your own for any vehicle, in your own
mod: [vehicle systems for modders](../../docs/vehicle-systems-modding.md).
