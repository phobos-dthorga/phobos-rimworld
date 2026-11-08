# Utility vehicles: design (8 Nov 2026)

**The stand-in is built: D'thorga Vehicles 0.1.0 (8 Nov 2026), named loadouts only (air brakes for the Big Rig and the HEMTT, the
Humvee's 24-volt system, gas-turbine tanks without cooling, steam locomotives left alone). No vehicle of ours is built. Not yet
tested in game.** The vehicles below wait for the owner's go after the framework is tested.

A design record. The owner asked on 8 Nov 2026 for utilitarian
vehicles in RimWorld, a mobile drill rig first and "all sorts" after, each with per-part health that affects how well
it does its job. **The vehicles wait for the framework** (owner, 8 Nov 2026): the
[vehicle systems service](vehicle-systems-design.md) is built first, and the mod this record describes, D'thorga
Vehicles, is created as a stand-in that carries loadouts for other mods' vehicles until our own follow. Code readings
are in [Vehicle Framework mechanics](vehicle-framework-mechanics.md); the art research is in
[art services](vehicle-art-services.md).

## Read this first

- **Owner's decisions (8 Nov 2026):** a new mod, D'thorga Vehicles; the vehicle systems framework first, this mod as a
  stand-in; a family of three to four vehicles designed in full and the rest sketched; 1.7 only; the art route is open
  (the owner is not committed to ChatGPT; PixelLab only where it fits).
- **Vehicle Framework (VF) on Steam is a 1.6 build** that the 1.7 game loads; SmashPhil says 1.7 is near. Defs here
  follow the schema that build reads; a rebuild is expected when VF 1.7 ships.
- **This mod will be unsafe to remove from a save**, as VF and Vanilla Vehicles Expanded (VVE) are: it holds the
  player's vehicles. Its README says so, and says to scrap the vehicles first.
- **Parts are appended only, never reordered**: saves hold part healths by position.
- **Every part feeds a stat that changes an outcome.** A stat nothing reads is a figure on a card (the Harrier's
  flight-control lesson): each of our stats has a floor and a refusal that names it.
- **No value loops.** A mobile rig drills the same deposits at the same rates as the game's deep drill; it buys
  mobility and self-containment, never more ore per hour than a built drill of the same cost.
- **Honest physics, labelled.** Every figure below is authored and untuned unless a source is named.

## Why a new mod, and why it starts as a stand-in

The consolidation rule (owner, 7 Nov 2026) lets a new mod exist for a different audience or different hard
dependencies. D'thorga Vehicles has both: its audience is players who run VF, and its hard dependencies are **Vehicle
Framework** (MIT), **Vanilla Expanded Framework** and **D'thorga Framework**. VVE is optional: engine blocks and upgrade
parts as costs where present, reached through `IfModActive` folders and `MayRequire`.

| Identity | Value |
| --- | --- |
| Display name | D'thorga Vehicles (agent default) |
| `packageId` and Harmony ID | `PhobosDthorga.Vehicles` |
| Prefixes | `Phobos` on defNames, namespaces, keys and saved keys |
| Build | One target, `net10.0` against the 1.7 reference, into `1.7/Assemblies`; `supportedVersions` 1.7 only |
| `Directory.Build.targets` | Reuses `VehicleFrameworkAssemblies` (the installed VF, `Private=false`) |

**As a stand-in** the mod ships no vehicle: About, README, CHANGELOG, and under `Mods/<Other>/` folders the named
loadouts that apply the systems service to VVE's vehicles, the Harrier and the HUMVEE. That proves the service on
vehicles the owner already drives. Our vehicles, their loadouts and art come after.

## Principles for every utility vehicle (agent proposal)

1. **Physics sets the kit, the kit sets the play.** Mass and footprint follow the real machine; a drill mast needs a
   long deck and outriggers; a generator set is a trailer-sized box.
2. **One stat per job.** Each vehicle's work is a `VehicleStatDef` of ours (table below) fed by the parts that do it.
   Parts → efficiency → stat → outcome, with a floor below which the job refuses and the refusal names the part.
3. **Deploy to work.** A working vehicle sets up (outriggers down, mast up) through a comp of ours that uses VF's own
   move and draft hooks and the Deployed and Undeployed events; it cannot move while set up. We do not borrow the
   turret comp's deploy state.
4. **Vanilla precedence.** Drilling reads the game's deep resource grid and portion sizes; surveying reuses the
   ground-penetrating scanner's find routine; a generator feeds the game's power net; water goes through Dubs Bad
   Hygiene where present; crews do the game's own jobs with the game's skills (Mining, Intellectual, Construction).
