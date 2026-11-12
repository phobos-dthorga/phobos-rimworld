# Vehicle upgrade families: upgrades shared across many vehicles (12 Nov 2026)

**Part built, nothing tested in game.** Built in D'thorga Framework 0.103.0 and Vehicles 0.47.0 (parts 1 and 2 of 3, 12 Nov 2026): the order for a whole family, the cold-weather pack, all-terrain tyres and work lights, then long range. Recovery follows in part 3. The sections "Built in part 1" and "Built in part 2" say what was built and where it departs from the proposal; the text before it is the proposal as the owner answered it. On 11 Nov 2026 the owner chose both a signature feature per
vehicle and "shared families" for the rest ([round two](vehicle-upgrades-round-two.md)); on 12 Nov 2026 they asked for this record
first, with nothing built. **Every family, figure and fit below is agent-proposed** for the owner to pick from. Facts marked "from
the code" were read on 12 Nov 2026 from our source and Vehicle Framework's 1.6 decompile with `scripts/query-code.py`.

## Read this first

- **Five families proposed**: recovery, long range, cold weather, work lights and all-terrain tyres. Each is a set of fittings that
  any vehicle can take where it has the right part, so one family serves dozens of vehicles.
- **They are fittings, beside other authors' trees** (owner, 11 Nov 2026). Nothing goes into another author's upgrade tree, and nothing
  changes another author's picture.
- **Who gets one is decided by effects, never by a list of vehicle names.** A fitting names a loadout role (`Wheels`, `Engine`, a
  cabin) and can match on what the vehicle is (piston or turbine, tracked or wheeled, its mass), as the fittings already do
  (`PhobosVehicleFittingDef`: `roles`, `piston`, `tracked`, `minKilograms`, `maxKilograms`).
- **Three of the five are mostly XML** on systems that already exist. Two need a small piece of code each (said per family).
- **Not proposed:** dust filters (nothing models dust or intake wear today, so it would be a new system, not a family) and armour
  (Vehicle Framework's own upgrade trees and Combat Extended already own a vehicle's armour; a second armour path would fight them).
- **Four questions for the owner** are at the end. Nothing is built until they are answered.

## What already exists to build on

From the code:

| What | Where | What a family can do with it |
| --- | --- | --- |
| Fittings by role, with stat and parameter factors, abilities (`grants`) and matches | `src/PhobosFramework.Vehicles/Defs/FittingDefs.cs`; `Fittings.xml` | Most families are new Defs of this type |
| Fit sets (a named group of fittings and tunes) | `PhobosFitSet_*` in `Fittings.xml`, used today for other factions' vehicles | A family could be ordered in one go as a set |
| The hitch and the towed mass | `src/PhobosFramework.Vehicles/Hitch/`, `HitchRule`, `TowedMassKg` (Framework 0.101.0) | Recovery tows through the same holder and drivetrain |
| The crawl floor: a vehicle without grip still crawls at 15% of its speed | `DrivetrainModel.CrawlFloor` | Recovery has something to rescue: a crawling vehicle |
| Tyre grip from tread wear | `TreadRules.GripFactor`, multiplied into the ground's grip by the drivetrain | All-terrain tyres move the same grip |
| Cold starts, block heater, night heater, cab heater | `Systems_Cooling.xml`, `Systems_Cabin.xml`, `PhobosFitting_BlockHeater` | Cold weather bundles what exists |
| Electrical loads cost engine power | `AccessoryRules.EngineShare` | Work lights have a real cost |
| The fuel range on the world map | `VehicleRange` | Long range shows its effect at once |
| Fuel capacity | Vehicle Framework's fuel comp holds it as a number, not a stat (`VehicleStatDefOf` has no fuel capacity) | Long range needs a small code hook |

## The families

### 1. Recovery

A winch and recovery straps: one vehicle pulls a bogged, crawling or broken one out and tows it home.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Get a vehicle out of mud or home after a breakdown without a helicopter | Nothing new; a recovery tow is slow and heats the tractor | A trip into soft ground takes a recovery vehicle along, as real convoys do |

- **Suits:** land vehicles with our drivetrain (the tow goes through it), heavier ones best; the drivetrain decides whether the pull is
  possible, so no vehicle list is needed.
