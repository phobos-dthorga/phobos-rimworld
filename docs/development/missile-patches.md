# Missile patches: research and decisions (7 Nov 2026)

**Since 7 November 2026 this mod is a part of [D'thorga Patches](../../mods/PhobosPatches/README.md)** ([why](mod-consolidation.md)); paths below are updated, the rest is as recorded.

The record behind D'thorga Missile Patches. **Tried once in game: no freeze, but far too slow** (see [results from play](#results-from-play)). The player
guide is the mod's README; this holds the log evidence, the code readings, the sources and who chose what.

## The hang: a strategic warhead landing

**What the owner reported** (7 November 2026): every strategic missile launched from InterRim Ballistic Missile (IRBM, Psionic
Zephyr, Workshop item 3682304832, declares 1.6, run on RimWorld 1.7.5000) "completely crashes" the game at landing or detonation.

**What the owner's `Player.log` showed** (run started 12:04:02, log copied with shared read):

```text
12:15:39  INFO [IRBM] Launched missile: DF-5 (Standard Strategic Nuclear Warhead)
12:16:16  INFO [IRBM] Strategic impact sequence initiated on map Lance Village. Screen fading to white...
12:16:16  INFO [IRBM] Strategic missile impacted at (84.4, -27.9, 45.9). Physics Radius: 1.49, Tile Radius: 6
12:16:20  ERROR penAmount or armorAmount are zero for Blunt on VAEA_Apparel_MiniTurretPack...
12:16:2x .. 12:18:12  ERROR Tried to destroy already-destroyed thing Fence... / Sandbags... / Barricade... / Wall... / ShelfSmall...
    at IRBM.StrategicImpactCountdown.ApplyStrategicDamage(...) in ...\IRBM\Source\StrategicImpactCountdown.cs:464
    at IRBM.StrategicImpactCountdown.OnWhiteScreenReached() ...:143
    at IRBM.StrategicImpactCountdown.Update() ...:77
    at IRBM.Patch_RootPlay_Update.Postfix() ...HarmonyPatches.cs:349
```

About a hundred such errors, spaced 0.05 to 1 s apart, over almost two minutes, all from the same call; the log then stops
without a "Log closed" line. **The Windows Application log** has the rest: at 12:18:27 Windows Error Reporting filed an
`AppHangB1` for `RimWorld.exe 1.7.5000.1275` and at 12:18:28 "Application Hang" event 1002, "stopped interacting with Windows and
was closed". So it is a hang on the main thread, not an exception: one frame that never ended. (The owner closed the
not-responding game, or Windows did; either way the frame was still inside the same loop two minutes in.)

The destroy errors are a symptom, not the cause: the mod snapshots every thing on the map and later reaches things that
something earlier in the pass had already destroyed (a wall's collapse taking a shelf with it, for instance).

The earlier run that morning (`Player-prev.log`, 12:00 to 12:03) ended normally and had no strategic launch; it is unrelated.

## Why one frame takes minutes (read from code)

**IRBM's side** (`StrategicImpactCountdown`, decompiled from the mod's 1.6 assembly on 27 October 2026; the assembly is dated
18 October 2026 and unchanged since; `.local/decompiled/IRBM-1.6`):

- A strategic warhead's impact (`WorldObjectComp_StrategicPayload`) finds every world object within the warhead's `tileRadius`
  (6 tiles for the extinction-class warhead) and, for each that **already has a map open**, calls `Initiate(map)`. Settlements
  without a map are simply destroyed as world objects. Lance Village had a map open.
- `Initiate` pauses the game, fades the screen to white over 3 s and counts down in `Update`, which the mod runs from a Harmony
  postfix on `Root_Play.Update` (so it runs every drawn frame, paused or not).
- When the screen is white, `OnWhiteScreenReached` runs **in that one frame**: the visuals, then for each target map
  `ApplyStrategicDamage`, then (dismantling warheads) a terrain pass over every cell, then fires on a fraction of all cells
  (`fireChance`, 0.2 for the extinction-class warhead), then conditions, weather, a letter.
- `ApplyStrategicDamage` copies `map.listerThings.AllThings` and, for each thing in the warhead's categories (pawn, building, item,
  plant), calls `TakeDamage` with the warhead's damage (extinction-class: Bomb, 10,000, armour penetration 200, pawns hit on
  their core part), unless the thing sits under thick rock roof. Every per-thing exception is swallowed. Warheads with
  `damageTimes` above 1 (the "clean" and "surgical" strategic warheads, 10 passes each) repeat the call every 60 ticks from a
  `GameComponent`.

