# Building, testing and deploying

**Status (23 Nov 2026): twenty-nine C# projects, RimWorld 1.7 only** (owner, 8 Nov 2026). Every project builds one assembly,
`net10.0` against the 1.7.4998 reference (Harmony 3 where a mod patches), into `mods/<Mod>/1.7/Assemblies/`. A tie-in assembly
builds into `mods/<Mod>/Mods/<Other>/1.7/Assemblies/` instead. Two other mods we reference still ship only a 1.6 build (VFE
Factory and InterRim Ballistic Missile, checked 17 Nov 2026); they are referenced from that folder, which the 1.7 game loads too.
Vehicle Framework shipped its 1.7 build on 14 Nov 2026, and we build against it
([dependency audit](dependency-audit-2026-11-16.md)).
`Directory.Build.targets` at the root turns the target into the game version, the output folder, the game and Harmony
references and the Workshop folders of other mods' assemblies, so a project file only names its mod folder (`ModFolder`), its
tie-in folder if any (`TieInFolder`) and whether it patches (`UsesHarmony`). `python scripts/deploy.py --build --apply --all`
builds every project and installs every mod, with RimWorld closed.

**Shared tools come from a submodule.** [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit) is pinned at
`external/phobos-modkit`; after cloning, run `git submodule update --init` once. `scripts/triage-log.py` uses it. A fix to a
shared tool goes to phobos-modkit first; then the pin moves here with `git -C external/phobos-modkit checkout <tag>` and a
commit. Its Rust programs build with `python scripts/query-save.py --build-indexer` into `%LOCALAPPDATA%\phobos-modkit\target`,
outside this cloud-synced folder, and save indexes stay under the system's temp folder for the same reason: they hold data
from the owner's saves.

## The projects

**The table is generated** from each project file and its C# source by `python scripts/doc-tables.py --write` (phobos-modkit's
doc-tables, configured in `config/doc-tables.json`); the gate fails when it no longer matches the files. Change the project files,
then run it, never edit the table by hand.

<!-- generated: table dotnet-projects -->
Generated from the project files by `python scripts/doc-tables.py --write`: change those files, not this table.

