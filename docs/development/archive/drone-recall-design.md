# Drones called home in time, and their wrecks taken home (23 Nov 2026)

**Status (23 Nov 2026):** built in D'thorga Framework 0.240.0 (the Def type, the recall, the wreck work, two settings, and the
top-up's three fixes) with D'thorga Patches 0.39.0 naming Fortified Features Framework's drone comp. Not yet tested in game: rows
383 to 385 of the test pass.

## Read this first

- **The problem:** the owner, 23 Nov 2026: The Dead Man's Switch's engineering drone bays "very rarely topped-up with steel, if at
  all", and "the carcasses of its robot spawn are littered all over the place".
- **One cause behind both:** drones died of a flat power cell instead of walking home. A drone that gets home gives its platform its
  cost back; one that dies gives nothing back and leaves a wreck. Platforms set to deploy by themselves spent their steel faster
  than haulers brought it.
- **What was built, at the owner's word ("go for 1, 2 and 3"):** (1) the top-up's own faults fixed; (2) drones called home once
  their power barely covers the walk; (3) wrecks carried home, where the platform takes back their steel.
- **Which drones is said in data** (`PhobosRecallDef`), never in code, as the top-up is. A renamed member makes the Def stand down
  with one warning; nothing breaks.
- **Fortified's own call home, at two hours of power, still stands.** Ours only adds an earlier call; nothing of theirs is patched.

## What was found [save, log, code]

From the owner's save PG19 (read only), the seen-in-play notes and Fortified Features Framework's build of 23 Nov 2026:

