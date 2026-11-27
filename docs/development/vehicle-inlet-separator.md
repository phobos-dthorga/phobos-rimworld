# An intake separator for turbine aircraft (28 Nov 2026)

**Status (28 Nov 2026):** a design record; nothing is built. The owner chose by card to build all three of the schematic's missing parts,
"each with its own record first" ([the schematic record](vehicle-schematic.md), M2). Building waits for the owner's answers below.

## Read this first

- **What it is**: a switch on turbine aircraft that throws dust and grit out of the intake before it reaches the engine, at a small
  cost in power. On over rough or dusty ground, off for a heavy lift on a hot day.
- **It works through what already exists**: landing dust, grit strikes and compressor fouling all fall while it is on. No new kind of
  harm is added.
- **The inlet barrier filter we already ship gets clogging** (IS4): today it costs a fixed share of power for ever.
- **The schematic record leaned the other way** (M2: fold it into compressor fouling); the owner chose to build it, so this record
  designs it to add a choice, not a second dust story.
- **Nothing here is tested in game.** Marks: **[O]** the owner's, **[A]** the agent's, **[code]** read from our source or Defs.
  Real-world statements from general knowledge, not looked up, say **unverified**.

## What a separator does

Some turboprops have an inertial separator the pilot selects in icing or dust: vanes turn the air sharply, heavier particles carry on
out of a bypass duct, and the engine gives a little less power and runs a little hotter while it is on. Many helicopters use inlet
barrier filters instead, which clog and need cleaning, and some turboshaft engines have a particle separator built in and always on
(general knowledge, **unverified**: no source was looked up, and which real engines have which is not checked).

## What exists to build on [code]

| Piece | State | Where |
| --- | --- | --- |
| Landing dust | Each lift-off or touch-down on dry loose ground costs each engine 0.5 % of its health; an inlet barrier keeps 80 % out | `LandingSurface.xml` |
| Grit strikes | Stones thrown into the intakes on rough ground, by the loadout's exposure; a strike costs 15 % | `StripDebris`, `intakeDebris` |
| Compressor fouling | Grows faster on rough ground, by the same exposure; costs up to 5 % of output | `CompressorFoulingRule` |
| The inlet barrier filter | An upgrade with a fixed power cost (`Phobos_InletBarrierLoss`) | `AircraftUpgrades.xml` |
| Switches | Loads with a toggle, and "automatic" loads that switch themselves | `ElectricalLoadSpec`, `CompPhobosVehicleSystems.Automatic` |

## The model [A]

| Row | What |
| --- | --- |
| IS1 | **A separator switch** on every aircraft with a gas turbine and no inlet barrier: on, it costs 3 % of power and raises turbine temperature as that demand would |
| IS2 | **What it keeps out while on**: half the landing dust, half the grit strikes, half the compressor fouling's rough-ground share |
| IS3 | **Automatic by default**: on over rough or dusty ground and while taking off or landing there, off elsewhere; the player can hold it on or off, as with an automatic load |
| IS4 | **The inlet barrier filter clogs**: a quantity `barrierClog` grows with the dust it stops; its power cost rises from its fitted share to twice that when fully clogged, with an amber lamp at 0.7 |
| IS5 | **The compressor wash cleans the barrier too**, with no new service |
| IS6 | **The schematic's separator is real** on the turbine pages, lit while on |

IS1's 3 %, IS2's halves and IS4's doubling are authored balance, not figures from a source.

## What it changes in play

- **Rough strips get a choice**: protect the engines and lose a little lift, or take the full power and the wear.
- **A heavy hover on a hot day** may need the separator off, which the hover check already judges.
- **The inlet barrier stops being free once fitted**: it needs a wash now and then.

## Other factions' vehicles

- **In a raid on us, or parked at a base we attack**: nothing; the lite tier keeps no wear.
- **Captured**: the switch on automatic, the barrier clean (agent default).

## The four questions

- **What it lets the player do**: trade a little power for engine life over rough and dusty ground.
- **What it stops them doing**: nothing; at worst a hover that would have just held now needs the switch off.
- **How it changes the way they do things**: where an aircraft lands and how heavily it is loaded meet in one switch.
- **What outside the vehicle notices**: **the ground**: desert and dry maps now ask for the switch. Nothing else, as a choice.

## Risk and settings

**Low** (the agent's grade, for the owner to confirm): a few percent of power, chosen by the player, and an automatic setting that
judges the ground. **Its own setting, "Intake separators", on by default.** Off, no switch is offered and nothing changes from today.

## Cost

A stat factor read where power is already worked out, one switch, and one quantity on the 250-tick update for a fitted barrier. Nothing
per tick.

## What a next game would need

The rule (a power share against shares of dust kept out) is game-free. A next game needs its own dust and wear events.

## Questions for the owner

On the Decisions page (`config/decisions.json`), each with the agent's lean.

1. **A switch, or always on?** Lean: a switch, automatic by default.
2. **Should the inlet barrier filter clog?** Lean: yes, cleaned by the compressor wash.

## Sources

- How separators and barrier filters work: general knowledge, **unverified** here.
- Our own records: [the schematic](vehicle-schematic.md), [the compressor wash](compressor-wash.md), [per-engine realism](per-engine-realism.md), [flight](systems/flight.md).