| Project | Builds into (then the game version's Assemblies) | Projects it references | Other assemblies | Harmony | Test projects |
| --- | --- | --- | --- | --- | --- |
| `PhobosFactory` | `mods/PhobosFactoryAdditions/` | `PhobosFramework` | `PipeSystem`, `VanillaFurnitureExpandedFactory` | yes | `PhobosFactory.Rules.Tests` |
| `PhobosFactory.Hygiene` | `mods/PhobosFactoryAdditions/Mods/DubsBadHygiene/` | `PhobosFactory`, `PhobosFramework.Hygiene` | `PipeSystem`, `BadHygiene` | yes | none |
| `PhobosFarming` | `mods/PhobosFarming/` | `PhobosFramework`, `PhobosFramework.Vehicles` | `Vehicles`, `SmashTools`, `CoreLib` | yes | none |
| `PhobosFramework` | `mods/PhobosFramework/` | none | none | yes | `PhobosFramework.Rules.Tests`, `PhobosFramework.Vehicles.Rules.Tests` |
| `PhobosFramework.Hygiene` | `mods/PhobosFramework/Mods/DubsBadHygiene/` | `PhobosFramework` | `BadHygiene` | yes | none |
| `PhobosFramework.Vehicles` | `mods/PhobosFramework/Mods/VehicleFramework/` | `PhobosFramework` | `Vehicles`, `SmashTools`, `SmashTools.PathFinder`, `CoreLib` | yes | `PhobosFramework.Vehicles.Rules.Tests` |
| `PhobosFramework.Vehicles.Dmse` | `mods/PhobosFramework/Mods/VehiclesDmse/` | `PhobosFramework`, `PhobosFramework.Vehicles` | `DMSE`, `Vehicles`, `SmashTools` | yes | none |
| `PhobosFramework.Vehicles.Hygiene` | `mods/PhobosFramework/Mods/VehiclesHygiene/` | `PhobosFramework`, `PhobosFramework.Hygiene`, `PhobosFramework.Vehicles` | `Vehicles`, `BadHygiene` | yes | none |
| `PhobosFramework.Vehicles.Outposts` | `mods/PhobosFramework/Mods/VehiclesOutposts/` | `PhobosFramework`, `PhobosFramework.Vehicles` | `Outposts`, `Vehicles`, `SmashTools` | no | none |
| `PhobosFramework.Vehicles.Pipes` | `mods/PhobosFramework/Mods/VehiclesPipes/` | `PhobosFramework`, `PhobosFramework.Vehicles` | `Vehicles`, `PipeSystem` | no | none |
| `PhobosFramework.Vehicles.Rimefeller` | `mods/PhobosFramework/Mods/VehiclesRimefeller/` | `PhobosFramework`, `PhobosFramework.Vehicles` | `Vehicles`, `SmashTools`, `Rimefeller` | yes | none |
| `PhobosMedical` | `mods/PhobosMedical/` | `PhobosFramework` | `PipeSystem` | yes | `PhobosMedical.Rules.Tests` |
| `PhobosMedical.Hygiene` | `mods/PhobosMedical/Mods/DubsBadHygiene/` | `PhobosMedical`, `PhobosFramework.Hygiene` | `BadHygiene` | yes | none |
| `PhobosMedical.UsefulMarks` | `mods/PhobosMedical/Mods/UsefulMarks/` | `PhobosMedical` | `UsefulColonistBar` | no | none |
| `PhobosPatches.AircraftRearm` | `mods/PhobosPatches/Mods/AircraftRearm/` | `PhobosFramework` | `Vehicles`, `SmashTools`, `CoreLib`, `UpdateLogTool` | yes | none |
| `PhobosPatches.Biotech` | `mods/PhobosPatches/Mods/Biotech/` | `PhobosFramework` | none | yes | none |
| `PhobosPatches.Classical` | `mods/PhobosPatches/Mods/Classical/` | `PhobosFramework` | none | yes | none |
| `PhobosPatches.CloseAirSupport` | `mods/PhobosPatches/Mods/CloseAirSupport/` | `PhobosFramework` | none | no | none |
| `PhobosPatches.CombatExtended` | `mods/PhobosPatches/Mods/CombatExtended/` | `PhobosFramework` | none | yes | `PhobosPatches.Rules.Tests` |
| `PhobosPatches.Harrier` | `mods/PhobosPatches/Mods/HarrierVtol/` | `PhobosFramework`, `PhobosFramework.Vehicles` | `Vehicles`, `SmashTools`, `UpdateLogTool` | yes | none |
| `PhobosPatches.Irbm` | `mods/PhobosPatches/Mods/IRBM/` | `PhobosFramework` | `IRBM` | yes | `PhobosPatches.Rules.Tests` |
| `PhobosPatches.NiceResearchTab` | `mods/PhobosPatches/Mods/NiceResearchTab/` | `PhobosFramework` | none | yes | none |
| `PhobosPatches.Outposts` | `mods/PhobosPatches/Mods/Outposts/` | `PhobosFramework` | `Outposts`, `Vehicles`, `SmashTools` | yes | none |
| `PhobosPatches.Prowler` | `mods/PhobosPatches/Mods/EA6BProwler/` | `PhobosFramework`, `PhobosFramework.Vehicles` | `Vehicles`, `SmashTools` | yes | none |
| `PhobosPatches.RimPacts` | `mods/PhobosPatches/Mods/RimPacts/` | `PhobosFramework` | none | yes | `PhobosPatches.Rules.Tests` |
| `PhobosPatches.VehicleFramework` | `mods/PhobosPatches/Mods/VehicleFramework/` | `PhobosFramework` | `Vehicles`, `SmashTools` | yes | `PhobosPatches.Rules.Tests` |
| `PhobosPatches.VisitSettlements` | `mods/PhobosPatches/Mods/VisitSettlements/` | `PhobosFramework` | none | yes | `PhobosPatches.Rules.Tests` |
| `PhobosRadio` | `mods/PhobosRadioFramework/` | `PhobosFramework` | `PipeSystem` | no | `PhobosRadio.Physics.Tests` |
| `PhobosRadio.CombatExtended` | `mods/PhobosRadioFramework/Mods/CombatExtended/` | `PhobosRadio`, `PhobosFramework` | `CombatExtended` | no | none |
| `PhobosRadio.Vehicles` | `mods/PhobosRadioFramework/Mods/VehicleFramework/` | `PhobosRadio`, `PhobosFramework`, `PhobosFramework.Vehicles` | `Vehicles`, `SmashTools` | no | none |
| `PhobosWaterTap` | `mods/PhobosWaterTap/` | `PhobosFramework.Hygiene` | `BadHygiene` | no | `PhobosWaterTap.Rules.Tests` |

31 projects (23 using Harmony) and 7 test projects. Read from each project file as written: MSBuild is not run, so a value set only in `Directory.Build.props` or `Directory.Build.targets` is not shown. Harmony: its own C# files have `using HarmonyLib` or a `[HarmonyPatch]`. Test projects: those that reference the project or compile its files.
<!-- end generated -->

What the table cannot read from the files, kept by hand:

- **On GitHub** (`.github/workflows/checks.yml`): only `PhobosFramework` builds there, and every test project in the last column
  runs. Every other project references another author's assembly, which cannot be shipped to GitHub (see below).
- **When a tie-in loads** is the mod's `LoadFolders.xml`: the player guide's [requirements table](../player-guide/getting-started.md#what-each-mod-needs)
  lists them. Those waiting for two mods at once: `PhobosFramework.Vehicles.Outposts` and `.Pipes` (Vehicle Framework and Vanilla
  Expanded Framework), `.Hygiene` (and Dubs Bad Hygiene), `.Rimefeller` (and Rimefeller), `.Dmse` (and The Dead Man's Switch -
  Expedition), and `PhobosPatches.Outposts` (Vanilla Expanded Framework and Vehicle Framework). `PhobosPatches.AircraftRearm`
  loads with any one of the Harrier, the Hind, the Mongoose, Close Air Support or the Prowler.
- **Temporary projects**, removed when the other author's fault is fixed: `PhobosPatches.VehicleFramework` (until the framework
  handles vehicle fires itself), `PhobosPatches.CombatExtended` (while an ammo set can hold empty entries), `PhobosPatches.Classical`
  (while Vanilla Factions Expanded - Classical's scorpion raid cannot be loaded from a save) and `PhobosPatches.NiceResearchTab`
  (while research projects' unlock lists can gain a blank entry).
- **Mods reached by name at run time, with no reference**: D'thorga Medical finds More Injuries so, and uses Vanilla Gravship
  Expanded through Defs only; `PhobosPatches.CombatExtended`, `.AircraftRearm`, `.RimPacts` and `.VisitSettlements` find their
  mods' members by name. `PhobosPatches.Classical` and `.NiceResearchTab` patch only the game's own methods. The
  [Harmony patch list](#harmony-patches) says which methods each project patches.

- **D'thorga Medical is one project with a folder per former mod** (`BurnsAndSmoke/`, `Dosing/`, `Triage/`, `Equipment/`, `SaveBridge/`), each keeping its namespace, because saves name classes by namespace (7 November 2026, [why](mod-consolidation.md)).
- **No project's DLL is copied beside another mod's**: `Directory.Build.targets` makes every project reference `Private=false` by default, transitive ones included. Before 7 November 2026 a transitive copy of `PhobosFramework.dll` shipped in Medical Equipment's Dubs Bad Hygiene folder.
- **Other mods' assemblies are referenced from the installed mod and never copied** (`Private=false`): their licences do not
  let us redistribute them (VEF is CC BY-NC-ND 4.0; VFE Factory and Dubs Bad Hygiene publish none). That is also why the
  projects that reference them cannot build on GitHub; their game-free rules still run there.
- **The medical projects reference D'thorga Framework with `ProjectReference ... Private="false"`**, so building one builds the
  framework first and the framework's DLL ships only in its own mod.
- **Build before deploying**: `deploy.py` copies whatever is in the mod folder and says "no assembly built" when a DLL is missing.
  `--build` builds every project, tie-ins included.
- **Rule tests compile only `src/<Project>/Rules/*.cs`** (the physics project its own folder), so they need neither the game nor
  any mod and run anywhere the .NET SDK does.
- Water Tap's texture and preview are generated art the owner picked (7 Nov 2026); `assets/water-tap/draw_tap.py` draws only the tap's layout reference.

### History

- **18 Oct 2026**: the 1.6 setup was proven by a throwaway probe mod (the tables below come from it).
- **19 Oct**: the radio framework's Phase 1 built (0.1.0 Draft). **29 Oct**: Factory Additions' assembly, the first with
  Harmony. **2 Nov**: the Dubs Bad Hygiene tie-in and Vehicle Patches' Harrier tie-in, the first tie-in assemblies. **3 Nov**:
  Medical Save Bridge. **4 Nov**: Water Tap, D'thorga Framework, Burns and Smoke, and the switch to **1.7 leading** with every
  project built for both versions. **5 Nov**: Emergency Dosing and Medical Equipment. **6 Nov**: Triage. **8 Nov**: the owner
  retired 1.6 support; everything new is 1.7 only. The same night: D'thorga Framework's vehicle systems, the first single-target (1.7)
  project. **8 Nov**: every project and mod dropped 1.6 (owner): one target each, no PolySharp, no `1.6` load folders.
- Vehicle Framework targets .NET Framework 4.8 and Vehicle Patches 4.7.2, so that project sets
  `ResolveAssemblyReferenceIgnoreTargetFrameworkAttributeVersionMismatch`; the game loads both whatever they declare.

## Harmony patches

**Every method each project patches, generated** from the C# source by the same `python scripts/doc-tables.py --write`. It reads
the `[HarmonyPatch]` attributes as written, so it lists a patch even when the method it names has gone from the game or the mod:
`python scripts/check-targets.py` and `python scripts/upstream.py` check that. Patches that find their target by name
(`TargetMethod`) and manual `.Patch(` calls are counted, not listed; any attribute form the reader does not know is listed as
"not read" with its file and line.

<!-- generated: table harmony-patches -->
Generated from the projects' C# source by `python scripts/doc-tables.py --write`: change those files, not this table.

| Project | Patched methods | Found by name at run time | Manual `.Patch(` calls | Not read |
| --- | --- | --- | --- | --- |
| `PhobosFactory` | 14 | 0 | 0 | 0 |
| `PhobosFactory.Hygiene` | 4 | 0 | 0 | 0 |
| `PhobosFarming` | 5 | 0 | 0 | 0 |
| `PhobosFramework` | 3 | 0 | 1 | 0 |
| `PhobosFramework.Vehicles` | 119 | 16 | 0 | 0 |
| `PhobosFramework.Vehicles.Dmse` | 3 | 0 | 0 | 0 |
| `PhobosFramework.Vehicles.Hygiene` | 2 | 0 | 0 | 0 |
| `PhobosFramework.Vehicles.Rimefeller` | 1 | 0 | 0 | 0 |
| `PhobosMedical` | 11 | 0 | 4 | 0 |
| `PhobosMedical.Hygiene` | 1 | 0 | 0 | 0 |
| `PhobosPatches.AircraftRearm` | 0 | 0 | 10 | 0 |
| `PhobosPatches.Biotech` | 2 | 0 | 0 | 0 |
| `PhobosPatches.Classical` | 1 | 0 | 0 | 0 |
| `PhobosPatches.CombatExtended` | 1 | 2 | 1 | 0 |
| `PhobosPatches.Harrier` | 13 | 5 | 5 | 0 |
| `PhobosPatches.Irbm` | 6 | 0 | 1 | 0 |
| `PhobosPatches.NiceResearchTab` | 1 | 0 | 0 | 0 |
| `PhobosPatches.Outposts` | 1 | 0 | 0 | 0 |
| `PhobosPatches.Prowler` | 0 | 2 | 0 | 0 |
| `PhobosPatches.RimPacts` | 0 | 0 | 1 | 0 |
| `PhobosPatches.VehicleFramework` | 5 | 0 | 0 | 0 |
| `PhobosPatches.VisitSettlements` | 0 | 0 | 3 | 0 |

193 patched methods named in attributes, 25 patch classes that find their target by name (`TargetMethod`), and 26 manual `.Patch(` calls, whose targets are found at run time and are not listed. Read from the source text, never from a build: a method a patch names but the game or the mod no longer has still shows here.

Every HarmonyPatch attribute was read.

<details><summary><code>PhobosFactory</code>: 14 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosFactory/`) |
| --- | --- | --- |
| `AdvancedProcessorsManager.AddIngredient` | prefix | `Patches/BillRepairPatches.cs` |
| `Building_Conveyor.PeriodicCacheCheck` | postfix | `Patches/PeriodicCacheCheckPatch.cs` |
| `Building_Conveyor.Tick` | prefix | `Patches/ConveyorTickPatch.cs` |
| `Building_Conveyor.TryPullFromDir` | prefix, postfix | `Patches/TryPullFromDirPatch.cs` |
| `CompAdvancedResourceProcessor.PostSpawnSetup` | prefix | `Patches/BillRepairPatches.cs` |
| `CompAdvancedResourceProcessor.ProcessesOptions (getter)` | postfix | `Patches/ProcessesOptionsPatch.cs` |
| `DefGenerator.GenerateImpliedDefs_PreResolve` | postfix | `Patches/DefGeneratorPatch.cs` |
| `Process.CheckInputSlots` | postfix | `Patches/BillRepairPatches.cs` |
| `Process.ExposeData` | postfix | `Patches/BatchInPartsPatches.cs` |
| `Process.InputFactoryHopperDetected` | postfix | `Patches/InputFactoryHopperDetectedPatch.cs` |
| `Process.ResetProcess` | postfix | `Patches/BatchInPartsPatches.cs`, `Patches/BillRepairPatches.cs` |
| `Process.Tick` | prefix | `Patches/BillRepairPatches.cs` |
| `Process.TrySpawnAtCell` | prefix | `Patches/TrySpawnAtCellPatch.cs` |
| `ProcessStack.ExposeData` | postfix | `Patches/BillRepairPatches.cs` |

</details>

<details><summary><code>PhobosFactory.Hygiene</code>: 4 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosFactory.Hygiene/`) |
| --- | --- | --- |
| `FoodPoisonCauseExtension.ToStringHuman` | postfix | `Patches.cs` |
| `Process.ApplyIngredientsAndQuality` | postfix | `Patches.cs` |
| `Process.ResetProcess` | postfix | `Patches.cs` |
| `Process.Tick` | prefix | `Patches.cs` |

</details>

<details><summary><code>PhobosFarming</code>: 5 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosFarming/`) |
| --- | --- | --- |
| `Pawn_RecordsTracker.Increment` | postfix | `HandSeed.cs` |
| `WorkGiver_Grower.ExtraRequirements` | postfix | `PaddockPatches.cs` |
| `WorkGiver_GrowerHarvest.HasJobOnCell` | postfix | `PaddockPatches.cs` |
| `WorkGiver_GrowerSow.JobOnCell` | postfix | `HandSeed.cs` |
| `Zone_Growing.GetGizmos` | postfix | `PaddockPatches.cs` |

</details>

<details><summary><code>PhobosFramework</code>: 3 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosFramework/`) |
| --- | --- | --- |
| `DesignationCategoryDef.ResolvedAllowedDesignators (getter)` | postfix | `UI/ArchitectSubTabs.cs` |
| `MainTabWindow_Architect.CacheDesPanels` | postfix | `UI/ArchitectSubTabs.cs` |
| `Pawn_JobTracker.StartJob` | postfix | `Work/LoopWatch.cs` |

</details>

<details><summary><code>PhobosFramework.Vehicles</code>: 119 patched methods, 16 by name</summary>

| Patched method | Kinds | Where (in `src/PhobosFramework.Vehicles/`) |
| --- | --- | --- |
| `AerialVehicleArrivalModeWorker_CenterDrop.VehicleArrived` | prefix | `Flight/FlightJourneys.cs` |
| `AerialVehicleArrivalModeWorker_EdgeDrop.VehicleArrived` | prefix | `Flight/FlightJourneys.cs` |
| `AerialVehicleArrivalModeWorker_TargetedDrop.VehicleArrived` | prefix | `Flight/FlightJourneys.cs` |
| `AerialVehicleInFlight.InitiateCrashEvent` | prefix | `Sling/SlingPatches.cs` |
| `AerialVehicleInFlight.Vehicles (getter)` | postfix | `Sling/SlingPatches.cs` |
| `Alert_PasteDispenserNeedsHopper.BadDispensers (getter)` | postfix | `Tanks/PasteTapAlertPatch.cs` |
| `ArrivalAction_LandToCell.SpawnSkyfaller` | prefix | `Flight/FlightJourneys.cs` |
| `Building_Trap.CheckSpring` | prefix | `Service/VehicleTraps.cs` |
| `Caravan.NightResting (getter)` | postfix | `Camp/NightDriving.cs` |
| `Caravan_BedsTracker.GetUsableBeds` | postfix | `Camp/Berths.cs` |
| `Caravan_BedsTracker.RecalculateUsedBeds` | postfix | `Camp/Berths.cs` |
| `Caravan_NeedsTracker.IngestDrug` | postfix | `LifeAboard/LifeAboard.cs` |
| `CaravanHelper.ExitMapAndCreateVehicleCaravan` | prefix | `Hitch/CaravanForming.cs` |
| `CaravanInventoryUtility.TryGetBestFood` | postfix | `Hitch/TowedLarder.cs` |
| `CompFueledTravel.CanDraft` | postfix | `Systems/BatteryDrive.cs` |
| `CompFueledTravel.CanMove` | postfix | `Systems/BatteryDrive.cs` |
| `CompFueledTravel.ChargeRate (getter)` | postfix | `Service/Patches.cs` |
| `CompFueledTravel.ShouldConsumeNow (getter)` | postfix | `Systems/BatteryDrive.cs` |
| `CompUpgradeTree.FinishUnlock` | postfix | `Service/UpgradePricing.cs` |
| `CompUpgradeTree.PrerequisitesMet` | postfix | `Service/UpgradeAnyOf.cs`, `Service/UpgradeResearch.cs` |
| `CompUpgradeTree.ResetUnlock` | prefix, finalizer | `Service/UpgradePricing.cs` |
| `CompVehicleLauncher.CanLaunchWithCargoCapacity` | postfix | `Flight/FlightJourneys.cs`, `Service/Patches.cs`, `Sling/SlingPatches.cs` |
| `CompVehicleLauncher.FlightSpeed (getter)` | postfix | `Flight/FlightJourneys.cs`, `Service/CruiseHeightPatches.cs`, `Sling/SlingPatches.cs` |
| `CompVehicleLauncher.FuelConsumptionWorldMultiplier (getter)` | postfix | `Service/CruiseHeightPatches.cs`, `Sling/SlingPatches.cs` |
| `CompVehicleLauncher.Launch` | prefix | `Flight/FlightJourneys.cs` |
| `DaysUntilRotCalculator.ApproxDaysUntilRot` | postfix | `Hitch/TowedLarder.cs` |
| `DaysWorthOfFoodCalculator.ApproxDaysWorthOfFood` | postfix | `Hitch/TowedLarder.cs` |
| `DeepResourceGrid.AnyActiveDeepScannersOnMap` | postfix | `Service/Patches.cs` |
| `Dialog_FormCaravan.DaysWorthOfFood (getter)` | prefix, postfix | `Hitch/TowedLarder.cs` |
| `Dialog_FormCaravan.DoWindowContents` | prefix | `Hitch/CaravanForming.cs` |
| `Explosion.StartExplosion` | postfix | `Service/GlassBlastPatch.cs` |
| `Ext_Vehicles.TryDropAllOutsideVehicle` | prefix | `Flight/FlightPatches.cs` |
| `Ext_Vehicles.TryDropOutsideVehicle` | prefix | `Flight/FlightPatches.cs` |
| `FactionDialogMaker.FactionDialogFor` | postfix | `Airdrop/SupplyDrops.cs` |
| `FireUtility.TryAttachFire` | prefix, postfix | `Flight/FlightPatches.cs`, `Service/FireFighting.cs` |
| `FlightPath.ConsumeNode` | prefix | `Service/LandingCheck.cs` |
| `FuelTargetUpdater.LaunchStatus` | postfix | `Service/FlightCargoReachPatches.cs` |
| `FuelTargetUpdater.TargeterUpdate` | postfix | `Service/CruiseHeightPatches.cs` |
| `FuelTargetUpdater.ValidateTargets` | postfix | `Service/CruiseHeightPatches.cs`, `Service/FlightCargoReachPatches.cs`, `Service/LandingCheck.cs` |
| `Game.DeinitAndRemoveMap` | prefix | `Flight/FlightSupport.cs` |
| `GenLeaving.DoLeavingsFor` | prefix | `Service/UnderlayRefunds.cs` |
| `GenRecipe.MakeRecipeProducts` | postfix | `SetOut/PasteBake.cs` |
| `GravshipPlacementUtility.PlaceGravshipInMap` | postfix | `Service/GravshipLandingAlignment.cs` |
| `GravshipUtility.PreLaunchConfirmation` | postfix | `Service/GravshipLaunchWarning.cs` |
| `ITab_Vehicle_Upgrades.DrawButtons` | prefix | `Service/UpgradeResearch.cs` |
| `ITab_Vehicle_Upgrades.DrawGrid` | prefix | `Presentation/UpgradeTabScroll.cs` |
| `ITab_Vehicle_Upgrades.DrawPrerequisites` | postfix | `Service/UpgradeAnyOf.cs` |
| `JobDriver_RepairVehicle.WorkComplete` | postfix | `Service/Patches.cs` |
| `JobGiver_GetFood.TryGiveJob` | postfix | `Visits/VisitReasons.cs` |
| `JobGiver_GetRest.TryGiveJob` | postfix | `Cabins/DeckTents.cs`, `Camp/BunkSleep.cs`, `Visits/VisitReasons.cs` |
| `JobGiver_SeekSafeTemperature.TryGiveJob` | postfix | `Visits/VisitReasons.cs` |
| `LaunchProtocol.GetArrivalOptions` | postfix | `Airdrop/AirdropArrival.cs` |
| `LordJob_FormAndSendVehicles.DetermineLeadVehicle` | postfix | `Hitch/CaravanForming.cs` |
| `MainTabWindow_Research.PostOpen` | postfix | `Service/SetAsideResearchPatches.cs` |
| `MapTemperature.OutdoorTemp (getter)` | postfix | `Cabins/DeckTents.cs` |
| `MechanitorBandwidthGizmo.GizmoOnGUI` | prefix, finalizer | `Remote/MechLink.cs` |
| `Messages.Message` | prefix | `Service/VehicleMessageLevel.cs` |
| `Need_Food.FoodFallPerTickAssumingCategory` | postfix | `Camp/CrewMachines.cs` |
| `Pawn.GetChildHolders` | postfix | `Sling/SlingPatches.cs` |
| `Pawn.ThreatDisabled` | postfix | `Service/SubmersiblePatches.cs` |
| `Pawn_MechanitorTracker.UsedBandwidth (getter)` | postfix | `Remote/MechLink.cs` |
| `Pawn_Ownership.ClaimBedIfNonMedical` | prefix | `Cabins/DeckTents.cs` |
| `Projectile.Launch` | prefix | `Service/AirborneEvasion.cs` |
| `ResearchManager.IsCurrentProject` | postfix | `Service/FieldResearchPatches.cs` |
| `ResearchManager.SetCurrentProject` | prefix | `Service/FieldResearchPatches.cs` |
| `ResearchManager.StopProject` | postfix | `Service/FieldResearchPatches.cs` |
| `RoleHelper.GetHandlerFor` | postfix | `Visits/VisitSeats.cs` |
| `ShortCircuitUtility.DoShortCircuit` | prefix | `Service/ShortCircuits.cs` |
| `TendUtility.CalculateBaseTendQuality` | postfix | `Camp/CrewMachines.cs` |
| `ThingOwnerUtility.TryGetFixedTemperature` | postfix | `Service/Patches.cs` |
| `Toils_LayDown.ApplyBedThoughts` | postfix | `Camp/CampJobs.cs` |
| `TransferableVehicleWidget.CanCaravan` | postfix | `Hitch/CaravanForming.cs` |
| `TurretShotReport.HitReportFor` | postfix | `Service/TurretPacePatches.cs` |
| `VehicleCaravan.Notify_MemberDied` | prefix | `Service/LostConvoys.cs` |
| `VehicleCaravanPathingHelper.EstimatedTicksToArrive` | prefix, postfix, finalizer | `Camp/NightDriving.cs` |
| `VehicleCaravanTicksPerMoveUtility.GetTicksPerMove` | postfix | `Service/Patches.cs` |
| `VehicleComponent.ApplyEMPDamage` | prefix, postfix, finalizer | `Service/VehicleEmp.cs` |
| `VehicleDef.ResolveReferences` | prefix | `Service/Patches.cs` |
| `VehicleDrawTracker.DrawPos (getter)` | postfix | `Flight/FlightPatches.cs` |
| `VehicleIgnitionController.Drafted (setter)` | prefix | `Hitch/CaravanForming.cs` |
| `VehiclePathFollower.CostToPayThisTick` | postfix | `Service/Patches.cs` |
| `VehiclePathFollower.PatherArrived` | prefix, postfix | `Service/BrakeOverrun.cs` |
| `VehiclePawn.AddOrTransfer` | prefix | `Flight/FlightPatches.cs` |
| `VehiclePawn.CanMoveFinal (getter)` | postfix | `Service/Patches.cs` |
| `VehiclePawn.Destroy` | prefix, finalizer | `Flight/FlightPatches.cs` |
| `VehiclePawn.DisembarkPawn` | prefix | `Service/SubmersiblePatches.cs` |
| `VehiclePawn.DynamicDrawPhaseAt` | prefix | `Service/SubmersiblePatches.cs` |
| `VehiclePawn.GetGizmos` | postfix | `Jobs/ClaimVehicle.cs` |
| `VehiclePawn.HasEnoughOperators (getter)` | postfix | `Remote/RemoteControl.cs` |
| `VehiclePawn.Kill` | prefix, postfix | `Service/VehicleHulls.cs`, `Service/VehiclePractice.cs` |
| `VehiclePawn.RecacheCompTickers` | postfix | `Flight/FlightPatches.cs` |
| `VehiclePawn.SeatsAvailable (getter)` | postfix | `Visits/VisitSeats.cs` |
| `VehiclePawn.TryAddPawn` | prefix | `Flight/FlightPatches.cs`, `Visits/VisitSeats.cs` |
| `VehicleReservationManager.VehicleListers` | postfix | `Flight/FlightPatches.cs` |
| `VehicleRoleHandler.AreSlotsAvailable (getter)` | postfix | `Cabins/CabinSharedSeats.cs` |
| `VehicleSkyfaller_Arriving.DrawAt` | postfix | `Sling/SlingPatches.cs` |
| `VehicleSkyfaller_Leaving.DrawAt` | postfix | `Sling/SlingPatches.cs` |
| `VehicleStatHandler.ApplyDamage` | prefix, postfix | `Service/SubmersiblePatches.cs`, `Service/VehicleEmp.cs` |
| `VehicleStatHandler.ComponentsPrioritized (getter)` | postfix | `Service/Patches.cs` |
| `VehicleStatHandler.ExposeData` | prefix, postfix | `Service/Patches.cs` |
| `VehicleStatHandler.NeedsRepairs (getter)` | postfix | `Service/Patches.cs` |
| `VehicleStatHandler.RecalculateHealthPercent` | postfix | `Service/Patches.cs` |
| `VehicleStatWorker.TransformValue` | postfix | `Service/Patches.cs` |
| `VehicleTurret.FireTurret` | prefix, finalizer | `Service/AimScatterPatches.cs` |
| `VehicleTurret.FireTurretCE` | prefix, finalizer | `Service/TurretPacePatches.cs` |
| `VehicleTurret.IsDisabled` | postfix | `Service/TurretPacePatches.cs` |
| `VehicleTurret.MaxTicks (getter)` | postfix | `Service/TurretPacePatches.cs` |
| `VehicleTurret.RecacheMannedStatus` | postfix | `Remote/RemoteGuns.cs` |
| `VehicleTurret.TurretDisabled (getter)` | postfix | `Service/SubmersiblePatches.cs`, `Service/TurretPacePatches.cs` |
| `VehicleTurret.TurretRotationTick` | prefix, finalizer | `Service/TurretPacePatches.cs` |
| `VehicleTurret.WarmupTicks (getter)` | postfix | `Service/TurretPacePatches.cs` |
| `VehicleTweener.TweenedPosRoot` | postfix | `Hitch/HitchPatches.cs` |
| `VehicleWorkGiver.JobOnThing` | prefix | `Flight/FlightPatches.cs` |
| `WorkGiver_DisassembleVehicle.HasJobOnThing` | postfix | `Flight/FlightPatches.cs` |
| `WorkGiver_RepairVehicle.CanBeWorkedOn` | postfix | `Service/Patches.cs` |
| `WorldObject.IncidentTargetTags` | postfix | `Service/SubmersiblePatches.cs` |
| `WorldPawns.PassToWorld` | prefix | `Hitch/HitchPatches.cs` |
| `WorldPawns.ShouldMothball` | postfix | `Service/Patches.cs` |
| `Zone_Stockpile.GetGizmos` | postfix | `Service/VehicleUnloadBays.cs` |
| by name in TargetMethod | postfix | `Camp/NightDriving.cs` |
| by name in TargetMethod | prefix | `Flight/FlightPatches.cs` |
| by name in TargetMethod | postfix | `Service/AirborneEvasion.cs` |
| by name in TargetMethod | postfix | `Service/GlassBlastPatch.cs` |
| by name in TargetMethod | postfix | `Service/SetAsideResearchPatches.cs` |
| by name in TargetMethod | prefix | `Service/TurretPacePatches.cs` |
| by name in TargetMethod | prefix | `Service/VehicleTraps.cs` |
| by name in TargetMethod (7 classes) | postfix | `Service/VehiclesAtWorkPatches.cs` |
| by name in TargetMethods (2 classes) | prefix, finalizer | `Service/AimScatterPatches.cs` |

</details>

<details><summary><code>PhobosFramework.Vehicles.Dmse</code>: 3 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosFramework.Vehicles.Dmse/`) |
| --- | --- | --- |
| `AerialVehicleInFlight.Launch` | prefix | `AirDefenceTieIn.cs` |
| `CompVehicleLauncher.Launch` | prefix | `AirDefenceTieIn.cs` |
| `VehicleCaravan.Launch` | prefix | `AirDefenceTieIn.cs` |

</details>

<details><summary><code>PhobosFramework.Vehicles.Hygiene</code>: 2 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosFramework.Vehicles.Hygiene/`) |
| --- | --- | --- |
| `Caravan_NeedsTracker.TrySatisfyPawnNeeds` | prefix | `CaravanWaterAboard.cs` |
| `JobGiver_DrinkWater.TryGiveJob` | postfix | `VisitDrink.cs` |

</details>

<details><summary><code>PhobosFramework.Vehicles.Rimefeller</code>: 1 patched method</summary>

| Patched method | Kinds | Where (in `src/PhobosFramework.Vehicles.Rimefeller/`) |
| --- | --- | --- |
| `PlaceWorker_DeepWellHead.HasScanner` | postfix | `RigJob_OilWell.cs` |

</details>

<details><summary><code>PhobosMedical</code>: 11 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosMedical/`) |
| --- | --- | --- |
| `Bill_Medical.ShouldDoNow` | postfix | `Equipment/Patches/MedicalPatches.cs` |
| `CompResourceStorage.AmountCanAccept (getter)` | postfix | `Equipment/Patches/MedicalPatches.cs` |
| `ForbidUtility.InAllowedArea` | postfix | `Protection/ProtectionPart.cs` |
| `Game.FinalizeInit` | postfix | `Equipment/Patches/MedicalPatches.cs` |
| `GenTemperature.TryGetTemperatureForCell` | postfix | `BurnsAndSmoke/Heat/FireHeatPatch.cs` |
| `HealthCardUtility.DrawOverviewTab` | prefix, finalizer | `HealthTabChoices.cs` |
| `MedicalCareUtility.MedicalCareSelectButton` | postfix | `HealthTabChoices.cs` |
| `Pawn_PathFollower.TryEnterNextPathCell` | prefix, postfix | `Protection/ProtectionPart.cs` |
| `PawnUIOverlay.DrawPawnGUIOverlay` | postfix | `Triage/UI/TriageTagOverlay.cs` |
| `PregnancyUtility.ApplyBirthOutcome` | postfix | `Radiation/Patches/BirthPatch.cs` |
| `TendUtility.CalculateBaseTendQuality` | postfix | `Dosing/Patches/FussingPatch.cs` |

</details>

<details><summary><code>PhobosMedical.Hygiene</code>: 1 patched method</summary>

| Patched method | Kinds | Where (in `src/PhobosMedical.Hygiene/`) |
| --- | --- | --- |
| `Building_WashingMachine.UnloadWashing` | prefix | `DbhWashing.cs` |

</details>

<details><summary><code>PhobosPatches.Biotech</code>: 2 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.Biotech/`) |
| --- | --- | --- |
| `Building_MechCharger.GetInspectString` | postfix | `RechargerRealPower.cs` |
| `Building_MechCharger.Tick` | prefix, postfix | `RechargerRealPower.cs` |

