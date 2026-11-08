# Gas turbines: design (8 Nov 2026)

A design record. **Built on 8 Nov 2026 (see [As built](#as-built-dthorga-framework-0110-dthorga-vehicles-020-dthorga-patches-0140-8-nov-2026)); not yet tested in game.** The text above that section keeps its original scope. The owner asked on 8 Nov 2026 for turbine engines
("jet turbines such as the Chinook or the mod I'm also using with the Harrier", "there's the M1A1 Abrams too") and said to
write it up. It extends the [drivetrain and engine speed design](vehicle-drivetrain-design.md) and is written against
[vehicle systems](vehicle-systems-design.md) **as built in D'thorga Framework 0.9.0** (8 Nov 2026). Studied sources are in
[vehicle simulation references](vehicle-references.md); the flight frameworks' behaviour comes from the code reading in
[vehicle patches](vehicle-patches.md) (3 Nov 2026). The owner reads and corrects this before any code.

## Read this first

- **Which vehicles.** On the owner's recorded mod list: the **Harrier** (potemkinvillager, on its own fork of the Fixed-Wing
  Aircraft Framework), and the **Hind** and **Mongoose** (potemkinvillager, on rabiosus's VTOL Vehicle framework). D'thorga
  Vehicles 0.1.0 already fits RimThunder's **M1A2** (Roaring Tiger) and **M1A2 SEP** (Desert Sabre) with a gas-turbine loadout
  without the cooling system. **A Chinook and an M1A1 are not on the recorded list.** The owner should name those mods: they may
  have been added since the last refresh (`python scripts/refresh-mod-list.py`). Selection is by effect, so they would be
  fitted anyway; naming them lets a loadout use their real figures.
- **One hook in the built code.** `CompPhobosVehicleSystems` sets `EngineLoad` to 0, 0.5 or 1 from drafted and moving. With an
  engine model named in the loadout, the model sets it instead. Every system that already reads `Input_EngineLoad` (oil heat,
  wear, the alternator) then sees real load with no change to its XML.
- **The same method as the drivetrain.** Work out the power or thrust the vehicle needs; derive spool speed, turbine temperature,
  fuel power and wear from it. Turbine temperature is **calculated, not integrated** (limits and wear still read it so; since Framework 0.29.1 the figure shown is a quantity that follows it while running and cools over about an hour after shutdown). Oil temperature and water level go through
  the built `SystemIntegrator`, which already takes exact exponential steps with a banded thermostat. Hot-section life is a
  counter.
- **Aircraft get their load from flight phases.** Vehicle Framework gives no throttle, but both flight frameworks keep distinct
  phases (taxi, take-off, departing, on station, landing). Each maps to a demand. The phase names below come from the 3 Nov code
  reading; **a fresh reading of the decompiles in `.local/` is needed before code** (the list is at the end).
- **The Harrier cannot take off vertically in its framework** (vehicle patches: "the Harrier has no vertical take-off"). It takes
  off from a beacon runway and lands on its pad first. So the hover margin decides **vertical landing**, not take-off.
- **Fuel balance is untouched by default.** Vehicle Framework's and the flight frameworks' fuel figures stay; the model shapes heat,
  temperature and wear. The drivetrain design's "load-dependent fuel use" setting covers turbines too.
- **No per-tick work.** The model runs when a phase changes, on Vehicle Framework's ScanShort while moving, and on the rare tick.
- **Every starting figure is labelled**: **[P]** primary, **[S]** secondary, **[I]** our inference, **[A]** authored.

## Three kinds of turbine

| Kind | Vehicles | What the crew watches | What limits it |
| --- | --- | --- | --- |
| **Turboshaft, free power turbine** | Helicopters (Hind, Mongoose; a Chinook) | Gas-producer speed (%), torque (%), turbine temperature. Rotor speed stays near 100%: the power turbine governor regulates fuel to the gas producer so the rotor stays nearly constant as loads vary [S: CH-47 operator's manual TM 1-1520-240-10] | Torque and turbine temperature; hot days; losing an engine |
| **Turbofan with vectored thrust** | The Harrier (a Pegasus in life) | Fan and core speed (%), jet-pipe temperature, water | Turbine blade temperature, worse at higher ambient temperatures [S: Wikipedia, "Rolls-Royce Pegasus"]; time-limited ratings |
| **Tank turbine** | M1A2 and M1A2 SEP (RimThunder); an M1A1 | Turbine speed (%), temperature, gear (the drivetrain design's tracked archetype) | Idle fuel use: the Abrams burns almost as much fuel idling as moving, because the turbine keeps turning when the tracks stop [S: StrategyPage, 2003] |

## The model, step by step

1. **Demand.** Ground vehicles: the drivetrain model's engine power ÷ rated power. Aircraft: the phase's demand (table below),
   scaled by weight and air density.
2. **Available.** Rated power or thrust × ambient derate × hot-section condition × (working engines ÷ engines).
   - **Ambient derate**: 1 − 0.7% per °C above 15 °C [A]. That sits in the 0.5% to 0.9% per °C published for industrial gas
     turbines [S: Università di Bologna], which aircraft engines need not follow. Replace it per engine when an aero figure is found.
   - **Hot-section condition**: eroded compressor blades, worn seals and growing tip clearances lower a turbine's performance
     over time [S: Aviation Week]. Here, a curve of the hot-section life used [A].
   - **Rating ladder**: maximum continuous, then time-limited ratings above it, each with its own time limit. For the Pegasus,
     between 64.3 kN of lift (maximum continuous, 91% rpm) and 95.9 kN (15 s wet, 107% rpm) at sea level [S: All-Aero]. A short
     lift dry rating sits between them; **no source found**, so it is authored at 90% of wet (86.3 kN) [A].
3. **Spool speed.** JSBSim's generic turbine makes thrust rise with the square of normalised core speed [S: JSBSim
   `FGTurbine`], so speed % = idle % + (100 − idle %) × √(demand ÷ available). Spool lag is a few seconds (the Abrams goes from
   idle to full power in 2.5 s [S: STRASAM]), shorter than one sample, so the lag is eased in the UI and sound only.
4. **Turbine temperature**, calculated each sample: ambient + a curve against demand ÷ available, running from idle temperature to
   the maximum-continuous limit at 1, then on up each time-limited rating's limit [A, per engine]. JSBSim's generic turbine uses a
   straight line from ambient + 363 °C at idle to ambient + 720 °C at full throttle [S: JSBSim `FGTurbine` source]; that is
   the default shape.
5. **Fuel power.** Fuel fraction = idle share + (1 − idle share) × demand [A, a Willans shape]. Idle share: **0.6 for a tank
   turbine** [A, matching "almost as much at idle"], 0.25 for aero engines [A]. That gives the tank 83% of its moving fuel use at
   idle, against 53% for a diesel-like engine. Heat goes to oil and exhaust, not coolant.
6. **Oil temperature** uses the built lubrication system, now fed by real `EngineLoad`. An oil cooler is a part [A].
7. **Hot-section life** is a counter that runs whenever temperature is above the maximum-continuous limit, faster the hotter
   it is (an exponential in degrees over the limit [A]; creep life is strongly temperature-dependent, but no figure is cited
   yet). Past 100% it raises the breakdown rate the built service system already applies. Over the top rating's limit, or past
   a rating's time, it damages the engine part straight away.
8. **Starting** links to the electrical system. JSBSim's ground start spins the core to about 25% N2, and fuel goes in once N2
   passes 15% [S: JSBSim `FGTurbine`]. Starter strength = battery charge × the built cold-capacity curve. Too weak to reach 15%
   is a **hung start**: the draft is refused, with the reason. Barely enough is a **hot start**: a weak battery may not spin the
   turbine fast enough to cool the combustion section [S: Aviation Week]. That costs hot-section life and sends a message. An
   APU, a ground power source or a jump-start from another vehicle (built in Framework 0.10.0) gives full strength.
9. **Water injection (Harrier).** A water quantity: up to 227 kg of distilled water, used at about 35 imperial gallons a minute
   for at most about 90 seconds; once it runs out, the limits drop back to the dry ratings [S: All-Aero]. 35 gal/min is 2.65 kg/s,
   so 227 kg lasts 86 s, which agrees. Water flows only while a wet rating is in use.

## Aircraft: from flight phase to demand

Demand is a fraction of maximum-continuous power or thrust. Every figure is authored.

**The Harrier** (Fixed-Wing Aircraft Framework fork; phases as read on 3 Nov):

| Phase | Where it shows in the framework | Demand |
| --- | --- | --- |
| Engine running, parked on the pad | drafted, not moving | 0.08 |
| Taxi | taxi orders (`OrderVehicle_PawnGotoAction_TaxiPatch`, `CanTaxi`) | 0.15 |
| Runway take-off | launch from a beacon runway | 1.0, using the take-off rating |
| Departing the map | `BeginDepartingMap` until `TryCompleteMapDeparture` | 0.85 |
| World flight | Vehicle Framework's `AerialVehicleInFlight` | 0.6 |
| On station | arrival spawns it at the edge, flying | 0.5 |
| Attack run | a bombing or rocket run (`Command_TargeterCooldownAction_AttackRunPatch`) | 0.8 |
| Vertical landing | landing on its pad (`OrderLandingHome`; the pad first) | weight ÷ hover thrust available: the **hover margin** |

**The vertical-landing check** (the real "bring-back" weight problem). Hover needs thrust at least equal to weight. Worked
example, using the record's empty weight of about 6,340 kg [S: Pima Air & Space Museum], 1,700 kg of fuel [A], two Mk. 82s at
227 kg [S] and a 100 kg pilot [A]: 8,594 kg, a weight of 84.3 kN.

| Ambient | Short lift dry (authored) | 15 s wet | Outcome |
| --- | --- | --- | --- |
| 15 °C | 86.3 kN | 95.9 kN | Lands vertically dry, with almost no margin |
| 30 °C | 77.2 kN | 85.8 kN | Needs water |
| 40 °C | 71.2 kN | 79.1 kN | Cannot land vertically: runway, or drop the bombs |

These are outputs of an authored derate and an authored short-lift rating, so the threshold temperatures will move. The shape is
real, though: hot days need water, and heavy hot landings need a runway.

**Helicopters** (VTOL Vehicle framework: grounded, flying on a map, departing, world flight, landing; to confirm in the
decompile). Hover power follows ideal momentum theory: power rises with weight to the power 1.5 and falls with the square
root of air density [S: momentum theory, any rotorcraft text; Leishman, *Principles of Helicopter Aerodynamics*, listed]. So
a hot day raises the power needed as it lowers the power available.

| Case (twin turboshaft, 80% of maximum gross weight, hover needing 90% of both engines' power at maximum gross [A]) | Needed | Available |
| --- | --- | --- |
| 15 °C, both engines | 0.64 | 1.00 |
| 40 °C, both engines | 0.67 | 0.83 |
| 40 °C, one engine at a contingency rating of 110% [A] | 0.67 | 0.45: **cannot hover; land running or shed load** |

Forward flight needs less than hover (0.6 of maximum continuous [A]); the attack-run and loiter figures follow the Harrier's.

**Weight.** Empty weight comes from the named loadout where a source exists (the Harrier's, above). Otherwise it is Vehicle Framework
Mass × kilograms per unit [A], as in the drivetrain design. Payload is cargo as a share of `CargoCapacity`, ordnance at its items'
masses (under Combat Extended, the bombs' real 227 and 907 kg) and fuel as a share of tank × internal fuel mass [A].

## Twin engines

A twin-engine design gets two engine roles (`Engine1`, `Engine2`), found by key or appended, each with its own health, starting and
hot-section life. Losing one halves available power; the other can use a contingency rating for its time limit. The inspect line
says "one engine out". Where a mod models a twin-engined aircraft with one engine part, the named loadout adds the second (owner
question 4).

## The tank turbine

The M1A2 loadouts in D'thorga Vehicles gain an `engine` (turbine kind, idle share 0.6) on the drivetrain design's tracked
archetype. The AGT1500 is a 1,500 hp (1,120 kW) turbine [S: secondary; Honeywell's page names the engine but was not read for the
figure]. **An APU as a Vehicle Framework upgrade node** (`CompUpgrade`): later Abrams carry one so onboard systems run with the
main turbine off [S: secondary]. With it fitted, an idle tank powers radios and sensors from the APU at a fraction of the fuel
[A: 0.05 of moving use]. Honeywell credits the AGT1500's digital control unit with 20% lower idle fuel consumption [S: Honeywell];
a later upgrade node could carry that too.

## Schema (draft; names settle at build; namespace `Phobos.VehicleSystems` as built)

The drivetrain design's engine block gains a `kind`. Turbine fields:

```xml
<engine>
  <kind>Turboshaft</kind>                      <!-- Piston, Turboshaft, Turbofan -->
  <idleSpeedPercent>60</idleSpeedPercent>
  <ratings>
    <li><name>maxContinuous</name><share>1.0</share><speedPercent>100</speedPercent><temperatureLimit>775</temperatureLimit></li>
    <li><name>takeoff</name><share>1.1</share><speedPercent>102</speedPercent><temperatureLimit>815</temperatureLimit><minutes>5</minutes></li>
    <li><name>contingency</name><share>1.2</share><speedPercent>104</speedPercent><temperatureLimit>850</temperatureLimit><minutes>2.5</minutes></li>
  </ratings>
  <idleTemperatureAboveAmbient>363</idleTemperatureAboveAmbient>
  <deratePerDegree>0.007</deratePerDegree>
  <idleFuelShare>0.25</idleFuelShare>
  <startSpeedPercent>15</startSpeedPercent>
  <waterInjection><tankKilograms>227</tankKilograms><kilogramsPerSecond>2.65</kilogramsPerSecond><rating>wet</rating></waterInjection>
</engine>
```

(The temperature limits shown are placeholders [A]. Only the T55-L-714A's 815 °C power turbine inlet temperature was seen, on a
Wikipedia mirror [S, low quality].)

A new **`PhobosVehicleFlightProfileDef`** maps phases to demand. Phases come from pluggable **phase readers**, one per flight
framework, each in a small optional assembly loaded with `IfModActive`. That follows the Framework's Dubs Bad Hygiene pattern:
references to the installed DLL with `Private=false`, never copied.

```xml
<Phobos.VehicleSystems.PhobosVehicleFlightProfileDef>
  <defName>PhobosFlightProfile_FixedWingJet</defName>
  <reader Class="Phobos.VehicleSystems.FixedWing.PhaseReader_FixedWingFramework" />
  <phases>
    <li><phase>Parked</phase><demand>0.08</demand></li>
    <li><phase>Taxi</phase><demand>0.15</demand></li>
    <li><phase>TakeoffRoll</phase><demand>1.0</demand><rating>takeoff</rating></li>
    <li><phase>Departing</phase><demand>0.85</demand></li>
    <li><phase>WorldFlight</phase><demand>0.6</demand></li>
    <li><phase>OnStation</phase><demand>0.5</demand></li>
    <li><phase>AttackRun</phase><demand>0.8</demand></li>
    <li><phase>VerticalLanding</phase><demand Class="Demand_HoverMargin" /></li>
  </phases>
</Phobos.VehicleSystems.PhobosVehicleFlightProfileDef>
```

A generic reader (`PhaseReader_VehicleFramework`) covers any other Vehicle Framework aircraft from its own events: launch
(take-off), `inFlight` (world flight), landing. A new **turbine system** (`PhobosVehicleSystem_Turbine`) carries the quantities
turbine temperature (derived), hot-section life, and water (when injected), with stages: over the limit (message, then part
damage), life past 100% (breakdowns), water low (message), water out (back to dry ratings). The schema check gains: ratings in
ascending order, each above maximum continuous with a time limit, a water rating naming a real rating, and a reader present for
every phase used.

## Performance

- **Per phase change**: one evaluation (a few multiplies and one curve).
- **Per rare tick**: the same, plus the built integrator for oil and water.
- **Ground turbines**: the drivetrain's ScanShort sample, plus five multiplies.
- **Phase readers** read a field or an enum through a cached accessor; nothing scans the map.
- Not measured; the drivetrain design's 500-vehicle benchmark covers these too.

## To confirm in the decompiles before code

The 3 Nov reading of the fork's 21,000 lines answered other questions. Before this is built, read `.local/decompiled/` for:

1. **Harrier fork**: the state fields or enum for taxi, take-off roll, airborne on a map, attack run and pad landing; whether pad
   landing is a hover state with a hook where a refusal could send it to the runway instead; where carried ordnance lives.
2. **VTOL Vehicle framework (Hind, Mongoose)**: how hover differs from forward flight on a map; how landing and take-off are
   signalled; whether flight fuel goes through Vehicle Framework's fuel comp, so the built `EngineRunning` sees flight.
3. **Both**: whether either framework already models engine temperature or limits (none was noted on 3 Nov).
4. **The Chinook and M1A1 mods**, once named: part keys (one engine part or two), fuel figures and mass.

## As built (D'thorga Framework 0.11.0, D'thorga Vehicles 0.2.0, D'thorga Patches 0.14.0; 8 Nov 2026)

**Built and checked offline; not yet tested in game.** Code: `src/PhobosFramework.Vehicles/Rules/TurbineModel.cs` (game-free),
`Systems/Powertrain.cs`, `Defs/PowertrainDefs.cs` (the flight profile and its phase reader), `Service/HoverLimits.cs`, the
`PhobosVehicleSystem_Turbine` and `PhobosVehicleSystem_WaterInjection` systems (`Systems_Turbine.xml`), the profiles
(`FlightProfiles.xml`), D'thorga Vehicles' Harrier, Hind and Mongoose loadouts, and D'thorga Patches' `HoverGate.cs`.

**The owner's answers (8 Nov 2026):**

| Question | Owner's answer | As built |
| --- | --- | --- |
| 1. Too heavy or too hot to land vertically? | **A player setting**: "Offer configurable settings to the player from the menu for this kind of thing." | D'thorga Framework's setting: **use the runway** (default; D'thorga Patches refuses the pad with the reason, and an automatic return falls back to the Harrier's runway landing), **allow it at a cost to the engine** (the engine runs past its top rating: hot-section wear, then damage, and a message), or **warn only** (the engine is held at its top rating, and a message) |
| 2. What refills the Harrier's water? | **Dubs Bad Hygiene plumbing beside the parked vehicle when Dubs is active, otherwise free** | A service of 227 litres; with Dubs, from a pipe with a water tower beside or under the jet (a tie-in assembly loaded only with both mods). Coolant top-ups use the same water, and their glycol is 3 chemfuel |
| 3. Turbine fuel use? | **Keep the mods' figures; the setting off** | As the drivetrain's: the setting covers turbines, off by default |

Questions 4, 5 and 7 took the agent's leans (agent choices, owner to revise): a second engine role only in named loadouts (the
Hind and the Mongoose already have two engine parts, `EngineStarboard` and `EnginePort`, so nothing is appended); % spool speed
and degrees C in Operator mode, kW in Engineer; a hot start costs hot-section life and a message. **Question 6 (owner, 8 Nov
2026): the "Chinook" is Vanilla Vehicles Expanded - Tier 3's Cherokee and the "M1A1" is the M1A2 Abrams** already fitted.
The Cherokee's own parts include two turboshafts (`LeftTurboShaft`, `RightTurboShaft`), now bound as its two engines; it flies
through Vehicle Framework's own launch, so it uses the stock aircraft profile. Its parts were read with `query-defs.py show
--lines 0`, added to phobos-modkit (0.9.0) for this, because the viewer had cut the Def at 80 lines.

**What the decompiles said (8 Nov 2026), which the build follows:**

- **Phases come from the frameworks' own `FlightState` field** (Grounded, GroundedIdle, SpoolingUp, SpoolingDown, TakingOff,
  Flying, Landing, DepartingMap, Crashing), the same names in the Harrier's fork and in rabiosus's VTOL Vehicle framework. VF's
  `inFlight` is true on a map only while leaving it, so it marks world flight only. The Harrier's attack run is its controller's
  `FixedWingAttackRunActive`; its pad landing is `CompHarrier.landingPhase` 2 or more.
- **Not optional assemblies after all**: one data-driven reader, `PhaseReader_Members`, reads those members by name through
  cached reflection, so the Framework references neither framework. A member that does not exist never matches.
- **Every pad-landing path asks the Harrier's static `Harrier.CanHover(vehicle, props)`** (the button, the order, each tick of the
  approach and hover, and the automatic return to a parking spot, which falls back to the runway when it is false). D'thorga
  Patches postfixes it, and replaces the Harrier's damaged-engine text with the hover limit's reason on the button, the order and
  the cancel.
- **Fuel**: all three aircraft burn fuel only through the frameworks while taking off, flying or leaving; a parked aircraft idling
  burns nothing, so idling counts for heat and wear only.

**Differences from the draft:** hot-section life is one counter per vehicle, not one per engine (twin engines share it; a wrecked
engine counts as out); the water rating is a `wet` flag on a rating rather than a named rating in the water block; the hover check
for a jet uses thrust (weight against the best rating's derated thrust) and for a helicopter momentum theory against an authored
90% of both engines at maximum gross weight; the Harrier's kerb mass is its real empty mass (6,340 kg) and full fuel an authored
3,400 kg. **Not built:** the M1A2's APU upgrade node (it needs a patch to RimThunder's upgrade trees) and "offer to drop the
stores".

**Checked offline:** the Harrier's hover table at 15, 30 and 40 degrees C (dry at 15, wet only at 30, not at all at 40), the
helicopter's hover power with heat, the 40 degree derate and the one-engine contingency share, the spool and temperature lines,
and the life curve (`tests/PhobosFramework.Vehicles.Rules.Tests`).

**To test in game (owner):**

1. The Harrier in Operator mode through taxi, take-off and an attack run: turbine speed and temperature rise and fall with the phase.
2. A laden Harrier on a hot day ordered onto its pad, under each of the three settings: refused with the reason (and an automatic
   return uses the runway); allowed with a message and hot-section wear; allowed with a warning only.
3. A Hind with one engine wrecked: "one engine out", and the other runs hot.
4. A coolant top-up and an injection-water refill, with and without Dubs Bad Hygiene plumbing beside the vehicle.
5. An M1A2 idling: warm turbine, no coolant system; with a weak battery, a hot start message.
6. Save and reload mid-flight: no errors.

## Bleed air (Framework 0.24.0, 8 Nov 2026)

The owner asked for it after the cockpit heating (bleed air heats a real cockpit): "Yes, please! Provided we can still maintain good
performance". **Not yet tested in game.**

- **Source:** P. F. Borchers, E. Moralez III, V. K. Merrick and M. W. Stortz, *YAV-8B reaction control system bleed and control power
  usage in hover and transition*, NASA TM-104021, April 1994 ([NASA NTRS](https://ntrs.nasa.gov/citations/19940030488)). Read on
  8 Nov 2026 from the scanned report: total reaction control bleed in hover varied from 9.2 to 4.5 lb/s, above 6 lb/s for 60% of the
  time and 8 lb/s for about 4%; peaks came with transient control (gusts, corrections); the Pegasus could provide twice the most the
  reaction controls used; and Rolls-Royce sized it "to provide constant thrust for bleed levels up to this mean total bleed, with
  turbine entry temperature (TET) limits dictating a thrust reduction for higher bleed rates" (pages 1 and 6 of the report).
- **The model (our inference from that):** bleed raises the engine's demand for the same thrust (`BleedRule.Demand`), so the turbine
  model runs it hotter and wears it as before, and only past its ratings does the hover fail; the hover margin shrinks by the same
  share. Reaction control bleeds only in hover phases, 3% of the engine's work in calm air (authored from NASA's flows; the share of
  the engine's work is our estimate, not a measured figure), up to half as much again as the wind rises from 0.5 to 1.5 in the game's
  wind units (NASA's peaks were about half again the usual bleed). Cabin heat or climate control on any turbine aircraft takes 0.5%
  (authored). A helicopter steers with its rotors: no reaction control bleed.
- **Cost:** one wind read, two load checks and a multiplication per aircraft per systems update (every 250 ticks).

## Open questions for the owner

| Question | Agent lean |
| --- | --- |
| 1. Too heavy or too hot to land vertically: what happens? | Refuse the pad landing with the reason; land on the beacon runway if there is one; otherwise offer to drop the stores. Never a silent crash |
| 2. What refills the Harrier's water? | Water from Dubs Bad Hygiene's plumbing where it is active (a phase-D tie-in), else a refill job at any water source; no new item |
| 3. Turbine fuel use: model it (the tank's idle burn) or keep the mods' figures? | Keep them by default (the drivetrain design's setting); the model shapes heat and wear |
| 4. Add a second engine part to twin-engined aircraft modelled with one? | Yes, in named loadouts only, appended as the built service already does |
| 5. Display: % for spool speed, °C for turbine temperature, kN and kW in Engineer mode? | Yes; Plain mode says only "engine hot" or "one engine out" |
| 6. Which mods are the Chinook and the M1A1? | Name them, and a named loadout uses their figures |
| 7. A hot start: damage, or a warning only? | Hot-section life and a message; real damage only past the top rating's limit |

## Who chose what

- **Owner (8 Nov 2026):** turbines for the Harrier, a Chinook and an Abrams; write it up.
- **Owner (8 Nov 2026):** the hover limit as a player setting; water from Dubs Bad Hygiene plumbing, else free; keep the mods'
  fuel figures.
- **Agent proposals, owner to confirm:** the three kinds; `EngineLoad` as the one hook; phases as the aircraft load signal; the
  hover-margin check on vertical landing; the authored derate, short-lift rating, idle shares, demand table and life curve; the
  phase readers as optional assemblies; the APU as an upgrade node.

## Sources

Linked in [vehicle simulation references](vehicle-references.md), section "Gas turbines" (added with this record):

- the CH-47 operator's manual (TM 1-1520-240-10) on governing;
- All-Aero on the Pegasus's ratings and water injection;
- Wikipedia on its blade-temperature limit;
- JSBSim's `FGTurbine` (start sequence, thrust against core speed, temperature line);
- StrategyPage, STRASAM and Honeywell on the AGT1500;
- the Università di Bologna derate range;
- Aviation Week on hot starts and wear;
- the Pima Air & Space Museum's empty weight, via [vehicle patches](vehicle-patches.md).

The flight phases come from that record's code reading of 3 Nov 2026. The worked figures came from a prototype run on 8 Nov 2026.
