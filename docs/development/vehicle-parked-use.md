# People use parked vehicles (25 Nov 2026)

**Status (25 Nov 2026):** built in D'thorga Framework 0.289.0, PV0 to PV7 ([as built](#as-built-framework-02890)); not yet tested in game
(test rows 518 to 521). PV8, a chat aboard, is not built.

How camping and life aboard work now: [systems/camping-and-life-aboard.md](systems/camping-and-life-aboard.md). This record adds
the half that page does not have: **people on foot choosing, by themselves, to go to a parked vehicle** for what it carries.

**A design record.** The owner, 25 November 2026: "there's a rich system for pawns taking advantage of a vehicle's full capabilities
[...] but what about while the vehicle is currently on a map? We could have pawns travelling between vehicles and going in/out to
fulfill their needs as required, or simply if they're just bored." Facts marked **[code]** were read on 25 Nov 2026 with
`scripts/query-code.py` and `scripts/query-patches.py` (the game 1.7, Dubs Bad Hygiene 1.7, our own `src/`), in our own words.
Figures marked **[A]** are authored starting values; **[I]** is our inference.

## Read this first

- **What already works:** a rider aboard a parked vehicle hears its stereo, reads, has a treat, drinks from the crew water tank and
  smokes (life aboard, every 250 ticks); an assigned colonist sleeps in the cab bunk; a camp's beds, stove, basin and lights work as
  the game's own buildings. **[code]** `LifeAboard.cs`, `BunkSleep.cs`.
- **What is missing:** nobody on foot ever *decides* to go to a vehicle. Riders are only those the player seated, and the bunk
  sleeper only goes because the player assigned them. So a stereo, a fridge of food or a tank of water sits unused unless ordered.
- **The pattern to grow from is already ours:** the cab bunk walks a colonist to the vehicle, climbs them into a free passenger seat
  (`TryAddPawn`), checks every 250 ticks whether they should get out (danger, fire, illness, rested, timetable) and climbs them out
  (`DisembarkPawn`) with a message saying why. This record makes that a general **visit** with a reason.
- **Every use is a fallback or a free choice, never an override:** food, water, warmth and sleep go to a vehicle only when the game's
  own search found nothing better; recreation is one more choice among the game's own, weighed by the game.