**The game's side** (RimWorld 1.7 assembly, decompiled with ilspycmd to `.local/decompiled/RimWorld-1.7`, 7 November 2026):

1. A destroyed building leaves things behind (`Building.Destroy` with `KillFinalize` runs `GenLeaving.DoLeavingsFor`), placed
   with `GenPlace.TryPlaceThing(..., ThingPlaceMode.Near)`.
2. `GenPlace.PlaceSpotQualityAt` asks `c.GetRoom(map)` for the candidate cell and the centre (line 261 of the decompile).
3. `GetRoom` goes through `RegionGrid.GetValidRegionAt`, which calls `map.regionAndRoomUpdater.TryRebuildDirtyRegionsAndRooms()`
   before answering.
4. Every building despawn has dirtied regions (`Thing.DeSpawn` calls `regionDirtyer.Notify_ThingAffectingRegionsDespawned`), so
   that call regenerates the dirtied regions and rebuilds their rooms, then fires `map.events.Notify_RegionsRoomsChanged`,
   which every mod that watches rooms answers.

So the pass is: destroy a wall, dirty its regions; place its leavings, rebuild those regions and rooms now; destroy the next
wall... Normally the rebuild happens once per frame, in `Map.MapUpdate`; inside this loop it happens once per building. The
cost of each rebuild grows with how much the base's rooms merge into the outdoors, and with the mods reacting to room changes;
the number of rebuilds is the number of buildings. On a town map under about 340 mods, that is minutes.

The 1.6 assembly has the same structure in these methods (`RegionAndRoomUpdater` has the same `working` and `enabledInt` fields
in the 1.6 reference assembly), so the difference between the owner's 1.6 and 1.7 play is most likely only the map and mod
count, not the engine; **not measured**.

Other costs in the same frame, smaller but real: `ListerThings.Remove` scans its lists for each removed thing (`IndexOf`, then a
swap), filth and rubble for each damaged building, each mod's patches on `TakeDamage` (11 in the owner's game) and
`Building.Destroy` (7), and the fire pass that spawns fires on a fifth of the map's cells.

## What the fix does

A Harmony prefix on `StrategicImpactCountdown.ApplyStrategicDamage` skips the original and queues a `StrategicSweep`
(`src/PhobosPatches.Irbm/StrategicSweeps.cs`); a postfix on `Root_Play.Update` runs the queue a slice per frame.

- **Spread over frames.** Each frame hits things until a real-time budget is spent (12 ms by default, a setting from 2 to 50;
  at least 8 things a frame so a slow thing can't make the blast creep). The game draws, the white screen fades as before,
  messages and the camera work.
- **The room rebuild is held during a slice.** The updater's private `working` flag is set for the slice through
  `AccessTools.FieldRefAccess`; while it is set, `TryRebuildDirtyRegionsAndRooms` returns at once and a room lookup in a
  dirtied cell answers null, which the callers already handle (an impassable cell has no room either: `GenPlace` then falls
  back to a reachability test, which answers false, and places the thing with a lower score). At the slice's end the flag is
  cleared and the rebuild is run once. Setting `Enabled = false` instead was rejected because `GetValidRegionAt` logs a warning
  on every lookup while the updater is disabled and anything is dirty.
- **Same outcome.** The per-thing rules are IRBM's, written again: the damage info with the mod's own weapon Def (so the
  health and battle logs name the missile), `IRBM_DamageContext` set for the mod's own damage patches and restored after each
  slice, the thick-rock-roof shelter, the core-part hit, and the dismantle paths (buildings refunded and removed, mineables
  mined out, plants harvested, fresh corpses stripped, their implants dropped and butchered). Things already destroyed or
  despawned are counted and skipped, which also ends the red "already-destroyed" errors.
- **Order from the centre out** (agent choice): the mod's order was spawn order; a wave reads better when the blast takes
  seconds. Nothing in play depends on the order.
- **One log line per finished blast** with counts, seconds, frames and swallowed errors, for the owner's reports.
- **Messages** in the game's own words, after two seconds and at the end, naming the place and never the mod.

What it leaves alone: `OnWhiteScreenReached`'s other work (visuals, the terrain pass of dismantling warheads, the fire pass,
conditions, weather, the letter) still runs in the frame the screen reaches white. The fire pass spawns thousands of fires
in one frame on a big map; if it proves slow in the owner's game, it is the next candidate, but it needs a replacement of the
whole private method and was not worth that on a guess.

## Decisions

