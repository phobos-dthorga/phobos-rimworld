# Gathering runs: a vehicle that fetches loose materials by itself (19 Nov 2026)

**Status (19 Nov 2026):** built in D'thorga Framework 0.143.0 and D'thorga Vehicles 0.67.0, on the Talus with its loading grab; several chunks a lift in Framework 0.144.0; not yet tested in game. Unloading at a chosen place is not built yet (the owner's card of 19 Nov 2026: build it next).

The owner's request, 19 Nov 2026: "a helper with the Talus Tipper Truck that is executed via a gizmo and is only available to activate
when the Loading Grab implement upgrade is present. With suitable driver or similar ability for the vehicle to drive, it will source
(raw) materials around the map of whatever kind it can load at any given moment. Perhaps too, the player can set filters for what they
prefer, similar to that of a Stockpile Zone. Genericize this code where you can while keeping it reasonable, because I see
implementations of this sort being used in other areas of our mods, for likewise purposes. Later on, we can maybe set unloading zones
and we are even halfway there already, with our partially implemented bunkers."

**How to read the marks:** **[code]** was read from Vehicle Framework's public source (`SmashPhil/Vehicle-Framework`, cloned 18 Nov 2026)
and checked against the decompile of the build installed on 18 Nov 2026, or from RimWorld 1.7's decompile, with `scripts/query-code.py`;
**[A]** is a figure or choice the agent authored for the owner to revise; **[I]** is our inference. **None of it is tested in game.**

## Read this first

- **What it is:** a **Gather** button on the Talus, shown once its **loading grab** is fitted. Pressed, the truck drives about the map by
  itself, stops beside loose things you chose, lifts them aboard with the grab, and moves on, until its cargo is full or nothing it can
  reach is left. A **Gather what** button opens the game's own stockpile filter, plus an optional area to keep it in.
- **It needs a way to drive:** someone aboard who can drive it, or a vehicle that needs no driver. Vehicle Framework's own rule decides
  (`VehiclePawn.CanDraft`, `CanMoveFinal` **[code]**). If the engine is off, the run starts it, and switches it off again at the end.
- **It only takes loose things:** never from a stockpile or a shelf, never anything forbidden, anything a colonist has already claimed to
  carry, or anything under the fog of war **[A]**. To keep something, forbid it.
- **It stops by itself** when the cargo is full, nothing it can reach is left, enemies threaten the map **[A]**, the grab or engine fails,
  or you give the truck any other order. One message at the end says what it gathered and why it stopped.
- **When full, it stops where it is.** Driving home and tipping into a bunker is the next step, not built (below, "Unloading, later").
- **The pace is the grab's: a lift every 20 seconds at normal speed** (`grabSecondsPerLift`, authored in Framework 0.101.0). Until
  Framework 0.144.0 a lift took one stack or one chunk, so a full base body of granite chunks was 96 lifts, **about 1.9 in-game days**.
  **Since 0.144.0 a lift also takes the same kind lying within 2 cells of the first thing** (`grabJawCells`, authored), up to the lift's
  500 kg: about 20 granite chunks from a heap, so a heaped full load is about 5 lifts, near **2.4 in-game hours** of lifting, plus the
  drives. Scattered chunks still take a lift each (the owner's card, 19 Nov 2026).
- **No Harmony patch.** It drives with the game's own "go to" job, the one Vehicle Framework gives when you right-click a cell **[code]**.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **The gathering run** | Clear a quarry, a mined-out hillside or a battlefield of loose materials with one button and one driver, nobody else carrying | Gather without the loading grab, or without a driver (or a vehicle that drives itself); gather from stockpiles or shelves, forbidden things or things a colonist has claimed | Collecting scattered chunks and resources stops being a hauling chore for the colony and becomes a truck's job while colonists work elsewhere; the driver is tied up for the run |
| **The filter ("Gather what")** | Choose what it fetches as for a stockpile: chunks only, steel and components, wood, anything | Fetch what is not chosen: it drives past it | The truck becomes a specialist (a chunk runner, a scrap collector) by its filter, not by its body |
| **The area** | Keep a run inside one of their areas: the home area, a quarry area | Wander the whole map when an area is set | Runs become predictable: a player can keep the truck away from a dangerous corner |
| **Stopping for enemies** **[A]** | Trust a run left unwatched not to drive into a raid | Gather while enemies threaten the map | A raid ends the run; the player sends it off again afterwards |
| **The end report** | See at once what the run brought and why it stopped | Nothing | No silent end: "full", "nothing left it can reach", "no way through", "other orders", the grab's or engine's fault |

## How a run works

A run has four phases, saved by name (append only): **Idle**, **Seeking**, **Driving**, **Working**.

1. **Seeking.** It lists every spawned thing of each kind the filter allows (`ListerThings.ThingsOfDef`, so it reads tens of lists, never
   the whole map **[code]** `ListerThings.cs`), keeps those it wants (below), and tries the nearest first, up to 16 a look **[A]**. A thing
   already within the grab's reach starts work at once. Otherwise it looks for a place to stand (below); if it finds one it drives there,
   and if not, it notes the thing as out of reach for the rest of the run. With nothing left untried, the run ends: "nothing left".
2. **Driving.** It orders the game's `Goto` job to the place through `Pawn_JobTracker.TryTakeOrderedJob`, as Vehicle Framework's own
   right-click does (`FloatMenuOptionProvider_OrderVehicle.PawnGotoAction` and `VehiclePathFollower.TryOrderMoveTo` **[code]**). Each rare
   tick it watches: still on its way, it waits; stopped with the thing within reach, it starts work; stopped short of it, it notes the thing
   and seeks again; standing still for more than 2,000 ticks **[A]** on an order it has not finished, it gives that thing up. Five drives in
   a row that reach nothing end the run **[A]**: "no way through". Any other order the truck takes (a right-click, a draft change, another
   job) ends the run: "other orders".
3. **Working.** It hands the grab what it wants, and the grab lifts as it always has, one lift at a time at its own pace. When nothing it
   wants is left in reach, the run seeks again; when the cargo is full, the grab or the engine fails, or the player stows the grab, the
   run ends with the grab's own reason.
4. **The end.** It stops the truck, stows the grab, switches the engine off if the run started it, and sends one message: loads, mass,
   places visited, and why it stopped.

**What it wants.** A spawned item on its own map that the filter allows, inside the area if one is set, not under fog, not forbidden, not
in any storage (stockpile zone or building), not claimed by anyone of the player's side (`ReservationManager.IsReservedByAnyoneOf`
**[code]**), not burning, and that the worker could lift at all (the grab: a whole unit no heavier than one lift, never a corpse or a packed
building). The loading grab's own rule about storage with "Vehicles may load from here" switched on does not apply to a run: a run only
takes loose things **[A]**.

**Where it stands.** A vehicle's centre cell is its position, and its body runs `size / 2` cells from it in its longest direction, in
whichever way it ends up facing **[I]** (from `GenAdj.OccupiedRect`). So it stands on the square ring one cell beyond that around the thing
(`GatherRule.StandOffRing`): from there no facing of its body covers the thing. The Talus (3 by 8, grab reach 4) stands 5 cells from the
thing, and from there the thing is within the grab's reach whichever way it faces (`GatherRule.RingAlwaysInReach`). Each cell of the ring
is tried nearest first, with Vehicle Framework's own tests: the body fits on the cell in any facing (`DrivableRectOnCell` with
`AnyRotation`), no other vehicle stands there (`PathingHelper.AnyVehicleBlockingPathAt`), and it can drive there
(`VehicleReachabilityUtility.CanReachVehicle`) **[code]**, the same tests its right-click menu runs through `TryFindNearestStandableCell`.
A longer vehicle with a shorter reach may stand where the thing is out of reach in some facings; it then notes the thing and moves on.

