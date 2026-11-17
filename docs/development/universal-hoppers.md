# Universal hoppers: research and ideas (17 Nov 2026)

**Status (17 Nov 2026):** research and record only; nothing is built.

The owner's request, 17 Nov 2026: "I just thought of something and its (near-)Universal Hoppers! Like, we could have one that takes
aggregate from our Talus and the patched conveyors of ours would then transport all that throughout the machinations of our colony!
This is just one example of what is to be many." The owner then said "aggregate" meant **chunks**. This record is the research and
the ideas; the owner picks what, if anything, is built.

**How to read the marks:** **[code]** was read from decompiled code or a mod's Defs with `scripts/query-code.py` and
`scripts/query-defs.py` on 17 Nov 2026, with its file and line; **[A]** is a figure or choice we authored; **[I]** is our inference
from what was read; **[S]** is a secondary source such as a Workshop comment. **None of it is tested in game.**

## Read this first

- **One storage building with the right marks is read by nearly everything** in the owner's game that takes from a hopper: the
  game's paste dispenser, Vanilla Furniture Expanded - Factory's belts and machines, The Dead Man's Switch's automatic benches and
  the nutrient paste grinder. "Near-universal" is mostly a matter of marks on a Def, not new machinery.
- **The Talus cannot tip onto any building today.** Its tip skips every cell with a building on it, and its loading grab skips
  anything in storage. Both are one line each. Until they change, nothing in this record connects to the truck.
- **Chunks do not stack.** One chunk fills a hopper cell and a belt cell. A full Talus carries about 96 of them.
- **For chunks the machines are the limit, never the belts.** A belt lane can pass about 1,500 chunks a day; one automated masonry
  saw cuts about 9. A bunker for the Talus is a store that feeds a slow line for days, not a way to move stone faster.
- **Belts take whatever is in a marked hopper and ignore its filter**, unless the storage also carries our filter mark.
- **Ordinary workbenches cannot be fed by any hopper.** The game has no hook for it; a hopper beside a bench only shortens the walk.
- **Blind spot:** Defs "as patched" come from the file the game wrote on 8 Nov 2026 (335 mods then, 349 now). Figures taken from it
  say so. Process times and chunk figures should be read again from a fresh file before any Def carries them.

## The owner's directions (17 Nov 2026)

