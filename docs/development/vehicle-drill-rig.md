# The mobile drill rig: design (9 Nov 2026)

**A design record. Nothing is built. Not yet tested in game. Agent-authored on the owner's directions of 9 Nov 2026, for the owner to
confirm.** It replaces the rig section of [the utility vehicles record](utility-vehicles-design.md) and follows the shape of that record's
bulk tanker. Code facts were read on 9 Nov 2026 from our decompiles (`all.cs` is `.local/decompiled/full/all.cs`) and from `query-defs`;
everything else is a proposal. The family's power rule is in [the generator set](vehicle-generator-set.md), written in parallel.

## Read this first

- **What it is:** a truck that carries a deep drill. Driven to a survey find and set up on its outriggers, it drills the same deposits at the
  same rates as the game's deep drill, then packs up and drives to the next lump. `Phobos_DrillRig`, one `VehicleDef`, 3 x 10.
- **One decision for the owner:** whether cuttings pile at the chute or load straight into a tipper parked alongside (agent lean: pile
  first, the tipper port later). **Decided by the owner, 9 Nov 2026:** the bare rig drills at 80% of a deep drill's rate until a bit node is
  fitted, so a node buys something; the generator set is built before the rig.
- **Risks:** insects, which the game ties to active deep drilling; the rig must copy that rule, not escape it. A value loop if a portion were
  cheaper or faster than the deep drill's: **this record keeps the game's 10,000 work per portion and its portion sizes** and prices the rig
  above a deep drill. Nothing here changes what a deep drill does.
- **What it depends on:** a ground-penetrating scanner find (deep ore does not exist until a scan creates it, below); the bulk tanker or a
  pipe for deepchem; [the generator set](vehicle-generator-set.md) or a base grid for electric drilling; Rimefeller for the oil well kit.
- **Fixed from the old text:** the hydraulic pump no longer feeds `Phobos_TipRate` (a tipper stat, copied by mistake); the tracks node is gone
  (a node never changes the parts list, principle 8); "depth reached" is now **reach**, because the game has no depth; no gas well (Helixien
  gas is off the owner's list); fuel capacity is added. **Open:** drill-bit wear (the systems' wear phase); a Dubs Bad Hygiene well (low lean).

## What it is and what it does in play

