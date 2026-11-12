# Vehicle patches: research and decisions (2 Nov 2026)

**Since 7 November 2026 this mod is a part of [D'thorga Patches](../../mods/PhobosPatches/README.md)** ([why](mod-consolidation.md)); paths below are updated, the rest is as recorded.

The record behind [D'thorga Vehicle Patches](../../mods/PhobosPatches/README.md). **Not yet tested in game.** The player guide
is the mod's README; this holds the code readings, the sources and who chose what.

## Rearming: one kind to a magazine (9 Nov 2026)

**Observed by the owner, and read in the Harrier's framework:** on landing "absolutely everybody comes out" and the counter goes up and down. The save (PG4) held HE and HEAT 57 mm rockets in store, with a pod on HEAT at 4 of 32 and no kind chosen.

- **The cause, from the code:** with no kind chosen, `WorkGiver_RearmVehicle.FindFallback` takes the loaded kind, or any kind if that one is reserved or out; `GroundRearm.LoadDirect` drops the whole magazine when a different kind arrives; `RoundsNeeded` then asks for a full magazine; and `AlreadyBeingCarried` counts only the same kind. Each swap called out more colonists for a new full load.
- **The fix** (`src/PhobosPatches.AircraftRearm/RearmOneKind.cs`, rule in `RearmRule`; the Hind and the Mongoose from Patches 0.16.4, whose copies of this code read the same): the postfix on `FindFallback` fetches only the loaded kind, or with an empty magazine the first kind on its way; the prefix on `LoadDirect` refuses a different kind unless the player chose it.
- **What it stops:** an automatic change of kind while rounds remain, so a part-full magazine whose kind has run out waits for the player. That is the agent's choice; the player can choose another kind on the weapon.
- **The author is active** (`workshop-research.py`, updated 1 Nov 2026), so by the standing rule this would be theirs to fix; the owner asked for it as a priority. **Remove the patch if their mod fixes it.** The patches find each aircraft's copy by name, and the load line says if one has changed. **Not yet tested in game.**

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

**What 0.1.0 does.** `src/PhobosPatches.Harrier/RimPactsArrivalFilter.cs` postfixes RimPacts' `Install` from a `Mod` constructor. `LoadedModManager`
loads assemblies, then creates mod classes, and static constructors run later still, so it's always in time. After `Install`, if `wowgag.RimPacts` owns
a postfix on the method whose declaring type is generic, it builds a delegate to the closed method, unpatches RimPacts' postfix and adds an ordinary one
that calls the delegate. Either load order works: if the framework patched first, its postfix stays and only RimPacts' is swapped.

**Harmony 3 check.** The assembly is compiled against Lib.Harmony 2.4.3 (since 0.3.2, 4 Nov 2026, the 1.7 build against Harmony 3). `Harmony.Patch`, `Unpatch(MethodBase, HarmonyPatchType, string)`, `GetPatchInfo`,
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

**Superseded in 0.6.0** (see [the bomb case](#the-bomb-case-060-7-nov-2026)): the cases below were heavy castings, or steel without
RimThunder, until then. The fills are unchanged.

| Bomb | 0.1.0 to 0.5.0 | Materials weighed | Without RimThunder |
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

**Superseded by 0.5.0:** the forger no longer runs `CastFromMold`; see the next section.

## Heavy castings in batches of 12 (0.5.0, 6 Nov 2026)

**Owner's request:** answer the candid limit above by making several castings per run; the owner proposed 12 and the agent agreed. `Mods/RimThunder/Patches/Forger_RimThunder.xml` now adds a recipe of our own, `PhobosForgeHeavyCastingBatch`, used only by `DMSE_TableLathe`, and no longer adds the forger to `CastFromMold`. The smelter and assembly platform keep RimThunder's one-at-a-time recipe.

| Step | Behaviour (read from the code and Defs, not seen in play) |
| --- | --- |
| Guard | One `PatchOperationConditional` on `Defs[ThingDef/defName="DMSE_TableLathe" and RecipeDef/defName="CastFromMold"]` adds the whole `RecipeDef` to `Defs`, so it never names a missing Def (RimThunder's recipe and casting are absent with Aoba.Motorization.Engine active) |
| Cost | 1,200 steel to 12 `ComponentHeavyCasting` and 12 `ChunkSlagSteel`: `CastFromMold` times 12 |
| Time | `workAmount` 6,000 / `WorkTableWorkSpeedFactor` 2: 3,000 ticks, one 30,000-work stage (`Building_WorkTableAutonomous.StartBill` reads `Bill.GetWorkAmount`) |
| Colonist | The same two 300-tick waits as before, now for 12 castings. Steel's stack limit is 75, so loading is about 16 carries, about 1.3 per casting against 2 per casting for 100 steel at the smelter |
| Output | `Building_WorkTableAutonomous.Finish` makes the products with `GenRecipe.MakeRecipeProducts` and places each with `GenPlace.TryPlaceThing` in Near mode, radius 30. Both products have `stackLimit` 1; the 1.7 game's Near mode loops `TryPlaceDirect`, which spawns one stack per cell, until the whole count is placed: 24 things around the interaction cell |

**Agent's reading of the gain:** about 600 ticks of fixed colonist waits for 12 castings, about 50 each, against about 500 ticks of colonist work per casting at the smelter at base `SmeltingSpeed`. Walking time for the carries depends on the base layout and isn't counted.

**Why 12 suits RimThunder, agent's reading:** RimThunder's and its add-ons' recipes ask for 1 to 3 castings each (every use in the installed XML), and this mod's Mk. 82 cases asked for 2 (until 0.6.0), so one batch covers several builds without leaving the bill idle for lack of steel in most colonies.

**Unverified:** whether a one-at-a-time bill already set up at a forger under 0.3.0 or 0.4.0 keeps running once the forger no longer lists `CastFromMold`. The README tells the player to delete it.

**Unchanged:** RimThunder's mass balance. 100 steel (50 kg at 0.5 kg each) gives a 75 kg casting and a lump of slag, so mass isn't conserved; that is RimThunder's own ratio, kept so both recipes agree.

## The bomb case (0.6.0, 7 Nov 2026)

**Owner's report:** heavy castings clog VFE Factory's automated machining bays making the bombs. **Owner's choice:** a stackable bomb
case of our own in place of the castings (offered with plain steel by weight and steel at casting cost), and a stand-in picture drawn
in code until generated art is picked.

**What the save showed** (PG2, 7 Nov 2026, a read-only copy deleted afterwards; Pipe System 1.7 decompiled in `.local/`):

- RimThunder's casting inherits `stackLimit` 1 from its `HeavyItemBase`. A hopper holds one stack per slot (`maxItemsInCell`; 4 on our
  deep hopper), so a casting fills a slot by itself. Each bay had one castings-only deep hopper at Critical priority, full with 4
  castings: two Mk. 82s' worth, brought one hauling trip each.
- **Neither bay was waiting on castings at that moment.** The bay at (260, 109) had 19 of 20 FSX, with its FSX hopper empty and 63 FSX on
  the map. The bay at (260, 117) waited for 2 components: their hopper group was at Low priority, and colonists only haul to better
  storage. Both were reported to the owner as layout and stock matters.
- The intake itself (`Process.CheckInputSlots`, `ThingAndResourceOwner.AddFromThing`) handles a one-item stack correctly. The fault is
  the slot each casting uses, not a code error. No log line named them.

**A hazard in the update, from the code.** `Process` saves its `ingredientsOwners` (one per ingredient, keyed by ThingDef) and builds
them only in `Setup()`, when the bill is added. After a recipe change, a saved bill still lists castings, while `CheckInputSlots`
matches items against the Def's new ingredients and calls `AdvancedProcessorsManager.AddIngredient`, whose `GetOwnerFor` then returns
null. That would throw every 100 ticks once a case reached an input cell. Hence the README's first step: delete the bays' bomb bills
before updating. `ResetOwners` drops an unfinished bill's counted ingredients beside the machine, so nothing is lost.

**The design** (agent defaults unless stated):

| Item | Steel | Mass | Work | Skill | Stack |
| --- | --- | --- | --- | --- | --- |
| `PhobosBombMk82Case` | 280 | 140 kg | 1,000 | Crafting 6 | 4 |
| `PhobosBombMk84Case` | 956 | 478 kg | 3,000 | Crafting 7 | 4 |

Named `PhobosBombCaseMk82` and `PhobosBombCaseMk84` until Patches 0.14.2. Those names ended in a digit, which the game forbids on an
item: found in the owner's log of 8 November 2026 (IDs such as `PhobosBombCaseMk84848484840`), renamed at the owner's direction
(an exception to keeping defNames stable, taken before the first publication). `BombCaseRename.cs` carries saves across.

- One case per bomb, two sizes: one body per real bomb, and the fewest hopper slots.
- Masses are the case weights above (total weight less fill, secondary sources), so the steel balances at 0.5 kg a unit. Work is what
  the 2 and 6 castings took (RimThunder's 500 each). Skill matches each size's bombs. No research: each bomb keeps its own. No slag,
  since a case is forged and machined, not cast. Bulk 20 and 40, as the bombs. Smeltable; no MarketValue, so it follows from the steel.
- Made at the machining table; the rotary forger is added when it exists. VFE Factory's generator then gives the automated machining
  bay a bill for each case, from `recipeMaker.recipeUsers` containing `TableMachining`.
- The two ThingDefs are added by the bomb patch itself, inside its guard on the Mk. 82's 227 kg, so they exist only under Combat
  Extended, where they are used. The forger step sits in the same sequence, after them.
- New totals: Mk. 82 166 kg; Mk. 84 incendiary 604 kg (short on purpose, as before), EMP 905, smoke 909, poison gas 911.
- **The picture** is a Pillow stand-in by `assets/vehicle-patches/draw_standins.py`: olive drab, open fuze-well ring and tail, two lugs,
  no fins and no yellow band. On US bombs a yellow band marks an explosive filling, so an empty case carries none (common knowledge,
  not checked against a primary source). Generated art is in `art-backlog.md`.

**Checked offline:** the patch applied to the Harrier's, Combat Extended's and the forger's real Defs by a small script copying the
game's operation rules, with and without the forger: every step matched as intended. **Not yet tested in game.**

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

**The fault found and fixed: a sortie's map could close under the jet.** `MapParent.TickInterval` calls `CheckRemoveMapNow` every interval; `Site.ShouldRemoveMapNow` and most modded rules return true once `MapPawns.AnyPawnBlockingMapRemoval` is false. That getter looks for colonists among spawned pawns and among pawns held in things on the map, through `ThingOwnerUtility.GetAllThingsRecursively`, which reaches a pawn's inventory, carried thing, equipment and apparel; Vehicle Framework keeps crew in `VehicleRoleHandler` holders that `VehiclePawn` doesn't list as child holders (no `GetChildHolders` on it; the handler, skyfaller and world-object classes have their own), so a pilot aboard is invisible to the game's check. Vehicle Framework's own postfix (`Patch_MapHandling.AnyVehicleBlockingMapRemoval`) covers vehicles that claim ground cells; an airborne jet releases its claims (`VtolGroundGridUtility.DetachFromGroundGrid` calls `VehiclePositionManager.ReleaseClaimed`). The framework's `MapParent_ShouldRemoveMapNow_AirborneVtolPatch` covers the rest, but its `TargetMethods` scans `typeof(MapParent).Assembly` only, so a mod's own `ShouldRemoveMapNow` isn't patched. Among the owner's mods, metadata shows such overrides in Real Ruins (`RealRuinsPOIWorldObject`, `AbandonedBaseWorldObject`, `SmallRuinsWorldObject`, each returning true as soon as the game's check is false), RimMarket (`ConvoyBattle`, the same), Vanilla Expanded Framework (`MovingBase`), Vanilla Gravship Expanded, IRBM, RimShips, DMS and DMS Expedition, and Empire. Fortified's `FFF_Company` calls `Site`'s rule, which the framework does patch, so it was covered. **The fix** (`src/PhobosPatches.Harrier/AirborneCrewKeepsMap.cs`) is one postfix on the game's getter: a spawned, undestroyed player `VehiclePawn` with any `IsFreeColonist` aboard blocks removal. Every rule above reads that getter, so one patch covers them all without naming a mod, and the VTOL Vehicle framework's helicopters with them (its DLL carries the same `MapHasAirborneVtolWithFreeColonist` scan). Cost: a loop over spawned pawns when the game's own check has already found nothing, which already loops over them.

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
| 6 Nov 2026 | Heavy castings at the forger in batches of 12, in place of one at a time | owner (request; agent agreed with 12) |
| 6 Nov 2026 | A recipe of our own for the forger only; smelter and platform keep RimThunder's; slag kept; 12 times the work | agent defaults |
| 7 Nov 2026 | Replace heavy castings in the bombs with a stackable bomb case of our own | owner (choice from three agent options; agent recommended plain steel) |
| 7 Nov 2026 | A hand-drawn stand-in picture until generated art | owner (direction) |
| 7 Nov 2026 | Two case sizes, one per bomb; real case weights in steel; castings' work; bombs' skill; stack 4; no research, no slag; machining table plus forger; only under CE | agent defaults |

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

- Upgrade costs, CE armour and what a shot-down Harrier leaves: checked on 7 Nov 2026, see the next section.
- The owner's log also has `Unable to unlock Vehicles.CompUpgradeTree post-load for Humvee`, twice. Unrelated to this.
- Loading the Harrier above RimPacts also avoids the failure, with no code. It's fragile, since any later patch of that method brings it back.
- The Harrier can't take off without a beacon runway, so a vertical landing on a sortie map strands it. A vertical take-off would be a Harrier feature, not a patch.
- Small patched members that CoreCLR might inline (`CanTaxi`, `VtolContent.ParkingIcon`): only an in-game check can tell.

## The Harrier against the Hind: upgrades, CE armour and wreckage (7 Nov 2026)

The owner asked to check both and propose (7 Nov 2026). Read from the Workshop items (Harrier 3826359341, Hind 3822052079, Mongoose
3819896964, VTOL Raids 3821646218, **all by potemkinvillager**) and the Vehicle Framework 1.6 decompile in `.local/`. Nothing is changed;
**nothing here was seen in play.** Values use this record's prices (steel 1.9, plasteel 9, components 32, VVE fuel tank 95). The Hind runs
on rabiosus's VTOL Vehicle framework, the Harrier on its own fixed-wing fork.

**Upgrades (outside Combat Extended): in proportion, no change proposed.**

| | Harrier | Hind |
| --- | --- | --- |
| Plasteel armour | 120 plasteel, 4 components, 6,000 work: about 1,208 silver, 15% of our new build value (7,895; it was 27% of stock 4,530) | 150 plasteel, 4 components, 6,000 work: about 1,478, 22% of 6,655 |
| Most fitted at once | Armour and one loadout, about 19% of build | Machine gun, heavy machine gun and armour, about 40% |
| Sharp armour per plasteel, body | +0.00125 | +0.00117 |

The loadouts (six bombs, the Mk. 84 variants, our drop tanks) are cheap pylon swaps that replace each other, which seems fair.

**Combat Extended armour: the base values are the author's choice; the armour upgrade is poor value.** Both mods ship their own CE
patches. Sharp is mm RHA, blunt MPa.

| Part | Harrier (700 kg) | Hind (1,100 kg) | Mongoose (600 kg) |
| --- | --- | --- | --- |
| Body | 2.5 / 6 | 11.25 / 22.5 | 9 / 18 |
| Cockpit | 4 / 8 | 8.1 / 13.75 | 6.5 / 11 |
| Engines | 3 / 7 | 8.75 / 17.5 each | 7 / 14 each |
| Fuel tank | 1.5 / 4 | 5 / 10 | 4 / 8 |
| Armour upgrade, body | +0.625 / +1.5 | +2.25 / +4.5 | +1.8 / +3.6 |

The Harrier's body is 22% of the Hind's under CE (69% outside it). The author's own comment in the CE patch says the first values stopped
every rifle round, so the jet was cut to a light airframe that rifle fire goes through, trading armour for speed (flight speed 16 against
the Hind's 10). The upgrade, though, takes the body from 2.5 to 3.125 mm for 120 plasteel, still under the 6 mm of a 5.56 mm round the
author quotes: 0.0052 mm a plasteel against the Hind's 0.015, about a third of the value.

**What a shot-down Harrier leaves: a fair share, no change proposed.** Every way it dies (cook-off, crash, combat) ends in a kill that
Vehicle Framework refunds at **25% of the cost list** (`RefundMaterialCount`, KillFinalize 0.25, checked), read from the adjusted cost
list, so it follows our new materials. Steel comes out as slag. Fuel, loaded ammunition and fitted upgrades refund 25% too. VTOL Raids
changes nothing here; a raider that flies off the map vanishes and leaves nothing. Expected from our list: about 100 steel, 30 plasteel, 7
or 8 components, the VVE parts at a quarter each, and a 25% chance of the turbine, about 1,974 silver all told (stock list about 1,133; the
Hind about 1,664), plus a quarter of its chemfuel and ammunition. A quarter of the build is the same for every Vehicle Framework vehicle,
so it is no loop.

**Proposals for the owner** (every count authored):

1. **Cheaper CE armour upgrade:** a CE-only patch cutting `Harrier_PlasteelArmour` to about 40 plasteel and 2 components, the Hind's 150
   scaled by 0.625 / 2.25. The base CE armour stays as its author set it, and our README would say rifles go through the jet under CE.
2. **Upgrades outside CE and the wreckage:** no change. The 25% is hard-coded in Vehicle Framework, and the turbine's lumpy 25% chance
   would need C# to smooth.
3. **Never add killed leavings to a vehicle:** Vehicle Framework adds them inside its cost-list loop, so they would drop once per cost
   entry (12 times on our list). Read from the decompile, not seen in play; worth a report to Vehicle Framework's authors if the owner
   wants one.

## Sorties blocked by RimPacts battles, and the parts list (7 Nov 2026, evening)

The owner's reports: the Harrier cannot fly sorties while a RimPacts client state is attacking or in an assault, and the parts list
should be as detailed as other vehicles'. Read from the decompiles (RimPacts 1.7, the fixed-wing framework, Vehicle Framework 1.6);
**not seen in play.** Built in D'thorga Patches 0.11.0.

**The sortie.** A client-state attack opens the target town's own map (RimPacts has no world object of its own for it) and registers
a spectate battle. Until the player presses RimPacts' "Join" on that settlement, `WorldComponent_RimPacts.SpectateLandBlockReason`
is non-null for that map and RimPacts' arrival filter removes every option with a landing step. The fixed-wing framework's postfix on
`LaunchProtocol.GetArrivalOptions` keeps only such options for an open map, so the list is empty and `CompVehicleLauncher.Select`
returns Reject: no text, nothing happens. Our re-added RimPacts postfix carries the same rule. Hostility is not involved: the sortie
path has no hostility check, and the coalition truce only adds visit and trade options the jet drops anyway. A second case the agent
suspected but could not confirm: during a RimPacts world war, a closed enemy town under a ceasefire or declared neutral loses its
"attack" options, which empties the list the same way. **Decisions:** a postfix on `CompVehicleLauncher.CanTarget` turns the empty list
into a failed target with the reason (every refusal says why); a setting lets fixed-wing aircraft skip RimPacts' filter (off by default:
RimPacts counts vehicle crew as the player's forces, so an overflight holds the battle's map and weighs in its end roll, which is what
its gate exists to prevent). The logs neither support nor contradict: RimPacts logs only in its verbose mode, and no error names the jet.

**The parts list.** Vehicle Framework saves a vehicle's parts as healths by position (`VehicleStatHandler.ExposeData`, confirmed in
the owner's Autosave-12: four healths, no keys), so a Def may gain parts only at the end of its list, and a saved vehicle never gets
them unless code adds them. `VehicleUpgrade.Unlock` dereferences `GetComponent(key)` with no null check, so an upgrade naming a part a
saved vehicle lacks throws. The Harrier's own code reads two keys, `Engine` (hover below 40%) and `Body` (cook-off), both configurable
in its extension; its framework reads no part at all, and `FlightControl` is read by nothing in Vehicle Framework 1.6. **Decisions:**
ten parts appended by XML (keys prefixed Phobos, the four originals untouched), the engine given `priorityStatEfficiency` so its
failure still stops the jet now that nozzles and landing gear share its stat averages; a postfix on `VehicleStatHandler.ExposeData`
in PostLoadInit appends missing parts the way `InitializeComponents` makes them; a postfix on `CanLaunchWithCargoCapacity` grounds a
vehicle whose flight control is under 0.35 (authored) when its design has a part carrying that stat; CE armour on the new parts
follows the jet's own CE scale. Health figures: new parts 420 in all beside the original 860 (authored).



Written for the mods' authors.

**For RimPacts (wowgag):**

> Hi! RimPacts' patch on Vehicle Framework's `LaunchProtocol.GetArrivalOptions` breaks any mod that patches the same method after it. `RptVehicleBridge.Install` uses a postfix inside a generic class (`RptVfArrivalFilter<>.MakeGenericType(ArrivalOption)`). Harmony stores patches by method token, which can't hold generic arguments, so when a second patch rebuilds the method it gets the open generic back and throws: `InvalidProgramException ... GetArrivalOptions_Patch2 ... resolves to an open generic type`. It breaks the Harrier VTOL Attack Aircraft mod's flight framework here, on RimWorld 1.7 with Harmony 3.0.0-rc.3. A plain postfix in a non-generic class would fix it, for example in a small assembly that references Vehicle Framework and loads only when it's active. Thanks for RimPacts!

**For the Harrier's framework (potemkinvillager), the map removal:**

> Hi again. `MapParent_ShouldRemoveMapNow_AirborneVtolPatch.TargetMethods` scans `typeof(MapParent).Assembly` only, so a mod's own `ShouldRemoveMapNow` override isn't patched. Real Ruins' three site classes and RimMarket's `ConvoyBattle` return true as soon as `MapPawns.AnyPawnBlockingMapRemoval` is false, and that getter can't see a pilot inside a Vehicle Framework vehicle, so a jet on a sortie over one of those sites can have the map closed under it. Scanning every loaded assembly, or postfixing the getter itself to count a free colonist aboard any spawned player vehicle, would cover them all. Thanks!

**For the Harrier (potemkinvillager):**

> Hi! Lovely jet. On 1.7 with RimPacts active, `FixedWingAircraftFramework.HarmonyInit` throws `InvalidProgramException ... GetArrivalOptions_Patch2 ... resolves to an open generic type`, and `PatchAll` stops there, so everything after `LaunchProtocol_GetArrivalOptions_FixedWingPatch` never loads, including boarding, refuelling and landing. The cause is on RimPacts' side: its postfix on that method is in a generic class, which Harmony can't rebuild. Might this explain Xenogray's report? Applying your patch classes one at a time, each in a try/catch, would limit the damage from any conflict like this. I've reported it to RimPacts' author too.

## The Harrier cannot be taken apart: a trace (0.18.0, 12 Nov 2026)

**Reported by the owner, 10 Nov 2026:** with a colonist, the disassembly runs for a few in-game hours, then stops near the end and the
colonist leaves; in god mode each try logs "Attempted to load a graphic off the main thread: Harrier/Body_B2" and a job fault
(NullReferenceException in `VehiclePawn.GenerateGraphic`), and the jet stays. **The owner chose a diagnostic first** (12 Nov 2026), over
a report only.

**Read again on 12 Nov 2026, with `scripts/query-code.py` and `scripts/query-patches.py`** (a reading of the code, not something seen in
play):

| What | Where | What it shows |
| --- | --- | --- |
| The installed Harrier | `AV8BHarrier.dll`, 9 Nov 2026, decompiled again into `.local/decompiled/AV8BHarrier-dll` (the 3 Nov copy kept beside it) | Its 9 Nov update added chemical bombs, evasion and hit spread; nothing touches removal. The fixed-wing framework's DLL is unchanged since 2 Nov, so our decompile of it is current |
| The fixed-wing framework's removal | `VtolVehiclePawn.Destroy` (`FixedWingAircraftFramework.decompiled.cs:18483`) | Cleans up its flight and renderer, puts the crew out of its inventory, then calls Vehicle Framework's |
| Vehicle Framework's removal | `VehiclePawn.Destroy` (`VehicleFramework-1.6:Vehicles.decompiled.cs:15545`) | Crew out, sounds, the destroyed events, the body's materials released, **the picture cleared** (`graphic = null`), each vehicle comp's `OnDestroy`, then the pawn's own `Destroy` |
| The game's pawn removal | `Pawn.Destroy` (`RimWorld-1.7:Verse.Pawn.cs:2349`) | Logs "unsupported mode" for a disassembly; the owner's logs have no such line, so the removal stopped before it |
| Where the error comes from | The game's `GraphicDatabase.GetInner` | Refuses to load a picture off the main thread and returns nothing, so Vehicle Framework's `GenerateGraphic` then fails on a null |
| Who asks off the main thread | Vehicle Framework's `VehicleRenderer.ParallelGetPreRenderResults` (`:75247`), reached from the fixed-wing framework's `DynamicDrawPhaseAt` | The game's parallel pre-draw asks for the picture on worker threads; with the picture cleared and the jet still on the map, that is the logged error |
| Patches that could hide a failure | VTOL Raids' finalizer and the Hind's prefix and postfix on `VehiclePawn.Destroy`; Vanilla Expanded Framework's `Log_Error_Patch` (`ModSettingsFramework`, decompiled 12 Nov into `.local/decompiled/ModSettingsFramework-1.7`) | None hides one: VTOL Raids' finalizer returns nothing, so the error passes on; the Hind's acts only on a vehicle killed; VEF's switch is on only while it loads its own settings |
| The job | `JobDriver_DisassembleVehicle` (`:1297`) | Its fail checks include the vehicle moving, being forbidden, losing its designation, or no longer able to be taken apart; any could end the colonist's job "near the end" |

**Inference, not yet seen:** something between the picture being cleared and the pawn's own `Destroy` throws: a vehicle comp's
`OnDestroy` (the turrets' clears their auto-target and materials; others are unknown) or a listener on the material pool. The jet then
stays on the map with no picture, and every frame's worker-thread draw tries to rebuild it. The colonist's half may be a fail check
ending the job before the removal, or the same throw.

**Built (agent's design):** `src/PhobosPatches.Harrier/DisassemblyTrace.cs`, temporary, under the setting **Log each step when a vehicle
is taken apart** (on by default). For a vehicle being taken apart only, it logs each stage: the job's finishing step, the fixed-wing
framework's removal (patched by name), Vehicle Framework's removal, the crew out, the materials released (so the destroyed events are
past), each comp's `OnDestroy` (every override found at run time, patched once per type), each turret's, the pawn's own `Destroy`,
the despawn, and every end of a disassembly job with its condition, the work done and what its fail checks read. A failure logs its
error with the last stage reached; the picture fault is logged once per vehicle, saying which thread asked. Prefixes, postfixes and
finalizers only: nothing skipped, nothing swallowed. Its row is 15 in [the register](temporary-patches.md).

**What the owner does:** check 62 in [the test pass](in-game-test-pass.md): one disassembly on a copy of the save, then say done.
**Next:** read the trace with `python scripts/triage-log.py`, then fix, report or patch the failing step under its own register row,
and remove the trace.
