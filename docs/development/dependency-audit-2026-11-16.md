# Dependency audit after the owner's break (16 Nov 2026)

**Answer first.** Of the 38 mods on the owner's list that updated between 13 and 16 Nov 2026, nine are ones our mods touch.
The big one is **Vehicle Framework's 1.7 release (1.7.2534, 14 Nov 2026)**: our build was still compiling against its old 1.6
files, and one of its changes made our world-map reach ring wrong. Both are fixed in D'thorga Framework 0.111.1 (not yet tested
in game). Everything else we patch or name still exists and still fits. Three VF changes reach our designs and need the owner's
call, and two need a look in game.

## Read this first (gotchas)

- **The installed D'thorga Framework (0.111.0) was built against VF 1.6.** With VF 1.7 in the game, its world-map reach ring would
  count every tile as almost free and fill out to its limit. Nothing was found that crashes. **0.111.1 is worth the next install**
  if vehicles or caravans are in play; otherwise it can wait for a batch.
- **VF 1.7 sends everyone out of a vehicle that stands still for five in-game hours** (12,500 ticks), with no exception for gunners
  or crew on station. Read from VF's code, not seen in game. See decision 2.
- **VF 1.7's hand brakes wear every part tagged `Wheel`**, on by default, beside our own tread wear. See decision 1.
- **Prison Labor fixed the crash the owner reported** (1.7.1, 13 Nov). Our guard stands down by itself.
- Everything below marked "read from the code" is what the decompiled code says, not observed play.

## How it was checked

All with our tools, in this order, on 16 Nov 2026:

