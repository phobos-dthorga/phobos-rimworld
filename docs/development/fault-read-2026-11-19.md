# Fault-read of the Osprey's first batch (19 Nov 2026)

**Status (19 Nov 2026):** 30 faults and 33 smaller points found by reading, none observed in game. Fixed: A1, A3 and A4 (Framework
0.162.0, the armour session); the Osprey's own, D1 to D5, D7, T1, T2, T4, T6, U1, L1, L2, W1 to W3, W5 and W7 (Framework 0.163.0 with
Vehicles 0.82.0, [as fixed](#as-fixed-framework-01630-and-vehicles-0820)). The flight session's: F1 to F11, H1 to H3 and H5, R1 to R4, S1 (as F10), D6, T3 and the flight's smaller W1, W5, W6 and W8 (Framework 0.164.0 with Vehicles 0.83.0, [as fixed](#as-fixed-framework-01640-and-vehicles-0830)); and, at the owner's word ("Fix all now"), the flight's W2 to W4, W7, W9 to W12, H4 and S2 (Framework 0.166.0, [as fixed](#as-fixed-framework-01660-the-flights-last-ten)). Left
open for the owner: A2 with W6, U2, W8. Not yet tested in game.

The Osprey's design asks for "a fault-read by a separate reader before the install" as part of its first batch
([the V-22 Osprey](vehicle-osprey.md#the-first-batch)); the owner gave the go on 19 November 2026. Four independent reads, one per
area, by fresh readers who had seen none of the work: the Osprey's Defs and patches; the core of
[our own flight over the map](vehicle-flight-over-the-map.md); what was added on top of it (raiders by Osprey, ropes and the hoist,
stock helicopters, the landing-surface rule); and the Osprey's own systems with [armour by class](vehicle-armour.md). Each read ours
against Vehicle Framework's public source (SmashPhil, commit `026420b5`), Combat Extended's source and the game's code, with
`scripts/query-code.py`, `query-defs.py` and `query-patches.py`. Nothing was built, run or edited.

**How sure:** every finding marked **[checked]** was confirmed again by the session that commissioned the reads, reading both sides on
19 Nov 2026. The rest are the readers' own confirmations by reading. **None of this is observed in game.** Line numbers are from the
day; the flight core's are of commit `e04f9c6f`.

## Read this first

- **The guns could not be fitted at all (D1).** The Osprey's Def has no turret comp, so every gun node throws, spends its materials and
  adds nothing. Row 221 could not pass.
- **The rotors would vanish when the engines start (T1)**: the spinning disc is drawn with a shader that discards it.
- **The worst for a colonist:** F1 (someone killed aboard in the air leaves no corpse and loses their gear) and H2 (patients on litters
  are set down at the map's edge when the aircraft leaves).
- **The worst for a save:** F2 (an airborne aircraft taken off the map by anything but our own hand-over comes back "in the air" at
  stale coordinates).
- **Every departure logs an exception (F6)**, harmless to the journey, loud in the log.
- **Test rows these findings change:** 203 (rotors and tilt), 204 to 212 (flight), 220 and 221 (upgrades and guns), 224 and 225 (kits),
  226 (landing surface). Testing them before the fixes would mostly find these faults again.
- **A blind spot in a tool:** `query-patches.py who "GenAdj.OccupiedRect"` reports nothing, yet Vehicle Framework prefixes
  `GenAdj.OccupiedRect(Thing)` by hand at run time to return its own rect. So `vehicle.OccupiedRect()` is right in every heading; only the
  three-argument `GenAdj.OccupiedRect(cell, rot, size)` is wrong for a vehicle (F10, S1).

## D: the Osprey's Defs

| # | Fault | Where | Whose |
| --- | --- | --- | --- |
| D1 | **[checked]** No gun can be fitted. `Vehicles.TurretUpgrade` calls `vehicle.CompVehicleTurrets`, null without `CompProperties_VehicleTurrets`, which the Def lacks. Two red errors, the node counts as fitted, its materials are spent, the seat edit and the belly gun's 360 kg still apply; taking it off throws again | `Vehicle_Osprey.xml` comps; `UpgradeTree_Osprey.xml` | Osprey |
| D2 | **[checked]** The parts bill has no `Turbine` tag, so `Engine1` and `Engine2` are filled by the piston-engine supply: the blueprint asks for piston engines, and a build and a repair disagree | `Bill_Osprey.xml` | Osprey |
| D3 | **[checked]** The ramp gun needs no crew chief: the seat has no `slotsToOperate`, so an empty seat counts as fulfilled and the gun aims, fires and auto-targets with nobody aboard. The seat edit is also listed after the turret, so the gun's manned state is cached before the seat names it | `Vehicle_Osprey.xml` roles; `UpgradeTree_Osprey.xml` | Osprey |
| D4 | **[checked]** The belly gun never stops for lack of power and draws none: aircraft loadouts have no `turretDrive` load (removed on purpose in 0.117.2), and the stop needs one. Row 221's last clause cannot pass | `Vehicle_Osprey.xml` loads; `TurretPace.cs` | Osprey |
| D5 | **[checked]** The infrared jammer and the terrain radar have `available` but no `when`, so they draw 60 A with the engines off; their text says "while the engines run" | `Vehicle_Osprey.xml` loads | Osprey |
| D6 | **[checked]** The rope and hoist mounts say `cellsOf AftCabin`, a part, where a role is looked up. No error: both sit on the root cell with no armour copied | `Flight_Osprey.xml` | Flight |
| D7 | **[checked]** No part is bound to the `LandingGear` role, so a rolling landing wears a random outer part; the record says the gear is bound | `Vehicle_Osprey.xml` part roles | Osprey |

Smaller: with Combat Extended's ammunition system off, our patch still forces cartridges (W1). A player's turret starts with
auto-targeting off, so "fires by itself" needs one click (W2). The terrain radar's text promises flying blind, which is not built (W3).
Vehicle Framework's installed build never calls `DrawUpgradeList` (the call is commented out in its source), so the armour-of-type-Set
trap of 0.75.0 is dormant and `upgradeExplanation` is not shown (W4). Without Combat Extended a belly-gun load is 1,500 steel (W5).
Everything the parent loadout appends sits on the left nacelle's cell (W6, with A2). Stale text in the tree's inlet-barrier comment and
the record's "Flying it" figures (W7). The Osprey's research line runs through the drill rig's box (W8).

## T and U: the tiltrotor renderer and the upgrade plumbing

| # | Fault | Where | Whose |
| --- | --- | --- | --- |
| T1 | **[checked]** The spinning rotor disc is translucent (73% of its pixels under half alpha, measured) and is drawn with the Cutout shader: the rotors vanish but for the hub when the engines run | `CompPhobosTiltrotor.cs` `Bare` | Osprey |
| T2 | **[checked]** The nacelles' paint masks are never found: the game's `Graphic_Single` looks for `_m`, the export writes Vehicle Framework's `m` suffix. The nacelles tint without their mask | `CompPhobosTiltrotor.cs` `Painted`; `export_osprey.py` | Osprey |
| T3 | **[checked]** The 60 and 90 degree nacelle pictures are never reached: the tilt on the map is capped at half, a tie goes to the lower picture, and the full-tilt branch ran only inside Vehicle Framework's launch animation, which our flight now skips. Row 203's "forward" case cannot be noted | `CompPhobosFlight.ForwardTiltShare`; `CompPhobosTiltrotor.Tilt` | Flight, with the Osprey's comp text |
| U1 | **[checked]** Six evidence entries can never match: `"detail": "fitted"` against a note written as "V-22 Osprey: fitted" (rows 220, 221, 224, 225) | `config/test-evidence.json` | Osprey |

Smaller: the renderer is added only at spawn, so an Osprey loaded while off the map has none until it next spawns (T4). Dialogs, the
blueprint and the world map draw without the renderer list, so no rotors or inlet barrier there, and another faction's Osprey spins
its rotors parked, being drafted at spawn (T5). Strings built each frame, and the spin angle loses precision after about 18 million
ticks (T6). The new stats show as zeros on every other aircraft's info card (U2).

## A: armour by class

| # | Fault | Where | Whose |
| --- | --- | --- | --- |
| A1 | **[checked]** EMP hardening did nothing under Combat Extended, whose damage step skips Vehicle Framework's EMP handling altogether | `VehicleEmp.cs` | Armour: **fixed, Framework 0.162.0** |

Smaller: every appended part on the Osprey lands on the left nacelle's cell, so Combat Extended would hit the right engine and gearbox
far more often (A2; the Osprey's). The hit-width note could be lost for a session (A3) and the armour gate check had four gaps (A4):
both **fixed in Framework 0.162.0**.

## F: the core of our own flight

| # | Fault | Where | Whose |
| --- | --- | --- | --- |
| F1 | **[checked]** Someone killed aboard in the air leaves no corpse and loses their gear: Vehicle Framework moves a dying passenger into the cargo, our in-air loading guard refuses the move, and the game destroys a corpse it cannot put in a seat | `FlightPatches.cs` `Patch_AddOrTransfer_Flight` | Flight |
| F2 | An airborne aircraft despawned by anything but our hand-over (a reformed caravan, a map removed) keeps its flying state and old coordinates, and comes back registered as airborne | `CompPhobosFlight.cs` `OnDeSpawn`, `PostSpawnSetup` | Flight |
| F3 | **[checked]** "Land" given while it is already settling changes the spot but not the state: it finishes coming down where it is and appears on the new spot | `CompPhobosFlight.cs` `Land`, `LeaveOrbit`, `TouchDown` | Flight |
| F4 | Spots and strips near the map's edge are accepted but can never be reached: the centre is held 5.5 cells in, the checks ask only that cells be on the map. A landing, a rolling landing or a take-off roll there never finishes | `CompPhobosFlight.cs` `Clamped`; `FlightSupport.cs` | Flight |
| F5 | **[checked]** Engines stopped in the air that cannot restart (a pilot dead, a refusal of ours) are retried every tick, with Vehicle Framework's reject message and sound each time, and it hovers with the ignition off | `CompPhobosFlight.cs` `Engines` | Flight |
| F6 | **[checked]** Every departure over the edge throws: `Step` calls `Arrive`, which hands over and despawns, then reads `Vehicle.Map.Size`. The journey goes ahead; the log gets an exception each time | `CompPhobosFlight.cs` `Step`, `Clamped` | Flight |
| F7 | The pawn path grid is never told the aircraft moved: the pad it left keeps its cost, where it lands is planned as free, and a pawn stepping into a cell under it pays the vehicle's 250 ticks | `CompPhobosFlight.cs` `SyncPosition`, `PutDown` | Flight |
| F8 | **[checked]** A forced landing wrecks the aircraft over any tree or stone chunk: Vehicle Framework's `Standable` is false for anything pass-through-only. The design wrecks it only over a building, impassable ground, another vehicle or deep water | `FlightSupport.cs` `CellRefusal` | Flight |
| F9 | For up to a second after a load an aircraft that cannot hover is treated as able to (the margin and hold are not saved and are judged once a second), then comes down | `CompPhobosFlight.cs` | Flight |
| F10 | **[checked]** Spot, strip, roof and arrival checks use `GenAdj.OccupiedRect(cell, rot, size)`, wrong by a row or column for an even-sided vehicle facing south or west. The Osprey (5 x 9) is safe; the Mosquito (2 x 5) and the Cherokee (3 x 10) are not (with S1) | `FlightSupport.cs`, `FlightJourneys.cs`, `CompPhobosFlight.cs` | Flight |
| F11 | "Allow at a cost" adds a whole over-limit lift-off's strain each second: an overhaul in ten seconds, and forward flight is charged too | `CompPhobosFlight.cs`; `HoverLimits.cs` | Flight |

Smaller: losing both pilots changes nothing (W1). Ground fire still attaches to it (W2). The flight ignores the drivetrain-model
setting (W3). A hop within the same map goes out over the edge and back (W4), braking at the edge before handing over (W5). A forced
landing mid-departure leaves the departure saved (W6). A rolling landing can end on an obstacle (W7). The Hover state never climbs
(W8). `Destroy(KillFinalize)` leaves no wreck, against the design, and shot down by gunfire everyone is put out unharmed (W9). Jobs
aimed at the aircraft do not end at lift-off (W10). A rebuilt comp list drops tick requests (W11). Arrival may wipe plants (W12).

## R, H, S and L: raiders, ropes and hoist, stock helicopters, the landing surface

| # | Fault | Where | Whose |
| --- | --- | --- | --- |
| H1 | **[checked]** The hoist can never reach the person it was sent for: the foot of the lines is offset by the shadow, which shrinks as it descends, so the menu's reach (at flight height) and the hook-up's (at work height) are 6 to 7 cells apart against a reach of 3 | `CompPhobosFlight.Lines.cs` `FootPoint`, `HoistCandidates`, `TakeOnHook` | Flight |
| H2 | **[checked]** Patients on litters are set down at the map's edge when the aircraft leaves on our flight: the litter keeps them only if Vehicle Framework's `inFlight` is set, which our hand-over sets after the despawn | `CompPhobosLitter.cs`; `CompPhobosFlight.cs` `HandOver` | Flight |
| H3 | Someone coming off a line can be put under a roof, in a room or in a wall: the order is checked at one point and they are set down at another, with a fallback that looks at nothing | `CompPhobosFlight.Lines.cs` `SetDown` | Flight |
| S1 | The even-sided stock helicopters' landing checks are a row or column off facing south or west (F10's cause) | as F10 | Flight |
| R1 | A raiders' aircraft that cannot hover on arrival circles until shot down or out of fuel: `RaiderThink` has no case for circling inbound | `CompPhobosFlight.Raider.cs` | Flight |
| R2 | Whoever leaves aboard stays in the raid's lord; raid animals go to the cargo and are never unloaded | `CompPhobosFlight.Raider.cs`; `RaiderFlight.cs` | Flight |
| R3 | The player is told to "pick another spot" for an enemy's aircraft, and an ally's aircraft shot down reads as good news | `CompPhobosFlight.cs` `TouchDown`; `CompPhobosFlight.Raider.cs` | Flight |

Smaller: an aborted take-off roll is judged by the landing-surface rule at its start, though the record said not (L1; the Osprey's).
"Never a break" stops at one health, but the engine model counts an engine out at 20%, so dust alone could put both engines out
together a second after a lift-off that was allowed (L2; the Osprey's). With the setting on, the Cherokee and the Louie lose water-drop
runs (S2). Anything in a raid's pawn list is seated (R4). A stalled hoist has no way out (H4). Nothing checks the hover margin before a
lift (H5).

## Found sound

The readers' own lists, kept short: the seat edit and a fitted turret through save, load and refund (once D1 is fixed); every Def name
and patch target; one outer part on each of the 45 footprint cells; the upgrade stats through fitting, load and refund, counted once;
the wider Combat Extended hit box; the classes and kits on both scales; the registry and ticking pairs; every saved field and its
default; the hand-over's order against Vehicle Framework's own; every Harmony target and injected type; the landing surface's
footprint, its `Rand` use and its static list; raid generation, raid points and "never a prize"; nothing ticking while idle.

## Could not be checked by reading

The game's shaders (T1 and T2 rest on the files and the lookup, which are certain); whether the reform-caravan dialog lists an airborne
vehicle (F2) and whether the landing targeter offers edge cells (F4); the cost of each cell crossed (needs `perf.py`, with the owner's
leave); Vanilla Vehicles Expanded's own postfixes on boarding and leaving (no source here); a map closing with a raiders' aircraft on
it. The sling lifted from a hover (Framework 0.161.0) landed while the reads ran: the flight core's reader took it in, the others
did not.

## As fixed (Framework 0.163.0 and Vehicles 0.82.0)

The Osprey's own rows, by the session that commissioned the reads; not yet tested in game.

- **D1:** the Def has an empty `Vehicles.CompProperties_VehicleTurrets`, as every vehicle whose tree adds a gun carries.
- **D2:** the bill carries the `Turbine` tag.
- **D3:** each ramp gun's seat edit sets `slotsToOperate` 1 and comes before its turret, so the gun finds its seat as it is fitted.
- **D4:** the Osprey's loadout has its own `turretDrive` load, "belly gun station", 25 A [A], fitted while a powered weapon is aboard (the
  belly gun alone) and drawing while that weapon has a target, as for every weapon no seat works. With no power and nobody aboard the gun
  stops.
- **D5:** both loads have `When_EngineRunning`.
- **D7:** the three undercarriage parts are bound to `LandingGear`.
- **W1:** the Combat Extended patch no longer forces cartridges: Combat Extended's own vehicle compatibility sets them from each turret's
  ammunition set while its ammunition system is on, and with that system off the guns keep their steel.
- **W2:** a new upgrade class, `PhobosAutoTargetUpgrade`, switches the belly gun's automatic aim on as it is fitted; never after a load.
- **W3 and W7:** the terrain radar's texts no longer promise what is not built; the inlet barrier's say what it now does.
- **W5:** without Combat Extended a 7.62 mm round is a twentieth of a steel and a .50 a quarter [A].
- **T1:** the spinning disc and the damage marks are drawn with the transparent shader.
- **T2:** the nacelles' masks are asked for by Vehicle Framework's suffix.
- **T4:** the renderer is added after a load too (`VehicleComp.PostLoad`, which Vehicle Framework calls after rebuilding its draw tracker).
- **T6:** the spin angle is worked in double, with a test at 18 million ticks; the nacelle's path is built once a frame, not once a side.
- **U1:** the six entries read `"*: fitted"`.
- **L1:** the record and the code's comments now say a take-off roll is judged as it starts.
- **L2:** dust never wears an engine below 30% of its health (`engineWearFloor`, an agent choice for the owner to revise): above the 20%
  at which the engine model counts an engine out, so dust alone grounds nothing.

**Left open, for the owner's word:** everything the parent loadout appends sits on the left nacelle's cell (A2, W6); the new stats show
as zeros on other aircraft's info cards (U2); the Osprey's research line runs through the drill rig's box (W8). **Known limits, kept:**
the paint and upgrade dialogs, the blueprint and the world map draw no rotors, and another faction's Osprey spins its rotors parked (T5);
Vehicle Framework's installed build never draws a node's upgrade list, so the armour kit's written-out explanation is not shown (W4).

## As fixed (Framework 0.164.0 and Vehicles 0.83.0)

The flight session's rows, by the session that wrote the flight; not yet tested in game. "The flight's W" means the smaller points in the
F section, not the Osprey's own W rows.

- **F1:** the in-air loading guard lets a `Pawn` or a `Corpse` through: Vehicle Framework's death-move into the cargo now holds the body.
- **F2:** `OnDeSpawn` of an aircraft not on the ground calls `Grounded()`: the ground state, nothing under way, anyone on its lines put
  back aboard (a seat or the hold), ticking stopped. The hand-over and the raiders' vanish set the ground state first, so they are untouched.
- **F3:** `LandRefusal` refuses while `Settling`, and the Land button is disabled then.
- **F4:** `FlightRules.InsideEdge`: a chosen spot's centre, and both ends of a strip, must lie inside the band the flight holds the centre
  in (not asked of a forced landing, whose centre is already held in). A take-off roll that makes no progress lifts off where it is; a
  landing roll that makes none stops there.
- **F5 with the flight's W1:** `Engines` brings it down once when nobody aboard can fly it, or when re-drafting is refused (a roll is
  stopped instead; a landing roll carries on).
- **F6:** `Step` returns after `Arrive` once the hand-over has taken it off the map.
- **F7:** `SyncPosition` works out the path cost of the cells it has left, as Vehicle Framework's mover does; `PutDown` those it left and
  stands on; `HoverPathCost` those under a hover or at its lines, once each time it settles. Passing over, it slows nobody, as designed.
- **F8:** `CellRefusal` on a cell not standable for the vehicle but walkable for a person (trees, bushes, rubble): a chosen landing or strip
  is refused with "trees, bushes or rubble stand on part of the spot"; a forced landing lands hard and crushes the plants. A raid's
  landing search asks as a chosen landing whose crew know the ground (`knowsGround`, the fog check only), not as a forced one.
- **F9:** the first tick after a spawn or load judges at once (`judged`, not saved).
- **F10 (S1):** `vehicle.VehicleRect(cell, rot)` in the spot, strip, roof and arrival checks.
- **F11:** `HoverLimits.Strain(comp, share)`; an over-limit hover second is a sixtieth of an over-limit lift-off (authored), only while
  hovering, lifting off, settling or at its lines.
- **H1:** the lines' foot is the ground beneath the ramp at any height; only their drawing slides as the shadow does.
- **H2:** `HandOver` holds the litter's patients through the despawn.
- **H3:** off a line with no open ground at the foot: the nearest standable, unroofed cell within ten, else any standable within twenty.
  A raid's ropes need eight open, unroofed cells within four of the hover.
- **H5:** the hoist refuses a person the aircraft could not hover with (`WouldHoverWith`).
- **R1:** a raiders' aircraft circling on arrival (it cannot hover) goes back to the edge.
- **R2 and R4:** only people, awake, are seated in a raid's aircraft; at the vanish, raiders still aboard are put down at the edge, staying
  in their raid; only the crew goes home.
- **R3:** the blocked-spot message is the player's own aircraft's only; an ally's aircraft lost is a negative event, with its own wording.
- **The flight's W5, W6, W8:** on its way out over the edge (or a raider leaving) it does not brake; a forced landing clears a journey
  under way; a hover below its height climbs.
- **D6:** a `RampSection` role on the aft cabin lends both mounts its armour; their cells are the ramp's row.
- **T3:** the nacelles tip fully forward on the way out over the edge, coming in from it, or a raider leaving; at most half way flying
  about the map.

## As fixed (Framework 0.166.0): the flight's last ten

At the owner's word ("Fix all now", 19 Nov 2026, on the question card of the leftovers); not yet tested in game.

- **W2:** a prefix on `FireUtility.TryAttachFire` that skips it for an aircraft of ours in the air, as VTOL Vehicle's and the Harrier's
  frameworks do for theirs: ground fire does not catch it. Burning in the air from hits is not modelled.
- **W3:** `FlightRules.Margin` and `WouldHoverWith` judge only with the full drivetrain model, as the hover limits, landing check and cruise
  height do; otherwise it can always hover.
- **W4:** a journey only to a landing spot on the map it is on (`FlightArrival.SameMapSpot`) is a flight to the spot and a landing, not out
  over the edge and back.
- **W7:** the whole strip is looked at as it arrives over it, a rolling landing chosen earlier too; blocked, a hovering aircraft lands
  straight down, one that cannot hover circles and says why.
- **W9:** a crash wrecks it through Vehicle Framework's own `Kill`, leaving its wreck, as the design says; a raiders' aircraft leaves none
  (never a prize). Destroyed in the air (shot down), everyone aboard is hurt by a fall from its height before Vehicle Framework puts them out.
- **W10:** the jobs of anyone working on, boarding, loading or refuelling it end as it leaves the ground (a straight lift-off or a roll).
- **W11:** a postfix on Vehicle Framework's `RecacheCompTickers` lets each comp of ours that still needs its tick ask again (`IKeepsTicking`:
  the flight, the sling, the litter).
- **W12:** the place to come in over the edge is looked for along the whole edge; failing a clear one, the one that would wipe least.
- **H4:** a hoist without power is wound by hand at 0.15 of its speed (authored), saying so; whoever is on a line is ticked while there.
- **S2:** a stock helicopter on our flight flies water-drop runs from the ground, as before its setting; in the air it is told to land first.
