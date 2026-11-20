# Bay trips: vehicles taken to a dyno bay for repair, without being told (20 Nov 2026)

**Status (20 Nov 2026):** all four batches built in D'thorga Framework 0.201.0: a vehicle that can still drive is driven to a dyno bay,
repaired there, and driven home, to its depot bay, a vehicle park or a spot nearby; one that cannot drive is towed there by a recovery
vehicle, or pushed by hand when it is light. Not yet tested in game.

**The owner, 20 Nov 2026:** "Something we NEED to implement, and we need to either use or develop our own code for this so it works
independently of what mods the player might have on their setup, is pawns automatically taking a worn or especially, busted, vehicle to a
nearby dyno for repair because with a large colony, and dozens of vehicles like mine, there are just broken vehicles littered everywhere."
And: "Study and learn from other mods if necessary".

How to read it: **[O]** is the owner's choice (cards, 20 Nov 2026); **[A]** is the agent's choice or figure, for the owner to revise; **[I]**
is our inference. Nothing here is tested in game.

## Read this first

- **Nothing here needs another mod.** The driver call, the self-drive, the recovery tow and the hitch are ours
  ([Vehicles at Work tie-ins](vehicles-at-work-tie-ins.md), VW3; `Service/VehicleAutoDrive.cs`; `Hitch/Recovery.cs`). With Vehicles at
  Work on, the driver comes from its Drive column, as for Deliver and Gather; without it, from ours.
- **Somebody must have Drive work.** It starts off for everyone. With nobody assigned, vehicles wait and the alert says so.
- **Aircraft, boats and trailers never go**: none of them can be driven across a map to a bay. A dyno bay built beside the pad still
  serves an aircraft where it stands, as now.
- **A wreck goes only when its parts are in the colony.** A vehicle whose repair nobody can pay for stays where it is and says which
  part it waits for, so wrecks without parts never fill the bay.

## What the owner's save showed

Read from Autosave-6 on 20 Nov 2026 with `scripts/query-save.py` (read only): 31 vehicles stand on the home map, of about 70 owned. A
Highwayman sampled had its battery, starter, fuel pump and radiator at 0 health; a Tango had several parts under 1. **Such a vehicle
cannot start**, so driving it to the bay is impossible: it must be towed or pushed. That is why batches 3 and 4 exist.

## The four questions

- **What it lets the player do.** Leave a motor pool to look after itself: a vehicle with a destroyed part, a serious fault, or work only
  a dyno bay can do is fetched to the bay, repaired there (a part fitted at the bay goes in at 60% health, not 25%), and put away.
- **What it stops.** Wrecks lying where they broke down. It also stops a repaired vehicle blocking the bay: it leaves when its work is done.
- **How it changes play.** The dyno bay becomes the workshop the colony's vehicles pass through, so it wants room around it, a parts
  shelf nearby, and somebody on Drive work. Fitting a recovery winch to one truck now pays for itself.
- **What outside the vehicle notices.** Colonists (Drive work to fetch it, the hitch-up and hauling work for a tow or a push, mechanics
  at the bay); the dyno bay's own line, which lists the vehicles in reach; the Fleet board (what each vehicle is doing and waiting
  for); one alert naming the vehicles that cannot go and why; each vehicle's logbook.

## What sends a vehicle [O]

A setting with three levels, starting at the middle one. Routine work is still done where the vehicle stands, as now.

| Level | What sends a vehicle |
| --- | --- |
| Bay-only work alone | Work that waits for a dyno bay: a heavy overhaul, a queued tune or change of grade, a diagnosis asked for |
| **Broken or bay-only** (the default) | The above, and a destroyed part to replace, and a serious fault that needs a service |
| Any repair or service due | The above, and every due service, fitting and damaged part |

A piece of work sends the vehicle only if **it can be paid for**: its supplies are aboard or lie unforbidden on the map **[A]**.

## Which vehicles [O]

