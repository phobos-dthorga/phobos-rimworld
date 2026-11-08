# Power and lift: rotor drives, thrust pods, many engines, diesel-electric (9 Nov 2026)

A design record, built the same day in D'thorga Framework 0.12.0 and D'thorga Vehicles 0.3.0 (Drafts). **Not yet tested in game.**
The owner asked which other vehicles could take "the two turboshaft-like thing or related while keeping things semi-realistic and
somewhat adapted to their real-life counterparts", then approved every proposal and asked for the work in two or three parts
(9 Nov 2026). This is part 1. Part 2 is real figures for the single-engine military vehicles, the L-ATV first; part 3 is a boiler
system for the steam locomotives. It extends the [drivetrain](vehicle-drivetrain-design.md) and
[turbine](vehicle-turbine-design.md) designs.

## Read this first

- **Four things, all generic**, so any mod's vehicles can use them through XML: any number of engines; a helicopter's rotor drive
  with a power limit; thrust-lifted craft that lose lift with each thruster; and a diesel-electric transmission with traction
  motors that heat.
- **A lift-off check for Vehicle Framework's own aircraft.** Helicopters and pods that launch through Vehicle Framework (the
  Mosquito, the Louie, the Cherokee, the Frog, the Toad, the Goliath) are checked against their weight, the day's heat and their
  working engines before they lift. **The owner's hover-limit setting** (9 Nov 2026) now covers this too: refuse with the reason
  (the default), allow at a cost to the engines, or warn only.
- **Nothing new is saved.** Engine counts, drive limits and lift margins are worked out from parts and Defs, as before. The new
  traction motor temperature is a system quantity, saved by system and name like the coolant's.
- **Every figure is labelled** as in the other records: **[P]** primary, **[S]** secondary, **[I]** our inference, **[A]** authored.

## Which vehicles, from their own parts

Read with `python scripts/query-defs.py show <Def> --type Vehicles.VehicleDef --lines 0` (the `--lines` option was added to
phobos-modkit 0.9.0 for this, because the viewer cut long Defs at 80 lines).

| Vehicle (mod) | Its parts | What it gets |
| --- | --- | --- |
| Frog (Vanilla Vehicles Expanded) | Engine, three thrusters | Thrust lift on three thrusters |
| Toad (Vanilla Vehicles Expanded) | Engine, four thrusters | Thrust lift on four thrusters |
| Goliath (VVE - Tier 3) | Engine, four thrusters; the mod already blocks launch when parts are damaged | Thrust lift on four thrusters, built heavy |
| Mosquito (Vanilla Vehicles Expanded) | Engine, rotor mast | One turboshaft, rotor drive |
| Louie (VVE - Tier 3) | Engine, rotor mast | One turboshaft, rotor drive (the Huey nod) |
| Cherokee (VVE - Tier 3) | Two turbo shafts, front and rear transmissions, front and rear rotor masts | Two turboshafts (0.2.0), now with a rotor drive |
| Goat DE2 (Trains of the Rim) | Engine, traction motor | Diesel-electric, traction motors that heat |

**Some Defs showed no engine part as written** (the BMP-1P, BMP-1PG and BMP-1AM, GTek's MaxxPro and L-ATV). That was our scan,
not the mods: it read only each Def's own block. Their engines and gearboxes are in their parent Defs (`BMP1_RuntimeBase`,
`HID_BaseMaxxProDash`, `HID_BaseJLTV`), so in game every one has an engine, and the default loadouts already bind it.

**Left out, for now (agreed with the owner):** the boats (the Dinghy's outboard and the Trawler's single diesel have nothing more to
model), the Warbird (a single piston fighter) and the Smuggler (one engine part; nothing says what it is meant to be, so no second
engine is invented).

## The model

### Any number of engines

A loadout binds `Engine1`, `Engine2` and so on, up to `Engine8`. An engine below 20% health is out; available output is the
working share. Without numbered engines, the one `Engine` counts, as before.

### Rotor drive

A helicopter's main transmission and rotor masts are rated for a power that can be below its engines' total, so the gearbox sets
the limit. The researched cases (9 Nov 2026):

