# The vehicle standard: what every kind of vehicle shows on Systems, Fittings and Health (27 Nov 2026)

**Status (27 Nov 2026):** the standard, and its first batch. **Batch 1 is built** (VS1 to VS3, radios everywhere): Radio Framework
0.49.0 and D'thorga Framework 0.356.0 ([as built](#built-batch-1-27-nov-2026)); not yet tested in game (rows 710 and 711). **Batch 2 is
built** (the audit, and the gaps it found): D'thorga Framework 0.359.0, D'thorga Vehicles 0.130.2 and Radio Framework 0.52.0 ([as
built](#built-batch-2-27-nov-2026)); not yet tested in game (row 723). Batches 3 to 5 are proposals. The owner answered four cards
([below](#questions-for-the-owner)).

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
| 3 | VS6, VS7 (fuel and oil for aircraft) | Real systems every aircraft has, with clear failures |
| 4 | VS9, VS10 (lights and transports' galleys) | Accessories that already exist; mostly XML |
| 5 | VS11 (what does not belong) | Takes things away from players' vehicles, so it waits for the owner's word on each |
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

**Not built in batch 1:** the crew's "in touch with home" mood aboard a vehicle in touch; boats on the marine VHF band (they use the
dual-band set, a labelled simplification; a decision is open); the flying relay (EW7, slice 3's second part), which needs a way for an
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

## Questions for the owner

Put as cards on 27 Nov 2026 and answered that day. Each as put: every option, the agent's recommendation marked, then the answer.

| # | Question | Options, as put | Answer |
| --- | --- | --- | --- |
| 1 | Which vehicles leave the factory with a voice radio fitted? | **Aircraft, military, boats** (recommended): every aircraft and helicopter, every armoured or military vehicle, boats; civilian trucks keep it as a fitting · Every powered vehicle · Aircraft only | **Aircraft, military, boats** |
| 2 | How should an aircraft's stock radio work? | **Multiband set** (recommended): covers our 2 m and 70 cm bands as real wideband sets do · **Add the aviation bands**: new air band and military UHF; stations need new kit · Our bands, labelled | **Add the aviation bands** |
| 3 | Should a Comms system appear on the Systems page? | **Every powered vehicle** (recommended), saying "no radio fitted" where there is none · Only with a radio | **Every powered vehicle** |
| 4 | How should the standard be kept? | **An audit kind** (recommended) in the vehicle bindings audit, from a data file and the game's own report · Audit and a gate check · Record only | **An audit kind** |

## Who chose what

- **Owner (27 Nov 2026):** radios on aircraft by default, shown on all three pages; a standard so players are not bewildered; slice 3
  to start. By card: factory radios on aircraft, military vehicles and boats; the real aviation bands; Comms on every powered vehicle; an
  audit kind to keep the standard.
- **Agent:** the standard's lines, the batches, the audit, and every lean here, for the owner to revise.

## Sources

- D'thorga Framework's bindings report of the owner's game, 27 Nov 2026, read with `scripts/vehicle-bindings.py` and a comparison by
  kind written for this record.
- Real-world basis: general knowledge of aircraft, armoured vehicle and boat equipment, **unverified** for this record; each batch
  that builds a line names its sources before it uses a figure.
