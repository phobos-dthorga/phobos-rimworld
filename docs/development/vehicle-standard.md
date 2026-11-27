# The vehicle standard: what every kind of vehicle shows on Systems, Fittings and Health (27 Nov 2026)

**Status (27 Nov 2026):** the standard, and its first batch. **Batch 1 is built** (VS1 to VS3, radios everywhere): Radio Framework
0.49.0 and D'thorga Framework 0.356.0 ([as built](#built-batch-1-27-nov-2026)); not yet tested in game (rows 710 and 711). **Batch 2 is
built** (the audit, and the gaps it found): D'thorga Framework 0.359.0, D'thorga Vehicles 0.130.2 and Radio Framework 0.52.0 ([as
built](#built-batch-2-27-nov-2026)); not yet tested in game (row 723). **Batch 3 is built** (each aircraft engine's own fuel and oil):
D'thorga Framework 0.360.0 ([as built](#built-batch-3-27-nov-2026)); not yet tested in game (row 724). **Batch 4 is built** (lights,
searchlights, night landings and galleys): D'thorga Framework 0.366.0 and D'thorga Vehicles 0.132.0 ([as
built](#built-batch-4-28-nov-2026)); not yet tested in game (rows 743 to 745). **Batch 5 is built** (what does not belong): D'thorga
Framework 0.368.0 ([as built](#built-batch-5-28-nov-2026)); not yet tested in game (row 747). Every batch is built; VS8 and VS12 wait for a
behaviour. The owner answered fifteen cards ([below](#questions-for-the-owner)).

**Why.** The owner, 27 Nov 2026, looking at their aircraft: "Wouldn't radios of some sort be included with the majority of the ones in
my game, if not all, by default? And if that's the case, howcome there's no mention of them on the Systems, Fittings, nor Health pages?
There at least is for Vehicles with all three." And: "there's bound to be A WHOLE LOT MORE missing than just this. We need to bring
everything in-line to some sort of standard so players aren't left bewildered and confused."

## Read this first

- **The facts come from the game itself**: D'thorga Framework's bindings report (`VehicleBindings.json`, written at the owner's last
  start, 27 Nov 2026 10:19 UTC), which lists every vehicle's systems, part roles and parts as the game really bound them; read with
  `python scripts/vehicle-bindings.py` and a short comparison by kind (below). **95 vehicles**: 59 land, 10 fixed-wing, 10 rotor (the
  Osprey and C-27J counted with the helicopters by their rotor drive), 4 boats, 12 trailers.
- **No vehicle comes with a radio.** Not one: every radio is a fitting the player adds. A real tank, truck or helicopter of the kind in
  the owner's game left the factory with one.
- **Aircraft and boats have no radio bays at all.** The voice radio, repeater, Morse key, remote control kit and route kit bays are
  appended to the land base only (`Loadouts_Default.xml`), so on an aircraft or a boat there is nothing on Fittings to fit and nothing on
  Health to hit.
- **No system speaks for a radio anywhere, land included.** On a land vehicle the radio appears only as its bays (Fittings) and their
  parts (Health); nothing on the Systems page says whether it has a radio, whether it is powered, or whether it is in touch.
- **Aircraft miss systems every real aircraft has**: a fuel system (pumps), an oil system for the engines, brakes on the landing gear,
  and lights. Boats miss radios and lights.
- **The land base gives everything to everything**: an M1A2 Abrams has a stereo slot, a fridge bay, a solar mount and a sleeper berth.
  The standard says what does **not** belong, as well as what is missing.
- **The schema check already says so for aircraft**: `scripts/check-vehicle-schemas.py` warns 68 times that a turbine aircraft's service
  system reads a radiator and a belt it does not have (they read as sound). That is VS11.
- **Submarines are left out** (the owner's back burner); the X-1 and the seabed crawler keep what they have.

## What each kind has today

From the report: the systems and part roles on at least 80 % of land vehicles but under half of another kind, and the reverse.

| Kind | Has that land lacks | Lacks that land has |
| --- | --- | --- |
| Fixed-wing (10) | Avionics, flight controls; avionics, autopilot, sensor, radar, ECM pod, ferry tank, uncrewed kit and oxygen bays | **Fuel**, **oil**, brakes and wheels, cooling, transmission and turbo systems; **every radio bay**; lamp and cab light mounts; fridge, water tank, outlet panel, stereo, house battery, solar, roof, side rail and recovery mounts |
| Rotor (10) | The same, plus rotor drive, turbine, hydraulics and compressor wash | The same as fixed-wing |
| Boats (4) | Nothing | Brakes and wheels (rightly); **every radio bay**; lamp and cab lights; fridge, water tank, outlet panel, stereo and the rest |
| Trailers (12) | Nothing | Engine, electrics and the rest (rightly: unpowered); every radio bay (rightly) |

## The standard

Each line says what every vehicle of a kind shows on each page, why a real one has it, and whether we have it. **A line is built only
where it changes an outcome** (the vehicles' rule: every node moves a figure that changes an outcome), so each has its behaviour named.

| # | What | Land | Fixed-wing | Rotor | Boat | Trailer | Real basis | Behaviour it needs |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| VS1 | **A comms system on Systems**: radio fitted or not, powered, in touch with home or not, jammed | Missing | Missing | Missing | Missing | No (unpowered) | Every crewed vehicle has a radio panel and a working check | Reads the radio net: no new rule |
| VS2 | **Radio bays on Fittings and their parts on Health**: voice radio and repeater | Has | **Missing** | **Missing** | **Missing** | No | Aircraft carry VHF and UHF sets; boats a marine VHF | The bays exist; append them to the other bases |
| VS3 | **A radio fitted from the factory** | Missing | **Missing** | **Missing** | Missing | No | Military vehicles and every aircraft leave the factory with radios; a civilian truck may not | A factory-fitted mechanism (none exists today), once per vehicle |
| VS4 | The Morse key bay | Has | Not needed | Not needed | Has? | No | Aircraft stopped carrying keys decades ago; ships kept them longest | Leave aircraft without it |
| VS5 | The remote control and route kit bays | Has | Has an equivalent (uncrewed kit) | Same | Missing | No | Remote boats exist; aircraft use their autopilot | The uncrewed kit asks a real link: slice 3's EW9 |
| VS6 | **A fuel system** (pumps, a fuel pressure reading) | Has | **Missing** | **Missing** | Has | No | Boost pumps in every turbine aircraft's tanks | Pump failure: the engines starve on a steep climb or a long flight |
| VS7 | **An oil system** | Has | **Missing** | **Missing** | Has | No | Turbines have oil pumps, coolers and chip detectors | Oil loss: engine wear, then a shutdown |
| VS8 | Brakes on the landing gear | Has (as brakes) | **Missing** | Missing (skids on some) | No | Some | Wheel brakes on every wheeled aircraft | A landing roll; a parked aircraft rolling |
| VS9 | **Lights**: lamps, landing lights, a searchlight | Has | **Missing** | **Missing** | **Missing** | No | Landing lights; helicopters' searchlights; navigation lights | The lamp's own behaviour (night work), and night landings |
| VS10 | A galley, water and outlets on transports | Has (all) | Missing on the C-17 | Missing on the Osprey and C-27J | Missing on the trawler | Kitchen trailer has | Transport aircraft and working boats have galleys | The accessories' own behaviour |
| VS11 | **What does not belong**: a stereo, fridge, solar panel and sleeper berth on a main battle tank; a radiator and belt in a turbine's service list | Too much | Service roles too wide | Same | Too wide | — | A tank has an intercom, not a stereo | Narrow the land base by vehicle class |
| VS12 | A crew intercom | Missing | Missing | Missing | Missing | No | Every armoured vehicle and aircraft | Only if it changes something (crew working a remote gun, say): set aside until it does |

## How the standard is kept

- **Run `python scripts/vehicle-bindings.py`** after a game start (built in batch 2, the owner's card): its **Below the standard** section
  names every vehicle that lacks a system or part role a built line asks of its kind, and **Still to come** counts what each planned line
  will reach. `config/vehicle-standard.json` is the standard as data: the kinds (first match wins), the lines (built or planned, by kind
  or by named vehicle, some only for powered vehicles), and exceptions with their reasons. It reads the report the game writes at each
  start, so it checks what the game really did: **a fix shows there only after the game has started with it**.
- **The standard is data**, so the next game's vehicles (Kenshi has none; a future one may) are a new file, not new code.

## Batches

| Batch | Lines | Why in this order |
| --- | --- | --- |
| 1 | VS1, VS2, VS3 (radios everywhere) | The owner's question; it is also slice 3 of electronic warfare (radio bays on aircraft, the flying relay) |
| 2 | The audit kind and `config/vehicle-standard.json`. **Built 27 Nov 2026** | So every later batch is checked, and nothing slips back |
| 3 | VS6, VS7 (fuel and oil for aircraft). **Built 27 Nov 2026** | Real systems every aircraft has, with clear failures |
| 4 | VS9, VS10 (lights and transports' galleys). **Built 28 Nov 2026** | Accessories that already exist; mostly XML (the searchlight, night landings and the galley's cooking became code) |
| 5 | VS11 (what does not belong). **Built 28 Nov 2026** | Takes things away from players' vehicles, so it waits for the owner's word on each (four cards) |
| Later | VS8, VS12 | Each needs a behaviour designed first |

## Built: batch 1 (27 Nov 2026)

On the owner's cards of the same day. **Not yet tested in game**: rows 710 and 711 of the [test pass](in-game-test-pass.md).

| Line | As built |
| --- | --- |
| VS1, Comms | `PhobosVehicleSystem_Comms`, shipped with the Radio Framework (its Vehicle Framework folder) and brought by the Electrical system's `comesWith` (a patch), so every vehicle with a battery has it whatever its loadout writes, and a game without the radio has none. Two readings, from D'thorga Framework's new `Input_Comms` through a seam (`VehicleComms`) the radio's tie-in answers: **radio** (none fitted, fitted but not working, working) and **link home** (no radio, jammed, out of touch, faint, in touch, away from home's map). No part roles, so a vehicle with no radio bays is not flagged |
| VS2, bays | An **aviation radio bay**, a **voice radio bay** and a **repeater bay** on every aircraft; a **voice radio bay**, a **repeater bay** and a **Morse key bay** on every boat. The same health and hit weights as the land base's |
| VS3, from the factory | A new loadout list, `factoryFittings` (and `factoryFittingsMilitary`), each fitting given once, free, to new vehicles and those already in a save, remembered by name so one taken off stays off, with a message. **Aircraft**: the new aviation radio. **Military land vehicles** (armed, or rated military against EMP): the dual-band radio. **Boats**: the dual-band radio |
| The aviation bands | The civil **air band** (117.975 to 137 MHz, ICAO Annex 10) and **military UHF** (225 to 399.975 MHz, the AN/ARC-164's range), by a new **AM voice** mode (6 kHz, 10 dB, authored from the 8.33 kHz channel and receivers' rated 6 dB). A band may now be given only by an upgrade (`upgradeFigure`): the **aviation transceiver** on the desk or terminal, and **aviation antennas** on a mast. A built-in whip never works them |
| The aviation radio | 10 W (the AN/ARC-164's), AM, both aviation bands, a 2 m blade (authored). It is the aircraft's own radio on the net, standing by (no channel), so its links are worked out and shown on Comms; it cannot be driven from a station (only the remote control kit's radio can) |

**What outside the vehicle notices:** the station's Waterfall tab, which lists the aviation bands once fitted; jamming, which reaches
the aviation bands as any other (the mast's noise covers every band the radio knows). **Cost:** the Comms readings ask a few fitting
lookups and one dictionary lookup on the systems' own rare step; an aircraft on the net is one more radio in its pass, as a hand-held is.

**Not built in batch 1:** the crew's "in touch with home" mood aboard a vehicle in touch; boats on the marine VHF band (built since, in Radio 0.57.0 and
Framework 0.367.0, on the owner's answer of 28 Nov 2026, "Add the marine band": the marine radio from the factory, the marine
transceiver and marine antenna for stations, and boats already built keep the dual-band set); the flying relay (EW7, slice 3's second part), which needs a way for an
aircraft to hold over a place on the world map, as flights there only go from A to B today.

## Built: batch 2 (27 Nov 2026)

The owner's word the same day ("Start batch 2 of the standard"). **Not yet tested in game**: row 723 of the [test
pass](in-game-test-pass.md). What it added, and what its first run found against the owner's report of 27 Nov 2026 (11:39 UTC; 95
vehicles: land 58, fixed-wing 10, rotor 10, trailer 13, boat 2, submarine 2):

| Piece | As built |
| --- | --- |
| The audit kind | **Below the standard** and **Still to come** in `scripts/vehicle-bindings.py`, from `config/vehicle-standard.json`; tests in `tests/test_vehicle_bindings.py` |
| Kinds | trailer (its loadout's name), submarine (named: the Seabed Crawler and the X-1), rotor (a rotor drive role), fixed-wing (flight controls), boat (the boat loadout), land (the rest) |
| Found: three land vehicles below VS2 and VS4 | The **Busbar generator set** and the **Goat DE2** (both write their own part roles) and the **VVE Lightning** (the electric loadout, written apart from the land base) had no voice radio, repeater or Morse key bay. **Fixed**: the bays added to their three loadouts |
| Found: a fault of batch 1 | The **submarines** use the boat loadout, so batch 1 gave them the boats' radio bays, a factory radio and the Comms system: new submarine work, which the owner's back burner (20 Nov 2026) forbids. **Undone**: a part role or a system may now say `notOnSubmersibles`, and the boat's three radio bays and the Comms system do, so a design with the submersible comp takes none of them. A factory fitting is no longer marked as given to a vehicle with no bay for it, so a submarine gets its radio once submarine work returns. In a save where a submarine already took the radio, the fitting stays recorded with no bay to sit in |
| Still to come | VS6 and VS7 (fuel and oil) on 20 aircraft, VS9 (lamps) on 24 aircraft and boats, VS10 (galleys) on 4 transports: batches 3 and 4 |

**What a next game would need:** a report of its own vehicles' systems and parts, and a standard file like this one; the audit's code
reads only the two files' shapes.

## Built: batch 3 (27 Nov 2026)

The owner's word the same day ("Absolutely start batch 3 of the standard"), and three cards (5 to 7, [below](#questions-for-the-owner)).
**Not yet tested in game**: row 724 of the [test pass](in-game-test-pass.md). The land fuel and lubrication systems could not simply be
given to aircraft: a system's figure is one for the whole vehicle, and its stages act on the whole vehicle or a random part of a role,
so one dead pump would have reached every engine. So the per-engine part is code, built like the side tanks (which already stop one
engine at a time), and the systems show the worst engine.

| Piece | As built |
| --- | --- |
| Parts | A system may now ask for a part beside each engine (`partPerEngine`; `EnginePumps.cs`): Engine2's fuel pump plays `FuelPump2`, and the numbered roles roll up into `FuelPump`. Appended after every other part we add, so a saved aircraft gains them whole. Health 15, inside, rarely hit, outside the destruction sum (the radio bays' figures) |
| VS6, `PhobosVehicleSystem_EngineFuel` | **Engine fuel feed**, the worst engine's pump. A pump under a tenth of its work starves its engine, which stops; under half gives three quarters (the land thresholds). **Fuel** temperature follows the air it meets at 0.5 an hour; **fuel frozen** from Jet A-1's -47 °C to solid at -57 °C (authored), stopping every engine half way |
| VS7, `PhobosVehicleSystem_EngineOil` | Each engine's own sump (saved): it burns with that engine's output, twice as fast worn, and loses oil when that engine or its oil pump is hit (the land shares). **Oil pressure**, the worst engine's: its pump's work times what its level allows. No pressure seizes a running engine in about five hours, low pressure wears it (the land figures). **Oil level** is the emptiest sump, so the dashboard, the low-oil service call and the service's top-up work as on land |
| Stopping an engine | `EngineFeeds.Starved(i)` beside the side tanks' in the powertrain's engine count and orders, so everything that follows a lost engine follows it: engine-out ratings, lopsided thrust, the hover check and a fall, the cruising height's turn back, being shot down |
| The air at height | On a leg flown at a cruising height, the fuel meets the standard atmosphere's air at that height (6.5 °C a kilometre from the ground's own air, to 11 km, never below -80 °C, authored) plus the ram rise of the air meeting the aircraft (T × 0.2 M²; Mach 0.7 fixed wing, 0.2 rotor, authored). **An agent addition to the card**, which spoke of maps: without it jet fuel would matter only on a map below -52 °C, and the real hazard is long, high, cold flights |
| Every engine out | `DeadStick.cs`: on the world map, an aircraft of ours with every engine stopped glides down onto the tile below and lands as a caravan; over water, impassable mountains or in space it crashes through Vehicle Framework's own crash. Before, it struggled on across the leg. A twin with one engine left keeps the cruising height's judgement |
| Take-off | Refused with a ruined pump, frozen fuel or an engine without oil pressure |
| The setting | **Fuel and oil faults stop aircraft engines** (Flight, on; the owner's card). Risk high, the agent's grade. Off, the readings are kept and shown but nothing stops or is harmed |
| The standard | VS6 and VS7 in `config/vehicle-standard.json` now ask for these systems and the `FuelPump` and `OilPump` roles on fixed-wing and rotor aircraft, built |

**What outside the vehicle notices:** the world map (an aircraft coming down where it is); the crew of a crashed aircraft. **Cost:** a
few part lookups per engine on the systems' own update (every 250 ticks on a map, each rare tick in flight), and one more check of the
engine count, which already looks at each engine's part. Not measured in play.

**Not built:** another author's flight on a map (the Harrier, the close air support jets, the Prowler, the Hind, the Mongoose) is not
brought down by us when every engine stops; our own flights already fall. A fuel heater or fuel-oil heat exchanger to keep the fuel
warm; fuel cross-feed between engines' pumps; the oil's own temperature on aircraft. **What a next game would need:** the rule
(`EngineFeedRule`) is game-free; a game with engines and a part model would need its own pumps and its own place to stop an engine.

## Built: batch 4 (28 Nov 2026)

The owner's word ("Start batch 4 of the standard", 28 Nov 2026) and four cards (8 to 11, [below](#questions-for-the-owner)). **Not yet
tested in game**: rows 743 to 745 of the [test pass](in-game-test-pass.md).

| Piece | As built |
| --- | --- |
| VS9, the lamp bracket | The land vehicles' **lamp bracket** on every aircraft and boat (never a submarine), with the work lamps' load, so the existing work lamps fitting lights the ground round a parked aircraft or a boat's deck at night (10 A on the 12-volt electrics, as a light land vehicle's) |
| VS9, landing lights | A **landing lights** mount on every aircraft, and a **landing lights** fitting on it from the factory (`factoryFittings`; a ruined mount puts them out until refitted). Their load burns at night while the engines run, switched on by default, and casts the aircraft's beam: the headlights' rule (`Headlights.cs` takes a loadout's first load with a beam), lighting the ground 6 cells ahead. 12 A, authored as the share of the generator a real pair of 600 W, 28 V lamps takes |
| VS9, the searchlight | A **searchlight turret** on every aircraft with a rotor drive (helicopters, and the tiltrotor and turboprop transports our loadouts give one), by a new `onlyWithRole`; the **searchlight** fitting goes on it. Its own load, switched by hand and off by default, casts a pool of light 3 cells ahead of the nose that follows the aircraft as it flies over the map, hovers or taxis (`Searchlight.cs`, at the headlights' cadence). What the light does is the game's: work in it goes at daylight's pace, and under Combat Extended a shot at someone in it is aimed by its light (CE reads the light at the target, `CE_Utility.GetLightingShift` from `LightingTracker.CombatGlowAtFor`, found with `scripts/query-code.py`). 18 A, authored as the share of the generator Spectrolab's 1,600 W SX-16 takes |
| Night landings | Under the existing **Rough landings** setting: a touch-down in the dark, with no landing lights burning and nothing lighting the ground under the aircraft (the game's own light grid below 0.3), has the same chance of a rough landing as landing blind (0.4), and an autopilot does not lessen it (`RoughLandingRule.Chance`, tested). Another faction's crew is taken to land with its lights on where it has them |
| VS10, the bays | The land vehicles' **fridge bay**, **water tank bay** and **outlet panel** on the C-17, the C-27J, the Osprey and the Trawler, for the fittings that already exist |
| VS10, the galley | A **galley bay** and a **galley** from the factory on the three that carry one in reality: the C-17, the C-27J and the Trawler (the Osprey has none; the Dinghy, an open boat, keeps the plain boat loadout). The galley is a fitting carrying the field kitchen's cooking rules (`PhobosCaravanCookingExtension`): while the caravan rests its cooks turn raw food into simple meals, 10 a cook, indoors with no outdoor spoiling, the stove running on the vehicle's own fuel (0.2 a meal, `fromVehicleTank`), and a boat at sea cooks on the move. A field kitchen in the caravan still cooks first, then a galley, then a kitchen box. The Trawler has a loadout of its own for this (D'thorga Vehicles), built on the boat's |
| The thrust pods | The Vanilla Vehicles Expanded **Frog**, **Toad** and **Goliath**, which batch 3 missed (their loadouts write their own systems), are **excused** from VS6 and VS7 (the owner's card): fictional chemfuel thrusters have no oil sump, and chemfuel is not jet fuel. `config/vehicle-standard.json` records why, and the audit no longer lists them |
| Saved vehicles | Every part this batch adds is appended **last** (a new `last` on a part to add), after the late parts, the turbocharger and each engine's pumps, which code appends after the late list: a part marked late today would still have moved those in a saved vehicle |
| The standard | VS9 (a lamp bracket on aircraft and boats, landing lights on aircraft, a searchlight turret on rotor aircraft) and VS10 (the bays on the four, a galley on three) are built in `config/vehicle-standard.json` |

**What outside the vehicle notices:** the ground round and ahead of it at night (the game's own light: work speed, and Combat Extended's
aim); a caravan's packs (raw food out, meals in) and the vehicle's fuel. **Other factions' vehicles:** in a raid on us, their aircraft burn
their landing lights while they move at night (the light tier's rule for headlights) and are taken to land lit; a searchlight is ours only
(another faction's vehicle keeps no switch). Parked at a base we attack or captured, they carry the same parts, and a captured one keeps its
fittings. **Cost:** the searchlight costs one check a tick while out and a look every 20 ticks while lit, as the headlights; a night landing
reads one cell of the light grid once a landing; the galley is looked for once an hour per caravan with the cooking setting on. Not measured
in play.

**Found on the way: batch 1's bays were not appended last.** The aviation radio, voice radio and repeater bays on aircraft, the boats' three
radio bays (Framework 0.356.0) and the bays batch 2 gave the Busbar, the Goat DE2 and the electric loadout (0.359.0) were appended in the
loadout's order, ahead of the parts code appends after them (the weapon mount, the glass, the fire-fighting parts, the spall liner, the
brakes at each wheel, the late parts, the turbocharger). In a save made before those versions, the first load after them read those parts'
saved health into the new bays and moved the rest along, once. It has happened already in any save loaded since, so the order is left as it
is: moving it back would do it again. Its effect was a one-time mix-up of part health (a dented canopy's wear shown on a radio bay, say),
never lost parts. `last` exists so it does not happen again.

**Not built:** the aircraft's electrics stay the land vehicles' 12-volt, 65 A system (a simplification; real aircraft run 28 V with larger
generators), so the lights' draws are authored as shares; a 28-volt aircraft electrical model would let them be real. A searchlight aimed
anywhere but ahead of the nose; a searchlight on boats; the galley on a map (it cooks for caravans only); meals in flight on the world map
(an aircraft's caravan cooks only on the ground). **Art:** the three fittings borrow existing icons (the headlights', the work lamps', the
meal halt's) until their own are drawn in the local run (the owner's card: "art included"). **What a next game would need:** the night-landing
rule is game-free; lights and galleys are this game's Defs.

## Built: batch 5 (28 Nov 2026)

The owner's word ("Do Batch #5", 28 Nov 2026) and four cards (12 to 15, [below](#questions-for-the-owner)). **Not yet tested in game**: row
747 of the [test pass](in-game-test-pass.md).

| Piece | As built |
| --- | --- |
| Which vehicles | **Military vehicles**, as the factory radios judge them (`VehicleFittings.CountsAsMilitary`, now one static test for both): armed by design, or a loadout rated military. In the owner's game this should take in the tanks, APCs and IFVs (the M1A2s, the Bradley, the M113s, the T-54/55, the Wiesel, Vanilla Vehicles Expanded's armed tracked vehicles) and the armed Humvee, JLTV and MaxxPro: **the agent's expectation, not checked vehicle by vehicle**; the bindings report's new "military" field says which, from the next game start. A truck armed only by an upgrade kit stays civilian |
| What goes | The **stereo slot**, the **sleeper berth** (heavy vehicles), the **roof mount** (roof tent) and the **side rail** (awning): `notOnMilitary` on their part roles. The solar mount and fridge bay stay (the agent's lean, the owner's pick: armies carry solar blankets and ration coolers) |
| How | Each part is still made in its place, so every stage appends as before, then taken out of the design with its place recorded (`removedParts`, `PartLayoutRule.PlacesOf`). Only a part we added; a design's own part never |
| Saves | Each vehicle now saves the **part layout** it follows (`PhobosVehicleSystems_PartLayout`; `PartLayouts.Current` is 1). A vehicle loaded from an older one drops the saved entries at the removed places, last first, before Vehicle Framework binds the rest in order (`PartsOnLoad`, `PartLayoutRule.DropAt`, tested), so every other part keeps its own wear. A place past the end of an older save is left alone. Says so in the log |
| What was fitted there | A stereo, cab bunk, roof tent or awning on a bay that went comes off at spawn, its kit into the vehicle's cargo as if a mechanic had taken it off, with a message (`SettleRemovedParts`; the owner's card, "Back into the cargo") |
| The turbines' service list | The radiator and drive belt the service system reads on a turbine, a boat cooled by the sea or a generator set were never shown or broken: an unbound role reads as sound. A system may now name them `optionalRoles`, and the schema check no longer warns of them, nor of the pumps a system adds beside each engine (`partPerEngine`): 113 warnings became 13 |
| The audit | The bindings report now says whether each vehicle counts as military and which parts its design had taken out, and a standard line may name `absentRoles` for `militaryOnly` vehicles: VS11 lists any military vehicle still showing a civilian bay, from the next game start |

**What outside the vehicle notices:** the vehicle's cargo (the kits), once. **Other factions' vehicles:** a raider's or a base's military
vehicle has no civilian bays either; one captured keeps that. **Cost:** at start-up, a handful of list operations per military design; at a
load, one list look per vehicle; nothing while playing. Not measured in play.

**Not built:** VS12's crew intercom (still set aside until it changes something); other classes' misfits (a dirtbike's sleeper bays are
already only on heavy vehicles; a locomotive keeps its stereo); the remaining 13 schema warnings, which are other questions (a boat's clutch,
the electric vehicle's starter). **What a next game would need:** the part layout rule is game-free; any game that saves parts by position
needs its own place to drop the entries.

## Questions for the owner

Put as cards on 27 Nov 2026 and answered that day. Each as put: every option, the agent's recommendation marked, then the answer.

| # | Question | Options, as put | Answer |
| --- | --- | --- | --- |
| 1 | Which vehicles leave the factory with a voice radio fitted? | **Aircraft, military, boats** (recommended): every aircraft and helicopter, every armoured or military vehicle, boats; civilian trucks keep it as a fitting · Every powered vehicle · Aircraft only | **Aircraft, military, boats** |
| 2 | How should an aircraft's stock radio work? | **Multiband set** (recommended): covers our 2 m and 70 cm bands as real wideband sets do · **Add the aviation bands**: new air band and military UHF; stations need new kit · Our bands, labelled | **Add the aviation bands** |
| 3 | Should a Comms system appear on the Systems page? | **Every powered vehicle** (recommended), saying "no radio fitted" where there is none · Only with a radio | **Every powered vehicle** |
| 4 | How should the standard be kept? | **An audit kind** (recommended) in the vehicle bindings audit, from a data file and the game's own report · Audit and a gate check · Record only | **An audit kind** |
| 5 | On a multi-engine aircraft, should each engine have its own pumps and oil, or one set for the whole aircraft? | **Each engine its own** (recommended): a fault takes one engine, so a twin limps home on the other · One set per aircraft: quicker, but one fault reaches every engine | **Each engine its own** |
| 6 | What may a fuel or oil fault do to an aircraft in the air? | **Real, with a setting** (recommended): oil gone seizes that engine in a few hours and a dead pump starves it; a twin flies on one engine, a single-engine aircraft must land; its own setting, on · Real, setting off · Wear and warn only | **Real, with a setting** |
| 7 | Jet fuel freezes far colder than diesel. What should aircraft fuel do in the cold? | **Freezes at its real point** (recommended), sourced · Never thickens | **Freezes at its real point** |
| 8 | Batch 4, lights (VS9): what should aircraft and boats get? | **Bracket + landing lights** (recommended): the lamp bracket on every aircraft and boat, and landing lights from the factory on every aircraft, on a part a hit can put out · **Also a helicopter searchlight**: a searchlight fitting that lights the ground under a helicopter flying over the map at night (the agent leaned to a later slice) · Lamp bracket only | **Also a helicopter searchlight** (28 Nov 2026) |
| 9 | Night landings: should landing in the dark without landing lights matter? | **Dark and unlit = risky** (recommended): under the existing Rough landings setting, the same chance as landing blind; lights or a lit spot take it away · Lights only · Always rough | **Dark and unlit = risky** |
| 10 | Batch 3 missed the thrust pods (Frog, Toad, Goliath). What should they get? | **Excuse them, recorded** (recommended) · Fuel pumps only (a pump per thruster, no oil, no freezing) · Same as every aircraft | **Excuse them, recorded** |
| 11 | Galleys (VS10): should anything come fitted from the factory? | **Bays only** (recommended) · C-17 and Trawler fitted · C-17 and Trawler only | The owner's own answer: "Bays only, unless some have a kitchenette in real life? Which if so, they get that by default or as an upgrade, art included." Built as: the bays on all four, and a galley from the factory on the C-17, the C-27J and the Trawler, which carry one |
| 12 | Batch 5 (VS11): which vehicles lose the civilian bays? | **Military vehicles** (recommended): armed by design or rated military, as the factory radios · Tracked fighting vehicles only · Name them one by one | **Military vehicles** (28 Nov 2026) |
| 13 | Which bays should those vehicles lose? (several) | **Stereo slot** (recommended) · **Sleeper berth** (recommended) · **Roof tent + awning rail** (recommended) · Solar mount and fridge bay (the agent leaned to keep them) | **Stereo slot, sleeper berth, roof tent and awning rail** |
| 14 | What happens to a fitting already on a bay that goes? | **Back into the cargo** (recommended), as its kit, with a message · Simply gone | **Back into the cargo** |
| 15 | Taking a bay away shifts every later part in a saved vehicle. How should saves be handled? | **Move saves over** (recommended): each vehicle remembers its part layout, and the removed entries are dropped at the first load · New vehicles only | **Move saves over** |

## Who chose what

- **Owner (27 Nov 2026):** radios on aircraft by default, shown on all three pages; a standard so players are not bewildered; slice 3
  to start. By card: factory radios on aircraft, military vehicles and boats; the real aviation bands; Comms on every powered vehicle; an
  audit kind to keep the standard. Batch 3 ("Absolutely start batch 3"), by card: each engine its own pumps and oil; real consequences
  in the air behind a setting that starts on; jet fuel freezing at its real point. Batch 4 ("Start batch 4 of the standard", 28 Nov
  2026), by card: the lamp bracket, landing lights and a helicopter searchlight; dark, unlit landings as risky as blind ones; the thrust pods
  excused; galleys where the real vehicle has one, from the factory, with their own art. Batch 5 ("Do Batch #5", 28 Nov 2026), by card:
  military vehicles lose the stereo slot, sleeper berth, roof mount and side rail; their fittings come back into the cargo; saves are moved over.
- **Agent:** the standard's lines, the batches, the audit, and every lean here, for the owner to revise. In batch 3: the code route
  (per-engine parts and a service, not the land systems), the air at cruising height and the ram rise, Jet A-1's figure (not Jet A's
  -40 °C, which the card named), solid at 10 °C below, the gliding landing or crash on the world map, and the setting's risk grade. In
  batch 4: landing lights on by default and from the factory; the searchlight on every aircraft with a rotor drive (the tiltrotor and turboprop
  transports included), switched by hand; an autopilot not helping a dark landing; the lights' draws as shares of the 12-volt generator; which
  of the four carry a real galley (the C-27J's from Leonardo's brochure, not the card's examples); the galley cooking for caravans on the
  vehicle's own fuel, its figures, and its place after a field kitchen; and appending every new part last. In batch 5: the part layout
  number on each vehicle and dropping saved entries at the first load; the fittings coming off at spawn; optional roles for the turbines'
  service list; the audit's military lines.

## Sources

- D'thorga Framework's bindings report of the owner's game, 27 Nov 2026, read with `scripts/vehicle-bindings.py` and a comparison by
  kind written for this record.
- Real-world basis: general knowledge of aircraft, armoured vehicle and boat equipment, **unverified** for this record; each batch
  that builds a line names its sources before it uses a figure.
- Batch 3: Jet A-1's freezing point of -47 °C at most (DEF STAN 91-091 and ASTM D1655, as given by Wikipedia, "Jet fuel"; secondary);
  the International Standard Atmosphere's lapse of 6.5 °C a kilometre to 11 km; total air temperature, T(1 + 0.2 M²) for air
  (standard aerodynamics). The rest is authored and labelled in `Systems_EngineFeed.xml`.
- Batch 4 (all secondary, as listed; the draws in our 12-volt model are authored):
  - [Spectrolab's SX-16 Nightsun](https://www.aeroexpo.online/prod/spectrolab-inc/product-175615-82966.html): a 1,600 W short-arc xenon
    searchlight, 30 to 40 million candlepower, about 1 km of reach, 55 to 67 A drawn (Spectrolab's catalogue, as AeroExpo lists it).
  - A 600 W, 28 V PAR-64 landing lamp ([Boeing Distribution's listing](https://shop.boeing.com/cpd/bdi_q4559x%3D18)); about 21 A is our
    sum (I = P / V).
  - The C-17's galley: a refrigerator, freezer, coffee maker, sink, waste container and an oven (a [USAF photo
    caption](https://www.mcchord.af.mil/News/Photos/igphoto/2000842834), as the search engine summarised it; the page itself not read).
  - The C-27J Next Generation's "rest area with seats and galley" ([Leonardo's brochure, a copy on
    Scribd](https://www.scribd.com/document/533263566/C-27J-Next-Gen-brochure-ENG); seen only as a search excerpt, **unverified**).
  - A trawler's galley: general knowledge of fishing vessels; a Danish-built trawler's deckhouse "galley with provision room"
    ([Fiskerforum](https://fiskerforum.dk/?p=332561), seen only as a search excerpt).
  - Combat Extended aiming by the light at the target: its own code, `CE_Utility.GetLightingShift` and
    `Verb_LaunchProjectileCE.ShiftVecReportFor` (read with `scripts/query-code.py`).