- **Builds on:** the hitch (a recovery tow is the hitch's slice 4 item, held by the same comp) and the crawl floor.
- **Cost and risk:** the winch pull on a crawling vehicle is new code (a short job and a rule); the tow itself reuses the hitch. Medium.
- **Other factions:** a raider's vehicle runs the lite tier, so it never bogs in our model; at a base we attack, a broken vehicle of
  theirs can be towed off once claimed (the claim order, Framework 0.97.0); a captured recovery vehicle works as ours.

### 2. Long range

Auxiliary tanks: more fuel aboard for long trips, paid for in cargo space and weight. On a helicopter, ferry tanks.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Reach a far site or a second colony without refuelling on the way | Carrying as much cargo (the tank takes space) | A long trip is planned as fuel against load |

- **Suits:** any vehicle with a fuel tank; aircraft too, as ferry tanks that take cabin space.
- **Builds on:** the range outline on the world map (Framework 0.85.0), which shows the gain without new interface.
- **Cost and risk:** the fitting needs a fuel capacity factor, which today's fitting Def does not have, and a hook that raises Vehicle
  Framework's fuel capacity number while the fitting is on. Small code; the risk is a vehicle saved with more fuel than its capacity once
  the fitting comes off (to be capped on removal, never deleted silently).
- **Other factions:** nothing: other factions' vehicles do not travel the world map on our fuel rules. A captured vehicle keeps its tanks.

### 3. Cold weather

One order fits a cold-climate pack: the block heater, a deep-cycle battery and a night heater switched on by default.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Run a winter camp or an ice-sheet colony without cold-start wear and flat batteries | Nothing; it costs materials and a little fuel overnight | Cold maps stop punishing vehicles that were prepared for them |

- **Suits:** any vehicle with an engine role (where the block heater fits today) and a starting battery.
- **Builds on:** fittings that exist (`PhobosFitting_BlockHeater`, `PhobosFitting_DeepCycleBattery`) and the fit set type. **Pure XML**,
  plus one player-facing order to fit a set (the sets are only given to other factions today).
- **Cost and risk:** low. The only new thing is letting the player order a set.
- **Other factions:** a raider's vehicle on the lite tier has no cold model, so the pack means nothing there; this is said, not hidden.

### 4. Work lights

Lamps on a mast or roof bar that light the ground round a parked, working vehicle.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Keep a drill rig, survey truck, tipper grab or generator set working at night at full pace | Leaving the engine off while they burn: the lamps draw on the battery | Night shifts at a remote site become possible, at a fuel cost |

- **Suits:** vehicles with a set-up job (the set-up comp) and an electrical system; the survey truck and drill rig already carry work
  light nodes in their own trees, and this extends the idea to any such vehicle as a fitting.
- **Builds on:** the electrical load rule (lamps cost engine power through `AccessoryRules`); the game's own light (a glow while set up).
- **Cost and risk:** small code: a glow that follows the vehicle while set up, and its draw added to the electrical load. Check first
  whether the existing work light nodes already light the ground or only change a figure.
- **Other factions:** a vehicle parked at a base we attack could show its lamps at night, which makes it a visible target; cheap, since
  the glow is the game's own and only on while set up.

### 5. All-terrain tyres

A deeper, blockier tread: more grip on soft ground, slower and louder on a road. The opposite of the road tyres that exist.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Cross mud and sand without bogging | Running as fast on a road; the tread also wears faster on hard ground | Tyres become a choice made for the map, not a default |

- **Suits:** wheeled vehicles (the `tracked` match excludes tracks); fit every axle for the full effect, as road tyres say.
- **Builds on:** `TreadRules.GripFactor`, which the drivetrain already multiplies into the ground's grip; road tyres as the pattern.
- **Cost and risk:** the fitting needs a grip parameter for the tyres, which the tread rule does not take yet: a small code change and a
  test. Otherwise XML.
- **Other factions:** the lite tier ignores grip, so nothing changes for a raider; an enemy fit set could list the tyres for flavour only.

## Order (agent default)

1. **Cold weather**, pure XML and a player order for fit sets that every later family can use.
2. **All-terrain tyres**, one small rule change.
3. **Long range**, one small hook, with the save case handled.
4. **Work lights**, after checking what the existing nodes do.
5. **Recovery**, together with the hitch's slice 4, since it is the same comp.

One version and one test list for the batch, as the owner prefers.

## Questions for the owner

**Answered by the owner, 12 Nov 2026:** (1) **all five**, work lights and all-terrain tyres included ("Both work lights and all-terrain
tires sound good"), in the order above; (2) recovery and the hitch's slice 4 **as one piece**; (3) **both**: a whole family in one order, or
each part on its own; (4) long range **on aircraft too**, as ferry tanks. Nothing is built yet: the build is the next batch, on the owner's go.

The questions as they were put:

1. **Which families**, of the five, and in what order? (Agent default: all five, in the order above.)
2. **Recovery and the hitch's slice 4:** build them as one piece, or the winch alone first? (Agent default: as one.)
3. **Fit sets for the player:** may the player order a whole family in one go, or fit each part on its own? (Agent default: both.)
4. **Long range on aircraft:** ferry tanks in the cabin too, or land vehicles only? (Agent default: aircraft too.)

## Not proposed, with the reason

- **Dust and sand filters:** nothing models dust or intake wear; it would be a new system.
- **Armour:** owned by Vehicle Framework's trees and Combat Extended.
- **Trains of the Rim's steam tree:** skipped by the owner on 12 Nov 2026 while that mod looks dormant (last updated 26 Aug 2025, declares
  1.6 only). Raise it again if the mod is updated.

## Built in part 1 (Framework 0.103.0, Vehicles 0.47.0, 12 Nov 2026)

**Not yet tested in game.** Built by the agent from the owner's answers; every figure is authored.

### The order for a whole family

- **A Def of its own, `PhobosVehicleFamilyDef`** (`src/PhobosFramework.Vehicles/Defs/FittingDefs.cs`), rather than the enemy fit sets: a
  family names fittings and the switches it turns on (`switchOn`), and nothing else. Parts 2 and 3 add families as Defs in
  `Families.xml`, with any new fittings beside them; they need no new code for the order.
- **Which vehicles it suits** follows from its fittings: it is offered where any of them suits a part (`VehicleFittings.Families`).
- **The order** (`VehicleFittings.FamilyPlan` and `OrderFamily`): each fitting, in the family's order, on every part of the vehicle it suits
  that does not carry it and is not already waiting for it, one fitting to a part. A part another fitting holds is fitted over, and the
  details strip says which. Refused parts (destroyed, a salvaged grade, research unfinished) are left out and the first reason is given.
  Each part is its own job for a mechanic, as before; the family's switches are turned on at once.
- **The window**: a Families section above the parts, one row a family, its picture button **Fit all** in the look of the other choices
  (lit green when every part it suits has it, amber while its work waits, dimmed with its reason). The details strip lists the work,
  materials, hours and skill.

### Cold weather

- Built as proposed: the block heater and deep-cycle battery fittings, and the night heater switched on. **The night heater still needs its
  setting** (vehicle systems settings, off by default); the pack switches the vehicle's own switch, not the setting.

### All-terrain tyres

- **`TreadRules.SoftGroundFactor`** (game-free, five tests): the tyre's factor applies in full on the softest going (the drivetrain's ground
  grip 0.3) and fades to none on firm ground (0.8), in step with the ground's grip. The tyres give 1.35 (authored).
