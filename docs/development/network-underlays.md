# Pipes and conduits beneath our things (13 Nov 2026)

What our buildings and vehicles lay beneath themselves to join a network (plumbing, power, Vanilla Expanded Framework's pipe nets),
what changed in D'thorga Framework 0.107.0 to 0.108.1, and an audit of everything of ours that uses a piped supply or power, with proposals for
the owner to pick from. **Built in 0.107.0: the first two sections. The audit's proposals are not built.** Nothing here is tested in
game.

## Read this first

- **Hidden again, from Framework 0.108.1** (owner, 13 Nov 2026, after testing 0.108.0: "all the pipes are visible now! You need to
  use the Hidden versions of them, mate"). Everything of ours that joins a network lays the network's **hidden piece**: Dubs Bad
  Hygiene's hidden plumbing (`sewagePipeHidden`), the game's hidden conduit (`HiddenConduit`), and a Vanilla Expanded Framework net's
  transparent pipe. **The connections show in the game's plumbing and power overlays**, as every hidden piece does. 0.107.0 and
  0.108.0 laid the visible pieces (the owner's call of the morning, "that's the convention of the vanilla game and other mods", reversed
  once seen in play); those sections below keep their original scope.
- **Saves convert by themselves.** A visible piece we laid is swapped for the hidden one on the same cell at the thing's next look (a
  building on load, a parked vehicle when it stops, starts or is looked at again). Only pieces we laid are touched; the player's own
  pipes and conduits, visible or hidden, are never touched.
- **Our pieces give nothing back** when taken apart (0.108.1), so laying them again at every stop makes nothing from nothing.
- **The generator set can now join through conduits beneath it**, as a choice beside running its cable out.
- **A piece the player removes stays removed** while the thing stays where it is (agent's choice, below).

## The rule, as built (Framework 0.107.0)

| What | Lays | When | Taken up |
| --- | --- | --- | --- |
| Our plumbed buildings: the conveyor oven (Factory Additions, with Dubs Bad Hygiene), the safety shower and eyewash, the electrolyser (Medical) | Dubs' visible plumbing on every footprint cell without a pipe | When built, and on every load | When the building goes |
| Our vehicles that take water (coolant top-up, the Harrier's injection water) | Dubs' visible plumbing | While parked on or against a plumbing run | When it drives off |
| The bulk tanker, the drill rig's buffer | The visible pipe of each Vanilla Expanded Framework net its contents or outlets use | While parked on or against that net | When it drives off |
| **The generator set, set up "through conduits beneath it"** (new) | The game's own power conduit on every footprint cell without a transmitter, and its junction box (the cable end) under it | When set up that way | When it packs up |

Only the player's own vehicles lay pipes (`CompPhobosVehicleSystems.ParkedForPipes` checks the faction). It never touches a pipe or
conduit it did not lay, and never lays where one already is.

**When a laid piece goes** (agent's choice, for the owner to revise): if the player deconstructs one, or it is destroyed, that cell is
not laid again while the thing stays put. The player's choice stands (a pipe may have been lifted to split two runs), and nothing is laid
for free over and over (Dubs' plumbing returns part of its stuff when deconstructed). It is laid again when the thing is placed afresh: a
vehicle parked again, a generator set set up again. A building keeps the gap until it is rebuilt. The player can always build their own
pipe on that cell; ours then leaves it alone. Code: `src/PhobosFramework/Persistence/PipeUnderlay.cs` (`NoteGone`, the saved declined
cells).

### The generator set's two ways to join the grid

**Set up** now opens a menu of two choices, each with its picture:

1. **Run the cable out** (as before): the cable end goes on a cell you choose within the cable's reach (one cell without a cable reel).
2. **Join through conduits beneath it** (new): its junction box goes under the truck, on a free cell beside a conduit outside it if
   there is one, and the game's power conduit is laid on the rest of its footprint. Any conduit touching the truck, on any side, is then
   on its power, as with a building standing on a conduit run. Refused, with its reason, when every cell under the truck already carries
   a conduit or a building.

Either way the set is still immobile while set up, needs someone aboard with the engine running to set up (0.106.1), and the conduits
go when it packs up. Code: `CompPhobosGeneratorSet.SetUpBeneath`, `GeneratorGizmos.ChooseWay`.

- **Lets the player:** park the set against an existing conduit run and have the camp's grid simply join, with nothing placed beside it.
- **Stops them:** nothing new.
- **Changes how they play:** where the cable end used to need a free cell outside the truck, a set in a tight spot can join through the
  ground beneath it.

**Other factions:** their sets never set up (their AI has no use for it), so nothing applies. A captured set behaves as ours.
**Cost:** nothing per tick; the conduits are laid once, at set-up.

## Framework 0.108.0: the loading fault, and pieces beneath every parked vehicle (13 Nov 2026)

**The fault (found in the owner's save of 13 Nov 2026, 07:31).** After 0.107.0, the Big Rig still stood on 18 pieces of the old hidden
plumbing and the six conveyor ovens on 72. A save from before 0.107.0 kept no cells beside its pieces, and `PipeUnderlay.ExposeData`
rebuilt them while loading by keeping only the pieces that were on the map. Nothing is on the map while a game loads (things are placed
afterwards), so every piece was dropped: the old hidden pipes stood orphaned, owned by nothing, and `Renew` had nothing to swap. A vehicle
driving off would have left them behind. Code: `src/PhobosFramework/Persistence/PipeUnderlay.cs`.

**The fix.** The cells of such a save are rebuilt at the first look, once things stand. **The recovery:** an underlay saved before 0.108.0
(the new key `PhobosPipeUnderlayAdopted` is missing) looks once, after the first load, for pieces of the old hidden kind (`sewagePipeHidden`;
a Vanilla Expanded Framework net's pipe whose colour has no alpha) standing exactly on its thing's own footprint where it holds nothing,
takes them back as its own, and `Renew` swaps them for visible ones (`PipeUnderlay.AdoptOnce`, `UnderlayRule.Adopts`). **It assumes a hidden
pipe exactly under one of our things was laid by our earlier versions** (agent's assumption, recorded): a player who built hidden pipe on
those very cells keeps it, as visible pipe on the same cells, and it then goes with the vehicle. New underlays never adopt.

**The new rule (owner, 13 Nov 2026).** "I think plumbing and power conduits should only appear when the vehicle is powered off? This will
be performant that way", then "The plumbing and power conduits should appear though regardless, so players are not confused", with the
owner's picks: always when parked with the engine off; every vehicle with a battery lays conduits; the generator set keeps its conduits while
set up. As built (`UnderlayRule.Lays`, `CompPhobosVehicleSystems.ParkedForPipes`):

| Network | Laid beneath a vehicle when | Taken up |
| --- | --- | --- |
| Dubs' plumbing | It takes water (a coolant top-up or injection water, or a tank of water or sewage) and stands with its engine off | Engine starts, or it moves |
| The game's power conduit (`VehicleConduits`, new) | It has a battery (our 12/24 V electrics or a traction battery) and stands with its engine off | Engine starts, or it moves |
| A Vanilla Expanded Framework line | It carries, pumps or burns that line's fluid, and stands with its engine off, or is set up for work | Engine starts (unless set up), or it moves |
| The generator set's own conduits | It is set up "through conduits beneath it", running or not | It packs up |

- **Triggers:** Vehicle Framework's MoveStart (taken up), MoveStop, IgnitionOn and IgnitionOff (looked at again), and after a load; the
  rare-tick look every eight rare ticks stays as a backstop. Laid once per stop; nothing each tick.
- **Nothing on water:** plumbing, conduit and line pipe are not laid on water cells (a boat at its mooring shows none there).
- **Shore charging:** `ShorePowered` already counts any thing on a powered net within the vehicle's reach, which now includes the conduits
  beneath it, so a conduit run touching a parked vehicle charges it. One yes-or-no answer per update: nothing is counted twice.
- **The generator set:** while set up, the battery conduits stand down (`VehicleConduits` skips a deployed set), so its own conduits and
  junction box are never doubled; packed up and parked with its engine off, it lays them like any vehicle.
- **A fuel line** fills the tank only where a run that is not ours touches the vehicle (`TouchesRun`, read before laying).
- **Other factions:** never; their vehicles lay nothing. A captured vehicle follows ours.

**Lets, stops, changes.** Lets the player see at a glance where every vehicle can be joined, and charge a parked vehicle from a conduit run.
Stops nothing. Changes how they play: a motor pool's vehicles show their connections whenever parked, and a run laid to their sides joins them.

## Framework 0.108.1: hidden again, and no refunds (13 Nov 2026)

**The owner's call, after testing 0.108.0:** "all the pipes are visible now! You need to use the Hidden versions of them, mate." The rule of
0.108.0 stands (laid whenever a vehicle stands with its engine off, taken up when it starts or moves, a removed piece not laid again while it
stays, the generator set keeping its own conduits while set up); only the piece changes.

| Network | 0.107.0 and 0.108.0 laid | 0.108.1 lays | Where the player sees it |
| --- | --- | --- | --- |
| Dubs Bad Hygiene's plumbing | `sewagePipeStuff` (visible) | `sewagePipeHidden` | The plumbing overlay |
| The game's power | `PowerConduit` (visible) | `HiddenConduit` (the game's own; `PowerConduit` only if a mod removes it) | The power overlay |
| A Vanilla Expanded Framework net | Its first visible pipe | Its pipe whose colour has no alpha (the net's transparent pipe), else its first | That net's overlay |

- **How saves convert:** `PipeUnderlay.Renew` swaps each piece of ours that is not of the wanted kind for the wanted one on the same cell.
  It now runs with the hidden kind, so the visible pieces 0.107.0 and 0.108.0 laid become hidden at the next look: our plumbed buildings on
  load, a vehicle's plumbing, conduits and lines when it stops, starts, loads or has its rare-tick look, and the generator set's own
  conduits at its next power update. Only pieces in an underlay's own saved list are swapped; the one-time adoption of 0.108.0 still takes
  back the old hidden pieces orphaned under our things, which are now already the wanted kind.
- **No refunds (a loop closed):** since 0.108.0 a vehicle lays its pieces again at every stop, and Dubs' pipe gives back half its stuff when
  deconstructed (the game's rule, `GenLeaving.GetBuildingResourcesLeaveCalculator`: cost times `resourcesFractionWhenDeconstructed`), so
  deconstructing them stop after stop would make steel from nothing. A piece any underlay laid now gives back nothing: a Harmony prefix on
  `GenLeaving.DoLeavingsFor` turns its destroy mode from Deconstruct to Vanish (`UnderlayPiecesRefundNothing`, never skipping the original;
  every other thing, the player's own pipes included, is untouched). Which pieces are ours is held in memory beside each underlay's list
  (`PipeUnderlay.LaidByUs`) and rebuilt as it loads; nothing new is saved. The game's hidden conduit already refunds nothing.
- **Lets, stops, changes:** lets the player keep the look of a motor pool or a plumbed room clean, with every connection still shown in the
  overlays; stops nothing; changes how they check a connection (the overlay, as for any hidden pipe, instead of the floor).
- **Other factions:** unchanged; their vehicles lay nothing. **Cost:** unchanged; the swap runs once per piece, then does nothing.

## The audit: everything of ours that uses a piped supply or power

Read on 13 Nov 2026 from our mods' Defs and patches (a scan of every `ThingDef` and `VehicleDef` of ours for power, fuel, Vanilla
Expanded Framework pipe, Dubs Bad Hygiene and Rimefeller comps), and from the vehicle systems' code.

| # | Thing (mod) | Uses | How it joins today | Lays beneath today | Proposal |
| --- | --- | --- | --- | --- | --- |
| N1 | Conveyor oven (Factory Additions, with Dubs Bad Hygiene) | Water | Plumbing beneath it | Yes, visible from 0.107.0 | Keep |
| N2 | Safety shower and eyewash (Medical, with Dubs Bad Hygiene) | Water | Plumbing beneath it | Yes, visible | Keep |
| N3 | Electrolyser (Medical) | Water, power, oxygen | Plumbing beneath it; power wired; oxygen through Vanilla Expanded Framework's own comps with Vanilla Gravship Expanded | Plumbing only | Keep: power is wired, as the game's buildings are; the oxygen net is the framework's own connection |
| N4 | **Bottle tap (Water Tap)** | Water | Dubs Bad Hygiene's own fixture comp | **No** | **Not built, and not needed** (13 Nov 2026): the tap carries Dubs Bad Hygiene's own pipe comp on its cell, so it joins the plumbing as Dubs' sinks and toilets do: a pipe touching it connects. A pipe beneath it would make two pipe comps in one cell, which Dubs refuses with an error (DubsBadHygiene HygienePipeMapComp, "More than 1 pipe comp in the same cell"), and our underlay would find the cell piped and lay nothing |
| N5 | Liquid oxygen store, oxygen concentrator, nebuliser, hyperbaric chamber, oxygen trolley, bedside stand (Medical) | Power, oxygen | Wired; oxygen as cylinders or the framework's own comps | No | Keep: wired power is the game's convention for a building, and the oxygen net is the framework's own |
| N6 | Anaesthesia machine, burn unit bed, ventilator, gas alarm (Medical) | Power | Wired | No | Keep |
| N7 | Depot bay, vehicle parts bench, dyno bay, power point (Framework) | Power; the bay also holds fuel | Wired; the bay's tank is filled by hand | No | Keep power wired. **The bay already fills from a chemfuel pipe** (found 13 Nov 2026): Vanilla Expanded Framework adds its own pipe refill (CompRefillWithPipes) at start-up to every building whose refuelable tank takes a fuel some net links to (PipeSystem AutoLinkRefuelables), and the bay's tank takes chemfuel, which Vanilla Chemfuel Expanded's line links at 1 unit to 1 chemfuel. A pipe touching the bay joins it, as with any Vanilla Expanded building; nothing of ours is built. Not yet tested in game |
| N8 | Our vehicles that take water | Water | Plumbing beneath them while parked against a run | Yes, visible | Keep |
| N9 | Bulk tanker, drill rig | Each net its contents or outlets use; Dubs plumbing and septic | Pipes beneath while parked against a run | Yes, visible | Keep |
| N10 | **Every fuelled vehicle of ours** | Chemfuel | Refuelled by hand, a tanker or a depot bay | No | **Built: Framework 0.107.0, not yet tested in game.** A parked fuelled vehicle of the player's joins any net whose linkToRefuelables names its own fuel (Vanilla Chemfuel Expanded's chemfuel line), lays that line's visible pipe beneath it, and with its engine off and not set up fills from the net's storage at 30 an hour (authored, as the depot bay's pump; at most an hour a look), converting the net's units by its ratio (PipeRefuelRule, with tests). Other factions' vehicles do not: only the player's join pipes, as for every link |
| N11 | Electric vehicles | Power | Charge parked beside a powered building or over a conduit | No | Keep: they read the grid as it stands; nothing to lay |
| N12 | Generator set | Power | The cable end, or conduits beneath it (0.107.0) | Yes, when set up that way | Built |
| N13 | Radio framework's coax, mast, whip, base station | Its own feedline net | Its own pipes | No | Not reviewed: the radio framework is on the back burner (owner, 9 Nov 2026) |
| N14 | Vanilla Chemfuel Expanded's chemfuel tap, a corpse fridge (Factory Additions patches) | Pipes | Their authors' own comps | No | Leave: other authors' buildings |

### The proposals, each answering for itself

| # | Lets the player | Stops them | Changes how they play | Cost |
| --- | --- | --- | --- | --- |
| N4 Bottle tap lays plumbing | Place the tap on or against a run and see the pipe join, as with the shower | Nothing | One less pipe to lay by hand; the tap reads like the rest of our fixtures | XML: add the comp in Water Tap's Dubs folder; a Water Tap fix release |
| N7 Depot bay draws chemfuel from a pipe | Keep a bay's tank full from the colony's chemfuel line, with no hauling | Filling it from nothing: the pipe's net must hold chemfuel | The depot bay joins the fuel line; hauling chemfuel to it stops being a job | Small C#: the bay's fill step reads the net, in the Vanilla Chemfuel Expanded tie-in |
| N10 Vehicles refuel from a chemfuel pipe | Park a truck against the chemfuel line and it refuels | Free fuel: the net's tanks pay for it | A motor pool plumbed with chemfuel needs no haulers or tanker | Small C#: a refuel step in the existing pipe link (`PipeNetLink`), which already lays the pipe |

**Argued against:** power conduits beneath our powered buildings. The game and other mods wire buildings to conduits within reach or
under them; laying conduits beneath a bench would be unusual, not conventional. The generator set is different: it is a vehicle that
joins and leaves the grid.

## Questions for the owner

**Answered by the owner, 13 Nov 2026:** (1) the bottle tap **lays plumbing** (N4); (2) parked fuelled vehicles **refuel from a chemfuel
pipe** (N10); (3) the depot bay **fills from one** (N7); (4) a deconstructed piece **stays removed** until the thing is placed afresh,
as built. Nothing of N4, N7 or N10 is built yet.

The questions as they were put:

1. **N4:** the bottle tap lays plumbing beneath it, as the shower does? (Agent lean: yes.)
2. **N10:** our vehicles refuel from a Vanilla Chemfuel Expanded chemfuel pipe while parked against it? (Agent lean: yes; it is where the
   pipe links already lead.)
3. **N7:** the depot bay fills its tank from a chemfuel pipe touching it? (Agent lean: yes, with N10.)
4. **The removal rule:** a piece the player removes stays removed until the thing is placed again (agent's choice), or is it laid again
   at the next look? (Agent lean: as built.)

## Who chose what

- **Owner (13 Nov 2026):** visible pieces, as the game and other mods show them; the generator set's conduits beneath it as an option
  beside the cable; an audit and proposals. Later the same day: pieces beneath every vehicle parked with its engine off, always; conduits for
  every vehicle with a battery; the generator set keeps its conduits while set up.
- **Agent choices in 0.108.0:** old hidden pieces exactly under our things are taken as ours, once; nothing is laid on water; a set-up
  vehicle keeps its working pipes; the battery conduits stand down while the generator set is set up; a fuel line fills only from a run that
  is not ours.
- **Agent choices, owner to revise:** Dubs' `sewagePipeStuff` and each net's first visible pipe; a removed piece stays removed until the
  thing is placed again; the junction box's cell (beside an outside conduit, else under the middle); every proposal above.

## Sources

- Dubs Bad Hygiene's pipe Defs (`BuildingsA_Pipes.xml`: `sewagePipeStuff`, `sewagePipeHidden`) and the game's `PowerConduit` Def,
  read with `scripts/query-defs.py`. Vanilla Expanded Framework's pipe nets as our `PipeNetLink` reads them.
- Our own code: `src/PhobosFramework/Persistence/PipeUnderlay.cs`, `src/PhobosFramework.Hygiene/PipeUnderlays.cs`,
  `src/PhobosFramework.Vehicles.Hygiene/PlumbingWater.cs`, `src/PhobosFramework.Vehicles.Pipes/PipeNetLink.cs`,
  `src/PhobosFramework.Vehicles/Systems/CompPhobosGeneratorSet.cs`.
