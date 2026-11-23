# The world map and the cab: caravans, climate, night heater, jump-starts (8 Nov 2026)

A design record, built the same day in D'thorga Framework 0.16.0 and D'thorga Vehicles 0.7.0 (Drafts). **Not yet tested in
game.** The owner asked how the vehicle systems behave in caravans on the world map, and about the cab heater, air conditioning
and passenger comfort (8 Nov 2026), then decided the four questions below. Labels: **[P]** primary, **[S]** secondary, **[I]** our
inference, **[A]** authored.

How camping and life aboard work now: [systems/camping-and-life-aboard.md](systems/camping-and-life-aboard.md); this record keeps its history.

**Merged here on 17 Nov 2026**, each kept as written: [Vehicle follow-ups: booster packs, ground on the world map, cab moods, RimHUD](#vehicle-follow-ups-booster-packs-ground-on-the-world-map-cab-moods-rimhud-8-nov-2026).

## Read this first

- **The systems already ran in caravans** (read from Vehicle Framework 1.6 and the game, 8 Nov 2026): a vehicle in a caravan is a
  world pawn that keeps ticking, so wear, charge, heat and fuel all carried on. What was silently skipped: the drivetrain's speed
  cap, the "will not move" refusals, services, jump-starts and shore charging. What was wrong: the load-dependent fuel correction
  (it ignored Vehicle Framework's world-map multiplier), and a resting caravan read as "engine off" so the heater never ran at
  night (which turned out to be right, see below).
- **An aircraft in flight is a world pawn that used to go to sleep** (read from the game and Vehicle Framework, 9 Nov 2026): the game's six-hourly sweep stops ticking a world pawn unless it is in a caravan,
  so a holed tank stopped leaking and the systems stopped running partway through a flight. Since Framework 0.57.1 the player's aircraft stay awake while they fly (`FlyingAircraftStayAwakePatch`). Vehicle Framework's fuel burn in flight
  is separate and needs the vehicle on a map to double up, so nothing is charged twice. **Lets the player:** nothing new. **Stops them:** outrunning a leak on a long flight. **Changes play:** a holed tank is a deadline in the air too.
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

## Vehicle follow-ups: booster packs, ground on the world map, cab moods, RimHUD (8 Nov 2026)

*Merged into this record on 17 Nov 2026 from `vehicle-crew-and-ground-design.md`; kept as written.*


A design record, built the same day in D'thorga Framework 0.18.0 (Draft). **Not yet tested in game.** These are four of the five
follow-ups listed after the world map and the tabs ([world map](vehicle-world-map-design.md), [tabs](vehicle-tabs-design.md));
the owner gave the go on 8 Nov 2026 ("proceed with your 'still to do' items"). The fifth, generated art, waits for the owner:
OpenAI's image API for vehicle bodies, the owner's local generation for icons (owner, 8 Nov 2026).

### Read this first

- **Booster pack**: a crafted item (research: batteries) that jump-starts with no second vehicle and **no kit** (it has its own
  clamps). 4 starts; a start back every 2 hours in the cargo of a running vehicle; a machining-table bill recharges it fully.
- **Ground on the world map**: the tile's biome, read from its Defs, sets a path cost that the drivetrain's existing ground
  curves turn into rolling resistance and grip. Deserts are sand, swamps mud, jungle undergrowth; winter adds snow; a road is firm.
- **Cab moods**: a person aboard a closed cab compares it with their own comfort range: glad of shelter, or unhappy in a cab too
  cold or too hot. A setting (on) turns them off.
- **RimHUD**: a value for RimHUD's pawn display through its published custom widget interface. No reference to RimHUD's code.
- **Two bugs from 0.16.0 fixed** while reading the jump-start job: the job ended when the helper picked the kit up (a job-wide
  despawn check), and the carry toil threw on the job's default count of -1. Neither was seen in game; both are read from the
  game's own toil code (`Toils_Haul.StartCarryThing`) and the job driver.

### Booster pack

| Choice | Value | Source or label |
| --- | --- | --- |
| Starts when full | 4 | Authored. Real lead-acid packs are sold on 10 to 20 starts of a small car and far fewer of a heavy diesel (approximate, general). |
| Recharge aboard | 2 hours a start while the engine runs | Authored. A 12 V socket charges a pack in roughly 4 to 8 hours (approximate, general). The draw on the vehicle's electrical system is left out. |
| Recharge at home | A bill at a machining table or fabrication bench, 600 work, powered | Authored. The bill makes a fresh pack from a part-used one, so its hit points come back full: a small repair, not worth code. |
| Mass, cost, research | 6 kg; 15 steel, 2 components; batteries | Authored stand-ins (a lead-acid pack weighs about 7 to 9 kg). |
| Kit | Not needed | A pack carries its own clamps. This also changes the assumed pack (the setting), which needed a kit in 0.16.0 and 0.17.0. |

**Order of routes** (`JumpStarts.Plan`): a donor vehicle with the leads (the kit when the setting asks for one), then a booster pack
with a start left, then the assumed pack (the setting). A refusal names what is missing: the kit when a donor is there, otherwise
a donor or a pack. The pack's uses live in the Framework core's `CompPhobosCharges` (saved, key `PhobosFramework_Charge`), its
arithmetic in `ChargeRules` (tested), and the recipe's "part-used only" filter in the core's `PhobosFramework_ChargesFull`
special filter (hidden from storage filters).

### Ground on the world map

The core's `BiomeGround` reads a tile without naming biomes (the owner's rule: match effects, not names):

- **Typical terrain**: the biome's `terrainsByFertility` entry at fertility 0.5 (authored middle of the usual range). Read on
  8 Nov 2026 with `query-defs` for the owner's biomes: desert and extreme desert give sand (path cost 4); forests, shrubland,
  tundra, bogs, swamps and lava fields give soil (2), grassland and glowforest their own soils (2); ice sheet, sea ice and glacial
  plain give ice (14); the scarlands give ancient megastructure (1, nearly paved). A biome with no list (the Dead Man's Switch's
  impact crater) takes soil's 2 (authored default).
- **Biome difficulty**: `movementDifficulty` (open country 1, jungle and glowforest 2, ice 1.5, swamps and cold bog 4). Each point
  above 1 adds 3 to the path cost (authored: a swamp's soil becomes 11, about soft mud).
- **Winter**: the game's own `WorldPathGrid.GetCurrentWinterMovementDifficultyOffset` (0 to 2); each point adds 4 (authored: deep
  winter adds 8, the game's own path cost for medium snow on a map).
- **Road**: path cost 0 plus the snow.

`WorldGroundRules.PathCost` (tested) adds them; the drivetrain's `RollingResistanceForPathCost` and `GripForPathCost`, the same
curves a map cell uses, turn the sum into rolling resistance and grip. Hills keep their grades (flat 0%, small 2%, large 5%,
mountainous 8%). The caravan's grip was a fixed 0.7 before; it now follows the ground (0.8 on a road, 0.6 on soil). Read once per
tile and in-game hour, never saved.

**Double counting, on purpose:** Vehicle Framework already slows caravans by biome. That is the time the terrain costs everyone;
this is whether a vehicle's engine and grip can manage the ground at all, which bites only a laden or underpowered vehicle.
**Ice** is judged by its path cost (14), so it reads as heavy going (rolling 0.25, grip 0.35) rather than slippery and easy
rolling: right for grip, wrong for rolling; a gameplay simplification.

### Cab moods

`ThoughtDef PhobosVehicleSystems_CabComfort`, a situational thought valid while despawned (people aboard are not spawned; the game
skips other situational thoughts for them). The worker reads the Cabin system's cached cab temperature, the comp's cached outside
temperature and the person's `ComfyTemperatureMin` and `ComfyTemperatureMax`; the stage comes from the core's `ShelterComfort`
(tested):

| Stage | When | Mood (authored) |
| --- | --- | --- |
| Warm cab | Comfortable inside, colder outside than the range | +3 |
| Cool cab | Comfortable inside, hotter outside than the range | +3 |
| Chilly cab | Below the range | -2 |
| Freezing cab | 10 degrees or more below | -5 |
| Hot cab | Above the range | -2 |
| Stifling cab | 10 degrees or more above | -5 |

For scale, the game's own: a fine meal +5, sleeping on the ground -4. The game's cold and heat injuries still act through the cab
temperature as before. Open cabs give nothing.

### RimHUD

RimHUD's wiki (read 8 Nov 2026, api version 1) lets a mod add a `RimHUD.CustomValueDef` naming a static class with
`GetParameters(Pawn)`, which RimHUD calls by reflection, insisting on the exact return type. Read in RimHUD's published source
(`External/CustomValueDef.cs`, `ExternalWidgetDef.cs`). Ours is `RimHudVehicleValue`: label "Vehicle", value "the Mule (driver)"
or "none", a tooltip with fuel, cab and the waiting line, a click that selects a vehicle on a map. The Def is in
`Mods/VehiclesRimHUD/Common`, loaded only with both Vehicle Framework and RimHUD (`Jaxe.RimHUD`). Players add it in RimHUD's
layout editor; no preset is shipped. RimHUD declares RimWorld 1.6 and runs on the owner's 1.7 game.

### To test in game (owner)

1. "Give a booster pack" (dev, full) beside a vehicle with "flatten the battery": right-click with a colonist; the option says
   "with a booster pack"; afterwards the pack shows 3 of 4 starts.
2. With a jump-start kit and a donor vehicle: the colonist fetches the kit, keeps going after picking it up, and finishes.
3. A flat pack (dev) and a machining table with power: the recharge bill takes it and makes a full one.
4. A caravan through desert or swamp with a laden truck: "log caravan state" shows the biome, terrain and path cost; the caravan
   explanation names a slowed vehicle.
5. A closed cab in frost with the heater on: "log cab comfort" shows each person's range and "ShelteredFromCold"; their mood
   shows "warm cab". Heater off long enough: "chilly cab".
6. With RimHUD: add "Vehicle (D'thorga Framework)" in its layout editor; a driver shows the vehicle and role.

### Who chose what

- **Owner (8 Nov 2026):** do the follow-ups; the booster pack as a real item with charges (named earlier as the follow-up);
  OpenAI for vehicle bodies and local generation for icons.
- **Agent choices, owner to revise:** every figure in the tables; packs need no kit; the route order; cab moods on by default; the
  RimHUD value's text; ice by path cost; the typical fertility of 0.5.