| Finding | Evidence |
| --- | --- |
| The top-up itself works | 41 deliveries to drone platforms noted 18 to 20 Nov; four trashpan stations with automatic deployment off held 131 to 150 of 150 steel |
| Every platform with automatic deployment on was starved | The four engineering bays held 2, 10, 20 and 20 of 360; two trashpan stations 0 and 20 of 150 |
| Drones died of a flat cell | Ten of ten recent trashpan wrecks sampled had 0 power left, some 55 cells from home, one a single cell away; one uca died the same way two cells from its bay |
| A return refunds, a death does not | Fortified's `JobDriver_ReturnToPlatform` calls the platform's `Retracted`, which refunds the drone's cost; nothing runs on a death |
| The call home ignores distance | `CompDrone.CompTick` calls `ReturnToPlatform(forceInterrupt: true)` once power falls below 5,000 ticks, every 500 ticks |
| Drones walk slower than their speed suggests | A live uca had walked home for 3,006 ticks and was still 64 cells out, 1,802 ticks left (about 47 ticks a cell against 15 for its speed) |
| 58 wrecks on the map | 51 trashpan, 5 wildfowl, 2 uca; mechanoid wrecks never rot, and haulers carried some to the uranium's stockpile |
| The top-up stood back while a drone walked home | The returning drone reserves its platform for the whole walk; the top-up refused a reserved building |
| Small loads, late | One stack a trip, the nearest however small (17, 20, 25, 28 noted); 20 in Hauling, behind almost all other hauling (the game's refuelling is 140) |

**Not found:** why a trashpan dies one cell from home with two hours of warning, or why the uca walked so slowly. That needs a look
at the running game while it happens. The padded walk below is meant to cover it whatever the cause; if drones still die close to
home, that look comes first.

## What it lets the player do, stops them doing, and changes

- **Lets:** leave drone platforms on automatic deployment and have them stay stocked: drones come home and give their steel back, and
  the wrecks of those that die anyway turn back into steel at a platform.
- **Stops:** nothing. Drafted drones are left to the player; both parts have a setting.
- **Changes:** drones far from home stop work sooner, so they work a little less per trip. Haulers top platforms up before most other
  hauling, with fuller loads, and carry wrecks to platforms before they would go to a stockpile.
- **What outside the drones notices:** the colony's haulers (new work, earlier), the steel stockpiles (steel comes back from wrecks), the
  map (no more wrecks piling up), and the log (a Debug line for each call home, with the power left and the walk). No letter or alert:
  a drone getting home is the ordinary case, not news (agent choice).

## Risk

| Part | What the player can lose | Grade | Default |
| --- | --- | --- | --- |
| Calling drones home sooner | A little drone work per trip; never a drone or a person | Low | On |
| Carrying wrecks home | Nothing: a wreck's steel goes to a platform rather than a stockpile, and a wreck could otherwise only be butchered for the same steel | Low | On |
| The top-up's fixes | Steel leaves stockpiles sooner and in bigger loads, as before but faster | Low | (the existing setting, on) |

Grades are the agent's proposal until the owner confirms them.

## How it works

| Piece | What it does | Where |
| --- | --- | --- |
| `PhobosRecallDef` | Names a drone's comp and, on it, the home, the comp's own method that sends the drone home, the job it does on the way, and the wait at home | `src/PhobosFramework/Recall/PhobosRecallDef.cs` |
| `RecallRule` | Game-free: the power a drone needs (the walk padded by half, the wait, 600 spare ticks), when the path is worth pricing, and how a wreck's salvage splits between the store and the ground | `src/PhobosFramework/Rules/RecallRule.cs`, tested |
| The call home | Every 250 ticks, each drone of the player's that is not drafted, downed or already going home: a straight line priced at four times its speed decides whether to ask the path finder; the path's cost, kept 1,000 ticks, decides whether to call it home through its own comp | `Recalls.LookAtDrones`, `GameComponent_PhobosRecall` |
| Wrecks taken home | Hauling work at 95 (before the game's corpse hauling at 90): a wreck goes to its own platform if that is a building with room whose material the wreck salvages to, else to the nearest such platform; the platform takes in the wreck's butchering yield of its material, the rest set down beside it | `Recalls.HomeForWreck`, `WorkGiver_PhobosReclaimWreck`, `JobDriver_PhobosReclaimWreck`, `Common/Defs/Phobos/Framework/Recall.xml` |
| Fortified's drones | `PhobosRecall_FortifiedDrone`: `Fortified.CompDrone`, home `PlatformOwner`, `ReturnToPlatform`, job `Props.returnToDraftPlatformJob`, 180 ticks at home | `mods/PhobosPatches/Mods/Fortified/Defs/Recall_Fortified.xml` |
| The top-up's fixes | A building is no longer held by the hauler (only a mechanoid is), so a drone walking home no longer turns haulers away; a load is gathered from several stacks up to what the hauler can carry; its place in hauling is 139, just below the game's refuelling | `TopUps.Holds`, `TopUps.JobFor`, `JobDriver_PhobosTopUp`, `TopUp.xml` |

**Power** is read from the game's own mechanoid power cell (`CompMechPowerCell.PowerTicksLeft`), so the Def names none. **The salvage** is
the drone's own butchering yield of the platform's material, the drone mod's figure (trashpan 25 steel, the same as its cost; uca 30, against
a cost of 40). Taking the whole yield, with no work and no skill, is an authored simplification (agent choice): the same steel a shredder
would give, without the bench.

**Cost to run.** Every 250 ticks the player's pawns on each map are scanned for named drones (a dictionary look-up each). For most drones
the straight-line check ends it; the path finder is asked only near the line, at most once every 1,000 ticks a drone. The wreck work
returns at once with no named drone wrecks on the map. Nothing is saved.

## For other modders

```xml
<Phobos.Framework.Recall.PhobosRecallDef>
  <defName>MyMod_Recall_Drone</defName>
  <compClass>MyMod.CompDrone</compClass>
  <homeMember>Home</homeMember>
  <recallMethod>GoHome</recallMethod>
  <returningJobMember>Props.goHomeJob</returningJobMember>
  <dockTicks>120</dockTicks>
</Phobos.Framework.Recall.PhobosRecallDef>
```

The drone must carry the game's `CompMechPowerCell`. For wrecks, its home must be a building a `PhobosTopUpDef` names, and the drone's
Def must list that material in its `butcherProducts`. `recall` or `reclaimWrecks` set to false turns either half off for that kind.

## Limits and open questions

- **A drone that walks slower than its path promises can still die.** The walk is padded by half; a fault that slows drones further
  needs the look named above. The padding and the spare are constants in `RecallRule`, not settings (agent choice: a setting nobody can
  judge is noise; the owner can ask for one).
- **A home that is a pawn** (the Hermit Crab, the Cluster Weaver, an apparel platform's wearer) is followed by the call home, but
  wrecks go only to buildings.
- **Fortified's own two-hour call stays.** A report to its author about calling by distance is the owner's to ask for.

## Who chose what

- **Owner (23 Nov 2026):** fix the top-up's faults, call drones home by distance, and have platforms take their dead back ("go for 1, 2
  and 3"); left aside: wrecks vanishing into steel on death.
- **Agent defaults:** both settings on; the padding (half again), the spare (600 ticks), the straight-line stretch (4) and the price kept
  1,000 ticks; looking every 250 ticks; drafted drones left alone; the top-up at 139 and wrecks at 95 in Hauling; the whole butchering
  yield as the salvage; a full home passed over for the nearest platform with room; no letter or alert.

## Sources

- Fortified Features Framework (AOBA): `CompDrone`, `CompMechPlatform` and `JobDriver_ReturnToPlatform`, read from the installed build of
  23 Nov 2026, decompiled into `.local/decompiled`; summarised in our own words.
- The Dead Man's Switch (AOBA): the drones' and platforms' Defs, for their power cells, costs and butchering yields.
- RimWorld 1.7 (Ludeon Studios): `CompMechPowerCell`, `PathFinder.FindPathNow`, `JobDriver_HaulToContainer` and `Toils_Haul`, for the power
  cell, the path's cost and the game's own way of gathering a load from several stacks.