5. **The systems service applies to every one of ours** from the first Def: a rig has a battery, an alternator, a
   radiator and oil like any other vehicle, through its loadout.
6. **UI presents, services decide.** Gizmos and inspect text read state; one service call does the work.
7. **XML first.** Vehicles, parts, stats and loadouts are Defs; C# only for the jobs and comps the Defs cannot express.

## The parts model

**New stats** (`VehicleStatDef`s of ours, `showOnVehicleTypes` Land):

| Stat | Operation | Fed by | Read by |
| --- | --- | --- | --- |
| `Phobos_DrillRate` | Average | Rotary head, drill string, drill bit, flushing pump, compressor | The drilling job's work per tick |
| `Phobos_RigStability` | MinValue | Outriggers, mast | The deploy gizmo: below the floor it refuses and names the part |
| `Phobos_RigControl` | MinValue | Control console, wiring | Deploying and drilling: below the floor, the rig will not run |
| `Phobos_ScanRate` | Average | Sensor boom, geophone array, recorder | The survey job's work per tick |
| `Phobos_GeneratorOutput` | Average | Engine, alternator, regulator | The watts offered to the net while deployed |
| `Phobos_TipRate` | Average | Tipping rams, hydraulic pump | The tipping job's duration |
| `Phobos_PumpRate` | Average | Pump, hoses | Tankers and the fire engine (catalogue) |

**Conventions.** Depth: external for anything a shot meets first (cab, wheels, mast, outriggers, body panels), internal
for what sits inside. Every part has real cells (Combat Extended hits only those). Running gear carries `MoveSpeed`; the
engine carries `MoveSpeed` with `priorityStatEfficiency` so a dead engine stops the vehicle whatever the wheels say.
Reactors: `Reactor_FuelLeak` on fuel tanks, `Reactor_Explosive` on engines as VVE does, and one of ours to write
later, a hydraulic leak that drains the hydraulic quantity of the systems service. Damage states and raised equipment
draw through `graphicOverlays` with a `ComponentRequirement`. Combat Extended armour follows the Harrier part's scale
(millimetres and megapascals, light vehicle). Repair order is list order, so the engine and controls come early.

## The first family, in full (agent default: the extraction chain; owner to confirm)

Four vehicles that share the deploy-and-work model and feed one another: find the deposit, drill it, haul the ore,
power the camp. Cells are the vehicle's own facing north, x across and z along, centred as VF centres them. Health,
cost and mass are authored and untuned.

### Survey truck