- Read through a `softGroundGrip` parameter on the running gear, which the fitting's parameter factor moves, so the window's figures show it.
- **Costs**: 0.95 of the speed and 1.15 times the wear, everywhere (a simplification: the proposal said "on a road").

### Work lights

- **Checked first, as proposed:** the survey truck's and drill rig's own work light nodes set the `Phobos_WorkLights` stat, which shows their
  lamps' switch and spares the drill rig its night penalty. **Neither lit the ground.**
- **Agent choice: a lamp bracket on every land vehicle**, not only those with a set-up job. The fittings model puts one fitting on one
  part, and the roles a land vehicle has already take fittings (the battery its deep-cycle battery, the alternator its heavy one), so the
  lamps needed a part of their own. It is appended in the land base loadout, as the radiator, water pump, belt, brakes and clutch already
  are, so another author's vehicle gains a part on its Health tab and nothing in its picture.
- **The lamps** (`PhobosFitting_WorkLamps`) grant `WorkLights`. Their switch (`workLamps`, in the light and heavy land loadouts) shows once
  they are fitted (`When_Granted`) and runs at night while the vehicle stands still: 10 A at 12 V or 5 A at 24 V (two 60 W LED floods,
  authored).
- **The light is the game's own** (`WorkLamps`): a glower registered with the map's light grid while the lamps burn and taken off when they
  stop, when the vehicle moves and when it leaves the map. It is never added to the vehicle's Def and never saved. Anyone working inside it
  works at the game's own lit pace, and the drill rig counts the lamps as its work lights (`WorkLamps.Fitted`).
- **Limits:** the generator set lists its own parts (`Inherit="False"`), so it has no bracket. The survey truck and drill rig can carry both
  their own lights and the lamps, and then draw both. The light is checked at the vehicle's rare tick, so it can lag a stop or a start by
  a few seconds.

### Other factions' vehicles

| Where | Cold weather | All-terrain tyres | Work lights |
| --- | --- | --- | --- |
| **A raid on us** | Nothing: the lite tier has no cold model | Nothing: the lite tier ignores grip | Nothing: the lamps are the player's to fit, and the lite tier lights nothing |
| **Parked at a base we attack** | Nothing | Nothing | Nothing yet: no enemy fit set lists the lamps |
| **One we capture** | Fits as ours | Fits as ours | Fits as ours; it has the bracket like every land vehicle |

Cost with several on a map: a family is worked out only while its window is open (every half second). The lamps add one check to a
vehicle's rare tick, and their light is the game's own glower, registered only while they burn.

## Built in part 2 (Framework 0.103.0, 12 Nov 2026)

