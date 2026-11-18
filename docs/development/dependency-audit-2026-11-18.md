# Dependency audit: Vehicle Framework 1.7.2537 and RimWorld 1.7.5013 (18 Nov 2026)

**Answer first.** Nothing of ours breaks. Since the 17 Nov mod-list refresh only two mods changed on disk, **Vehicle Framework**
(1.7.2534 to 1.7.2537, 18 Nov) and **Real Ruins** (17 Nov), and the game moved from 1.7.5011 rev24 to **1.7.5013 rev95**. The full
gate passed against the new files (49 of 49 steps: every project rebuilt, and `check-targets` found every method and type we patch
or look up by name). Vehicle Framework's update is small, fixes three things we had noticed or worked around, and leaves one gap
in our refusal messages, written up below as a proposal.

## Read this first (gotchas)

- **Vehicle Framework now enforces the research named on upgrade nodes, on every tree.** Until now it declared the field and never
  read it, which is why D'thorga Framework has `PhobosEnforceUpgradeResearch`. All eleven of our trees with research carry that
  marker, so **our vehicles behave exactly as before**. On other authors' trees, only the **Harrier** names research on upgrades
  (two nodes: one needs IEDs, one Microelectronics Basics); those now lock until researched. The GTek trucks' research is on their
  build costs, not upgrades, and the Vanilla Vehicles Expanded trees name none.
- **No new install is needed for our mods.** The installed builds load against the new Vehicle Framework: the gate's target check
  read every name we use, and its build compiled against them. The game update could not be diffed (no older copy of its assembly
  survives on disk, and Ludeon's notes for 5013 were not found); the target check is the evidence that our patches still land.
- Everything below is read from Vehicle Framework's decompiled code, not seen in play. **Not yet tested in game.**

## How it was checked

- `python scripts/refresh-mod-list.py`, then each mod's update date compared with the committed list (load numbers shifted by
  one, so the raw diff is noise).
- Vehicle Framework's six 1.7 assemblies decompiled with ilspycmd into `.local/decompiled/VehicleFramework-1.7-2026-11-18`, the
  same way as the 16 Nov tree, then `python scripts/query-code.py compare` between the two: 15 new method bodies, one new
  signature, one assembly removed, one added.
- Our own source searched for every changed member; other authors' upgrade trees read as written (the patched-Defs file dates
  from 8 Nov, older than our newest trees).
- `python scripts/check-all.py`: passed.

## What changed in Vehicle Framework, and what it means for us

| Change (read from the code) | Effect on us |
| --- | --- |
| **Upgrade research is enforced** (`CompUpgradeTree.PrerequisitesMet` now checks each node's research as finished) | Our trees: none (already enforced by our marker). Harrier: two nodes newly locked. Other factions' vehicles: Vehicle Framework's random upgrades ask the same method, so they skip nodes the player has not researched; ours already did this on our trees. Our `UpgradeNeedsResearchPatch` is now a harmless duplicate. See the proposal below |
| **Upgrade materials counted by items, not stacks** (`UpgradeNode.MatchedItemCount`, used by `AvailableSpace`) | Before, a vehicle mid-upgrade holding one stack of 75 steel counted "1" and kept asking for more, so haulers could carry far more than the upgrade needed into it. Fixed upstream. Our code never reads either |
| **Loading cargo: the right pawn is checked** (`WorkGiver_PackVehicle.GetThingsToLoad` tested the hauler's spawn state instead of the passenger's; pawns that always ride as cargo are no longer dropped for want of a seat) | Concerns pawns in a loading list, not goods, so probably **not** the cause of the HEMTT's unloaded steel (`hemtt-cargo-loading-open`); the dev action stays the way to find out |
| **No seat dialog for a vehicle with no seats** (`TransferableVehicleWidget.DrawCard`) | Helps our trailers when a caravan is formed; the "lone trailer as crewless caravan" check from 16 Nov now meets less in its way |
| **"Full tank" means nothing left to add** (`CompFueledTravel.FullTank`), and the Refuel menu option is hidden on a full vehicle | We never read `FullTank`; our bowsers and top-up machines decide fullness themselves |
| **Turret targeting draws its minimum range** in red (was drawing the maximum twice) | Display only |
| **Vehicle icons drawn by Vehicle Framework** (bool prefixes, priority 800, on `Widgets.ThingIcon` twice and `Widgets.DefIcon`) | We call none of these for vehicles and patch none of them |
| Paint properties set only when the shader takes a paint mask (`VehiclePawn.ResetMaterialProperties`) | Our bodies use paint-mask shaders; no change expected |
| `SmashTools.Rendering.dll` removed; `UpdateLogTool.dll` added (an in-game "what's new" window); build-gizmo material prefix removed; dev spawner name fix; path request disposal tidied | We reference the folder, not that assembly; the gate built without it |
| **About file no longer lists Vanilla Outposts Expanded as incompatible** | RimSort stops warning about the pair, which our garage outpost tie-in uses. The 16 Nov audit's note on it is superseded |

## Real Ruins and the game

- **Real Ruins (17 Nov):** our only use is a general rule in D'thorga Patches' Harrier part that keeps any mod's map open while the
  jet flies over it with its pilot; it names no Real Ruins code. Nothing to check.
- **RimWorld 1.7.5013:** not diffed (see gotchas). Every Harmony target and by-name lookup still resolves.
- Other updates between 15 and 17 Nov (Harmony, Search and Rescue, RimPacts, Prison Labor, Vanilla Expanded Framework and others)
  were handled on 16 and 17 Nov.

## Proposal (agent's, for the owner)

**Let the research refusal speak on every tree.** Vehicle Framework now locks a research-gated node on any tree, but its own
button then shows a fixed line about another upgrade, which is wrong for research. Our refusal button says "needs <research>",
but only on trees with our marker. Dropping the marker check from `UpgradeResearchGate.Missing` would give the Harrier (and any
future tree from any author) the right reason, and would make our duplicate postfix removable. The marker class would stay, so
our XML still loads. It changes only what a refusal says, never what is allowed. Not built.
