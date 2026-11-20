# Depots and forward fuel points: design (20 Nov 2026)

**Status (20 Nov 2026):** design only, nothing built. Rows DP1 to DP10; the owner answered the five questions the same day
([the answers](#the-owners-answers-20-nov-2026)); one question left, on the setting rule's reach.

The owner, 20 Nov 2026, choosing what follows the emergent-play work: "Sling lifts a hull and design the depots, step-wise if
necessary". This is the design for row EM4 of [the emergent-play record](vehicle-emergent-play.md): "Leave a bowser or fuel cans at a
tile; an aircraft sets down at it".

Evidence marks: **[code]** read in our source or a decompile, with the place; **[A]** the agent's proposal or figure, for the owner
to revise; **[P]** a primary source; **[S]** a secondary source or a search summary, the page itself not read in full.

## Read this first

- **A depot is fuel you carried out earlier, waiting where you will need it.** A tanker run before a long flight; a bowser left at
  the pass before winter. Range stops being one tank and becomes something built by earlier trips.
- **Most of the pieces exist.** A place on the world map that holds vehicles for as long as needed (the lost-convoy place), tanks
  and bowsers that hold fuel, a convoy that refuels from cargo, and an aircraft that sets down to pour fuel on the way. What is
  missing is leaving a vehicle on purpose, and drawing from it without taking it away.
- **Two batches** (the owner's answers): depots themselves first, by road and by air, in vehicles or as a bare cache of crates
  (DP1 to DP6); then four pressures with a cause (DP7 to DP10), each behind its own setting.
- **No random rolls** (the owner's rule of 20 Nov 2026). Every pressure on a depot has a cause the player can see coming: an enemy's
  distance, the season, the age of the fuel, a war's front line.
- **Settings by risk** (the owner, 20 Nov 2026): "Activities with a higher risk should get a proper setting to disable them as such
  but otherwise, they are enabled by default UNLESS they are a HIGH or HIGHER risk." So every pressure has its own setting; the low
  and moderate ones start on, the high ones start off.
- **Vehicle Framework already has "leave vehicles here"**, with a clock: its stash is deleted after 15 to 30 days. A depot is our own
  place instead, and never expires by itself (the owner's answer).

## What exists today

| Piece | What it does now | Where |
| --- | --- | --- |
| Vehicle Framework's stash | A convoy can leave its vehicles on a tile; the people walk on. The vehicles and the cargo left in them wait in a world object. **It is deleted after 15 days for the smallest vehicle to 30 for the largest.** A caravan that arrives takes everything back at once | **[code]** `StashedVehicle.Create`, Vehicle Framework decompile of 18 Nov 2026, line 98192-98250; `Dialog_StashVehicle`, line 85968 |
| Our parked-vehicle warning | A letter some days before a stash is deleted | **[code]** `GameComponent_PhobosParkedWarning`, `src/PhobosFramework.Vehicles/Service/FleetService.cs` |
| The lost-convoy place | A world object of ours that holds several vehicles with their cargo for as long as it takes, and gives them back on a small map when a caravan arrives | **[code]** `PhobosSlingWreck`, `src/PhobosFramework.Vehicles/Sling/SlingWreck.cs` |
| Garage outposts | A staffed outpost holding vehicles with no time limit; they are not ticked while parked. Needs Vanilla Outposts Expanded | **[code]** `Outpost_PhobosGarage`, `src/PhobosFramework.Vehicles.Outposts/Outpost_PhobosGarage.cs:26-30` |
| A convoy refuels from cargo | Any vehicle of a convoy tops up from fuel carried anywhere in the convoy | **[code]** `CargoRefuel.Tick`, `src/PhobosFramework.Vehicles/Service/CargoRefuel.cs:39` |
| An aircraft sets down for fuel | Flying a journey with its tank at a quarter or less, it lands on the tile beneath, pours from **its own hold**, and flies on. Not over water, mountains or an enemy's ground | **[code]** `CargoRefuel.FlightLook`, same file, line 186-236; `NoLandingReason`, line 165 |
| Tanks and bowsers | A tanker or bowser trailer holds fuel as a fluid and pours it into vehicles on a map | [working trailers](vehicle-trailers.md) |
| A trailer goes along off the map | What a vehicle tows travels with it in a convoy | **[code]** `src/PhobosFramework.Vehicles/Hitch/TowTravel.cs:9-31` |
| The sling | A helicopter carries a vehicle, a trailer included, to another tile | [sling loads](vehicle-sling-design.md) |
| Several legs in one flight | A flight is a list of places with one arrival action at the end. Whether the player can add a place in between is **not read** | **[code]** `FlightPath`, Vehicle Framework decompile, line 84386-84476 |

**Nothing in the owner's mod list does this** ([the list](owner-mod-list.md), searched for stash, cache, depot and supply on 20 Nov
2026): Vanilla Outposts Expanded and its add-ons are the nearest, and they are staffed outposts, not unattended fuel.

## The design

| ID | What | Builds on | Size [A] |
| --- | --- | --- | --- |
| DP1 | **Leave as a depot.** A convoy standing on a tile leaves chosen vehicles or trailers there, with what is in their cargo and tanks, as a depot. No clock: it waits until someone comes. People never stay with it | The lost-convoy place, made to be chosen on purpose; Vehicle Framework's own leave-behind window for choosing | Medium |
| DP2 | **Draw from a depot.** A convoy on the depot's tile refuels from the depot's tanks and fuel cans without taking the vehicles away, and can swap cargo with it through the game's own transfer window. Pouring takes time, by the amount | `CargoRefuel`, `CargoFuelRule.StopTicks` (a quarter of an hour a tank today) | Medium |
| DP3 | **An aircraft's stop at a depot.** (a) Works with nothing new: land at the depot's tile as a convoy, draw (DP2), take off. (b) Proposed: a journey may name a depot as a stop; the aircraft sets down there as it does today for its own hold, pours from the depot, and flies on. The range shown when choosing a destination counts the depots along the way | `CargoRefuel.FlightLook` and its landing and lift-off; `FlightRange` | (a) none; (b) medium, after a read |
| DP4 | **Laying a depot by air.** A helicopter slings a bowser to the tile, lands as a convoy and leaves it (DP1). Nothing new to build: the sling carries trailers already | The sling, DP1 | None |
| DP5 | **Reading it.** The fleet board lists depots in a group of their own with their fuel; the depot's line on the world map says what it holds; the convoy's snow forecast already says when the road closes | Fleet board, `WorldObjectComp`, `WorldSeasons` | Small |
| DP6 | **Taking a depot back.** A convoy on the tile takes a vehicle of the depot into the convoy, as it recovers a lost one today; the depot goes when its last vehicle does | The lost-convoy place's recovery | Small, with DP1 |
| DP1(b) | **A bare cache** (the owner's answer to question 2). A convoy leaves items from its cargo on the tile with no vehicle: fuel cans, parts, food. A world object holding items, drawn from and taken back through the same transfer window; the aircraft's stop pours from it as from a bowser | DP1's place, holding items as well as vehicles | Medium, with DP1 |
| DP7 | **Hostile neighbours.** A depot within a few tiles of a hostile faction's settlement is found and stripped after a time set by the distance (the closer, the sooner [A]), shown on the depot's line from the day it is laid: "found in about N days". Its fuel and cargo are taken; its vehicles are left, emptied [A]. **High risk: its setting starts off** | `WorldPlaces`, the factions' settlements | Small to medium |
| DP8 | **Weather and season.** A depot left through a winter, or on ground that floods, slowly loses condition: its vehicles' outer parts wear a little a day, and a share of a cache's cans leaks [A]. Read from the season ahead (`WinterForecastRule`, built) and the tile; said on its line | The season forecast, part wear | Small. **Low risk: starts on** |
| DP9 | **Fuel ageing.** Stored fuel slowly goes stale: after some months a growing share of a depot's fuel is unusable [A]. A real effect (petrol loses its light ends and oxidises; diesel gathers water and gums), with ballpark months, unverified; chemfuel is the game's own and follows the same rule as a gameplay choice. The line says how fresh it is | — | Small. **Moderate risk: starts on** |
| DP10 | **Wars move the front.** With RimPacts active, a depot on a tile that passes to a faction at war with the player is seized, as its settlement's ground grows. **High risk: its setting starts off.** Read RimPacts' map of who holds what first | RimPacts' territory, read as its other tie-ins are | Medium, after a read |

### What each one lets, stops, changes, and what else notices

| ID | Lets | Stops | Changes | What outside the vehicle notices |
| --- | --- | --- | --- | --- |
| DP1 | Put fuel where a later trip will want it | Nothing | A long trip becomes two: the laying, then the journey | The world map gains a place of yours; the bowser is away from home until fetched |
| DP2 | Go further than one tank by road, and swap a tired load for a fresh one | Nothing | Where to stop becomes a choice made days earlier | The depot's stock goes down by exactly what was poured |
| DP3 | Fly beyond one tank and the hold's cans | Nothing | An aircraft's reach is drawn by where depots stand | The depot; the aircraft is on the ground for the pour, as today |
| DP5 | See what is out there without visiting | — | — | — |
| DP1(b) | A depot with no vehicle tied up in it | Nothing | A light aircraft can lay one from its hold | The tile, which holds a cache |
| DP7 | Weigh a depot's convenience against its enemies | A depot left near enemies for ever | Where a depot stands matters | The hostile faction, which strips it |
| DP8 | — | A depot left untended through the bad season | Depots are laid for a season, then used | The weather, which wears it |
| DP9 | — | Fuel left for years | A depot is a plan for months, not a stockpile | The fuel itself |
| DP10 | — | A depot on a front that moves | Wars reach the logistics behind them | The faction at war, which takes the ground |

## Choices inside the design

- **Our own place** (the owner's answer to question 1), not Vehicle Framework's stash with its clock stopped: stopping another
  author's clock changes their design under them, and their stash gives everything back at once and cannot be drawn from.
- **A vehicle, a trailer or a bare cache holds the fuel** (the owner's answer to question 2). The game cannot leave loose items on a
  world tile by itself, so the cache is our place holding the items.
- **Parked vehicles are not ticked**, as in the garage. What happens to a depot over time (DP8, DP9) is worked out from the days
  since it was last visited when someone looks or arrives, never each tick: a depot still costs nothing to run.
- **Fuel is never made or lost in the transfer.** What is poured leaves the depot and enters the tank; honest quantities, the rule
  for every accessory.
- **Hostile ground.** An aircraft will not set down on a tile an enemy holds; the same rule would keep a depot from being laid
  there **[A]**.

## Other factions' vehicles

- **In a raid on us:** nothing. A depot is the player's place on the world map; raiders neither use nor see it. Their fuel is set
  on arrival by their own journey (Framework 0.192.0).
- **Parked at a base we attack:** nothing changes there.
- **One we capture:** it can be left at a depot or draw from one like any vehicle of ours.
- **Cost with several about:** a depot's vehicles are held, not ticked. The fleet board reads depots only while it is open.

## Saves and removal

- One world object per depot, holding its vehicles. Safe to add to a save in progress.
- **Removing the mod with a depot standing would lose its vehicles**, as with a garage or a lost-convoy place today: the README's
  "before removing" list would gain "bring every depot's vehicles home".

## What is not proposed

- **Pressure with no cause.** Theft by nobody in particular, a fixed clock, a chance of loss: each would be a roll. DP7 to DP10 are
  the pressures with a cause, the owner's picks.
- **Depots other factions lay or use.** They stay on the light tier (the owner, 11 Nov 2026).
- **Automatic resupply runs to a depot.** That is EM15, held until remote farms or the garage see play.
- **A staffed depot.** That is the garage outpost, which exists.

## Proposed batches

| Batch | Rows | Why this order |
| --- | --- | --- |
| 1 | DP1, DP1(b), DP6, DP2, DP3(b), DP5 | Depots complete in themselves, by road and by air (the owner: the aircraft's stop in the first batch): lay, draw, stop on the way, read, take back. DP3(b) starts with reading how Vehicle Framework lets a player order a flight of several legs (`FlightPath.AddNode` exists; the order's own window is unread) |
| 2 | DP8, DP9, then DP7, then DP10 | The pressures, each behind its own setting: the slow ones first, the ones that take a depot after, RimPacts' last after its read [A] |

## The owner's answers (20 Nov 2026)

1. **Our own depot place**, not Vehicle Framework's stash with its clock stopped (the agent's lean).
2. **Also bare crates** (against the agent's lean of vehicles only): DP1(b).
3. **The aircraft's own stop at a depot, in the first batch** (the agent had it second).
4. **Pressure with a cause**, all four the agent offered: hostile neighbours, weather and season, fuel ageing, RimPacts wars. With
   the rule on settings by risk quoted under "Read this first".
5. **Swapping cargo is enough** (the agent's lean): a depot's line names its fuel only.

**The risk grades are the agent's [A], for the owner to revise:** losing the whole depot's contents at once (DP7, DP10) is high; a
slow, foreseen loss of part of it (DP8 low, DP9 moderate) is not.

**Left open:** whether the rule on settings by risk holds for every mod, as a shared rule, or for the depots alone.

## Who chose what

- **Owner (20 Nov 2026):** that depots be designed, after the emergent-play record's EM4; no random rolls (the same day, on that
  record); the five answers above, and the rule on settings by risk.
- **Agent choices, for the owner to revise:** every row, size, lean and batch here.

## Sources

- **Depot laying on a real journey.** Amundsen's party laid three depots at 80, 81 and 82 degrees south between February and April
  1911, about 3,000 kg of supplies, before the journey to the Pole the next season (**[S]** search summaries of the
  [Fram Museum's page on the third Fram expedition](https://frammuseum.no/polar-history/expeditions/the-third-fram-expedition-1910-1914/)
  and of [Amundsen's own account](https://gutenberg.org/files/4229/4229-h/4229-h.htm); the pages not read in full).
- **Mawson's air tractor** made "a successful depot-laying trip with 700 pounds of supplies on 2 December" 1912 (**[P]**, cited in
  full in [the emergent-play record](vehicle-emergent-play.md)).
- **Forward refuelling for helicopters** is ordinary military practice: the US Army's techniques publication for forward arming and
  refueling points, ATP 3-04.94 of 26 January 2012 (**[S]** a search summary; an
  [archived copy](https://webarchive.library.unt.edu/web/20130213212822/http:/armypubs.army.mil/doctrine/DR_pubs/dr_a/pdf/atp3_04x94.pdf)
  not read in full, and not checked for a later edition). Our inference, not the source's: a bowser slung to a tile and left is the
  game's version of it.
- **Vehicle Framework** (SmashPhil and contributors): its stash, flight path and leave-behind window, read in the decompile of the
  installed build of 18 Nov 2026 and summarised in our own words.
