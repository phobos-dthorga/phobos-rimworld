# The world map and the cab: caravans, climate, night heater, jump-starts (8 Nov 2026)

A design record, built the same day in D'thorga Framework 0.16.0 and D'thorga Vehicles 0.7.0 (Drafts). **Not yet tested in
game.** The owner asked how the vehicle systems behave in caravans on the world map, and about the cab heater, air conditioning
and passenger comfort (8 Nov 2026), then decided the four questions below. Labels: **[P]** primary, **[S]** secondary, **[I]** our
inference, **[A]** authored.

## Read this first

- **The systems already ran in caravans** (read from Vehicle Framework 1.6 and the game, 8 Nov 2026): a vehicle in a caravan is a
  world pawn that keeps ticking, so wear, charge, heat and fuel all carried on. What was silently skipped: the drivetrain's speed
  cap, the "will not move" refusals, services, jump-starts and shore charging. What was wrong: the load-dependent fuel correction
  (it ignored Vehicle Framework's world-map multiplier), and a resting caravan read as "engine off" so the heater never ran at
  night (which turned out to be right, see below).
- **Nothing sheltered a passenger.** Neither the game nor Vehicle Framework gives a vehicle an interior temperature; a pawn aboard
  felt the tile's weather. The cab heater was a 12 A electrical load and nothing more.
- **Owner's decisions (8 Nov 2026):** a closed cab with its own temperature; engine off at rest with an optional fuel-burning
  night heater as a setting; the speed cap and the refusals apply to caravans, with a caravan jump-start; a craftable jump-start
  kit, with two settings: "Jump-start leads must be crafted" (on) and "Every vehicle carries a booster pack" (off).

## The cab

The game asks a held thing's holder chain for a fixed temperature before it looks at the cell or the tile
(`ThingOwnerUtility.TryGetFixedTemperature`; that is how a transport pod stays at 14 °C **[read from the game]**), and a
passenger's holder is the vehicle's role handler, then the vehicle, then the caravan **[read from VF]**. So a cab temperature given
there reaches the game's own hypothermia, heatstroke and comfort rules on maps and in caravans alike. The Framework core gained a
registry for it (`HeldTemperature`: any holder type can register a reader); the vehicle part registers Vehicle Framework's role
handler and applies the registry with one postfix (the core has no Harmony by design).

`PhobosVehicleSystem_Cabin` holds `cabTemperature`, all flows `Toward` through the integrator:

| Flow | Toward | Rate (share of the gap an hour) **[A]** | When |
| --- | --- | --- | --- |
| outside | the air around the vehicle | 2 (a parked cab meets the air in about 1.5 h) | always |
| heater | 18 °C | 6 | the cab heater load is on (switched on, below 5 °C outside, engine running) |
| cooler | 24 °C | 4 | the air conditioner load is on (above 26 °C, engine running) |
| night heater | 18 °C | 3 | the night heater load is on (switched on, the caravan rests, the setting is on) |

Settling points: −20 outside with the heater, about 8 °C (cold, not freezing); 40 outside with the air conditioner, about 29;
−10 at camp with the night heater, about 7. The rule tests hold them. Stages: freezing (<0), cold (0–10), comfortable (10–28),
hot (28–38), stifling (38+); no effects of our own, the game's rules act through the temperature. **Closed or open:** a loadout's
`cabin` block (closed by default) and a vehicle's `PhobosVehicleCabinExtension`; D'thorga Vehicles marks VVE's Dirtbike, Charley
and Dinghy open **[A, from their descriptions]**. The air conditioner is a new load (30 A at 12 V, 20 A at 24 V **[A]**).

## Night rest and the night heater

Vehicle Framework rests vehicle caravans from 22:00 to 06:00 **[read from VF]**, and the engine is off at camp (as you would). The
night heater is a switchable load (2 A for its fan **[A]**, off by default per vehicle, behind the global setting) that runs only
while the caravan rests; a hidden derived quantity marks it running and burns 0.4 chemfuel an hour through the existing fuel
effect (real parking heaters burn about 0.2–0.5 L/h **[A, general]**). **Set the switch before leaving**: load switches are gizmos,
unreachable on the world map.

## Caravan speed and refusals

- **Fuel:** the load-dependent setting's correction now uses VF's world rate (`ConsumptionRateWorldPerTick`, with the author's
  world multiplier: the Mule halves it) while the caravan moves, and nothing while it stands; on a map, the daily rate while moving
  and half of it idling, as before. `FuelRules` (tested).
- **Speed cap:** a postfix on VF's `VehicleCaravanTicksPerMoveUtility.GetTicksPerMove` divides by the worst `SpeedCap` among the
  caravan's vehicles (`CaravanRules.CombineCaps`, never below the 15% crawl), with an explanation line naming the vehicle. The cap
  comes from the drivetrain's caravan sample (road and hilliness; biome is a follow-up).
- **Refusals:** the comp caches `MoveRefusal` on every update; a postfix on VF's `VehiclePawn.CanMoveFinal` getter (off-map only)
  returns false with one, and VF's own `VehicleCantMove` halts the caravan. A `WorldObjectComp` added to VF's `VehicleCaravan` Def by
  XML shows each refusal in the caravan's inspect text and offers the caravan jump-start.

## Jump-starts: the kit, the settings, caravans

- **The kit** (`PhobosVehicleSystems_JumpStartKit`): leads with clamps, 20 steel and 1 component **[A]**, 3 kg **[A]**, Crafting 3 at a
  machining table or a fabrication bench, no research gate **[A]**, never used up. Placeholder art drawn in code.
- **"Jump-start leads must be crafted"** (default on): on a map the helper fetches a kit from stock (the core's `ItemFinder`) and
  puts it down beside the vehicle after; without one the order is refused with the reason. In a caravan a kit must be aboard. Off:
  the leads are free, as in 0.10.0. **This changes 0.10.0's behaviour under the default**, and the README says to craft a kit first.
- **"Every vehicle carries a booster pack"** (default off): on, a jump-start needs no donor, on the map or in a caravan; the charge
  comes from the assumed pack and the message says so. The booster pack as a real crafted item with charges is a follow-up.
- **Caravan jump-start:** a button on the caravan for each vehicle that needs one, from another vehicle in the caravan with charge
  (or the booster pack), disabled with the reason when no kit is aboard, no donor has charge, or nobody can do it. Instant, since
  caravans have no jobs **[A]**.

## Checked offline

Rule tests: the fuel correction (zero at a nominal cruise; the world rate with the Mule's multiplier), caravan caps (slowest
vehicle, crawl floor, ticks per move), the cab's settling points and cooling, the night heater in frost. Schema checks: the cabin
block, `When_LoadActive` naming a real load.

## To test in game (owner)

1. A vehicle caravan with the fuel setting on: fuel falls at Vehicle Framework's world rate (and the Mule's half rate).
2. At 22:00 the engine is off; with the night heater setting and the vehicle's switch on, the cab stays warm and fuel ticks down.
3. A passenger in a closed cab in frost gets no hypothermia; on a Dirtbike they do.
4. "Set caravan cap" (dev): the caravan's tiles a day halve and the explanation names the vehicle.
5. "Flatten the battery" (dev) in a caravan: it stops, the inspect text says why, the caravan button jump-starts it with a kit
   aboard ("give a jump-start kit") and refuses with the reason without one; switch 1 off needs no kit; switch 2 on needs no donor.
6. On the map: a jump-start fetches the kit, or refuses without one.

## Later: cab heaters and climate control (8 Nov 2026)

- **Fixed in Framework 0.21.2 and Vehicles 0.9.1:** aircraft and boats had no cab heater, and steam cabs no heat at all, so their cabs
  could only cool. Found from the owner's Harrier at -9 °C; a parked vehicle with its engine off still follows the weather, by design.
- **Automatic climate control (owner, 8 Nov 2026; Framework 0.22.0, Vehicles 0.10.0):** vehicles fitted with it hold the cab near
  21 °C, heating below 20 and cooling above 22 while the engine runs, at the heater's and air conditioner's rates; the manual ones stand
  down meanwhile. Fitted by era (agent reading: modern real vehicles, Vanilla Vehicles Expanded's Tier 3 but the Springbok and Louie),
  by spacer tech, or on every closed cab with the setting (off). Not yet tested in game.

## Who chose what

- **Owner (8 Nov 2026):** the four decisions above; the kit; the two settings.
- **Agent choices, owner to revise:** every rate and target in the cab's table; the air conditioner's amps; 0.4 fuel an hour; the
  open VVE vehicles; the kit's cost and no research gate; the instant caravan jump-start; the booster pack as a later item.
