# Refuelling from cargo on the world map (18 Nov 2026)

**Status (18 Nov 2026):** Built in D'thorga Framework 0.137.0. **Not yet tested in game** (row 193). Not built: refuelling from cargo on a
map, or in flight.

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

## Not built

- **On a map** (owner's decision 4). A drafted convoy far from base still needs a colonist to refuel it.
- **In flight** (decision 5). An aircraft's range before launch counts its tanks, as Vehicle Framework's does.