</details>

<details><summary><code>PhobosPatches.Classical</code>: 1 patched method</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.Classical/`) |
| --- | --- | --- |
| `Lord.ExposeData` | postfix | `RaidOrdersOnLoad.cs` |

</details>

<details><summary><code>PhobosPatches.CombatExtended</code>: 1 patched method, 2 by name</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.CombatExtended/`) |
| --- | --- | --- |
| `Pawn_EquipmentTracker.DropAllEquipment` | postfix | `KeepGuns/DroppedWhenDownedPatch.cs` |
| by name in TargetMethod | prefix | `Airdrop/ShotDownParachutes.cs` |
| by name in TargetMethod | postfix | `KeepGuns/SidearmLoopGuard.cs` |

</details>

<details><summary><code>PhobosPatches.Harrier</code>: 13 patched methods, 5 by name</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.Harrier/`) |
| --- | --- | --- |
| `BackCompatibility.BackCompatibleDefName` | postfix | `BombCaseRename.cs` |
| `CompVehicleLauncher.CanTarget` | postfix | `SortieGate.cs` |
| `JobDriver.Cleanup` | prefix | `DisassemblyTrace.cs` |
| `JobDriver_DisassembleVehicle.FinishedRemoving` | prefix, finalizer | `DisassemblyTrace.cs` |
| `LoadIDsWantedBank.RegisterLoadIDListReadFromXml` | prefix | `BombCaseRename.cs` |
| `LoadIDsWantedBank.RegisterLoadIDReadFromXml` | prefix | `BombCaseRename.cs` |
| `Pawn.Destroy` | prefix, finalizer | `DisassemblyTrace.cs` |
| `RGBMaterialPool.Release` | prefix | `DisassemblyTrace.cs` |
| `VehiclePawn.DeSpawn` | finalizer | `DisassemblyTrace.cs` |
| `VehiclePawn.Destroy` | prefix, finalizer | `DisassemblyTrace.cs` |
| `VehiclePawn.DisembarkAll` | postfix | `DisassemblyTrace.cs` |
| `VehiclePawn.GenerateGraphic` | finalizer | `DisassemblyTrace.cs` |
| `VehicleTurret.OnDestroy` | finalizer | `DisassemblyTrace.cs` |
| by name in TargetMethod | postfix | `HarrierEvasion.cs` |
| by name in TargetMethod (4 classes) | prefix, postfix | `HoverGate.cs` |

</details>

<details><summary><code>PhobosPatches.Irbm</code>: 6 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.Irbm/`) |
| --- | --- | --- |
| `Building_CIWS.FindBestTarget` | postfix | `Patches/CiwsAircraftPatches.cs`, `Patches/CiwsShellsPatches.cs` |
| `Building_CIWS.InterceptionSuccess` | prefix, postfix | `Patches/CiwsAircraftPatches.cs`, `Patches/CiwsShellsPatches.cs` |
| `Building_CIWS.IsTargetValid` | postfix | `Patches/CiwsAircraftPatches.cs`, `Patches/CiwsShellsPatches.cs` |
| `FogGrid.FloodUnfogAdjacent` | prefix | `Patches/QuietRevealPatches.cs` |
| `StrategicImpactCountdown.ApplyStrategicDamage` | prefix, finalizer | `Patches/QuietRevealPatches.cs`, `Patches/StrategicDamagePatches.cs` |
| `StrategicImpactCountdown.OnWhiteScreenReached` | prefix | `Patches/WhiteScreenPatch.cs` |

