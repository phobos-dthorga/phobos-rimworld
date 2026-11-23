# Walk-in cabins for our vehicles (22 Nov 2026)

**Status (22 Nov 2026):** built, not yet tested in game: D'thorga Framework 0.211.0 (the cabin), 0.211.1 (people in it count as aboard and
share the seats' limit), 0.230.0 (the cab's air and food from the cargo) and D'thorga Vehicles 0.96.0 (the Osprey's layouts).
Test rows 314, 349 and 350.

## Read this first

- **The owner's request (22 Nov 2026):** after seeing the UH-60M Black Hawk's walkable interior, "let's enable it for our Osprey, but have it
  as a setting people can turn off if they so wish". Built on RimWorld's own pocket maps (the game's small maps that are on no world tile,
  added with Anomaly; the game code is in every copy, DLC or not).
- **The owner's choices (22 Nov 2026, question cards):** people **go in by an order**, and the seats work as before; the cabin **exists only
  while someone is in it**; its **layout follows the cabin's fittings**; the setting **starts on**.
- **Written from the game's own code**, never from the Black Hawk's: its author's way of doing it was read only to judge that mod.
- **Not yet tested in game.**

**Since Framework 0.217.0 a cabin can also be a deck tent** (a tent on a flat deck, with beds, the outside air and its own rules):
[vehicle-accessories.md](vehicle-accessories.md#built-tents-for-every-vehicle-22-nov-2026), step 3. What follows is the cabin as built
for the Osprey.

## The four questions

| Question | Answer |
| --- | --- |
| What it lets the player do | Walk colonists into the Osprey's cabin to wait, eat, rest or tend the wounded, on the ground or in flight; carry a downed colonist in, where the cabin's litters (with the litter kit) take them and doctors tend them on the way home |
| What it stops them doing | Nothing they could do before: the seats and boarding are unchanged. A colonist in the cabin cannot leave while the aircraft is in the air |
| How it changes play | A medical evacuation becomes a flight with treatment aboard, not a wait for landing; a long transport leg is a place to sit, not a vanishing |
| What outside the vehicle notices | The game's own needs, moods and doctors work inside as anywhere; the map it left stays open while people are in the cabin and the aircraft is away (the game's own rule for pocket maps), and closes once it lands elsewhere |

## How it works

- **A layout in XML** on the vehicle's cabin comp: rows of characters, `S` a seat, `L` and `l` a litter's head and foot, `T` a fuel tank
  (blocking), `E` the way out, `.` floor. The first layout whose fittings are all fitted is used, so a litter kit or a ferry tank changes the
  cabin. The cabin map is the layout's size: its edge is the cabin's wall, so no wall is built.
- **Going in:** right-click the vehicle with colonists selected and choose to go into the cabin, or to carry a downed colonist in. They walk to
  the vehicle, wait a moment, and appear at the way out inside. Drafted stays drafted, as through the game's own portals.
- **Leaving:** the way out inside, by right-click or its button, puts them beside the vehicle wherever it now stands. Refused, with the
  reason, while it is in the air.
- **When it exists:** made when the first person goes in; removed on the vehicle's rare tick once nobody is inside and the vehicle stands on
  a map, anything left inside set down beside it.
- **One limit with the seats (Framework 0.211.1; owner, 22 Nov 2026):** those in the cabin and those in the passenger seats are counted
  together against the passenger seats (24 on the Osprey), and the cabin holds no more than its own seats and litters. Going in when full
  is refused; a passenger seat cannot be taken once both are full between them (a postfix on Vehicle Framework's seat check, which only
  ever answers "full"). People in the cabin weigh on the aircraft and need oxygen at height as seated passengers do.
- **In flight:** the cabin map stays where it is. The game will not close the map the vehicle left while people are in the cabin; when the
  vehicle lands, the cabin is pointed at its new map and the old one may close.
