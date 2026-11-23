# Standby power

**Status (22 Nov 2026):** built in D'thorga Framework 0.222.0 (`src/PhobosFramework/Power/`, rule `StandbyPowerRule` with 19 tests).
Not yet tested in game: row 341 of the test pass.

The owner's request of 22 Nov 2026: LightsOut and Turn It On and Off - RePowered each took about 13% of every tick in Dubs Performance
Analyzer. "We'll build our own, better, more powerful". This record says what was built and why it costs so little.

## Read this first

- **On by default, for everyone who has D'thorga Framework.** Idle buildings draw 10% of their power; lights in empty rooms go dark.
  A master switch and one switch per kind, under D'thorga Framework's settings, **General, Standby power**.
- **It stands down by itself while LightsOut or Turn It On and Off - RePowered is active**: the three would fight over the same
  buildings. The settings page and the load line say so. Switch them off to use ours.
- **No Harmony patch, and nothing runs every tick.** Each map looks once a second of game time.
- **Safe to add and to remove mid-colony.** Nothing is saved; the game sets every building's draw from its Def again when a map loads.

## Why the two mods cost so much

Read from their authors' public source on 22 Nov 2026, both MIT: [LightsOut](https://github.com/NathanIkola/LightsOut) (Nathan Ikola's repository, uploaded to the Workshop by juanlopez2008) and [Turn It On and Off - RePowered by Mlie](https://github.com/emipa606/TurnOnOffRePowered).
Nothing of theirs was copied: ours is written from the game's own code.

- **LightsOut** (juanlopez2008, updated 17 Oct 2026) patches `CompPowerTrader.PowerOutput`'s getter with a prefix and a postfix. Every
  power grid adds up every building's draw on every tick, so its code runs for every powered building, every tick, though the answer
  changes only when someone starts or stops using the building. It also patches `Pawn_PathFollower.TryEnterNextPathCell`: every step
  of every pawn looks up their room before and after.
- **Turn It On and Off - RePowered** (Mlie, updated 12 Nov 2026) rebuilds its "used last tick" sets every tick, checks every powered
  door (some through reflection) and turret every 4 ticks, and rescans the whole base whenever the count of colony buildings changes,
  so every wall or conduit finished triggers one.
- The two overlap: both put idle workbenches on standby, so each bench's power was managed twice.

A caution the owner was given: Dubs Performance Analyzer adds a little to every call it times, so a method called thousands of times
a tick can look bigger than it is. The real figure is the TPS with the analyzer closed.

## How ours works

- **At start-up** (`StandbyKinds`) a comp goes on every building of a kind below, found by what it is, never by its name (the owner's
  rule: match effects, not names). Only buildings powered by the game's own `CompPowerTrader`, exactly, that draw power: another comp
  class, a generator or a battery manages its own draw.
- **On each map, once a second** (`MapComponent_PhobosStandby`), one walk over its people notes the rooms someone awake is in and the
  buildings someone is working at (at its interaction spot, or beside it) or watching. Then each building of ours is settled. The
  buildings come from a list kept as they are built and taken down (the Framework's `MapRegistry`), so no look walks every building.
- **The draw is written into the game's own power comp only when it changes** (`CompPowerTrader.PowerOutput`, a plain field the
  grids read as they read any other). Nothing intercepts the grids' reading.
- **A light goes dark through the game's own question.** The game asks every comp of a light that implements `IThingGlower` whether
  it should be lit; ours answers no while the room is empty, and asks the light to look again when that changes. No patch.
- **Another mod's draw is left alone.** If a building's draw is neither ours, the listed one nor nothing, another mod sets it, and
  standby power stands down for that building, at full power and lit, until it is built again.

| Kind | Found by | In use while | Setting |
| --- | --- | --- | --- |
| Light | A plain building that glows, needs no ticking, is not a sun lamp and has no schedule | Someone awake is in its room (a wall light: the room it faces). Outdoors: always | Lights go dark in empty rooms |
| Machine | A workbench or research bench class | Someone's job is at it and they stand at it | Workbenches, research benches and televisions |
| Watch | A building with a recreation kind (a television) | Someone is watching it | Same |
| Turret | A turret class | It has a target | Turrets, hydroponics and monitors |
| Grower | A plant grower class (hydroponics) | Anything grows in it | Same |
| Facility | A building with the facility comp (a vitals monitor) | A bed it serves is occupied, or a bench or television it serves is in use | Same |
| Door | A door class | It stands open | Powered doors |

Who counts: people not hostile to the colony (colonists, prisoners, slaves, guests, visitors); animals only with **Animals turn the
lights on** (agent default: off, as LightsOut's own); never mechanoids. A sleeper lights nothing.

For modders: `PhobosStandbyExtension` on a ThingDef, `never` to keep standby power off it, `kind` to take one up as that kind.

## Decisions

- **Home: D'thorga Framework** (the owner's choice, 22 Nov 2026, over a new mod and D'thorga Patches).
- **Scope: all seven kinds** (the owner's choice: lights, benches with research and televisions, turrets with hydroponics and
  monitors, and doors).
- **Standby draw: 10% of the listed power** (the owner's choice: realistic, a setting with 0, 5, 10, 20 and 30%).
- **In use: the listed power**, never more (the owner's choice over Turn It On and Off's 2.5 times).
- **A light that is off draws nothing** (agent default [A]): a switched-off light has no standby electronics.
- **Once a second** (agent default [A]): a light or a bench a second late is not seen in play. At speed 3 that is about a sixth of a
  real second.
- **Doors need no patch** (agent choice): a powered door stays open about two seconds after anyone passes, so the once-a-second look
  catches it. The draw while open is a simplification: a real door's motor draws while moving, not while held open.
- **On by default** (rule: a behaviour that puts nothing at risk starts on). **Risk graded low** (agent grade, for the owner to
  confirm): nothing can be lost. At worst a light comes on a second late, or a grid sized to the bone browns out a moment when many
  machines start at once, as it would in the base game.

## The four questions

- **What it lets the player do:** run more of the colony on the same generators and batteries, and get through the night on less.
- **What it stops them doing:** nothing. A building on standby works exactly as before; a turret on standby still fires.
- **How it changes the way they play:** power planning follows use, not count: a big workshop that sits idle costs little, a busy one
  its full rating. Batteries last longer overnight while the colony sleeps.
- **What outside notices:** the power grids and their batteries, every generator's fuel (a wood or chemfuel generator burns for the
  power drawn), and the colonists, who walk into a dark room and see it light. The base is darker at night where no one is about.

## Limits

- **Not yet tested in game.** The once-a-second look, the light's question and the stand-down for another mod's draw are read from
  the game's code (`CompPowerTrader`, `CompGlower`, `CompPower` of 1.7), not seen running.
- A building that another mod lights or powers by its own rule without changing its draw (a glow it adds itself) is not seen.
- A workbench worked by a machine with no pawn at it (Project RimFactory's assemblers) would sit on standby while working; VFE
  Factory's machines are not workbench classes and are left alone. None of these is on the owner's list.
- Rooms count by the game's own rooms: a light in a huge open-plan room lights whenever anyone is anywhere in it.
