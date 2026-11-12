# Vehicle upgrade families: upgrades shared across many vehicles (12 Nov 2026)

**A proposal. Nothing here is built, and nothing is tested in game.** On 11 Nov 2026 the owner chose both a signature feature per
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

1. **Which families**, of the five, and in what order? (Agent default: all five, in the order above.)
2. **Recovery and the hitch's slice 4:** build them as one piece, or the winch alone first? (Agent default: as one.)
3. **Fit sets for the player:** may the player order a whole family in one go, or fit each part on its own? (Agent default: both.)
4. **Long range on aircraft:** ferry tanks in the cabin too, or land vehicles only? (Agent default: aircraft too.)

## Not proposed, with the reason

- **Dust and sand filters:** nothing models dust or intake wear; it would be a new system.
- **Armour:** owned by Vehicle Framework's trees and Combat Extended.
- **Trains of the Rim's steam tree:** skipped by the owner on 12 Nov 2026 while that mod looks dormant (last updated 26 Aug 2025, declares
  1.6 only). Raise it again if the mod is updated.

## Who chose what

- **Owner:** families as well as signatures (11 Nov 2026); our features beside other authors' trees, never inside (11 Nov 2026); a design
  record first, nothing built (12 Nov 2026); Trains of the Rim skipped (12 Nov 2026).
- **Agent-proposed:** every family, its fit, its order, and what is left out.

## Sources

- Vehicle Framework, by Smash Phil: its stats and fuel comp, read from our decompile and summarised in our own words.
- Our own code and Defs, cited by path above. No figure is set here, so none needs a source yet.