| Decision | Who | Why |
| --- | --- | --- |
| A mod of its own, D'thorga Missile Patches, in IRBM's load folder | Agent | The same pattern as Vehicle Patches: one patch mod per other author's mod, nothing loaded without it |
| Built on D'thorga Framework (load line, debug category) | Owner (7 Nov 2026: "make use of our Framework wherever appropriate") | The shared helpers; `OptionalMods` is not needed because the assembly loads only with IRBM |
| Prefix that skips the original | Agent, recorded reason | The original is one loop with no extension point; pacing it needs replacing it. A setting turns the prefix off |
| Hold the updater's `working` flag rather than `Enabled` | Agent | No warning spam; identical effect on lookups |
| 12 ms a frame, 8 things minimum, 2 s before a message | Agent defaults | For the owner to tune in play |
| Centre-outward order | Agent | Reads as a wave; no gameplay effect |
| No pause forced during the blast | Agent | IRBM pauses at initiation; a warhead with 10 passes would otherwise pause and unpause 10 times |
| The fire pass left as it is | Agent | Not shown slow; replacing it means replacing a private method wholesale |

## Test protocol (owner runs it)

1. Deploy, switch the mod on after InterRim Ballistic Missile and D'thorga Framework, load a throwaway copy of the save. Read
   the load line: `[D'thorga Missile Patches] 0.1.0 loaded. On: strategic blasts spread over frames, 12 ms a frame, rooms
   rebuilt once a frame during a blast.`
2. Dev mode, debug actions, category **D'thorga**: **Missile Patches: strike current map** on a map you can lose (an enemy
   town you have open, or the home map of a throwaway save). Expect the map to empty from the centre out over a few seconds
   with the game still responding, a message if it runs over two seconds, and one log line `Blast over <place>: N of M things
   hit in S s over F frames`. Report that line.
3. Then the real thing: a strategic missile at a settlement you have a map open on (the case that hung). Expect the white
   screen, the fade back, the wave, the letter, no hang. Quote the blast line and any error naming `Phobos.MissilePatches`.
4. If anything about rooms looks wrong after a blast (temperature, roof, indoors status on the struck map), switch off
   "Rebuild rooms once per moment during a blast" and repeat step 2; report the difference in the blast line's seconds.
5. Optional: a "surgical" or "clean" strategic warhead, which strikes ten times a game second apart, to see the passes queue.

## Results from play

**7 November 2026, the owner, Missile Patches 0.1.0** (read from `Player.log`, run started 13:11:45): load line as expected ("strategic
blasts spread over frames, 12 ms a frame, rooms rebuilt once a frame during a blast"). A DF-5 struck Lance Village at 13:18:37 (the
same target as the hang). **The game did not hang:** no Application Hang in the Windows log, and the log kept running. **But the blast
did not finish:** at 13:32:57, more than 14 minutes later, it was still crossing the map, with no "Blast over" line, and
"Tried to destroy already-destroyed thing" errors from our sweep (now walls first, centre outward) at roughly the rate of the hang.

**Correction to "What the fix does".** Holding the room rebuild was **not** most of the saving, as this record and the README first
said: the same error rate means the work per thing is about what it was, so the rebuild was not the main cost. The two likely costs,
both unmeasured: the frame budget (12 ms is a small share of a frame on a game with about 340 mods, so the blast gets perhaps a seventh
of real time; agent's estimate from the timings), and the per-building work of every mod's damage and destruction hooks plus the
game's roof-holder checks (`RoofCollapseCellsFinder.ProcessRoofHolderDespawned`, a flood fill over connected roof for each fallen wall,
read 7 Nov 2026). **Work stopped here at the owner's request**: they have proposals for what to do about this mod.

## Stage 1: the strike finishes (0.8.0, 7 Nov 2026)

The owner asked for proposals in our own flavour, with Dubs Rimatomics' nukes as inspiration, and approved a two-stage plan.

