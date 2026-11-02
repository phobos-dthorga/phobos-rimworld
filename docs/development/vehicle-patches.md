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

## Not changed, for the owner to decide

- Upgrade costs and CE armour weren't compared with the Hind's.
- A shot-down VTOL Raids Harrier may leave a share of the new cost; not checked.
- The owner's log also has `Unable to unlock Vehicles.CompUpgradeTree post-load for Humvee`, twice. Unrelated to this.
- Loading the Harrier above RimPacts also avoids the failure, with no code. It's fragile, since any later patch of that method brings it back.

## Bug reports to post (drafts; the owner posts them)

Written for the mods' authors.

**For RimPacts (wowgag):**

> Hi! RimPacts' patch on Vehicle Framework's `LaunchProtocol.GetArrivalOptions` breaks any mod that patches the same method after it. `RptVehicleBridge.Install` uses a postfix inside a generic class (`RptVfArrivalFilter<>.MakeGenericType(ArrivalOption)`). Harmony stores patches by method token, which can't hold generic arguments, so when a second patch rebuilds the method it gets the open generic back and throws: `InvalidProgramException ... GetArrivalOptions_Patch2 ... resolves to an open generic type`. It breaks the Harrier VTOL Attack Aircraft mod's flight framework here, on RimWorld 1.7 with Harmony 3.0.0-rc.3. A plain postfix in a non-generic class would fix it, for example in a small assembly that references Vehicle Framework and loads only when it's active. Thanks for RimPacts!

**For the Harrier (potemkinvillager):**

> Hi! Lovely jet. On 1.7 with RimPacts active, `FixedWingAircraftFramework.HarmonyInit` throws `InvalidProgramException ... GetArrivalOptions_Patch2 ... resolves to an open generic type`, and `PatchAll` stops there, so everything after `LaunchProtocol_GetArrivalOptions_FixedWingPatch` never loads, including boarding, refuelling and landing. The cause is on RimPacts' side: its postfix on that method is in a generic class, which Harmony can't rebuild. Might this explain Xenogray's report? Applying your patch classes one at a time, each in a try/catch, would limit the damage from any conflict like this. I've reported it to RimPacts' author too.