| Step | Tool | Result |
| --- | --- | --- |
| Which mods changed | `python scripts/refresh-mod-list.py` (diff of `owner-mod-list.md`) | 38 updated; 9 we touch: Vehicle Framework, Vanilla Vehicles Expanded and Tier 3, Navistar MaxxPro, Harrier, Prison Labor, RimPacts, Hospitality, Cherokee Sling |
| Does our C# still build | `python scripts/check-all.py` | Passed, but against VF's **1.6** folder (`Directory.Build.targets` named it). Pointed at 1.7: two errors, both in `PhobosFramework.Vehicles`. Fixed; 45 of 45 steps pass |
| Members we name by string | `scripts/check-targets.py` (part of the gate) | All found |
| What changed in each mod's code | `python scripts/query-code.py compare` (VF's 1.6 and 1.7 files side by side on disk; the others against earlier decompiles in `.local/decompiled`) | VF: 886 declarations added, 399 removed, 195 new signatures, 867 new bodies. SmashTools lost about 1,200 to new files (`CoreLib`, `SmashTools.Rendering`, `SmashTools.PathFinder`) |
| Our Harmony patches on them | `python scripts/query-patches.py mod "PhobosDthorga.*"`, set against the comparison | 40 VF members patched: 29 unchanged, 11 new bodies, none renamed or re-signed |
| Our XML against their Defs | `python scripts/query-defs.py` (patch targets, MayRequire'd Defs, parents) | Every patch target and every VVE Def we name exists. Removed or retyped VF fields: none used by our XML |
| What players and authors say | `python scripts/workshop-research.py report` | Below, per mod |

## Fixed in D'thorga Framework 0.111.1

1. **Build against what the game loads.** `Directory.Build.targets` now takes VF's, VFE Factory's and IRBM's game-version folder as
   soon as it exists, and 1.6 only until then. A fixed 1.6 path had let the gate pass against files the game no longer loads.
2. **The reach ring.** VF 1.7 kept the old `VehicleCaravan_PathFollower.CostToMove(vehicles, ticksPerMove, start, end)` but marked it
   obsolete, and its body builds the new `TicksPerMoveData` without `ticksPerMove`, so each tile costs the 1-tick minimum.
   `VehicleRange` now calls the new overload with `ticksPerMove` set. This is a fault in VF's compatibility overload; not dire
   (anyone calling the new overload is unaffected), so no report is drafted unless the owner wants one.
3. **`CoreLib` reference.** VF 1.7 moved shared types such as `Path.Node` into `CoreLib.dll`; the vehicle project references it.

## Our patches on VF: what changed under them

| Our patch | VF member | What changed | Effect on us |
| --- | --- | --- | --- |
| `SpeedCapPatch` (drivetrain speed cap) | `VehiclePathFollower.CostToPayThisTick` | Now multiplied by VF's new acceleration (`controller.MoveSpeedPct`) | Our cap multiplies VF's result, so it still applies, on top of the ramp-up. The drivetrain record's "acceleration only under VVE's controller" no longer holds: VF accelerates every vehicle itself |
| `CaravanSpeedCapPatch` | `VehicleCaravanTicksPerMoveUtility.GetTicksPerMove` | Rewritten around a caravan summary; ridden animals and speed bonuses now count | Our postfix scales the result as before |
| `LiftOffPatch`, `Patch_CanLaunch_Sling` | `CompVehicleLauncher.CanLaunchWithCargoCapacity` | Rotation check only while spawned; crew check simplified | None |
| `SortieGatePatch` | `CompVehicleLauncher.CanTarget` | Fuel no longer checked here | None; the Harrier now adds its own fuel check (below) |
| `UpgradePaidPatch`, `UpgradeRefundPatch` | `CompUpgradeTree.FinishUnlock`, `ResetUnlock` | Log text only | None |
| Disassembly trace (temporary patch 15) | `VehiclePawn.Destroy`, `DisembarkAll`, `GenerateGraphic`, `RGBMaterialPool.Release` | `Destroy` now drops inventory and leavings itself and turns other destroy modes into Vanish before the game's own destroy | The Harrier take-apart fault may be gone or moved; the trace still hooks every stage. None of our comps depend on the destroy mode |

Unchanged, so the temporary patches built on them are still needed: `VehiclePawn.DeSpawn` (fires left behind, patch 2),
`JobDriver_WorkVehicle.MakeNewToils` (no work bar, patch 14), `CompUpgradeTree.PrerequisitesMet` (upgrade research, permanent).

## Other mods

| Mod | What changed | Effect on us |
| --- | --- | --- |
| **Harrier** (15 and 16 Nov) | New patches for VF 1.7: `Patch_CanTarget17` (a prefix that may skip `CanTarget`), `Patch_ArrivalOptions17` (replaces the arrival list for fixed-wing aircraft), `Patch_SortieFuel17`, `Patch_UiPicture17`, and `SafePatching`, which skips four of the Fixed-Wing Aircraft Framework's patches on 1.7. A player reported VF and VTOL Vehicle's updates broke launching; the author says it is fixed for the Harrier | Our sortie gate is a postfix and still runs. Our hover-gate targets are unchanged. Its XML is unchanged since 9 Nov, so our bomb patches' gate (Mass 227) still holds. Read from the code; untested |
| **RimPacts** (15 Nov) | Large release; the three members we touch kept their signatures and logic | Its arrival filter is still installed from a generic class, so temporary patch 5 is still needed, and the Harrier now patches the same method |
| **Prison Labor** (1.7.1, 13 Nov) | `GetFoodRestrictionPatch.Postfix` returns `CurrentFoodPolicy` instead of calling back into the method it patches | **The crash is fixed upstream**; temporary patch 1 stands down by itself and can be removed at a later Patches release |
| **Vanilla Vehicles Expanded, Tier 3** (14 Nov) | Now declare 1.7 | Every VVE part and research we name exists |
| **Navistar MaxxPro** (15 Nov) | Now declares 1.7 | Our patch targets exist |
| **Hospitality** (13 Nov) | Code changes; we only name it in Medical's load order | None |
| **Cherokee Sling** (15 Nov, switched off in the owner's list) | Now needs a new mod, **Sling Framework** (`yaroslav.slingframework`), which is not on the owner's list | Our sling already stands down on the Cherokee while Cherokee Sling is active. If Sling Framework comes to give slings to other helicopters, our stand-down list would need it too |

The mod list also flags **Vehicle Framework and Vanilla Outposts Expanded as marked incompatible** (by VF's new About file) and a
load-order rule for The Dead Man's Switch sounds. Neither is ours to change; RimSort will show both.

## Decisions for the owner

1. **Wheels wear twice.** VF's hand brakes (setting "Use hand brakes", on by default) scratch every `Wheel`-tagged part by 0.15 per
   node of braking; our tread wear wears the same wheels. Leave both, or have our tread wear stand down while VF's hand brakes are
   on? (The question the vehicle health record left for this release.)
2. **Crew leaving parked vehicles after five hours.** VF's rule would empty a sonar or listening watch, a parked ambulance's
   doctors (the litter) and a defensive vehicle's gunners, and every five hours shows a submerged submarine's "nobody can step out"
   message (our guard stops anyone landing in the water). VF offers a public `VehiclePawn.ResetIdleTicks()`, so our stations could
   keep their crew while they are working, with no Harmony. Keep crew aboard while a station of ours is working, or leave VF's
   rule alone?
3. **Prison Labor guard.** Remove it at the next Patches release, or keep it standing down for a while?

**Answers (owner, 16 Nov 2026):**

- **2: use Vehicle Framework's own means where they exist.** Built in Framework 0.112.0 (`CrewOnStation`): our stations reset VF's
  idle count through its public `ResetIdleTicks` while they are engaged, as VF's turrets do while aiming or firing. When all is
  quiet the five-hour rule stands. Not yet tested in game.
- **3: remove it.** Done in Patches 0.19.2.
- **1: still open.** The owner asked whether our tread wear is our own. It is: one wear figure per vehicle that cuts grip on soft
  ground and is renewed by new tyres or a tyre service. VF's hand brakes instead scratch the health of each wheel part.

## Checks in game (not yet tested)

1. **The reach ring** on the world map for a vehicle caravan, after installing Framework 0.111.1: about the size it was before.
2. **An unhitched trailer.** VF 1.7 lets a caravan made only of vehicles that need no driver form with no colonist (read from the
   code: `CaravanFormation.CheckForErrors`, `Ext_Vehicles.IsAutonomousVehicle`). Our trailers have no seats, so VF counts them as
   such. Can a lone Pintle trailer be sent off as a caravan, or drafted and driven? If yes, the trailer needs a refusal of its own.
3. **The Harrier**: launch and land once, since its author changed its VF patches twice during the break.