Every land vehicle of the player's on a home map, each with a switch on the Fleet board to keep it out. Left alone, always **[A]**: a
vehicle with anyone aboard, with its engine on, moving, set up for work or held by a set-up job, towing or being towed, on another
errand of ours, forbidden, or burning; and every vehicle while enemies threaten the map.

## The trip

```mermaid
flowchart TD
    look[A look, about every 1,000 ticks] --> need{Work that sends it,<br/>and payable?}
    need -- no --> stay[Stays; says which part it waits for]
    need -- yes --> place{A working bay<br/>with room in reach?}
    place -- no --> wait[Stays; the alert says why]
    place -- yes --> drive{Can it drive?}
    drive -- yes --> call[Calls a driver]
    drive -- no --> tow{Recovery vehicle free?}
    tow -- yes --> fetch[Towed to the bay: batch 3]
    tow -- no --> push{Light enough to push?}
    push -- yes --> hands[Pushed by hand: batch 4]
    push -- no --> wait
    call --> bay[At the bay: driver gets out, mechanics work]
    fetch --> bay
    hands --> bay
    bay --> done{Work done, or a day<br/>with nothing done?}
    done -- yes --> home[Depot bay, else vehicle park, else nearby]
```

- **One saved place for a trip's state, short jobs, a time limit per stage** (the lesson from Vehicles at Work): the state lives in the
  vehicle's own comp (`CompPhobosBayTrip`), on the shared base every errand of ours uses (`CompPhobosVehicleRun`).
- **The driver is one we called.** The trip starts only when a colonist arrives through the driver call. Crew the player boards
  themselves cancels it: the player has plans for the vehicle.
- **At the bay** the driver gets out and the engine goes off. The vehicle stays while work that sent it remains, or any work is ready.
  It gives up its place after a day in which nothing could be done **[A]**, and does not come back for a day.
- **Afterwards [O]**: its depot bay if it has one and the pad is free; else the vehicle park (batch 2); else a spot nearby, clear of
  every bay's reach. A vehicle the player parked at the bay themselves is never moved: only one we brought leaves by itself.
- **A drive that stalls** is tried again from another spot, three times **[A]**, then the crew gets out where it stands and the
  vehicle waits a day.
- **Enemies**: no trip starts while enemies threaten the map; one under way stops, its crew gets out, and it tries again later.

## Batches

| Batch | What | Version | Status |
| --- | --- | --- | --- |
| 1 | Trips for vehicles that can drive; the trigger setting; the Fleet board switch; the alert; home to a depot bay or nearby | Framework 0.201.0 | Built, not yet tested in game |
| 2 | **The vehicle park** [O]: a zone of ours on the motor pool tab; a repaired vehicle with no depot bay parks there | Framework 0.201.0 | Built, not yet tested in game |
| 3 | **The recovery tow** [O]: a driver takes a vehicle fitted with the recovery winch, a colonist couples the wreck, it is towed into the bay's reach and let go, and the tow vehicle drives back | Framework 0.201.0 | Built, not yet tested in game |
| 4 | **Pushing by hand** [O]: with no recovery vehicle free, colonists push a light vehicle to the bay, limited by weight with the pull rule the winch uses | Framework 0.201.0 | Built, not yet tested in game |

## The vehicle park (batch 2)

A zone (`Zone_PhobosVehiclePark`), marked with its own tool on the motor pool tab and grown with the same tool while one is selected. It
keeps nothing: when a vehicle needs a place, each park is searched from its south-west corner, facing north then east **[A]**, for a place
where the whole body lies on the park, with a cell clear all round of other vehicles and of places other trips keep, so a park fills in
a row and nothing stands door to door. A vehicle standing in a park already, with no depot bay to go to, is home and is not moved.

- **Its picture** is drawn in code (a parking sign: `assets/phobos-vehicles/draw_bay_trip_icons.py`), since a sign is flat shapes; the
  local icon batch may replace it on the owner's word.