| Direction | The owner's choice |
| --- | --- |
| What "aggregate" means | Chunks. No new material |
| Idea families to explore | Vehicles into hoppers; hoppers into other mods; belts out to vehicles |
| Not chosen | Hoppers into our own machines (parts bins at the depot and dyno bays, the field kitchen, Medical's equipment, the later chemical industry mod). Not explored here |
| How wide to survey | Active mods only. Project RimFactory Revived is subscribed but switched off, and is not read |
| What to deliver | Research and a record. Nothing built |

## What the research found

### Who reads a hopper, and how

| Reader | What it accepts as a hopper | What it takes, and how | Source |
| --- | --- | --- | --- |
| The game's paste dispenser | Any building whose Def has `building.isHopper` | Reads the things on the cells sharing an edge with it, never diagonals. Takes food straight off the cell. Ignores the hopper's filter, forbidding and reservations | **[code]** RimWorld 1.7, `Building_NutrientPasteDispenser`, `full:all.cs:585261-585408`; `StorageGroupUtility.IsHopper`, `all.cs:576881` |
| The cook's "fill hopper" job | Only the Def named `Hopper` | Any other hopper is filled by ordinary hauling at its storage priority | **[code]** `WorkGiver_CookFillHopper`, `all.cs:311125-311222` |
| VFE Factory belts | A `Building_Storage` that is the factory hopper class or whose Def carries Pipe System's `FactoryHopperExtension` | The cell behind the belt first, then each side. Takes the first item on any of the storage's cells, the whole stack, with no filter check. Looks every 10 ticks | **[code]** `Building_Conveyor.TryPullFromDir`, `VFEFactory:Building_Conveyor.cs:1630-1685`; `TryPullItem`, `1607-1625` |
| VFE Factory and Pipe System machines | Anything on an input cell whose Def carries `FactoryHopperExtension` (it need not be storage) | Every item spawned on the input cell, plus the inner store of anything there that exposes one. Matches by item or category, never by the hopper's filter. Looks every 100 ticks | **[code]** Pipe System build of 17 Nov 2026, `Process.InputFactoryHopperDetected`, `Process.cs:817-829`; `CheckInputSlots`, `831-919`; `GetThingsAndContainedItems`, `344-365` |
| The Dead Man's Switch automatic benches (mill, furnace, reaction chamber, printer) | A linked facility within 6 cells whose Def carries `Fortified.ModExtension_MaterialSource`, and which the bench's Def lists as linkable | The bench picks its bill's ingredients from the linked storage and moves them into itself, with no pawn. Skips forbidden and reserved items | **[code]** Fortified build of 12 Nov 2026, `LinkedStorageIngredientPuller.cs:54-103, 205-261, 286-335`; `DMS_Hopper` as patched |
| Vanilla Nutrient Paste Expanded's grinder | A building touching its edge that carries `CompRegisterToGrinder` | Food on the hopper's **own position cell only**, so a hopper wider than one cell feeds it from one cell | **[code]** `VNPE-1.6:Building_NutrientGrinder.cs:70-89, 141-157`; `CompRegisterToGrinder.cs:29-54` |
| Ordinary workbenches | Nothing | A pawn fetches ingredients from anywhere in the bill's radius. No rule lets storage feed a bench | **[code]** `WorkGiver_DoBill.TryFindBestIngredientsHelper`, `all.cs:311779-311894` |

### Where a belt delivers

**[code]** `Building_Conveyor.GetConveyorOrStorageAt` (`Building_Conveyor.cs:444-473`), `GetDumpCapability` (`1338-1500`) and
`CompleteTransfer` (`1771-2010`). A belt hands its item to whatever is ahead, in this order:

1. **Another belt.**
2. **Storage**, if its filter accepts the item and a cell has room (`GetMaxItemsAllowedInCell`).
3. **A refuelable building**: it refuels it directly, up to what the building needs (`1810-1852`). No hopper is involved.
4. **Open ground**: one stack per cell, then it waits.
5. **Any other building**: it waits for ever.

Anything of the Item category can ride a belt; there is no list of allowed Defs. A belt cell carries one stack and moves it one
cell in 40 ticks (`ConveyorExtension`, `Buildings_Conveyors.xml:51-55`). A belt also swallows any item that lands on its own cell
(`CheckForItemsOnCell`, `1687-1710`, every 10 ticks).

### How much a cell holds

**[code]** `GridsUtility.GetMaxItemsAllowedInCell` (`all.cs:71845-71848`) returns the `maxItemsInCell` of the building on the cell,
or 1 on open ground. The game's own hopper and VFE Factory's hold 1; our deep factory hopper holds 4; The Dead Man's Switch feed
hopper holds 3 on each of its 2 cells. Nothing in the game caps the figure.

### What takes chunks today

| Machine | Mod | Takes | Gives | Time | Source |
| --- | --- | --- | --- | --- | --- |
| Automated masonry saw | VFE Factory | 1 stone chunk, from one input cell | 20 blocks of that stone | 6,400 ticks | **[code]** `VFEFactory_MakeStoneBlocks`, as patched 8 Nov |
| Automated smelter | VFE Factory | 1 stone chunk | 3 steel | 3,200 ticks | **[code]** `VFEFactory_SmeltMetalFromChunk`, as patched 8 Nov |
| Alloy forge | VFE Factory | 1 stone chunk | 3 obsidian | 18,200 ticks | **[code]** `ProcessDefs_AlloyForge.xml:95-125`, as written |
| Auto mill | The Dead Man's Switch | Chunks from a linked feed hopper | Stone blocks, by its description | Not read | **[code]** `DMS_Production_AutoMill.xml`; its recipe list is built in code and was not read |

All three VFE Factory processes take chunks **only from marked hopper cells** (`onlyGrabAndOutputToFactoryHoppers`).

**The Dead Man's Switch already ships a chunk hopper.** Its feed hopper (`DMS_Hopper`, 2 by 1, powered, 3 items a cell) is described
by its authors as moving "the coarse material that a normal conveyor line cannot handle, such as stone chunks, slag and mech
wreckage", and its filter allows chunks, corpses, raw food and wood. It carries no factory-hopper mark, so VFE Factory's belts and
machines cannot read it.

### The Talus as it stands

- **[code]** Its cargo is Vehicle Framework's ordinary inventory, 2,400 kg on the base truck
  (`mods/PhobosVehicles/Defs/Phobos/Vehicles/Tipper/Vehicle_Tipper.xml:35`). What the high sides and the rock and bulk bodies add
  was not read for this record.
- **[code]** The tip puts the load on cells in a half-disc of radius 8 behind the tailgate, and refuses any cell with a building:
  `if (!cell.Standable(map) || cell.GetEdifice(map) != null) return false;`
  (`src/PhobosFramework.Vehicles/Systems/CompPhobosTipper.cs:89`). The next line already respects how many items a cell may hold.
- **[code]** The loading grab lifts loose items within reach and refuses anything in storage (`CompPhobosTipper.cs:255-260`).
- **Corrected in this change:** [the tipper record](vehicle-tipper-hauler.md) said a tip onto a hopper cell "already works". It does not.

### Chunks, in figures

| Figure | Value | Source |
| --- | --- | --- |
| A granite chunk | Stack limit 1, 25 kg | **[code]** `ChunkGranite` as patched 8 Nov |
| Chunks in a full base Talus | 96 | **[I]** 2,400 kg divided by 25 kg |
| One belt lane | At most 1 item every 40 ticks: about 62 an hour, 1,500 a day | **[I]** from 40 ticks a cell and 1 item a cell; not measured |
| A belt clearing one Talus load | About 3,840 ticks, an hour and a half | **[I]** |
| One masonry saw | About 9 chunks a day; 96 chunks in about 10 days, giving 1,920 blocks | **[I]** from 6,400 ticks a chunk and 60,000 ticks a day |
| One smelter | About 19 chunks a day; 96 chunks in about 5 days, giving 288 steel | **[I]** from 3,200 ticks a chunk |

So a truckload is roughly **ten days of work for one saw**. The bunker's job is to hold that load out of the way and let the line
draw on it, and its size is set by the truck, not by the belt.

### Author activity

Read with `scripts/workshop-research.py report` on 17 Nov 2026 (the newest 50 comments of each).

| Mod | Authors | Updated | Declares | Uploader's last reply | What it showed |
| --- | --- | --- | --- | --- | --- |
| Vanilla Furniture Expanded - Factory | Oskar Potocki, Sarg Bjornson and one more | 14 Oct 2026 | 1.7, 1.6 | 6 Nov 2026 | Active. The uploader says bugs reported in comments are not answered there. One player reports an endless error storm when drop pods land on a belt **[S]** |
| Vanilla Expanded Framework (Pipe System) | Oskar Potocki, XeoNovaDan and five more | 16 Nov 2026 | 1.7 | Not read | Active by its update date. The installed Pipe System file is newer than our earlier decompile; it was decompiled again and the hopper code is unchanged |
| The Dead Man's Switch | AobaKuma, Bread Mo and three more | 12 Nov 2026 | 1.7 | 3 Nov 2026 | Active |
| Fortified Features Framework | AOBA | 12 Nov 2026 | 1.7 | Not read | Active by its update date only |
| Vanilla Nutrient Paste Expanded | Oskar Potocki, Kikohi | 14 Oct 2026 | 1.7, 1.6, 1.5 | 31 May 2026 | Updated for 1.7; the uploader's replies are older |
| VNPE: Reimagined Progression | MrHydralisk | 11 Nov 2026 | 1.7, 1.6, 1.5 | 13 Nov 2026 | Active; fixes within days |
| Vehicles at Work | 生吃螃蟹要吐壳 | 11 Nov 2026 | 1.7, 1.6 | 15 Nov 2026 | Active; hauling faults are being reported and answered |
| Munitions Industries for VFE - Factory | turkler | 24 May 2026 | 1.6 only | 23 Aug 2026 | Quiet. It runs on the owner's 1.7 but declares only 1.6 |

## What "universal" can honestly mean

A hopper is universal to the degree that one Def carries every reader's mark. All of these are marks on a Def, added in XML, each
behind `MayRequire` so a player without that mod sees nothing.

| Mark | Makes it readable by | Kind |
| --- | --- | --- |
| `building.isHopper` | The game's paste dispensers | The game's own field |
| `PipeSystem.FactoryHopperExtension` | VFE Factory belts; every Pipe System machine's input and output cells | Vanilla Expanded Framework |
| Our `BeltFilterExtension` | Makes belts take only what the storage's filter allows | D'thorga Factory Additions |
| `Fortified.ModExtension_MaterialSource`, a facility comp, and a line in each bench's linkable list | The Dead Man's Switch automatic benches | Fortified Features Framework, with a patch on each bench Def |
| `VNPE.CompProperties_RegisterToGrinder` | The nutrient paste grinder | Vanilla Nutrient Paste Expanded |

**What stays out of reach, and why:**

- **Ordinary workbenches.** There is nothing to mark. Feeding them would mean a Harmony patch on the game's bill search or a bench
  class of our own, as The Dead Man's Switch wrote. Not proposed.
- **A per-building switch for every reader.** Our "Belts may take from here" switch works because we patch the belt. The paste
  dispenser and The Dead Man's Switch benches read the Def, so a switch for them would need a patch on each. Marks on chosen Defs
  are the honest route.
- **The cook's fill job** will never fill a hopper of ours on its own; ordinary hauling does, at the storage's priority.
- **The grinder** reads one cell of a wide hopper.

## The ideas

Each row says what it **lets** the player do, what it **stops** them doing and how it **changes** the way they play. "Kind" is what
building it would take. Verdicts are the agent's, for the owner to overrule.

### Vehicles into hoppers

| # | Idea | Reads → changes | Lets · Stops · Changes | Kind | Verdict |
| --- | --- | --- | --- | --- | --- |
| UH1 | **Tip into storage.** The Talus's tip may land on a storage building's cells where its filter accepts the item and the cell has room | `CompPhobosTipper.Fits` → which cells count | **Lets** a truck deliver straight into a hopper, bunker or shelf. **Stops** nothing landing where the filter refuses; such items fall on open ground in the fan as today. **Changes** the tip from a pile for haulers into a delivery | Small C# | **First.** Everything else in this family rests on it |
| UH2 | **A receiving bunker.** A wide storage building a truck backs up to, sized for one load, carrying every mark | New Def → a store belts and machines draw from | **Lets** a whole load go in with one tip and a line draw on it for days. **Stops** a load being tipped where it will not fit: the rest stays aboard and the truck says why. **Changes** a quarry run into "tip and leave" | New building with art, XML | **Strong.** Needs UH1. See the bunker comparison below |
| UH3 | **The drill rig's output into storage.** The rig puts its ore and chunks into accepting storage beside it instead of on the ground | A new `RigBufferOutlet` (`CompPhobosDrillRig.cs:30-34`) → where portions land | **Lets** a rig beside a bunker or belt feed a line unattended. **Stops** nothing. **Changes** a rig site into a fixed mine head | Small C# | Medium. VFE Factory's own drill already makes chunks at a belt; ours earns it for ore |
| UH4 | **Vehicles at Work hauls to the bunker** (was SD3) | Its haul picks a store with the game's own storage search (`StoreUtility.TryFindBestBetterStorageFor`, `VehiclesAtWork-1.7:WOR/Haul.cs:139`) | **Lets** its vehicle hauls end at the bunker. **Stops** nothing. **Changes** nothing we build | None **[I]** | Free if the bunker is real storage. An argument for that kind of bunker |
| UH5 | **The tanker's cans into a hopper** (was SD33) | `Port_Items` (`TankPorts.cs:29-75`) → prefer accepting storage in its contact cells | **Lets** a fuel line start at the tanker. **Stops** nothing. **Changes** little | Small C# | Low, as SD33 was: the mod it serves is quiet and declares 1.6 only |

### Hoppers into other mods

| # | Idea | Reads → changes | Lets · Stops · Changes | Kind | Verdict |
| --- | --- | --- | --- | --- | --- |
| UH6 | **Teach the hoppers the owner already has.** Give VFE Factory's hopper and our deep hopper The Dead Man's Switch material-source mark, and give its feed hopper the factory-hopper mark with our filter mark | Def marks → who may read which hopper | **Lets** a belt deliver to a hopper that an automatic bench of the other mod then draws from, and lets VFE Factory's saw and smelter read the chunk hopper. **Stops** belts taking from the feed hopper what its filter refuses. **Changes** two separate factory systems into one line | XML only | **First, with UH1.** No art, no code |
| UH7 | **One hopper Def with every mark**, placeable anywhere | New Def → one building for every reader | **Lets** one hopper type serve every machine. **Stops** nothing. **Changes** which hopper a player reaches for | XML, art | Low once UH6 and UH2 exist: the bunker is this hopper, larger |
| UH8 | **The chunk line**, the owner's example: Talus, bunker, belt, saw or smelter | UH1, UH2 and UH6 together | **Lets** stone go from a quarry run to blocks with no hauling. **Stops** nothing. **Changes** stonecutting from a pawn's job into a line fed by truck | Nothing of its own | The worked example. Its limit is the machines, at about 9 chunks a day for each saw |
| UH9 | **Fuel by belt** | A belt already refuels a refuelable building ahead of it | **Lets** wood or chemfuel reach a generator by belt. **Stops** nothing. **Changes** nothing we build | None | Already in VFE Factory. Worth a line in our guide, nothing more. It does not reach a vehicle, which is not a building |
| UH10 | **Item to pipe and back** | Pipe System's converters must be plain storage and read their own cell (`CompProperties_ConvertThingToResource.cs:26-41`) | **Lets** a hopper end a belt in a pipe. **Stops** nothing. **Changes** nothing the owner's game lacks | XML | Not now: the owner's game has these already for chemfuel and paste, and the idea belongs with the family the owner did not choose |

### Belts out to vehicles

| # | Idea | Reads → changes | Lets · Stops · Changes | Kind | Verdict |
| --- | --- | --- | --- | --- | --- |
| UH11 | **A belt's end beside a parked Talus.** A belt ending on bare ground drops its goods there; the loading grab lifts loose goods within reach | Nothing: both behaviours exist | **Lets** a belt load a truck with no building. **Stops** nothing. **Changes** nothing we build | None **[I]** | **Worth one look in game before building UH12 or UH13.** One stack per ground cell, so it is slow |
| UH12 | **A load-out bunker.** Storage with a switch, "Vehicles may load from here", off by default; the grab may take from it | `CompPhobosTipper.Grabbable` → what the grab may lift | **Lets** belts fill a bunker while the truck is away, and the truck take the lot when it comes. **Stops** the grab emptying ordinary stockpiles. **Changes** factory output into something a truck collects | Small C#, the bunker of UH2 | **Strong**, after UH2 |
| UH13 | **A loading chute.** A building at a belt's end that passes what lands in it into the cargo of a vehicle parked against it | New building → `VehiclePawn.AddOrTransfer` | **Lets** hands-free loading. **Stops** overloading: it holds goods when the truck is full or absent, and says which. **Changes** a parking spot into a loading bay | New building with art, C# | Medium. More work than UH12 for the same result with a grab |
| UH14 | **A wastepack run.** Belt toxic wastepacks to the load-out and carry them off by truck | UH12 or UH13 | **Lets** waste leave the base by the truckload. **Stops** nothing. **Changes** where waste piles up | Nothing of its own | An example. A wastepack stacks to 5 at 6 kg each (as patched 8 Nov), so a base Talus carries 400 **[I]** |
| UH15 | **Staging for a caravan.** Factory output held in a bunker beside the vehicle that will carry it | Vehicle Framework's own loading jobs | **Lets** a shorter carry when loading. **Stops** nothing. **Changes** little | None | Low: the pawns still carry every stack |

### Speculative, and labelled so

| # | Idea | Verdict |
| --- | --- | --- |
| UH16 | **Crushed stone.** A crusher at the bunker turning chunks into a loose road stone, for Rails and Roads of the Rim (on the owner's list; Vampiresbane) | Parked. The owner meant chunks, and no road in the owner's game asks for a new material. It would be a new item, a new machine and a tie-in with a mod not read for this record |

### The bunker: two ways to hold a truckload

| | Goods shown on its cells | Goods held inside, let out one at a time |
| --- | --- | --- |
| How | Plain storage with a high `maxItemsInCell` | Our own building with an inner store and a gate cell, as the cold box holds its goods |
| Who can read it | Every reader in the first table, unchanged; haulers; Vehicles at Work; the filter tab | Only what we write code for. Machines could read it through Pipe System's inner-store interface; belts could not |
| Code | None beyond UH1 | A new class, a gate rule, saved state |
| Cost to run | The same as a stockpile of 96 chunks | Less to draw, more to tick |
| Removing the mod | The goods stay on the ground | The goods inside are lost unless emptied first |

**Agent default: goods shown on its cells.** It is the one that makes the bunker universal, and it costs no code.

## Argued against

- **Tipping straight onto belts.** A belt swallows what lands on it, but carries one chunk a cell, so a load would need 96 belt
  cells under the fan. One Workshop comment reports an error storm from drop pods landing on belts **[S]**; things arriving on belts
  from above is not a path its authors tested.
- **A sorting grate at the bunker.** VFE Factory's own filter belt already sorts: a belt with a narrowed filter sends allowed
  goods ahead and the rest aside (our [conveyors record](vfe-factory-conveyors.md)).
- **A hopper that feeds ordinary workbenches.** No hook, as above.
- **Reading Project RimFactory.** Switched off in the owner's game; out of scope by the owner's choice.

## Other factions' vehicles

- **A vehicle in a raid on us.** The tip and the grab are orders from the player; no raider gives them, so nothing changes. A bunker
  is a building like any other and can be attacked.
- **A vehicle parked at a base we attack.** Nothing here places a bunker or a chute at another faction's base. If base generation
  ever does, its goods are ordinary loot.
- **A vehicle we capture.** Once it is ours it tips and loads like any other.
- **A rule for UH12 and UH13:** a load-out bunker or chute serves only vehicles of its own faction, so a visitor's or a raider's
  vehicle parked beside one is never loaded or emptied.
- **Cost with several vehicles on a map:** none of these ideas ticks per vehicle. UH13's chute would look for a vehicle on a rare tick.

## Where it would live

Agent default, for the owner to revise.

| Piece | Mod | Why |
| --- | --- | --- |
| UH1, UH3, UH12: the tip, the rig outlet, the grab | D'thorga Framework's vehicle part | The Talus's code is there |
| The "Vehicles may load from here" switch | D'thorga Framework core | Generic: a second copy of the belt switch's idea |
| UH2: the bunker's Def and art | D'thorga Vehicles | Content. Its marks for other mods sit behind `MayRequire` |
| UH6: marks between VFE Factory's and The Dead Man's Switch's hoppers | D'thorga Factory Additions, in a folder loaded only with The Dead Man's Switch | It already depends on VFE Factory |

Each tie-in gets a row in [the tie-in register](tie-in-register.md) in the change that builds it.

## Cost to run and saves

- **UH1 and UH6** add nothing that ticks and save nothing. Both are safe to add to and remove from a save.
- **UH2**, as plain storage, saves what any shelf saves. Removing the mod leaves its goods on the ground.
- **UH12** saves one switch per storage building, as "Belts may take from here" does.
- **UH13** would save what it holds; a save made with it needs the chute emptied before the mod is removed.

## Questions for the owner

1. **Which kind of bunker?** (agent default: goods shown on its cells.)
2. **How big?** (agent default: one base Talus load, 96 chunks, for example 3 by 4 cells at 8 a cell **[A]**, as wide as the truck.)
3. **What may it hold?** (agent default: anything haulable through its filter, with chunks ticked to start.)
4. **What first?** (agent default: UH1 and UH6 together, since neither needs art; then UH2; then UH12.)
5. **Look at UH11 in game first?** A belt ending beside a parked Talus with the loading grab may already load it. (agent default:
   one look, when the owner next has a belt and the truck on the same map.)
6. **Crushed stone, UH16:** leave parked, or explore? (agent default: parked.)

## Who chose what

- **Owner (17 Nov 2026):** universal hoppers as a research and record round; chunks, not a new material; the three families; active
  mods only.
- **Agent defaults, owner to revise:** every verdict and the order; the bunker's kind, size and filter; where each piece lives; the
  rule that load-out serves only its own faction; parking UH16.
- **Agent's reading, not the owner's:** every **[I]** figure in this record.

## Sources

What each was read for. None of these authors endorses this record; the inferences are ours.

- **Ludeon Studios**, RimWorld 1.7: hoppers, the paste dispenser, storage limits, the bill search.
- **Oskar Potocki, Sarg Bjornson and the Vanilla Expanded team**, [Vanilla Furniture Expanded - Factory](https://steamcommunity.com/sharedfiles/filedetails/?id=3686924415): belts, the factory hopper and the chunk processes.
- **Oskar Potocki, XeoNovaDan and the Vanilla Expanded Framework team**, [Vanilla Expanded Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=2023507013): Pipe System's machine input cells and converters.
- **AobaKuma, Bread Mo and team**, [The Dead Man's Switch](https://steamcommunity.com/sharedfiles/filedetails/?id=3819799478), and **AOBA**, [Fortified Features Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3498575851): the feed hopper, the auto mill and the linked-storage puller.
- **Oskar Potocki and Kikohi**, [Vanilla Nutrient Paste Expanded](https://steamcommunity.com/sharedfiles/filedetails/?id=2920385763): the grinder.
- **Smash Phil**, [Vehicle Framework](https://steamcommunity.com/sharedfiles/filedetails/?id=3014915404): vehicle cargo.
- **生吃螃蟹要吐壳**, [Vehicles at Work](https://steamcommunity.com/sharedfiles/filedetails/?id=3835520732): its haul plan.

Decompiled code stays in the ignored `.local/decompiled/` and is summarised here in our own words.