**Where the time went** (read from code; the owner's Combat Extended settings file has `fragmentsFromWalls` on):

1. **Combat Extended's wall fragments.** CE's prefix on `DamageWorker.Apply` (`Harmony_DamageWorker_Apply`) gives a building hit
   with 10,000 Bomb (x4 for impassable buildings) up to 100 medium fragments, and drains `CompFragments.FragRoutine` at once. The
   game is paused through a strike, so they stay, and `Thing.DeSpawn`'s `spawnedThings.Remove` and `ListerThings.Remove` scan ever
   longer lists. This fits the owner's log: about 60 hits a minute at first, 3 a minute at the end. Not measured.
2. **Roof checks per fallen wall**: `RoofCollapseCellsFinder.ProcessRoofHolderDespawned` flood-fills the connected roof (unbounded)
   and checks about 145 cells within 6.9 of a holder, each a bounded flood fill.
3. **The kill path**: leavings, filth, effects, lord and faction notices, and seven mods' patches on `TakeDamage`, `Kill`,
   `Destroy` and `DeSpawn`, each spawning more things.

**How Rimatomics does it** (`NuclearStrike.cs`, 1.7): it strips every built roof first; skips every cell under natural rock roof,
thin or thick; vanishes things near ground zero with `Destroy(Vanish)`; damages beyond with a log falloff; and runs its passes back
to back behind a "Detonating..." loading screen. One tile only, no time budget.

**What 0.8.0 does**: `HitRule` vanishes what the warhead's damage would destroy (pawns still take it); built roofs go first; the
shelter setting (default any rock roof, the owner's choice) keeps sheltered cells out of the snapshot; CE's fragments are off during a
slice by default, through its settings object by name, put back after each slice; 40 ms a frame while the screen is white; repeat
strikes fold into passes; the queue moved to D'thorga Framework (`FrameWorkQueue`). Settings and their cost warnings are in the
README. Stage 2, a blast model of our own for the extinction-class warhead, is in [strategic strike](strategic-strike.md) (0.9.0).

## No "Area revealed" letters during a blast (0.10.0, 7 Nov 2026)

**Owner's call** after the strike on Lance Village left about eighty "Area revealed" letters, which piled into the game's "more..."
bundle and could only be cleared one right-click at a time: no such letters for a strategic blast, since it leaves nothing in the
places it opens.

- **Why there were so many** (read from the 1.7 code, `Verse.FogGrid`): removing a wall or rock calls `FloodUnfogAdjacent`, which sends
  one letter for every separate fogged pocket that opens, unless its `sendLetters` argument is false. A blast removes rock all over
  the map, so every cave, ruin and vault sends its own. The bundle button (`Verse.BundleLetter`) refuses right-click by design, and
  its menu entries only open letters.
- **What the fix does:** a prefix on both `FloodUnfogAdjacent` overloads sets `sendLetters` to false while a blast works on that map
  (one of our blast jobs queued for it, or the missile mod's own pass while spreading is off). It skips nothing; the fog clears as
  before. The mechanoid warning shares the letter and goes quiet with it during a blast (agent choice within the owner's "every
  letter of this kind"). A setting, on by default; a dev action dismisses letters sent before 0.10.0.
- **Not yet tested in game.**

## Bug report to post (draft; the owner posts it)

**Hold this draft:** its cause is the room rebuild, which play did not bear out (see above). Revise it once the cost is measured.

To Psionic Zephyr, on the Workshop page of InterRim Ballistic Missile:

> On RimWorld 1.7 with a large mod list, a strategic warhead landing on a settlement I had a map open on froze the game for over
> two minutes until Windows closed it as not responding. The log shows `StrategicImpactCountdown.ApplyStrategicDamage` still
> running two minutes after "Strategic impact sequence initiated", with "Tried to destroy already-destroyed thing" errors every
> few hundred milliseconds. The cause, from the game's code: each destroyed building's leavings are placed through
> `GenPlace.TryPlaceThing`, which asks for a room, which rebuilds every region dirtied so far (`RegionGrid.GetValidRegionAt` calls
> `TryRebuildDirtyRegionsAndRooms`), so the rebuild runs once per building instead of once per frame. Two suggestions: spread the
> pass over frames with a time budget, and hold the room rebuild during each slice (the updater's `working` flag, or batch the
> destruction before any leavings are placed). Skipping things with `Destroyed` set would also end the red errors. I've published
> a compatibility patch that does this, D'thorga Missile Patches, and would happily retire it if the mod itself changes.

## Sources

- The owner's `Player.log` and `Player-prev.log` of 7 November 2026, and the Windows Application event log (events 1001 and
  1002, 12:18:27 to 12:18:28).
- IRBM 1.6 assembly and symbols, Workshop item 3682304832, decompiled to `.local/decompiled/IRBM-1.6` (27 October 2026; file
  dates unchanged on 7 November).
- RimWorld 1.7.5000 `Assembly-CSharp.dll`, decompiled by type to `.local/decompiled/RimWorld-1.7` (`Verse.RegionGrid`,
  `Verse.RegionAndRoomUpdater`, `Verse.GenPlace`, `RimWorld.GenLeaving`, `RimWorld.FilthMaker`, `Verse.ListerThings`,
  `Verse.Building`, `Verse.Thing`); the 1.6 reference assembly (`Krafs.Rimworld.Ref` 1.6.4633) for the updater's fields.
- The IRBM Workshop comments page (fetched 7 November 2026, first page of 855): players asking for 1.7 since mid-October, no
  author reply visible, no comment reporting this hang; the description page was region-blocked, so the licence is unread.
