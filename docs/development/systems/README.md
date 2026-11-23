# How each system works now

Living pages, one a system, written fresh from the code and kept current by every change that alters the system. Why a system
is the way it is lives in its [decision log](../decisions/README.md) and in the [archived records](../archive/README.md).

- [Engines and drivetrains](engines-and-drivetrain.md) (23 Nov 2026): engine kinds, gears and the speed cap, loads, fuel,
  starting, failure, thin air and descents. Living; the pilot.
- [Flight](flight.md) (23 Nov 2026): our own flight over a map, leaving and arriving, cruising height, lift-off limits, the
  sling, refuelling on a journey, avionics, aim, dodging, countermeasures, jamming and the aircraft in the fold. Living.
- [Vehicle damage](vehicle-damage.md) (23 Nov 2026): conditions, the Health tab, repairs, knock-outs, spall, glass, armour,
  fire, blasts and EMP. Living.
- [Upgrades and fittings](upgrades-and-fittings.md) (23 Nov 2026): fittings, tunes, part grades, families, upgrade trees and
  their prices, the dyno bay, parts and parts bills. Living.
- [Camping and life aboard](camping-and-life-aboard.md) (23 Nov 2026): accessories and the power budget, the camp, deck tents,
  bunks, driving through the night, caravan cooking, walk-in cabins, life aboard, hotboxing and the cab. Living.
- [Hitches and trailers](hitches-and-trailers.md) (23 Nov 2026): the hitch, couplings, chains, trailers and their set-up, towing
  on the map and the world map, the recovery winch and tow, and pushing by hand. Living.
- [Stations, yard and depots](stations-yard-and-depots.md) (23 Nov 2026): the motor pool's stations, depot bays, building
  upgrades, the service yard, bay trips, the park, the fleet board, the garage, depots, chutes and unloading bays. Living.
- [Gases and air](gases-and-air.md) (23 Nov 2026): the Framework's gas layer, carbon monoxide, fire smoke, masks, supplied oxygen
  and protective-gear lines. Living.

## The template

1. Title, then `**Status (date):**` in one line: what is built, and which test-list rows the owner has passed.
2. `## Read this first (gotchas)`, then `## What it does` (what a player meets, as mechanism) and `## How it works` (data
   flow in Mermaid, the per-tick and per-rare-tick path, where the numbers come from).
3. `## Code` (file, what it holds), `## Defs and XML` (each type linked to the reference site), `## Settings` (labels and
   defaults; link the settings reference), `## Saved data and removal`, `## Tie-ins`, `## Performance` (measured, with source).
4. `## Limits and known gaps`, `## Tests` (rule tests, Python tests, test-list rows), `## Why it is this way` (links only),
   ending with a **Records kept in place** line: the records still in `docs/development/` that cover part of this system.
5. No dated history and no "the owner said" outside the Status line and the decision log; real-world figures keep their
   sources, and authored values and simplifications say so.
6. Prose lines stay within 130 characters; table lines may run to 160.
