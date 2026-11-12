# Pipes and conduits beneath our things (14 Nov 2026)

What our buildings and vehicles lay beneath themselves to join a network (plumbing, power, Vanilla Expanded Framework's pipe nets),
what changed in D'thorga Framework 0.107.0, and an audit of everything of ours that uses a piped supply or power, with proposals for
the owner to pick from. **Built in 0.107.0: the first two sections. The audit's proposals are not built.** Nothing here is tested in
game.

## Read this first

- **Visible, as the game does it** (owner, 14 Nov 2026: "that's the convention of the vanilla game and other mods"). Everything of ours
  that joins a network now lays the network's **ordinary, visible piece**: Dubs Bad Hygiene's plumbing (`sewagePipeStuff`), the visible
  pipe of a Vanilla Expanded Framework net, the game's own power conduit. Until 0.106.1 we laid Dubs' hidden plumbing, which shows only
  in the plumbing overlay, so the owner never saw the feature work.
- **Saves convert by themselves.** A hidden piece we laid earlier is swapped for the visible one on the same cell at the thing's next
  look (a building on load, a parked vehicle at its next rare tick). Only pieces we laid are touched.
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

## The audit: everything of ours that uses a piped supply or power

Read on 14 Nov 2026 from our mods' Defs and patches (a scan of every `ThingDef` and `VehicleDef` of ours for power, fuel, Vanilla
Expanded Framework pipe, Dubs Bad Hygiene and Rimefeller comps), and from the vehicle systems' code.

| # | Thing (mod) | Uses | How it joins today | Lays beneath today | Proposal |
| --- | --- | --- | --- | --- | --- |
| N1 | Conveyor oven (Factory Additions, with Dubs Bad Hygiene) | Water | Plumbing beneath it | Yes, visible from 0.107.0 | Keep |
| N2 | Safety shower and eyewash (Medical, with Dubs Bad Hygiene) | Water | Plumbing beneath it | Yes, visible | Keep |
| N3 | Electrolyser (Medical) | Water, power, oxygen | Plumbing beneath it; power wired; oxygen through Vanilla Expanded Framework's own comps with Vanilla Gravship Expanded | Plumbing only | Keep: power is wired, as the game's buildings are; the oxygen net is the framework's own connection |
| N4 | **Bottle tap (Water Tap)** | Water | Dubs Bad Hygiene's own fixture comp | **No** | **Lay plumbing beneath it**, as the shower does |
| N5 | Liquid oxygen store, oxygen concentrator, nebuliser, hyperbaric chamber, oxygen trolley, bedside stand (Medical) | Power, oxygen | Wired; oxygen as cylinders or the framework's own comps | No | Keep: wired power is the game's convention for a building, and the oxygen net is the framework's own |
| N6 | Anaesthesia machine, burn unit bed, ventilator, gas alarm (Medical) | Power | Wired | No | Keep |
| N7 | Depot bay, vehicle parts bench, dyno bay, power point (Framework) | Power; the bay also holds fuel | Wired; the bay's tank is filled by hand | No | Keep power wired. **Idea:** the depot bay draws fuel from a Vanilla Chemfuel Expanded chemfuel pipe touching it (N10) |
| N8 | Our vehicles that take water | Water | Plumbing beneath them while parked against a run | Yes, visible | Keep |
| N9 | Bulk tanker, drill rig | Each net its contents or outlets use; Dubs plumbing and septic | Pipes beneath while parked against a run | Yes, visible | Keep |
| N10 | **Every fuelled vehicle of ours** | Chemfuel | Refuelled by hand, a tanker or a depot bay | No | **Refuel from a chemfuel pipe while parked** against a Vanilla Chemfuel Expanded run, laying that pipe beneath it, as the tanker already does |
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

1. **N4:** the bottle tap lays plumbing beneath it, as the shower does? (Agent lean: yes.)
2. **N10:** our vehicles refuel from a Vanilla Chemfuel Expanded chemfuel pipe while parked against it? (Agent lean: yes; it is where the
   pipe links already lead.)
3. **N7:** the depot bay fills its tank from a chemfuel pipe touching it? (Agent lean: yes, with N10.)
4. **The removal rule:** a piece the player removes stays removed until the thing is placed again (agent's choice), or is it laid again
   at the next look? (Agent lean: as built.)

## Who chose what

- **Owner (14 Nov 2026):** visible pieces, as the game and other mods show them; the generator set's conduits beneath it as an option
  beside the cable; an audit and proposals.
- **Agent choices, owner to revise:** Dubs' `sewagePipeStuff` and each net's first visible pipe; a removed piece stays removed until the
  thing is placed again; the junction box's cell (beside an outside conduit, else under the middle); every proposal above.

## Sources

- Dubs Bad Hygiene's pipe Defs (`BuildingsA_Pipes.xml`: `sewagePipeStuff`, `sewagePipeHidden`) and the game's `PowerConduit` Def,
  read with `scripts/query-defs.py`. Vanilla Expanded Framework's pipe nets as our `PipeNetLink` reads them.
- Our own code: `src/PhobosFramework/Persistence/PipeUnderlay.cs`, `src/PhobosFramework.Hygiene/PipeUnderlays.cs`,
  `src/PhobosFramework.Vehicles.Hygiene/PlumbingWater.cs`, `src/PhobosFramework.Vehicles.Pipes/PipeNetLink.cs`,
  `src/PhobosFramework.Vehicles/Systems/CompPhobosGeneratorSet.cs`.
