# Ventilation and hydrogen: design (7 Nov 2026)

A design record, nothing built. The owner's call (7 Nov 2026): the oxygen electrolyser's hydrogen becomes **a flammable room gas of
ours**, and the colony gets **vent-like furniture** to deal with it: a powered extractor fan, a passive roof or wall vent, a fume hood
and a hydrogen detector. How gases work is in the [D'thorga Framework record](phobos-framework.md); the electrolyser is in the
[medical equipment record](medical-equipment.md).

## Read this first

- **Hydrogen earns its place under the gas rule** (owner's call-out, 7 Nov 2026): nothing in the owner's mods carries it, and a room
  filling with a flammable gas from a machine is play inference can't give. It is additive and per room, like carbon monoxide: it
  exists only while something adds it, with no air, pressure or vacuum model.
- **Nothing here is sourced yet** except where a link is given. The figures below are from memory and marked unverified until
  [radio and chemistry checks](radio-reference-checks.md)-style records show a source.
- **The vents are the first of our buildings that take a gas away.** They help with carbon monoxide and the framework's smoke too,
  which is most of their value outside the electrolyser.

## Hydrogen

| Property | Value | State |
| --- | --- | --- |
| Made by the electrolyser | Twice the oxygen's volume (2 H2O gives 2 H2 + O2) | Chemistry, exact |
| Flammable in air | About 4% to 75% by volume | From memory, unverified |
| Lighter than air | About a fourteenth of air's density, so it rises and gathers under a roof | From memory, unverified |
| Ignition energy | Very low: a small spark lights it | From memory, unverified |

**Proposed rule (agent proposal):** the electrolyser emits hydrogen into its room through the framework's existing
`CompPhobosGasEmitter`. A room's hydrogen share comes from the gas layer as carbon monoxide's does, scaled by the room's size. Above
the lower flammable limit, any fire, spark or explosion in the room ignites it: a flash fire across the room, burning the gas off.
Below the limit nothing happens. Outdoors it never builds up. Every game figure (emission per hour, the share that counts as 4%, the
flash's damage) is authored and lives in a rules Def.

**What the player sees (agent proposal):** the electrolyser's inspect line names the room's hydrogen in plain words ("the air in here
is building up hydrogen"), a detector warns before the flammable share, and the Engineer display mode shows the percentage.

## The four vents

| Building | What it does | Needs | State |
| --- | --- | --- | --- |
| **Extractor fan** | Pulls the room's gases outside through a wall: hydrogen, carbon monoxide and the framework's smoke | Power, built in an outside wall | Owner's pick, design below |
| **Passive vent** | Lets light gases (hydrogen) escape slowly through a roof or wall, no power | An outside wall or a roofed cell under open sky | Owner's pick |
| **Fume hood** | Catches gas at its source: beside an emitter, most of what it emits never reaches the room | Power, adjacent to the emitter | Owner's pick |
| **Hydrogen detector** | A wall alarm that warns before the room reaches the flammable share | Power | Owner's pick; pairs with the planned carbon monoxide alarm |

**Proposed rules (agent proposal):** each vent removes a share of a room's gas per hour (the fan most, the passive vent least, and only
light gases); the fume hood multiplies its neighbour's emission by a factor below one. These are game-free rules with unit tests,
like the framework's gas mix. A vent works only where it can reach outdoors, which the framework's enclosure rule already tells.

## Open questions

| Question | For |
| --- | --- |
| Which mod holds the vent buildings? Agent lean: **D'thorga Burns and Smoke**, which owns room hazards (smoke, carbon monoxide), with the services in the framework. The alternatives are Medical Equipment (beside the electrolyser) or a small new mod | Owner |
| Should a flash fire also damage pawns in the room directly, or only through the game's fire? | Owner, after play |
| Do the game's own vents (Odyssey's, vanilla's) count as passive vents for our gases? Inference first, under the gas rule | Agent, from the code |
| The hydrogen figures above, before any Def carries them | Agent: find sources |