- **The air inside (Framework 0.230.0; row 349):** the game gives a pocket map one fixed temperature from its Def (21 degrees C), with no
  per-map figure. The deck tent's postfix on `MapTemperature.OutdoorTemp` now answers for every open cabin: a walk-in cabin takes its
  vehicle's cab temperature from `CabClimate` (the Cabin system's heater, air conditioning and drift to the weather), and a vehicle with no
  cab climate of ours keeps the 21. A loose end the owner asked to have tidied (22 Nov 2026). Other mods patch the same getter (Deep And
  Deeper with a prefix, Vanilla Gravship Expanded with a postfix); ours answers only for our cabins' maps.
- **Food (Framework 0.230.0; row 350):** nobody inside could reach the vehicle's cargo. On the vehicle's rare look, anyone hungry in the
  cabin eats the best food in the cargo they can eat now, by the game's own caravan rules (`CaravanPawnsNeedsUtility`), eaten whole as a
  caravan's people eat. **Agent's choice:** no ration locker or galley; the cargo is the larder. **Settled (22 Nov 2026):** Vehicle Framework
  keeps an aircraft in flight among the world's pawns and ticks it there (its source: `AerialVehicleInFlight` does not tick its
  contents, the world's pawns do), so the cabin's people eat in flight; life aboard runs there too since Framework 0.236.0.
- **If the vehicle is lost:** everyone inside, and what was left there, is set down beside the wreck, or, if it was lost away from any
  map, on the map the cabin was last pointed at.

## Risk

**Low** (agent's grade, for the owner to confirm): it only moves the player's own people between two places, never harms them, and every
way it could strand someone (a flight, a closed map, a lost vehicle) puts them back on a map. Its cost is one small map's ticking while
anyone is inside. The setting, **Walk-in cabins**, starts on.

## Other factions' vehicles

The cabin is for the player's people: another faction's vehicle offers no cabin order, and a captured one offers it once it is the
player's.

## Proposals for later (23 Nov 2026)

The owner asked, after the C-17 joined the fold, whether walk-in cabins belong anywhere else, then: "Write down the two proposals for
later then." **Nothing is built or decided**; both wait on the gate below. Agent's proposals, ranked.

**The gate, first: measure the Osprey's.** Its cabin is not yet tested in game (rows 314, 349, 350), and its cost was never measured: a
cabin ticks one small map for as long as anyone is in it. The owner's standing view is that realism costing frame rate feels like lag.
So, in order: the owner tests the Osprey's cabin; then a `perf.py` recording (asked for first, the game saved) with someone in the cabin,
against one without. A small cost opens the two proposals; a large one sends the cabin itself back for a cheaper design first.

| # | Proposal | Why | What it would take | Waits on |
| --- | --- | --- | --- | --- |
| CB1 | **A crew room on Vanilla Vehicles Expanded's Goliath** | Its own description calls it "more of a movable mini-base than a deployment pod": a 5 by 5 craft with four turret stations. Bunks, and later a workbench, fit what it already is | A loadout entry with `CompProperties_PhobosVehicleCabin` layouts, as the Osprey's: bunks first, a workbench row once fitted (layouts follow fittings). No new code expected; its own art for the room | The gate above |
| CB2 | **A flying hospital on the C-17 Globemaster III** | Real C-17s fly aeromedical evacuation, up to 36 litters; the Osprey's litter layout would suit its hold | A layout following a litter fitting, sized for the hold. **First check its author's updates**: z3nt3 gave the Black Hawk an interior of their own (a pocket map), and may do the same for the C-17; two interiors on one aircraft would clash, so ours waits until theirs is ruled out or can host ours | The gate, and the author's next updates ([c17-globemaster.md](c17-globemaster.md)) |

**Weighed and set aside (agent's judgement, 23 Nov 2026):** trucks and trailers (the cab bunk, field kitchen and shower trailer already
cover living aboard without a second map); the jets and attack helicopters (nowhere to walk); the trains (our loadouts cover the
locomotives only; coaches would first need Trains of the Rim's carriages taken into the fold).

