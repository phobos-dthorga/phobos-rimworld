# The tipper hauler: design (9 Nov 2026)

**Built in D'thorga Framework 0.49.0 and D'thorga Vehicles 0.21.0. It borrows the bulk tanker's picture until it has its own (owner, 9 Nov 2026), so no raised-body or body-upgrade look shows yet. Not yet tested in game.** The owner chose to build it on 9 Nov 2026 without the Rails and Roads stone test, which still decides only whether it can serve as a road builder's wagon. What was built differs from what follows: there is no `WorkLights` node, because nothing yet lights a moving vehicle's surroundings and the node would move no figure; the load comes off a few things every rare tick rather than one at a time; and the tip picks its own cells, leaving out buildings entirely.

**The design as written, agent-authored for the owner to confirm;** every figure is authored unless a line
says "from the code" (with its file and line) or "secondary". It replaces the tipper sketch in [the utility vehicles design](utility-vehicles-design.md)
and follows the shape of that record's bulk tanker. `VF.cs` below is our decompile of Vehicle Framework's 1.6 build (`Vehicles.decompiled.cs`).

## Read this first

- **What it is:** a dump truck. A heavy body with a high `CargoCapacity`, loaded by Vehicle Framework's own cargo dialog and the game's hauling jobs,
  whose one new trick is a **tip job** that puts the whole load on the ground behind the truck in one go. Driver only. It never deploys.
- **Decided (1): the capacity (owner, 9 Nov 2026).** 2,400 kg for the base body, 4,200 kg at the top of the tree. The owner considered 3,250 kg
  and stayed at 2,400 once it was clear a full load of chunks must fit the tip area: 3,250 kg is 130 granite chunks, against about 100 cells. Vanilla Vehicles Expanded's Big Rig
  carries 2,200 and RimThunder's HEMTT 15,000 (patched Defs, read 9 Nov 2026), so a huge body is already on the owner's list.
- **Decide (2): the two bodies.** Rock body (tips fast, same capacity) against bulk body (carries more, tips slower), exclusive. Or one body only.
- **Decide (3): the spreader.** Rails and Roads of the Rim builds roads **on the world map, from a caravan**; nothing on a map receives stone. The
  tailgate spreader as sketched moves no figure that any code reads, so the agent cuts it from the first tree (below, "The spreader").
- **The risk:** the tip job is new code on the game's placement rules (`GenPlace`), and chunks need one cell each: a full load of chunks needs
  about a hundred clear cells. A big `CargoCapacity` also raises every caravan's capacity, so it is a balance figure, not just a truck figure.
- **Depends on:** the drill rig for something worth hauling (its cuttings and chunks), [the garage](vehicle-garage-design.md) to keep it between
  jobs, [the generator set](vehicle-generator-set.md) for the camp's power (the family rule: the tipper's only electrical loads are its lights).
- **Not here:** no Helixien gas hooks; deepchem and every other fluid ride in the bulk tanker, never in the tipper.

## What it is and what it does in play

The real machine behind it is a 6x4 rigid tipper: a three-axle truck with a steel body that two hydraulic rams lift from behind the cab, so the
load slides out over a tailgate. In play it is the truck that turns a hundred carries into one drive. A colonist loads it at the rig or the quarry
(the ordinary cargo dialog, or hauling jobs to its stock), the driver takes it home, parks it with its tail to the yard and presses **Tip**. The body
rises, the load lands in a fan of cells behind the truck, and the colonists haul from there as they would from any pile. Loading still costs the
carries at the far end; the drive and the tip replace the carries at this end, and a round trip replaces a day of hauling.

