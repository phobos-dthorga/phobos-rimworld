# Vehicle patches: research and decisions (2 Nov 2026)

The record behind [D'thorga Vehicle Patches](../../mods/PhobosVehiclePatches/README.md). **Not yet tested in game.** The player guide
is the mod's README; this holds the code readings, the sources and who chose what.

## The startup failure: RimPacts and the Harrier's framework

**What the owner's log showed** (RimWorld 1.7.4995, Harmony 3.0.0-rc.3):

```text
Error in static constructor of FixedWingAircraftFramework.HarmonyInit
HarmonyException: Patching exception in method ... Vehicles.LaunchProtocol::GetArrivalOptions(...)
InvalidProgramException: ... GetArrivalOptions_Patch2(...): token 0x06000007 resolves to an open generic type.
```

**The cause, from the assemblies** (read 2 Nov 2026):

1. RimPacts by wowgag (`RptVehicleBridge.Install`, called from its static constructor) patches `GetArrivalOptions` with
   `typeof(RptVfArrivalFilter<>).MakeGenericType(ArrivalOption).GetMethod("Postfix")`. Our inference for why it's generic: RimPacts has no
   compile-time reference to Vehicle Framework, and a postfix must name the return type exactly.
2. Harmony stores a patch as a module ID and method token (`Patch.PatchMethod`, `AccessTools.GetMethodByModuleAndToken`, `Module.ResolveMethod`).
   A token can't record generic arguments, so the patch comes back from Harmony's shared state as a method on the open generic type.
3. The wrapper's number is `VersionCount + 1`, so `_Patch2` means one earlier patch. The first wrapper works because that `Patch` object still
   holds the closed method. The second is the failing one. RimPacts is line 156 of the owner's mod list and the Harrier line 248.
4. The framework calls `PatchAll` once, in `HarmonyInit`, so the exception stops every later class: 65 `[HarmonyPatch]` attributes in all, with
   more than 50 after this one, including `VehiclePawn_BoardPawn_VtolPatch`, the refuel and repair work givers, `LandingTargeter_*`,
   `OrderVehicle_PawnGotoAction_TaxiPatch`, `Command_TargeterCooldownAction_AttackRunPatch` and `MapParent_ShouldRemoveMapNow_AirborneVtolPatch`.

**Ruled out:** CeleTech Arsenal's `CeleTech_Shuttle.dll` contains the same name, but it's its own private method, and the mod is off. A scan of every
active mod's DLLs for the method name, in both encodings, finds only Vehicle Framework, RimPacts and the framework.

**A player report that may be the same thing:** Xenogray on the Harrier's Workshop page (Nov 2026) could build the jet but not board it, and
upgrades never finished. That matches the missing patches, but only if they run RimPacts; we can't tell.

**What 0.1.0 does.** `src/PhobosVehiclePatches/RimPactsArrivalFilter.cs` postfixes RimPacts' `Install` from a `Mod` constructor. `LoadedModManager`
loads assemblies, then creates mod classes, and static constructors run later still, so it's always in time. After `Install`, if `wowgag.RimPacts` owns
a postfix on the method whose declaring type is generic, it builds a delegate to the closed method, unpatches RimPacts' postfix and adds an ordinary one
that calls the delegate. Either load order works: if the framework patched first, its postfix stays and only RimPacts' is swapped.

**Harmony 3 check.** The assembly is compiled against Lib.Harmony 2.4.3. `Harmony.Patch`, `Unpatch(MethodBase, HarmonyPatchType, string)`, `GetPatchInfo`,
`Patches.Postfixes`, `Patch.owner`, `Patch.PatchMethod`, `AccessTools.TypeByName`, `AccessTools.Method`, `HarmonyMethod(MethodInfo)` and the enum value
`Postfix` (2) were read in Harmony 3.0.0-rc.3 and match.

**1.7 readiness.** All 64 of the framework's named patch targets exist in the 1.7 game or Vehicle Framework (a name scan, not a run). Vehicle Framework has no
1.7 folder and runs its 1.6 build. RimThunder Core has no 1.7 load block, and `ModContentPack` falls back to the newest lower block, so its parts load; the
owner's save lists them. A second failure hidden behind the first can't be ruled out until the game runs.

## The build cost

Stock: 450 steel, 150 plasteel, 25 components, 6 advanced components, 1 VVE engine block. The new list is in the README.

Item data read from the installed mods: `EngineTurbine` 225 kg, 2,500 silver (RimThunder Core), VVE parts 4 to 18 kg, steel 0.5 kg and 1.9 silver, plasteel
0.25 kg and 9 silver, components 0.6 kg and 32 silver.

| Vehicle | Parts value | Share of market value | Parts mass | Vehicle mass |
| --- | --- | --- | --- | --- |
| Harrier, stock | 4,530 | 32% | 299 kg | 700 |
| Harrier, new | 7,895 | 56% | 658 kg | 700 |
| Harrier, new, without RimThunder | 6,370 | 46% | 487 kg | 700 |
| Hind | 6,655 | 55% | 573 kg | 1,100 |
| Mongoose | 5,975 | 63% | 497 kg | 600 |

**What's sourced:**

- Four swivelling exhausts: [Smithsonian National Air and Space Museum, Pegasus Mk 5](https://airandspace.si.edu/es/collection-objects/bristol-siddeley-pegasus-mk-5-turbofan-engine/nasm_A19720632000).
- 25% of airframe weight in composites: [AV-8B/GR Mk 5 paper, Proc. IMechE, 1986](https://journals.sagepub.com/doi/pdf/10.1243/PIME_PROC_1986_200_178_02).
- Empty weight about 6,340 kg (secondary): [Pima Air & Space Museum](https://pimaair.org/designation/av-8b/). Engine dry weight about 1,796 kg, Pegasus 11-61
  (secondary): [All-Aero](https://all-aero.com/2024/01/10/bristol-be53-pegasus-vtol-rolls-royce-pegasus-f402). So about 28%, against 32% for the turbine in the game.

**Authored:** every count. Plasteel as a stand-in for composites is a simplification. No source was found for the landing gear's layout.

**Vehicle Framework reads the first cost item** for the destruction sound (`VehiclePawn`, `CostList[0].thingDef.stuffProps`), so steel stays first.

## Bombs under Combat Extended

CE's `Harrier_CE_Mk84.xml` and `Harrier_CombatExtended.xml` give Mk. 84s 907 kg and the Mk. 82 227 kg, from about 70 kg and 33 kg of materials. CE's own shells nearly balance:
its 155 mm HE takes 94 steel, 17 FSX and 2 components, about 57 kg.

**Real fill weights** (secondary): 87 kg for the Mk. 82 ([Mark 82 bomb](https://en.wikipedia.org/wiki/Mark_82_bomb)), 429 kg for the Mk. 84
([MAPS Air Museum](https://mapsairmuseum.org/wp-content/uploads/2024/02/Mark-84-Bomb.pdf)). The rest is the case: 140 kg and 478 kg.

**Item masses:** heavy casting 75 kg, FSX and prometheum 0.5 kg, steel 0.5 kg, chemfuel 0.05 kg, wastepack 6 kg, components 0.6 kg.

| Bomb | New | Materials weigh | Without RimThunder |
| --- | --- | --- | --- |
| Mk. 82 | 2 castings, 20 FSX, 300 chemfuel, 2 components | 176 kg of 227 | 280 steel for the castings |
| Mk. 84 incendiary | 6 castings, 100 prometheum, 1,500 chemfuel, 2 components | 576 kg of 907 | 956 steel |
| Mk. 84 EMP | 6 castings, 840 steel, 12 components | 877 kg | 1,796 steel in all |
| Mk. 84 smoke | 6 castings, 760 steel, 1,000 chemfuel, 1 component | 881 kg | 1,716 steel in all |
| Mk. 84 poison gas | 6 castings, 72 wastepacks, 1 component | 883 kg | 956 steel |

**How the fill was set.** A full match took 174 FSX or 858 prometheum. The owner rejected 858 prometheum as far too much play time to make and asked to balance it with chemfuel, then
asked for FSX to match. Both now use about 0.23 of the scarce ingredient and 3.5 chemfuel per kg of real fill, with chemfuel's low unit mass explaining the shortfall.

**Against the owner's PG15 stocks** (740 FSX, 762 prometheum, 20,304 chemfuel, 5 wastepacks, 30,777 steel): four Mk. 82s take 80 FSX and 1,200 chemfuel, about 3 refiner-days of FSX at 25 a day.

**MarketValue** is removed so `StatWorker_MarketValue` works it out from the ingredients. **Guards:** the Mk. 82 change requires `statBases[Mass="227"]`, which only the Harrier's CE patch sets;
`check-factory-patches.py` reads unpatched Defs, so it can't enter that branch, and it was checked by reading `FWA_Ammo.xml` and `Harrier_CombatExtended.xml`.

## Bombs and rockets at the ammunition fabricator (0.2.0, 3 Nov 2026)

**Owner's request:** make the Harrier's bombs, then also its rockets, at The Dead Man's Switch's semi-automated ammunition fabricator (`DMS_TableAmmoPrinter`). `Mods/HarrierVtol/Patches/Fabricator_HarrierVtol.xml` adds
it to each of the five bombs' `recipeMaker/recipeUsers`, which `RecipeDefGenerator` copies to the recipe, and to the `recipeUsers` of the three rocket `RecipeDef`s. The machining table stays.

**Why a recipe-user entry is enough** (Fortified 1.7 build of 31 Oct 2026, read 3 Nov 2026). `Building_WorkTableAutonomous` takes any `Bill_Production`. `WorkGiver_DoAutonomousBill` turns the game's own `DoBill` job into a loading job, so it finds ingredients the normal way.

**Why not CE's route.** DMS's `AmmoProduction.xml` puts its machine on CE's ammo through `AmmoInjector` and trade tags. That only handles CE's ammo, and the bombs are plain items with a `recipeMaker`.

**The rockets.** `AmmoSet_Harrier57mmRocket` holds `Ammo_57mmRocket_HE` and `Ammo_57mmRocket_HEAT` (CE's) and `Ammo_Mongoose57mmRocket_AP` (carried by the Harrier, Hind and Mongoose). Their tags include DMS's, but `InjectAmmos` handles only ammo a weapon's `CompProperties_AmmoUser` names or a `Patches.GetUsedAmmo` callback returns, and a vehicle turret names its ammo in `CETurretDataDefModExtension`. Whether the rockets already reach the fabricator that way wasn't settled (inference: possibly, since the AP recipe lists no bench of its own and the author reports making it). Naming the fabricator directly works in either case: `ToggleRecipeOnBench` uses `AddDistinct`, and `AllRecipes` only asks whether the list contains the bench. Thermobaric isn't in the jet's set and is left out.

**The AP recipe is defined three times** among the owner's mods, one copy per author's mod. The patch reaches every copy, so it doesn't matter which the game keeps. `check-factory-patches.py` now accepts several matches for a path to one whole Def, and still rejects them for anything narrower.

**The guard.** The fabricator's Def is in DMS's `1.7/CE` folder, so it exists only with CE. `PatchOperationConditional` on it needs no `loadAfter`: `LoadedModManager` combines all mods' XML before `ApplyPatches`. Each add has `success` set to `Always`, because `Harrier_Mk84Bomb_ToxGas` exists only with Biotech and a failed add would end the sequence.

**What the code does with a bomb** (from the same reading; none of this was seen in play):

| Step | Behaviour |
| --- | --- |
| Time | Work / `WorkTableWorkSpeedFactor` 2: 4,500 becomes 2,250 ticks for the Mk. 82 (the Harrier's CE patch sets 4,500) and 6,000 becomes 3,000 for a Mk. 84. Rockets, for 5: HE 13,000 to 6,500, HEAT 8,600 to 4,300, AP 5,500 to 2,750. Each fits one 40,000-work stage |
| Colonist | `DMS_Operate`, Crafting work. Loading and finishing each wait 300 ticks. `PawnSatisfiesSkillRequirements` applies at the recipe's Crafting 6 or 7 for bombs and 4 for rockets (`LauncherAmmoRecipeBase`). The machine's own skill of 12 only rates quality, and these items have none |
| Output | `Finish` places products `Near` the interaction cell and ignores the bill's storage setting |
| Interruption | The loading job's finish action calls `Cancel` unless the job succeeds, which drops the container's contents beside the machine |

**Inference, untested:** a very long load (10 or more trips for 1,500 chemfuel) goes through the same remainder queue as ordinary ammo. It's expected to work, but no ordinary ammo was checked at this size.

**Owner's save** (Autosave-4, read only, 3 Nov 2026): two fabricators, at (199, 259) and (193, 259).

## Heavy castings at the rotary forger (0.3.0, 3 Nov 2026)

**Owner's request:** make RimThunder's heavy component at the rotary forger. The item is `ComponentHeavyCasting` (its recipe and label say "heavy compoment", so the owner's spelling note is RimThunder's own) and the forger is `DMSE_TableLathe` from The Dead Man's Switch - Expedition. `Mods/RimThunder/Patches/Forger_RimThunder.xml` adds the forger to the `recipeUsers` of `CastFromMold`, RimThunder's only recipe for it. No other installed mod's XML mentions that recipe.

**Why that's enough.** The forger's parent is `DMS_AutofacturerBenchBase`, so it's the same `Fortified.Building_WorkTableAutonomous` as the fabricator, and Expedition's `Patches.xml` adds it to `DMS_Operate`. See the fabricator section for how the machine treats a recipe.

| Step | Behaviour (read from the code and Defs, not seen in play) |
| --- | --- |
| Time | `workAmount` 500 / `WorkTableWorkSpeedFactor` 2: 250 ticks, one 30,000-work stage. `SmeltingSpeed` plays no part on the machine |
| Colonist | `DMS_Operate`, Crafting work. Loading and finishing each wait 300 ticks (`workTime` is unset, so the default). No skill or research on the recipe; the forger itself needs `DMS_Autofacturer` |
| Output | Both products, the casting and `ChunkSlagSteel`, are placed `Near` the interaction cell |

**Candid limit, agent's reading:** at the smelter this is about 500 ticks of colonist work at base `SmeltingSpeed`, so the forger takes slightly more colonist time for this short recipe. It helps with bench queues and who does the work, not speed.

**Realism note, agent's reading.** Rotary forging suits shafts and similar round parts, such as RimThunder's "cranks", and less so its cylinders and manifolds. The recipe is a shared one, says "Casting metal" and still gives slag, a simplification kept so there's one recipe. A separate forged recipe with no slag would be a small addition if the owner wants it.

**Owner's save** (PG16, read only, 3 Nov 2026): two forgers, at (221, 264) and (234, 264).

## Sorties, RimWorld 1.7 and Combat Extended: the review (0.3.1, 3 Nov 2026)

**Owner's request:** look through the Harrier's code for bugs, especially how it runs sorties, the move from 1.6 to 1.7, and Combat Extended. **Nothing here was seen in play.**

**What was read.** The Harrier's shipped C# (26 files), its bundled Fixed-Wing Aircraft Framework (build 19o, decompiled to 21,000 lines), VTOL Raids' jet add-on, Vehicle Framework's 1.6 build, Combat Extended with its `VehiclesCompat` assembly, the 1.7 game's `MapParent`, `Site`, `MapPawns` and `MapDeiniter`, the owner's log of 3 Nov (both runs) and the newest save, PG17. Decompiled output stays in `.local/decompiled/`.

**The log.** The run of 12:25 shows no error or warning naming the Harrier, its framework, VTOL Raids or Vehicle Framework; our RimPacts line is there and VTOL Raids reports "Fixed-wing add-on ready. Jets: Harrier_Jet". The earlier run has one `Invalid count: 0` on a HaulToContainer job carrying a component to a runway beacon frame: the game's own construction hauling, from a queued job, not the aircraft code; harmless.

**1.7.** `.local/tools/rimexodus-framework/apicheck.ps1` compares an assembly's member references with a provider assembly's metadata, by name and parameter count. Run for AV8BHarrier.dll, FixedWingAircraftFramework.dll and VTOLRaids.FixedWing.dll against the 1.7 `Assembly-CSharp.dll`, Vehicle Framework's `Vehicles.dll` and `SmashTools.dll`, Harmony 3's `0Harmony.dll`, the framework itself and the 1.7 `UnityStubs.dll` (which stands in for the Unity modules the mods reference): 21 pairs, 0 missing types, 0 missing members. Harmony 3 logged no patch failure. What this can't show: a parameter whose type changed but not its count, and CoreCLR inlining a small patched member so a patch never runs, which the author met once (`OrderLandingHome`) and worked around. Small targets of the same shape: the `CanTaxi` getter (a wrecked engine could taxi), `VtolContent.ParkingIcon` (the pad button's icon only); `LaunchProtocol.CanLaunchNow` is virtual and safe. Items for the owner's in-game list, not faults found.

**Sorties, as the framework runs them** (code reading):

| Step | Code | What it does |
| --- | --- | --- |
| Button | `VtolVehiclePawn.SortieGizmo` | Sets `SortieTargeting` and opens Vehicle Framework's destination chooser. Disabled whenever the Launch button is: under a roof, no operator, no fuel, moving |
| Choices | `LaunchProtocol_GetArrivalOptions_FixedWingPatch` | An open map keeps the options with a landing targeter, relabelled fly to or land at; a closed map parent keeps only the edge-drop arrival, relabelled fly raid on (a settlement) or fly sortie to |
| Refusals | `CompVehicleLauncher_CanTarget_FixedWingSortiePatch` | The player's own home map while sortie-targeting; any tile that isn't a map parent |
| Fuel | `TryFuelForSortie` | World fuel there, world fuel back (from the target to the home it would return to), one hour of local burn, 10% of the tank |
| Departure | `BeginDepartingMap`, `TryCompleteMapDeparture` | Flies to the map edge, despawns, becomes an `AerialVehicleInFlight` with the arrival action; pending path, action, direction and home parent are saved |
| Arrival | `ArrivalAction_LandToCell_Arrived_VtolPatch`, `AerialVehicleArrivalModeWorker_EdgeDrop_VehicleArrived_VtolPatch` | Spawns at the edge facing the map centre, flying; "on station" |
| Return | `TickAutoLand`, `TickBingoFuel`, `ReturnToBase`, `HomeForReturn` | When the last rocket or bomb goes, or fuel falls to the trip home plus 10%: home is the map it took off from if that still has a player home map, else the nearest player home whose runways include a 24 by 5 rectangle. None: "no home runway. Circling" |
| Home | `ArriveFixedWing`, `OrderLandingHome` | Enters from the edge, lands on a runway (the Harrier's own code sends it to its pad first) |

**Fuel maths** (Vehicle Framework 1.6, `FuelNeededToLaunchAtDist` and `AerialVehicleInFlight`): fuel per tile = 150 a day / 60,000 x 1,000 / FlightSpeed 16 = 0.156; ticks per tile = 1,000 / 16 = 62.5. A 30-tile sortie is about 5 fuel and 1,900 ticks each way. On a map, `fuelConsumptionPerTick` 0.0167 empties 250 in 15,000 ticks, 6 hours. So fuel limits time over the target, not reach, and the bingo rule keeps enough for the trip home.

**The fault found and fixed: a sortie's map could close under the jet.** `MapParent.TickInterval` calls `CheckRemoveMapNow` every interval; `Site.ShouldRemoveMapNow` and most modded rules return true once `MapPawns.AnyPawnBlockingMapRemoval` is false. That getter looks for colonists among spawned pawns and among pawns held in things on the map, through `ThingOwnerUtility.GetAllThingsRecursively`, which reaches a pawn's inventory, carried thing, equipment and apparel; Vehicle Framework keeps crew in `VehicleRoleHandler` holders that `VehiclePawn` doesn't list as child holders (no `GetChildHolders` on it; the handler, skyfaller and world-object classes have their own), so a pilot aboard is invisible to the game's check. Vehicle Framework's own postfix (`Patch_MapHandling.AnyVehicleBlockingMapRemoval`) covers vehicles that claim ground cells; an airborne jet releases its claims (`VtolGroundGridUtility.DetachFromGroundGrid` calls `VehiclePositionManager.ReleaseClaimed`). The framework's `MapParent_ShouldRemoveMapNow_AirborneVtolPatch` covers the rest, but its `TargetMethods` scans `typeof(MapParent).Assembly` only, so a mod's own `ShouldRemoveMapNow` isn't patched. Among the owner's mods, metadata shows such overrides in Real Ruins (`RealRuinsPOIWorldObject`, `AbandonedBaseWorldObject`, `SmallRuinsWorldObject`, each returning true as soon as the game's check is false), RimMarket (`ConvoyBattle`, the same), Vanilla Expanded Framework (`MovingBase`), Vanilla Gravship Expanded, IRBM, RimShips, DMS and DMS Expedition, and Empire. Fortified's `FFF_Company` calls `Site`'s rule, which the framework does patch, so it was covered. **The fix** (`src/PhobosVehiclePatches/AirborneCrewKeepsMap.cs`) is one postfix on the game's getter: a spawned, undestroyed player `VehiclePawn` with any `IsFreeColonist` aboard blocks removal. Every rule above reads that getter, so one patch covers them all without naming a mod, and the VTOL Vehicle framework's helicopters with them (its DLL carries the same `MapHasAirborneVtolWithFreeColonist` scan). Cost: a loop over spawned pawns when the game's own check has already found nothing, which already loops over them.

**Combat Extended.** Every CE member the Harrier reaches by name exists in the installed build (CE's `CombatExtended.dll` of 18 Oct 2026 and `ModPatches/VehicleFramework/Assemblies/VehiclesCompat.dll`): `CE_Utility.GetCollisionBodyFactors(Pawn)`, which asks `VehiclesCompat`'s callback first and the Harrier's postfix then widens to 5 cells; `VehicleArmorPatch.TryPenetrateComponents(VehicleStatHandler stats, ref DamageInfo dinfo, List<VehicleComponent> components, VehiclePartDepth hitDepth, StringBuilder report)`, whose argument order matches the Harrier's prefix (`__0`, `__2`, `__3`) and whose `VehicleStatHandler.vehicle` field exists; `VehicleTurret.LaunchProjectileCE`, the static delegate Vehicle Framework checks for null before firing the vanilla way, which is how the Harrier drops its bombs outside CE; `ProjectileCE.launcher`, `equipmentDef` and its two `Launch` overloads; Vehicle Framework's `CETurretDataDefModExtension` fields the CE patch sets. No fault found. The earlier log's `Verb_LaunchProjectileStaticCE` registration errors belong to Vanilla Apparel Expanded's mini turret pack with MVCF, not the aircraft.

**The Hind and Mongoose** use rabiosus's VTOL Vehicle framework (`VTOLVehicle.VtolVehiclePawn`, a separate assembly); the Harrier's framework is a fork with its own namespace. Both patch Vehicle Framework side by side, each prefix checking its own vehicle type; the log shows no clash.

**Not faults, but worth the owner's knowing:** the Harrier has no vertical take-off, so a jet landed on a sortie map stays there; the sortie button is off under a roof; `HomeForReturn` needs a complete 24 by 5 beacon rectangle. **Owner's save** (PG17, read only, 3 Nov 2026): two beacons, at (139, 196) and (146, 196), 8 cells apart; no rectangle yet. The other two go 23 cells away along the strip.

**Unverified:** since 0.1.0 swapped RimPacts' postfix for ours, which runs first on `GetArrivalOptions`, ours or the framework's relabelling, wasn't settled; both are plain postfixes and either order gives the same options. The Harrier's Workshop comments couldn't be read (Steam answered 429 three times).

## Decisions

| Date | Decision | Who |
| --- | --- | --- |
| 2 Nov 2026 | Replace the Harrier's build materials, using RimThunder and VVE where appropriate | owner (request) |
| 2 Nov 2026 | RimThunder's turbine as the engine, with its research added to the blueprint | owner (agent recommended) |
| 2 Nov 2026 | Fix the root of the startup failure, not only work around it | owner |
| 2 Nov 2026 | A runtime swap of RimPacts' postfix, in place of a rescue that re-applied the framework's patches | agent (to meet the owner's request) |
| 2 Nov 2026 | Honest bomb costs and fuel tanks on Drop Tanks | owner (agent mentioned both) |
| 2 Nov 2026 | Prometheum and FSX cut, with chemfuel carrying the fill; the same ratio for both | owner (agent chose 100 and 1,500, and the ratio) |
| 2 Nov 2026 | Mod name, a general vehicle patches mod with a folder per patched mod | agent default |
| 2 Nov 2026 | Heavy castings for bomb cases, steel as the fallback | agent default |
| 2 Nov 2026 | Construction skill 10; EMP, smoke and tox fills; Drop Tanks' 20 steel | agent defaults |
| 2 Nov 2026 | Work and research for bombs left unchanged | agent default |
| 3 Nov 2026 | The Harrier's bombs at the ammunition fabricator | owner (request) |
| 3 Nov 2026 | The Harrier's rockets there too | owner (request) |
| 3 Nov 2026 | RimThunder's heavy casting at the rotary forger | owner (request) |
| 3 Nov 2026 | Same recipe, slag included, smelter and platform kept; in the vehicle patches mod, in its own folder | agent defaults |
| 3 Nov 2026 | Keep the machining table as well, same costs; rockets stay in 0.2.0, which was never installed or released | agent defaults |
| 3 Nov 2026 | Review the Harrier's code for sortie, 1.7 and Combat Extended faults, fixing what needs it | owner (request) |
| 3 Nov 2026 | Keep a sortie's map open through the game's own check rather than patching each mod's rule; player vehicles with a free colonist aboard, all of them | agent default |

## Spaceship Turret Hull under Combat Extended (5 Nov 2026)

**Not yet tested in game.** The owner reported that the hulls looked supported (the stats show a calibre and magazine) but couldn't be loaded. Their screenshots showed a heavy autocannon hull with 20x102mm NATO and a magazine of 100.

**The cause, from the files:**

- MK's seven hulls set `thingClass` to `Building_TurretGun`. CE's reload job, gizmos and ammo checks work through `Building_TurretGunCE`, or a type registered in `TurretRegistry`; the vanilla class is neither. CE converts turrets by defName (`Patches/Core/ThingDefs_Buildings/Buildings_Security.xml`) and ships no patch for this mod.
- CE replaces `Gun_MiniTurret`, `Gun_AutocannonTurret` and `Gun_TurretSniper` by name, which is why four hulls show CE stats. The stat card reads the gun, so it looks finished while the building can't use it.
- `Gun_TurretShotgun`, `Gun_TurretSniperPrecision` and `Gun_TurretIncendiary` are MK's own and stay stock. CE's gun autopatcher is off by default (`enableWeaponAutopatcher`).
- The owner's PG22 holds four hulls, all `SpaceshipHeavyTurretHull`, saved with the vanilla class.

**What the save does.** `Verse.ScribeExtractor.SaveableFromNode` makes the class named in the save; the Def's class is only a fallback when that type can't be found. So existing hulls stay vanilla: unharmed, and still unable to load. A converter would have to swap the building in place, and despawning a roof-holding wall carries a roof-collapse check we haven't read. (Changing the class alone wouldn't do: the vanilla class saves its gun as `gun`, CE reads `gunInt`, and `InitGun` throws on a null gun.) Agent default: no converter; the player rebuilds. The owner has four, so this is cheap, and a converter stays possible if other players need it.

**Checked by reading:** a shot from a wall starts at height 2, and CE sets `ignoreRoof` for shots from that height under a roof (`ProjectileCE`), so a roofed hull shouldn't hit its own roof. Colonists reload by touch, so a wall with one open neighbouring cell should do.

**Who chose what (all agent defaults, for the owner to revise):**

| Choice | Value | Basis |
| --- | --- | --- |
| Home | D'thorga Vehicle Patches | Already active and loads after both mods. A gravship is a vehicle. No new mod to switch on |
| Cooldown, no explosion | 1.0 s, removed | What CE does to its own turrets |
| Aim | Light 0.25/0.5, autocannon 0.75/1.25, long range 0.75/1.5 | CE's values; which hull counts as which kind is authored |
| Shotgun hull | 12 gauge, range 19, drum of 20 | Figures from CE's `Gun_ChainShotgun`; range is MK's 15.5 times CE's ratio (16 / 12.9); drum size authored |
| Sniper hull | .50 BMG, range 84, magazine 10 | Figures from CE's `Gun_SniperRifle`; range is MK's 50 times (75 / 44.9); calibre, spread, recoil and magazine authored |
| Incendiary hull | 30x64mm fuel, range 45, magazine 10 | Figures from CE's `Gun_IncendiaryLauncher`; range is MK's 27 times (40 / 23.9); magazine authored |
| Uranium slug hull | Relabelled 40mm flak hull | CE's `Gun_TurretSniper` is a 40x311mmR flak gun. The defName stays |

**Left for the owner: cost.** A heavy autocannon hull costs 12 steel, 5 components, 2 advanced components and 8,000 work, for the same gun as CE's autocannon turret at 425 steel, 40 plasteel, 9 components and 35,000 work. The hull also carries armour and an airtight wall. The patch keeps MK's costs until the owner chooses.

**Not traced:** the inspect pane shows "10 W (500 W active)" where the stat card says 200 W, so another mod is changing turret power. It doesn't affect loading.

## Not changed, for the owner to decide

- Upgrade costs and CE armour weren't compared with the Hind's.
- A shot-down VTOL Raids Harrier may leave a share of the new cost; not checked.
- The owner's log also has `Unable to unlock Vehicles.CompUpgradeTree post-load for Humvee`, twice. Unrelated to this.
- Loading the Harrier above RimPacts also avoids the failure, with no code. It's fragile, since any later patch of that method brings it back.
- The Harrier can't take off without a beacon runway, so a vertical landing on a sortie map strands it. A vertical take-off would be a Harrier feature, not a patch.
- Small patched members that CoreCLR might inline (`CanTaxi`, `VtolContent.ParkingIcon`): only an in-game check can tell.

## Bug reports to post (drafts; the owner posts them)

Written for the mods' authors.

**For RimPacts (wowgag):**

> Hi! RimPacts' patch on Vehicle Framework's `LaunchProtocol.GetArrivalOptions` breaks any mod that patches the same method after it. `RptVehicleBridge.Install` uses a postfix inside a generic class (`RptVfArrivalFilter<>.MakeGenericType(ArrivalOption)`). Harmony stores patches by method token, which can't hold generic arguments, so when a second patch rebuilds the method it gets the open generic back and throws: `InvalidProgramException ... GetArrivalOptions_Patch2 ... resolves to an open generic type`. It breaks the Harrier VTOL Attack Aircraft mod's flight framework here, on RimWorld 1.7 with Harmony 3.0.0-rc.3. A plain postfix in a non-generic class would fix it, for example in a small assembly that references Vehicle Framework and loads only when it's active. Thanks for RimPacts!

**For the Harrier's framework (potemkinvillager), the map removal:**

> Hi again. `MapParent_ShouldRemoveMapNow_AirborneVtolPatch.TargetMethods` scans `typeof(MapParent).Assembly` only, so a mod's own `ShouldRemoveMapNow` override isn't patched. Real Ruins' three site classes and RimMarket's `ConvoyBattle` return true as soon as `MapPawns.AnyPawnBlockingMapRemoval` is false, and that getter can't see a pilot inside a Vehicle Framework vehicle, so a jet on a sortie over one of those sites can have the map closed under it. Scanning every loaded assembly, or postfixing the getter itself to count a free colonist aboard any spawned player vehicle, would cover them all. Thanks!

**For the Harrier (potemkinvillager):**

> Hi! Lovely jet. On 1.7 with RimPacts active, `FixedWingAircraftFramework.HarmonyInit` throws `InvalidProgramException ... GetArrivalOptions_Patch2 ... resolves to an open generic type`, and `PatchAll` stops there, so everything after `LaunchProtocol_GetArrivalOptions_FixedWingPatch` never loads, including boarding, refuelling and landing. The cause is on RimPacts' side: its postfix on that method is in a generic class, which Harmony can't rebuild. Might this explain Xenogray's report? Applying your patch classes one at a time, each in a try/catch, would limit the damage from any conflict like this. I've reported it to RimPacts' author too.