| Helicopter (our vehicle) | Engines | Drive limit | Our `transmissionLimit` |
| --- | --- | --- | --- |
| Bell UH-1H (the Louie) | one T53-L-13, 1,400 shp | "limited to 1,100 shp (820 kW) by transmission" **[S: Wikipedia, "Bell UH-1 Iroquois", from Jane's 1987-88; a DTIC test document agrees, seen only as a search excerpt]** | 0.86 **[I: 1,100 against 1,400 ÷ 1.1, taking 1,400 as the take-off rating]** |
| Bell UH-1N (for comparison) | PT6T-3 Twin-Pac, 1,800 shp take-off | flat-rated to the transmission's 1,290 shp **[S: Wikipedia, "Pratt & Whitney Canada PT6T", from Wilkinson 1970]** | not used |
| Agusta A129 Mangusta (the Mongoose) | two Gem 2-1004D, 664 kW each | gearbox 970 kW **[S: Flight International, seen only as a search excerpt; low confidence]** | 0.80 **[I]** |
| CH-47 Chinook (the Cherokee) | two T55 | not found; an Army CH-47A/B document says the "transmission torque limit is reached before engine power limit" (excerpt only) | 0.9 **[A]** |
| Mi-24 (the Hind) | two TV3-117 | not found | 0.9 **[A]** |
| The Mosquito | (the mod names no real helicopter) | | 0.9 **[A]** |

The Hind and the Mongoose have no gearbox part of their own, so their loadouts append a main rotor gearbox among the engines. The turbine block's `transmissionLimit` (or a loadout's override) is that limit
as a share of all engines' combined maximum-continuous output, scaled by the `RotorDrive` parts' average condition.

- **Lift** is the smaller of what the engines give (after the hot-day derate, hot-section condition and engines out) and what the
  drive takes.
- **Over-torque**: demand above the drive's limit damages the `RotorDrive` parts and says so (`PhobosVehicleSystem_RotorDrive`;
  4 health an hour **[A]**). The engines are not made hotter by it: the gearbox, not the turbine, is past its limit.

### Thrust lift

`EngineKind.Thruster`: the turbine block's `liftToWeightAtMaxGross` sets every rating's thrust as a multiple of maximum gross weight
(empty mass, a full cargo hold and full fuel) **[A per craft]**. No derate with heat, no spool or turbine temperature **[A:
simplification]**. Each thruster out removes its share, so:

| Craft | Thrusters | Lift ÷ weight, full, all working **[A]** | One out | Two out |
| --- | --- | --- | --- | --- |
| Frog | 3 | 1.5 | 1.0 (just lifts full) | 0.5 |
| Toad | 4 | 1.4 | 1.05 | 0.7 (lighten it) |
| Goliath | 4 | 1.2 (the mod: "incredibly heavy", boosted off) | 0.9 (lighten it) | 0.6 |

Real precedents: with N equal thrusters, holding a hover after one fails needs a thrust-to-weight of N ÷ (N − 1): 1.5 for three,
1.33 for four **[I: arithmetic]**. So the Frog (1.5) just holds on two when full, the Toad (1.4) holds on three, and the Goliath
(1.2) cannot. A Falcon 9 lost one of its nine engines on the CRS-1 flight (2012) and still put Dragon into orbit **[S: Wikipedia,
"SpaceX CRS-1"]**; the Yak-38's lift engines were so critical that its seat ejected the pilot automatically if one failed **[S:
Wikipedia, "Yakovlev Yak-38"]**. Our pods do not model a lost thruster's tilt: a three-thruster craft could not hold its attitude
on two without vectoring **[I]**, which is one more reason to keep a crippled Frog on the ground.

### Lift-off check

A flight profile with `liftCheckAtLaunch` is checked by a postfix on Vehicle Framework's
`CompVehicleLauncher.CanLaunchWithCargoCapacity(out string disableReason)`. Every launch path and the launch button ask this
method, which already gives the button its reason (read from Vehicle Framework 1.6, 9 Nov 2026). Under the owner's setting:

| Setting | Lift beyond the engines |
| --- | --- |
| Refuse (default) | No launch; the button says why: unload, repair the engines, or wait for cooler air |
| Allow at a cost | Launches; on Vehicle Framework's `AerialVehicleLaunch` event the turbines lose 10% of their hot-section life (or the engine 8 health without a turbine system) **[A]**, with a message |
| Warn only | Launches, with a message |

Helicopters use momentum theory against the profile's `hoverPowerAtMaxGross` (0.85 for Vehicle Framework's own helicopters **[A]**),
capped by the rotor drive; pods use thrust against weight.

### Diesel-electric

