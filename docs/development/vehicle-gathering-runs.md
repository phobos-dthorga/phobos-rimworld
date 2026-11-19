# Gathering runs: a vehicle that fetches loose materials by itself (19 Nov 2026)

**Status (19 Nov 2026):** built in D'thorga Framework 0.143.0 and D'thorga Vehicles 0.67.0, on the Talus with its loading grab; several chunks a lift and unloading at a chosen place in Framework 0.144.0 and Vehicles 0.68.0; "Vehicles may unload here", any marked bay and Deliver in Framework 0.175.0 and Vehicles 0.86.1; not yet tested in game. Militarised service vehicles have their own record.

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
- **When full, it unloads where you chose, or stops where it is.** Since Framework 0.144.0, with a storage building or stockpile chosen
  under **Unload at**, it backs up there tail first, tips what it gathered and goes back out (below, "Unloading").
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
| `Dialog_PhobosThingFilter` and `FilterDropdown` | The core's `UI/` | A window with the game's stockpile filter and any choices above it (an area, a place to unload). For any thing of ours the player tells what to take: a run's filter today, a bunker's or a chute's later |
| `VehicleAutoDrive` | The Framework's vehicle part, `Service/` | Whether a vehicle can drive itself now and why not, starting its engine, finding a place to stand near a cell or to back up to a set of cells, ordering the drive with an end facing, telling an order of ours from the player's, halting. Any errand a vehicle runs by itself: an unloading trip, a fuel run, a recovery |
| `IVehicleGatherWorker` | The Framework's vehicle part, `Gather/` | What a vehicle picks things up with where it stands. The Talus's grab is the first; a fork-lift, a magnet or a pump hose would be others |
| `IVehicleUnloadWorker` and `PhobosBackUpSideExtension` | The Framework's vehicle part, `Gather/` | What a vehicle puts its load down with, over its tail (the Talus's tip), and which side of a building it backs up to |
| `CompPhobosGatherRun` | The Framework's vehicle part, `Gather/` | The run itself: filter, area, phases, buttons, the end report. Put on any vehicle whose Def also carries a worker, with its filters in XML |

What stays in the Talus: that its grab is the worker, and its Def's filters (chunks and raw resources to start, anything but corpses to
choose from **[A]**).

## Unloading

