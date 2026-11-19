# Vehicles at Work: what we gain from it and what we learn from it (19 Nov 2026)

**Status (19 Nov 2026):** built in D'thorga Framework 0.108.4: VW1 (its trips obey our refusals to move; it was SD1), not yet tested in
game; not built: VW2 to VW7. Two faults of ours found by this read (F1, F2) are recorded, not fixed.

**A research record.** The owner, 19 November 2026: "This is a research and record purview, strictly. But I wish to add 'Vehicles at
Work' as a soft dependency. How might we benefit from that both as a soft-dep and perhaps learning from as well? With the latter, I think
we are always doing such regarding the Agricultural mod but it might be applicable to other parts of our RimWorld mods too." This record
answers both halves. It gathers what the [soft-dependency cross-links](soft-dependency-crosslinks.md) (SD1 to SD3), the
[universal hoppers](universal-hoppers.md) (UH4), the [gathering runs](vehicle-gathering-runs.md) and the
[farm machines](farming-machines.md) records already say about the mod, and adds a full read of its code.

How to read it:

- **The mod**: [Vehicles at Work](https://steamcommunity.com/sharedfiles/filedetails/?id=3835520732) by 生吃螃蟹要吐壳
  (`shengchipangxieyaotuke.vehiclesatwork`), on the owner's list at load position 232. Authorship is theirs; Steam only hosts it.
- **[code]** was read on 19 Nov 2026 from our decompile of its 1.7 assembly (`.local/decompiled/VehiclesAtWork-1.7`, made 13 Nov 2026 from
  the build installed on 11 Nov 2026, which is still the newest: the Workshop item was last updated 11 Nov 2026), with
  `scripts/query-code.py` and `scripts/query-patches.py`. Paths such as `Wheels.cs:137` are inside its `WOR` folder. It has no public
  source (`config/source-repos.json` has no entry), so a decompile is the only way to read it. **Each citation was read once**, by a
  research pass; the lines behind F1 and F2 were read a second time. Decompiles stay in `.local/` and are summarised in our words.
- **[page]** is its Workshop description and 50 of its 116 comments (10 to 19 Nov 2026), read with `scripts/workshop-research.py`.
  **The 66 older comments were not seen**: the tool shows 50 and has no option for more (a tool gap, below).
- **[A]** is the agent's choice or figure, for the owner to revise; **[I]** is our inference. **Nothing here is tested in game.**
- **VW rows** are proposals, in the cross-links record's layout; each says what it **lets** the player do, what it **stops** and how it
  **changes** play (owner rule, 8 Nov 2026). Kinds: **XML**, **C#** (by name), **assembly**, **hook**.

## Read this first

- **It is already a soft dependency of ours, in part.** D'thorga Framework 0.108.4 (13 Nov 2026) makes its trips leave out a vehicle our
  systems refuse to move, and gives our reason when a trip is called off (`src/PhobosFramework.Vehicles/Service/VehiclesAtWorkPatches.cs`;
  [the tie-in register](tie-in-register.md)). The owner has not yet run its test (check 91 in [the test pass](in-game-test-pass.md)).
- **What the mod is.** Colonists drive vehicles *to* their work. They never work *from* one. It gives four things: commuting (with lifts
  for colleagues), hauling by vehicle (loaded and unloaded by hand), supply runs to building sites, and parking lots with a new **Drive**
  work type. Every vehicle is opt-in, land vehicles only, one driver only.
- **The strongest new link is parking (VW2).** The loudest complaint on its page is messy parking with no fixed spot **[page]**. Our depot
  bays are a fixed spot with a facing, fuel and service. Making a bay the vehicle's parking place answers its players' complaint with what
  we already built.
- **The link that would change play most is a driver fetched for our errands (VW3)**: Deliver and Gather wait for a driver today. It is a
  new behaviour, so it is the owner's call.
- **It has no modder's API.** No extension, interface, event or Def field; everything is public static and found by name. Every link is
  **C# by name**, as VW1 is, and stands down by itself if a name goes.
- **It is twelve days old and moving fast**: created 7 Nov 2026, updated 11 Nov, the author's last reply 17 Nov, 12,515 subscribers
  **[page]**. Names we tie to may change; `scripts/check-targets.py` (in the gate) and `scripts/upstream.py` say when they do.
- **Two faults of ours came out of the read** ([below](#faults-of-ours-this-read-turned-up)): our own errands skip the refusals VW1 gives
  theirs, and a stockpile zone's "Vehicles may unload here" mark does not survive a gravship flight.

## What the mod does, from its code

**Opt-in, per vehicle.** Each vehicle has a `VehicleUse` record whose `trips` and `hauling` switches start off (`VehicleUse.cs:12-16`),
three buttons (Take trips, Haul with it, Drivers) added by a postfix on `VehiclePawn.GetGizmos` (`UI.cs:73-121`), and a list of who may
drive it (`MayDrive`, `VehicleUse.cs:45`). All its Vehicle Framework access is by reflection in one class, `VF`; its assembly does not
reference Vehicle Framework **[code]**.

| Feature | How it works **[code]** |
| --- | --- |
| **Commute** | A postfix on `Pawn_PathFollower.StartPath` notes a colonist setting off on a walk of at least 35 cells (`Commute.cs:30-85`). Every 30 ticks its map component (`Wheels`) weighs up to 4 parked vehicles within 40 cells and drives only if the trip takes under 85 % of the walk (`Commute.cs:369-472`) |
| **A trip** | The colonist's job is ended and a copy kept; a short job walks them to the vehicle and seats them; the **vehicle's** ignition is switched on and it is given the game's own `Goto` job (`VF.cs:667-703`); on arrival the colonist gets out and the copied job is started again, or the work giver is asked afresh (`Wheels.cs:677-739`, `:859-999`) |
| **Lifts** | A colleague going the same way takes a spare seat or is picked up on the way (`Together`) |
| **Haul by vehicle** | A work giver under Hauling, once per 300 ticks per map: up to 150 haulables sampled, the densest cluster at least 45 cells from its storage, up to 30 stacks by cargo mass (`Haul.cs:76-215`). Loaded by hand, driven, and **unloaded by hand, one stack at a time**, into wherever the game's storage search says (`JobDriver_UnloadVehicle.cs:41-68`) |
| **Supply runs** | A builder fills the vehicle with what the blueprints and frames within 12 cells still need, drives, and hands it out with the game's own job (`Supply.cs:98-197`) |
| **Parking lots** | A zone (`Zone_ParkingLot`) that may be kept for one vehicle; a cell is chosen outdoors, away from doors, clear of other vehicles and of other trips' targets, where the body fits (`Lots.cs:42-123`). A new work type, **Drive** (off to start), returns a vehicle left idle 5,000 ticks to its lot |
| **Which vehicles** | `Wheels.Ready` (`Wheels.cs:137-156`): the player's, spawned, not air or sea, **not drafted, not moving, nobody aboard**, fuel above 5 %, able to move, not burning, not forbidden, no trip of its own under way. One driver seat only: a vehicle needing two has its switches turned off with a message (`Wheels.cs:362-372`) |
| **Settings** | Shortest trip 35, shortest haul 45, longest walk to a vehicle 40, a map speed boost of 2.5 times, gentler arrival (`WORSettings.cs:7-21`). The owner plays with the boost at 1 (`scripts/mod-settings.py`) |

**When a trip is cut short** (`Wheels.cs:306-344`, `:530-550`, `:1071-1106`): a colonist drafted, downed or given a forced order cancels it;
**the player ordering the vehicle elsewhere releases it**, with nobody put out; each stage has a time limit; a stuck drive is given up
after 240 ticks without moving, three times over. There is no check for enemies while driving.

**Its 16 Harmony patches** (`scripts/query-patches.py`, matching the code). The ones that touch what we do: the postfix on
`VehiclePawn.GetGizmos`; a postfix on `VehicleStatWorker.GetValue` that multiplies the map speed of **every** player vehicle, opted in or
not (`Speed.cs:13`); a postfix on `ForbidUtility.IsForbidden` that hides things a trip has claimed from everyone (`Claims.cs:116`); two on
the gravship's zone methods that carry its lots across a flight; and a postfix on Vehicle Framework's `CanBeHauledToVehicle` that allows
loading outside the home area. Three prefixes skip the original, all for Atmosphere Overhaul's wind throwing vehicles (`Compat.cs:99-121`).

## What is already built

| # | What | Where | Status |
| --- | --- | --- | --- |
| VW1 (was SD1) | Its two vehicle tests (`WOR.VF:Healthy`, `WOR.VF:CanMove`) answer no while a system of ours refuses to move (worn-out brakes, a set-up job, a submarine on the bottom); its "why it is stuck" line gives our reason | `VehiclesAtWorkPatches.cs`, three postfixes found by name, switched on only while the mod is active, standing down if a name goes; `MoveRefusals.For` | Framework 0.108.4. Not yet tested in game (check 91) |

## Links worth having

| # | Link | Reads → changes | Lets · Stops · Changes | Kind | Author | Verdict **[A]** |
| --- | --- | --- | --- | --- | --- | --- |
| VW2 | **A depot bay is the vehicle's parking place** (SD2, sharpened) | A vehicle's assigned depot bay (`Systems/DepotBays.cs`) → where its parking looks first (`Lots.Spot`, `Lots.cs:42`) and what counts as parked (`ParkedRight`, `Lots.cs:27`), so the commute's own parking and its Drive work bring the vehicle home to its bay, facing as the bay faces | **Lets** a motor pool that puts itself away: each truck back on its own pad, refuelled, charged and serviced by the bay. **Stops** vehicles left across a doorway or half in a lot. **Changes** where bays go: one per working vehicle, by the workshops | C#, by name: two postfixes | Active; players ask it for exactly this | **Take first** |
| VW3 | **Drive work fetches a driver for our errands** | A Deliver or Gather ordered on a truck with nobody aboard → a standing order; a colonist with its Drive work who is on that truck's Drivers list (`VehicleUse.MayDrive`) boards with Vehicle Framework's own boarding job, and the errand starts | **Lets** "deliver this load" or "gather that" be pressed and left. **Stops** nothing: with a driver aboard it starts at once, as now. **Changes** Deliver from a button to an order that waits, and makes the Drivers list say who drives our errands too | XML (a work giver under its work type, `MayRequire`), C# by name | As above | **Read first**: a new behaviour, the owner's call ([question 1](#questions-for-the-owner)) |
| VW4 | **Its hauls end at our bunker and marked storage** (UH4) | Its unloading asks the game's storage search (`Haul.cs:139`, `JobDriver_UnloadVehicle.cs:41-68`) → a bulk bunker is storage like any other | **Lets** its vehicle hauls fill bunkers. **Stops** nothing. **Changes** nothing we build | None **[I]** | As above | Free by design; **check in play** |
| VW5 | **Busy both ways** | Its trip on a vehicle (`Wheels.TripOfVehicle`, read only) → our Gather and Deliver are greyed out with "it is on a work trip". The other way is already true: its `Ready` refuses a drafted, moving or occupied vehicle, and every errand of ours has its engine running and its driver aboard | **Lets** both systems share one truck without surprises. **Stops** an errand of ours taking a truck from under a commuter (today it would end their trip: their release rule, **[I]**). **Changes** nothing else | C#, by name, no patch | As above | **Take**, small |
| VW6 | **Our tip unloads its hauls** (SD3 again) | Its unloading stage on a truck with a tip, at a marked bay → our tip instead of hand unloading | **Lets** a Talus used for its hauls empty in one go. **Stops** nothing. **Changes** little: a Talus has Gather and Deliver for this | Assembly, into its trip stages | As above | **Set aside** still. A guide note instead: on a Talus, leave **Haul with it** off and use Gather and Deliver |
| VW7 | **Farm machines and its haul** | Its haul work on any opted-in vehicle → a far field's harvest carted to storage with nothing of ours built; a combine or tractor of ours keeps both its switches off, which is where they start | **Lets** grain carting on day one of D'thorga Farming, with whatever truck the colony has. **Stops** a colonist commuting in the combine. **Changes** nothing we build | None **[I]** | As above | Record in [farm machines](farming-machines.md); check in play |

**Found, with nothing to build:**

- **Its map speed boost reaches our trucks, harmlessly.** It multiplies every player vehicle's move speed (2.5 times by default; the
  owner's is 1). None of our fuel or engine figures read that stat: only our "is it stopped" tests do (`Service/Mobility.cs`), and a thrown
  track's zero stays zero **[code]**. An errand of ours simply finishes sooner at a higher boost.
- **Aircraft and boats are never taken**: it leaves out air and sea vehicles by their type (`VF.cs:433-502`). The Osprey, stock
  helicopters and our submarines are untouched **[code]**.
- **A trailer is never taken**: it has no driver's seat **[I]**. A tractor with a trailer hitched is not read; check in play.
- **Our crews on station are safe**: a vehicle with anyone aboard is never chosen.
- **A correction to our own changelog**: Framework's entry for the gathering run says a hauler of theirs taking the truck mid-run ends the
  run. From its code that cannot happen, as the truck is drafted and occupied. The entry stays as written; this is the correction.

## Argued against

| Idea | Why not |
| --- | --- |
| Our own commuting | It exists and is active (as the cross-links record said on 13 Nov 2026) |
| Our own parking zones | Its lots and our bays cover it together (VW2) |
| Teaching its haul to use our loading grab | Its haul is a hand-loading trip run by its own state machine; the grab already has Gather. Two ways to do one thing on one truck |
| Hard dependency | It is twelve days old, has no public source and no API. Soft, by name, standing down by itself |

## What to learn from it

**Worth taking up**, each with where it applies:

| Lesson | From **[code]** | Where it applies |
| --- | --- | --- |
| **Keep a trip's state in one saved place, with short throwaway jobs and a time limit per stage** | Its map component pushes each next job; the jobs hold nothing (`Wheels.cs`) | Our run comp already works this way. **Farming's "get a driver aboard" step** and VW3 should too |
| **Interrupt, keep a copy of the job, start it again; ask the work giver if the copy has gone stale** | `Wheels.Copy`, `Restart`, `Resume` (`Wheels.cs:941-999`, `:859-899`). No vanilla work giver is patched | **Farming**: a colonist drives a tractor to the field and then does ordinary sowing or harvest work. Any errand of ours that borrows a colonist |
| **Decide by time, with margins** | Drive only under 85 % of the walk, driving costed at 1.1 times, at most 4 vehicles weighed | A farm machine fetched for a small field; whether Deliver is worth a trip for a few stacks |
| **Ask "can it move" again after boarding** | Vehicle Framework's answer is stale just after a pawn is added; it recaches up to 4 times (`Wheels.cs:351-357`) | VW3, and farming's boarding: our errands today need the driver already aboard, so they have not met it |
| **Notice a stall sooner** | 240 ticks without moving, 3 tries, then it ends the trip where it stands | Ours waits 2,000 ticks and 5 failed drives (`CompProperties_PhobosGatherRun`): about 33 seconds at normal speed before it notices. Worth shortening **[A]** |
| **Opt-in per vehicle, a drivers list, and a "why it will not go" line** | `VehicleUse`, `Dialog_Drivers`, `VF.WhyStuck` | Farm machines; VW3 reads its list instead of making a second one |
| **Parking rules** | Outdoors, away from doors, clear of other trips' targets, the body fitting (`Lots.Spot`, `VF.ParkingNear`) | Where a Deliver set to stay, or a finished Gather, leaves the truck; `VehicleAutoDrive.TryFindBackUpSpot` |
| **Carry zone-bound state across a gravship flight** | It patches `Gravship.TransferZones` and `GravshipPlacementUtility.CopyZonesIntoMap` | **F2 below**: our zone marks are lost the same way |
| **A cheap first test on every hot patch** | Its `StartPath` and `IsForbidden` postfixes test a count first, so they cost nothing until a vehicle is opted in | Any patch of ours on a method that runs for every pawn |

**Not to copy, and what to guard against:**

- **It never reserves the vehicle** (`JobDriver_GetIn.cs:19-22`): its only lock is "no trip of mine". VW5 is our guard.
- **`IsForbidden` as a claim system**: it changes the answer for every pawn, raiders included, and holds stale references between refreshes.
- **Reflection that answers "true" when a name is gone** (`VF.Healthy`, `CanMove`, `HasFuel`, `CanDrive`): a Vehicle Framework rename
  would turn its safety tests into yes. Ours stand down to "do nothing" instead, and the gate reads every name.
- **A cancelled trip can leave the engine running** (`Wheels.cs:1076-1093`): the vehicle then reads as drafted and it will not take it
  again. With our idle fuel use, such a truck burns its tank away **[I]**. Theirs to fix ([question 4](#questions-for-the-owner)).
- **Fuel is a 5 % floor**, with no thought for the distance.
- **No check for enemies while driving.** Ours end an errand when enemies come, unless the armoured cab's switch says otherwise.
- **All its text is hard-coded English**, with no language files. Ours stays in Keyed files.
- **The job it will resume is not saved**: a save mid-trip comes back through the work giver only.
- **One stat multiplier for every vehicle on the map**, whether opted in or not.

## Faults of ours this read turned up

Recorded here and in [outstanding work](outstanding-work.md); not fixed by this record.

- **F1. Our own errands skip our own refusals.** `VehicleAutoDrive.CannotDrive` (`src/PhobosFramework.Vehicles/Service/VehicleAutoDrive.cs:27-48`)
  asks about the hitch, a set-up job, fuel and Vehicle Framework's own tests, but never `MoveRefusals.For`. So a Gather or a Deliver can drive
  a truck whose brakes are worn out: the gap VW1 closed for Vehicles at Work on 13 Nov 2026, still open in ours. `MoveRefusals` describes itself
  as the one place this is asked "for anything that sends a vehicle somewhere without the player's float menu". Read in code twice; not
  reproduced in play. The fix is one line there **[I]**.
- **F2. A stockpile zone's "Vehicles may unload here" mark is lost on a gravship flight** (Framework 0.175.0, built on 19 Nov 2026). The game
  deletes each stockpile zone on the ship at take-off and makes a new one on landing (`Gravship.TransferZones` and
  `GravshipPlacementUtility.CopyZonesIntoMap`, RimWorld 1.7's decompile, read 19 Nov 2026) **[code]**; our marks are kept by zone, so they go
  with the old one. **A storage building keeps its mark**: bulk bunkers on a gravship are unaffected. Not reproduced in play. The mod's own
  two patches there show the way to carry them.
- **F3. Stale lines**, corrected with this record: the index said "SD1 to SD44" (the last is SD47), and the cross-links record said
  "row 228" (the mod is at 232).

## Other factions' vehicles

The mod drives only the player's vehicles (`Wheels.Ready` asks for the player's faction), so VW1 to VW7 touch only ours. A captured vehicle
starts with both its switches off, like any other. Its `IsForbidden` postfix is the one part that reaches other factions: a raider is told
a claimed thing is forbidden **[code]**; nothing of ours depends on that.

## Cost to run

- **VW1** is one cached read per decision of theirs (as recorded on 13 Nov 2026).
- **VW2** would add one bay lookup per parking decision, a few a minute at most.
- **VW3** would be a work giver scanned as any other; it must offer nothing when no errand waits, by a count tested first.
- **VW5** is one read per button draw, behind the half-second recheck Deliver's button already has.
- **The mod's own cost** is not ours to tune, and is unmeasured: the author says twelve vehicles ran without trouble **[page]**. Its haul
  planner samples 150 haulables and its parking search may walk 8,000 cells **[code]**. If the owner sees a slowdown with it on,
  `scripts/perf.py` says where.

## Questions for the owner

1. **VW3, a driver fetched for our errands.** Should Deliver and Gather become orders that wait for a driver? And if so, only with Vehicles
   at Work's Drive work, or with a work type of our own when it is absent? Agent's lean: yes, and through its Drive work only at first.
2. **VW2, bays as parking places.** Take it as the next link? Agent's lean: yes, first.
3. **F1 and F2.** Fix them next, ahead of new links? Agent's lean: yes; F1 is a safety gap and F2 loses the owner's choices on the Audeamus.
4. **The engine left running** after one of its cancelled trips is its author's to fix, and the author is active. A report draft can be
   written for the owner to post, if wanted; offered once.
5. **The stall wait.** Shorten ours from 2,000 ticks toward its 240? Agent's lean: to about 600, since our trucks back up to bays slowly.

## Who chose what

- **Owner (19 Nov 2026):** the mod as a soft dependency; research and record only; lessons for the farming mod and for the rest.
- **Owner (13 Nov 2026), carried forward:** VW1 fixed at once (as SD1 and TI4); SD2 after it; SD3 set aside.
- **Agent:** the VW prefix and numbers; every verdict and lean; VW2's shape (a bay as the parking place, where SD2 had a lot holding a
  bay); VW3, VW5 and VW7 as new rows; the lessons chosen and where each applies; the reading of its code as described.

## Sources

- **生吃螃蟹要吐壳**, [Vehicles at Work](https://steamcommunity.com/sharedfiles/filedetails/?id=3835520732): its description and comments
  of 10 to 19 Nov 2026 **[page]**, and its 1.7 assembly, decompiled for research and kept in `.local/` **[code]**. No endorsement is implied.
- **SmashPhil**, [Vehicle Framework](https://github.com/SmashPhil/Vehicle-Framework): the boarding job, drafting and pathing its trips and
  our errands both use.
- **Ludeon Studios**, RimWorld 1.7: the gravship's zone handling, from our decompile.
- **Tool gaps met**: `scripts/workshop-research.py report` prints no description and at most 50 comments. The description was read from
  its cache with its `sql` report; the older comments were not read. A change for phobos-modkit, when next touched.