## The generic parts

The owner asked for the parts other mods of ours can reuse. Each lives once, in D'thorga Framework:

| Part | Where | What it is for |
| --- | --- | --- |
| `GatherRule` | The core's `Rules/`, tested in `tests/PhobosFramework.Rules.Tests` | The stand-off ring, whether it always reaches, what a worker's stop means for the run, when a drive is stuck and when to give up |
| `Dialog_PhobosThingFilter` | The core's `UI/` | A window with the game's stockpile filter and, if asked, an area choice. For any thing of ours the player tells what to take: a run's filter today, a bunker's or a chute's later |
| `VehicleAutoDrive` | The Framework's vehicle part, `Service/` | Whether a vehicle can drive itself now and why not, starting its engine, finding a place to stand near a cell, ordering the drive, telling an order of ours from the player's, halting. Any errand a vehicle runs by itself: an unloading trip, a fuel run, a recovery |
| `IVehicleGatherWorker` | The Framework's vehicle part, `Gather/` | What a vehicle picks things up with where it stands. The Talus's grab is the first; a fork-lift, a magnet or a pump hose would be others |
| `CompPhobosGatherRun` | The Framework's vehicle part, `Gather/` | The run itself: filter, area, phases, buttons, the end report. Put on any vehicle whose Def also carries a worker, with its filters in XML |