**Not yet tested in game.** Built by the agent from the owner's answers (long range on aircraft too, as ferry tanks); every figure is
authored.

### Long range

- **Two fittings, one family.** The **auxiliary fuel tank** (`PhobosFitting_AuxFuelTank`: capacity ×1.5, cargo ×0.85) goes on a new
  **auxiliary tank cradle**, appended in the combustion base loadout, so every fuelled land vehicle and boat has one. The **ferry tank**
  (`PhobosFitting_FerryTank`: capacity ×1.75, cargo ×0.7) goes on new **ferry tank tie-downs**, appended in the aircraft base loadout, so
  every fuelled aircraft has them. Each vehicle has only the mount for its own, so the family `PhobosFamily_LongRange` names both and
  fits whichever suits. Electric vehicles have neither: they carry no liquid fuel.
- **The capacity hook, from the code.** Vehicle Framework's `CompFueledTravel.FuelCapacity` is its Def's figure passed through
  `VehicleStatHandler.GetStatOffset(VehicleStatUpgradeCategoryDefOf.FuelCapacity, ...)`, its own extension point for upgrades that add to a
  tank; `AddStatOffset` adds to it, and the offsets are not saved. So a fitting gained `fuelCapacityFactor`, and
  `VehicleFittings.SyncFuelCapacity` adds the difference between what the tanks should add (`FittingRule.FuelCapacityOffset`: the
  capacity without them times the factor less one) and what it last added, held in a field that is never saved. **No Harmony.**
- **Save and load.** Because neither our figure nor Vehicle Framework's offset is saved, both start from nothing after a load. The sync
  runs in the existing post-load hook on `VehicleStatHandler.ExposeData` (where part grades are put back), which reaches a vehicle in a
  caravan too, and again in the systems comp's `PostSpawnSetup`; it is idempotent. Vehicle Framework saves the fuel itself without
  trimming it, and trims to capacity only when it burns fuel on a map (`ConsumeFuel`), which comes after both.
- **Removal (agent choice).** Taking a tank off with more fuel aboard than the vehicle's own tank takes **drains the whole units above
  into containers of the vehicle's fuel beside it**, with a message; the fraction is lost in the draining (the game counts fuel in whole
  items). A tank destroyed with its mount **loses** that fuel, with a message saying how much. Removal is never refused: the player can
  always undo a fitting. A vehicle off the map loses the excess, since there is nowhere to put containers.
- **What follows the bigger tank without new code:** the refuel gizmo and refuelling jobs (Vehicle Framework fills to a share of
  `FuelCapacity`), our depot bay and the tanker's ports (both read `FuelCapacity`), and the range outline on the world map, which reads
  the fuel aboard.
- **Which aircraft.** Every fuelled aircraft on the aircraft loadout, helicopters and jets alike: the Cherokee, the Louie, the Hind (VTOL
  Vehicle) and the Harrier (the fixed-wing framework) all use Vehicle Framework's own fuel comp, and no mod on the owner's list overrides
  its capacity (read 12 Nov 2026), so the offset reaches them all.
- **Limits, said plainly.** The tank's weight and its fuel's are counted only as the cargo space it takes, not as mass on the drivetrain.
  A design whose capacity Vehicle Framework sets outright (an override in its settings) ignores offsets, so a tank adds nothing there.
  Removing the mod with a tank fitted keeps the fuel until it is next burnt, when it is trimmed to the vehicle's own capacity.
- **Checked:** `FittingRule.FuelCapacityOffset`, `FuelAbove` and `WholeUnits` have unit tests; the self-test fits a tank to its test
  vehicle, checks the capacity rises by the factor, that a second sync adds nothing, and that it goes back when the tank comes off.

### Other factions' vehicles (long range)

| Where | Long range |
| --- | --- |
| **A raid on us** | Nothing: no enemy fit set lists a tank, and other factions' vehicles do not travel the world map on our fuel rules |
| **Parked at a base we attack** | Nothing yet |
| **One we capture** | Fits as ours; it has the cradle or tie-downs like every vehicle of its kind |

Cost with several on a map: the sync runs only when a fitting changes, a vehicle loads or one spawns; nothing per tick.

## Who chose what

- **Owner:** families as well as signatures (11 Nov 2026); our features beside other authors' trees, never inside (11 Nov 2026); a design
  record first, nothing built (12 Nov 2026); Trains of the Rim skipped (12 Nov 2026).
- **Agent-proposed:** every family, its fit, its order, and what is left out.

## Sources

- Vehicle Framework, by Smash Phil: its stats and fuel comp, read from our decompile and summarised in our own words.
- Our own code and Defs, cited by path above. No figure is set here, so none needs a source yet.
- The tanks' factors (1.5 and 1.75) and cargo costs are authored. That real ferry tanks can more than double an aircraft's fuel is general
  knowledge, not a cited figure, and no figure here rests on it.