A light truck carrying a geophysical sensor boom: the ground-penetrating scanner on wheels. It drives to a site, sets
up, and a colonist runs the survey (Intellectual, as the game's scanner asks), which reuses the scanner's own find
routine against the deep resource grid. Prior art: none.

| Item | Value |
| --- | --- |
| Footprint, drawSize | (2, 4), (3, 5) |
| Mass | 160 |
| Fuel | Chemfuel, 16 a day, capacity 70 |
| Crew | Driver; one surveyor at the console while deployed (a job, not a seat) |
| Research | Ground-penetrating scanner; the game's research that unlocks the building |
| Cost (authored) | 140 steel, 6 components, 1 ground-penetrating scanner's worth of parts (60 steel, 4 components); VVE engine block where VVE is present |
| Tie-ins | Vanilla Helixien Gas Expanded: find gas (open); Dubs Bad Hygiene: find groundwater for a well rig (open) |

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 120 | External | all, weight 3 | BodyIntegrity | Takes the hits that reach nothing else |
| Cab | 50 | External | (0, 2) (1, 2) | | Crew |
| Engine | 50 | Internal, priority | (0, 2) (1, 2) | MoveSpeed | Dead engine stops the truck |
| Fuel tank | 30 | Internal | (0, 0) | | Leak reactor |
| Front axle and wheels | 30 | External | (0, 1) (1, 1) | MoveSpeed | |
| Rear axle and wheels | 30 | External | (0, −1) (1, −1) | MoveSpeed | |
| Outriggers | 40 | External | (0, −1) (1, −1) | Phobos_RigStability | Must be sound to set up |
| Sensor boom | 40 | External | (0, 0) (1, 0) (0, −1) (1, −1) | Phobos_ScanRate | The survey's speed |
| Geophone array | 30 | External | (0, −1) (1, −1) | Phobos_ScanRate | |
| Recorder and console | 30 | Internal | (0, 1) | Phobos_ScanRate, Phobos_RigControl | Below the control floor the survey will not run |
| Wiring | 25 | Internal | (0, 0) | Phobos_RigControl | |
| Hydraulic pump | 30 | Internal | (1, 0) | Phobos_RigStability | Raises the boom and the outriggers |
| Lights | 15 | External | (0, 2) (1, 2) | | An electrical load at night |

The systems loadout adds a radiator, battery, alternator and starter beside the engine.

### Mobile drill rig

A truck-mounted exploration rig: a mast folded along the deck, raised over the rear when set up, outriggers at the
corners. Real rigs of this kind (truck-mounted reverse-circulation, diamond and water-well rigs) are the grounding;
the simplifications are labelled. It drills the deep resource grid under its own footprint at the game's portion
sizes and yields stone chunks where the grid is empty, as the deep drill does. A colonist operates it (Mining, with the
game's mining speed, as the deep drill asks). Prior art: none.

| Item | Value |
| --- | --- |
| Footprint, drawSize | (2, 5), (3, 6) |
| Mass | 420 |
| Fuel | Chemfuel, 30 a day driving, 45 a day drilling (authored; a rig works its engine harder than the road does) |
| Crew | Driver; one driller at the controls while deployed (a job) |
| Research | Deep drilling |
| Cost (authored) | 320 steel, 40 plasteel, 10 components, 1 deep drill's worth of parts (the game's deep drill costs 150 steel and 2 components; the rig's drilling parts are priced above it so a rig never undercuts a built drill); VVE engine block where present |
| Electric variant | Open: VF's electric fuel would let the rig charge on a cell with power; deferred (agent lean: chemfuel first) |
| Tie-ins | Dubs Bad Hygiene: drill a well head in a dry biome (sketched below); Vanilla Helixien Gas Expanded: a gas well |

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 180 | External | all, weight 3 | BodyIntegrity | |
| Cab | 50 | External | (0, 2) (1, 2) | | Crew |
| Engine | 60 | Internal, priority | (0, 2) (1, 2) | MoveSpeed, Phobos_DrillRate | Drives the road and the rig |
| Transmission | 40 | Internal | (0, 1) (1, 1) | MoveSpeed | |
| Front axle and wheels | 35 | External | (0, 2) (1, 2) | MoveSpeed | |
| Rear axles and wheels | 50 | External | (0, −1) (1, −1) (0, −2) (1, −2) | MoveSpeed | Tandem axles |
| Fuel tank | 40 | Internal | (0, 0) | | Leak reactor |
| Hydraulic pump | 35 | Internal | (1, 0) | Phobos_RigStability, Phobos_TipRate | Raises the mast and outriggers |
| Hydraulic lines | 25 | Internal | (0, 0) (0, −1) | Phobos_RigStability | A hydraulic leak reactor later |
| Outriggers | 50 | External | (0, 1) (1, 1) (0, −2) (1, −2) | Phobos_RigStability | Below the floor the rig will not set up |
| Mast | 70 | External | (0, −1) (1, −1) (0, −2) (1, −2) | Phobos_RigStability | Raised when deployed (overlay) |
| Rotary head | 45 | External | (0, −2) (1, −2) | Phobos_DrillRate | Turns the string |
| Drill string | 40 | External | (0, −2) | Phobos_DrillRate | |
| Drill bit | 25 | External | (0, −2) | Phobos_DrillRate | A consumable later: wears with rock drilled (open) |
| Draw-works and winch | 40 | External | (0, −1) (1, −1) | Phobos_DrillRate | Trips the string |
| Flushing pump | 35 | Internal | (1, −1) | Phobos_DrillRate | Clears cuttings; dry drilling is slow |
| Compressor | 35 | Internal | (1, 0) | Phobos_DrillRate | |
| Control console | 35 | Internal | (0, 1) | Phobos_RigControl | Below the floor nothing runs |
| Wiring | 30 | Internal | (1, 1) (1, 0) | Phobos_RigControl | |
| Cuttings chute | 20 | External | (1, −2) | | Takes hits |
| Lights | 15 | External | (0, 2) (1, 2) (0, −2) (1, −2) | | Load at night |

Rules (authored): deploying needs stability and control at or above 0.5; drilling needs control at or above 0.35;
drill work per tick is the game's deep-drill work scaled by `Phobos_DrillRate` and the operator's mining speed; the
rig's deploy and drilling states are motes and overlays, never text alone.

### Tipper hauler

A dump truck: a high `CargoCapacity` body that tips its whole load onto a stockpile zone in one job, so hauling ore
from the rig to the base is a drive, not a hundred carries. Prior art: Forklift Logistics and Caravan Hauler haul;
nothing tips.

| Item | Value |
| --- | --- |
| Footprint, drawSize | (2, 4), (3, 5) |
| Mass | 300 |
| Fuel | Chemfuel, 26 a day, capacity 100 |
| Crew | Driver |
| Research | Vehicles' own tier, or VVE's hauler research where present |
| Cost (authored) | 260 steel, 8 components; VVE engine block where present |

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 150 | External | all, weight 3 | BodyIntegrity | |
| Cab | 50 | External | (0, 2) (1, 2) | | |
| Engine | 55 | Internal, priority | (0, 2) (1, 2) | MoveSpeed | |
| Transmission | 40 | Internal | (0, 1) (1, 1) | MoveSpeed | |
| Front axle and wheels | 35 | External | (0, 2) (1, 2) | MoveSpeed | |
| Rear axles and wheels | 50 | External | (0, −1) (1, −1) | MoveSpeed | |
| Fuel tank | 35 | Internal | (0, 1) | | Leak reactor |
| Tipper body | 80 | External | (0, 0) (1, 0) (0, −1) (1, −1) | CargoCapacity | Holed body spills capacity |
| Tipping rams | 40 | Internal | (0, 0) (1, 0) | Phobos_TipRate | Slow rams, slow tip |
| Hydraulic pump | 35 | Internal | (1, 1) | Phobos_TipRate | |
| Tailgate | 30 | External | (0, −1) (1, −1) | CargoCapacity | |
| Wiring | 25 | Internal | (0, 1) | | |
| Lights | 15 | External | (0, 2) (1, 2) | | |

### Mobile generator set

A towed-size engine and alternator on wheels. Parked and set up, it feeds the power net through its own cells (the
route, a power comp that transmits while deployed, is verified at build). `Phobos_GeneratorOutput` scales the watts.
Prior art: RV with built-in PD (power from vehicle fuel, 1.6).

| Item | Value |
| --- | --- |
| Footprint, drawSize | (2, 3), (3, 4) |
| Mass | 220 |
| Fuel | Chemfuel, 12 a day driving, 24 a day generating at full output (authored; the game's chemfuel generator burns 4.5 a day for 1,000 W, and the set's output is priced against it so fuel per watt is no better) |
| Output | 1,200 W at full health (authored) |
| Crew | Driver only; no one tends it while it runs |
| Research | Electricity; chemfuel power |
| Cost (authored) | 200 steel, 6 components, a chemfuel generator's worth of parts (100 steel, 3 components) |

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 110 | External | all, weight 3 | BodyIntegrity | |
| Cab | 40 | External | (0, 1) (1, 1) | | |
| Engine | 55 | Internal, priority | (0, 1) (1, 1) | MoveSpeed, Phobos_GeneratorOutput | One engine drives both |
| Alternator | 40 | Internal | (0, 0) (1, 0) | Phobos_GeneratorOutput | |
| Voltage regulator | 25 | Internal | (1, 0) | Phobos_GeneratorOutput | |
| Fuel tank | 35 | Internal | (0, −1) | | Leak reactor |
| Axle and wheels | 35 | External | (0, 0) (1, 0) | MoveSpeed | |
| Outriggers | 30 | External | (0, −1) (1, −1) | Phobos_RigStability | Set up before it runs |
| Radiator | 30 | External | (0, 1) (1, 1) | | The systems service's cooling |
| Control panel | 25 | Internal | (0, −1) | Phobos_RigControl | |
| Wiring | 25 | Internal | (1, −1) | Phobos_RigControl | |

### What each one needs in art

Per vehicle: north, east and south bodies at 128 px per drawSize unit, a colour mask for each, overlays for the
deployed state (mast raised, outriggers down, boom up, tipper raised) and for damage, and a 64 px icon. The route is
in [art services](vehicle-art-services.md).

## The catalogue, sketched

One line per idea. "Reuses" names the game or mod system the vehicle drives; "Prior art" names Workshop mods that
already do it (IDs in the mechanics record); the lean is the agent's.

| Family | Vehicle | What it does | Reuses | Job stat and key parts | Tie-ins | Prior art | Lean |
| --- | --- | --- | --- | --- | --- | --- | --- |
| Extraction | Quarry excavator | Mines mountain and rock faces from a cab, faster than a pick | The mining job and skill | Phobos_DigRate: boom, bucket, slew ring, hydraulics | | none | High: nobody has it |
| Extraction | Water-well rig | Drills a well head in a dry biome | Dubs Bad Hygiene wells | Phobos_DrillRate | DBH | none | Medium: needs DBH |
| Extraction | Gas-well rig | Taps helixien gas where the survey finds it | Vanilla Helixien Gas Expanded | Phobos_DrillRate | VHGE | none | Low until VHGE is on the owner's list |
| Haulage | Flatbed | Carries bulky things and a disabled vehicle | CargoCapacity | Deck, winch | Recovery | Caravan Hauler, Forklift Logistics | Medium |
| Haulage | Tanker | Carries chemfuel or water; refuels vehicles, fills DBH storage | VF fuel comps, DBH plumbing | Phobos_PumpRate: pump, hoses, tank | DBH, the fire engine | Trains of the Rim (tanker cars) | High |
| Haulage | Refrigerated van | Keeps food cold on the road | VVE's refrigerator comp | | | VVE | Skip: exists |
| Construction | Bulldozer | Smooths and clears terrain, pushes rubble | The game's smoothing and clearing jobs | Phobos_BladeRate: blade, tracks, hydraulics | Rails and Roads of the Rim | none | High |
| Construction | Road paver | Lays road tiles as it drives | Rails and Roads of the Rim | Phobos_PaveRate | RRotR | none | Medium |
| Construction | Crane | Lifts and places heavy things, gravship parts | The hauling job; VGE parts | Phobos_LiftRate: boom, winch, outriggers | VGE | an R3 Capraia add-on mentions a crane | Medium |
| Agriculture | Tractor with implements | Ploughs, sows, harvests into cargo | The growing jobs | Phobos_FieldRate: PTO, implement | | Tractor: Industrial Age (1.6) | Low unless that mod stays on 1.6 |
| Agriculture | Forwarder | Hauls logs from the felling site | The hauling job | CargoCapacity, crane | | none | Medium |
| Power | Generator set | In the family | | | | RV with built-in PD | In the family |
| Power | Battery bank | A trailer of batteries charged at base and drained at camp | The game's battery comp | Capacity: cells, bus bars | The systems service | none | Medium |
| Power | Floodlight tower | Lights a camp at night | The game's glow comp | Phobos_GeneratorOutput | | none | Low |
| Emergency | Ambulance | Carries patients, a bed role, the fluids kit aboard | D'thorga Medical's equipment | Phobos_MedicalBay: bed, oxygen, lights | Medical | HE-Ford ambulances | Medium: ours would run the Medical part's kit |
| Emergency | Fire engine | Pumps water onto fires | Burns and Smoke, DBH water | Phobos_PumpRate: pump, hoses, tank, monitor | Medical, DBH | HE-Pierce Arrow Pumper | Medium |
| Emergency | Recovery truck | Tows a disabled vehicle home | The systems service's jump-start; VF's move hooks | Phobos_LiftRate: winch, boom | Search and Rescue | none | High |
| Communications | Radio van | A vehicle-mounted station and relay on vehicle power | The radio framework's listed tie-in | A load on the electrical system; mast, set | Radio | none | High, once the radio framework reaches Phase 2 |
| Survey | Survey truck | In the family | | | | none | In the family |
| Maintenance | Repair bay | A parked workshop that raises RepairRate for vehicles alongside | VF's RepairRate stat | Phobos_ServiceRate: hoist, tools | The systems service's service job | none | Medium: VF has no workbench, so this is the honest form |

## Vehicle Framework 1.7 gate and the build plan

- **1.7 only.** One build target; no `1.6` folder; `supportedVersions` 1.7.
- Defs follow the schema VF's 1.6 build reads, which is what the 1.7 game loads today; C# compiles against the installed
  `Vehicles.dll` through `Directory.Build.targets`, as the Harrier part does. When VF 1.7 ships: re-read the part
  fields, the `VehicleComp` hooks, the overlay API and the settings attributes against the new build, rebuild and
  re-deploy.
- **Order** (owner: framework first): the vehicle systems service and the stand-in mod's loadouts for VVE, the Harrier
  and the HUMVEE, proven on vehicles the owner already drives; then this family, one vehicle at a time: Defs and stats,
  the deploy comp and the jobs, the floors and refusals, art, README and changelog, and the owner's short test
  protocol (a drive, a set-up, a refusal, a repair, a save and reload).

## Art plan (agent recommendation, owner to confirm; the research is in [art services](vehicle-art-services.md))

- **Per vehicle:** three bodies, three masks, two or more overlays, one icon; 128 px per drawSize unit.
- **Pipeline:** the north body from a pay-per-image API with a vanilla-look prompt (thick near-black outline, soft cel
  shading, pale panels, rounded corners, straight top-down) and style references; downscaled by script. East and south
  by **script rotation of the north** (a true top-down east is north turned a quarter), then a reference-guided edit
  that fixes lighting and end details, so all three facings share one design. Overlays as edits of the body, cut out by
  script. Masks by script posterisation of the panel zones, with a model pass only where the script cannot separate
  them. The icon by script.
- **Services (owner's pick, 9 Nov 2026: the OpenAI image API for the bodies, the owner's local generation for icons):** the
  research put the Gemini image API (Nano Banana 2.1, about US$0.034 an image) first and the OpenAI image API (gpt-image-2
  medium, about US$0.041) second; the owner chose OpenAI, which they know, and which their own sources rate best for RimWorld's
  look; FLUX.2 edits on fal.ai as the backup for the turned-image edits. All
  pay-per-image with no subscription; the whole family costs a few dollars. **PixelLab is not for the bodies**: its
  rotation tools stop at 128 to 256 px and are trained on characters; the 19 Oct 2026 reasoning in
  [artwork direction](artwork-direction.md) stands. It could draw 64 px icons if the owner wants it used.
- **Who does what:** the agent drives the API through the `openai` engine of `scripts/art-run.py` (built 8 Nov 2026,
  [how to run it](vehicle-art-services.md#running-the-openai-engine)) with the owner's key in the `OPENAI_API_KEY`
  environment variable, never in a file; the owner buys the credit, sets the account's spending limit and makes every pick. Every image goes through the near-copy check
  and gets a record (tool, model, prompt, date, SHA-256, AI disclosure) with the service's output terms as read on the
  day; masters go to the `art-masters` branch. The local Klein route stays a free baseline for icons and items.

## Who chose what

- **Owner (8 Nov 2026):** utilitarian vehicles with per-part health and efficiency; a mobile drill rig first; a new
  mod; the framework before the vehicles, the mod as a stand-in; three to four vehicles in full; 1.7 only; the art
  route open to suggestions.
- **Agent defaults, owner to confirm:** the extraction chain as the family; chemfuel rigs; drilling on the game's deep
  resource grid at its rates; the stats table; every health, cost and mass figure; the catalogue's leans; the art
  pipeline and services.

## What was argued against, and why

- **Our own deposit model** (richer ore bodies the rig alone could reach): a second resource grid beside the game's,
  and a value loop waiting to happen. The game's grid, found by the game's scanner, keeps the rig honest.
- **A worker seat role** for the driller: VF's roles handle movement and turrets only; a job at the vehicle uses the
  game's skills and work priorities, as the deep drill does.
- **Borrowing the turret comp's deploy state**: it belongs to turrets; a comp of ours on VF's move and draft hooks is
  smaller and ours to change.
- **Electric-only rigs**: a truck rig on a diesel-hydraulic power pack is the real machine; the electric variant stays
  an option.

## Open questions for the owner

| Question | Agent lean |
| --- | --- |
| The family: survey truck, drill rig, tipper hauler, generator set? | Yes; the tanker is the first addition |
| Chemfuel rig, or an electric variant alongside? | Chemfuel first |
| Drill-bit wear as a consumable? | Later, with the systems service's wear phase |
| Wheels or tracks on the rig? | Wheels (a truck rig); tracks as a variant |
| Which art service? | **The OpenAI image API for vehicle bodies; local generation for icons** (owner, 9 Nov 2026); no subscription |
| Any C# before VF 1.7 ships? | Yes, against the installed build the game already loads; expect a rebuild |
| The mod's display name | D'thorga Vehicles |

## Sources

- Vehicle Framework and Vanilla Vehicles Expanded code and XML: [Vehicle Framework mechanics](vehicle-framework-mechanics.md).
- The game's deep drill, ground-penetrating scanner and chemfuel generator figures: RimWorld 1.7 Defs as installed
  (deep drill 150 steel and 2 components; chemfuel generator 4.5 fuel a day for 1,000 W), to be re-read at build.
- Prior-art mods: the community collections of VF mods named in the mechanics record, read 8 Nov 2026.
- Real rigs as grounding only (no figure is taken from them): truck-mounted reverse-circulation, diamond and water-well
  rigs as built by the mining-equipment makers; unverified, named for orientation.
