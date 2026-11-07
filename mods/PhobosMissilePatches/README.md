# D'thorga Missile Patches

Fixes for **InterRim Ballistic Missile** by Psionic Zephyr, switched on only while it is active. In this version: a strategic
warhead no longer hangs the game when it lands. It copies nothing from the missile mod.

Version 0.1.0, built 7 November 2026 for House D'thorga. **Not yet tested in game.**

## Read this first (gotchas)

- **Needs D'thorga Framework and Harmony. Load it after InterRim Ballistic Missile.** The About file asks for this, so RimSort
  does it for you. **You have to switch it on yourself**: nothing else requires it. It does nothing without the missile mod.
- **RimWorld 1.7 and 1.6.** Its small assembly is built for each. The missile mod itself ships only a 1.6 build, which the
  game also runs on 1.7.
- **Safe to add to a running save.** It saves nothing of its own.
- **Safe to remove.** It changes no saved thing. Remove it while no strategic blast is in progress: a blast that is still
  crossing a map when you save is forgotten on load, whether or not the mod is still active (see the limits below).
- **A strategic blast now takes a few seconds instead of one frozen frame.** The screen goes white as before; the blast crosses the
  struck map from the centre outward while the game keeps drawing and responding. If it runs longer than a couple of seconds you
  get a message saying the blast is still tearing through the place, and another when it has spent itself.
- **Settings:** how much of each frame the blast may use (default 12 ms; higher is quicker and more stuttery), and a switch that
  turns the whole fix off, which gives you the missile mod's own one-frame behaviour.
- **Not yet tested in game.** Everything here is read from the owner's log and the mods' code. The owner's own report of the
  hang is what this fixes.

## What it fixes

**The hang.** When a strategic warhead (the extinction-class nuclear warhead, for instance) lands, the missile mod fades the
screen to white and then, in a single frame, deals its damage to every pawn, building, item and plant on every map within the
warhead's reach. On the owner's heavily modded game that frame ran for more than two minutes on an enemy town map, until Windows
reported RimWorld as not responding and closed it. The game's log shows the loop still running when it died.

**Why one frame takes minutes.** Two things compound:

1. The loop is one frame's work however large the map is: tens of thousands of things, each damaged, killed and replaced by
   rubble and leavings, with every mod's damage and destruction hooks running for each.
2. After each destroyed building, placing the next one's leavings asks the game which room a cell is in. That question makes the
   game rebuild every region and room the earlier destructions had dirtied, on the spot. So each wall that falls makes the next
   one slower, and the cost grows with the size of the base and with the number of mods that react to room changes.

Read in the game's code (`GenPlace`, `RegionGrid`, `RegionAndRoomUpdater`) and the missile mod's (`StrategicImpactCountdown`);
see the [design record](../../docs/development/missile-patches.md) for the details.

**What this mod does instead.**

- The blast is queued and crossed **a slice per frame**, within a time budget (12 ms by default, a setting). The game keeps
  drawing, the white screen fades as it always did, and the blast reads as a wave spreading from the map's centre.
- **During each slice the game's room rebuild is held**, so rooms and regions are rebuilt once per frame instead of once per
  fallen wall. This is most of the saving.
- **The outcome is the missile mod's own.** The same damage, the same armour penetration, the same thick-rock-roof shelter, the
  same hit to a pawn's core, the same dismantle paths for warheads that use them, the same repeated passes for warheads that
  strike several times, and the missile's name in the health and battle logs. Only the pacing and the order change.
- Things that are already gone by the time the wave reaches them (a wall that collapsed with its roof, say) are skipped quietly.
  The missile mod logged a red error for each of those.

## What it does not do

- It does not speed up the fires the warhead starts, the terrain changes of dismantling warheads, or the mushroom cloud. Those
  stay as the missile mod does them, in the frame the screen reaches white.
- It does not change what a warhead costs, reaches or destroys.

## Settings

| Setting | Default | What it does |
| --- | --- | --- |
| Spread a strategic blast over several moments | On | Off gives you the missile mod's own one-frame blast. |
| Time each moment may spend on the blast | 12 ms | 2 to 50 ms. Higher finishes sooner and stutters more. |
| Rebuild rooms once per moment during a blast | On | Off if a blast ever leaves rooms behaving oddly; please report it. |

## Limits

- **A blast in progress is not saved.** If you save while the wave is still crossing a map and load that save, what the wave had
  not yet reached survives. Wait for the message that the blast has spent itself, or a few seconds, before saving.
- **A warhead that strikes several times** queues its later passes one game second apart, as the missile mod does; if a pass
  takes longer than that, the passes line up and run one after another.
- The order of destruction is from the centre out; the missile mod's was the order things had appeared on the map. Nothing in
  play depends on it.

## For testing (dev mode)

Under the **D'thorga** debug category: **Missile Patches: strike current map** runs an extinction-class blast on the map you are
looking at, through the missile mod's own code path, so you can watch the fix without building a missile. **It destroys
everything on that map**; use a throwaway save. **Log blast queue** and **drop queued blasts** are there for diagnosis. Each
finished blast logs one line: how many things it hit, in how many seconds and frames, and any errors it swallowed.

## Sources

Owner's `Player.log` of 7 November 2026 and the Windows Application log's hang report; the missile mod's 1.6 assembly
(Workshop item 3682304832, shipped with its symbols); RimWorld 1.7's assembly. The missile mod's Workshop description could not be
read from here (region-blocked), so its licence is not recorded; this mod ships none of its files.
