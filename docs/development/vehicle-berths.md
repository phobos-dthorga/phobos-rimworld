# Sleeping berths: roof tents, cab bunks and driving through the night (13 Nov 2026)

**A research and design record. Nothing is built, and nothing here is tested in game.** The accessories record
([vehicle-accessories.md](vehicle-accessories.md), Y21) advised leaving berths out; the owner, 13 November 2026, chose to have them
**designed properly**. Facts marked **[code]** were read on 13 Nov 2026 with `scripts/query-code.py` and `scripts/query-patches.py`
and are summarised in our own words; **[A]** is authored; **[I]** is our inference. Vehicle Framework references are from its
**1.6** decompile, the only one we hold. The owner's accessory rules apply: every berth uses up something real, and there is no
drag.

## Read this first

- **There are three different things here, and they are not equally hard:**
  1. **B1, the roof tent at camp:** a fold-out bed beside the parked vehicle. Colonists choose it by themselves because it is a
     real bed. **Easy**: Defs and the camp order (Y3).
  2. **B2, berths on the road:** a fitted berth is a caravan bed that works **while moving**, so riders arrive rested.
     **Moderate**: two small patches beside Field Tents' and Vehicle Framework's own.
  3. **B3, driving through the night:** with a rested relief driver in a berth, a vehicle caravan need not stop at night.
     **Moderate to hard**, and it changes travel times a great deal.
  4. **B4, sleeping inside a parked vehicle on a map,** chosen by the colonist, warmed by the cab. **Hard**: a job giver of our
     own, and the most to go wrong.
- **The earlier objection, answered:** "a colonist will never choose a berth" is true of a *seat*. It is not true of a bed
  building (B1), of a caravan (B2, B3, where the game hands out beds itself), or of a job giver of ours (B4).
- **Recommendation:** B1 with the camp batch; B2 and B3 together as their own batch after "life aboard" (Y4); B4 last, and only
  if cold-weather camping without a tent still feels missing after B1 to B3.

## What the code says