- **A gravship flight does not carry it** **[code]**: the game carries stockpile and growing zones only (`Gravship.TransferZones`,
  RimWorld 1.7's decompile, read 20 Nov 2026). A park marked on a deck is marked again after landing.
- **Before removing the mod**, delete every vehicle park: a zone of a class the game cannot find fails to load.

## A vehicle that cannot drive (batches 3 and 4)

The owner: "Tow, or push light ones by hand". A vehicle that will not drive keeps a place at the bay it fits **facing as it faces now**,
since it is carried as it stands; then `BayTripRule.Carry` decides: towed if a recovery vehicle is free, else pushed if it is light
enough, else it stays and its line says why (nothing free to tow it; how many people it would take).

### The recovery tow

- **Which vehicle tows** **[A]**: the nearest of the player's vehicles fitted with the recovery winch, on no trip, not kept out, free, able
  to drive, and heavy enough that the tow needs no warning (the tow ratio, 1.5 times its own weight). An over-heavy tow stays the
  player's own choice, never made for them.
- **The steps**: it remembers where it stood and calls a driver; drives to a stop within the hitch's reach of the wreck; asks for the
  wreck to be coupled, and its driver gets out (the hitch's own hauling job does the coupling, whoever takes it); calls a driver again;
  tows; lets go; stands by; drives back to where it stood and its driver gets out.
- **The tow is the hitch's real tow**: the towed weight reaches the drivetrain, and brakes and clutch wear as on any tow.
- **Where it stops** **[A]**: about the tow's own length beyond the wreck's place, on the far side from where it came, so the wreck
  trailing behind comes to rest at its place or near it. In the bay's reach, the wreck has arrived.
- **Short of the bay's reach, it is winched in** **[A]**: the wreck is moved a cell at a time, at the winch's pace, along a way found once
  (`GridPathRule`, A* over the cells its body fits), while the recovery vehicle stands by with its crew aboard. **A gameplay
  simplification**: a real winch pulls in a straight line towards itself; ours follows the found way, as if the crew re-rigged the
  line at each corner. The winch's own force rule is not asked again here, since the tow ratio already passed the pair.
- **Each stage has a limit**: three stops beside the wreck that would not couple, 10,000 ticks for someone to couple it, and a day
  standing by. A tow that fails lets the wreck go where it is, says why on the wreck's line, and is tried again a day later.

### Pushing by hand

- **How many people** is the winch's own sum (`RecoveryRule.Required`: the vehicle's weight times its ground's resistance, or its
  ground's grip if its wheels will not turn) over one person's steady push, **25 kgf [A]** (a ballpark, unverified against any table),
  counted up to the next whole person, **six at most [A]**. So a car on firm ground takes one or two, a truck on soft ground more than
  six can give, and a vehicle whose wheels will not turn far more: it stays.
- **The work** is hauling work (`WorkGiver_PhobosPushVehicle`), offered until as many are at the vehicle or on their way as it takes.
  A pusher stands beside the body, never where it is about to move, and follows it. It moves a cell only while enough of them report,
  at **90 ticks a cell [A]** with hands to spare and twice that at their limit; the count is taken again at every cell, so soft ground
  on the way can stop it.
- **It turns to roll along its length** where its body fits that way here and all the way on; otherwise it is moved as it stands.
- **Hunger and sleep come first**: a pusher's job lets something more urgent take over every 2,000 ticks. With too few hands for
  10,000 ticks the push is given up for a day.

## Next: the service yard

Designed on 20 Nov 2026, to be built after this record's test rows: [the service yard](vehicle-service-yard.md), a zone of capped size
where vehicles are serviced, tuned, fuelled and parked in one place, and bay trips' first destination.

## Risk and settings

The owner's rule (20 Nov 2026): a behaviour that puts things or people at risk has its own setting, on unless its risk is high.

| Behaviour | Risk **[A]**, for the owner to confirm | Setting | Starts |
| --- | --- | --- | --- |
| Bay trips | **Moderate**: a colonist drives a faulty vehicle across the home map. Nothing is lost at once; it never starts with enemies about and stops when they come; every refusal of ours to move still holds (worn-out brakes); the player sees each trip on the vehicle's line and the Fleet board | Vehicles go to a dyno bay for repair by themselves | On |
| What sends a vehicle | None of its own | What sends a vehicle to the bay (three levels) | Broken or bay-only |
| The recovery tow | **Low**: the tow vehicle's brakes and clutch wear as on any tow, and it never takes a tow heavier than the ratio. It is away from where it stood for the length of the tow | A vehicle that cannot drive is towed to the dyno bay | On |
| Pushing by hand | **Low**: colonists spend time outdoors pushing, never with enemies about, and stop for hunger and sleep | Light vehicles that cannot drive are pushed to the dyno bay by hand | On |

## Other factions' vehicles

Only the player's vehicles on a player's home map take part. A raider's vehicle, and one parked at a base the player attacks, is never
looked at: the comp does nothing for another faction, and its look is one cheap test. A captured vehicle joins as soon as it is the
player's, with its switch on, like any other. A trip under way when a vehicle changes hands ends where it stands.

## Cost to run

Nothing runs each tick. Each land vehicle of the player's takes one look about every 1,000 ticks **[A]**, staggered by its ID; the look
reads lists the systems already keep (the repair list is cached by the health view's version). A place at a bay is searched for only
when a vehicle has work that sends it and nothing else stands in the way. The alert reads each vehicle's last answer and works nothing out.

## Authored figures [A]

| Figure | Value | Where |
| --- | --- | --- |
| Ticks between looks | 1,000 | `bayTripLookTicks` |
| Drives that may fail before a trip is given up | 3 | `bayTripFailedDrives` |
| Wait after a trip is given up, or a place given up | 60,000 ticks (a day) | `bayTripBackOffTicks` |
| Time at the bay with nothing done before the place is given up | 60,000 ticks (a day) | `bayTripIdleTicks` |
| Time a call for a driver may stand before the alert names it | 15,000 ticks (six hours) | `bayTripDriverWaitTicks` |
| A drive standing still this long has stalled | 600 ticks, as the gathering run's | `bayTripStuckTicks` |
| Wait for someone to couple a wreck | 10,000 ticks | `bayTripCoupleWaitTicks` |
| Wait for enough people to push | 10,000 ticks | `bayTripPushWaitTicks` |
| One person's steady push | 25 kgf (a ballpark, unverified) | `pushKgfPerPerson` |
| Most people on one vehicle | 6 | `pushMostPeople` |
| Ticks to push one cell with hands to spare | 90 (twice that at their limit) | `pushTicksPerCell` |
| Cells a search for a way may look at | 6,000 | `bayTripPathNodes` |

## Who chose what

- **Owner (20 Nov 2026):** the feature, and that it must not depend on another mod; a three-level trigger setting starting at "broken or
  bay-only"; wrecks towed, or pushed by hand when light; afterwards the depot bay, then our own vehicle park zone, then nearby; every
  vehicle takes part, with an off switch, on the Fleet board.
- **Agent (batches 3 and 4):** which vehicle tows and that it never takes an over-heavy tow; the driver getting out for the coupling;
  the stop beyond the place; the winching-in along a found way and its simplification; a person's push and the most people; pushing as
  hauling work; turning to roll.
- **Agent:** everything marked [A]; the payable test; the list of vehicles left alone; that only a called driver starts a trip; that a
  vehicle the player parked at the bay is not moved; the batches and their order; the risk grades.

## Sources

- **生吃螃蟹要吐壳**, [Vehicles at Work](https://steamcommunity.com/sharedfiles/filedetails/?id=3835520732): the lessons taken (one saved
  place for a trip, a time limit per stage, an early stall check, a "why it will not go" line), from our read of 19 Nov 2026
  ([the record](vehicles-at-work-tie-ins.md)). No code of theirs is used, and no endorsement is implied.
- **SmashPhil**, [Vehicle Framework](https://github.com/SmashPhil/Vehicle-Framework): boarding, drafting and pathing, which every trip uses.