`TransmissionKind.DieselElectric`: the engine drives a generator and traction motors drive the axles, so there is no gearbox
(the British Rail Class 08: an English Electric engine, a DC generator and two traction motors with double-reduction gearing
**[S: Wikipedia, "British Rail Class 08"]**; the EffiShunter 300: a Siemens traction alternator and motors on the axles **[P: CZ
LOKO's product page]**).

- The wheels get the engine's rated power less the generator's and motors' losses (`efficiency`, 0.8 **[A]**) at any speed, but no
  more pull than the motors' maximum (`maxTractiveEffortKilonewtons`). Above that, or past the rails' grip, the speed cap binds,
  with the usual 15% crawl.
- **The engine runs at the power asked of it**, its speed rising from idle to rated with the throttle (a straight line **[A]**; real
  throttles step through notches).
- **Traction motors heat with the square of their pull** over their continuous rating (`continuousTractiveEffortKilonewtons`)
  **[I: resistive heating follows current squared, and pull follows current]**. `PhobosVehicleSystem_TractionMotors` holds their
  temperature, fitted to the short-time ratings in a US Army manual for a 500 hp Baldwin-Lima-Hamilton switcher (TM 55-2210-223-12)
  **[P, as transcribed in our research]**:

  | Current (times continuous) | Manual allows | Our motors reach 180 degrees C, from cold, in |
  | --- | --- | --- |
  | 1,200 A (1) | without limit | never (they settle 130 degrees C over the air) |
  | 1,400 A (1.17) | 20 minutes | about 20 minutes |
  | 1,600 A (1.33) | 10 minutes | about 10 minutes |
  | 1,920 A (1.6) | 5 minutes | about 5.5 minutes |

  The manual then asks for 15 to 30 minutes idling to cool; ours lose most of their heat in about 10 **[I: our fit]**. "Running
  hot" from 155 and burning their windings from 180 **[A: the lines follow insulation thermal classes F and H]**. A rule test holds
  the fit.
- **Steel wheels on rails** roll on 0.002 and grip at 0.25 whatever the ground beneath (`fixedRollingResistance`, `fixedGrip`)
  **[A]** in the archetype. The Goat grips at 0.32, the EffiShunter 300's 113 kN starting pull on 36 t **[I from P]**.

**The Goat DE2's figures** follow the closest real machine found, the two-axle EffiShunter 300 **[P: CZ LOKO]**: 328 kW, 113 kN
starting tractive effort, 36 t. Its continuous pull is not published, so it takes the Class 08's ratio (starting about 3.2 times
continuous, 35,000 against 11,100 lbf **[S: Wikipedia; Keighley and Worth Valley Railway's data file]**): 35 kN **[I]**. It also
has a brake pipe, so it gets the air brakes and an appended compressor, as real locomotives brake with air.

## Settings

The hover-limit setting's label now reads "When an aircraft is too heavy or too hot to lift off or hover onto its pad". No new
setting.

## For modders

- `Engine1` to `Engine8` roles; `RotorDrive` role with `PhobosVehicleSystem_RotorDrive`; `TractionMotor` role with
  `PhobosVehicleSystem_TractionMotors`.
- Turbine block: `transmissionLimit`, `liftToWeightAtMaxGross`; both may be overridden in a loadout's drivetrain `set`.
- Engine: `kind` `Thruster`; `ratedKilowatts` for a real engine's power instead of `kilowattsPerTonne`.
- Transmission: `kind` `DieselElectric`, `maxTractiveEffortKilonewtons`, `continuousTractiveEffortKilonewtons`.
- Running: `fixedRollingResistance`, `fixedGrip`.
- Flight profile: `liftCheckAtLaunch`. Shipped: `PhobosFlightProfile_StockRotorcraft` and `PhobosFlightProfile_StockThrustLift`.
- Part finder: `keyContainsAny` (several hints, any of which will do) for a role made of several kinds of part.
- Inputs: `Input_Turbine` reading `OverTorque`; `Input_TractionLoad`.
- The schema check reads all of these.

## Checked offline

Rule tests (`tests/PhobosFramework.Vehicles.Rules.Tests/PowerAndLiftTests.cs`): a diesel-electric runs its engine at the power
asked, holds a hard start to its motors' pull and heats them, and lets its power set the speed on a grade; the rotor drive caps lift
and marks over-torque; a pod loses lift with each thruster and lifts again once lightened. Schema check fixtures for the new rules.

## To test in game (owner)

1. A laden Louie or Cherokee on a hot day: refused at lift-off with the reason, under the default setting; unloaded, it launches.
2. A Toad with one thruster wrecked launches; with two wrecked and full, it is refused.
3. The Goat pulling hard from a standstill: "running hot", then easing off cools it.
4. Each hover-limit setting once at lift-off.

## Who chose what

- **Owner (9 Nov 2026):** every proposal approved; the boats, the Warbird and the Smuggler left out; the no-engine Defs documented;
  the work split into parts; the L-ATV soon.
- **Research corrections (9 Nov 2026):** the Harrier's lift ladder (64.3 to 95.9 kN) is the Pegasus 11-21's (F402-RR-406), from
  the US Navy's 1986 AV-8B data sheet, not the later 11-61's (up to 106 kN); the loadout says so and keeps the 11-21's ladder.
- **Agent choices, owner to revise:** the lift-to-weight figures for the pods; the appended gearboxes on the Hind and the Mongoose; the Mosquito's turboshaft; the lift-off check as part
  of the hover-limit setting; the strain costs; the traction motors' heating and cooling rates and lines; rail friction; the
  placeholder drive limits until sourced.