</details>

<details><summary><code>PhobosPatches.NiceResearchTab</code>: 1 patched method</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.NiceResearchTab/`) |
| --- | --- | --- |
| `ResearchProjectDef.UnlockedDefs (getter)` | prefix, postfix, finalizer | `ResearchUnlocksGuard.cs` |

</details>

<details><summary><code>PhobosPatches.Outposts</code>: 1 patched method</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.Outposts/`) |
| --- | --- | --- |
| `Dialog_CreateCamp (constructor)` | postfix | `ConvoyOutpostRefusal.cs` |

</details>

<details><summary><code>PhobosPatches.Prowler</code>: 0 patched methods, 2 by name</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.Prowler/`) |
| --- | --- | --- |
| by name in TargetMethod (2 classes) | postfix | `ProwlerPatches.cs` |

</details>

<details><summary><code>PhobosPatches.VehicleFramework</code>: 5 patched methods</summary>

| Patched method | Kinds | Where (in `src/PhobosPatches.VehicleFramework/`) |
| --- | --- | --- |
| `CompVehicleTurrets.PostExposeData` | prefix, finalizer | `TurretQueueSave.cs` |
| `JobDriver_WorkVehicle.MakeNewToils` | postfix | `VehicleFrameworkPatchesMod.cs` |
| `VehiclePawn.DeSpawn` | prefix | `VehicleFrameworkPatchesMod.cs` |
| `VehiclePawn.TickRare` | postfix | `VehicleFrameworkPatchesMod.cs` |
| `VehicleTurret.ResolveGraphics` | prefix | `TurretGraphicWhileLoading.cs` |

</details>
<!-- end generated -->

## The checks (run before every commit)

**Before every commit, run `python scripts/check-all.py` and chain the commit on its exit code**
(`python scripts/check-all.py && git commit ...`), as AGENTS.md requires. It is phobos-modkit's gate: it builds every project, runs
every rule-test project and the Python tests, and checks links, mod layout, text keys, vehicle schemas, the research tree, version
copies and changelogs, printing only what failed. Its steps are in `config/gate.json`, found by glob, so a new project is never left
out. `--only` and `--skip` narrow it while iterating. The commands below are some of its steps, for running one alone:

```text
python scripts/check-doc-links.py
python scripts/check-mod-layout.py
python -m unittest discover -s tests
dotnet test tests/<Project>             each rule-test project whose rules you changed
python scripts/check-factory-patches.py (needs the installed game; for Factory Additions and its tie-ins)
python scripts/check-vehicle-schemas.py (offline; for the vehicle systems' systems and loadouts)
python scripts/check-upgrade-trees.py   (offline; our upgrade trees and their diagrams; show prints them, --write redraws)
python scripts/deploy.py --build --apply --all   (with RimWorld closed)
```

`check-factory-patches` and `deploy` are not part of the gate. **On GitHub** (`.github/workflows/checks.yml`) every push runs
the gate without the mod builds (`check-all.py --skip "build *"`), so every rule-test project and every check that needs no other
mod, and builds D'thorga Framework's core for 1.7. `check-factory-patches` and `deploy` need the installed
game and run only on a machine with RimWorld.

- **check-doc-links** finds relative Markdown links whose target file is missing. It does not check `#heading` fragments.
- **check-mod-layout** checks every `mods/<Mod>/` folder:
  - `About/About.xml` has the required fields (including an author and `supportedVersions`), a `packageId` of the form
    `PhobosDthorga.<ModName>`, and **Ludeon's EULA disclaimer word for word** in the description;
  - package IDs are unique, and `modVersion` is MAJOR.MINOR.PATCH;
  - a `README.md` and `CHANGELOG.md` exist;
  - every folder `LoadFolders.xml` names exists and is not empty;
  - no two folders in one version block hold a Defs or Patches file at the same path (the game would load only the one in the
    folder listed last);
  - no Keyed string uses a numbered placeholder such as `{0}`: name it, as in `{COUNT}`;
  - no build products (DLLs, `bin/`, `obj/`) are present.
- **check-factory-patches** reads the installed game, the active mods (ModsConfig.xml) and the Workshop folder, never writing to
  them, and checks D'thorga Factory Additions (or `--mod <Name>`) against them: no file is skipped for sharing a path with a
  later folder's file, every patch xpath matches one node, every Def name a ProcessDef uses exists, every hopper-only bill has an
  output cap within its product's stack limit, defined bills match the bills patched onto machines, every ParentName resolves,
  and every bench bill template names a machine, benches, research, sound and effect that exist. Paths as for deploy.py
  (`--game`, `--workshop`, `--config`, the environment variables or `.local/install-settings.json`). An offline check is not an
  in-game test.
- **The unit tests** (`tests/test_*.py`) exercise every script in `scripts/` and `scripts/art/` against synthetic folders.

### Other scripts

The scripts below are some of the tools. **The full set, the order to use them in and their gaps are in
[analysis-tooling.md](analysis-tooling.md)**; AGENTS.md says which one answers which question (saves, logs, Defs, code, Harmony
patches, start-up time, settings, crash dumps, `check-targets.py`, `cleanup.py`).

| Script | What it does | Flags |
| --- | --- | --- |
| `scripts/triage-log.py` | Summarises `Player.log` against `Player-prev.log`: messages grouped into signatures, one exception worded several ways shown together, the mods involved named, and notices for the save loaded and the log limit. Reads only; works while the game runs. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-log tool | `--show ID`, `--previous`, `--no-previous`, `--top`, `--levels`, `--json`, `--logs`, `--game`, `--workshop`, `--no-mods`, `--no-redact` |
| `scripts/workshop-research.py` | What players report about mods on the Workshop lately: `report <mods>` (update date, declared versions, recent comments, the uploader's last reply, the newest comments that mention problems), `versions` (active Workshop mods not declaring the game's version), `details`, `comments`, `sql`. Reads Steam's public pages only, paced 8 s apart for comments and cached for 24 hours. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-workshop tool | report name first, then mods by name, package ID or Workshop ID; `--max-age`, `--refresh`, `--offline`, `--days`, `--top`, `--json` |
| `scripts/query-defs.py` | Which active mods define and patch each Def, from an index of the installed game: `summary`, `who <defName>` (every definition, the one the game keeps, every patch naming it), `show` (XML and parents), `list`, `patches`, `mods`, `sql`. Indexes about 340 mods in about 8 s and rebuilds itself when the mod list, a Workshop item, the game or a local mod changes; reports then take about 0.3 s. Defs are shown as written, without other mods' patches. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-defs tool | report name first, then `--top`, `--json`, `--rebuild`, the report's filters (`--help` on each report) |
| `scripts/query-save.py` | Answers questions about a save from an index: `summary`, `pawns`, `hediffs`, `things`, `find <ID>`, `grid` (terrain, roofs, gas, fog, snow, pollution and deep resources per map, a text picture, or one cell), `world` (settlements, sites and caravans by faction; `--id` for one object's values, such as an aircraft's flight path), `components` (the game's, world's and maps' components, often mods' saved state, value by value), `diff <other save>` (with `--component` to compare one mod's state), `schema`, `sql`. The first question indexes a frozen copy of the save (about 2.5 s for 300 MB), deleted straight afterwards; later ones reuse the index until the save changes. Build its Rust indexer once with `--build-indexer`. Calls [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit)'s rimworld-save tool | report name first, then `--save`, `--top`, `--json`, the report's filters (`--help` on each report) |
| `scripts/refresh-mod-list.py` | Rewrites [the owner's mod list](owner-mod-list.md) from the game's files, reading only | `--online` (re-read the Vanilla Expanded collection), `--check` (report, write nothing), `--game`, `--workshop`, `--config`, `--doc` |
| `scripts/generate-art.py` | Runs a texture request through the local ComfyUI ([artwork direction](image-routes-research.md#artwork-direction)) | `request_file`, `--only`, `--start`, `--comfy` |
| `scripts/art-run.py` | Generates, trains or runs an overnight art batch, stopping safely | `generate`, `train` or `night`, then `source`; `--stop-at`, `--only`, `--comfy`, `--min-free-gb`, `--also-stop-on`, `--near-copy-against`, `--dry-run` |
| `scripts/art/` | The review tools: contact sheets, sprite export, the review page, near-copy checks, model fetching | see each script's `--help` |

## Where things go

```text
mods/<Mod>/                 the mod exactly as players get it, minus built DLLs
  About/About.xml           identity, dependencies, supported versions, modVersion
  About/Preview.png         mod list and Workshop image
  About/PublishedFileId.txt written by the game at first upload; committed then
  LoadFolders.xml           which folders load on 1.7, and for optional mods
  Common/Defs, Patches, Languages, Textures, Sounds
  1.7/Assemblies/           built from src/ against the 1.7 reference, never committed
  Mods/<OtherMod>/          optional tie-in content, loaded only when that mod is active
  Mods/<OtherMod>/1.7/       a tie-in's own assembly
  README.md, CHANGELOG.md
src/<Project>/              C# source, building into mods/<Mod>/1.7/Assemblies/
workshop/<Mod>/             Workshop page drafts and release notes, once publishing starts
```

The `Mods/<OtherMod>/` folder name is a house convention, not a game rule. **Name every file in it after
the folder** (`Patches/MedicineGranulator_MedicinesPlus.xml`): files are keyed by their path inside a load
folder, so two `Patches/MedicineGranulator.xml` in different folders means only one loads.

A starting `LoadFolders.xml`:

```xml
<loadFolders>
  <v1.7>
    <li>/</li>
    <li>Common</li>
    <li>1.7</li>
    <li IfModActive="CETeam.CombatExtended">Mods/CombatExtended</li>
  </v1.7>
</loadFolders>
```

A tie-in with its own assembly lists both its content folder and its version folder
(`Mods/HarrierVtol` and `Mods/HarrierVtol/1.7`); `check-mod-layout` treats any folder whose last part is a game version
as a build-product folder that need not be committed.

Check each optional mod's real `packageId` in its `About.xml` before using it. Combat Extended's,
`CETeam.CombatExtended`, was read from its installed `About.xml` (Workshop item 2890901044) on 18 Oct 2026.

## The C# toolchain

**The same code builds to the same bytes** (19 Nov 2026). The .NET SDK stamps the git commit into an assembly's informational
version unless told not to (`IncludeSourceRevisionInInformationalVersion`), so every commit anywhere changed every rebuilt
assembly by about 100 bytes, and D'thorga Companion's Mods page read unchanged mods as changed (owner's report). It is off; the
install record (`InstallRecord.json`) keeps the commit instead. Checked: Water Tap built at two commits came out byte for byte the
same. An assembly still changes when its own code does, or when code it uses changes how it compiles.

| | RimWorld 1.7 (the only version, owner, 8 Nov 2026) |
| --- | --- |
| Target | `net10.0` |
| Game references | `Krafs.Rimworld.Ref` 1.7.4998-unstable (matches the owner's game build) |
| Harmony | `Lib.Harmony` 3.0.0-rc.4, compile-time only |
| Output | `mods/<Mod>/1.7/Assemblies/` |
| Tools | .NET SDK 10 builds it; Visual Studio 2026 or Rider 2026 to edit |

The sources and dates are in the [RimWorld modding reference](rimworld-modding-reference.md#building-c-mods).
`Directory.Build.props` sets the shared options (the latest C# language version, nullable references, deterministic builds,
no git commit in the assembly's version, warnings as errors, no `.deps.json`), and `Directory.Build.targets` everything that
depends on the target: `GameVersion` (1.7), the output folder
`mods/$(ModFolder)/$(TieInFolder)$(GameVersion)/Assemblies/` (set as `OutDir`, `TargetDir` and `TargetPath`, because the SDK
has already derived those from `OutputPath` by the time the file is imported), the Krafs reference, Harmony
3 when a project sets `UsesHarmony`, and the Workshop folders `VefAssemblies`, `DbhAssemblies`, `RimefellerAssemblies` and
`UsefulMarksAssemblies` (these mods ship 1.7 assemblies), `VfeFactoryAssemblies`, `VehicleFrameworkAssemblies` and `IrbmAssemblies` (each takes the
mod's 1.7 folder when it exists and its 1.6 folder only until then; Vehicle Framework shipped 1.7 on 14 Nov 2026, while VFE Factory
and IRBM still ship only 1.6, and the game loads those on 1.7). Override any of them with `-p:Name=...` or an environment
variable, or `WorkshopRoot` for all of them.

A project file that references another mod (D'thorga Radio Framework's):

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <RootNamespace>Phobos.Radio</RootNamespace>
    <AssemblyName>PhobosRadio</AssemblyName>
    <ModFolder>PhobosRadioFramework</ModFolder>
    <Version>0.1.1</Version>
  </PropertyGroup>
  <ItemGroup>
    <Reference Include="PipeSystem">
      <HintPath>$(VefAssemblies)\PipeSystem.dll</HintPath>
      <Private>false</Private>
    </Reference>
  </ItemGroup>
</Project>
```

`Private=false` keeps `PipeSystem.dll` out of our `Assemblies` folder: players get it from VEF, and its
licence forbids us shipping a copy. Set `VefAssemblies` (as an environment variable or `-p:VefAssemblies=…`)
when VEF lives elsewhere. Harmony is not referenced until a feature needs a patch; `UsesHarmony` then adds it with
`ExcludeAssets="runtime"`, so `0Harmony.dll` stays out too. A medical mod references the framework instead:

```xml
<ItemGroup>
  <ProjectReference Include="../PhobosFramework/PhobosFramework.csproj" Private="false" />
</ItemGroup>
```

One project, one target, the conditions in `Directory.Build.targets`. A project
that patches adds `<UsesHarmony>true</UsesHarmony>`; a tie-in assembly adds `<TieInFolder>Mods/HarrierVtol/</TieInFolder>`.
Harmony 3.0.0-rc.4 accepted every existing patch at build time; its nullable annotations are stricter than Harmony 2's, which
surfaced one unchecked dereference in Vehicle Patches.

## RimWorld 1.6, retired

From 18 Oct to 8 Nov 2026 every project also built `net472` against the 1.6 reference, with Harmony 2 and PolySharp (Sergio
Pedri's MIT source generator) for the C# features Unity's .NET Framework 4.7.2 libraries lack (`record`, `init`, `required`,
`[MemberNotNull]`). The owner retired 1.6 on 8 Nov 2026 and every mod dropped it on 8 Nov 2026. The git history before then has
the two-target setup, should it ever be wanted again.

## Building on Linux

The build needs only two packages: Krafs and Lib.Harmony. Because Krafs brings the whole reference set, the .NET 10 SDK compiles
every project on Linux with no Mono and no Windows targeting pack. **GitHub's `ubuntu-latest` runner
proves it on every push** for the projects that reference no other mod's assembly (the table above). Code is exercised in the game.

## Deploying to the game

`python scripts/deploy.py [--build] [--apply] <Mod>...` or `--all` installs mods. It:

- previews by default, and copies only with `--apply`;
- with `--all` takes every folder under `mods/` that has an `About/About.xml`; with `--build` (whether mods are named or `--all`)
  runs `dotnet build` on every project under `src/` first, stopping at the first failure before anything
  is copied;
- refuses while RimWorld is running, found by its process name (`RimWorld.exe` for 1.7 on Windows, `RimWorldWin64.exe` and
  `RimWorldWin.exe` for 1.6, and the Linux and Mac names);
- with `--apply`, first renames every named mod's installed folder aside and straight back, so a folder something holds open
  stops the whole run before **any** mod changes, and the refusal names any File Explorer window open inside it (otherwise it
  names the usual holders: a terminal or editor open in it, RimSort). **On 8 Nov 2026 an Explorer window open in
  `PhobosVehicles\Mods\TrainsOfTheRim\Defs` stopped every install at D'thorga Vehicles**, after earlier mods had updated and
  before D'thorga Water Tap; Windows reports an open folder as "Access is denied", not "in use";
- moves the previous install to `.local/deploy-backups/`, renaming it on the game's own drive first: Windows
  refuses that rename while the game holds any file in it open, so an unrecognised running game still stops it
  with nothing changed;
- copies `mods/<Mod>/` and its built assemblies into `<game>\Mods\<Mod>\`;
- **encodes each installed mod's textures to DDS with RimSort's todds** (13 Nov 2026), so RimSort's texture step is not needed for
  our mods: a fresh install replaces the mod folder and so wiped the DDS files RimSort had made, and the owner ran todds over every
  mod after each update. It uses RimSort's own "optimized" preset exactly (`-f BC1 -af BC7 -on -vf -fs -ss Textures -t -p <folder>`,
  read from RimSort's source, `app/utils/todds/wrapper.py` at commit c02f2b3; the vertical flip matters), only on the mods just
  deployed and only PNG files with `Textures` in their path. todds comes from `--todds`, `RIMSORT_TODDS`,
  `.local/install-settings.json` (key `toddsPath`) or RimSort's default install; without it, or if it fails, the mods still work
  from their PNG files and the run says so. `--no-dds` skips the step. **Confirmed working by the owner in their own install, 13 Nov 2026**;
- never touches `ModsConfig.xml` or RimSort's lists or settings.

The game path comes from `--game`, the `RIMWORLD_PATH` environment variable, `.local/install-settings.json` (key
`rimworldPath`; `check-factory-patches` and `refresh-mod-list` also read `workshopPath` and `rimworldConfigPath` there, and
`triage-log` reads `rimworldLogsPath`, `query-save` reads `rimworldSavesPath` and `modkitCachePath`, and `query-defs` reads
`workshopPath`, `rimworldConfigPath` and `modkitCachePath`), or the default Steam path. **Until 30 Oct 2026 the script knew only 1.6's process name**: one deploy that day
began while the owner's 1.7 game ran and stopped at the first locked file, with the install intact; the rename
step and the 1.7 name come from that.

## Cleaning up

`python scripts/cleanup.py` lists what the tools, builds and tests leave behind, with each group's size and why it may go;
`--apply` deletes it (the owner runs that). It calls phobos-modkit's `cleanup` (since 1.8.0, 11 Nov 2026; its guide is
`docs/cleanup.md` in the submodule), which clears:

| What | Where | Rule |
| --- | --- | --- |
| Save indexes and other derived data | The shared tools' cache, under the temporary folder | An index whose save has been saved again or has gone; anything unchanged for 14 days (`--older-than`); never the Workshop cache |
| The shared tools' development build | `%LOCALAPPDATA%\phobos-modkit\target\debug` | Only the kit's own tests use it; the release programs stay; skipped while cargo runs (`--keep-build` to keep it) |
| Old install backups | `.local/deploy-backups` | The newest of each current mod stays; merged mods keep none |
| Test repositories | The temporary folder, `archive-test-*` | All |
| .NET build logs | The temporary folder, `Microsoft.NET.Workload_*.log` | Older than a day |
| RimWorld crash dumps | `%LOCALAPPDATA%\CrashDumps` | All but the newest |
| Git's file-watcher daemons | Running processes | Stopped; a global `core.fsmonitor` starts one in every repository, and git starts one again when needed |

The repository's own rules are in `config/cleanup.json`: **something new that a script or test leaves behind gets a rule
there**. It reports, never deletes, a folder a failed deploy left in the game folder. **Tests that make a git repository set `core.fsmonitor false` in it and delete it
with read-only handling** (`remove_tree` in `tests/test_archive_branch.py`): git's object files are read-only, and
`shutil.rmtree(..., ignore_errors=True)` left 400 repositories and 220 daemons behind before 8 Nov 2026.
