# Vehicle Framework mechanics: research (8 Nov 2026)

A research record behind [vehicle systems](vehicle-systems-design.md) and [utility vehicles](utility-vehicles-design.md).
**Everything here was read from source and decompiled code; nothing was observed in play.** Sources: Vehicle Framework's
GitHub `develop` branch (SmashPhil, head b557926d of 31 Oct 2026), the decompile of its 1.6 Workshop build in
`.local/decompiled/VehicleFramework-1.6/` (marked "1.6 decompile" below; `develop` matched it on every point checked
unless noted), Combat Extended's `VehiclesCompat` decompile, the RimWorld 1.7 assembly, and Vanilla Vehicles Expanded's
XML on GitHub. Read 8 Nov 2026.

## Read this first

- **Vehicle Framework (VF) on Steam is a 1.6 build.** Workshop item 3014915404 declares 1.4, 1.5 and 1.6, last updated
  21 Oct 2025, about 787,000 subscribers. The 1.7 game loads it; the owner plays that way. The `develop` branch has a
  `1.7/` folder and declares 1.7 and Odyssey. SmashPhil is active (commits 31 Oct 2026; issues still answered) and wrote
  on 1 Nov 2026 that 1.7 "will be updated in the near future... there are more changes than merely updating the mod"
  ([issue 379](https://github.com/SmashPhil/Vehicle-Framework/issues/379)). No GitHub releases exist.
- **VF's Workshop comments cannot be read by a visitor**: the uploader's profile is private, so
  `scripts/workshop-research.py` returns none (8 Nov 2026). Read them in the Steam client.
- **Vanilla Vehicles Expanded (VVE)** cannot be removed from a save (Oskar Potocki, Workshop comment, 7 Nov 2026), and
  neither can VF: both hold the player's vehicles.
- **Parts are saved by position.** A design may only ever append parts; a saved vehicle never gains them unless code
  adds them ([D'thorga Framework's vehicle systems do](../../src/PhobosFramework.Vehicles/Service/Patches.cs), since 8 Nov 2026 for D'thorga Patches' Harrier too); a design with fewer parts
  than its saved vehicle fails on load (inferred from the code).
- **Combat Extended replaces VF's damage model**: it hits only parts on the cells its shot crosses.
- **Licence:** MIT, copyright 2019 to 2026 SmashPhil ([LICENSE](https://github.com/SmashPhil/Vehicle-Framework/blob/develop/LICENSE)).
  Referencing `Vehicles.dll` and patching it are allowed; redistributed code keeps the notice. VVE's GitHub repository
  carries no licence file: reference only, copy nothing.

## 1.7 status and authors

| Mod | Workshop | Declares | Last update | Notes |
| --- | --- | --- | --- | --- |
| Vehicle Framework | 3014915404 | 1.4, 1.5, 1.6 (Steam); 1.7 and Odyssey on `develop` | 21 Oct 2025 (Steam); 31 Oct 2026 (GitHub) | "Enable krafs nuget package for 1.7 unstable" and pathfinder fixes on `develop`; issue 381 filed 7 Nov 2026 |
| Vanilla Vehicles Expanded | 3014906877 | 1.5, 1.6, 1.7 | 15 Oct 2026 | Ships a 1.7 assembly; Oskar Potocki, 23 Oct 2026: updating again would "wipe out some saves... I will wait" |

VVE's newest comments (about 2,700 in all; read 8 Nov 2026): on 1.7 the pathing restrictions do not update until a save
and reload (6 Nov); colonists would not build vehicles (27 to 28 Oct); a "lack of crew" error with a full vehicle (3 Nov);
under Combat Extended, switching ammunition inside a tank fails. All are player reports, not confirmed causes.

**Combat Extended** ships its own VF compatibility: `Source/VehiclesCompat` (armour, gravity factor, settings) and
`ModPatches/VehicleFramework` under `IfModActive`, with patches for VVE, Tier 3 and Upgrades
([repository](https://github.com/CombatExtended-Continued/CombatExtended)). **Odyssey:** VF's `fishingProperties` is
marked `MayRequireAnyOf` Odyssey or VCEF, and a 1.7 compatibility file hooks Odyssey fishing only. **Gravships:** on a
crash report ([issue 231](https://github.com/SmashPhil/Vehicle-Framework/issues/231)) SmashPhil said vehicles move with a
launching gravship and blamed other mods; no gravship-specific code was found in the 1.6 build.

## Parts and health

**`VehicleComponentProperties`** (1.6 decompile; GitHub `Source/Vehicles/Components/Vehicles/Health/`):

| Field | Meaning | Rules at load |
| --- | --- | --- |
| `key`, `label` | Identity and in-world name | Key non-empty and unique per design |
| `compClass` | Default `VehicleComponent` | |
| `health` | Hit points | Above 0 |
| `depth` | `Undefined`, `External`, `Internal` | |
| `efficiencyWeight` | Weight in averaged stats (default 1) | Never 0, or damage becomes NaN |
| `hitWeight` | Chance share when a hit falls through | |
| `armor` | Stat modifiers (CE patches these to millimetres and megapascals) | |
| `empSeverity` | | |
| `priorityStatEfficiency` | Combined separately; the lower result wins | |
| `hitbox` | `cells`, or a `from`/`to` rectangle, or `side` (Front, Right, Back, Left, Body); `fallthrough` default true | Non-null; **no hitbox gives the single centre cell (0, 0)**; cells outside the vehicle log an error |
| `categories` | The `VehicleStatDef`s this part feeds | |
| `efficiency` | A curve of health fraction to efficiency; default (0,0) (0.25,0) (0.4,0.4) (0.7,0.7) (0.85,1) (1,1) | At least five points if set |
| `reactors` | `Reactor` subclasses with `Hit` and `Repaired`: `Reactor_Explosive`, `Reactor_FuelLeak`, `Reactor_TurretPopper` | |
| `tags` | Free strings; VVE uses `Wheel` | |

Cells are the vehicle's own, facing north, centred on the vehicle rect: VVE's Mule (size 2 by 4) uses x of 0 and 1 and
z from −1 to 2.

**How health becomes an outcome.** A part's efficiency is its curve evaluated at its health fraction.
`VehicleStatHandler.StatEfficiency(stat)` combines the parts that list the stat in `categories` by the stat's
`operationType` (`None`, `Average` weighted by `efficiencyWeight`, `Sum`, `MinValue`, `MaxValue`); priority parts are
combined separately and the lower of the two results wins. The stat's value is its base (from `vehicleStats` or the
Def's default) through stat parts and offsets, times that efficiency, clamped. Built-in stats: MoveSpeed,
AccelerationRate, CargoCapacity, RepairRate, BodyIntegrity (all Average), Mass (None), WorkToSabotage, and for aircraft
FlightSpeed and FlightControl. **A mod can add a stat:** `VehicleStatDef` is a public Def (`workerClass`,
`operationType`, `parts`, `category`, `formatString`, `showOnVehicleTypes`); put it in `vehicleStats` and in a part's
`categories`, read it with `vehicle.GetStatValue(def)`, and show it on the stat card through
`VehicleCompProperties.StatCategoryDefs()`. A stat nothing reads is only a figure on a card: D'thorga Patches'
[flight-control rule](../../src/PhobosPatches.Harrier/FlightControlRule.cs) is the precedent for giving one an effect.

**Repair.** `WorkGiver_RepairVehicle` and `JobDriver_RepairVehicle` use construction speed and the Construction skill;
every 60 ticks of work they heal the first damaged part in list order by the vehicle's `RepairRate`.

**Damage.** `VehicleComponent.TakeDamage` fires the `DamageTaken` and `HealthChanged` events; `HealComponent` fires
`Repaired` and `HealthChanged`. Under VF alone, a hit on a cell with no live part at the current depth falls through
to a random internal part by `hitWeight` (a bigger weight is hit more), then any live part. **Under Combat Extended**
(its `VehiclesCompat`, when `patchArmorDamage` is on; re-read 8 Nov 2026) a prefix replaces `ApplyDamage`: the shot walks a
line of cells along its angle and hits only parts on those cells, External parts first and inward only once it has got
through; its "hit any component" setting applies on the first cell only, to External parts whose hitbox avoids the edges.
**CE sorts the parts on a cell by a random draw times `hitWeight`, smallest first, so there a smaller weight is hit
more**, the reverse of VF; armour is the part's `armor` times its efficiency, sharp in mm and blunt in MPa, and a blunt hit
with penetration under half the armour does nothing. CE lowers `component.health` directly and still runs every reactor,
passing the damage before armour, sometimes twice per part and even when the armour stopped the round. Parts added by a mod
therefore want real cells, a low `hitWeight` under VF and a high one under CE (D'thorga Framework inverts it, or takes
`hitWeightCE`), and a reactor that counts health lost rather than damage dealt.

**VVE's parts, as shipped** (`1.7/Defs/VehicleDefs/Tier1/Mule/Mule_VehiclePawn.xml`, abridged):

```xml
<li>
  <key>Engine</key><label>Engine</label><health>50</health>
  <priorityStatEfficiency>true</priorityStatEfficiency><depth>Internal</depth>
  <reactors>
    <li Class="Vehicles.Reactor_Explosive"><indicator>VF_Explosive</indicator>
      <chance>0.25</chance><maxHealth>0.2</maxHealth><radius>3</radius><damageDef>Flame</damageDef></li>
  </reactors>
  <hitbox><cells><li>(0, 2)</li><li>(1, 2)</li></cells></hitbox>
  <categories><li>MoveSpeed</li></categories>
</li>
<li><key>FrontLeftWheel</key><health>30</health><depth>External</depth>
  <armor><ArmorRating_Sharp>0</ArmorRating_Sharp></armor><tags><li>Wheel</li></tags>
  <hitbox><fallthrough>false</fallthrough><cells><li>(0, 2)</li></cells></hitbox>
  <categories><li>MoveSpeed</li></categories></li>
<li><key>LeftSidePanel</key><health>40</health><depth>External</depth><hitbox><side>Left</side></hitbox></li>
<li><key>Alternator</key><label>Alternator</label><health>20</health><depth>Internal</depth></li>
```

The alternator has no hitbox and no categories: it is health only, reachable through fall-through. VVE's land vehicles
carry `Engine`, `Gearbox` and `Alternator`; its aircraft `FuelIntake`, `Transmission`, `Servos`, `RotorMast` or
`Thruster`.

## Ticking, state and events

- **Comps.** `VehicleComp : ThingComp`. `TickByRequest` is false by default (the comp ticks every tick); set it true and
  call `StartTicking` and `StopTicking` to tick only while needed (VF's fuel and launcher comps do). Independently,
  `VehiclePawn.TickRare` runs every 250 ticks and calls `CompTickRare` on **every** comp: a cheap, always-on hook.
  `TickInterval(delta)` reaches comps too. Hooks on `VehicleComp`: `CompCaravanGizmos`, `CompFloatMenuOptions`,
  `CanMove`, `CanDraft`, `CompStatCard`, `Animations`, `PostGeneration`, `EventRegistration`; `VehicleAIComp` adds
  `AITick`. VVE's `CompRefrigerator` (a toggle that burns fuel through `CompFueledTravel.ConsumeFuel`) is a complete
  working example.
- **Moving.** `vehiclePather.Moving` on a map; `VehicleCaravan.vehiclePather.MovingNow` on the world. No instantaneous
  speed is stored; a cell's cost is `nextCellCostTotal`. VVE adds `CompVehicleMovementController.currentSpeed` with
  acceleration on its land vehicles.
- **Engine on.** VF has no engine state apart from draft: `VehiclePawn.Drafted` reads `ignition.Drafted`, whose setter
  fires `IgnitionOn` and `IgnitionOff`. VF undrafts a vehicle on an empty tank and when MoveSpeed falls to 0.1 or less
  after damage. **Flight:** `CompVehicleLauncher.inFlight`.
- **Events** (`VehicleEventDefOf`): IgnitionOn, IgnitionOff, Braking, MoveStart, MoveStop, CargoAdded, CargoRemoved,
  PawnEntered, PawnExited, PawnChangedSeats, PawnCapacitiesDirty, PawnKilled, PawnRemoved, ScanShort (every 60 ticks),
  ScanRare (every 250), OutOfFuel, Refueled, Deployed, Undeployed, HealthChanged, DamageTaken, Repaired, Spawned,
  Despawned, Destroyed, AerialVehicleLaunch, AerialVehicleLanding, AerialVehicleCrashLanding, AerialVehicleLeftMap,
  AerialVehicleOrdered, UpgradeEnqueued, UpgradeCompleted, UpgradeCanceled, UpgradeRefundEnqueued,
  UpgradeRefundCompleted, ColorChanged; `develop` adds FactionChanged, PositionChanged, RotationChanged. Subscribe by
  overriding `VehicleComp.EventRegistration` and calling `Vehicle.AddEvent(def, handler)`.
- **Fuel** (`CompProperties_FueledTravel`): `fuelType`, `electricPowered`, `fuelConsumptionRate` (units per in-game
  day; per tick is the rate over 60,000), `fuelCapacity`, `fuelConsumptionWorldMultiplier`, `chargeRate` (default 1),
  `dischargeRate` (default 2), `ambientHeat` (default 0), `fuelConsumptionCondition` (flags Drafted 1, Moving 2,
  Flying 4, Always 8; default all three of the first), `leakDef`, `autoRefuelPercent`, `targetFuelLevelConfigurable`.
  A drafted vehicle that is not moving burns half rate. Fuel use starts and stops on the MoveStart, MoveStop,
  OutOfFuel, Refueled, IgnitionOn and IgnitionOff events. `ambientHeat` is pushed through `GenTemperature.PushHeat`
  every 250 ticks whenever the vehicle is spawned, engine or not; no VVE vehicle sets it. Leaks come from
  `Reactor_FuelLeak` on parts. VVE scales fuel efficiency by an ideology precept through a postfix.
- **Electric vehicles.** With `electricPowered`, a vehicle not charging drains `dischargeRate` over 60,000 per tick;
  charging moves `chargeRate` from the power net into the tank every 120 ticks while it stands on a cell whose
  `CompPower` transmits and whose net stores more than `chargeRate`; it disconnects while moving.
- **Cargo** is the `CargoCapacity` stat, which parts can scale. **No workbench:** nothing in VF implements `IBillGiver`.
- **Roles** (`properties.roles`): `key`, `label`, `handlingTypes` (None, Movement, Turret only; there is no worker
  role), `slots`, `slotsToOperate`, `turretIds`, `hitbox`, `exposed`, `chanceToHit`, `comfort`.
  `properties.vehicleJobLimitations` caps how many pawns work on one vehicle. Jobs at a vehicle build on the public
  abstract `JobDriver_WorkVehicle` (`TotalWork`, `Stat`, `Skill`, `WorkComplete`) and `VehicleWorkGiver`.
- **Upgrades:** `CompProperties_UpgradeTree` names an `UpgradeTreeDef` of nodes (`work`, ingredients,
  `researchPrerequisites`, `prerequisiteNodes`, `replaces`, `refundFraction`, `graphicOverlays`); upgrade types
  `StatUpgrade`, `CompUpgrade` (activates or deactivates `CompProperties`), `TurretUpgrade`, `VehicleUpgrade` (roles),
  `ActionUpgrade`, `SoundUpgrade`, `SettingsUpgrade`. `VehicleUpgrade.Unlock` dereferences a part by key with no null
  check.
- **Deploying:** `CompVehicleTurrets` owns the Deployed state (`JobDriver_Deploy`, `VehicleStatPart_Deployed`) and the
  Deployed and Undeployed events.
- **Design fields:** `vehicleType` (Land, Sea, Air, Universal), `vehicleCategory` (Transport, Trader, Combat, Work),
  `navigationCategory`, `buildDef`, `components`, `vehicleStats`, `drawProperties`, `fishingProperties`; terrain through
  `properties.defaultImpassable`, `customTerrainCosts`, `customThingCosts`, `offRoadMultiplier`, and world-map biome,
  road, river and hilliness costs. Movement permissions are derived, not a field.

## VF's per-vehicle settings

Classes marked `[VehicleSettingsClass]` (`VehicleDef`, `VehicleProperties`, `VehicleCompProperties`) expose fields
carrying `[PostToSettings(...)]` (with `SliderValues`, `NumericBoxValues`, `DisableSettingConditional` as helpers). At
startup `SettingsCustomizableFields` walks every design and every comp in its `comps` list and snapshots the fields.
Player overrides live in VF's own settings file and are **never written back into the Def**: each consumer reads
`SettingsCache.TryGetValue(def, typeof(Props), "field", fallback)`, as the fuel comp does for its rates. So a mod's own
`VehicleCompProperties` subclass can put tuning fields in VF's window (mark the class `[VehicleSettingsClass]`
explicitly; the attribute is inheritable but saying so costs nothing), but a toggle that decides whether the comp exists
at all must be the mod's own setting and needs a restart.

## Retrofitting parts and comps onto other mods' designs

- **XML:** a `PatchOperationAdd` on `Defs/Vehicles.VehicleDef[not(@Abstract='True')]/components` appends parts to every
  concrete design; keys must be unique per design; `ResolveReferences` then fills defaults and builds hitboxes.
  Patches run before inheritance is resolved, so target concrete designs.
- **C#:** `VehicleDef.PostLoad` handles graphics only; `CacheCompProperties` caches only the fuel, launcher, turret and
  upgrade-tree props; per-vehicle part lists are built from `VehicleDef.components` when a vehicle is created
  (`VehicleStatHandler.InitializeComponents`), with no global cache. Injecting before reference resolution (a postfix on
  the game's `DefGenerator.GenerateImpliedDefs_PreResolve`, or VF's own implied-Defs event) lets VF resolve hitboxes and
  snapshot settings for the injected comps; injecting later from a static constructor has no ordering guarantee against
  VF's own and would need a manual `ResolveReferences`.
- **Saves:** `VehicleStatHandler.ExposeData` saves parts as a list of healths with no keys and rebinds them by index to
  the design's list on load; the [parts-on-load postfix](../../src/PhobosFramework.Vehicles/Service/Patches.cs) (D'thorga Patches' until 8 Nov 2026, now D'thorga Framework's)
  creates missing parts at full health in the same load step (`Activator.CreateInstance(compClass, vehicle)`,
  `Initialize`, `PostCreate`, the private `componentsByKeys`, `RecacheStatCategories`, `MarkAllDirty`). A design with
  **fewer** parts than the save would index past the list (inferred). Other saved state per vehicle: the pather,
  ignition, the stat handler, the shared job, animator, angle, reverse, crash-landed, pattern, retexture, movement
  status, fishing, cargo to load, handlers and boarding assignments; the fuel comp saves fuel, auto-refuel and the
  target level; the launcher saves `inFlight`. A comp newly added to a design appears on old saves with default state.

## Game hooks for temperature and breakdown (RimWorld 1.7)

- `Thing.AmbientTemperature` falls back from the spawned cell to the holder to the world tile to 21 °C, so it works for
  a vehicle in a caravan. `GenTemperature.GetTemperatureForCell`, `MapTemperature.OutdoorTemp` and
  `GenTemperature.GetTemperatureAtTile` are the underlying calls.
- `GenTemperature.PushHeat(cell, map, energy)` puts heat into the cell's room or splits it across neighbours.
  `CompHeatPusher` pushes `heatPerSecond` every 60 ticks (or scaled on the rare tick) while spawned, enabled and between
  its minimum and maximum ambient temperatures; `CompHeatPusherPowered` adds power, fuel and breakdown gates.
- `CompBreakdownable` keeps a saved `brokenDown` flag, registers with the map's `BreakdownManager`, and every 1,041
  ticks while powered rolls `Rand.MTBEventOccurs(13680000f, 1f, 1041f)`, a mean time between failures of 228 days;
  `DoBreakdown` sends the "Breakdown" signal; `JobDriver_FixBrokenDownBuilding` takes 1,000 ticks and one component.
  The manager assumes buildings, so a vehicle model mirrors the roll instead of registering.

## Powertrain data already present, and inference by effect

VF provides `vehicleStats` (MoveSpeed, Mass, CargoCapacity, FlightSpeed for aircraft) and the fuel properties above.
VVE defines no horsepower or engine type anywhere; every VVE vehicle burns Chemfuel and none is electric. Read from
`1.7/Defs/VehicleDefs/Tier*/<Name>/<Name>_VehiclePawn.xml`:

| defName | Type | Mass | MoveSpeed or FlightSpeed | Fuel per day / capacity | Alternator part |
| --- | --- | --- | --- | --- | --- |
| VVE_BangBus | Land | 180 | 10.4 | 14.5 / 80 | yes |
| VVE_Bunsen | Land, tracks | 260 | 7.0 | 29.5 / 160 | yes |
| VVE_Dinghy | Sea | 80 | 7 | 8 / 40 | no |
| VVE_Dirtbike | Land | 30 | 12 | 12.5 / 30 | no |
| VVE_Frog | Air | 80 | FlightSpeed 50 | 800 / 160 | no |
| VVE_Highwayman | Land | 220 | 9.2 | 24.5 / 80 | yes |
| VVE_Mule | Land | 240 | 10 | 18 / 80 | yes |
| VVE_Prowler | Land | 100 | 11 | 18.5 / 60 | yes |
| VVE_Roadkill | Land, tracks | 300 | 6.6 | 28.5 / 60 | yes |
| VVE_Scytheman | Land | 135 | 8.6 | 21 / 60 | yes |
| VVE_Toad | Air | 140 | FlightSpeed 50 | 1600 / 260 | no |
| VVE_Traveller | Land | 180 | 12.8 | 22.5 / 80 | yes |
| VVE_Bulldog | Land, tracks | 450 | 7.2 | 24 / 75 | yes |
| VVE_Charley | Land | 50 | 12.8 | 14.5 / 50 | no |
| VVE_Mosquito | Air, rotor | 420 | FlightSpeed 10.5 | 24.5 / 120 | no |
| VVE_Roadrunner | Land | 260 | 14.8 | 20.5 / 120 | yes |
| VVE_Smuggler | Air | 400 | FlightSpeed 22 (MoveSpeed 1.5) | 22.5 / 120 | no |
| VVE_Snatcher | Land | 200 | 12 | 16.5 / 120 | yes |
| VVE_Tango | Land, tracks | 450 | 6.4 | 28 / 75 | yes |
| VVE_Trawler | Sea | 80 | 4.6 | 14 / 150 | no |
| VVE_Wagon | Land | 210 | 12.4 | 18.5 / 80 | yes |
| VVE_Warbird | Air | 280 | FlightSpeed 30 (MoveSpeed 2) | 36.5 / 70 | no |
| VVE_Wisent | Land | 380 | 10.2 | 36 / 160 | yes |

**Inference rules** (ours; they read effects, never names, per the owner's rule of 5 Nov 2026): a fuelled design that is
not `electricPowered` has a combustion engine with coolant and oil; `electricPowered` means a battery pack and motor;
a `FlightSpeed` stat or a launcher comp marks an aircraft; `vehicleType` Sea marks a boat; parts tagged `Wheel` are
running gear; thermal mass and alternator size scale with `Mass`; fuel burn stands in for engine power. Where a design
already has engine, alternator or gearbox parts, a loadout binds to them (found by category and depth, with the key as
a hint) instead of adding duplicates.

## Prior art

**Vehicle systems:** no mod found (8 Nov 2026) that models engine temperature, batteries, oil, wear or breakdowns on VF
vehicles. VVE models acceleration and braking and an ideology fuel precept; its alternator is health only. VVE Upgrades
Extended (Samael Gray, 3392370730) extends VVE Upgrades' minor upgrades to more vehicles; Engine Industries for VVE
(3409497931) swaps crafting parts. Fluffy Breakdowns (maintenance for buildings) stops at 1.4.

**Utility vehicles on VF** (from the community collections 3497770083 and 3419713592; dates are last updates, versions
are Steam tags): Tractor: Industrial Age 3276694110 (plough, plant, harvest; Aug 2025; 1.6); DAYUN V9 heavy tractor
3505163059 (Oct 2025; 1.6); HE-Ford F350 Ambulance 3274067876 (Jul 2025; 1.6); HE-Ford E-Series vans 3690023821
(ambulance, fire, police; Mar 2026; 1.6); HE-Pierce Arrow Pumper 3276087323 (fire truck with hose; Jul 2025; 1.6);
Forklift Logistics 3827402029 (26 Oct 2026; 1.6); Caravan Hauler 3798638836 (hauling truck and garage; 19 Oct 2026;
1.6 with an experimental 1.7 build); RV with built-in PD 3342334887 (power from vehicle fuel; Jul 2025; 1.6); Vehicle
Map Framework 3426502333 (interior maps; 17 Oct 2026; 1.6); Mobile Fortress 3722000482 (Aug 2026; 1.6); Utility
Vehicle 3627439568 (22 Oct 2026; 1.7); Trains of the Rim 3530446424 (tanker cars; Aug 2025; 1.6). **No mobile drill,
mining rig, bulldozer or mobile workbench was found.**

## Art requirements

- The body graphic is `Graphic_Vehicle`. Textures `<name>_north`, `_east`, `_south`, `_west`, with optional diagonals;
  a missing south is the north rotated, a missing west is the east mirrored, diagonals reuse the cardinals.
- Colour masks carry the suffix `m` (`_northm`) for the three-colour pattern shader `CutoutComplexPattern`;
  `GraphicDataRGB` adds `colorThree`, `pattern`, `tiles`, `displacement`. Turrets use `Graphic_Turret` with `_m`.
- Animated classes: `Graphic_Animate`, `Graphic_Rotator`, `Graphic_Propeller`, `Graphic_ReversePropeller`.
- `drawProperties.graphicOverlays` (identifier, graphic data, rotation, `component`): an overlay's
  `ComponentRequirement` was read here as showing or hiding it by a part's health fraction. **It does not** (found 9 Nov 2026 when the
  bulk tanker looked damaged from new): only turrets ever call `RecacheComponent` and `RegisterEvents`, so on a graphic overlay
  `MeetsRequirements` stays at its default of true and the overlay always shows. Draw damage states from a comp, as `CompPhobosTank` does.
- VVE's Mule: size (2, 4), drawSize (3, 5); north 384 by 640, east 640 by 384, south, each with a mask; six PNGs and no
  west. **128 px per drawSize unit.**

## Sources

- Vehicle Framework: [repository](https://github.com/SmashPhil/Vehicle-Framework),
  [commits on develop](https://github.com/SmashPhil/Vehicle-Framework/commits/develop),
  [issue 379](https://github.com/SmashPhil/Vehicle-Framework/issues/379), [issue 231](https://github.com/SmashPhil/Vehicle-Framework/issues/231),
  [LICENSE](https://github.com/SmashPhil/Vehicle-Framework/blob/develop/LICENSE), the wiki (a home page and
  [FAQ](https://github.com/SmashPhil/Vehicle-Framework/wiki/Frequently-Asked-Questions) only; the modder guide it links is
  a Google Doc behind a sign-in); the 1.6 Workshop build decompiled to `.local/` (never committed). Steam item details
  through Steam's public API, 8 Nov 2026.
- Vanilla Vehicles Expanded: [repository](https://github.com/Vanilla-Expanded/VanillaVehiclesExpanded) (XML, `CompRefrigerator`,
  `CompVehicleMovementController`); [Workshop page](https://steamcommunity.com/sharedfiles/filedetails/?id=3014906877)
  comments, 8 Nov 2026.
- Combat Extended: [VehiclesCompat source](https://github.com/CombatExtended-Continued/CombatExtended/tree/master/Source/VehiclesCompat)
  and its decompile in `.local/`.
- RimWorld 1.7 assembly (1.7.9805) decompiled to `.local/`: temperature, heat pusher, breakdown classes.
- Community collections of VF mods: [3497770083](https://steamcommunity.com/sharedfiles/filedetails/?id=3497770083),
  [3419713592](https://steamcommunity.com/sharedfiles/filedetails/?id=3419713592).