**Built in Framework 0.144.0 and Vehicles 0.68.0** (the owner's card of 19 Nov 2026: a chosen place, next). Not yet tested in game.

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Unload at a chosen place** | Leave a run going until the map is clear: the truck shuttles between piles and a bunker or stockpile with no one but its driver | Unload anywhere but their own storage that takes something it gathers; tip its spare parts with the load | A quarry run becomes "send it and come back to a full bunker"; a bunker feeding belts can be kept topped up by a truck |
| **Tail first, from a bunker's open end** | Trust the truck to back up the right way to a bunker with walls on three sides | Tip over a bunker's wall | Bunkers are built with their open end facing the yard, as a real one is |
| **The place full ends the run** | See at once that the bunker needs emptying | Pile what would not fit beside the bunker | A full bunker is a job for belts or haulers, not a heap |

**How it works.** Two phases are appended (`Returning`, `Unloading`). When the grab stops full, or a look finds the cargo full, a run with a
place goes back; when a look finds nothing left to gather and something it gathered is aboard, it goes back for a last trip and then ends.

- **Where it stands.** `VehicleAutoDrive.TryFindBackUpSpot` takes every cell of the place that has no place cell beyond it in a facing,
  and puts the vehicle's centre so the first cell behind its body is that cell: its tail against the place, facing away, its body clear of
  it on ground it can drive. The middle of a side comes first (a bunker's centre cell before its corners), then the nearest; each is
  tested as `TryFindStandCell` tests a place. A building whose Def carries `PhobosBackUpSideExtension` is backed up to only on that side
  (`GatherRule.BackUpFacing`); the bulk bunker's says **Back**, because its one picture turns with it and opens at the bottom when it faces
  north (`BulkBunker.xml`; the pilot record's layout) **[I]**.
- **Arriving tail first.** It orders the drive and then sets Vehicle Framework's end facing (`VehiclePathFollower.SetEndRotation`), which
  Vehicle Framework applies when the vehicle arrives (`PatherArrived` sets `FullRotation`) **[code]**: the same turn on arrival the
  player's own right-click with a facing gives. `TryOrderMoveTo` was not used: it reads the "queue order" key, which only means something
  in a click. A spot it cannot reach, or where it stalls for 2,000 ticks, is passed over; five failures in a row end the run.
- **Tipping.** The Talus's tip is the unloading worker (`IVehicleUnloadWorker`). For a run it puts down only what the filter allows, so
  spare parts stay aboard, and fills storage in its fan before the ground, as it always has. A full place ends the run with the tip's own
  words ("14 chunks stay aboard"); emptied, the truck goes back out.
- **Its words.** The end message adds how many things it tipped, where and on how many trips; its inspect line says it is taking its load
  to the place, or tipping there. The Gather button's description says what it does when full.

**Limits.** A stockpile has no open end, so the truck backs up to its nearest straight side, and the tip's fan may put a few things on
cells just outside it. A place whose filter takes only some of what the run gathers gets those; the rest lands in the fan beside it, as
any tip does. Only one place, not a list (the marked bays below are the list).

## Marked bays and Deliver

**Built in Framework 0.175.0 and Vehicles 0.86.1** (owner, 19 Nov 2026: "something (semi-)automated like the already 'Vehicles may load
from here'", for unloading; then four answers: both uses below, the bunker marked to start, zones too, and a per-truck choice of what it
does afterwards). Not yet tested in game.

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **"Vehicles may unload here" on storage and zones** | Say once, on the storage itself, where trucks may tip, as they already say where a grab may lift | Have a truck tip into a shelf or stockpile they did not choose | Unloading points are part of laying out a base, like hoppers and belts, rather than a setting on each truck |
| **Deliver** (Talus, no grab needed) | Send a truck loaded at the rig, the quarry or a chute to put its load away with one button | Tip spare parts, or anything no marked storage takes, with the load | A drill-rig-to-bunker shuttle needs one press per load instead of drive, back up, Tip |
| **Moving on when a bay fills** (Deliver and gathering's "any marked bay") | Spread a load over several bunkers or a bunker and a stockpile | End a run with a full bunker while a marked one beside it is empty | Several small bays work as one big one; a mixed load sorts itself by each bay's filter |
| **Drive back after delivering** (per truck, on to start) | Have the truck come back to where it was loaded, ready for the next load; or leave it at the bays | Nothing | The loading point and the bays become two ends of a route the truck runs on its own |

**How it works.**

- **The switch.** On a storage building it is a second switch on `CompPhobosVehicleLoadOut` (saved key `PhobosVehiclesMayUnload`), added
  in code to every storage type as the load switch is. It starts on for a type whose Def carries `PhobosBackUpSideExtension`, the mark of
  storage built for trucks to back up to (the bulk bunker) **[A]**, and is saved only where it differs from that, so bunkers built before
  0.175.0 load marked. Zones carry no comps: `MapComponent_PhobosUnloadZones` keeps the marked zones by reference (key
  `PhobosVehicleUnloadZones`), dropping deleted ones, and a Harmony postfix on `Zone_Stockpile.GetGizmos` adds the button after the game's
  own (TD Enhancement Pack - Continued postfixes the same method; both only add; `scripts/query-patches.py`, 19 Nov 2026).
- **Choosing a bay** (`UnloadBayRule`, game-free, tested). Every marked place of the player's on the map is a candidate: whether it takes
  any kind aboard (one thing standing for each kind), whether one of its cells has room for one by the game's own `IsValidStorageFor`, and
  its distance (a building from its centre, a zone from its nearest cell). The nearest that takes something and has room wins; a bay found
  full, or with no spot to back up to after five tries, is passed over for the rest of the trip. With none, the reason says which: nothing
  marked, nothing that takes the load, or all full or out of reach.
- **Tipping at a bay.** The tip puts down only what that bay takes (and, for gathering, what the run gathers), so the rest stays aboard for
  the next bay instead of landing in the fan. After each tip that emptied what the bay takes, or filled it, the truck makes for the next
  bay while anything aboard is for one; then a delivery finishes, and a gathering run goes back out or ends by the usual rule.
- **Deliver** is a run of the same comp with no gathering: it starts at `Returning`, takes everything aboard except the items the part
  tables name (`PhobosVehiclePartSupplyDef`'s items and substitutes) **[A]**, and ends `Delivered` or `NoBay`. With the truck's switch on, it
  then drives back to the cell and facing it started from (`Homing`, appended); a fault, an order, enemies or a stop leave it where it is.
- **Buttons.** Deliver carries the game's own eject picture (`UI/Commands/PodEject`, the cryptosleep casket's) and the drive-back switch
  our depot's "go to bay" picture, until ours are drawn **[A]**. Deliver is greyed out with its reason; the game opens no right-click menu
  on a greyed-out button (`Command.GizmoOnGUIInt`, read 19 Nov 2026) **[code]**, which is why the drive-back choice is its own switch
  rather than a right-click menu on Deliver. Deliver's check lists every marked bay, so the button asks at most twice a second.

**Limits.** A marked zone's tip may land a few things just outside it, as for a chosen zone. Bays on another map are never used. A
delivery's reason line is not saved, so one loaded during its drive back reports its end's own line. Hand tipping with **Tip** ignores
the switch: it still fills whatever storage is in its fan.

## Other factions' vehicles

The run is a player's order: its buttons show only on the player's vehicles, as the grab's do. A Talus in a raid, or parked at a base the
player attacks, never runs one and costs nothing. A captured Talus has the buttons once it is the player's. Nothing is saved on a vehicle
that has never run. The same holds for Deliver, and a truck only ever chooses bays of the player's own: another faction's marked storage
is never a candidate, and its switches show only to its owner.

## Cost to run and saves

- **Nothing while idle.** No tick work at all without a run.
- **Seeking** reads only the lists of the kinds the filter allows, at most once a rare tick, and tests at most 16 things and a few ring
  cells each for a place to stand: the reachability test is Vehicle Framework's region search, the one its right-click menu runs.
- **Driving and working** look once a rare tick (every 250 ticks).
- **Saved** on the run's comp, each key stable: its filter, its area, its unloading place, the phase, the thing it is heading for, where it
  will stand and which way it will face, whether this is its last trip, when it last moved, its tallies, and whether it started the
  engine. The lists of things out of reach and of spots it failed to back up to are not saved: after a load it may try one again. The grab
  and the tip each save that they work for a run. **Safe to add mid-game. Safe to remove** only with no run going: a run left going on a
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

**Researched the same day, at the owner's word:** [militarised service vehicles](vehicle-militarised-service-vehicles.md), a design
record with the owner's six answers (a choice of gun mounts; weight counting like cargo; all five trucks, the Talus first; an armoured
truck may keep working while enemies are about, behind a switch; our own guns plus optional extras; no hard dependency). Nothing of it
is in code.

## Who chose what

- **Owner (19 Nov 2026):** the helper on the Talus, behind the loading grab, by a button; a driver or a vehicle that drives itself; raw
  materials around the map; stockpile-like filters; the code generic where reasonable; unloading later.
- **Owner (19 Nov 2026, cards):** several chunks a lift; enemies end the run; unloading at a chosen place next; the buttons' own icons
  later; and the idea of militarised service vehicles, to research.
- **Owner (19 Nov 2026, marked bays):** an unloading switch like the loading one; both a gathering choice and a Deliver button using it;
  the bulk bunker marked to start and everything else unmarked; stockpile zones as well as buildings; and an option, per truck, to drive
  back or stay afterwards.
- **Agent (marked bays):** the bunker recognised by its back-up-side mark; spare parts read from the part tables; only what a bay takes
  tipped there; a bay passed over after five failed tries; drive back on to start; the drive-back choice as its own switch; the two
  borrowed pictures; the half-second recheck on the button.
- **Agent:** the jaw's reach of 2 cells; for unloading, the middle of a side first, the bunker backed up to from behind, only what it
  gathers tipped (spare parts stay), a full place ending the run, one place rather than a list; loose things only; never forbidden, claimed or fogged things; the optional area; ending on enemies; engine started and
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
