# Vehicle follow-ups: booster packs, ground on the world map, cab moods, RimHUD (9 Nov 2026)

A design record, built the same day in D'thorga Framework 0.18.0 (Draft). **Not yet tested in game.** These are four of the five
follow-ups listed after the world map and the tabs ([world map](vehicle-world-map-design.md), [tabs](vehicle-tabs-design.md));
the owner gave the go on 9 Nov 2026 ("proceed with your 'still to do' items"). The fifth, generated art, waits for the owner:
OpenAI's image API for vehicle bodies, the owner's local generation for icons (owner, 9 Nov 2026).

## Read this first

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

## Booster pack

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

## Ground on the world map

The core's `BiomeGround` reads a tile without naming biomes (the owner's rule: match effects, not names):

- **Typical terrain**: the biome's `terrainsByFertility` entry at fertility 0.5 (authored middle of the usual range). Read on
  9 Nov 2026 with `query-defs` for the owner's biomes: desert and extreme desert give sand (path cost 4); forests, shrubland,
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

## Cab moods

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

## RimHUD

RimHUD's wiki (read 9 Nov 2026, api version 1) lets a mod add a `RimHUD.CustomValueDef` naming a static class with
`GetParameters(Pawn)`, which RimHUD calls by reflection, insisting on the exact return type. Read in RimHUD's published source
(`External/CustomValueDef.cs`, `ExternalWidgetDef.cs`). Ours is `RimHudVehicleValue`: label "Vehicle", value "the Mule (driver)"
or "none", a tooltip with fuel, cab and the waiting line, a click that selects a vehicle on a map. The Def is in
`Mods/VehiclesRimHUD/Common`, loaded only with both Vehicle Framework and RimHUD (`Jaxe.RimHUD`). Players add it in RimHUD's
layout editor; no preset is shipped. RimHUD declares RimWorld 1.6 and runs on the owner's 1.7 game.

## To test in game (owner)

1. "Give a booster pack" (dev, full) beside a vehicle with "flatten the battery": right-click with a colonist; the option says
   "with a booster pack"; afterwards the pack shows 3 of 4 starts.
2. With a jump-start kit and a donor vehicle: the colonist fetches the kit, keeps going after picking it up, and finishes.
3. A flat pack (dev) and a machining table with power: the recharge bill takes it and makes a full one.
4. A caravan through desert or swamp with a laden truck: "log caravan state" shows the biome, terrain and path cost; the caravan
   explanation names a slowed vehicle.
5. A closed cab in frost with the heater on: "log cab comfort" shows each person's range and "ShelteredFromCold"; their mood
   shows "warm cab". Heater off long enough: "chilly cab".
6. With RimHUD: add "Vehicle (D'thorga Framework)" in its layout editor; a driver shows the vehicle and role.

## Who chose what

- **Owner (9 Nov 2026):** do the follow-ups; the booster pack as a real item with charges (named earlier as the follow-up);
  OpenAI for vehicle bodies and local generation for icons.
- **Agent choices, owner to revise:** every figure in the tables; packs need no kit; the route order; cab moods on by default; the
  RimHUD value's text; ice by path cost; the typical fertility of 0.5.
