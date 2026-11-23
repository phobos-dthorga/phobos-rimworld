# Ventilation and hydrogen: design (7 Nov 2026)

A design record, nothing built. The owner's call (7 Nov 2026): the oxygen electrolyser's hydrogen becomes **a flammable room gas of
ours**, and the colony gets **vent-like furniture** to deal with it: a powered extractor fan, a passive roof or wall vent, a fume hood
and a hydrogen detector. How gases work is in the [D'thorga Framework record](phobos-framework.md); the electrolyser is in the
[medical equipment record](medical-equipment.md).

**Merged here on 17 Nov 2026**, each kept as written: [Workstation environmental effects: design](#workstation-environmental-effects-design-7-nov-2026).

How gases and air works now: [systems/gases-and-air.md](systems/gases-and-air.md); this record keeps its history.

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
| Which mod holds the vent buildings? Agent lean: **D'thorga Burns and Smoke**, which owns room hazards (smoke, carbon monoxide), with the services in the framework. The alternatives are Medical Equipment (beside the electrolyser) or a small new mod. **Since 7 Nov 2026 Burns and Smoke and Medical Equipment are both parts of [D'thorga Medical](../../mods/PhobosMedical/README.md)** ([why](mod-consolidation.md)), so either answer means D'thorga Medical | Owner |
| Should a flash fire also damage pawns in the room directly, or only through the game's fire? | Owner, after play |
| Do the game's own vents (Odyssey's, vanilla's) count as passive vents for our gases? Inference first, under the gas rule | Agent, from the code |
| The hydrogen figures above, before any Def carries them | Agent: find sources |

## Workstation environmental effects: design (7 Nov 2026)

*Merged into this record on 17 Nov 2026 from `workstation-environmental-effects.md`; kept as written.*


A design record. **Nothing is built and nothing is tested in game.** The owner asked for it on 7 November 2026 ("design record now");
building waits for the owner's go. The idea is the owner's, from 5 November 2026: workstations give off gases and other environmental
effects, and players can switch them on a kind at a time in a running colony.

### Read this first

- **Owner's rules that shape it:** effects are read from what a building does, never from a list of workstation names
  ([match effects, not names](../../AGENTS.md)); existing gases and effects first, our own gas only where it earns its place; gases are
  additive and per room, with no air, pressure or vacuum model ([D'thorga Framework](phobos-framework.md)); text a player reads in play
  names no mod or setting.
- **Switched on a kind at a time, mid-save** (owner): every category below is its own setting, off or on at any point in a colony, with
  nothing saved that a later switch could break.
- **It needs a way to clear the air first.** The vents (extractor fan, passive vent, fume hood) wait with hydrogen
  ([ventilation and hydrogen](ventilation-and-hydrogen.md), deferred by the owner on 7 Nov 2026). Without them the only counterplay is an
  open door or a bigger room, so the first build should stay mild or wait for the vents. **For the owner to decide.**
- Every figure here is a proposal, not tuned.

### What a workstation does, read from its Def (agent proposal)

| Category | How it is found (by effect) | What it gives off while worked | What it does to people |
| --- | --- | --- | --- |
| Combustion | A fuel comp whose fuel filter takes wood or chemfuel, or a heat pusher, on a work table | Smoke (the game's) and carbon monoxide (the Framework's room gas) | The burns part's smoke and carbon monoxide, as from a fire, scaled down |
| Solvents | A work table whose bills make drugs or chemicals (products in the drug or chemical categories, or ingredients such as neutroamine or chemfuel) | A new "solvent fumes" room gas, if the gas rule allows it | Mild nausea and headache; a light chemical lung injury from long hours |
| Refining | A building whose products include chemfuel or other fuels (Rimefeller and VFE Factory's refiners are found the same way) | Fuel vapour | Mild; a fire hazard is the interesting part, held for the owner |
| Metal fumes | A work table whose bills use the smithing or machining skill with metal ingredients | Metal fumes (welding and smithing) | A light chemical lung injury over long shifts; a mask helps |
| Stink | Butchering and corpse handling | The game's rot stink | The game's own effect (lung rot exposure); nothing new |

**Whether a table is worked:** the game already tracks whether a work table was used this tick, the flag fuel-burning benches use to
burn fuel only while someone works at them. Emitting only while that is true keeps idle benches silent and costs nothing when the colony
is asleep. To check against the 1.7 decompile when it is built.

### How it would run (agent proposal)

- **One comp added at load** to every work table a category matches, by a small indexer, as the medical parts index devices; no
  Harmony. It reads the in-use flag on a rare tick and adds its gas to the room through the Framework, the same path as the burns part's
  carbon monoxide.
- **Per-room, additive, and diluted by room size**, as every Framework gas is: a forge in a hall barely shows; a forge in a closet does.
- **Saved:** nothing. The comp's state is derived; switching a category off stops the emission at once, and the gas already in a room
  fades the Framework's way.
- **Performance:** one check per worked table per rare tick; nothing while idle.

### The gas rule, argued per gas

| Gas | Existing effect to reuse | Own gas? |
| --- | --- | --- |
| Smoke | The game's smoke, Combat Extended's smoke inhalation | No |
| Carbon monoxide | The Framework's carbon monoxide room gas, already used by fires | No new gas |
| Rot stink | The game's | No |
| Solvent fumes | None found in the owner's mods (to search again when built) | Yes, if the owner wants the drug lab to matter |
| Fuel vapour | None found; Rimefeller has its own refinery gases to check first | Only after checking Rimefeller |
| Metal fumes | None found | Possibly folded into a general "fumes" gas with solvents |

### Open questions for the owner

| Question | Agent lean |
| --- | --- |
| Build before the vents, mild, or wait for the vents? | Wait for the vents, or build combustion only (it reuses everything) |
| One general "fumes" gas, or solvent, vapour and metal fumes separately? | One general gas: fewer settings, the same play |
| Should fuel vapour be a fire hazard (a spark near a refinery)? | Interesting, but its own design record |
| Which categories start switched on in a new colony? | Combustion only; the rest off until chosen |

### Sources

Our own reading of the game's Defs and the owner's mod list; no outside figures are used. The owner's idea and rules are recorded in
the project memory and [AGENTS.md](../../AGENTS.md).
