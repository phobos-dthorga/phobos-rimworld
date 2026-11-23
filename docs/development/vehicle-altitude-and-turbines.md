# Thin air, turbines and aircraft fuel (18 Nov 2026)

**Status (18 Nov 2026):** Built in D'thorga Framework 0.132.0 and D'thorga Vehicles 0.65.2: thin air at height (engine power, turbine
output, rotor lift, radiator cooling), the turbine gaps T-1, T-2 with step S7 and T-4, and aircraft fuel following the engines both
between tiles and on a map (T-3). **Not yet tested in game** (rows 187 and 188). Not built: thin air for other factions' vehicles and
the light model, water boiling lower, brake cooling, and descents (held). The landing check at a
journey's end is built in Framework 0.140.0 ([below](#the-landing-check-at-a-journeys-end-framework-01400)), **not yet tested in game**
(row 198). A flight's own cruising height between tiles is built in Framework 0.141.0, in
[our own flight over the map](vehicle-flight-over-the-map.md#built-18-nov-2026-the-cruising-height-and-phases-that-follow-the-hover), **not yet
tested in game** (row 199). Upgrades that move Vehicle Framework's fuel rate now reach a flight mod's own on-map rate (20 Nov 2026,
[below](#their-20-nov-2026-updates-the-harriers-armour-fuel-is-handed-back-on-a-map)), **not yet tested in game** (row 282).

How engines and drivetrains work now: [systems/engines-and-drivetrain.md](systems/engines-and-drivetrain.md); this record keeps its history.

This record follows [engine loads](archive/vehicle-engine-loads.md), which found that turbines had not come as far as piston engines (its
gaps T-1 to T-4), and [world elevation](world-elevation-and-descents.md), which found that every world tile has a real height in
metres. The owner chose to build both in one slice, so engines and air are tested once.

## Read this first

- **Your settlement stands 17 m above the sea** (read from the save PG12). Nothing changes there. Thin air is met on the road and in
  the air: half the land on your world is above 500 m, a tenth above 1,500 m, the highest tile is 3,968 m.
- **An aircraft idling on the ground with its pilot aboard now burns fuel.** It burned none before, on the Harrier, the Hind and the
  Mongoose. Take the pilot out when it is not needed.
- **A hover burns more than flying about**, about twice as much on a Harrier.
- **Flying between places is unchanged for an aircraft in good order with nothing switched on.** A worn engine and what is switched
  on add a few per cent, and the range shown before a flight already counts them.
- **Two new settings, both on:** "Aircraft fuel follows the engines" and "Thin air at height" (D'thorga Framework, vehicle systems).
- **Only the player's own vehicles are affected**, and thin air takes engine power only where the vehicle has the full drivetrain
  model.

## The owner's decisions

| # | Decision | Whose |
| --- | --- | --- |
| 1 | **Build the turbine gaps now**, not after testing: "I'd rather test when the new features are more complete" | owner, 18 Nov 2026 |
| 2 | **Aircraft fuel follows the engines in world flight and on a map** (hover, take-off and idling burn by phase), not world flight alone | owner, 18 Nov 2026, against the agent's recommendation of world flight only |
| 3 | **Altitude is built in the same slice**, on one air figure shared by piston engines, turbines and rotors | owner (agent's recommendation), 18 Nov 2026 |
| 4 | Each has its own setting, on by default | agent default |
| 5 | The tank is read, not the flight frameworks' rules, to know what they burned (below) | agent's choice |
| 6 | Thin air leaves other factions' vehicles and the light model alone | agent default, from the standing decision that the light model has no drivetrain |
| 7 | A flight between tiles keeps its departure tile's air; no cruising height is modelled | agent default |
| 8 | **An aircraft that cannot hover in the air where its journey ends comes in for a rolling landing** under the refuse setting, wearing its undercarriage | owner (agent's recommendation), 18 Nov 2026, over a crash landing or straining the engines |
| 9 | The landing is judged in the destination tile's air, at the weight on arrival; a refuelling stop is never made where it could not hover | agent default |
| 10 | A rolling landing costs one undercarriage part 5 % of its full health, or one outer part where the aircraft has no undercarriage part | agent default (authored) |

## What each behaviour lets the player do, stops them doing, and changes

| Behaviour | Lets | Stops | Changes |
| --- | --- | --- | --- |
| Thin air takes engine power | Understand why a laden truck labours and a helicopter lifts less in high country; choose a turbocharged vehicle or a lower road | Treating a 3,000 m plateau like the coast | The turbocharger fitting has a second use; where a base or an outpost stands matters to its aircraft |
| A hover and a climb burn more | Plan a sortie by what it does, not only how long it lasts | Hovering about for free | Vertical landings and long hovers cost fuel; a runway landing is the cheap one |
| Idling on the ground burns fuel | Keep an aircraft ready to go, at a price | Leaving a crewed aircraft running all day for nothing | Shut down, or pay |
| Loads and wear reach the turbine and its fuel | See what the engine is driving, on aircraft too | Nothing | A worn hot section costs range as well as margin |

**Candidly, on size:** between tiles the change is a few per cent at most. On a map the hover and the idle are the two the player
will notice. Thin air is nothing at the owner's settlement and matters most to helicopters lifting a full load in high country.

## Thin air

### The rule

`AirRule` in the Framework's core (game-free, `src/PhobosFramework/Rules/AirRule.cs`), from a tile's height in metres:

| Figure | Formula | Whose |
| --- | --- | --- |
| Pressure against sea level | (1 − 0.0065 × h ÷ 288.15) ^ 5.25588 | The International Standard Atmosphere, troposphere (see [sources](#sources)) |
| Standard density against sea level | the same to the power 4.25588 | The same |
| Density at the temperature it really is | pressure × 288.15 ÷ (273.15 + °C) | Ideal gas |
| An engine with no turbocharger | 1.132 × density − 0.132 | The empirical correction in Leishman's textbook (see [sources](#sources)) |
| A turbocharged engine | all its power to `turbochargerHoldsToMetres` (1,500 m), then as an engine with none would fall from there | The height from one Cummins engine's sheet; the shape authored, and it gives that sheet's 3.4 % per 305 m (see [sources](#sources)) |
| A gas turbine's output and thrust | × pressure | A simplification of the textbook's first-order relations (see [sources](#sources)) |
| Hover power a rotor needs | × 1 ÷ √(density at the real temperature) | Momentum theory, as the turbine model already used for heat |
| A radiator's cooling | × pressure ^ 0.8 (`airCoolingExponent`) | Authored: air's mass flow follows pressure, the heat it takes a little less. A Caterpillar dealer's rule of thumb is of the same size (see [sources](#sources)) |

Worked by height (standard temperature for the height):

| Height | Pressure | No turbocharger | Turbocharged | Turbine | Radiator | Hover power needed | Land on the owner's world above it |
| --- | --- | --- | --- | --- | --- | --- | --- |
| 17 m (the settlement) | 0.998 | 0.998 | 1 | 0.998 | 0.998 | × 1.001 | nearly all |
| 500 m | 0.942 | 0.947 | 1 | 0.942 | 0.953 | × 1.024 | half |
| 1,000 m | 0.887 | 0.895 | 1 | 0.887 | 0.909 | × 1.050 | about a quarter |
| 1,500 m | 0.835 | 0.846 | 1 | 0.835 | 0.865 | × 1.076 | a tenth |
| 2,000 m | 0.785 | 0.798 | 0.944 | 0.785 | 0.824 | × 1.103 | |
| 2,500 m | 0.737 | 0.752 | 0.889 | 0.737 | 0.783 | × 1.131 | about a hundredth |
| 3,000 m | 0.692 | 0.708 | 0.837 | 0.692 | 0.745 | × 1.161 | |
| 4,000 m | 0.608 | 0.625 | 0.739 | 0.608 | 0.672 | × 1.223 | none (the highest tile is 3,968 m) |

The owner's own places in PG12: the settlement at 17 m, two outposts at 204 m and 272 m, two abandoned settlements at 469 m and
746 m. None loses more than a twelfth.

### Where the height comes from

- `TileAir.ElevationMetres` (`src/PhobosFramework/WorldMap/TileAir.cs`) reads `SurfaceTile.elevation`: the map's tile on a map, the
  caravan's tile on the world map. Water and anything below the sea read as the sea.
- **A local map has no height of its own**, so a map is as high as its tile (the elevation record).
- **Off the planet's surface it reads as the sea.** In orbit a tile's elevation is the layer's radius, not metres; engines in a
  vacuum are not modelled.
- The vehicle reads it at its usual look (every 250 ticks), and again only when its tile changes.

### What reads it

| What | How | Where |
| --- | --- | --- |
| A piston engine's power | `AirPowerFactor` multiplies what the faults of step S4 leave, as lost power in the drivetrain model: it tells only where power holds the vehicle back | `Powertrain.SampleGround` |
| A ground turbine (the Abrams) | The same, at the air's pressure; and its turbine model has less to give, so it runs hotter for the same work | `Powertrain.SampleGround`, `TurbineModel.Evaluate` |
| An aircraft's engines | Less output and thrust; the same work runs hotter and reaches its limits sooner | `Powertrain.SampleAircraft` |
| A helicopter's hover | More power needed, less to give: the hover margin and the lift check before launch | `Powertrain.HoverDemand`, `HoverCheck` |
| A jet's vertical landing | Less thrust against the same weight | `TurbineModel.HoverMargin` |
| The radiator | `Input_AirCooling` on the cooling system's radiator flow | `Systems_Cooling.xml` |

**Which engines are turbocharged** (`EngineXml.turbocharged`): the Framework's archetypes for trucks, tracked vehicles and
diesel-electric locomotives are, as built (authored: the class each stands for); the petrol car and the boat are not. A fitting that
grants `Boost` (the turbocharger) gives any piston engine the same hold. The real engines in D'thorga Vehicles are listed in
[sources](#sources) with what each is.

### What it does not do

- **Other factions' vehicles and the light model are untouched.** Lost power is carried by the drivetrain model, which they do not
  have; taking it as lost speed would slow every raider in high country whether or not power held them back.
- **A flight between tiles has no cruising height** (as this record built it; Framework 0.141.0 adds one, as the status line says). It keeps the air of the tile it left, for its lift check at launch; where it
  lands, the air of that tile decides its landing ([the landing check](#the-landing-check-at-a-journeys-end-framework-01400)).
  One is designed, not built, in [our own flight over the map](vehicle-flight-over-the-map.md#a-cruising-height-between-tiles).
- **Fuel does not change with height.** Thin air leaves less power; the work done burns what it burned.
- **Not built:** water boiling lower, brakes cooling less, steam locomotives, electric motors' cooling.

### The landing check at a journey's end (Framework 0.140.0)

The lift check before launch reads the air of the place the aircraft leaves. A journey to a higher or hotter place, or one on which
an engine is lost, could land beyond the engines without a word. Raised on 18 Nov 2026 when the owner asked what engine work the V-22
Osprey would need ([modern military vehicles](modern-military-vehicles.md)); worth having for every helicopter and pod already in play.

| Lets the player | Stops them | Changes how they play |
| --- | --- | --- |
| See before launch that a heavy aircraft cannot hover down at a high or hot destination, and lighten it or accept the cost | Landing a full load at a high, hot outpost as if it were the coast | A high outpost wants lighter loads, cooler hours or an undercarriage repaired now and then |

**How it works:**

- **The moment:** a prefix on Vehicle Framework's `FlightPath.ConsumeNode` as the journey's last tile is reached, before its arrival
  action lands the aircraft on a map or as a caravan (`LandingCheck`, `Patch_FlightPath_ConsumeNode_LandingCheck`). A strafing run,
  a crash already under way, circling and recon are left alone. The landing itself is Vehicle Framework's, unchanged.
- **The air:** the hover check (`Powertrain.HoverCheck`, now with a weight, temperature and pressure of its own) at that tile's
  temperature now (`GenTemperature.GetTemperatureAtTile`) and, with "Thin air at height" on, its pressure. The weight is the aircraft's
  now. Engines lost on the way count, as at launch.
- **What follows**, by the hover-limit setting:
  - **Refuse** (the default): it comes in for a rolling landing, as a helicopter does when it cannot hover and as the Harrier takes its
    runway. Its undercarriage takes 5 % of one part's full health (`RunOnWearShare`, authored), never below 1. With no part bound to
    the `LandingGear` role, which no loadout binds yet, one of its outer parts takes it.
  - **Allow at a cost:** it lands as usual and its engines take one over-limit lift's strain (`HoverLimits.Strain`, shared with the launch).
  - **Warn only**, or figures only: it lands and a message says so.
- **Before launch:** the destination picker adds a line where the aircraft would arrive too heavy to hover there, with the share of
  the power it needs, at its weight less the fuel the journey burns (a postfix on `FuelTargetUpdater.ValidateTargets`, after the cargo
  reach one).
- **Refuelling stops** ([cargo refuel](vehicle-cargo-refuel.md)): under refuse, it never sets down where it could not hover, and flies
  on; under the other settings it sets down and the outcome applies.
- **The player's aircraft only.** Other factions' aircraft do not fly these journeys; in a raid or at a base we attack, none of this
  applies.

**Simplifications (authored):** a rolling landing needs open ground, which every landing site in the game already is, so no surface is
checked; the warning uses the temperature at the time of picking, and the landing the temperature on arrival; a slung load is not in
the hover weight (the sling has its own hook limits), so setting it down first would change nothing here.

**Not built:** "drop the stores" (outstanding work V6) for an aircraft that could hover lighter. A helicopter cannot throw out its
cargo in flight, and no aircraft we tie into dumps fuel; it waits for an aircraft with external stores.

## Turbines catch up

| Gap | What was missing | What is built |
| --- | --- | --- |
| **T-1** | The Abrams' turbine model took its demand from driving alone | The ledger's power is added to it: lights, the alternator after a start and anything else it drives heat and wear the turbine (`Powertrain.SampleGround`) |
| **T-2, S7** | An aircraft had only its generator on the ledger, and that did not reach the turbine; bleed air sat inside the turbine model | Flying's demand is the engine's "drive"; bleed air (the puffer jets, cabin air) and the generator are ledger rows, shown on the Systems tab, and all of it is the turbine's demand (`Powertrain.SampleAircraft`, `CompPhobosVehicleSystems.ReadLedger`) |
| **T-4** | A turbine's generator was costed at spool speed times a pulley ratio, the far end of the efficiency curve (about 45 %) | It turns at `generatorRatedRpm` (4,000 rpm, authored) times the spool's share: about 55 % |
| **T-3** | Aircraft fuel never followed the engine | Below |

**An aircraft engine's size.** A loadout that gave an aircraft no power figure took the truck's default of 30 kW a tonne, so a
generator's 5 kW read as 2 % of a Hind's engines where it is nearer 0.15 %. The aircraft archetypes now carry authored figures (250
kW a tonne for turboshafts, 1,500 for jets and thrusters), which size the ledger's shares and nothing else: lift and thrust come
from the ratings.

## Aircraft fuel

### What was there

- **Between tiles**, Vehicle Framework burns `ConsumptionRatePerTick × FuelConsumptionWorldMultiplier` each tick
  (`AerialVehicleInFlight.SpendFuel`†), and reads the same product to say how far an aircraft can reach
  (`CompVehicleLauncher.FuelNeededToLaunchAtDist`†). We already had one patch on that multiplier, for a sling load and failed
  navigation (`Patch_FlightFuel_Sling`).
- **On a map**, the two flight frameworks burn a flat `fuelConsumptionPerTick` of their own each tick while taking off, flying and
  leaving the map, and nothing while landing, idling or spooling (`VtolFlightController.Tick`† in both). Vehicle Framework itself
  burns nothing there for these aircraft: their fuel condition is "Flying".

| Aircraft | Tank | Vehicle Framework's rate (between tiles) | Its framework's rate on a map |
| --- | --- | --- | --- |
| Harrier | 250 | 150 a day | 0.0167 a tick: 1 a second, about 6.7 times the other |
| Hind | 350 | 75 a day × 0.8 | 0.0078 a tick: 0.47 a second |
| Mongoose | 250 | 60 a day × 0.8 | 0.0078 a tick |

### Between tiles

The one patch now also multiplies by `Powertrain.WorldFlightFuelShare`: the fuel at a cruise with today's loads and wear, against the
fuel at the same cruise in good order with nothing on. So the author's rate holds exactly for a sound aircraft, and the range the
launch screen shows is the fuel that will be burned.

| What | Adds |
| --- | --- |
| Nothing switched on, engines in good order | nothing |
| Cabin heat or air conditioning (bleed air, 0.5 %) | about 0.3 to 0.5 % |
| A generator carrying 5 kW | about 0.2 % |
| A hot section with its life used up (condition 0.9) | 4.5 % |
| A hot section far past its life (condition 0.6) | 18 % |

- The generator's share uses the steady loads alone, at full speed, with no battery to refill: the figure is held for a whole flight,
  because a vehicle's comps do not tick in flight†.
- **No new Harmony patch.**

### On a map

**The author's on-map rate holds while flying about in good order.** Each phase burns that rate times the fuel its demand asks
against the fuel "on station" asks, on the turbine model's own line: the idle share plus the rest in step with demand, going on
past maximum continuous as far as the top rating.

Worked with the idle fuel shares now in the Defs: 0.05 for a jet, 0.2 for a helicopter's turboshaft, both re-authored from
published idle fuel flows now that something reads them (see [sources](#sources)); they were 0.25, read by nothing:

| Harrier, 1 a second on station | Demand | Burns | A full tank lasts |
| --- | --- | --- | --- |
| Idling on the ground, pilot aboard | 0.08 | 0.24 a second | 17 minutes |
| Taxiing | 0.15 | 0.37 a second | |
| Taking off | 1.0 | 1.91 a second | |
| Climbing away | 0.85 | 1.64 a second | |
| On station | 0.5 | 1.00 a second | 4.2 minutes |
| Attack run | 0.8 | 1.55 a second | |
| Runway landing | 0.4 | 0.82 a second | |
| Hovering down, light | 1.1 | 2.09 a second | |
| Hovering down, heavy | 1.35 | 2.54 a second | |

| Hind or Mongoose, 0.47 a second on station | Demand | Burns | A full Hind tank lasts |
| --- | --- | --- | --- |
| Idling on the ground, pilot aboard | 0.1 | 0.17 a second | 34 minutes |
| Lifting off or settling, light | about 0.6 | 0.42 a second | |
| Lifting off or settling, at maximum gross weight | about 0.9 | 0.57 a second | |
| On station | 0.7 | 0.47 a second | 12.5 minutes |
| Climbing away | 0.85 | 0.54 a second | |

Minutes are of play at normal speed. **The owner is asked in row 187 whether idling feels too thirsty**; the knob is each engine's
`idleFuelShare`, and each phase's demand in `FlightProfiles.xml`.

**How the correction works** (`Powertrain.OweFuel`, `ApplyAircraftFuel`):

1. Every sample (once a second while the aircraft is active) adds what the engines owe for the ticks it stands for.
2. At the vehicle's look, the tank is read: what it lost since the last look to everything else is what Vehicle Framework and the
   flight framework burned.
3. The difference is burned, or handed back, through the one fuel service (`VehicleFuel`), which tells the powertrain about our own
   burns so they never count as someone else's.

**Why the tank is read, not the frameworks' rules** (agent's choice): the two frameworks decide when to burn inside their own flight
code, by state and by whether the aircraft may move while taking off. Copying those rules would charge twice, or refund, the day one
of them changes. Reading the tank needs to know only the rate their Defs give, and that only as the reference.

**Guard rails:**

- The reference rate is read by name from the flight framework (`FlightProps.fuelConsumptionPerTick`, named in the profile's XML).
  **If it cannot be read, the aircraft's fuel on a map is left alone**, never guessed: guessing low would refund most of what the
  framework burns.
- What the tank lost counts as engine fuel only up to what the frameworks could have burned (their rates, a twentieth over). A
  drained or jettisoned tank is not refunded.
- **While the tank leaks, nothing is ever handed back.** More is still burned where owed. A leak can hide up to what was owed for
  that look, so a leaking aircraft may briefly burn less than it should (a simplification).
- A gap of more than 600 ticks (a flight between tiles, a load) is stale: nothing is owed for it and the tank's loss across it is
  not read.
- Aircraft that fly only through Vehicle Framework's launcher (its stock helicopters, pods) take its own rate as the reference, at
  their cruise demand: drafted on the ground they burn about half of it, which is what Vehicle Framework charges a drafted vehicle
  standing still.

### What the fuel line shows

The engine's figure on "Burning about X a day" is what the frameworks burned and our correction together, so it is the true rate in
every phase.

### Their 20 Nov 2026 updates: the Harrier's armour fuel is handed back on a map

Read on 20 Nov 2026 from the Harrier build the assembly archive kept that day (`python scripts/assembly-archive.py compare
AV8BHarrier.dll --show AV8BHarrier.Patch_HarrierArmourFuel`) and its Defs in the Workshop folder. Nothing of ours was changed.

**What the author added** (their code and XML): a Plasteel Armour upgrade node (`Harrier_PlasteelArmour` in `Harrier_UpgradeTree.xml`)
that adds armour ratings and no mass, and says "15% more fuel used". It charges that 15% twice over, one way for each place the jet
burns:

- **Between tiles:** a `StatUpgrade` adds 22.5 to Vehicle Framework's `FuelConsumptionRate` (150 to 172.5 a day).
- **On a map:** `Patch_HarrierArmourFuel`, a postfix on `VtolFlightController.ConsumeFuel`, burns another 0.15 of the flight
  framework's `fuelConsumptionPerTick` whenever the framework burned and the node is fitted (`HarrierArmourUpgrade.FuelFactor`, 1.15).

**How ours meets it** (our reading of our code, not seen in play):

- **Between tiles it holds.** Our one patch multiplies Vehicle Framework's rate by `WorldFlightFuelShare`, so 172.5 a day stays the
  base and our share moves it as for any Harrier.
- **On a map our correction hands it back.** `Powertrain.OweFuel` (`src/PhobosFramework.Vehicles/Systems/Powertrain.cs:951`) owes the
  flight framework's on-map rate times the phase's share; it does not know the armour is fitted. `ApplyAircraftFuel` (`:970`) reads
  what the tank lost, counts up to `othersAtMost` (1.05 × (the on-map rate + Vehicle Framework's own rate), about 1.21 times the
  Harrier's on-map rate) as burned by others, and gives back the difference. The armour's 1.15 is under that ceiling, so all of it
  counts as burned by others, and on station `owed − seen` is −0.15 of the rate: **the 15% is handed back in full** while "Aircraft
  fuel follows the engines" is on (its default). In other phases the share moves the base, and the armour's 0.15 of the author's flat
  rate still comes back.
- **Not a double charge but the opposite**, and only on a map: an armoured Harrier burns as an unarmoured one there.
- **Our hover sums do not see the plates either**: `Powertrain.HoverDemand` (`:596`) reads mass, and the node adds none. Our own armour
  kits do add their weight as carried mass ([vehicle armour](vehicle-armour.md)), so they reach the hover and fuel sums.

**Built 20 Nov 2026 (Framework, the release after 0.194.0), at the owner's word** ("Yup, I'd build it, and if you can do so
defensively for other cases like this, and with other vehicles"). **Not yet tested in game** (row 282). `MapFuelPerTick` carries a
flight mod's own on-map rate by `FuelRateRule.RateFactor`: Vehicle Framework's rate for the aircraft now (`FuelEfficiency`, which holds
its upgrades and the player's Vehicle Framework setting) against the rate its Def writes. 172.5 / 150 = 1.15 for the armoured Harrier,
so the engines owe the author's extra burn and nothing is handed back. No upgrade is read by name.

How far it reaches, read from the code that day:

| Case | Before | Now |
| --- | --- | --- |
| A flight mod with its own flat on-map rate (the fixed-wing jet and helicopter profiles: Harrier, Hind, Mongoose) | Upgrades and the player's setting ignored on a map | Carried by the factor |
| An aircraft burning Vehicle Framework's rate on a map | Already followed it (`ConsumptionRatePerTickRaw` is built on `FuelEfficiency`) | Unchanged |
| Any aircraft between tiles | Already followed it (our share multiplies Vehicle Framework's world rate) | Unchanged |
| Ground vehicles, on a map and between tiles | Already followed it (our correction is a share of `FuelEfficiency` and of its world rate) | Unchanged |
| Our own fittings and upgrades | None touches `FuelConsumptionRate` (searched 20 Nov 2026), so nothing is counted twice | Unchanged |

**Limits, said plainly:** a mod that burns more on a map through its own code *without* moving Vehicle Framework's rate is still
handed back up to the ceiling (`othersAtMost`), as before: the tank cannot say whose burn it was. The factor is kept between a tenth
and ten times (authored bounds), and a figure that cannot be read leaves it at 1. A player's own Vehicle Framework fuel setting for a
flight-mod aircraft now reaches its on-map burn too; the owner had none set that day.

## Other factions' vehicles

- **In a raid on us:** none of this applies. Aircraft fuel follows the engines only for the player's own aircraft
  (`AircraftFuelFollows`), and thin air is read only in the full simulation, which other factions' vehicles do not run. A raider's
  helicopter idles and hovers on its author's rates, at any height, so nothing strands it sooner.
- **Parked at a base we attack:** the same. Its engine is off; nothing is read.
- **Captured:** from the moment it is ours it is simulated in full, so it follows its engines and the air like any of ours.
- **Cost with several on a map:** none for theirs. For ours, one height read per vehicle when its tile changes, and one tank read
  per look for an aircraft.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| **Aircraft fuel follows the engines** | On | Off: aircraft burn their authors' flat rates, between tiles and on a map |
| **Thin air at height** | On | Off: every tile is treated as sea level |

Both are in D'thorga Framework's settings, under vehicle systems, beside "Fuel use follows engine load", and take effect at once.

## To watch in play

- Whether idling on the ground feels too thirsty (row 187).
- A Harrier's vertical landing at a high place: thinner air takes thrust, so it may need its water, or a runway, where it did not.
- A helicopter with a sling load from a high outpost: the hover margin on its Systems tab says how close it is.
- An aircraft's ledger in Engineer mode: kilowatts for bleed air are worked from an authored engine size, so they are indicative.

## Sources

Findings are the sources'. The model, every simplification and every authored figure are ours; no source endorses them. Web
sources were read by a research helper on 18 Nov 2026; "how read" says how.

| Source | What it supports | Where we use it | How read |
| --- | --- | --- | --- |
| J. Gordon Leishman, Embry-Riddle Aeronautical University, *Introduction to Aerospace Flight Vehicles*, chapter "Atmospheric Properties" ([chapter](https://eaglepubs.erau.edu/introductiontoaerospaceflightvehicles/chapter/international-standard-atmosphere-isa/)) | The International Standard Atmosphere: sea level 101,325 Pa and 288.15 K, 6.5 °C a kilometre; pressure ratio (1 − 2.256 × 10⁻⁵ h) ^ 5.256 and density ratio to the power 4.256, to 11,000 m | `AirRule.PressureRatio`, `StandardDensityRatio` (our constants are the same to four figures) | The page's text; a textbook, secondary |
| NASA Glenn Research Center, "Earth Atmosphere Model - Metric Units", updated 13 May 2021 ([page](https://www.grc.nasa.gov/www/k-12/airplane/atmosmet.html)) | The same model with slightly different constants (101.29 kPa, 288.08, 0.00649) | A cross-check only | The page |
| Leishman, the same book, chapter "Piston Aeroengines" ([chapter](https://eaglepubs.erau.edu/introductiontoaerospaceflightvehicles/chapter/reciprocating-engine-propeller/)) | An engine with no supercharger: power follows density, with "an empirical correction ... often used in practice" of 1.132 σ − 0.132; its example, 160 hp at σ = 0.75 gives 114.7 hp | `AirRule.NaturallyAspiratedPower` (a test checks the example) | The page's text. It names no originator |
| Garrett Motion, "Turbocharging At Elevation", 15 Aug 2019 ([article](https://www.garrettmotion.com/news/newsroom/article/how-to-turbocharge-at-elevation-counteracting-lower-air-density/)) | "A naturally aspirated combustion engine will lose 3% of its power for every 1,000 ft of elevation gain" | A check: ours gives 10.5 % at 1,000 m, which is 3.2 % per 1,000 ft | The page; a turbocharger maker |
| Cummins Power Generation, generator set DFHD specification sheet S-1311m, 9/06 (the QST30-G5; [copy at a dealer](https://csdieselgenerators.com/Images/Generators/2619/Cummins-DFHD-spec-sheet.pdf)) | A turbocharged, aftercooled diesel: full power to 1,512 m at up to 40 °C, then 3.4 % per 305 m to 3,000 m | `turbochargerHoldsToMetres` = 1,500, and the fall above it (ours gives 11 % over the next 1,000 m, which is 3.4 % per 305 m) | The PDF's text. **A generator-set rating, not a truck's** |
| Cummins Power Generation, generator set DQHAB specification sheet S-1539b, 12/07 (the QSM11-G4; [copy at a dealer](https://woodstockpower.com/wp-content/uploads/2019/04/Cummins-300-DQHAB.pdf)) | Another turbocharged diesel holds standby power only to 875 m at 40 °C, then loses 4 % per 300 m | Shows the hold height varies by engine: 1,500 m is one engine's figure, taken as the class's (authored) | The PDF's text |
| Leishman, the same book, chapter "Helicopters" ([chapter](https://eaglepubs.erau.edu/introductiontoaerospaceflightvehicles/chapter/helicopters/)) | A turboshaft's power to a first order: sea-level power × δ × √θ (pressure and temperature ratios). Ideal hover power T^1.5 ÷ √(2 ρ A): it rises as 1 ÷ √density | The turbine's × pressure (we keep the turbine design's own temperature term, 0.7 % per °C above 15 °C, in place of √θ: a simplification). `TurbineModel.HoverPowerNeeded` | The page's text |
| Virginia Tech, AOE 3104 course notes, "Performance: 10. Thrust Models" ([notes](https://archive.aoe.vt.edu/lutze/AOE3104/thrustmodels.pdf)) | Jet thrust follows (density ratio) ^ x, x "typically 1"; 0.8 to 1.2 for a turbofan in the troposphere | A jet's thrust × pressure (a simplification: pressure, with our temperature term, stands for density) | The PDF's text; no author printed |
| Federal Aviation Administration, *Helicopter Flying Handbook*, chapter 7 "Helicopter Performance" ([chapter](https://www.faa.gov/sites/faa.gov/files/regulations_policies/handbooks_manuals/aviation/helicopter_flying_handbook/hfh_ch07.pdf)) | "As density altitude increases, more power is required to hover. At some point, the power required is equal to the power available. This establishes the hovering ceiling" | The hover margin and the lift check falling with height | The PDF's text |
| Toromont Power Systems (a Caterpillar dealer), "Consultant's Corner: Radiator Design Considerations" ([note](https://www.toromontpowersystems.com/pdf/ConsultantsCorner/Radiator%20Design%20Considerations.pdf)) | "Reduce radiator performance 2.5F for every 1000 ft. of elevation to compensate for lower air densities" | The direction and rough size of `Input_AirCooling`. **The exponent 0.8 is authored**, not from this | The PDF's text; a dealer's rule of thumb |
| C. Meher-Homji (Bechtel), A. F. Bromley and J.-P. Stalder (Turbotect), "Gas Turbine Performance Deterioration and Compressor Washing", 2nd Middle East Turbomachinery Symposium, March 2013 ([paper](https://turbolab.tamu.edu/wp-content/uploads/2018/08/METS2Tutorial5.pdf)) | Industrial turbines' heat rate rises "about 0.4 to 0.5 percent per 1 percent deterioration in power output" from fouling | `turbineWearFuelPenalty` = 0.45. **Approximate:** our "condition" is a worn hot section, theirs is fouling, and theirs are industrial machines | The PDF's text |
| R. J. Bouchard, W. R. Beyerly and G. P. Sallee, Pratt and Whitney for NASA, "Short-term performance deterioration in JT9D-7A(SP) engine 695743", NASA-CR-135431, 1978 ([record](https://ntrs.nasa.gov/citations/19780022178)) | One airliner engine had lost 1.5 % in thrust specific fuel consumption from new | A check that a few per cent is the right size for an aero engine | The abstract page only |
| Theo Rindlisbacher and Lucien Chabbey, Swiss Federal Office of Civil Aviation, *Guidance on the Determination of Helicopter Emissions*, edition 2, Dec 2015 ([guidance](https://www.bazl.admin.ch/dam/fr/sd-web/kB3i-4iu8Mwx/guidance_on_the_determinationofhelicopteremissions.pdf)) | A large twin's turboshaft (the AS332's Makila 1A1) at ground idle, 6 % power, burns 0.024 kg/s; at take-off 0.079 kg/s | The turboshaft's `idleFuelShare` = 0.2. **Our inference:** by the guidance's own fuel-flow formula that idle is about a quarter of maximum-continuous flow | The PDF's text |
| ICAO Aircraft Engine Emissions Databank, hosted by EASA, version 32B ([databank](https://www.easa.europa.eu/en/domains/environment/icao-aircraft-engine-emissions-databank)) | Take-off and idle (7 % thrust) fuel flows, kg/s: CFM56-7B26 1.221 and 0.113; Spey Mk511 0.889 and 0.119; JT8D-17 1.245 and 0.147; Tay 651 0.870 and 0.120 | The jet's `idleFuelShare` = 0.05. **Our inference:** idle is 9 to 14 % of take-off flow, and ours gives 11 %. **No military engine is in the databank**; the Spey is the nearest. The databank says its flows are sea-level static with no bleed, and not for comparing engines | The spreadsheet |
| RimWorld 1.7, Vehicle Framework 1.7, the Fixed-Wing Aircraft Framework fork and rabiosus's VTOL Vehicle framework, decompiled | Everything marked † | Read with `scripts/query-code.py`; summarised in our words, never copied | Decompiles in `.local/` |
| The Harrier's, Hind's and Mongoose's Defs | Their tanks and fuel rates | Read with `scripts/query-defs.py` | As written by their authors |
| The owner's save PG12 | The heights of the owner's places | Read-only, through the save tool's index | By the author |

**Unverified. No Def or rule carries these as fact:**

- **That the 1.132 σ − 0.132 correction is Gagg and Farrar's.** Their paper exists (R. F. Gagg and E. V. Farrar, "Altitude Performance
  of Aircraft Engines Equipped with Gear-Driven Superchargers", SAE paper 340096, 1934), and it is widely credited to them, but
  neither the paper nor a page naming them for this formula could be read. The code credits the textbook.
- The US Standard Atmosphere 1976 and ICAO Doc 7488 at first hand: the figures above rest on the textbook.
- A truck engine's own altitude rating. Both Cummins sheets are generator sets.
- The Rolls-Royce Pegasus's idle fuel flow, or any military engine's.
- A turbine maker's statement of power against altitude.

**Authored, with no source:** the radiator's exponent 0.8; `generatorRatedRpm` 4,000; the aircraft engine sizes (250 and 1,500 kW a
tonne); each flight phase's demand (they were authored before this record); which of the Framework's archetype engines are
turbocharged (the class each stands for).

### The real engines in D'thorga Vehicles

What each is, for `turbocharged` in `Drivetrains_RealVehicles.xml` and the shunter's loadout. Read by a research helper on 18 Nov 2026.

| Engine (vehicle) | Aspiration | Source | How read |
| --- | --- | --- | --- |
| Banks 866T 6.6 L V8 (L-ATV) | Turbocharged | Gale Banks Engineering, "Banks Powered Oshkosh JLTV" ([page](https://official.bankspower.com/project/custom-projects/banks-powered-oshkosh-jltv)): "866T V8 Turbo Diesel" | The page; the maker |
| Navistar MaxxForce D9.3 (MaxxPro) | Turbocharged, twin turbochargers | Wikipedia, "Navistar DT engine" | The page; secondary. Navistar Defense's own site is offline |
| GM 6.5 L V8, 160 hp (HMMWV) | **Naturally aspirated** | AM General, M1097A2 specification sheet ([copy](https://warwheels.net/images/M1097a2specsAMG.pdf)): "naturally aspirated diesel engine", 160 hp at 3,400 rpm; US Army TM 9-2320-280-10, table 1-5 | The PDF; the maker |
| Toyota 1VD-FTV (Land Cruiser 70) | Turbocharged, intercooled (one turbocharger in the 70 series) | Wikipedia, "Toyota VD engine" | The page; secondary |
| Caterpillar C15, 600 hp (PLS A1) | Turbocharged, aftercooled | California Air Resources Board, executive order A-013-0194-1 for the 2008 on-road C15 ([order](https://ww2.arb.ca.gov/sites/default/files/classic/msprog/onroad/cert/mdehdehdv/2008/caterpillar_hhdd_a0130194r1_15d1_1d3-0d01.pdf)): "TC(2), CAC" | The PDF. Oshkosh's own sheet names the engine but not its aspiration |
| Detroit Diesel 6V53, 212 hp (M113) | **Not turbocharged**: a two-stroke with a scavenging blower only | AFV Database, "M113" ([page](https://afvdatabase.com/usa/m113.html)); Wikipedia, "Detroit Diesel Series 53" | The pages; secondary. The M113A3's 275 hp 6V53T is the turbocharged one |
| Cummins VTA-903T, 600 hp (Bradley) | Turbocharged, aftercooled | Cummins Inc., V903 bulletin 5600645 ([bulletin](https://mart.cummins.com/imagelibrary/data/assetfiles/0063416.pdf)): "Aspiration: Turbocharged and Aftercooled" | The PDF; the maker |
| Continental AVDS-1790-2, 750 hp (M48H) | Turbocharged, twin turbochargers | Wikipedia, "M60 tank", "CM-11 Brave Tiger" and "Continental AV1790"; AFV Database calls it "supercharged", the older US Army word for the same | The pages; secondary |
| MTU 8V 199, 600 kW (M10 Booker) | Turbocharged | Wikipedia, "Boxer (armoured fighting vehicle)": a militarised Mercedes-Benz OM 500 with changes "to the turbocharger"; MTU's sheet for the sibling 8V 502 | The pages; secondary. **No MTU page read says it of the 8V 199 itself** |
| V-54 V12, 520 hp (T-54) | **Naturally aspirated** | Russian Wikipedia, "В-2": the V-54 among the unsupercharged diesels that followed the V-11 | The page; secondary, and the only support found |
| UTD-20 V6, 300 hp (BMP-1) | **Naturally aspirated** | Department of Defence Production, Ministry of Defence, India ([listing](https://www.ddpmod.gov.in/hi/node/8050)): "Naturally Aspirated", of the licence-built engine in the BMP-2 | The page. Said of the same engine in the BMP-2, not the BMP-1 by name |
| Audi five-cylinder, 64 kW (Wiesel 1) | Turbocharged | Wikipedia, "Wiesel AWC": "turbo-diesel" | The page; secondary. The Bundeswehr's own page does not say |
| Caterpillar C13B, 328 kW (the EffiShunter 300 our shunter follows) | Turbocharged, aftercooled | CZ LOKO, "EffiShunter 300" ([page](https://czloko.com/products/lokomotivy-1/effishunter-300-2.htm)); Ziegler Cat, the C13B's page | The pages; the maker and a dealer |
| Honeywell AGT1500 (M1 Abrams) | A gas turbine | AFV Database, "M1 Abrams" | The page |

**To watch:** the four without a turbocharger lose about a tenth of their power at 1,000 m. On a road they will not show it; a laden
M113 or T-54 in soft ground at height will.