The rig is a heavy wheeled truck with a mast folded along the deck and a rod rack beside it. Driven to a spot the scanner has marked, the
crew sets it up: outriggers down, mast up over the rear, a colonist at the controls. It then works the deposit under and around the hole,
portion by portion, dropping each at the cuttings chute for a tipper or a hauler to take away. When the lump is spent it says so ("The ground
here is spent: only stone comes up") and the crew packs up and moves on. It cannot move while set up. Over a deepchem pocket it raises the
liquid instead, into a small buffer of its own, straight into the tanker parked beside it, or into a pipe laid to the rig.

**Grounding only, labelled.** Real truck-mounted rigs (reverse-circulation and diamond exploration rigs, water-well rigs) are the shape: a
6 x 4 truck, a mast raised hydraulically over the rear, outriggers at the corners, a rod rack, a compressor and a flushing pump. The game's
deep drill is the mechanics; the rig buys mobility and self-containment, never more ore than a built drill would give.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Deploy** (outriggers down, mast up) | Drill where the truck stands, with no building, no power line and no deconstruction afterwards | Move or draft while set up; set up at all when stability or controls are below 0.5 (the refusal names the part) | A deposit far from the base becomes a trip, not a construction project; the rig is a camp you drive |
| **Drilling a lump** | Take the game's portions (45 steel, 10 plasteel, 15 uranium and so on) at the game's work per portion, dropped at the cuttings chute | Drill faster or richer than a deep drill; drill with controls below 0.35; drill without a colonist at the controls (Mining skill) | Ore comes in truckloads: the tipper hauls from the chute, and the driller's Mining skill matters as it does at a deep drill |
| **Reach** (the mast) | Work every ore cell within the drill's radius of the hole; the tall mast widens it | Reach a lump the radius does not cover: the rig must move | Fewer set-ups per lump with the tall mast; a wide lump still takes two or three positions |
| **Exhaustion and chunks** | Keep drilling after the ore is gone and get the bedrock's stone chunks, as a deep drill does | Get anything on a map with no bedrock under the hole; keep taking ore that is not there (the rig says the ground is spent) | A spent lump is a signal to pack up; chunks are a by-product, never the reason to stay |
| **Insects** | Nothing new | Drill for weeks on end with no risk: active drilling can bring a tunnel hive, as it can at a deep drill | Drilling becomes a watch: guards near the rig, a plan to leave; the cooldowns mean one hive, not a stream |
| **Drilling deepchem** (the head node, Vanilla Chemfuel Expanded present) | Raise a deepchem lump into a buffer, into the tanker parked alongside, or into a pipe laid to the rig | Get deepchem as items; drill deepchem without the head; drill with the buffer full and no counterpart (the refusal says so) | The rig and the tanker work as a pair: one drills, the other carries; a pumpjack and its pipeline are no longer the only way to a pocket |
| **The oil well kit** (Rimefeller present) | Drill a well head where there is oil, feeding the kit's steel, then leave; the well head stays for Rimefeller's pipes | Skip Rimefeller's own gates (a console for shallow oil, a powered deep scanner for deep); pump oil itself | An oil field is opened by the rig on a trip, and the derrick's place is taken by the thing that stays: the well head |
| **Electric drilling on camp power** (the power pack node) | Drill on shore power from the generator set or a base grid: quieter, no fuel burned | Drill at full rate on a weak net: a net with little stored and nothing coming in slows it (authored) | At the base the rig runs off the grid; in the field the generator set is the camp's power and the rig its biggest load |
| **The bit nodes** (diamond bit or hammer, one or the other) | Diamond: the deep drill's full rate on ore. Hammer: a faster bedrock pass and a slightly slower ore pass | Fit both; beat a deep drill on ore with either | A choice by job: exploration (ore) or quarrying (chunks); the bit shows on the mast |
| **Lights** (the work lights node) | Drill through the night at the full rate | Nothing (without lights, an unlit hole drills at a reduced rate; authored) | A two-shift rig; the lights are a load on the alternator or the shore line |
| **A destroyed drilling part** | Replace a part at 0% with supplies and go on | Drill with the bit, rotary head or mast at 0%; set up with an outrigger at 0% | The rear is what gets shot: a convoy carries a spare bit and steel, and a rig with a broken mast drives home to be fixed |

## The honest mechanics

### What the game does

- **A portion is 10,000 work.** Each tick of the drill job adds the driller's `DeepDrillingSpeed` times the tick's delta; the yield uses the
  driller's `MiningYield` (all.cs:534020, 534039-534051). `DeepDrillingSpeed` is 1 at Mining 8 (0.04 base plus 0.12 a level, `query-defs`),
  so one portion is 10,000 ticks, four in-game hours, about six portions a day. **A portion takes `min(count present, deepCountPerPortion)`
  from the first ore cell within 21 cells of the drill** (radius about 2.6, the Def's `specialDisplayRadius`), drops
  `max(1, round(portion x yield))` at the interaction cell, and decrements the grid (all.cs:534060-534075, 534203-534224).
- **Exhaustion:** when the valuables run out the drill messages "exhausted", forbids nearby drills with nothing left, and falls back to the
  bedrock's stone chunks; with no bedrock there is no fallback (all.cs:534080-534101, 534226-534243). **The drill job is a pawn at the
  interaction cell calling `DrillWorkDone` every tick; the drill must be a `Building`** (the job casts it, all.cs:287023-287050), so a
  vehicle cannot carry `CompDeepDrill`.
- **Deep ore does not exist until a scanner find creates it:** only the scanner's find (all.cs:567040) and the drill's take (all.cs:534070)
  write `DeepResourceGrid` (an ore and 0 to 65,535 per cell, all.cs:70394-70455). Commonality / per portion / lump cells: Steel 4/45/20-30,
  Plasteel 1/10/2-10, Uranium 1/15/4-10, Silver 0.5/70/2-10, Gold 0.5/8/1-4, Jade 0.5/15/1-4; `deepCountPerCell` 300 by default
  (all.cs:45088-45094). `VCHE_Deepchem` is a deep resource at 2/35/20-40 (`query-defs`).
- **Insects are tied to active drilling, not to exhaustion:** `CompCreatesInfestations` can fire only if the drill was used last tick, with a
  7-day cooldown per drill (420,000 ticks) and a shared cooldown within 10 cells; the storyteller rolls a mean time per drill, and the event
  spawns a tunnel hive spawner of 200 to 1,000 points (0.3 to 0.6 x raid points) near the drill (all.cs:533837-533878, 390629-390666,
  385490-385511). The deep drill itself: 200 W, 1 x 1, Light terrain, 100 steel and 2 components (`query-defs DeepDrill`, as written and patched).
- **Deepchem is piped, not dropped:** PipeSystem's `CompDeepExtractor` is a building that pulls a deep resource straight into a pipe network
  every `ticksPerPortion` ticks, decrementing the grid (`.local/decompiled/PipeSystem-1.7/PipeSystem/CompDeepExtractor.cs:70, 128-207`); the
  precedent is `VCHE_DeepchemPumpjack`: 300 W, 60 ticks a portion, 150 steel and 4 components, research Biofuel refining (`query-defs`).
- **Nothing drills a geyser:** geysers are fixed features read only by the buildings placed on them (`CompPowerPlantSteam`, DBH's
  `CompGeoBoiler`; all.cs:422148-422159, 408601, 348217). No geothermal or water drilling exists in the game.
- **Rimefeller oil** (`.local/decompiled/Rimefeller/Rimefeller/`): two Perlin-seeded `OilGrid`s, shallow and deep, flood-filled into fields
  where the seed is above 0.01 (`MapComponent_Rimefeller.cs:52-58, 373-395`; `OilGrid.cs:60-122`); a field regenerates when unused for 240
  ticks and loses pressure as drawn (`OilField.cs:47-50, 72-80, 119-122`). Shallow wells need a working resource console, deep wells a powered
  `CompDeepScanner` (`PlaceWorker_WellHead.cs`, `PlaceWorker_DeepWellHead.cs`). `CompOilDerrick.Drill` consumes 1 steel per 25,000 units of
  progress, 100 in all, then spawns a well head; a broken-down derrick spills (`CompProperties_OilDerrick.cs:7-15`; `CompOilDerrick.cs:169-185,
  188-207, 240-268`; the progress is the supervising pawn's, `JobDriver_SuperviseDrilling.cs:53`). `DrillQuake` fires above 6 wells (`IncidentWorker_Earthquake.cs:20-22, 38`).
- **Dubs Bad Hygiene groundwater is pre-seeded, never drilled:** `WaterGrid` and `DeepWaterGrid` are made at map start
  (`.local/decompiled/DBH-1.7/DubsBadHygiene/MapComponent_Hygiene.cs:89-93, 661-789`) and wells only read them (`CompWaterInlet.cs:18, 46-63`).

### What we reuse and what we must add

- **The mirror of `DrillWorkDone`.** Our comp on the vehicle (D'thorga Framework's Vehicle-Framework part) copies the deep drill's routine
  against the same grid: `GetNextResource` from the hole cell, the same portion maths, drop, exhaustion message and chunk fallback. Our job
  puts the driller beside the rig and calls the comp every tick; work per tick is `DeepDrillingSpeed` x `Phobos_DrillRate`. **Nothing is faster than the game's.**
- **Deploy** is the tanker's comp on Vehicle Framework's move and draft hooks (`CompPhobosTank.CanMove` and `CanDraft`,
  `src/PhobosFramework.Vehicles/Tanks/CompPhobosTank.cs:299-303`), generalised into the Framework's core as the family's deploy base.
- **Reach is a radius we choose.** The game has no depth, so "tall mast" means more cells from the hole, not deeper: the bare rig uses the
  game's 21-cell pattern (radius about 2.6) and the tall mast widens it (authored, below); the player sees reach as a ring when the rig is selected.
- **The deepchem counterpart, honestly.** The rig cannot be a Vanilla Expanded Framework pipe connector: VEF registers connectors at spawn
  from their neighbours (`PipeNetManager.RegisterConnector`, PipeSystem-1.7 `PipeNetManager.cs:93`), and a thing that moves would leave a
  stale one (the tanker's record says the same). Two routes, neither a connector. **A pipe laid to the rig:** the head reads the deepchem
  net at the rig's contact cells (`PipeNetManager.GetPipeNetAt`, as the tanker's `Port_PipeNet.Find` does, `Port_PipeNet.cs:54-67`) and
  pushes each portion with `PipeNet.DistributeAmongStorage` (`PipeNet.cs:664`). **The tanker alongside:** the head's buffer is the tanker's
  own `CompPhobosTank` at a small capacity with deepchem contents (no new item), and the tanker draws from it through a vehicle-to-vehicle
  port, `Port_VehicleTank`, which does not exist today (the tanker's ports are items, vehicle fuel and pipe nets: `TankPorts.cs:30, 79`,
  `Port_PipeNet.cs:12`); it also lets two tankers share a load, so it belongs in the Framework's Tanks folder. The rig never drops deepchem
  as an item (owner, 9 Nov 2026).
- **The Rimefeller kit is our job that leaves Rimefeller's well head.** Set up over oil with the kit fitted, the rig feeds the kit's steel at
  the derrick's rate (1 per 25,000 units of progress, 100 in all), then spawns the well head Def the derrick spawns (`CompOilDerrick.cs:188-207`;
  its name is read at build). It honours the derrick's gates (console for shallow oil, powered deep scanner for deep) and never pumps: the
  well head and Rimefeller's pipes do that, so `DrillQuake` and the pressure model stay Rimefeller's. **No DBH well:** nothing is drilled in DBH.
- **Why not the game's infestation comp on the vehicle:** `CompCreatesInfestations.CanCreateInfestationNow` skips its "used last tick" test
  when the parent has no `CompDeepDrill` (all.cs:533837-533845), so on a vehicle it would roll hives while the rig sat in the garage. Our comp
  carries the rule itself (below); whether the incident worker can see it, or needs an incident of ours, is settled at build.

### Rates and figures

| Figure | Value | Label |
| --- | --- | --- |
| Work per portion, portion sizes, yield | 10,000 work; the game's per-resource counts; the driller's `MiningYield` | From the code (all.cs:534020, 534060-534075) |
| `Phobos_DrillRate` | Average of the drilling parts' efficiency, times the bit node's figure; the bare rig at full health is 0.8 | Authored (decision 1 for the owner) |
| Bit nodes | Diamond bit: 1.0 on ore and on bedrock. Hammer: 0.9 on ore, 1.5 on the bedrock chunk fallback | Authored; the game has no rock hardness, so "against rock" means the chunk pass |
| Reach | Bare: the game's 21 cells (radius about 2.6). Tall mast: radius about 3.6 (the game's radial pattern to that radius) | Authored, against the deep drill's radius (`specialDisplayRadius` 2.6) |
| Fuel | 30 a day driving, 40 a day drilling (compressor and hydraulics on the one engine), capacity 160 | Authored; the tanker burns 28 driving on 120 (`Vehicle_BulkTanker.xml:330-332`) |
| Electric drilling | On shore power the rate is 1.0 x the net's state: full with stored energy or a positive gain, 0.6 when the net has neither; 0 W of fuel | Authored, through the existing shore-power check (`CompPhobosVehicleSystems.ShorePowered`, `CompPhobosVehicleSystems.cs:983-1004`, reach `shorePowerReach` 1 cell, `Rules_VehicleSystems.xml:22`) |
| Lights | Unlit hole at night: 0.8 x the rate; with the lights node, 1.0 | Authored; the game's own darkness rule for deep drilling was not read |

A Mining 8 driller at the bare rig takes about 4.8 portions a day (about 216 steel from a steel lump); with the diamond bit, six and 270, the deep drill's figure. A steel lump of 20 to 30 cells at 300 a cell holds 6,000 to 9,000 steel, a month or more either way: reach and mobility, not rate, are what the rig sells.

### Insects

The game's rule, restated: a deep drill can bring a tunnel hive only while it is being drilled, never when idle or spent; at most one every
7 days per drill, with a shared quiet within 10 cells; the storyteller sets the odds. The rig keeps every part of it: its comp records "drilled
last tick", holds the 7-day cooldown on itself (so moving never resets the clock), honours the 10-cell shared cooldown with any drill or rig
nearby, and uses the game's own incident and points. **No setting, unless the owner asks.**

### Sources and simplifications

The portion maths, the radius, the exhaustion rule and the infestation rule are the game's and are cited; everything marked authored is
ours; the real rig's mass and power are secondary grounding. One engine does everything (road, compressor, hydraulics, drilling): real rigs
often carry a second deck engine, and folding it into one part is a simplification. The drivetrain model's 30 kW per tonne gives the
20-tonne rig about 600 kW (`Drivetrains.xml:45-69`), more than a real road engine of about 300 kW; the extra stands in for the deck engine.
Deepchem is an item Def with deep-resource fields (`query-defs VCHE_Deepchem`); keeping it liquid-only on the rig is the owner's choice.

### Argued against or later

- **A tracks node:** a node never changes the parts list (principle 8), and the drivetrain picks a tracked archetype only when no part is
  tagged `Wheel`; the rig is wheeled. **A depth model:** the game has none; it would be a figure nothing reads. **Our own deposit grid:** a
  second grid beside `DeepResourceGrid` is a value loop waiting to happen and a save burden. **Geothermal drilling:** geysers are fixed features
  nothing creates. **A DBH well kit:** nothing is drilled in DBH; open, low lean. **Bit wear now:** the systems' wear phase, as a consumable
  quantity beside oil and coolant. **A Helixien gas well:** the mod is off the owner's list (owner, 9 Nov 2026).

## The rig

| Item | Value |
| --- | --- |
| Footprint, drawSize | (3, 10), (4, 11): the heavy trucks' scale set by the tanker on 9 Nov 2026; a mast folded along a 10-cell deck reads as a rig, and a 2-wide rig was two thirds the width of its family |
| Empty mass and power | Vehicle Framework Mass 400 (the tanker is 340); kerb mass about 20,000 kg and a real road engine of about 300 kW (secondary: truck-mounted reverse-circulation rigs; grounding only). The drivetrain model gives about 600 kW at 30 kW per tonne |
| Drivetrain Def | `PhobosDrivetrain_HeavyDiesel` (`Drivetrains.xml:45-69`) |
| Fuel | Chemfuel: 30 a day driving, 40 a day drilling, capacity 160 (authored). Four days' driving, or two driving and two drilling |
| Crew | Driver; one driller at the controls while set up (our job, Mining skill, `DeepDrillingSpeed`); a passenger seat |
| Research | Deep drilling (the game's: 1,000, Industrial, after Microelectronics, at the hi-tech bench; `query-defs DeepDrilling`). Deepchem head: Biofuel refining, the pumpjack's. Oil well kit: the derrick's own research, read at build |
| Cost (authored) | `PhobosVehicles_EngineAssembly` 1, `PhobosVehicles_DrivetrainKit` 1, `PhobosVehicles_ElectricalKit` 1, plus 360 steel, 40 plasteel and 12 components. RimThunder's heavy engine and transmission are accepted substitutes (the rig is above 15 t empty); VVE's car parts are not (under 6 t only). **The floor it must not undercut:** the deep drill's 100 steel and 2 components (its Def, as written and as patched); the rig's drilling parts alone are priced above it |
| Systems loadout | The heavy-diesel drivetrain; radiator, battery, alternator and starter; loads: work lights, controls, the electric power pack when fitted; the deepchem buffer as a tank system when fitted |
| Tie-ins by `IfModActive` folder | Vanilla Chemfuel Expanded: the deepchem head node and its buffer. Rimefeller: the well drilling kit node and its job. Neither required; a player with neither sees a rig that drills ore |
| Prior art | None on Vehicle Framework; the game's deep drill (mechanics); Vanilla Chemfuel Expanded's deepchem pumpjack (a fixed deep extractor) |

## Upgrade tree

Vehicle Framework nodes (`UpgradeNode`: `work`, `ingredients`, `researchPrerequisites`, `prerequisiteNodes`, `disableIfUpgradeNodesEnabled`,
`graphicOverlays`, `refundFraction` 0.5; VF 1.6 decompile `Vehicles.decompiled.cs:34815-34866`); `StatUpgrade` moves a stat (`:33992-34218`),
`CompUpgrade` activates a comp (`:33709-33785`). Node keys are stable once a save can hold them; every figure below is authored.

| Node key | Figure it moves | Cost | Research | Prerequisite or exclusive with | Look |
| --- | --- | --- | --- | --- | --- |
| `DiamondBit` | `Phobos_DrillRate` to 1.0 on ore and bedrock | 30 plasteel, 2 components | Deep drilling | Exclusive with `RcHammer` | A slim coring bit on the mast |
| `RcHammer` | `Phobos_DrillRate` 0.9 on ore, 1.5 on the bedrock chunk pass | 60 steel, 2 components | Deep drilling | Exclusive with `DiamondBit` | A fat hammer bit and a bigger air hose |
| `TallMast` | Reach from radius about 2.6 to about 3.6; `Phobos_RigStability` minus 0.15 (the StatUpgrade's offset against a MinValue stat is verified at build) | 120 steel, 4 components | Deep drilling | None | A taller mast section; the raised overlay is longer |
| `ElectricPowerPack` | Drill on shore power at 0 fuel; rate by the net's state | 80 steel, 6 components | Electricity | None | A cable reel and a switchbox on the deck |
| `WellDrillingKit` (Rimefeller folder) | Enables the well job; the kit holds 100 steel as its supply | 100 steel, 4 components | The derrick's research | None | A casing rack and a cellar frame at the rear |
| `DeepchemHead` (Vanilla Chemfuel Expanded folder) | Enables deepchem drilling; adds the 300-unit buffer (`Phobos_TankCapacity`) and the counterpart ports | 90 steel, 4 components | Biofuel refining | None | A small tank and a hose coupling behind the cab |
| `WorkLights` | Night rate from 0.8 to 1.0; an alternator load | 40 steel, 1 component | Electricity | None | Lamp masts on the cab and the rear |

## Parts

Cells run x from −1 to 1 and z from −4 to 5 on a 3 x 10 footprint: bonnet at z 5 and 4 with the front wheels at z 4, cab at z 3, the deck
from z 2 back, the rear axles at z −1 and −2, the mast folded over z −2 to −4 and raised over the rear. The hole is the cell behind the rear
centre, (0, −5), outside the footprint; the operator stands beside it. Parts are appended only, never reordered; wheels are tagged `Wheel`.

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 190 | External | all, weight 3 | BodyIntegrity | |
| Cab | 50 | External | (−1, 3) (0, 3) (1, 3) | | Crew |
| Engine | 60 | Internal, priority | (−1, 4) (0, 4) (1, 4) | MoveSpeed, Phobos_DrillRate | One engine: road, compressor, hydraulics |
| Transmission | 40 | Internal | (0, 2) | MoveSpeed | |
| Front axle and wheels | 35 | External | (−1, 4) (1, 4) | MoveSpeed | Tagged Wheel |
| Rear axles and wheels | 50 | External | (−1, −1) (1, −1) (−1, −2) (1, −2) | MoveSpeed | Tandem; tagged Wheel |
| Fuel tank | 40 | Internal | (−1, 1) | | VF's fuel-leak reactor |
| Hydraulic power pack | 35 | Internal | (1, 1) | Phobos_RigStability | Raises the mast and outriggers (no longer feeds a tipper stat) |
| Hydraulic lines | 25 | Internal | (0, 1) (0, 0) (0, −1) | Phobos_RigStability | Our hydraulic-leak reactor later |
| Outriggers | 50 | External | (−1, 2) (1, 2) (−1, −4) (1, −4) | Phobos_RigStability | Below the floor the rig will not set up; drawn down when deployed |
| Mast | 70 | External | (0, −2) (0, −3) (0, −4) | Phobos_RigStability | Raised when deployed (overlay); the tall mast node changes the overlay, not the part |
| Rotary head | 45 | External | (0, −3) | Phobos_DrillRate | Turns the string |
| Drill string and rod rack | 40 | External | (−1, 0) (−1, −1) | Phobos_DrillRate | Rods along the deck |
| Drill bit | 25 | External | (0, −4) | Phobos_DrillRate | The bit node sets the label and the figure; wear later |
| Draw-works and winch | 40 | Internal | (0, −1) | Phobos_DrillRate | Trips the string |
| Compressor | 40 | Internal | (1, 0) (1, −1) | Phobos_DrillRate | Air for the hammer and the flushing |
| Flushing pump | 35 | Internal | (−1, −2) | Phobos_DrillRate | Dry drilling is slow |
| Control console | 35 | Internal | (1, −2) | Phobos_RigControl | Below the floor nothing runs |
| Wiring | 30 | Internal | (1, 3) (1, 2) | Phobos_RigControl | |
| Cuttings chute | 20 | External | (1, −3) (1, −4) | | Portions drop beside it |
| Lights | 15 | External | (−1, 5) (1, 5) (−1, −4) (1, −4) | | Load at night |

**Floors (authored, kept from the old text):** setting up needs `Phobos_RigStability` and `Phobos_RigControl` at or above 0.5; drilling
needs `Phobos_RigControl` at or above 0.35; a refusal names the part ("The outriggers are too damaged to set up"). **Reactors:** VF's
fuel-leak reactor on the fuel tank and its explosive reactor on the engine, as the tanker has; our hydraulic-leak reactor later. **What the
rig loses first:** the rear faces fire while it works, so the bit, rotary head, mast, outriggers, chute and rear lights go first; a bit, head
or mast at 0% stops drilling until its supplies (steel and plasteel at the parts' price) arrive; an outrigger at 0% stops it setting up; the
engine at 0% stops everything. The deploy and drilling states are motes and overlays, never text alone.

## Art request

The pilot is the bare body, north, on the tanker's 3 x 10 canvas at drawSize (4, 11): a heavy 6 x 4 truck, bonnet forward, a flat deck
with a folded mast and a rod rack along it, outriggers stowed, nothing raised, no cargo, crew, text or damage. Overlays, each its own north,
east and south on the body's canvas: the mast raised over the rear (a longer variant for the tall mast), the outriggers down, the bit (coring
or hammer, at the mast foot), the power pack (cable reel and switchbox), the well kit (casing rack and cellar frame), the deepchem head (small
tank and coupling behind the cab), the lamp masts lit, and damage states (a bent mast, a missing outrigger) through `ComponentRequirement`.
Icon: the rig side-on, mast raised. Generation under `paid-image-generation-rules.md`, dry run first.
- **Size it first** (owner's rule, 9 Nov 2026): before the first picture, measure the planned footprint against RimThunder's 3 x 10 PLS, the biggest truck on the owner's list, and generate at 1.5 times the drawn size ([the rules](paid-image-generation-rules.md)); the tanker had to grow from 2 x 6 to 3 x 10 after its first look.

## Open questions for the owner

| Question | Agent lean |
| --- | --- |
| Should the bare rig drill at 0.8 of a deep drill's rate until a bit is fitted, or at 1.0 from the start? | **Decided by the owner, 9 Nov 2026: 0.8.** A node must buy something, and the rig's mobility is already its advantage |
| A Dubs Bad Hygiene well kit? | No: nothing is drilled in DBH; it would be a construction shortcut |
| Drill-bit wear now, or in the systems' wear phase? | The wear phase, as a consumable quantity beside oil and coolant |
| A setting for insects (off, or a longer cooldown)? | None unless asked: the game's rule, honestly copied, is the design |
| Reach figures: 2.6 bare and 3.6 with the tall mast? | Yes; widen only if play shows lumps need more than three positions |
| Do cuttings pile at the chute, or load straight into a tipper alongside? | Pile first; a tipper port is a later node once the tipper exists |
| Build order: before or after the generator set? | **Decided by the owner, 9 Nov 2026: after the generator set.** This record's own lean was the rig first, to prove the deploy base and the drilling job the family shares; the generator set will have to prove the deploy base instead |

## Who chose what

- **Owner (9 Nov 2026):** one record per vehicle, research only; no Helixien gas hooks; deepchem as the family tie, into the tanker or a
  pipe, never an item, no new item; the Rimefeller well kit; DBH wells not drilled; the pump off `Phobos_TipRate`; the tracks node dropped;
  "depth" becomes reach; fuel capacity added; bit wear deferred; camp power through `ElectricPowerPack`; our part items always exist and set
  repair costs, RimThunder's heavy parts as substitutes; the insect rule kept honest.
- **Owner (9 Nov 2026, on reading this record):** the bare rate of 0.8; the generator set before the rig.
- **Agent defaults (owner to confirm):** the 3 x 10 footprint and every figure marked authored (the bit figures, the reach
  radii, the shore-power rule, the 300-unit buffer, the part cells and healths); `Port_VehicleTank` as the tanker-to-rig route; the deploy
  comp generalised into the Framework core; the Rimefeller kit honouring the derrick's gates; the open-question leans. **Correction to the
  brief:** the deep drill costs 100 steel and 2 components, not 150, in its Def as written and patched.

## Sources

- The game, decompiled (`.local/decompiled/full/all.cs`) at the lines cited; Defs through `python scripts/query-defs.py show` (`DeepDrill`,
  `DeepDrillingSpeed`, `DeepDrilling`, `VCHE_Deepchem`, `VCHE_DeepchemPumpjack`) and `patched DeepDrill` (file of 8 Nov 2026). Decompiles in
  `.local/decompiled/`, summarised in our words: PipeSystem 1.7, Rimefeller, Dubs Bad Hygiene 1.7 and Vehicle Framework 1.6, at the files
  and lines cited above.
- Ours: `src/PhobosFramework.Vehicles/Systems/CompPhobosVehicleSystems.cs`, `src/PhobosFramework.Vehicles/Tanks/` (`CompPhobosTank.cs`,
  `TankPorts.cs`), `src/PhobosFramework.Vehicles.Pipes/Port_PipeNet.cs`, the Framework's `VehicleSystems/Drivetrains.xml`, `Tanks.xml` and
  `Rules_VehicleSystems.xml`, and `mods/PhobosVehicles/Defs/Phobos/Vehicles/BulkTanker/Vehicle_BulkTanker.xml`.
- Grounding only, secondary and unverified in detail: manufacturers' descriptions of truck-mounted reverse-circulation, diamond and
  water-well rigs (15 to 25 tonnes, road engines of about 300 kW, a deck engine for the compressor); no figure from them drives a rule.