What stays in the Talus: that its grab is the worker, and its Def's filters (chunks and raw resources to start, anything but corpses to
choose from **[A]**).

## Unloading, later

Not built: the owner said "later on". What exists and what is missing, so the next step starts from the facts:

- **There already:** the tip fills storage behind the truck first (Framework 0.125.0, UH1), and the bulk bunker is storage built for one
  load (Vehicles 0.63.0, UH2). A run that drives to a bunker and tips would deliver into it with no new tipping code.
- **Missing:** the truck must stand with its **tail** to the bunker. Vehicle Framework's plain `Goto` job leaves a vehicle facing the way it
  last moved; an end facing is given only by its `PathOrderData.endRotation`, through `TryOrderMoveTo`, which in the installed build
  queues behind a held "queue order" key **[code]** (`VehiclePathFollower.TryOrderMoveTo`). A Phase `Returning` and `Unloading` would be
  appended to the phases, with a place to unload chosen like the filter, and a way to arrive tail first (a last short reverse, or setting
  the end facing). That is the one honest unknown.

## Other factions' vehicles

The run is a player's order: its buttons show only on the player's vehicles, as the grab's do. A Talus in a raid, or parked at a base the
player attacks, never runs one and costs nothing. A captured Talus has the buttons once it is the player's. Nothing is saved on a vehicle
that has never run.

## Cost to run and saves

- **Nothing while idle.** No tick work at all without a run.
- **Seeking** reads only the lists of the kinds the filter allows, at most once a rare tick, and tests at most 16 things and a few ring
  cells each for a place to stand: the reachability test is Vehicle Framework's region search, the one its right-click menu runs.
- **Driving and working** look once a rare tick (every 250 ticks).
- **Saved** on the run's comp, each key stable: its filter, its area, the phase, the thing it is heading for, where it will stand, when it
  last moved, its tallies, and whether it started the engine. The list of things out of reach is not saved: after a load it may try one
  again. The grab saves that it works for a run. **Safe to add mid-game. Safe to remove** only with no run going: a run left going on a
  save without the mod leaves a truck that may have its engine running.

## Prior art