It is deliberately simple: no deployment, no outriggers, no crew but the driver, no power beyond its lights. Its upgrades change how much it carries
and how fast it sheds the load; its damage shows where it hurts (a holed body carries less, wrecked rams tip slowly, a shot engine stops road and tip
alike). What stops a tip is always the ground, and the truck says so.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **A high-capacity body** | Carry 2,400 kg: 96 granite chunks, 120 sandstone chunks, 4,800 steel or 1,920 granite blocks in one trip. A 60 kg capacity (the tanker's) carries two granite chunks | Nothing; the load still has to be put aboard by hand or by hauling jobs | Ore and rock move by the truckload; the quarry can be far from the base |
| **The tip job** | Empty the whole load in one short job where the truck stands | Tip where the ground behind is blocked, or selectively; it is all or nothing (what will not fit stays aboard, and the truck says so) | Unloading becomes a place to park, not a hundred jobs; the yard behind the tipping spot is planned for it |
| **A holed body** | Keep driving and tipping | Load as much: the body's part efficiency lowers `CargoCapacity` | Body damage costs trips, not the truck; it is repaired before the next big haul |
| **Rock body or bulk body** | Choose a steep body that sheds chunks fast, or a long body that carries half as much again | Have both: they are exclusive | The truck is shaped for its yard: a chunk hauler for the quarry or a bulk hauler for ore and blocks |
| **High sides** | Carry 600 kg more of anything | Nothing | More per trip against a longer tip and a heavier truck |
| **Tailgate spreader** (Rails and Roads) | Nothing yet: road legs are built on the world map by a caravan, with no site on a map to feed | Not applicable | Cut from the first tree; the tipper's real road role is the stone wagon of a road-building caravan (below) |
| **The laden truck is slower** | Drive full | Drive full as fast as empty: the drivetrain model moves the real mass, kerb plus cargo | A full tipper on a long road is a slow convoy; empty it comes home quickly |
| **Hauling for the rig** | Shuttle cuttings and chunks from a mobile rig to the base | Mine faster: the rig drills at the game's own rates, the truck only moves the result | Rig, tipper and generator set become one camp, and the drive home is the loop |
| **In a caravan** | Add its `CargoCapacity` to the caravan's | Tip on the world map; it tips only on a map | A trading convoy carries a truckload more; at a destination the load comes off in one job |

## The honest mechanics

**What the game and Vehicle Framework do (from the code).** A vehicle's cargo is its ordinary pawn inventory, and Vehicle Framework patches
`MassUtility.Capacity` so a vehicle's capacity is its `CargoCapacity` stat (VF.cs:43923, 44031-44044, 14660); loading and launch compare
`InventoryMass` against it (VF.cs:1471, 18536, 20888, 39592); `Dialog_LoadCargo` is a transfer dialog whose mass used is transferables plus
current gear and inventory, against `CargoCapacity` (VF.cs:36524-36568); a `VehicleCaravan` is a `Caravan` and its capacity is the sum of its
vehicles' `CargoCapacity` (VF.cs:87015, 80948). Mass is item by item, stack count times the item's Mass stat (`all.cs:708450-708458`). Chunks are
one item each and stack to one (`ChunkBase`, `stackLimit` 1); granite 25 kg, marble 25, sandstone 20, limestone 22, slate 18, steel slag 8; steel is
0.5 kg a unit and stacks to 75; a granite block is 1.25 kg (query-defs, 9 Nov 2026). A cell takes one item unless a building on it allows more
(`GridsUtility.GetMaxItemsAllowedInCell`, `GridsUtility.cs:267-270`). **No tip, dump, volume or loose-cargo concept exists** in Vehicle Framework or
the outposts code (grep over both decompiles, 9 Nov 2026). Upgrade nodes set or add vehicle stats (`StatUpgrade`, VF.cs:33992-34218), activate comps
(`CompUpgrade`, VF.cs:33709-33785), and carry `work`, `ingredients`, `researchPrerequisites`, `prerequisiteNodes`, `disableIfUpgradeNodesEnabled`,
`graphicOverlays` and a `refundFraction` of 0.5 (VF.cs:34815-34866).

**What we must add.** One job and one comp, in D'thorga Framework's Vehicle Framework part so any truck can tip.

- **The tip job.** A gizmo on the vehicle, driver aboard, on a map. It moves things from `inventory.innerContainer` to the ground at `Phobos_TipRate`
  things a second (a thing is one stack placed), each through `GenPlace.TryPlaceThing` in `Near` mode centred on the cell behind the tailgate, with an
  `extraValidator` allowing only the half-disc behind the truck, radius 8, in its facing. The game picks the cell by its own rules: a matching stack
  first; never a cell at its item limit, a door, a full-fill building or a work table; the same room preferred (`GenPlace.PlaceSpotQualityAt`,
  `GenPlace.cs:214-311`; the search reaches 12.9 cells, `GenPlace.cs:19`). The body is raised while it runs: a `graphicOverlay` our comp draws.
- **The refusal.** `Near` mode logs an error when nothing fits (`GenPlace.cs:72-89`), so the job never lets it fail: before each placement the comp
  counts a free cell by the same rules, and when none is left it stops and says so, in the world's words: **"The ground behind it is full. 14 chunks
  stay aboard."** A job that cannot start says the same before the body lifts. Below the `Phobos_RigControl` floor: **"The body will not lift: the
  tip controls are wrecked."** No silent success: the finished job reports what it put down.
- **Not a stockpile rule.** Placement checks a building's filter (`IHaulDestination`, `GenPlace.cs:246`), not a zone's: items tipped into a zone that
  does not allow them lie there until hauled away. Honest for a dump truck.

**The spreader and Rails and Roads.** Read 9 Nov 2026 from the Workshop item (3271115410, `mlie.railsandroadsoftherim`): its XML, README and English
strings. **Its assembly is not decompiled** (nothing under `.local/decompiled` names it), so how it matches a resource and draws it from a caravan is not read.

- A road's cost is a `DefModExtension_RotR_RoadDef` on its `RoadDef`: `costs` of Work, WoodLog, Stone, Steel, Chemfuel, and for glitter roads Plasteel,
  Uranium and components. Stone road: 1,200 Work, 240 wood, **1,800 Stone**, 300 steel. Asphalt: 1,800 Work, **3,600 Stone**, 600 steel, 300 chemfuel.
  Railroad: 1,200 Work, 600 wood, 800 Stone, 600 steel, 40 chemfuel (`1.6/Defs/RoadDefs/RoadDefs.xml`; the game's own roads get costs by
  `1.6/Patches/patches.xml`). **"Stone" names no ThingDef**; whether any block type counts, or only one, cannot be told from the XML.
- Construction happens at a `RoadConstructionSite` world object with `RoadConstructionLeg`s (`1.6/Defs/WorldObjectDefs/WorldObjects.xml`), worked by a
  caravan on the world map with Construction skill; resources travel in the caravan and are consumed as the work proceeds, a fraction at a time
  (README, "Important note"; the string `RoadsOfTheRim_CaravanNoResource`). An ISR2G item (120 steel, 10 components) replaces wood and stone at
  four times the work. **There is no map-side site, no cell that receives stone, and no road laid on a map.**
- **Consequence.** A spreader that "tips in a line" has nothing to tip onto. What the tipper honestly gives a road builder is capacity: a stone road
  leg's 1,800 blocks are 2,250 kg, one base-body load; an asphalt leg's 3,600 blocks are 4,500 kg, two loads or one bulk body with high sides and
  a little over. **Unverified:** whether the site draws on cargo inside a vehicle, which needs Rails and Roads' code and Vehicle Framework's caravan
  inventory patches read together; **the owner should test a stone load in the tipper at a site before relying on it.** A spreader node could
  return if a decompile shows an honest figure to move (a leg's Work or Stone cut by a labelled share through one patch); until then it is cut.

**Figures (authored unless marked).**

| Figure | Value | Why |
| --- | --- | --- |
| `CargoCapacity`, base body | 2,400 kg | 96 granite chunks or 4,800 steel; just over the Big Rig's 2,200 (from its patched Def), far under the HEMTT's 15,000 |
| High sides | +600 kg | 24 more chunks; a quarter more, for a longer tip and a heavier truck |
| Rock body | 2,400 kg, `Phobos_TipRate` x1.5 | A steep body sheds chunks fast: 96 chunks in about 32 s real time (about 0.8 in-game hours) |
| Bulk body | 3,600 kg, `Phobos_TipRate` x0.75 | Half as much again for ore, blocks and steel; 7,200 steel is 96 stacks |
| `Phobos_TipRate` | 2 things a second at full health | 96 chunks in 48 s real time at normal speed, about 1.2 in-game hours (2,500 ticks an hour) |
| The fan | Half-disc, radius 8, about 100 cells | Holds a base load of chunks; a high-sided or bulk load of chunks (120 to 168) will not all fit, by design: chunks want the rock body, bulk wants stackables |
| To carry 20 chunks | 500 kg | The brief's test case; so is 1,000 steel |

**Sources and simplifications.** A real 6x4 tipper carries 13 to 16 t; 2,400 to 4,200 kg is a gameplay cut so a truckload is not a colony's whole
stock. Item masses, stack limits and placement rules are the game's; the tip rate, the fan and the body figures are authored and untuned. Vehicle
Framework's capacity unit is treated as kilograms because it is compared with `InventoryMass`, which sums item Mass stats (from the code above).
Whether Vehicle Framework drops the excess when a damaged body's `CargoCapacity` falls below the load aboard is **not read**; the agent's lean is
that nothing falls off, the truck refuses to load more and its Health tab names the body.

**Argued against or later.** *Replacing Vanilla Outposts Expanded's deliveries:* outposts produce every `TicksPerProduction` ticks (900,000 by
default, times a setting; output = base + per pawn + per skill, times a multiplier, `Outpost.cs:68, 237-244, 283-291`) and deliver to the nearest
player home by teleport, a generated pack animal, store, pods or pack-or-pods (`Outpost.cs:718-833, 867-940`); they touch vehicles only to empty
them (`Outpost.cs:43-45, 326-332`). A mining outpost (`Outpost_Mining`, steel to components) already delivers without a truck; making it wait for
ours would be a worse outpost and a Harmony patch on a system that works. **The tipper is for on-map hauling, caravans and the family's own
chain, rig to base.** *A loose-cargo or volume model:* nothing in the game or Vehicle Framework carries volume; mass does the work. *Tipping into a
stockpile zone selectively:* hauling with extra steps; the game's haulers already do it from the pile. *A conveyor tie-in:* Vanilla Factory Expanded's
belts pull only from its hoppers (code-verified earlier, in memory); a tip onto a hopper cell already works through the placement rules and needs
no code of ours. *A heated body for frozen loads:* cold only slows an item's rot, so heating would move no figure a player wants moved; cut.

## The truck

| Item | Value |
| --- | --- |
| Footprint, drawSize | (3, 8), (4, 9). Heavy on the tanker's 3 x 10 scale (the owner finds the 3 x 6 Big Rig small): a rigid tipper is shorter than a tanker, so two cells shorter, same width. Cells x -1 to 1, z -3 to 4, cab forward at high z |
| Empty mass and power | 10,000 kg, 220 kW (secondary: typical of the 6x4 rigid tipper class, not a specific model). Vehicle Framework `Mass` 360 on the tanker's scale (340 for 9,000 kg, authored, not comparable between mods) |
| Drivetrain Def | `PhobosDrivetrain_HeavyDiesel` (30 kW per tonne, `Drivetrains.xml:45-69`) with `emptyKilograms` 10,000 and `ratedKilowatts` 220 in the figures extension, as the tanker carries 9,000 and 224 (`Vehicle_BulkTanker.xml:321-324`). The model moves kerb plus inventory (`Powertrain.MassKg`, `PhobosFramework.Vehicles/Systems/Powertrain.cs:289`), so a full base body is 24% more mass to move and a full bulk body with high sides 42% |
| Fuel | Chemfuel, 28 a day driving, capacity 120 (the tanker's figures, authored); a tip runs a few minutes at idle and gets no figure of its own |
| Crew | Driver only |
| Research | Machining and Electricity, as the tanker (`Vehicle_BulkTanker.xml:463-466`) |
| Cost | 1 `PhobosVehicles_EngineAssembly`, 1 `PhobosVehicles_DrivetrainKit`, 1 `PhobosVehicles_ElectricalKit`, plus 260 steel and 8 components; 15,000 work. Our parts always exist and set repair costs (owner, 9 Nov 2026); RimThunder's medium engine (4 to 15 t empty) and transmission are accepted substitutes at 10 t, VVE's car parts are not (under 6 t only). `PhobosVehicles_Transmission` is the repair part for the transmission component; the kit already includes one for the build (open question below) |
| Systems loadout | The heavy land loadout's set: electrical, cooling, lubrication, fuel, service, running gear, brakes, transmission, cabin (`Loadouts_Default.xml:348-365`). Loads: headlights (existing) and the work lights (node). No hydraulic quantity yet: the rams run off the engine through a power take-off, and the family's hydraulic-leak reactor comes later |
| Tie-ins by `IfModActive` folder | None in the first build. A `Mods/RailsAndRoads` folder is reserved for the spreader if a decompile gives it a figure |
| Prior art | Forklift Logistics and Caravan Hauler haul (IDs in [the mechanics record](vehicle-framework-mechanics.md)); Trains of the Rim's gondola is 30,000 capacity, cargo only. **Nothing tips** |

## Upgrade tree

| Node key | Figure it moves | Cost | Research | Prerequisite or exclusive with | Look |
| --- | --- | --- | --- | --- | --- |
| `HighSides` | `CargoCapacity` +600 | 60 steel, 1,500 work | None beyond the truck's | None | Taller side boards on whichever body is fitted |
| `RockBody` | `Phobos_TipRate` x1.5 | 120 steel, 4 components, 3,000 work | Machining | Exclusive with `BulkBody` (each names the other in `disableIfUpgradeNodesEnabled`) | A short, steep body with a thick floor |
| `BulkBody` | `CargoCapacity` set to 3,600, `Phobos_TipRate` x0.75 | 150 steel, 4 components, 3,000 work | Machining | Exclusive with `RockBody` | A long, shallow body overhanging the rear axles |
| `WorkLights` | Lights the fan behind the truck while it tips: pawns hauling there at night keep full work speed (the game's `WorkSpeedGlobal` falls to 0.80 at no light, 1.00 from 0.30 glow, `StatPart_Glow` on the stat's Def); an electrical load on the systems service | 30 steel, 2 components, 800 work | Electricity | None | Two lamps on the headboard, lit |
| `TailgateSpreader` | None found: cut from the first tree (see "The spreader") | | | Would live in a Rails and Roads folder | A spreader box on the tailgate |

The bare truck (no node) is the plain 2,400 kg box, so a player drives it before choosing a body. A refit refunds half (`refundFraction` 0.5, from
the code). Node keys are saved in the player's game: never rename or remove one.

## Parts

Cells on 3 x 8: x from -1 to 1, z from -3 (tail) to 4 (nose); engine and front axle at z 4, cab at z 3, body from z 1 to z -2, tailgate at z -3. The
family's conventions apply (external for what a shot meets first, real cells, engine with `priorityStatEfficiency`). **Append only, never reorder.**

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 160 | External | all, weight 3 | BodyIntegrity | |
| Cab | 50 | External | (-1, 3) (0, 3) (1, 3) | | The driver |
| Engine | 60 | Internal, priority | (-1, 4) (0, 4) (1, 4) | MoveSpeed, Phobos_TipRate | Drives the road and, through the power take-off, the pump |
| Transmission | 40 | Internal | (0, 2) | MoveSpeed | |
| Front axle and wheels | 35 | External | (-1, 4) (1, 4) | MoveSpeed | |
| Rear axles and wheels | 55 | External | (-1, -1) (1, -1) (-1, -2) (1, -2) | MoveSpeed | Tandem axles |
| Fuel tank | 35 | Internal | (1, 2) | | Vehicle Framework's fuel-leak reactor |
| Tipper body | 90 | External | (-1, 1) (0, 1) (1, 1) (-1, 0) (0, 0) (1, 0) (-1, -1) (0, -1) (1, -1) (-1, -2) (0, -2) (1, -2) | CargoCapacity | A holed body carries less; the node sets its label (rock, bulk), the part key never changes |
| Tipping rams | 40 | Internal | (0, 1) (0, 0) | Phobos_TipRate | Slow rams, slow tip |
| Hydraulic pump and power take-off | 35 | Internal | (-1, 2) | Phobos_TipRate | Below the floor nothing lifts |
| Tailgate | 30 | External | (-1, -3) (0, -3) (1, -3) | CargoCapacity | A lost tailgate sheds part of the load's room |
| Tip controls and valve block | 25 | Internal | (0, 3) | Phobos_RigControl | Below the floor the body will not lift, and the refusal names it |
| Wiring | 25 | Internal | (1, 3) | Phobos_RigControl | |
| Lights | 15 | External | (-1, 4) (1, 4) (-1, -3) (1, -3) | | Headlights and the work lights |

Reactors: the fuel tank leaks (Vehicle Framework's `Reactor_FuelLeak`); the engine explodes as Vanilla Vehicles Expanded's do (`Reactor_Explosive`);
a hydraulic leak on the rams waits for the family's own reactor. **What a hit ruins first:** the body, twelve of the truck's twenty-four cells and
external, so most shots cost capacity; then the rear wheels and the cab. The rams sit under the body and go only once it is holed. `Phobos_TipRate`
averages engine, rams and pump; `Phobos_RigControl` takes the minimum of controls and wiring, with the rig's floor.

## Art request

The route and rules are in [art services](vehicle-art-services.md) and [paid image generation](paid-image-generation-rules.md). **The pilot is the bare
body, north**, inspected at native size first. Base body: north, east and south at 128 px per drawSize unit (512 x 1152 for (4, 9)), the chassis-cab
with a plain box body, empty (no cargo, crew, damage or text), with a colour mask. Overlays, each a north, east and south set on the body's canvas:
the body **raised** (the tip, drawn by our comp); the **rock body** and the **bulk body**; **high sides** on each body (or one that reads on all); the
**work lights** lit; **damage** (a holed body, drawn by our comp: Vehicle Framework never reads a part requirement on an overlay, as the tanker found).
The spreader is not drawn until it has a figure. A 64 px icon from the north. Record each picture's tool, prompt, date and source hashes beside it.
- **Size it first** (owner's rule, 9 Nov 2026): before the first picture, measure the planned footprint against RimThunder's 3 x 10 PLS and HEMTT, and generate at 1.5 times the drawn size ([the rules](paid-image-generation-rules.md)); the tanker had to grow from 2 x 6 to 3 x 10 after its first look.

**Motion (9 Nov 2026; [the animation research](vehicle-animation-research.md)):** the tipper body is its own sprite, separate from
the chassis, so a comp can slide it rearward a little and grow it over two seconds with its shadow lengthening while the load lands; the tailgate is its own
small sprite on a hinge; dust in the fan is a fleck. The rock and bulk bodies are each such a sprite. No frame sheets.

## Open questions for the owner

| Question | Agent lean |
| --- | --- |
| How much should the base body carry: 2,400 kg, or more against the HEMTT's 15,000 already on the list? | **Decided by the owner, 9 Nov 2026: 2,400 kg.** A truckload that still needs a yard, and the tree takes it to 4,200. A larger base load would need a larger tip area |
| Two bodies or one? | Two, exclusive: the choice is what makes the tree a tree. If one, the rock body and `HighSides` only |
| When a holed body's `CargoCapacity` falls below the load aboard, does anything fall off? | Nothing falls; the truck refuses more and the Health tab names the body. A spill reactor is a later option |
| Does the tip stop at the fan's edge or search further (the game reaches 12.9 cells)? | The fan, radius 8, so the yard is predictable and the refusal honest |
| Keep the `TailgateSpreader` key reserved, or drop the idea? | Reserve the key and a folder; build nothing until Rails and Roads' code is read |
| Should the build cost list `PhobosVehicles_Transmission` beside the drivetrain kit, which already includes a transmission? | No: the kit builds it, the transmission item repairs it. If the owner wants provider parts to matter more, list both |
| A test the owner can run before any code: a stone load in a Vehicle Framework truck at a Rails and Roads site, does the site draw on it? | **The owner agreed to run it (9 Nov 2026); not yet run.** It decides whether the tipper is a road builder's wagon at all |
| The footprint, 3 x 8 against the tanker's 3 x 10? | 3 x 8: a rigid tipper is shorter than a tanker; the owner judged size by eye once already and should see the pilot |

## Who chose what

- **Owner (9 Nov 2026):** one record per vehicle, research and record only; no Helixien gas hooks; deepchem rides in the tanker; the camp's power is
  the generator set's and the tipper's loads are lights only; our part items always exist and set repair costs, RimThunder's medium or heavy engine by
  empty mass and transmission as substitutes, VVE's car parts under 6 t only; every node moves a figure and none changes the parts list; heavy trucks
  sized against the 3 x 10 PLS.
- **Owner (9 Nov 2026, on reading this record):** the base load is 2,400 kg; the owner will test a stone load at a Rails and Roads site before any
  tipper code.
- **Agent:** every figure in "Figures" and "The truck" but the base load; the half-disc fan, radius 8; things a second as the tip unit; the all-or-nothing tip and its
  refusal wording; the two exclusive bodies; cutting the heated body and the spreader; the work lights node; 3 x 8; the parts table; the leans above.

## Sources

- Decompiles, read 9 Nov 2026 and summarised in our words: Vehicle Framework 1.6 (`.local/decompiled/VehicleFramework-1.6/Vehicles.decompiled.cs`, lines as
  cited); RimWorld 1.7 (`Verse.GenPlace.cs:19, 27-127, 129-189, 214-311`, `Verse.GridsUtility.cs:267-284`, the item mass sum at `all.cs:708450-708458`);
  Vanilla Expanded Framework's outposts (`VEF-Outposts-1.7/Outposts/Outpost.cs`, `ResultOption.cs`, `DeliveryMethod.cs`). The VOE assembly is not decompiled.
- Defs through `scripts/query-defs.py`, 9 Nov 2026: `WorkSpeedGlobal`'s `StatPart_Glow`; the chunk, steel and block Defs; `Outpost_Mining`, `Outpost_Drilling`;
  patched `VVE_BigRig` 2,200, `VVE_Mule` 600, `HMV_A` 300, `RT_M1075_HEMTT` 15,000 (3 x 10, Mass 2,000), `TOTR_Trains_Railcar_Gondola` 30,000 (3 x 10).
- Rails and Roads of the Rim (Continued), Workshop 3271115410, Vampiresbane, continued by Mlie; original Roads of the Rim by Loconeko, RimRoads by Jecrell (its
  README): `1.6/Defs/RoadDefs/RoadDefs.xml`, `1.6/Defs/WorldObjectDefs/WorldObjects.xml`, `1.6/Defs/ThingDefs/RotR_Buildings.xml`, `1.6/Patches/patches.xml`,
  `Languages/English/Keyed/settings.xml`, `README.md`; its assembly not read; it declares 1.3 to 1.6, not 1.7 (`About/About.xml`).
- Our own: `Vehicle_BulkTanker.xml`, `Drivetrains.xml`, `Loadouts_Default.xml`, `Parts.xml`, `Powertrain.cs:289`; [the utility vehicles design](utility-vehicles-design.md);
  [vehicle health, slice 3](vehicle-health-consequences.md). The 6x4 tipper's class figures (10 t empty, 220 kW, 13 to 16 t payload) are secondary, from general
  manufacturer literature for the class, not a named model.
