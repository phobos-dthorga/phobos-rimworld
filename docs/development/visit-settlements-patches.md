# Visit Settlements patches: research and decisions (8 Nov 2026)

The record behind D'thorga Patches' part for Visit Settlements (Nina.Goblin): two faults in its code, found from the owner's saves and
logs on 8 November 2026, and the patches that work round them. Each section is kept as written in the record it came from. How to play
is in the player guide's [Visit Settlements section](../player-guide/patches.md#visit-settlements); the RimPacts side of the lost
defence is in [RimPacts patches](rimpacts-patches.md).

**Status (17 Nov 2026):** built in D'thorga Patches 0.13.0 (a visited settlement's map stays open while the player's people can still
act there or a RimPacts battle is on, `VisitMapStaysOpen`) and 0.14.1 (its food drops no longer throw on every tick,
`SettlementEventsGuard`); since 0.16.0 both log under their own category. Both are temporary, rows 3 and 4 in
[the register](temporary-patches.md), and both bug reports are drafted, not posted. **Not yet tested in game:** the owner confirmed the
workaround in play (8 Nov 2026), not the fix; the log of 8 November suggests the food-drop guard works, and
[the test pass](in-game-test-pass.md) waits on the owner's word.

**The sections:** [a RimPacts battle map closed](#visit-settlements-closes-a-rimpacts-battle-map-8-nov-2026) (8 Nov 2026);
[food drops that throw every tick](#visit-settlements-food-drops-throw-every-tick-8-nov-2026) (8 Nov 2026);
[moved from the README](#moved-from-the-readme-17-nov-2026) (17 Nov 2026).

## Visit Settlements closes a RimPacts battle map (8 Nov 2026)

*Moved here on 17 Nov 2026 from `missile-patches.md`; kept as written.*


**The report.** The owner lost a RimPacts world war defence of Barracuda's Stream (Ryūmon Protectorate, the owner's puppet state)
against The Cannibal Lineup, reloading several times with and without the Harrier: always lost about 13 in-game minutes after PG2,
and the force never came home.

**What the saves showed** (`scripts/query-save.py` on phobos-modkit 0.7.0 and 0.8.0, built for this: world objects, component
values and `diff --component`):

- PG2 (tick 48,332,465): the defence at stage 3, the raid landed at 48,331,250; on the map 60 Cannibal Lineup fighters standing, 59
  Ryūmon defenders, the owner's six DMS II mechs and an M1A2 with four colonists aboard. Visit Settlements' world component listed
  the map (registered between Autosave-15 and PG1, so the caravan entered through its Visit button).
- PG3 after the loss: RimPacts' history records the defence lost at tick 48,333,000 and one occupation mark for Barracuda's Stream
  (control only; no settlement changed owner anywhere). The battle map was gone. The tank and its four colonists were world pawns,
  out of play; the three mechs were world pawns of three other factions (Orbital Guild, Anlium, The Cannibal Lineup).

**Read in the code** (summarised in our own words; decompiles stay in `.local`):

- RimPacts 1.7 (30 Oct 2026) checks a defence every 250 ticks; at stage 3 it is won when the attacking faction has no conscious
  fighter on the map (fogged, podded and vehicle-borne count), and **lost the moment the settlement's map is gone**. It keeps a defence
  or watched battle map open through a postfix on `Settlement.ShouldRemoveMapNow`. The "End defense" button appears only once all
  the player's forces have left.
- Ruled out: our Harrier map patch and the Harrier framework's (both only keep maps), Vehicle Framework (only `CheckRemoveMapNow`),
  Custom Base Framework and RimExodus (direct removal only on a failed generation or their own camp maps), RimPacts' own map removal
  (spectated battles only; none was on). The owner's tests with and without the Harrier ruled it out in play.
- **Visit Settlements 1.7 (22 Oct 2026)** postfixes `Pawn_HealthTracker.MakeDowned`: when any pawn on a map it tracks goes down
  and `FreeColonistsSpawned` are all downed, it calls `Game.DeinitAndRemoveMap` directly. With every colonist inside the tank that list
  is empty, the check is true, and the first downing closes the map, past every keep-open rule. The game passes the pawns left on it
  to the world as lost (`MapDeiniter.PassPawnsToWorld`). The mechs changing faction afterwards is our inference: the game reuses
  world pawns when it generates pawns for other factions.

**The fix (Patches 0.13.0, not yet tested in game).** A tie-in assembly in `Mods/VisitSettlements/<version>`, loaded only with Visit
Settlements, prefixes its private `DeinitializeMap(Map)` and skips it while a colonist of the player's there is conscious (on foot,
aboard a Vehicle Framework vehicle through its `AllPawnsAboard`, or in a spawned holder), a player mechanoid there is on its feet, or a
RimPacts defence or watched battle is on there (`DefendMissionOn`, `SpectateOn`, read by name). Agent choices for the owner: mechs
count whether or not they have an overseer (they can still fight and carry the downed); a battle keeps the map even with everyone
down, as RimPacts' own rule does; no setting, as it only fixes a fault; one log line per map an in-game hour.

**Owner's workaround before 0.13.0:** keep one colonist on foot and conscious on a map entered with Visit; enter RimPacts defences
through RimPacts' own option or shuttle. Confirmed by the owner in play on 8 Nov 2026 ("you're bang on right").

### Bug report for Visit Settlements (draft; the owner posts it)

> Hi Nina, thanks for Visit Settlements. A report from RimWorld 1.7 (build of 22 Oct): the downed check in
> `VS_Pawn_Downed_Patch.Postfix` uses `map.mapPawns.FreeColonistsSpawned.All(p => p.Downed)`. When every colonist on a visited map
> is inside a vehicle (Vehicle Framework), a shuttle or a pod, that list is empty, `All` returns true, and the first pawn of anyone's
> to go down calls `DeinitializeMap`, closing the map with the player's people aboard. The game then passes them to the world as
> lost, and with RimPacts a defence fought there counts as lost. Because `DeinitAndRemoveMap` is called directly, other mods' keep-open
> rules (RimPacts' defences and battles) are skipped too. Requiring at least one free colonist in `FreeColonists` (not only spawned
> ones), or calling `map.Parent.CheckRemoveMapNow()` instead of removing the map directly, would avoid it. Happy to share details.

## Visit Settlements' food drops throw every tick (8 Nov 2026)

*Moved here on 17 Nov 2026 from `missile-patches.md`; kept as written.*


**What the owner saw.** `Player-prev.log` of 8 November ended at the game's 10,000-message limit at 18:24 after 6,846 identical
`NullReferenceException`s from `VS_WorldComponent_SettlementEvents.WorldComponentTick` in six minutes; the next session's `Player.log`
did the same at 20:07 after 8,247. After the limit no mod can log anything for the rest of the session
([logging record](logging-framework-design.md)).

**The cause** (read from the owner's log trace and Visit Settlements' 1.7 assembly of 22 October 2026, decompiled to
`.local/decompiled/VisitSettlements-1.7`; Nina.Goblin's code, our summary). The world component counts ticks and, at
`regularDaysCount` days, calls `DropFoodPodsForSettlements()`, then resets `tickCounter` to 0. That routine sums, for each friendly
settlement map it keeps, `pawn.needs.food.FoodFallPerTickAssumingCategory(...)` over every non-hostile, non-player pawn. A pawn that
does not eat has no food need (`needs.food` is null): a mechanoid, a robot or an android, which the owner's list brings (the Dead Man's
Switch mechanoids, Apex Mechanoids, Enemy Mechanitors). The lambda throws (`<>c__DisplayClass8_0.<DropFoodPodsForSettlements>b__1`,
called from `Enumerable.Sum`), the reset after the call never runs, and the component calls it again on the next tick: once a tick,
for the rest of the session. Its raid timer has the same shape (`raidTickCounter` resets only after `TryTriggerRaidEvent` returns).

**The fix, D'thorga Patches 0.14.1 (not yet tested in game).** `SettlementEventsGuard` in the Visit Settlements part, found by name and
shape with no reference to Visit Settlements' assembly: a prefix on that one lambda (found as the only method named after the routine
that takes a `Pawn` and returns a `float`; the routine's other float lambda takes a `Thing`) returns 0 for a pawn with no food need and
skips the original only then, when it would throw (recorded reason: no extension point inside a lambda); and a finalizer on the
world tick that, if anything still throws there, resets both counters as a success would have and logs one warning an in-game day
(core `Rules/TickThrottle.cs`). The finalizer swallows the exception on purpose: rethrown, the game would log it again next tick,
which is the fault itself; it logs the original in full.

**Bug report to post (draft; the owner posts it)**, to Nina.Goblin on Visit Settlements' Workshop page:

> Hi Nina, another one from RimWorld 1.7 (build of 22 Oct), with a large mod list. `DropFoodPodsForSettlements` sums
> `pawn.needs.food.FoodFallPerTickAssumingCategory(...)` over every friendly pawn on a settlement map, but a pawn that doesn't eat
> (a mechanoid, a robot, an android from another mod) has `needs.food == null`, so the sum throws. Because `WorldComponentTick` resets
> `tickCounter` only after the call returns, it calls it again on every tick: about 7,000 errors in six minutes for me, which hits
> RimWorld's 10,000-message log limit and stops all logging for the session. Skipping pawns with `needs?.food == null` in that sum
> would fix it, and resetting the counter before the call (or in a `finally`) would stop any future fault there from repeating every
> tick. `raidTickCounter` and `TryTriggerRaidEvent` have the same shape. I've put a small compatibility patch for both in D'thorga
> Patches and will retire it if the mod changes. Thanks for the mod!

## Moved from the README (17 Nov 2026)

*Moved here on 17 Nov 2026 from `missile-patches.md`, which took this paragraph from D'thorga Patches' README the same day;
kept as written.*

**How to tell the Visit Settlements part is working.** The load line says "a visited settlement's map stays open while your people
can still act there or a battle is on", and, for the food drops, "friendly settlements' food drops leave out pawns that do not eat"
and "a fault in its settlement events waits its usual days instead of repeating every tick". When it saves a map, the log says "Kept
the map of ... open", once an in-game hour. If Visit Settlements changes its code, the load line says the part is off and why. A
failure in those events is logged as one warning a day.