- **Other factions: nothing**, as a stated choice (see [Other factions' vehicles](#other-factions-vehicles)).

## What the research found [code]

| Fact | Where | What it means here |
| --- | --- | --- |
| A rider aboard a vehicle on a map starts no job; their needs keep falling | Vehicle Framework; our [accessories record](archive/vehicle-accessories.md) | Whatever a visitor gets aboard, our look must give it (life aboard already does) |
| The game's joy giver is a class with `GetChance` and `TryGiveJob`, chosen by the game by joy kind and tolerance | `RimWorld.JoyGiver` (full:all.cs:303443) | A vehicle can be one more recreation choice, weighed fairly against the game's own |
| **Hungry colonists already eat from a pack animal's packs on the map** (`canUsePackAnimalInventory: true`), scanning only colony *animals* | `JobGiver_GetFood.TryGiveJob` (full:all.cs:300561), `FoodUtility.TryFindBestFoodSourceFor` (273709-273716) | A vehicle is a pawn but no animal, so its cargo is skipped. Counting it like a pack animal's packs extends a vanilla rule |
| The food, rest and safe-temperature givers return nothing when they find nothing | `JobGiver_GetFood`, `JobGiver_GetRest`, `JobGiver_SeekSafeTemperature` (302527-302549) | A postfix that acts only on "nothing" never overrides the game or another mod |
| Dubs' thirst giver tries the pawn's own drink, then `ClosestSanitation.FindBestDrink`, else nothing; **no mod patches it** | `DubsBadHygiene.JobGiver_DrinkWater.TryGiveJob` (DBH-1.7:47-132) | The same fallback works for water |
| Other patches on these givers in the owner's list: VEF and Prisoner Realism (food, prefixes), Colony Hospital (food, postfix), Use Bedrolls and Replace Stuff plus our bunk and tent (rest), Vanilla Temperature Expanded (safe temperature, prefix) | `query-patches.py who` | Ours runs after them, on their answer; it touches nothing when they found something |
| The parked cab cools to the outside in about an hour and a half; its heater needs a warm engine; the **night heater** burns 0.4 chemfuel an hour with the engine off, and runs only at a night camp with its setting and switch on | `Systems_Cabin.xml` | A cab is only a warm place to sit if it was just driven or the night heater may run for visitors |
| A thirsty rider drinks 1.5 L from the crew water tank, with Dubs' own contamination check | `CompPhobosCrewWater.RiderDrinks` | The on-foot drink reuses it unchanged |

## The rows

| Row | What a colonist does by themselves | How (built on) | Ends when |
| --- | --- | --- | --- |
| **PV0** | **The visit**: walks to a parked vehicle of the colony, climbs into a free passenger seat, stays while the reason holds, climbs out | The bunk's walk, climb and wake check, made general: a `PhobosVehicleVisitDef` per reason (XML), one saved list of visitors on the vehicle | The reason's own end, or danger, fire, illness, drafting the vehicle, a caravan forming, the timetable, or 2 hours [A] |
| **PV1** | **Sits in with the radio on** when bored: listens, reads a book from the cargo, has a cuppa | A joy giver of the music kind life aboard already gives (`HighCulture`); needs a stereo fitted. Life aboard then gives music, reading and treats as it does now | Recreation at the game's "satisfied" line, or the recreation time ends |
| **PV2** | **A hotbox session**: someone whose drug policy allows a smoked drug for recreation goes to sit in a closed cab and lights up; friends looking for recreation are more likely to join a vehicle where a friend already sits | A joy giver of the chemical kind; only with the Hotboxing setting on, a smoked drug in reach and the policy allowing it. Smoking and the shared high are hotboxing's, unchanged. Friend: opinion 20 or more [A]; chance ×3 [A] | As PV1 |
| **PV3** | **Warms up (or cools down) in the cab** when the weather is hurting them and the game finds no safe room | Fallback on `JobGiver_SeekSafeTemperature`: a closed cab now inside their comfortable range, or one whose night heater may run (its switch on and "Night heater at camp" on): the night heater runs while a visitor warms up, burning its 0.4 chemfuel an hour | The cab no longer helps, or the outside is comfortable again |
| **PV4** | **Eats from the cargo** when hungry and the game finds no food | Fallback on `JobGiver_GetFood`: the game's own `BestFoodInInventory` on the vehicle, like a pack animal's packs (food policy included); walks over, takes one portion, eats it by the game's own eating | Fed |
| **PV5** | **Drinks from the crew water tank** when thirsty and Dubs finds no water (Dubs Bad Hygiene only) | Fallback on Dubs' `JobGiver_DrinkWater`: walks to the vehicle and drinks there, by `RiderDrinks` (1.5 L, Dubs' contamination check). Standing at the tap; no climbing in | Drunk |
| **PV6** | **Naps sitting up in a closed cab** when tired and the game would put them on the ground | Fallback on `JobGiver_GetRest` when it chose the ground: a free passenger seat; rests at 0.85 [A] (between the ground and a bedroll), in the cab's air; a memory "slept sitting up in a cab" −1 [A] instead of the ground's | Rested, or the bunk's wake rules |
| **PV7** | **Moves between vehicles**: one whose visit has ended climbs out, and the game picks their next need, which may be another vehicle's | Nothing new: the visit always ends by itself, so the next choice is the game's | — |
| PV8 | *Later, not in this batch:* **a chat aboard**: two visitors in one vehicle talk, for social recreation | The game's social interactions need both people spawned; would be our own look | — |

**Players' seated riders are untouched**: only a visit ends by itself. Someone the player put aboard stays until the player takes them
out, as now.

## The three questions, and the fourth

| Question | Answer |
| --- | --- |
| **What it lets the player do** | Use a parked vehicle as a break room, a pantry, a water point and a warm place without ordering anyone: at camp on a trip, at an outpost, or in the motor pool at home |
| **What it stops them doing** | Nothing new is refused. A vehicle with visitors aboard puts them out before it moves: drafting it or forming a caravan climbs them out, with a message |
| **How it changes the way they play** | Where a vehicle is parked starts to matter (beside the work site it becomes the crew's break room). What is packed into it (books, tea, smokes, food, water) is used up by the crew, so provisioning a vehicle becomes part of a trip, and its battery, chemfuel and tank run down from real use |
| **What outside the vehicle notices** | Colonists' recreation, hunger, thirst, rest, temperature injuries and moods (the memories already built: hotboxed, cab moods); the vehicle's battery, chemfuel, cargo and water; the logbook (a line when a visit is first of its kind that day, e.g. "Ana and Tom hotboxed in the Tourer") [A]; the game's own drug and food records. Raids: visitors climb out at danger, as bunk sleepers do. *A logbook line was proposed here;
it is not built* |

## Risk, and its setting

| Row | Grade (agent's proposal) | Why |
| --- | --- | --- |
| PV1, PV2, PV6 | Low | The worst is a flatter battery or a smoke the player's own drug policy allowed; both are visible on the dashboard and the drug tab |
| PV3 | Low | Chemfuel at 0.4 an hour, only with the night heater the player switched on |
| PV4 | Low to moderate | It eats trip food, but only when there is no other food on the map: slow, visible on the cargo, and the choice a starving colonist would make |
| PV5 | Low | Water the tank already gives riders; contamination as Dubs rules |
| The battery | Moderate | Hours of radio on a parked vehicle can flatten the starting battery; see the question cards |

So **every row starts on** (the shared rule: on, unless high). One setting switches the whole family off: **"People use parked
vehicles"** (on). Each vehicle has one switch, **"Open to the colony"** (on), to keep one vehicle's stores for the road; both wait on
the owner's card.

## Other factions' vehicles

- **In a raid on us:** nothing. Raiders follow their raid's orders, never recreation or needs jobs of this kind **[I]**.
- **Parked at a base we attack:** nothing. Its people defend the base under their own orders **[I]**.
- **Captured:** it becomes ours, and everything here works on it.
- **Guests, visitors and prisoners** never visit a colony's vehicle (agent default).

## Performance

- **No new tick.** Each map keeps a short list of the colony's parked, usable vehicles, refreshed from the vehicle's existing
  250-tick look; a joy giver's chance is 0 at once when the list is empty.
- **The fallbacks run only when the game found nothing**, which is rare, and then look only at that short list.
- **A visitor's end check** joins life aboard's existing look on the same vehicle.
- To measure once built: the "vehicle rare tick / full" timer already covers life aboard.

## Saved data and removal

- **Saved:** each vehicle's visitors (pawn, reason, since), under `PhobosVisit_*` keys, so a visit loaded from a save still ends.
- **Removal:** a visitor aboard when the Framework is removed stays aboard as a plain rider; take them out by hand. Nothing else is left.

## The owner's choices (25 Nov 2026, question cards)

| Choice | The owner's answer |
| --- | --- |
| Which uses | **All four**: radio, books and a cuppa (PV1); hotbox sessions (PV2); warm up and nap in the cab (PV3, PV6); eat and drink from its stores (PV4, PV5) |
| In or beside | **Climb in** (recommended): the cab's air and life aboard apply |
| The battery | **Guarded, harsh as a setting**: a visitor's radio stops at the start line; "Visitors may flatten the battery" (off) lets it run flat |
| Keeping stores for the road | **One switch per vehicle**, "Open to the colony" (on), plus the one setting "People use parked vehicles" (on) |

Everything else here is the agent's proposal or default, labelled where it matters.

## As built (Framework 0.289.0)

- **Code**: `src/PhobosFramework.Vehicles/Visits/` (the service, its saved game component, the four reasons, the jobs, the recreation
  choices and the fallbacks), `Rules/VisitRule.cs` (tested), and `src/PhobosFramework.Vehicles.Hygiene/VisitDrink.cs` (PV5). Defs in
  `Visits.xml`; figures in the rules Def; words in `PhobosVisits.xml`.
- **A reason is a Def** (`PhobosVehicleVisitDef`: its job, its longest stay, its worker), so another mod can add one.
- **Visitors climb out before the vehicle moves**, on Vehicle Framework's own ignition and move events (no patch); one still aboard
  as it leaves the map rides on as a rider, the visit over.
- **The bunk's sleeper is now asleep to life aboard too.** The game counts a rider with no job as awake, so before this a bunk sleeper
  heard the stereo and could read, snack or smoke; one check now covers bunk sleepers and nappers.
- **Differs from the design above:** the fridge's cold stacks are not taken first (the game's own choice of the best food is used), and
  the logbook line is not built.
- **Known limits:** a visitor takes a free passenger seat, so a boarding order on a full vehicle waits until they climb out or the
  vehicle is drafted; nappers rest at a flat figure, with no comfort from the seat beyond their memory.

## Sources

- The game's code (RimWorld 1.7) and Dubs Bad Hygiene's (1.7), read with `scripts/query-code.py`, summarised in our own words.
- Our own records: [camping and life aboard](systems/camping-and-life-aboard.md), [vehicle accessories](archive/vehicle-accessories.md),
  [hotboxing](archive/vehicle-hotboxing.md).
