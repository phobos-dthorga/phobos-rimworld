# Avionics for every aircraft (13 Nov 2026)

**Status (20 Nov 2026):** built in D'thorga Framework 0.106.0 (A5, A6 with round three's G16, A8, A9), 0.110.0 with Vehicles
0.53.0 (the avionics fit, A1, A2, A7 and L1; A5 also answers L2), 0.119.0 with Vehicles 0.58.0 and Patches 0.21.0 (A12, A14 with L3, A16)
and 0.180.0 with Vehicles 0.89.0 (A16's cartridges in a magazine, topped up by haulers);
not built: A3, A4, A10, A11, A13, A15, A17 and L4 (L5 set aside; L6's stale notes corrected, with a reading of 17 Nov 2026). Not yet tested in game.

A research record. The owner, 13 November 2026: "One area I have seen that
is lacking is with the avionics of aircraft, particularly with the Harrier, but this does apply to all aircraft that are part of my
game, in general." It is the second of three records of the cross-links round three review. It follows
[the authors' override contract](vehicle-author-overrides.md), whose `avionics` block it fills in, and is followed by [cross-links,
round three](vehicle-crosslinks.md#vehicle-cross-links-round-three-the-crew-weather-and-ground-combat-damage-electrics-and-hydraulics-13-nov-2026), which covers ground vehicles and boats.

How to read it:

- Facts marked "from the code" were read on 13 Nov 2026 with `scripts/query-code.py` from our decompiles and are summarised in our
  own words. `VF` is Vehicle Framework 1.6 (`VehicleFramework-1.6:Vehicles.decompiled.cs`), `FWA` the fixed-wing framework
  (`FixedWingAircraftFramework.decompiled.cs`), `AV8B` the Harrier mod (`AV8BHarrier.decompiled.cs`), `VTOL` VTOL Vehicle
  (`VTOLVehicle.decompiled.cs`), `all.cs` the game.
- Real-world figures carry a label: **[P]** a primary source, **[S]** a secondary one, **[I]** our inference, **[A]** authored for
  play. Every figure without a label is authored.
- Cost classes, as in the earlier rounds: **XML** (Defs only), **input** (one small input or condition class), **hub** (a change in
  the systems comp or a shared service), and one new class, **hook** (a patch into another author's code, named each time).
- Every name of a Def, field or class under "The fit" and in the proposal tables is **proposed**.

## Read this first

- **Today avionics is one electrical load.** Every aircraft draws 15 A for "avionics" while its engine runs
  (`Loadouts_Default.xml:871-876`), and nothing depends on it. No instrument, sensor, flight-control hydraulics or oxygen exists
  in our systems.
- **The proposal is a small "fit"**: each aircraft carries some of nine capabilities (attitude instruments, navigation, autopilot,
  radar, a sight, a night sight, a warning receiver, countermeasures, oxygen). Each sits on a part, draws current and can be shot
  out or lose its power. **What a capability does is change an outcome the player already cares about**: whether the aircraft may
  launch, how fast and far it flies, how hard it lands, how well it aims and evades.
- **Most of the first batch needs no patch into anyone's code.** The game's own darkness and weather are the inputs; our launch
  check, our stat part and the world-flight speed patch we already have are the outputs.
- **Scaling other authors' weapons is possible and stays small**, as the owner allowed: one postfix scales the Harrier's evasion;
  bomb and rocket aim need three more hooks and come later.
- **One fault is folded in.** The Harrier's flight-control rule today grounds other authors' aircraft by accident; proposal A8
  replaces it with one deliberate rule in the Framework.
- **A correction to the survey**: the Harrier's "hit spread" is how a hit on the jet is shared among its parts, not how its weapons
  aim. It is left alone.
- **Five questions for the owner** are at the end.

## The aircraft today

Eleven aircraft are on the owner's list. "Hooks" are parts the author wrote that nothing of ours binds yet.

| Aircraft | What it is, in the real world's terms | Flown by | Hooks free today | What our loadout gives it |
| --- | --- | --- | --- | --- |
| Frog (`VVE_Frog`) | A one-seat thrust pod; nothing real | Vehicle Framework | Stabilizers | Engine and three thrusters; no turbine |
| Toad (`VVE_Toad`) | A larger thrust pod | Vehicle Framework | None suited | Engine and four thrusters |
| Goliath (`VVE_Goliath`) | A heavy siege pod on boosters | Vehicle Framework | None suited | Engine and four thrusters |
| Mosquito (`VVE_Mosquito`) | A light scout helicopter | Vehicle Framework | **Dashboard, Servos**, Gearbox, Rudder | Engine, rotor drive |
| Smuggler (`VVE_Smuggler`) | A small cargo plane | Vehicle Framework | **Dashboard, Servos**, Rudder, wings | The generic aircraft loadout only |
| Warbird (`VVE_Warbird`) | A piston fighter in the P-51's mould | Vehicle Framework | **Dashboard, Servos**, Rudder, wings | The generic loadout, which gives it a turbofan (a fault, row L5) |
| Louie (`VVE_Louie`) | A utility helicopter, VVE's nod to the UH-1 | Vehicle Framework | **Dashboard, Servos**, Gearbox, Rudder | Engine, rotor drive, hook, cabin |
| Cherokee (`VVE_Cherokee`) | A tandem-rotor transport in the CH-47's mould | Vehicle Framework | **Dashboard, Front Servos, Rear Servos**, Gearbox | Two turboshafts, rotor drive, hook, cabin |
| Hind (`Hind_Helicopter`) | The Mi-24 gunship | VTOL Vehicle | **Cockpit** only | Two engines, rotor gearbox, hook, cabin |
| Mongoose (`Mongoose_AttackHelicopter`) | A fictional attack helicopter | VTOL Vehicle | **Cockpit** only | Engines, rotor gearbox |
| Harrier (`Harrier_Jet`) | The AV-8B | The fixed-wing framework | **Cockpit**, and our own **avionics, nose radar, hydraulics, wiring** parts (D'thorga Patches) | Turbofan with wet ratings, injection water |

From the code, what is and is not there:

- **Modelled for aircraft**: the turbine's temperature, ratings and hot-section life; hot and hung starts; engines out as lost
  power; rotor over-torque; the hover margin and lift checks; bleed air; one battery and a generator; cab temperature
  ([turbine design](vehicle-turbine-design.md), [power and lift](vehicle-power-and-lift-design.md)).
- **Vehicle Framework's `FlightControl` stat is read by nothing in its own code** (it is named only at `VF:18254` and
  `VF:51125`). Our Harrier patch is the one reader (`src/PhobosPatches.Harrier/FlightControlRule.cs:17-39`).
- **Every Vehicle Framework stat already takes our stat part** (`src/PhobosFramework.Vehicles/Service/VehicleSystems.cs:229`),
  so a stage's `Effect_StatFactor` can scale `FlightSpeed` or `FlightControl` in XML alone.
- **World flight speed and fuel already have a hook of ours**: postfixes on `CompVehicleLauncher.FlightSpeed` (`VF:18422`) and
  `FuelConsumptionWorldMultiplier` (`VF:18424`), written for the sling (`src/PhobosFramework.Vehicles/Sling/SlingPatches.cs:51`,
  `:65`). Vehicle Framework's own `fuelEfficiencyWorldModifier` is set back to nothing each time the aircraft is put on a map
  (`VF:18893`), so it cannot carry a lasting factor.
- **Launch refusals already pass through one postfix of ours** on `CompVehicleLauncher.CanLaunchWithCargoCapacity`
  (`src/PhobosFramework.Vehicles/Service/Patches.cs:238`).
- **The game already measures visibility**: `WeatherManager.CurWeatherAccuracyMultiplier` (`all.cs:430034`) is what fog and rain
  do to shooting, and Vehicle Framework applies it to its turrets (`VF:62443`). Our `When_Night` reads the hour
  (`src/PhobosFramework.Vehicles/Schema/Conditions.cs:65-76`).
- **No aircraft framework has real height.** VTOL Vehicle draws an aircraft higher and larger and nothing more; the world map has
  no altitude at all. Anything about altitude must be said by flight phase.
- **No ejection exists in any of the four code bases** (searched). A VTOL aircraft that cannot land is destroyed where it is
  (`VTOL:6266-6270`, from `ResolveEmergencyLanding`, `VTOL:6025-6081`); a crash on the world map becomes Vehicle Framework's
  shuttle-downed site (`VF:77809`).
- **Anti-air fire in the owner's game is ordinary shooting**: pawns, turrets and Combat Extended's weapons aimed at an airborne
  aircraft. VTOL Raids brings raiders in aircraft with their own combat AI. Vehicle Framework's world-map flak exists but nothing
  calls it (`RegisterAerialVehicle`, `VF:90047`, has no callers).

## The fit: how an aircraft's avionics are described

Nine capabilities, each one thing a crew either has or lacks. A capability **works** when its part is healthy enough, its load has
power, and it is switched on. One new system, `PhobosVehicleSystem_Avionics`, holds a hidden reading for each (part efficiency
times "powered"), with three named stages: `working`, `degraded` (below 0.6) and `out` (below 0.2). Named stages are what the
override contract asks for.

| Capability | What it stands for | Levels | Draws (proposed, at 24 V) | Kept on the battery when the generator is lost? |
| --- | --- | --- | --- | --- |
| `Attitude` | Attitude, heading and airspeed instruments | Basic | 2 A | Yes |
| `Navigation` | Basic: compass and radio aids. Full: inertial or Doppler navigation and a moving map | Basic, Full | 2 A, 6 A | Basic yes, Full no |
| `Autopilot` | An autopilot or stability augmentation | Basic | 4 A | No |
| `Radar` | A weather or attack radar | Basic | 20 A | No |
| `Sight` | A gunsight or bombing sight | Basic, Full | 1 A, 5 A | Basic yes |
| `NightSight` | Infrared sensor or night-vision fit | Basic | 4 A | No |
| `Warning` | A receiver that tells the crew they are being tracked or shot at | Basic | 2 A | Yes |
| `Countermeasures` | Chaff and flare dispensers | Basic | 1 A | Yes |
| `Oxygen` | Crew oxygen for a high cruise | Basic | 2 A | Yes |

- **Levels, not eras.** Round one set aside "era-specific electrics" because loadouts do not know an era
  ([round one](vehicle-crosslinks.md#vehicle-system-crosslinks-research-and-record-9-nov-2026)). Two levels say enough: a Warbird's gunsight is Basic, a Harrier's is Full.
- **The amps replace the flat 15 A.** An aircraft's avionics draw is the sum of what it carries: a Mosquito about 4 A, a Harrier
  about 45 A. All authored; the direction is real (a radar is the heavy load).
- **Where a capability sits.** On the author's own part where one suits (`Dashboard`, `Servos`, `Cockpit`), on our Harrier parts,
  or on one appended "avionics bay" part where the aircraft has none (question 2).

### The override Def's `avionics` block (proposed fields)

This fills the placeholder in [the override contract](vehicle-author-overrides.md).

| Field | Meaning |
| --- | --- |
| `noAvionics` | The aircraft takes no avionics from us: no fit, no loads, none of the rules below |
| `fit` | A list. Each entry: `capability` (a name from the table), `level` (`Basic` or `Full`), `part` (the key of one of the vehicle's own parts) or `role` (a part role), `amps`, `essential` (stays on the battery), and `drop` to remove a capability our default gave |
| `cruisesHigh` | The aircraft cruises where the crew need oxygen (row A14) |
| `ejects` | The crew have ejection seats (row A17, if built) |
| `factors` | Per capability, a `factor` on what its loss costs, or `off` for one rule, addressed like any cross-link: system `PhobosVehicleSystem_Avionics`, the capability's quantity and the stage name |

```xml
<avionics>
  <fit>
    <li><capability>Attitude</capability><part>InstrumentPanel</part></li>
    <li><capability>Navigation</capability><level>Full</level><part>InstrumentPanel</part><amps>5</amps></li>
    <li><capability>Autopilot</capability><role>FlightControls</role></li>
    <li><capability>Radar</capability><drop>true</drop></li>
  </fit>
  <cruisesHigh>false</cruisesHigh>
</avionics>
```

### Agent-proposed defaults for the eleven

By what each aircraft is. **Every aircraft gets at least `Attitude`**, so nothing that flies today is grounded on the day this is
installed (question 5). Sources for the real aircraft are under "Sources"; the VVE aircraft are fictional and their fits are
authored.

| Aircraft | Attitude | Navigation | Autopilot | Radar | Sight | Night sight | Warning | Counter-measures | Oxygen | Flight hydraulics on |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| Frog, Toad, Goliath | Yes | Basic | | | | | | | | None (thrusters) |
| Mosquito | Yes | Basic | | | | | | | | Servos |
| Smuggler | Yes | Basic | | | | | | | | Servos |
| Warbird | Yes | Basic | | | Basic | | | | | Servos |
| Louie | Yes | Basic | | | | | | | | Servos |
| Cherokee | Yes | Full | Yes | | | | | | | Front and Rear Servos (two systems) |
| Hind | Yes | Full | Yes | | Full | | Yes | Yes | | Appended, or Cockpit |
| Mongoose | Yes | Full | | | Full | Yes | Yes | | | Appended, or Cockpit |
| Harrier | Yes | Full | Yes | Yes | Full | Yes | Yes | Yes | Yes | Our hydraulics part |

## 1. Instruments and navigation

**The real basis.** An aircraft flown without sight of the ground needs gyroscopic attitude and heading instruments, and the
United States' rules require them for instrument flight **[P]** ([14 CFR 91.205](https://www.ecfr.gov/current/title-14/chapter-I/subchapter-F/part-91/subpart-C/section-91.205);
the FAA's handbook chapter on [flight instruments](https://www.faa.gov/sites/faa.gov/files/10_phak_ch8.pdf) covers the
pitot-static, gyroscopic and compass instruments). A pilot without them in cloud or darkness loses control through spatial
disorientation: in a 1954 University of Illinois experiment every one of twenty pilots untrained on instruments reached a dangerous
attitude, in between 20 and 480 seconds **[S]** (the well-known "178 seconds" is that experiment's average and is disputed as a
survival figure; [AVweb](https://avweb.com/flight-safety/technique/surviving-vfr-into-imc/)). An autopilot "can meaningfully cut
workload during critical phases of flight" **[P]** ([FAA Advanced Avionics Handbook, chapter 4](https://www.faasafety.gov/files/events/EA/EA03/2019/EA0392003/aah_ch04.pdf)).
The Night Attack Harrier added an infrared sensor, night-vision goggles and a digital moving map, and the cockpit carries an
inertial navigation system **[S]** ([Naval Technology](https://www.naval-technology.com/projects/av8b-harrier-ii-plus/)).

**"Poor visibility"** below means night by our rules' hours, or weather whose accuracy multiplier is under 0.8 (fog and heavy rain
in the base game), read from what the weather does and never from its name. On the world map there is no weather between maps, so
only the map an aircraft leaves and the map it arrives on count (a limit, said plainly).

| # | Link | Reads and changes | Lets the player | Stops them | Changes how they play | Cost | An override can | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A1 | **Blind flying needs instruments** | `Attitude` and poor visibility on the aircraft's map → the launch is refused with the reason (or allowed with a warning, by a setting, as the hover limit is) | Fly at night and in fog with working instruments | Launching into a foggy night with the instrument panel shot out or unpowered | Instruments become a part worth repairing first; a damaged aircraft waits for daylight | **input** (one condition, `When_PoorVisibility`) and **hub** (our launch check takes a list of refusals) | Drop `Attitude`, switch the rule off, or scale nothing (it is yes or no) | **Take, first batch** |
| A2 | **Navigation keeps a flight on track** | `Navigation` out, or Basic in poor visibility → world flight speed ×0.85 and fuel ×1.15 (a wandering track); Full navigation flies straight in anything | Fly long legs at night at full speed with a moving map | Nothing outright | Range at night depends on the fit; a hit to the panel can turn a round trip into a one-way one | **hub**: one `FlightFactors` service that the sling's two patches fold into, so every factor on flight speed and fuel multiplies in one place | Change the level, the factors, or switch it off | **Take, first batch** |
| A3 | **An autopilot eases the landing and the long leg** | `Autopilot` working → A4's rough-landing chance halved, and A2's wandering halved when navigation is only Basic | Land a tired or damaged aircraft with less risk | Nothing | A reason to fit one to a transport, and a part to protect | **XML** once A2 and A4 exist | Add or drop it; scale it | **Take with A4**, not alone: by itself it changes nothing |
| A4 | **Rough landings** | Arriving on a map in poor visibility without `Attitude`, or with flight control below half (A8) → the landing gear, skids or airframe take a share of damage, with a message | Risk a landing when it matters | Landing a crippled aircraft for free | The choice between waiting, diverting and risking the airframe | **hub**: handled at the aircraft's first tick after it is put on a map, as the sling's set-down is (never inside its spawning) | Scale the damage; switch it off | **Take, second batch**. Vehicle Framework's own aircraft only at first; the Harrier and the VTOL helicopters have landing rules of their own |

## 2. Power for the aircraft

**The real basis.** A generator is an engine-driven accessory, and when it fails the battery carries on alone: certification rules
require a battery to supply the loads essential to continued safe flight and landing for at least 30 minutes after the generating
system is lost, including the time the pilot needs to notice and shed loads **[P]** ([14 CFR 23.1359](https://www.govinfo.gov/content/pkg/CFR-2012-title14-vol1/pdf/CFR-2012-title14-vol1-sec23-1359.pdf));
real batteries often give less than their rating **[S]** ([EAA](https://inspire.eaa.org/2025/10/22/all-amped-up/)). A helicopter
with failed hydraulics has heavy, stiff cyclic and collective controls and is landed with a run-on landing **[P]**
([Robinson R66 handbook, section 3](https://robinsonheli.com/wp-content/uploads/2021/08/r66-poh-3.pdf); the FAA's
[Helicopter Flying Handbook, chapter 11](https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/helicopter_flying_handbook/hfh_ch11.pdf)
is the chapter on such emergencies). The CH-47 has two independent flight-control hydraulic systems, and its flight control
system works through them **[P]** ([CH-47 technical manual excerpt](https://chinookhelicopters.tpub.com/TM-55-1520-240-23-6/css/TM-55-1520-240-23-6_18.htm)),
which is why the Cherokee's two servo parts suit two systems.

| # | Link | Reads and changes | Lets the player | Stops them | Changes how they play | Cost | An override can | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A5 | **The generator follows the engine** | Aircraft generator output from the turbine's spool speed, not the piston-engine rpm curve that a turbine always tops out (`Systems_Electrical.xml:223-233`): about half at idle, full from cruise | Nothing new | Running every load for ever at ground idle | Idling on the pad with heater, radar and lights slowly drains the battery, as a truck's does | **XML** (an aircraft electrical system that inherits ours with a different supply factor, as the electric vehicles' does) | Set the curve or the generator's amps | **Take, first batch**: it mends a loose end **Built: Framework 0.106.0 (13 Nov 2026), not yet tested in game.** |
| A6 | **A lost generator starts a clock** | Generator out (its part, or every engine) → loads not marked essential drop off by themselves; the battery, sized for about 30 minutes of essential loads, runs down; flat, every capability reads `out` and A1 and A2 apply | Fly home on the battery after a hit | Staying up indefinitely with no generator | A generator hit is a decision: land now, or run for home against the battery | **XML**: a derived "generator supplying" reading, `when` conditions on the loads, and the aircraft's battery figures | Mark loads essential or not; size the battery | **Take, first batch** **Built: Framework 0.106.0 (13 Nov 2026), not yet tested in game.** |
| A7 | **Flight controls need hydraulics** | Aircraft with boosted controls get our existing hydraulics system (`Systems_Fluid.xml:107`) bound to `Servos` or the Harrier's hydraulics part: hits drain the fluid; low pressure → `FlightControl` ×0.5 | See why the controls are heavy, and top the fluid up | Flying on as if nothing happened with the lines shot through | Hydraulic fluid joins the pre-flight; a leak is a reason to land soon (the danger clock already shows leaks) | **XML** (the system exists; loadout bindings and one stage) | Say which part carries it, or that the controls are manual (`drop`) | **Take, first batch** |
| A8 | **Flight control means something, on every aircraft** | One rule in the Framework: `FlightControl` under 0.35 refuses the launch with the reason (the Harrier's figure today); between 0.35 and 1, world flight speed falls with it (to ×0.8) and A4's landing risk rises | Know before take-off that the aircraft will handle badly | Launching with wrecked controls | Control surfaces, servos and wiring become targets that matter on every aircraft, not only the Harrier | **hub**; it **replaces** `src/PhobosPatches.Harrier/FlightControlRule.cs`, which today applies to any aircraft with a flight-control part whenever the Harrier mod is active and so can ground the Frog, Toad, Smuggler, Warbird, Cherokee and Goliath by accident | Set the floor, scale it, or switch it off | **Take, first batch**: it also mends that fault **Built: Framework 0.106.0 and Patches 0.19.0 (13 Nov 2026), not yet tested in game; the Harrier-only rule is removed.** |
| A9 | **An engine out is felt** | The `enginesOut` stage, a label with no effect today (`Systems_Turbine.xml:126-143`), gets a message and `FlightSpeed` ×0.8, and takes its share of generator output with it (A6) | Limp home on one engine | Cruising at full speed on half the engines | A twin is worth its second engine | **XML** | Scale it | **Take, first batch**. The Hind's own code already slows it to 0.8 on the map **Built: Framework 0.106.0 (13 Nov 2026), not yet tested in game.** |

## 3. Sensors and weapons aiming

**The real basis.** The first AV-8Bs aimed with the angle-rate bombing system, a laser spot tracker and television tracker; the
Night Attack variant added an infrared sensor and night-vision goggles; the Plus variant added the APG-65 radar, described as an
all-weather detection and tracking radar **[S]** ([Naval Technology](https://www.naval-technology.com/projects/av8b-harrier-ii-plus/);
[Naval History and Heritage Command](https://www.history.navy.mil/research/histories/naval-aviation-history/naval-aircraft/current-aircraft-inventory/av-8b-harrier-ii.html)).
Sources disagree on which nose sensor the radar displaced, so nothing here depends on that. The Mi-24 carries an ASP-17 gunsight
and the Raduga-F unit for its guided missiles **[S]** ([Aerospaceweb](https://aerospaceweb.org/aircraft/helicopter-m/mi24)).

**What can be scaled, from the code.** The owner allowed our conditions to make another author's weapons better or worse through
hooks that never skip their code.

| Figure | Where | How | Risk |
| --- | --- | --- | --- |
| The Harrier's flight evasion (0.4) | `AV8BHarrier.Evasion.ChanceFor`, public and static, `AV8B:1802-1817`; both its Combat Extended and its base-game paths ask it | A postfix that scales the result | Low: the easiest hook of all |
| Bomb and rocket aim points | `FixedWingAttackRunUtility.BombAimPoint`, public and static, `FWA:1686-1708` | A postfix that moves the point returned | **To check first**: it returns a Unity vector, and that framework is built against another Unity; Harmony may refuse the type |
| Bomb scatter | `forcedMissRadius`, read in `VtolVehicleTurret.FireTurret`, `FWA:20681` | Swap the fire mode's figure for the call and restore it after | Medium: the fire mode is shared by every Harrier |
| Rocket spread under Combat Extended | Vehicle Framework's public delegate `VehicleTurret.LookupProjectileCountAndSpreadCE`, `VF:62870` | Wrap it and scale the spread it returns | Medium: it is one delegate for every vehicle, so the wrapper must check whose shot it is |
| Vehicle Framework turrets (its aircraft, and the VTOL helicopters' own path) | The weather factor in `TurretShotReport.HitReportFor`, `VF:62443`; the field is private (`VF:62337`) | A postfix that changes the report | Medium: a private field by name, checked by `scripts/check-targets.py` |
| The VTOL helicopters' speed on a map | `AircraftProperties.moveSpeed`, read at `VTOL:5104`, `:5126`, `:6106` | Swap and restore round the movement tick, as the Hind's own mod does | Medium |

**Left alone:** the Harrier's "hit spread" (`AV8B:2737-2781`). It decides which of the jet's parts a hit lands on. It is the
author's damage model, not an aiming figure, and nothing of ours should scale it.

| # | Link | Reads and changes | Lets the player | Stops them | Changes how they play | Cost | An override can | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A10 | **A sight aims the weapons** | `Sight` out → miss radius and spread ×1.5; degraded ×1.25; a Full sight working is today's accuracy | Keep attacking with a damaged sight, at a cost | Pin-point bombing with the sight shot out | The cockpit and nose become what an attack aircraft's worth hangs on | **hook** ×3 (bomb aim, bomb scatter, rocket spread), plus the Vehicle Framework turret report for the others | Set the level, scale it, switch it off | **Take, second batch**, one framework at a time, after the Unity-type check |
| A11 | **Night and bad weather blunt an attack unless the aircraft can see** | In poor visibility, air-to-ground accuracy ×0.7 unless `NightSight` (night) or `Radar` (weather) works. Vehicle Framework's turrets already lose accuracy to weather; a working radar would give half of it back | Attack at night with the right fit | Day-fighter accuracy at midnight | The Harrier and the Mongoose earn their night sights; the Warbird is a day aircraft | **hook**, the same ones as A10 | As A10 | **Take with A10** |
| A12 | **Evasion depends on controls and warning** | The Harrier's evasion × flight control (A8) × 1.0 with `Warning` working, 0.75 without | Dodge as well as today in a sound jet | Dodging fire in a jet with shot-up controls | Damage compounds: a jet hit in the hydraulics is easier to hit again | **hook** (one postfix on `Evasion.ChanceFor`) | Scale it, switch it off | **Take, first among the hooks**: smallest and clearest. The VTOL helicopters have no evasion figure to scale |
| A13 | **Radar as a strike warning** | A working radar on a parked or airborne aircraft warns of an inbound strike a little earlier | An earlier warning | Nothing | An aircraft on the pad doubles as a sentry | **job**, in the strike code | Drop the radar | **Not here**: it belongs with the radar already designed for [cross-map strikes](cross-map-strikes.md) and the [Seabed Crawler](vehicle-seabed-crawler.md); raise it there |

## 4. Crew survival and defences

**The real basis.** Without extra oxygen a crew may not stay above 12,500 ft cabin altitude for more than 30 minutes, or above
14,000 ft at all **[P]** ([14 CFR 91.211](https://www.law.cornell.edu/cfr/text/14/91.211)), and the time a person stays useful
shrinks from tens of minutes near 18,000 ft to under a minute at 35,000 ft **[S]** ([FAA Advisory Circular 61-107B](https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_61-107B.pdf)
holds the official table; the figures quoted are from secondary summaries). The AV-8B was the first United States production
combat aircraft with an on-board oxygen generator, which sieves engine bleed air **[S]**
([MiGFlug](https://migflug.com/afterburner/obogs-fighter-pilot-oxygen-why-not-pure-oxygen/)). It carries an AN/ALR-67 warning
receiver and AN/ALE-39 chaff and flare dispensers **[S]** (a secondary description found by search, among
[War History](https://warhistory.org/2020/07/16/mcdonnell-douglas-av-8b-harrier-ii/) and
[Warrior Lodge](https://warriorlodge.com/pages/mcdonnell-douglas-av-8b-harrier-ii); which page carries the sentence was not confirmed),
and an SJU-4 ejection seat built to work down to the hover **[S]** ([The Ejection Site](https://ejectionsite.com/av8bseat.htm)).
The Mi-24 carries a warning receiver, an infrared jammer and an ASO-2 dispenser **[S]**
([Aerospaceweb](https://aerospaceweb.org/aircraft/helicopter-m/mi24)).

| # | Link | Reads and changes | Lets the player | Stops them | Changes how they play | Cost | An override can | Verdict |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| A14 | **No oxygen, no high cruise** | An aircraft marked `cruisesHigh` with `Oxygen` out (or its engine's bleed lost) flies its world leg low: speed ×0.85, fuel ×1.2, through A2's `FlightFactors` | Get home without oxygen | The fast, thrifty high cruise without it | The Harrier's oxygen set is one more thing a hit can take; helicopters never notice | **XML** once A2's service exists | Say whether the aircraft cruises high; scale it | **Take, second batch**. Crew hypoxia is question 3 |
| A15 | **A cold cockpit up high** | In world flight an aircraft that cruises high takes a colder outside temperature in the cab's `outside` flow; cabin bleed (the cab heater load) covers it while an engine runs | Nothing new | A warm cockpit with the engines out at height | The crew feel a flame-out through the game's own cold, as a cab's passengers already do | **input** (flight phase readable in XML, row L3) | Switch it off | **Second batch, small.** No new condition: the game's hypothermia does the rest |
| A16 | **Countermeasures** | Cartridges made at the parts bench and loaded aboard; a button spends some for a short evasion bonus while under fire | Buy a safer pass over a defended target | Using them for nothing: they run out | A consumable to stock before a strike | **hook** (the same evasion postfix) and **job** (the item and the button) | Drop the capability | **Later.** Real dispensers answer missiles, and the anti-air in this game is guns and rockets, so the honest effect is small. Only where an evasion figure exists (the Harrier) |
| A17 | **Ejection** | An aircraft marked `ejects`, destroyed while airborne on a map, puts its crew down nearby, hurt but alive | Lose the jet and keep the pilot | Nothing | Pilots become worth more than airframes | **hook**, into each framework's crash path | Mark an aircraft as having seats | **Read first.** What happens to a crew today when a VTOL helicopter or the Harrier is destroyed in the air was not traced, and Vehicle Framework's world crash already makes a site with the crew. Not proposed until that is known |

**Revisited on 13 Nov 2026** in [air-defence tie-ins](air-defence-tie-ins.md): The Dead Man's Switch - Expedition has chaff, fire-control
lock and seekers, so A16 has an opponent once our aircraft are visible to its air defence (AD3); A12 gains radar-directed guns (AD1); A13
gains a way in through AD8 and AD9.

## Loose ends found on the way

| # | Loose end | Proposal | Cost | Verdict |
| --- | --- | --- | --- | --- |
| L1 | A rating's time limit (`minutes`) is stored and checked for being present (`PowertrainDefs.cs:21`, `:144`) but never enforced | Time spent above maximum continuous beyond the limit uses hot-section life twice as fast, with one message | **hub** (one counter) | **Take**: it makes "take-off power for five minutes" mean something |
| L2 | The aircraft generator always gives full output | Row A5 | XML | Take |
| L3 | Flight phase, airborne or not, and the bleed share cannot be read from XML | `When_FlightPhase`, `When_Airborne`, `Input_Bleed` | **input** | **Take** when A14 or A15 is: they need it, and authors will too |
| L4 | Aircraft carry no oil or fuel-feed systems, so a turbine has no oil to lose and no boost pump to fail | Give turbines the lubrication system with their own figures | XML | **Set aside**: more upkeep on every aircraft for little the player would notice; cold starts already fall back sensibly |
| L5 | The Warbird and the Smuggler get the generic aircraft loadout, so a piston fighter is given a turbofan | A piston-aircraft loadout: the piston drivetrain, cooling and oil, and a propeller | XML | **Take**: it is a fault, and it makes the Warbird feel like one |
| L6 | The Harrier README's part health figures are stale, and the hit-share note in `Parts_HarrierVtol.xml:9` probably is (inference from `AV8B:2690-2734`) | Correct them when A8 touches those files | Docs | **Done:** the README in Patches 0.19.0; the note on 17 Nov 2026, with a finding ([below](#l6-read-our-ten-parts-thin-out-hits-on-the-harriers-own-17-nov-2026)) |

### L6 read: our ten parts thin out hits on the Harrier's own (17 Nov 2026)

Read at the owner's word from the Harrier's assembly of 16 Nov 2026 (`AV8B`, decompiled that day; its build of 3 Nov has none of
this) and Vehicle Framework 1.7. **Nothing is changed in play by this reading; the choice at the end is the owner's.**

- **The Harrier now decides where a hit lands itself.** A prefix on Vehicle Framework's `VehicleStatHandler.ApplyDamage` (`AV8B:2839-2857`)
  moves every hit but EMP: with the Def's `hitSpreadChance` (0.4 on `Harrier_Jet`) onto the cells of **one part other than the airframe,
  chosen evenly** among every living part with cells of its own (`AV8B:2764-2806`); otherwise onto an airframe cell. At start-up it also
  takes out of the airframe's cells every cell a smaller part covers, internal parts excepted under Combat Extended (`AV8B:2715-2762`).
- **So the old note was wrong since that build**: the airframe's weight of 3 no longer shares a cell with an external part.
- **The finding: "evenly" counts our parts.** The Harrier's own list has three such parts (cockpit, engine, wing fuel tanks), so its
  author's design sends about 13 % of all hits at each. Our patch adds ten, so thirteen share the four in ten: **about 3 % each**. The
  engine and the wing tanks, with their leak and fire, are struck about a quarter as often as the author intended; canopy, radar, pylons
  and the rest soak up the difference. The jet is harder to bring down than its author made it, and has more small repairs.
- **Not checked in play.** Whether it shows depends on the fire it takes; the arithmetic is from the code.
- **Choices, none taken:** leave it (a fuller parts list that also spreads damage, as a real airframe's systems would); give fewer of
  our parts cells of their own, so they are reached only through the airframe; or ask the author for a weight on the spread (the code
  takes none today). The author is active (updated 15 Nov 2026), so by our rule a request, not a patch.

## How it shows

- **Plain mode**: words only, on the inspect line and the Dashboard lamps: "Instruments out: it cannot fly at night or in fog",
  "On the battery: about 20 minutes of instruments left", "Controls heavy: hydraulic pressure low".
- **Operator**: adds the amps and the minutes on the battery, and names each capability that is degraded.
- **Engineer**: each capability's reading, its part and its draw.
- **The Systems tab** gains one avionics card: a row per capability with a lamp, opening to its part and its load, as the other
  cards do. The launch button and the world-map destination range already show refusals and the range, so the new factors appear
  there without new interface.
- Stages that should reach Plain mode are marked `notable`; each capability takes one of the dashboard warning pictures or words
  until one exists.

## Fittings and upgrades that follow (ideas, not proposals)

Beside other authors' upgrade trees, as our fittings are: an autopilot kit; a navigation set (Basic to Full); a night sight; a
weather radar for transports; a warning receiver; a second battery for the essential bus. Each would be a capability added to the
fit, on the avionics part.

## Other factions' aircraft

| Where | What the fit does there | Lets the player | Stops the player |
| --- | --- | --- | --- |
| **A raid on us** (VTOL Raids' aircraft) | Raiders' aircraft run the lite tier: a capability reads from its part's health alone, with no battery or generator worked out. A gunship hit in the cockpit aims worse once A10 exists; it is never refused a launch (the rule from the other-factions audit) | Blunt an attack by hitting the cockpit | Nothing |
| **Parked at a base we attack** | Nothing runs for a parked aircraft | Nothing | Nothing |
| **Captured** | It becomes ours with whatever is broken: an aircraft taken with its panel shot out cannot be flown home at night | Capture an aircraft and fly it out by day | A free night escape in a wreck |

Cost with several on a map: nothing each tick for any of them.

## Cost

- **Nothing runs each tick.** Capabilities are derived readings worked out on the systems' rare tick, as every other reading is.
- **Launch and landing rules run at those events.** The world-flight factors are read when Vehicle Framework asks for the speed or
  the fuel rate, from values cached at the rare tick.
- **Weapon hooks run per shot or per bomb**, and each begins by asking whether the shooter is an aircraft with a fit.
- The whole of it sits inside the vehicle model's budget in [the health design](vehicle-health-design.md).

## Argued against

- **A full electrical bus model** (main, essential and battery buses, breakers, inverters). The player would see one more diagram
  and no new decision. "Essential or not" on each load gives the one decision that matters.
- **Failures of single instruments the player never sees** (a toppled gyro, a blocked pitot tube, icing). Each is real; none would
  change what the player does beyond what "instruments degraded" already says.
- **Height as a number.** No framework has it, and inventing it would be a second model beside theirs. Flight phase says enough.
- **Scaling the Harrier's hit spread.** It is the author's damage model.
- **A second smoke, weather or blast model.** Atmosphere Overhaul and Combat Extended own those; we read the game's weather only.
- **Radar as a detection game on a map.** The game shows the player everything on a map already; a radar that reveals what is
  visible would be decoration.
- **Era tiers.** Two levels are enough, and loadouts do not know an era.

## Order to build

1. **Power first, all XML**: A5 (generator follows the engine), A6 (essential loads and the battery clock), A9 (engine out), with
   the fit and the avionics system they need. Nothing an author's code is touched.
2. **Controls**: A7 (flight hydraulics) and A8 (one flight-control rule in the Framework, replacing the Harrier's, which mends the
   accidental grounding).
3. **Blind flying**: A1, then A2 with the `FlightFactors` service.
4. **The first hook**: A12, the Harrier's evasion.
5. **Loose ends**: L5 (a piston loadout for the Warbird and Smuggler) and L1 (rating time limits).

Second batch: A4 and A3 (landings and the autopilot), A14 and A15 with L3 (oxygen and the cold cockpit), then A10 and A11 (aiming)
one framework at a time. Later: A16. Not until read: A17.

## Built (13 Nov 2026, the owner's step 4)

**Framework 0.110.0 and Vehicles 0.53.0, not yet tested in game:** the fit (an `avionics` block on the loadout, `AvionicsRule`, every
aircraft's fit as in the table above, loads drawing what the fit carries, the appended avionics bay), `PhobosVehicleSystem_Avionics`
with A1 (blind take-offs refused, a warn-only setting) and A2 (navigation, with one fuel factor folded into the sling's patch; speed
through the `FlightSpeed` stat), A7 (`PhobosVehicleSystem_FlightHydraulics` on the servo aircraft and the Harrier) and L1 (rating time
limits, hot-section life twice as fast past them). **Narrowed:** only attitude and navigation have readings yet; the other seven
capabilities draw their power and wait for their rules. The Hind, Mongoose and thrust pods have no hydraulics yet (no part to bind).
**Set aside: L5.** It is not XML as recorded: our aircraft model samples every aircraft engine as a turbine, so a piston aircraft
needs its own engine sampling (power and rpm by flight phase, for cooling and oil) before its loadout can mean anything. The Smuggler
and Warbird got loadouts of their own for their avionics and hydraulics, and keep the turbofan until then.

**Owner, 13 Nov 2026:** looking for oxygen on a Harrier after this install, the owner chose to **fold A14 (with L3) into step 5**, the
air-combat batch (AD3, AD2, A16), so oxygen arrives in the same install.

## Built (17 Nov 2026, the aircraft batch)

**Framework 0.119.0, Vehicles 0.58.0 and Patches 0.21.0, not yet tested in game**, with [air-defence tie-ins](air-defence-tie-ins.md) AD2 and
AD3: the warning receiver, the dispensers and crew oxygen have readings (`warningReceiver`, `countermeasures`, `crewOxygen`) on the avionics
system. **A14** as designed (the Harrier `cruisesHigh`; its oxygen needs its part, power and an engine's bleed air through `Input_Bleed`),
with the owner's hypoxia setting giving a short condition of our own, `PhobosAltitudeHypoxia`, when oxygen fails during a high cruise.
**L3**: `When_Airborne`, `When_FlightPhase` and `Input_Bleed`. **A16**: cartridges made at the parts bench and carried as cargo, fired
automatically against DMS Expedition's shots on the world map, and a Harrier button on a map (owner, 17 Nov 2026). **A12**: the Harrier's
evasion postfix scales by flight control and the receiver, as designed. A hit in flight on the world map damages the aircraft and downs
it only if it cannot fly (owner, 17 Nov 2026). Limit: aircraft on the world map do not evolve their systems in flight (Vehicle Framework does
not tick them), so oxygen and power change in flight only when a hit lands.

## A16 revisited: the dispensers' magazine (20 Nov 2026)

**Framework 0.180.0 and Vehicles 0.89.0, not yet tested in game.** The owner, 20 November 2026: loading cartridges had become "all but
impossible" because cargo loading met "a number of differing restrictions across the board; some shared, but usually due to differing
reasons for each piece of aircraft", and proposed handling countermeasures as ammunition.

- **Why not Vehicle Framework's turret ammunition:** a VF turret reloads only from the vehicle's own cargo (`VehicleTurret.ReloadInternal`,
  VehicleFramework 1.7, read through `query-code.py`), so a hidden turret would have kept the same blockage. The specific refusals were not
  recorded; the magazine bypasses cargo altogether rather than answering each.
- **What was built:** `CompPhobosDispensers`, a magazine of real cartridge Things given to every design whose avionics carry
  countermeasures, sized by the fit's `countermeasureCartridges` (Harrier 60, an AN/ALE-39 set; Hind 128, four ASO-2V of 32; both
  secondary sources, in the loadouts). D'thorga Framework's [topping up](top-up-design.md) gained vehicles, so haulers carry cartridges
  from storage to a parked aircraft once ten are missing (`magazineLeastLoad`, authored); the job never reserves the aircraft, as VF's
  own loading job does not. The magazine's weight loads the engine as a turret's rounds do and takes no cargo room.
- **Lets** the player stock any aircraft with dispensers the same way. **Stops** a crate of cartridges in the cargo acting as a
  bottomless reserve: cargo cartridges are never fired. **Changes** rearming into a ground step like fuel, done by haulers.
- **Older saves:** once per aircraft, cargo cartridges move into the magazine while there is room (owner's choice); the rest stay cargo.
- **Other factions' aircraft** (owner's choice: "partly full"): a quarter to three-quarters of the magazine (authored), whole salvos,
  rolled once from the aircraft's ID; one captured keeps what is left. The player sees a count only on their own aircraft.
- **Limits:** the player's top-up fill level cannot lower it, since a magazine's own level is full. Nothing unloads a magazine. A
  setting that takes our systems off a design drops its comp, and the cartridges in it, at the next load. The Osprey has no dispensers
  (its infrared jammer needs none), though the real V-22 carries them; not changed here.

## Questions for the owner

**Answered by the owner, 13 Nov 2026:** (1) blind flying **refuses the launch, with a warn-only setting**; (2) aircraft with no
instrument part **gain an appended avionics bay**; (3) no working oxygen means **a low cruise, plus a setting to give crews hypoxia
where it is detected, off by default** (hypoxia only where D'thorga Medical's breathing layer, or another mod's, can carry it);
(4) countermeasures **later**; (5) **no aircraft is worse off on install day**. Later the same day: the avionics bay is **marked on our own
screens** (the Health tab's damage picture and the diagnosis report), never drawn on other authors' art. Nothing was built when they
answered; see the status line at the top.

The questions as they were put:

1. **Blind flying (A1): refuse the launch, or warn and allow?** Agent lean: **refuse by default, with a setting to warn only**,
   the same shape as the hover limit, so a player who wants the risk can take it.
2. **Aircraft with no instrument part** (the Hind, the Mongoose and the thrust pods): **append one "avionics bay" part**, as our
   Harrier patch did, or hang everything on the cockpit they have? Agent lean: **append**. A cockpit hit on the Hind already
   means a crash in its own code, so instruments there could never be damaged and survived. Saved aircraft would gain the part on
   load, as saved Harriers did.
3. **Oxygen (A14): a lower, slower cruise only, or crew hypoxia as well** (with D'thorga Medical's supplied oxygen)? Agent lean:
   **the low cruise only** at first; hypoxia in flight is harsh on crews the player cannot reach.
4. **Countermeasures (A16) as a consumable: now, later or never?** Agent lean: **later**, once evasion scaling (A12) has been
   played.
5. **The day it is installed: should any aircraft be worse off than today?** Agent lean: **no**. Every aircraft gets working
   attitude instruments and its present accuracy with everything sound; the costs arrive only with damage, lost power, night and
   weather.

## Who chose what

- **Owner (13 Nov 2026):** the review, with avionics as the gap seen most; all four areas (instruments and navigation; power;
  sensors and aiming; crew survival and defences); our conditions may scale other authors' figures through hooks that never skip
  their code; record only, then the owner picks; the override contract this record's fit plugs into.
- **Agent defaults, owner to revise:** the nine capabilities and two levels; every figure; the per-aircraft fits; what counts as
  poor visibility; "essential" loads; the appended avionics part; the order to build; leaving the hit spread alone.
- **Owner (20 Nov 2026), A16 revisited:** countermeasures as ammunition; haulers top up; other factions' aircraft partly full; cargo
  cartridges moved in once; capacities left to the agent for the owner's review at the end of the slice.
- **Agent choices (20 Nov 2026), owner to revise:** the magazine sizes (60 and 128, from the real dispensers), ten missing before a
  hauler walks, the quarter-to-three-quarters share, the default of 30 for a fit that names none, no cargo room taken.

## Sources

- **Rules and handbooks (primary).** [14 CFR 91.205](https://www.ecfr.gov/current/title-14/chapter-I/subchapter-F/part-91/subpart-C/section-91.205),
  instruments and equipment (the paragraph for instrument flight was not read item by item);
  [14 CFR 91.211](https://www.law.cornell.edu/cfr/text/14/91.211), supplemental oxygen;
  [14 CFR 23.1359](https://www.govinfo.gov/content/pkg/CFR-2012-title14-vol1/pdf/CFR-2012-title14-vol1-sec23-1359.pdf), the
  30-minute battery; the FAA's Pilot's Handbook of Aeronautical Knowledge, [chapter 8](https://www.faa.gov/sites/faa.gov/files/10_phak_ch8.pdf);
  the FAA's [Advanced Avionics Handbook, chapter 4](https://www.faasafety.gov/files/events/EA/EA03/2019/EA0392003/aah_ch04.pdf);
  the FAA's [Helicopter Flying Handbook, chapter 11](https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/helicopter_flying_handbook/hfh_ch11.pdf)
  (named as the chapter; its hydraulics text was not read); [FAA AC 61-107B](https://www.faa.gov/documentLibrary/media/Advisory_Circular/AC_61-107B.pdf)
  (named for the table; not read); Robinson Helicopter Company's [R66 handbook, section 3](https://robinsonheli.com/wp-content/uploads/2021/08/r66-poh-3.pdf);
  the United States Army's CH-47 manual as republished at [chinookhelicopters.tpub.com](https://chinookhelicopters.tpub.com/TM-55-1520-240-23-6/css/TM-55-1520-240-23-6_18.htm).
- **Aircraft descriptions (secondary).** The AV-8B: [Naval Technology](https://www.naval-technology.com/projects/av8b-harrier-ii-plus/),
  the [Naval History and Heritage Command](https://www.history.navy.mil/research/histories/naval-aviation-history/naval-aircraft/current-aircraft-inventory/av-8b-harrier-ii.html),
  [NAVAIR](https://www.navair.navy.mil/product/AV-8B-Harrier), [War History](https://warhistory.org/2020/07/16/mcdonnell-douglas-av-8b-harrier-ii/),
  [Warrior Lodge](https://warriorlodge.com/pages/mcdonnell-douglas-av-8b-harrier-ii),
  [The Ejection Site](https://ejectionsite.com/av8bseat.htm), [MiGFlug](https://migflug.com/afterburner/obogs-fighter-pilot-oxygen-why-not-pure-oxygen/).
  The Mi-24: [Aerospaceweb](https://aerospaceweb.org/aircraft/helicopter-m/mi24). Spatial disorientation:
  [AVweb](https://avweb.com/flight-safety/technique/surviving-vfr-into-imc/). Battery life in practice:
  [EAA](https://inspire.eaa.org/2025/10/22/all-amped-up/).
- **Dispenser capacities (secondary, 20 Nov 2026, A16 revisited).** The AN/ALE-39's 60 cartridges: [Military Periscope](https://www.militaryperiscope.com/weapons/sensorselectronics/electronic-support-measureselectronic-warfare/anale-39/)
  and [cmano-db](https://cmano-db.com/weapon/2012/), which names the AV-8B among the aircraft fitted with it; the AV-8B's own load was not
  found. The Mi-24V's four to six ASO-2V-02 of 32 flares each: [Weapon Systems](https://weaponsystems.net/system/671-Mil%20Mi-24V), one
  source only. Gathered from search summaries, as above.
- **How the real-aircraft statements were gathered:** from search summaries of the pages listed, on 13 Nov 2026. Single sentences
  were not checked against each page, so each is marked secondary, and no figure in a proposal rests on one.
- **Unverified, and not relied on:** the Harrier's stability augmentation by its name (sources mention one only in general
  terms); the UH-1's hydraulic layout (the Louie's fit is authored); which Mi-24 variant carries which sensor.
- **Other authors' code**, read from our decompiles and summarised in our own words; none of it is copied: Vehicle Framework, by
  Smash Phil; the Harrier and the fixed-wing framework; VTOL Vehicle, by rabiosus; the Hind and Mongoose mods; VTOL Raids; Vanilla
  Vehicles Expanded and its Tier 3, by the Vanilla Expanded team. No endorsement by any of them is implied.
- **Our own records**: [the override contract](vehicle-author-overrides.md), [the turbine design](vehicle-turbine-design.md),
  [power and lift](vehicle-power-and-lift-design.md), [the Harrier patches](vehicle-patches.md),
  [Vehicle Framework mechanics](vehicle-framework-mechanics.md), [the sling](vehicle-sling-design.md),
  [cross-links round one](vehicle-crosslinks.md#vehicle-system-crosslinks-research-and-record-9-nov-2026) and [round two](vehicle-crosslinks.md#vehicle-crosslinks-round-two-deeper-chains-more-fittings-and-what-the-stations-should-do-10-nov-2026).
