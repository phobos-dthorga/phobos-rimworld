# The service yard: servicing, tuning, fuelling and parking in one place (20 Nov 2026)

**Status (20 Nov 2026):** design only; nothing is built. To be built once the owner has tried bay trips in play (test rows 294 to 297),
so it builds on what works (owner's choice).

**The owner, 20 Nov 2026:** "Maybe we should develop a bay of sorts, with dyno functionality, but it's a larger zone on its own and meant
for servicing and parking in one?" Then: "It could also have the role of refilling fuel too", "It should be something with a specific, or
at least, a restrained size so that the player cannot build something of unlimited size and start cheesing things", and "We should make
Fleet view a practical part of this too."

How to read it: **[O]** is the owner's choice (cards, 20 Nov 2026); **[A]** is the agent's choice or figure, for the owner to revise;
**[I]** is our inference. It builds on [bay trips](vehicle-bay-trips.md), the dyno bay, the depot bay and the vehicle park.

## Read this first

- **A zone of its own, with a size it cannot grow past** [O]. Its allowance comes from the equipment built in it: a bigger yard has to
  be paid for. **A dyno bay is required**; a yard without one is not a yard.
- **The vehicle park stays as it is**: plain parking, any size. Parking alone gives nothing to cheat with; servicing does.
- **One vehicle at a time on each dyno bay, with no moving** [O]: vehicles take turns where they are parked. A labelled
  simplification: a real chassis dyno holds the vehicle on its rollers.
- **A new fuel pump building** [O] fills the yard's vehicles in turn. It is the only new picture this needs.
- **The Fleet board is where a yard is run** [O]: its vehicles, its queues, its fuel and its room, with the orders that go with them.
- **Bay trips aim at a yard first**, and a vehicle repaired there stays parked there. It is moved out only to make room for one that
  needs the yard.

## Why it beats a lone dyno bay

Today a broken vehicle goes park, dyno bay, park: two drives, two drivers, and a bay held while it waits for work. In a yard it goes
once and stays; the mechanics and the pump come to it. With dozens of vehicles that is far fewer trips, fewer colonists taken from their
work to drive, and less traffic round the bay.

## What a big yard would let a player get away with

| What the yard gives | Grows with its size? | Already limited by |
| --- | --- | --- |
| Faster repair work | **Yes**: every vehicle in it | Only the number of mechanics |
| Dyno work, and new parts at 60% health | No | One vehicle at a time per dyno bay |
| Fuel | No | The pump's rate, one vehicle at a time |
| Parking | No | Gives nothing by itself |

**The work bonus is the one to hold down** **[I]**: one huge yard would give every vehicle the depot bay's speed-up for the price of one
dyno bay, and depot bays would be pointless. Hence the size limit, and a bonus a little smaller than the depot bay's own.

## The four questions

- **What it lets the player do.** Run a motor pool as one place: broken and worn vehicles come in, are fixed, tuned and fuelled where they
  stand, and wait there for their next job.
- **What it stops.** A yard as big as the base: its size is held to what its equipment pays for. Also vehicles shuttling between a bay
  and a park.
- **How it changes play.** The colony plans a yard: roomy ground by the parts bench, a dyno bay, a pump on the chemfuel line, a roof if
  it can. Depot bays stay the place for a vehicle's own home and pad.
- **What outside the yard notices.** Mechanics (faster work in the yard); haulers (the pump's tank where it has no pipe); the room it
  stands in, if roofed (an engine running indoors fills it with carbon monoxide, under the existing **Exhaust in closed rooms**
  setting); the Fleet board (below); the alert for vehicles that cannot get a place. **Open [I]:** whether a roofed, heated yard spares
  engines the cold-start problem depends on which temperature the cold start reads, not yet checked.

## The zone and its size [O]

- **Marked like any zone**, from the motor pool tab, and grown with the same tool while one is selected.
- **Its allowance is the sum of its equipment's** [A], counting equipment standing in the yard or touching its edge:

  | Equipment | Adds | Why it adds [A] |
  | --- | --- | --- |
  | Dyno bay (required) | 80 cells | Room for two big trucks or four cars round it: a HEMTT stands on 3 x 10 cells |
  | Fuel pump | 40 cells | A forecourt's worth |
  | Inspection pit | 40 cells | A bay's worth |

- **The tool refuses cells past the allowance and says why**, with the yard's cells and its allowance. It never draws past it.
- **Equipment lost makes a yard too big; it is never shrunk for the player.** It keeps its cells, stops giving its services, and says
  it is too big by so many cells until it is trimmed or equipped again (the rule against silently deleting the player's things).
- **One yard per zone**; several yards are fine, each held to its own allowance.

## What the yard gives

- **Faster work**: jobs on a vehicle in the yard take 80% of the usual work [A], where a depot bay's own vehicle takes 70%. A vehicle on
  its depot bay's pad inside a yard takes the better of the two.
- **The dyno, one at a time** [O]: each working dyno bay in the yard has one turn. The vehicle whose turn it is counts as at the dyno
  (tunes and changes of grade, the heavy overhauls, a diagnosis, new parts fitted at 60% health); the rest wait. Turns go in the order
  vehicles asked, longest waiting first [A], and a vehicle gives up its turn as soon as nothing it waits for needs the dyno. A vehicle
  standing in a dyno bay's own reach outside the yard takes turns in the same line.
- **Fuel** [O]: see the pump, below.
- **Parking**: a vehicle repaired in the yard stays there.

## The fuel pump [O]

- **A small building** (1 x 1 or 1 x 2 [A]) with a tank of its own, filled from the chemfuel pipe network as the depot bay is, or by
  haulers where there is no pipe. Needs power, as the depot bay does.
- **It fills one vehicle at a time**, the emptiest first [A], at the depot bay pump's rate (30 an hour, authored), every vehicle standing
  in the yard it serves.
- **One fuel**: a vehicle of another fuel is told so, as the depot bay tells it. Electric vehicles would want a charging point; a later
  idea, not in this design.
- **Art**: one new picture, a fuel pump. Generated with the owner's go and the game closed, before the first test (the owner's rule:
  real art before a first test). The yard's tool icon is drawn in code, as the vehicle park's is.

## Bay trips and the yard

- **A yard comes first.** A vehicle with work that sends it looks for a yard with the equipment that work needs and room for it, then a
  dyno bay's reach as now. Driven, towed or pushed as now.
- **It stays.** Its work done, it is not driven home.
- **Room is made when it is needed** [A]: when a vehicle needs the yard and it is full, the vehicle that has stood there longest with
  nothing left to do is taken out (to its depot bay, the vehicle park or nearby, as now). Nothing is moved out for any other reason.
- **The player's own parking is left alone**: a vehicle the player parked in the yard counts as parked there, and is moved out only
  under the same rule.

## The Fleet board [O]

The owner: "We should make Fleet view a practical part of this too." The board becomes where a yard is run. What follows is the agent's
proposal [A]; all of it presentation, every button one call to the yard's service.

- **A group for each yard**, under its map: its name, its cells against its allowance, its equipment and whether each works, and how
  many vehicles it holds.
- **The yard's queues**: whose turn it is on each dyno bay and who waits next, and the pump's turn and its fuel left. **Move up** puts a
  vehicle first in a queue (the player's word overrides the order of asking).
- **For each vehicle**: what it is doing in the yard (waiting for the dyno, on the dyno, being fuelled, done and parked), and what it
  waits for. **Send to yard** orders a vehicle in, by bay trip, choosing the yard when there are several; **Take out of yard** moves it
  out now. The bay trips switch stays where it is.
- **Filters**: in a yard; waiting for a yard; cannot get to one.
- **Go to** jumps to the yard on the map, as it jumps to a vehicle now.

## Risk and settings

| Behaviour | Risk [A], for the owner to confirm | Setting | Starts |
| --- | --- | --- | --- |
| The yard itself | None: the player builds it, and it only speeds work, fills tanks and holds vehicles | None | |
| Vehicles moved out to make room | **Low**: a vehicle with nothing left to do is driven a short way, under the bay trips switch | Bay trips' own | On |

## Other factions' vehicles

A yard is the player's and serves only the player's vehicles. A raider's vehicle in it is ignored; one captured there joins the queues
once it is the player's. Nothing here looks at another faction's vehicles, so it costs nothing in a raid.

## Cost to run

The allowance is counted again only when the zone or its equipment changes. Whether a vehicle is in a yard is one zone lookup. The
queues are short lists, looked at on the rare tick; the dyno's "at the rig" answer is already kept for a second (`VehicleFittings.NearRig`).
The pump fills on the rare tick, as the depot bay's does. The Fleet board's yard group is built when the board is, as its rows are.

## Authored figures [A]

| Figure | Value |
| --- | --- |
| Cells a dyno bay, a fuel pump and an inspection pit add | 80, 40, 40 |
| Share of the usual work in a yard | 80% (the depot bay's own pad: 70%) |
| The pump's rate | 30 an hour, as the depot bay's |
| Queue order | longest waiting first; the player's Move up first of all |

## Open questions for the owner

1. The figures above, once seen in play.
2. Whether a vehicle should stay in the yard after its work (as designed) or go home as bay trips do now.
3. A charging point for electric vehicles: worth a later design?
4. The yard's name in the game: "service yard" is the agent's pick.

## Who chose what

- **Owner (20 Nov 2026):** the yard, servicing and parking in one; fuel too; a restrained size, held by a cap that grows with its
  equipment; one vehicle at a time per dyno bay, without moving; a new fuel pump building; the Fleet board as a practical part of it;
  designed now and built after the bay trips test.
- **Agent:** everything marked [A]; that the vehicle park stays unlimited parking and the yard is its own zone (from the owner's size
  limit); that a dyno bay is required; the work bonus a little under the depot bay's; making room only when needed; the Fleet board's
  parts; the risk grades.
