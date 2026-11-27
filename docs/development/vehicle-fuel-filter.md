# A fuel filter that clogs (28 Nov 2026)

**Status (28 Nov 2026):** a design record; nothing is built. The owner chose by card to build all three of the schematic's missing parts,
"each with its own record first" ([the schematic record](vehicle-schematic.md), M1). Building waits for the owner's answers below.

## Read this first

- **What it is**: every fuelled vehicle gets a fuel filter that clogs as fuel passes through it, faster with dirty fuel, and is
  changed at the engine service. A clogged filter feeds the engine poorly.
- **The agent's lean caps the harm**: fully clogged, the engine gives three quarters of its power, as a weak fuel pump does today. It
  never stops an engine, so the risk is low and the setting starts on (question 1).
- **Dirty fuel is new**: today stale fuel in a depot simply disappears ([depots, DP9](archive/vehicle-depots.md)). This record proposes
  that a tank filled from a stale store carries dirt as well (question 2).
- **Nothing here is tested in game.** Marks: **[O]** the owner's, **[A]** the agent's, **[code]** read from our source or Defs.
  Real-world statements from general knowledge, not looked up, say **unverified**.

## What a fuel filter does

Diesel and turbine fuel systems carry filters that catch dirt, rust and water before they reach the injection pump and injectors;
diesels often have a water separator ahead of the fine filter. A filter that clogs starves the engine first at high load, as power
falls off when it is asked for most, and is changed at routine service (general knowledge, **unverified**: no source was looked up).

## What exists to build on [code]

| Piece | State | Where |
| --- | --- | --- |
| The land fuel system's feed | `fuelFeed` from the fuel pump's work; under 0.5 the engine gives three quarters, under 0.1 nothing | `Systems_Lubrication.xml`, `PhobosVehicleSystem_Fuel` |
| An aircraft engine's feed | Per engine, from its own pump, the same thresholds | `EngineFeeds`, `EngineFeedRule` |
| Fuel burned | Kept per use | `VehicleFuelLog`, `VehicleFuel.Burn` |
| Stale fuel | A depot's fuel older than 90 days loses 1 % a day | `DepotPressureRule.StaleShare` |
| The engine service | Restores engine hours, the air filter and the oil | `Systems_Service.xml` |
| The schematic | Draws a dashed fuel filter on its pages | [the schematic record](vehicle-schematic.md) |

## The model [A]

| Row | What |
| --- | --- |
| FF1 | **A quantity `fuelFilter`**, 0 clean to 1 clogged, in the land fuel system and the aircraft engine fuel system: one filter per vehicle |
| FF2 | **It clogs with the fuel burned**: a clean tank's fuel clogs it over about 1,000 litres' worth of the vehicle's own burn, times (1 + 4 × the tank's dirt). Authored, so a truck on clean fuel needs it changed about every second engine service |
| FF3 | **Clogged, it feeds less**: nothing below 0.6; from there the feed falls in a line to 0.5 at fully clogged, which is the existing "weak" stage. It joins the pump's work in `fuelFeed` (the lower of the two) |
| FF4 | **Warning at 0.7**, "fuel filter clogged", a notable amber stage with a message, so a lamp lights before power is lost |
| FF5 | **The tank's dirt**: a share kept per vehicle, averaged by amount as fuel is added. Fuel from a stale store brings dirt in step with how long it has been stale; every other source is clean |
| FF6 | **The engine service changes the filter** (restores it to 0), with nothing more in its supplies; a mechanic may also drain and rinse it as a field fix, back to half |
| FF7 | **The schematic's filter is real**: its dashed box becomes a filter drawn in its tone |

FF2's thousand litres and FF5's link to stale fuel are authored balance, not figures from a source.

## What it changes in play

- **Old depot fuel becomes a choice.** Burn it and change filters sooner, or leave it. Today it only disappears.
- **A long trip on a dirty load slows down near the end**, with a lamp first. A spare service on the road fixes it.
- **The engine service matters a little more**, but costs nothing new.

## Other factions' vehicles

- **In a raid on us, or parked at a base we attack**: nothing. Their vehicles run the lite tier, where nothing evolves.
- **Captured**: a clean filter and a clean tank, so the player inherits nothing they cannot see (agent default).

## The four questions

- **What it lets the player do**: keep engines fed by servicing on time and choosing clean fuel; read the filter on the schematic.
- **What it stops them doing**: nothing, until neglected; then it slows them, never stops them (with the lean of question 1).
- **How it changes the way they do things**: where fuel comes from starts to matter, not only how much there is.
- **What outside the vehicle notices**: **the depots**: a store left too long now costs a convoy power, so the world's caches carry a
  story of their age.

## Risk and settings

**Low** (the agent's grade, for the owner to confirm): a slow loss, warned of by a lamp, capped at three quarters of the engine's
power, and never a stop. **Its own setting, "Fuel filters clog", on by default.** Off, nothing clogs and a clogged filter costs
nothing. If the owner chooses that a filter can starve an engine (question 1), the risk becomes **moderate**, still on by default.

## Cost

One quantity and one flow on the systems' 250-tick update, and one saved share per vehicle for the tank's dirt. Nothing per tick.

## What a next game would need

The rule (clogging by fuel through and dirt, the feed curve) is game-free. A next game needs only its own fuel use and its own
service.

## Questions for the owner

On the Decisions page (`config/decisions.json`), each with the agent's lean.

1. **Can a fully clogged filter stop an engine?** Lean: no, three quarters of its power at worst.
2. **Should stale depot fuel be dirty fuel?** Lean: yes, stale fuel still goes, and what is left of a stale store is dirty.

## Sources

- How fuel filters work: general knowledge, **unverified** here.
- Our own records: [the schematic](vehicle-schematic.md), [depots](archive/vehicle-depots.md), [engines and the drivetrain](systems/engines-and-drivetrain.md).