**Vehicles at Work** (on the owner's list) puts a colonist with hauling work into a vehicle to carry ordinary hauls to stockpiles; the
colonist loads it by hand (`WOR/Haul.cs`, `JobDriver_LoadVehicle` in its decompile **[code]**). It does not drive a vehicle by itself, use a
grab, or gather by a filter. If its hauler takes the Talus mid-run, the run sees another order and ends; not tested.

## Questions for the owner

**Answered by the owner on 19 Nov 2026 (cards):** 1, several chunks a lift, built in Framework 0.144.0; 2, enemies end the run, as built;
5, unloading at a chosen place, built next; and the two buttons' own icons in a small batch later that day, on the owner's go.

| # | Question | Agent lean |
| --- | --- | --- |
| 1 | The grab's pace, a lift every 20 seconds, makes a full body of chunks about 1.9 in-game days. Keep it, or let one lift take several chunks lying side by side? | **Decided: several a lift** (owner). A lift takes the same kind within 2 cells of the first, up to its 500 kg |
| 2 | Should enemies end a run, or only pause it until they are gone? | **Decided: end it** (owner) |
| 3 | Should a run also take from storage with "Vehicles may load from here" switched on? | No: that is what the grab by hand is for. A run clears loose things |
| 4 | The default filter: chunks and raw resources. Add stone blocks? | Keep it; the filter is there to change it |
| 5 | Unloading: a chosen bunker or stockpile, with the run driving home, tipping and going out again? | **Decided: a chosen place, next** (owner) |

## The owner's idea: militarised service vehicles (19 Nov 2026)

With the answer on enemies, the owner asked: "perhaps we could offer upgrades where we militarize some or all of our service vehicles?
Either with advantages only or some disadvantages as a result to boot?" **Not designed or built.** It is a family of new behaviours, so
it needs its own record first, saying what each upgrade lets the player do, what it stops them doing and how it changes play (armour
that weighs and slows, a gun ring that needs a gunner, a run that carries on under fire), checked against what our fittings, the
modern military vehicles record and Combat Extended already give. **Agent lean:** research it as its own record when the owner says go,
with disadvantages as well as advantages, since a truck that is better at everything would leave the tree no choice.

## Who chose what

- **Owner (19 Nov 2026):** the helper on the Talus, behind the loading grab, by a button; a driver or a vehicle that drives itself; raw
  materials around the map; stockpile-like filters; the code generic where reasonable; unloading later.
- **Owner (19 Nov 2026, cards):** several chunks a lift; enemies end the run; unloading at a chosen place next; the buttons' own icons
  later; and the idea of militarised service vehicles, to research.
- **Agent:** the jaw's reach of 2 cells; loose things only; never forbidden, claimed or fogged things; the optional area; ending on enemies; engine started and
  stopped by the run; the stand-off ring; 16 things a look, 2,000 ticks' patience, five failed drives; the Talus's two filters; the game's
  haul and stone-chunk icons on the two buttons until our own are drawn; every leaned answer above.

## Sources

- Vehicle Framework, `SmashPhil/Vehicle-Framework` (MIT), read through `scripts/query-code.py` on 19 Nov 2026 and checked against the
  installed build of 18 Nov 2026: `VehiclePathFollower.StartPath` and `TryOrderMoveTo`, `FloatMenuOptionProvider_OrderVehicle.GetSingleOption`
  and `PawnGotoAction`, `JobGiver_GotoNearestHostile.TryGiveJob`, `PathingHelper.TryFindNearestStandableCell`, `VehicleIgnitionController.Drafted`,
  `VehiclePawn.CanMoveFinal`, `CanMove` and `CanDraft`.
- RimWorld 1.7's decompile: `ThingFilterUI.DoThingFilterConfigWindow`, `ThingFilter`, `ListerThings.ThingsOfDef`, `Area.AssignableAsAllowed`,
  `GenHostility.AnyHostileActiveThreatToPlayer`; the game's own `UI/Designators/Haul` and `UI/Icons/ThingCategories/StoneChunks` pictures.
- Vehicles at Work's decompile (`.local/decompiled/VehiclesAtWork-1.7`), summarised in our words.
- Our own: [the tipper](vehicle-tipper-hauler.md), [universal hoppers](universal-hoppers.md), `CompPhobosTipper.cs`, `GrabRule`, `TipRule`.
