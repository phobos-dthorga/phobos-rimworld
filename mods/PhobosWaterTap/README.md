# D'thorga Water Tap

A water bottle tap for **Dubs Bad Hygiene**: a small plumbing fixture that keeps a stack of bottled water ready and refills it by
itself from your water towers. Nobody has to stand at a basin filling bottles.

Version 0.2.1, built 4 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Needs Dubs Bad Hygiene, with its thirst option on.** Without thirst, Dubs Bad Hygiene has no water bottles, so the tap doesn't
  appear at all. **It isn't made for Dubs Bad Hygiene Lite**, or for the full mod's lite mode: neither has plumbing. Load it after
  Dubs Bad Hygiene. The About file asks for this, so RimSort does it for you. It needs nothing else, not even Harmony.
- **It needs a pipe and a water tower.** Research **plumbing**, then find it under **Architect > Hygiene**. Build it on a pipe or next
  to one. Wells and pumps fill towers, and the tap draws from towers only, like every other Dubs Bad Hygiene fixture.
- **It replaces a pipe it's built on, and then carries the water itself**, like a basin does. If you deconstruct it, lay a pipe back on
  that cell or the network may be cut in two.
- **The colony limit stops a flood, and starts at 50 bottles a map.** Once a map holds that many, every tap on it stops filling. **Change it
  with Colony limit -10 and +10 on any tap.** Raise it for a big colony or before a big caravan. It counts bottles in storage or on the floor,
  **not in pockets, and not riding on conveyor belts**, so a long jammed belt can hold bottles above the limit.
- **Pawns only carry bottles away if you tell them to.** The tap's storage priority starts at Important, so bottles stay put until
  someone drinks or packs them. If you lower it, or give water a Critical stockpile, haulers carry bottles off and the tap refills, up to the
  colony limit.
- **This changes every water bottle in your game, not just the tap's** (see [Water quality](#water-quality)). A bottle from untreated water
  now stays untreated when its stack is split. Before, it came out clean by accident. Colonists on untreated water can now get sick from
  bottles, as they already can from a basin.
- **Combat Extended players:** a pawn on a custom loadout with "drop undefined" drops the bottles it packs, as Dubs Bad Hygiene's own
  tooltip warns, and haulers may then carry them back to the tap. Add a water slot of 3 to that loadout.
- **Safe to add to a running colony.** Existing bottles keep working, and any that were already split read as treated.
- **Safe to remove, once you deconstruct your taps.** A tap left standing vanishes with the mod and leaves a gap in the pipe. The bottle
  rule saves nothing, so bottles are unaffected. Each map saves its colony limit, so **the first load after removing the mod logs two errors
  about the missing class** (read from the game's code, not yet seen in game). The game drops it and goes on, and they stop once you save.
- **Not yet tested in game.** Everything here is read from the game's and the mods' code, and built and checked offline.

## What it does

Each rare tick (250 game ticks), a tap with room makes one bottle. Colonists drink from it and pack bottles from it the way they do from
any loose bottles, and caravans can load from it.

| What | Value | Where it comes from |
| --- | --- | --- |
| Bottles kept ready | 0 to 10, starts at 10 | One stack, the bottle's stack limit |
| Colony limit | 0 to 1000 a map, starts at 50, in steps of 10 | Authored |
| Fill rate | 1 bottle per 250 ticks, 240 a day at most | Authored |
| Water per bottle | 0.5 L | Dubs Bad Hygiene's own charge for packing a bottle, and half a kilogram of water, the bottle's mass |
| Most water used | 120 L a day per tap | From the two above |
| Cost and work | 20 steel, 600 work | Authored |
| Power | None | Pumps supply the pressure. Authored simplification |

**Fewer bottles** and **More bottles** change how many this tap keeps ready, one at a time. At 0 it stops filling. **Colony limit -10** and **+10**
change the limit for the whole map, whichever tap you press them on, and a click counts once however many taps are selected. The inspect pane always
shows the map's count against the limit, and says what the tap is doing or the one thing it's waiting for:

| It says | What to do |
| --- | --- |
| Filling bottles / Full | Nothing |
| This map has reached its colony limit | Nothing, or press **Colony limit +10** if you want more bottles |
| Switched off | Press **More bottles** |
| Another item is on the tap | Have it moved |
| No water tower on this pipe network | Connect it to your plumbing |
| The water towers are empty | Get more water into the towers |
| The water is contaminated | Clear the contaminated water tower. The tap won't bottle this water |
| The water in the pipes has changed | Nothing: it fills again once the bottles here are taken |

### Water quality

Each bottle holds the water that filled it: treated, or untreated. On a network with mixed towers, the bottle gets the worst of them. Dubs Bad
Hygiene checks this when someone drinks, the same way it checks a basin.

Dubs Bad Hygiene has no stack rules for this quality: a piece split off a stack came out treated, and merged stacks kept only one side's
quality. So a pawn taking two bottles from an untreated stack carried away clean water. This mod adds the rules, for all bottles:

- **A bottle split off a stack keeps its water.**
- **Bottles of different water don't stack.** They are separate bottles.
- **Labels say so**: "water (untreated)". Treated water just says "water".

### Conveyor belts

With **Vanilla Furniture Expanded - Factory**, belts and factory machines can take bottles from the tap, as they do from a factory hopper. With
**D'thorga Factory Additions** the tap also gets the **Belts may take from here** switch, starting on. A belt empties the tap and it refills
until the colony limit is reached, counting what the belt has delivered. Bottles still on the belt aren't counted, so the most that can sit above the
limit is what your belts can hold.

## For modders

The tap is an ordinary Def with two comps, so another building can do the same job in XML:

```xml
<li Class="DubsBadHygiene.CompProperties_Pipe">
  <mode>Sewage</mode>
</li>
<li Class="Phobos.WaterTap.CompProperties_BottleTap">
  <bottle>DBH_WaterBottle</bottle>
  <waterPerBottle>0.5</waterPerBottle>
  <bottlesPerFill>1</bottlesPerFill>
  <startingTarget>10</startingTarget>
</li>
```

The building must be a `Building_Storage` with `tickerType` Rare. Mark its Def `DBHThirst="true" DBHLite="true"`, so that Dubs Bad Hygiene drops it
whenever the bottle or the plumbing research doesn't exist. The record of how this was worked out is
[Water tap: research and decisions](../../docs/development/water-tap.md).

## Checks done

On 4 November 2026, against RimWorld 1.7.4998 and the owner's 326 active mods: it compiles against Dubs Bad Hygiene's 1.6 and 1.7 assemblies, and the
game members it calls were found in the installed 1.7 game. The tap's rules pass 29 unit tests. The patch checker found every xpath and Def name. **None of this is an in-game test.**
Not checked: anything on a running colony, including the texture in place, pawns using the tap, belts taking from it, and the colony limit.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
