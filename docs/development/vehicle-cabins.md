# Walk-in cabins for our vehicles (22 Nov 2026)

**Status (22 Nov 2026):** built, not yet tested in game: D'thorga Framework 0.211.0 (the cabin) and D'thorga Vehicles 0.96.0 (the Osprey's layouts).
Test row 314.

## Read this first

- **The owner's request (22 Nov 2026):** after seeing the UH-60M Black Hawk's walkable interior, "let's enable it for our Osprey, but have it
  as a setting people can turn off if they so wish". Built on RimWorld's own pocket maps (the game's small maps that are on no world tile,
  added with Anomaly; the game code is in every copy, DLC or not).
- **The owner's choices (22 Nov 2026, question cards):** people **go in by an order**, and the seats work as before; the cabin **exists only
  while someone is in it**; its **layout follows the cabin's fittings**; the setting **starts on**.
- **Written from the game's own code**, never from the Black Hawk's: its author's way of doing it was read only to judge that mod.
- **Not yet tested in game.**

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
- **In flight:** the cabin map stays where it is. The game will not close the map the vehicle left while people are in the cabin; when the
  vehicle lands, the cabin is pointed at its new map and the old one may close.
- **If the vehicle is lost:** everyone inside, and what was left there, is set down beside the wreck, or, if it was lost away from any
  map, on the map the cabin was last pointed at.

## Risk

**Low** (agent's grade, for the owner to confirm): it only moves the player's own people between two places, never harms them, and every
way it could strand someone (a flight, a closed map, a lost vehicle) puts them back on a map. Its cost is one small map's ticking while
anyone is inside. The setting, **Walk-in cabins**, starts on.

## Other factions' vehicles

The cabin is for the player's people: another faction's vehicle offers no cabin order, and a captured one offers it once it is the
player's.
