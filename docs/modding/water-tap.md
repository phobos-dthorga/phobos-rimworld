# D'thorga Water Tap for modders

How to make another building do the tap's job, in XML. Players start at
[the mod's README](../../mods/PhobosWaterTap/README.md) and [the player guide](../player-guide/water-tap.md). **Not yet tested in
game.**

## Your own tap

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

- The building must be a `Building_Storage` with `tickerType` Rare.
- **Mark its Def `DBHThirst="true" DBHLite="true"`**, so that Dubs Bad Hygiene drops it whenever the bottle or the plumbing research
  doesn't exist.
- `Sewage` is Dubs Bad Hygiene's mode for both water and waste: the fixture replaces a pipe under it and carries the water itself.

The record of how this was worked out, with the code readings behind each figure, is
[Water tap: research and decisions](../development/water-tap.md).

## Checks done

On 4 November 2026, against RimWorld 1.7.4998 and the owner's 326 active mods: it compiled against Dubs Bad Hygiene's 1.6 and 1.7
assemblies (1.6 was retired on 8 November 2026; it is 1.7 only since), and the game members it calls were found in the installed 1.7
game. The tap's rules pass 29 unit tests, and the patch checker found every xpath and Def name. **None of this is an in-game test.**
Not checked: anything on a running colony, including the texture in place, pawns using the tap, belts taking from it, and the colony
limit.
