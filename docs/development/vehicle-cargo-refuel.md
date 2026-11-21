# Refuelling from cargo on the world map (18 Nov 2026)

**Status (18 Nov 2026):** Built in D'thorga Framework 0.137.0 (caravans, row 193) and 0.139.0 (aircraft setting down on a journey, row
197). **Not yet tested in game.** Not built: refuelling from cargo on a map, or pouring into an aircraft in the air.

The owner asked for a vehicle to top its tank up from the fuel its caravan carries, "without you making a bunch of stops across the
world map", with the range worked out again to count it.

## The owner's decisions (18 Nov 2026)

| # | Decision | Whose |
| --- | --- | --- |
| 1 | A toggle per vehicle, shown when its fuel is in the caravan's cargo | owner |
| 2 | **Off until the player switches it on**, so fuel carried to trade is never burned | owner (agent's recommendation) |
| 3 | **Top up when the tank falls to a quarter, with a short stop** (a quarter hour for a whole tank, authored) | owner, against the agent's recommendation of no stop |
| 4 | **World map only**; on a map colonists already refuel from stockpiles | owner (agent's recommendation) |
| 5 | Aircraft only on the ground: nothing pours cans into an aircraft in flight; the ferry tank is for that | agent default, from the physics |

## What it lets, stops and changes

| Lets the player | Stops them | Changes how they play |
| --- | --- | --- |
| Send a long convoy with cans of fuel aboard and leave it to drive; see the true reach, cans included | Nothing they could do before | Fuel becomes cargo worth planning: how many cans for how far. The stops cost a little time, so a long trip on cans is slightly slower than one on full tanks |

## How it works

- **The switch** is saved on the vehicle (`CompPhobosVehicleSystems.RefuelFromCargo`, key `PhobosVehicleSystems_RefuelFromCargo`).
  The gizmo, "Refuel *vehicle* from cargo", shows on the caravan while it carries that vehicle's fuel, or while the switch is on.
- **The look** is the caravan comp's, every 250 ticks, staggered (`CargoRefuel.Tick`). While the caravan is on its way somewhere,
  every vehicle with the switch on and its tank at a quarter or less is filled, emptiest first, from the caravan's cargo.
- **What it pours** comes through Vehicle Framework's own `CompFueledTravel.ConsumeFuelFromInventory`, the method its refuel button
  calls, so it takes from the same places: every vehicle's cargo and every pawn's.
- **The stop.** Pouring a whole tank costs a quarter hour (`refuelStopHoursPerTank`), less in proportion. It is added to the leg the
  caravan is on (`nextTileCostLeft`), which Vehicle Framework pays at one a tick, so the caravan arrives that much later. No pause
  flag is touched, so the player's own pause is never undone.
- **Messages:** each top-up says what it poured and what is left, quietly; running out of cans is said once.
- **The reach** (`VehicleRange.BudgetTicks`, `CargoFuelRule.BudgetTicks`): a convoy moves as one, so every vehicle burns for the same
  time. Vehicles on one kind of fuel with the switch on share what is carried: they last (their tanks + the cans) ÷ their burns added.
  A vehicle with the switch off still runs on its tank alone and can limit the convoy. The inspect line says when cans are counted,
  and the range ring on the globe follows.

## Simplifications (authored)

- The engine is taken to burn on through the stop, at its moving rate: a quarter hour's fuel on a whole tank, small.
- The range counts driving time, not the stops.
- The quarter hour for a whole tank is authored, not measured; a real 20-litre can takes a minute or two.

## Other factions' vehicles

- **In a raid on us, or parked at a base we attack:** not on a world-map caravan, so none of this applies.
- **Other factions' caravans** are left alone: the look runs only for the player's caravans. Nothing of theirs is refuelled or stranded
  by it.
- **Captured:** a vehicle taken into one of the player's caravans has the switch off until the player turns it on.
- **Cost:** one look per caravan every 250 ticks, a list of its vehicles; the cargo is read only for a vehicle that wants fuel.

## Aircraft: setting down on a journey (Framework 0.139.0)

The owner asked for aircraft to refuel at a landing partway (18 Nov 2026), after finding the caravan switch missing on a Cherokee on the
colony map. Decision 5 stands: nothing pours into an aircraft in the air. Instead the aircraft lands on the way.

### Decisions (18 Nov 2026)

| # | Decision | Whose |
| --- | --- | --- |
| 6 | Build it: an aircraft on a journey sets down partway to refuel from the fuel it carries | owner |
| 7 | **Automatic, at a quarter tank**, with the same per-vehicle switch as caravans, rather than refuel stops the player marks on the route | agent default: it matches the caravans, and Vehicle Framework's route has no way to mark a waypoint as a stop |
| 8 | **It sets down on open land only**: not water, not impassable mountains, not a tile with a hostile faction's site | agent default |
| 9 | **The stop** is the caravans' pouring time (a quarter hour for a whole tank) plus 0.1 hours to come down and lift off (`refuelLandingHours`, authored). The engines are off: the stop burns nothing | agent default |
| 10 | **A destination beyond the tank's reach can be picked** when the tank and the fuel aboard cover it, with the number of stops in the tooltip | agent default: without it the switch only adds safety margin |
| 11 | **Fuel aboard** is the aircraft's own cargo and its riders' pockets, the fuel that leaves with it | agent default |

### What it lets an aircraft do, stops and changes

| Lets the player | Stops them | Changes how they play |
| --- | --- | --- |
| Fly a helicopter or jet beyond its tank's reach with cans aboard, and see the true reach | Nothing they could do before | Cargo space becomes range: cans aboard trade against load. A route over land is longer-legged than one over the sea, and an enemy's country cannot be used for a stop |

### How it works for aircraft

- **The switch** is the caravans' (`RefuelFromCargo`). Its button, "Refuel *aircraft* from cargo", shows on the aircraft on a map and in
  flight while it carries its fuel aboard or the switch is on (`CargoRefuel.AircraftToggle`), with its own description.
- **The look**: every 250 ticks in flight (`WorldObjectComp_PhobosFlightSystems`, `CargoRefuel.FlightLook`). With the switch on, fuel
  aboard and the tank at a quarter or less, it takes the tile beneath it (Vehicle Framework's `WorldHelper.GetNearestTile`) and checks it
  (`NoLandingReason`). Where it can land, it pours what fills the tank and takes those units out of the cargo and pockets.
- **The stop**: Vehicle Framework moves an aircraft and burns its fuel only while `CompVehicleLauncher.inFlight` is true. We set it false
  for the stop and true again at the end (`CargoRefuel.HoldTick`, checked every tick while down). The route is never touched, so the
  aircraft goes on to the same destination. The end tick is saved (`PhobosVehicleSystems_LandedRefuelUntil`). After a load, Vehicle
  Framework puts every journey back in the air, so the stop is put back once. A new order from the player during a stop ends it.
  Recon circling is left alone.
- **Messages**: setting down (what was poured, what is left, when it lifts off) and lifting off, quietly; the inspect pane says it is down.
  "Cannot set down here" and "no fuel left aboard" are each said once.
- **The reach**: in flight, the range line and the circle add the fuel aboard while the switch is on (`FlightRange.Of`). When picking a
  destination, two postfixes on Vehicle Framework's `FuelTargetUpdater` (`ValidateTargets`, `LaunchStatus`) allow a journey the tank alone
  cannot cover where `CargoFuelRule.StopsForJourney` says the tank and the fuel aboard can. The line is yellow where they would not bring
  it back. Every other refusal (beyond the aircraft's own limit, no target) stands.
- **Waking**: a world pawn that is not in a caravan is put to sleep at the game's six-hourly sweep; our existing patch keeps the player's
  aircraft awake in flight, and now during a stop too.

### Aircraft simplifications (authored)

- It sets down and lifts off where it is: no landing site is chosen and no map is made, and nothing can attack it on the ground.
- Any aircraft may set down on any open land, as Vehicle Framework's own world-map landings allow; a jet needs no runway.
- The reach when picking a destination assumes every can is poured; it does not account for a stop being put off over water. **A long sea
  crossing still needs the fuel in the tank**, and the player guide says so.
- The look is every 250 ticks, so the tank may be a little under a quarter when it sets down.

### Other factions' aircraft

- **In a raid on us, or parked at a base we attack:** this is on the world map only, so none of it applies.
- **Other factions' aircraft in flight** are left alone: the look runs only for the player's.
- **Captured:** the switch starts off.
- **Cost:** one look per player aircraft in flight every 250 ticks; one field read every tick while it is down.

## A towed fuel bowser (Framework 0.219.0, 22 Nov 2026)

**The gap:** a fuel bowser on the hitch gave its caravan nothing on the world map. Vehicle Framework's own refuelling takes fuel items
from the caravan's members, and a bowser holds litres in a tank, riding in the hitch outside the caravan. The agent found it on
22 Nov 2026; the owner said proceed. Row 335 of [the in-game test pass](in-game-test-pass.md). Not yet tested in game.

- **What it lets the player do:** run a long trip on a towed bowser, as on cans. **What it stops:** nothing. **How it changes play:**
  the bowser earns its tow: 2,500 litres behind a truck is a reach no load of cans gives. **What outside notices:** the caravan's
  reach ring, its pace (the same short stop) and the bowser's litres.
- **How:** `CargoRefuel.Carried` counts, beside the cargo, every towed tank compartment whose contents a hose fills this vehicle's fuel
  from (`Port_VehicleFuel`), one tank unit for one unit of fuel, as on a map. The top-up takes from towed tanks first, then the cans.
  The reach estimate reads the same count, so the ring grows by itself.
- **Risk: low** (agent's grade): it uses fuel the player put in the bowser; nothing is lost. It needs the vehicle's own **Refuel from
  cargo** switch, as cans do; no new setting.

## Not built

- **On a map** (owner's decision 4). A drafted convoy far from base still needs a colonist to refuel it.
- **In the air** (decision 5). An aircraft refuels only by setting down (decisions 6 to 11).
- **Refuel stops the player marks on the route.** Vehicle Framework's route has no way to mark a waypoint as a stop.