| Question | Finding [code] |
| --- | --- |
| How does a colonist pick a bed? | The game searches the map for **bed buildings** by Def, best rest first, after the pawn's own bed and their partner's (`full:all.cs:331351-331442`). Anything that is a bed building, reachable and allowed, can be chosen |
| When does a caravan member rest? | When the caravan is not moving, **or the pawn is in a caravan bed**, or is carried (`RimWorld-1.7:RimWorld.Planet.Caravan_NeedsTracker.cs:111-118`). Rest gained is the bed's rest figure |
| Is a vehicle caravan "moving" to that rule? | Yes: Vehicle Framework makes the caravan's own moving flag follow its vehicles (`VehicleFramework-1.6:Vehicles.decompiled.cs:43973,44744-44748`), so riders do **not** rest on the move today |
| Who gets a caravan bed while moving? | In a vehicle caravan, only the sick and carried (`VehicleFramework-1.6:Vehicles.decompiled.cs:44762-44834`). A healthy rider gets a bed only when stopped |
| What counts as a caravan bed? | Packed beds in the inventory; Field Tents adds its own by a postfix on the game's list (`query-patches`: Field Tents, `Caravan_BedsTracker.GetUsableBeds`) |
| When does a vehicle caravan stop for the night? | When anyone needs rest, it is night on the tile, and not every vehicle drives itself (`VehicleFramework-1.6:Vehicles.decompiled.cs:85629-85670`), replacing the game's own rule (`:44470-44478`; `RimWorld-1.7` `Caravan.NightResting`). Vehicle Framework applies this patch by hand, so `query-patches` does not list it |
| Can our code put a pawn aboard and take them off? | Yes: `TryAddPawn(pawn, handler)` and `DisembarkPawn(pawn)`, and a boarding job exists (`VehicleFramework-1.6:Vehicles.decompiled.cs:15192,15256,1140`) |
| Can we add a behaviour to colonists without a patch? | Yes: the game lets a think tree be inserted at named points (`Humanlike_PreMain`, `Humanlike_PostDuty` and others in the game's own Defs; `full:all.cs:260036-260059`) |
| What does a rider feel? | The cab's temperature, through our own hook (`src/PhobosFramework.Vehicles/Service/CabClimate.cs`); no bed memories at all, good or bad, because no bed is involved |

## B1. The roof tent at camp

- **What it is:** a fitting on the roof mount. Real roof tents hinge out over the vehicle's side and stand on their ladder; in the
  game, the camp order (Y3) sets it out as a **two-person bed building with its own canvas roof** on the cells beside the
  vehicle, and packs it away again.
- **Figures [A]:** rest 1.0, comfort 0.8, 55 kg on the roof mount (which it shares with the roof rack and the solar panel:
  one of the three). Cloth, steel and components to make, at the parts bench.
- **Mood:** it is roofed, so no "slept under the stars"; it is not a room, so the game's "slept outside" still applies. Proposed:
  its own small memory, "slept in the roof tent", +4 [A], which cancels it. Dry in rain, unlike a swag.
- **Uses up:** materials, weight, the roof mount.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Two good beds wherever the vehicle parks, up in two clicks, dry in rain | Carrying a roof rack or a roof solar panel on the same vehicle | A choice of roof: more cargo, more power, or a bedroom |

## B2. Berths on the road

- **What it is:** a **cab bunk** (fitting, cab mount, closed cabs on vehicles heavy enough to have a sleeper [A: 3,500 kg and
  up]) or the roof tent's road form is **not** offered: a folded roof tent sleeps no one while moving. So B2 is the bunk.
- **How:** the bunk is a bed thing the fitting holds. We add it to the caravan's bed list (a postfix beside Field Tents'), and
  while the caravan moves we give free bunks to the most tired riders **who are not driving** (a postfix after Vehicle
  Framework's own bed pass, which leaves healthy riders without one). The game's own rule then rests them on the move.
- **Figures [A]:** rest 0.9, comfort 0.7; 40 kg; the bunk takes one seat's place where the cab has more than two.
- **Uses up:** materials, weight, a seat; in the cold, the night heater's fuel to keep the sleeper warm, by rules already built.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Arrive with part of the crew rested | Seating as many people in that cab | Who rides and who sleeps is worth a thought before a long run |

## B3. Driving through the night

- **What it is:** a switch on a vehicle caravan, **Drive through the night** (off by default). With it on, the caravan does not
  stop at night while **every vehicle has a fit driver**.
- **The rule [A]:**
  - A driver is fit while their rest is above 0.3. Below that, the vehicle needs its **relief driver**: a rider who can drive,
    whose rest is above 0.6. They change places, the tired one takes the bunk, and the caravan carries on.
  - With no relief driver for any one vehicle, the whole caravan stops for the night as it does today.
  - People with no bunk simply get more tired; nothing else happens to them.
- **What it costs, all by rules already built or agreed:** fuel for the extra hours; the headlights' amps, or the 0.85 speed of a
  vehicle driving unlit at night (built, Framework 0.106.0); a tired crew at the far end if bunks are short; and, once the gentle
  driver rule (G1, agreed, not built) is in, a tired driver's slower pace.
- **How:** a postfix on the game's night-rest answer, after Vehicle Framework's, that turns a "stop" into a "go" only when the
  switch is on and the rule holds; and the change of places through Vehicle Framework's own seat calls. How a caravan's seats
  are kept while travelling was **not read in full**; that is the first check before building.
- **Balance, honestly:** a caravan that never stops covers roughly twice the ground in a day [I]. That is the point, and it is
  paid for with two drivers per vehicle, a bunk each and the fuel; but it shortens every journey in the game, so it is a
  setting, off by default, and the figures above are for tuning in play.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Reach a distant target, or get the wounded home, in about half the time | Nothing new: with the switch off, all is as before | Crews are built in pairs of drivers; long runs become a plan about people, not only fuel |

## B4. Sleeping inside a parked vehicle

- **What it is:** on a map, a colonist with a bunk assigned goes to the vehicle at bedtime, climbs in, sleeps in the cab's warmth
  and climbs out in the morning, by themselves.
- **How:** a small job giver of ours, inserted at one of the game's own think-tree points, that fires where the game's "go to
  bed" would for a pawn with an assigned bunk, and sends them to board. Aboard, the "life aboard" check (Y4) raises their rest at
  the bunk's figure while the vehicle stands still. When rested, or when their timetable says work, the check sets them down
  beside the vehicle.
- **Assignment:** bunks are assigned per vehicle in a small window, as beds are.
- **What must be right, and why this is last:**
  - **Danger:** a raid, a fire or a draft must set sleepers down at once; nobody may be trapped asleep in a vehicle that is hit.
  - **The vehicle moving:** a driver must not drive off with sleepers unless that is meant; starting the engine warns.
  - **Medical rest never happens aboard:** patients need real beds and doctors (the litter kit is the exception, built).
  - **Order of thinking:** our job giver must not beat the game's handling of mental breaks, duties or emergencies, and other
    mods insert at the same points.
  - **No bed memories:** no bedroom, no barracks, no "slept outside". Proposed: one neutral-to-good memory, "slept in the cab",
    +2 [A].
- **Uses up:** the night heater's fuel in the cold, or the battery for the electric one; a seat.

| Lets the player | Stops the player | Changes how they play |
| --- | --- | --- |
| Sleep warm on a freezing map with no tent and no building | Using that seat for a passenger at night | A heavy truck with a heater becomes a shelter in its own right |

## Other factions' vehicles

| Where | What happens | Lets the player | Stops the player |
| --- | --- | --- | --- |
| **A raid on us** | Nothing runs: raiders do not sleep on a raid, and the lite tier skips the checks | Nothing | Nothing |
| **A base we attack** | With the camp batch, a base's vehicle may stand with its roof tent out (B1), as a plain bed. A sleeper in it is a base's sleeper like any other | Find a camp asleep | Nothing |
| **A vehicle we capture** | Its roof tent or bunk comes with it and follows our rules | Use it | Nothing |

Other factions' caravans on the world map are not simulated person by person, so B2 and B3 do not apply to them.

## Cost to run

- **B1:** none; a bed building.
- **B2:** the caravan's own bed pass, which already runs; a handful of comparisons per vehicle with a bunk.
- **B3:** one check when the game asks whether the caravan rests, and a change of places a few times a day.
- **B4:** the job giver runs only for pawns with an assigned bunk; the rest is the Y4 check.

## Questions for the owner

**Answered by the owner, 13 Nov 2026:** (1) **all four parts, in that order**: B1 with the camp batch, B2 and B3 together later,
then B4, which is wanted and not a maybe; (2) night driving **as a setting, off by default**, at about twice the distance in a
day, tuned in play; (3) the roof tent's memory at **+4**; (4) cab bunks on **heavy vehicles only**, 3,500 kg and up. Nothing is
built yet: each batch waits on the owner's go.

The questions as they were put:

1. **Which parts?** B1 with the camp batch, B2 and B3 together later, B4 last and only if still wanted (agent default), or
   another choice?
2. **B3's size:** is about twice the distance in a day, for two drivers and a bunk per vehicle, the trade wanted (agent default:
   yes, as a setting, off by default), or should night driving be slower still?
3. **The roof tent's memory:** +4 to cancel "slept outside" (agent default), or none?
4. **Bunks on which vehicles:** only heavy ones with room for a sleeper, 3,500 kg and up (agent default), or any closed cab?

## Who chose what

- **Owner (13 Nov 2026):** berths designed properly, against the agent's advice to leave them out.
- **Owner (13 Nov 2026, later):** all four parts in order, B4 included; night driving as a setting, off by default; the roof
  tent's memory at +4; bunks on heavy vehicles only.
- **Agent defaults, owner to revise:** the split into B1 to B4 and their order; every figure marked [A]; the relief-driver rule;
  the switch off by default; no berths for a folded roof tent on the move; the memories.

## Sources

- RimWorld 1.7 (Ludeon Studios), Vehicle Framework 1.6 build (Smash Phil) and Field Tents (lordfelix): read from our decompiles
  and through `scripts/query-patches.py`, summarised in our own words.
- Our own records: [vehicle-accessories.md](vehicle-accessories.md) (the camp order Y3, life aboard Y4),
  [vehicle-hotboxing.md](vehicle-hotboxing.md) (riders and caravans), `vehicle-crosslinks-round-three.md` (G1 the gentle driver
  rule, G9 driving unlit at night).
