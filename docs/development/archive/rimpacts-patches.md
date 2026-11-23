# RimPacts patches: research and decisions (2 to 16 Nov 2026)

The record behind D'thorga Patches' work for RimPacts (wowgag): what was read in RimPacts' code, the owner's logs and saves, why each
patch exists, and who chose what. Each section is kept as written in the record it came from, in date order. How to play is in the
player guide's [RimPacts section](../../player-guide/patches.md#rimpacts), and the settings are in
[the README](../../../mods/PhobosPatches/README.md#settings). Visit Settlements closing a RimPacts battle map is in
[Visit Settlements patches](visit-settlements-patches.md#visit-settlements-closes-a-rimpacts-battle-map-8-nov-2026); the Harrier's
other patches are in [vehicle patches](vehicle-patches.md), and the missile strike in [missile patches](missile-patches.md).

**Status (17 Nov 2026):** built in D'thorga Patches 0.1.0 (the load fix that lets the Harrier's flight framework load beside RimPacts),
0.11.0 (a sortie onto a battle not joined says why it is refused; the setting **Sorties may fly over a battle you have not joined**, off
by default), 0.11.1 (an ally's assault on a struck town faces its garrison from before the strike; 0.11.0's attempt did not work),
0.12.0 (the world war quest's countdown) and 0.19.3 (assaults leave a vehicle with nobody aboard alone); 0.16.1 and 0.16.8 fixed what
the assault note and the quest check keep. The load fix and the empty-vehicle patch are temporary, rows 5 and 17 in
[the register](../temporary-patches.md): row 5's report is drafted, not posted, and row 17 is not reported, at the owner's word. **Not yet
tested in game:** checks P1 and 118 in [the test pass](../in-game-test-pass.md) wait on play.

**The sections:** [the startup failure](#the-startup-failure-rimpacts-and-the-harriers-framework) (2 Nov 2026);
[sorties blocked by RimPacts battles, and the parts list](#sorties-blocked-by-rimpacts-battles-and-the-parts-list-7-nov-2026-evening)
(7 Nov 2026); [assaults on a struck town](#rimpacts-assaults-on-a-struck-town-7-nov-2026-evening) (7 Nov 2026);
[moved from the README](#moved-from-the-readme-17-nov-2026) (17 Nov 2026).

## The startup failure: RimPacts and the Harrier's framework

*Moved here on 17 Nov 2026 from `vehicle-patches.md`; kept as written.*


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

## Sorties blocked by RimPacts battles, and the parts list (7 Nov 2026, evening)

*Moved here on 17 Nov 2026 from `vehicle-patches.md`; kept as written.*


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

## RimPacts assaults on a struck town (7 Nov 2026, evening)

*Moved here on 17 Nov 2026 from `missile-patches.md`; kept as written.*


The owner's report: after a DF-5 on Lance Village (Anlium) they called their client state, the Ryūmon Protectorate, to attack; the force
"never eventuated", RimPacts declared "Defense held", and later about twelve towns were lost (that last part happened after the save
and was reloaded away, so it is unexamined). Read from PG2 of 7 Nov 2026 (letters, the battle map) and RimPacts 1.7's code:

- **Timeline (ticks):** impact 48,051,475; battle begins 48,053,429; "assault force has reached" 48,056,000; "Defense held"
  48,057,000; a second assault opened at 48,057,788, due 48,062,788; PG2 saved at 48,059,629.
- **The cause:** `SpawnSpectateAttackers` generates a squad from points, then `TrimToHeadcount(list, ceil(defenders x techFactor),
  attacker, "관전 공격대")`, where defenders is `DefenderHeadcountOn`: live (not dead, not downed) humanlike or mech pawns of the
  defending side. Our strike sheltered nine Anlium people (rock roof, the default shelter setting) and left two standing, both psycasters
  with psychic amplifiers; Ryūmon and Anlium are both industrial (factor 1), so the squad was about two. In PG2 one Ryūmon soldier
  remains, downed at the east edge (gunshots, blast); 1,496 fires still burned. `ProcessSpectate` checks every 250 ticks and ends the
  battle for the defender when no attacker is a live fighter, checking the attacker first (a mutual wipe-out also goes to the defender,
  where the manual end button gives it to the attacker). A lost battle costs the attacker 4 power; it moves no towns.
- **Decisions (owner: "the nuked town should face the squad it normally would"):** the strike counts the town's standing defenders
  (its own faction and factions not hostile to it, never the player) at its start, before any harm, in a saved world component. Agent
  choices: a 15-day window; the first count kept if a town is struck again within it.
- **0.11.0 did not work** (owner's report, late evening; PG1 and PG2 of 21:21 and 21:23, and Player.log). The log shows the 0.11.0
  prefix on `TrimToHeadcount` ran ("keeps up to 39 soldiers (RimPacts would have trimmed it to 2)"), yet two Ryūmon soldiers came.
  RimPacts' `SpawnSpectateAttackers` uses one figure three times: `num15 = ceil(DefenderHeadcountOn(...) x techFactor)` is passed to
  `RptGenTrim.Begin`, whose postfix on `PawnGroupMakerUtility.ChoosePawnGenOptionsByPoints` cuts the generated options to that many;
  the top-up loop runs only while the squad is smaller than it; then `TrimToHeadcount` trims to it. Raising only the last step changed
  nothing. The strike logged "Lance Village had 39 defenders standing before the strike" and killed 35.
- **0.11.1:** a postfix on `DefenderHeadcountOn` itself raises the count to the pre-strike garrison, so RimPacts' own cap, top-up and
  trim (tech factor included) all follow. It applies only while RimPacts' battle record says spawned and not spawn-done (inside the
  synchronous long event that raises the assault; the support-force spawn there reads only the out parameter), only for that battle's
  map and attacker, only for a town with a recent count, and never when the real count is 0, so RimPacts' "defenders wiped out, the
  attack wins" paths and its arrival check are untouched. The `TrimToHeadcount` prefix and the letter backfill for strikes without a
  count are removed (agent choice: an untrimmed squad cannot be expressed through the count without inflating the top-up).
- **The twelve towns (answered):** RimPacts' world war quest, not combat. `WorldWarQuestAccept` sets a deadline of accept + 180,000
  ticks (3 days); `WorldWarQuestDaily` runs once a game day and, if the target has not fallen and the deadline has passed, fails the
  quest with `WorldWarQuestApplyPenalty`, which moves world war control (`ControlSideOf`) of about three quarters of the reward towns,
  not ownership (settlement ownership was identical in both saves). Accepted at tick 47,828,059, due 48,008,059; the battle opened at
  48,051,717; the daily check at 48,060,000 failed it and 11 border towns changed control. At PG1 Lance Village still had two Anlium
  walker frames standing (nine people downed), so the town had not fallen; had it fallen before that check, the capture path would
  have counted the war won. RimPacts counts as attackers only the attacking faction's live fighters and the player's when the player
  joins the attack (`CountSpectateSides`); allied factions (Ryūga, the 688th) and the mercenary company fight but do not count. The
  owner's DMS II mechs counted, as the player joined the attack. Nothing in our patches touched the quest.
- **The countdown (Patches 0.12.0; owner: "bring it in line with the game's vanilla style").** The quest window shows a red time left
  and "Expires in" only for an accepted quest holding an enabled QuestPart_Delay with `isBad` and expiry text (`MainTabWindow_Quests`,
  1.7). RimPacts' quest (`wwQuestObj`, with its own `QuestPart_RptWarQuest`) has none: its deadline is `wwQuestDeadlineTick` in its
  world component. A game component in a new RimPacts tie-in assembly (`Mods/RimPacts/<version>`, loaded only with RimPacts) checks
  every 250 ticks and adds one vanilla QuestPart_Delay to the running quest, enabled through the game's own `Enable`, ending at the
  first daily check at or after the deadline (RimPacts' saved `nextDailyCheckTick`, a day apart), with the game's `QuestExpiresIn`
  and `QuestExpiresOn` text. Agent choices: count to the deciding check rather than the nominal deadline (the owner's war failed 0.87
  days after it); reuse the game's keys so every language has them; no alert in the right-hand list (the owner asked for the
  quest window's style). Not yet tested in game.

## Moved from the README (17 Nov 2026)

*Moved here on 17 Nov 2026 from `vehicle-patches.md`, which took these paragraphs from D'thorga Patches' README the same
day; kept as written.*

**RimPacts' assaults and empty vehicles (Patches 0.19.3, temporary).** RimPacts lists every standing pawn of the defending faction as
an assault target, and a vehicle is a pawn that never goes down, so an empty one stayed a live target until it was destroyed. The
game's order for such a target is to trash it (melee, sometimes fire), because Vehicle Framework rightly says an empty vehicle is no
threat and nobody shoots at it; and Vehicle Framework destroys a vehicle only when all its parts together fall under 1 %. RimPacts
also waits for no live "pawn" target to remain before its general assault. The patch makes RimPacts' live-target test answer "no" for
a vehicle its own unmanned test (nobody aboard, not autonomous) calls unmanned, the test it already uses when counting fighters.
Soldiers on such a vehicle get a new target at RimPacts' next look, every 300 ticks. Its row is 17 in
[the register](../temporary-patches.md).

**The RimPacts load fix in the log.** With the fix working, the log shows a line starting "[D'thorga Vehicle Patches] RimPacts'
landing rule". If RimPacts changes how it does this, the fix finds nothing to do and steps aside.
