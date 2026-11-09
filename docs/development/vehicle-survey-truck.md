# The survey truck: design (9 Nov 2026)

**Built in D'thorga Framework 0.46.0 and D'thorga Vehicles 0.18.0. It borrows the bulk tanker's picture until it has its own (owner, 9 Nov 2026: no stand-in vehicle pictures), so no upgrade or set-up look shows yet. Not yet tested in game.** What was built differs from what follows:
the Dubs Bad Hygiene `Hydrogeology` node and the Rimefeller deep-well postfix came in Framework 0.48.0, the sensor showing the water while the truck is set up and selected, where the table says for a day after a survey; a plugged-in truck is a real load on the grid through a small power point of ours (700 W, or 1,000 W with the array on), priced
in the game's watts so that one generator set can run it, where the table below says 3,600 W; and while the array is recharging the battery the survey stops, which is what gives the record's 1.5 times. The agent took its
leans on the open questions below: a new medium drivetrain, 3 times and 150 A, a 600 Ah battery, 30% and 80% marks, Fabrication plus long-range research for the array.

**Size, 9 Nov 2026 (owner, before the art):** 3 x 7 cells, drawn 4 x 8, instead of the (2, 7) and (3, 8) below. At 2 wide it was 3.5 times as long as wide, nearly the proportions the owner found
tiny on the tanker, and narrower than the Humvee (3 x 6) on the owner's list. The part cells now run x -1 to 1; the keys and their order are unchanged. Vehicles 0.22.1.

**As rebalanced, 9 Nov 2026 (Framework 0.49.1, owner's rule: an order of magnitude over what a vehicle copies unless it feels like cheating):** the deep
find is MTB 0.3 day (about 7 hours) and certain at 0.6, the long-range find 0.4 and 0.8; the curve and the powered array's 3x are unchanged (owner's choices).
The minting arithmetic below at the new pace: finds 1 to 6 on one map total about 3.4 days instead of 34 (0.3 + 0.45 + 0.68 + 1.0 + 1.5 + 2.4), with the array a
third of that, and the sixth find still takes 2.4 days alone, so a home map is worked out in a week and the real speed stays in the long-range mode. The game's
own scanner is left as it is (owner): it is the slow option, and the reason to build the truck. **The tables below keep the original figures as the design history.**

**The design as written, agent-authored for the owner to confirm;** the owner's decisions of 9 Nov
2026 are binding and listed under [Who chose what](#who-chose-what). It replaces the "Survey truck" sketch in
[utility vehicles](utility-vehicles-design.md) and follows the shape of that record's bulk tanker. Every figure is **authored** unless
marked **from the code** (with its file and line) or **secondary** (a published source, not verified here).

## Read this first

- **What it is:** a light 4x4 truck carrying a geophysical sensor boom and recorder: the game's ground-penetrating scanner on wheels,
  with the long-range scanner's job added by an upgrade. A colonist works the survey with Intellectual skill (the game's `ResearchSpeed`).
- **Owner must decide (1):** the drivetrain. The agent leans to a new `PhobosDrivetrain_MediumDiesel` (Unimog class); the existing
  `PhobosDrivetrain_HeavyDiesel` matches the power-to-weight but has a highway truck's wheels and drag.
- **Decided (2): the diminishing-returns curve (owner, 9 Nov 2026).** Each find on a map multiplies the time to the next by 1.5, capped at 8x,
  to be tuned in play. A gentler curve mints ore faster; a harsher one makes the base mode pointless after three finds.
- **Owner must decide (3):** the fast mode's figures: a 150 A array and a 600 Ah battery give about 1.5x the standard speed in the field
  and 3x on shore power. Raising the battery raises the field speed toward the shore figure.
- **The one real risk is minting deep ore.** From the code, deep ore does not exist until a scanner finds it (all.cs:567040); a mobile,
  faster scanner is a mobile mint. The curve above and the no-value-loop arithmetic are the answer; **the owner should read that section.**
- **Depends on** [the generator set](vehicle-generator-set.md) for camp power: any vehicle of ours with electric loads draws from the
  generator set's net when parked beside it, through the existing shore-power check. That record owns the rule; this one has one row for it.
- **Two Harmony postfixes** are needed, both small: the deep-ore overlay check (so a deployed truck shows the ore it found) and Rimefeller's
  deep-well placement check. Nothing else of the game's scanner can be reused as a comp; our comp mirrors its routine.


### Overdrive (owner, 9 Nov 2026; built in Framework 0.50.0)

The vehicle's answer to Vanilla Expanded's overclock, whose price is a tripled breakdown chance (`VEF_BuildingBreakdownFactor` 3 on the automated drill
platform; its slider runs 0.1 to 2 times, scaled by `VEF_BuildingMaxOverclockFactor`; its power draw rises with the square of the multiplier; from the
PipeSystem 1.7 and VEF 1.7 decompiles, read 9 Nov 2026). Ours is a toggle with its own icon. **Authored:** 2x the work rate, the engine flat out (full-load
heat and oil), fuel 2.5x, the working parts (those feeding the work stat) losing 2 health an hour while it works, the power point drawing 2x on a grid, and it
cuts itself out at half health on any working part or a coolant temperature of 120 (the cooling system's overheating stage), saying which.

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| Overdrive | Finish a lump or a survey twice as fast when the camp is exposed | Run it as the default: the working parts wear and their repair costs our part items; a worn or overheating vehicle refuses it | Overdrive is a decision about spares and fuel, not a free setting |

## What it is and what it does in play

The truck drives to a site, puts its outriggers down and raises its boom, and a colonist takes the console. It cannot move or be drafted
while set up: the same deploy comp on Vehicle Framework's move and draft hooks that the bulk tanker uses. While the colonist works, the
truck is doing what the game's ground-penetrating scanner does: after some days, a lump of deep ore appears under the map it stands on,
and the deep-ore overlay shows it. With the long-range node fitted, the console can instead be tuned to one ore and search the world
around it; a find there starts the game's own mineral-lump quest on a nearby tile, which is where the drill rig and the tipper then go.
Every later find on the same map takes longer than the last. Standard surveying runs off the truck's alternator with the engine idling,
or off the camp's power when parked beside the generator set. The fast mode is an overhaul: a power-hungry array that runs flat out on
shore power, or on a large battery until that is spent; with the battery fitted the engine starts itself to recharge when the battery
runs low, burning fuel unattended.

**Grounding (secondary, labelled):** truck-mounted seismic recording units and wireline logging trucks are real machines; a logging truck
carries a recorder cabin, a winch and sealed radioisotope sources for density and porosity logging, and gamma-ray spectrometry maps
potassium, uranium and thorium. That is why uranium is in the cost. The game's scanner is a single abstract instrument, and so is ours:
the two sensor nodes are flavour that moves figures, not a model of seismic or nuclear logging.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Deploy** (outriggers down, boom up) | Survey anywhere the truck can drive, including a camp with no buildings | Move or draft the truck while set up; set up with broken outriggers ("Cannot set up: the outriggers are too damaged") | A survey is a stop on a journey, not a building at home |
| **The survey job** (base) | Have one colonist find deep ore under any map, at the game's own speed | Survey without a colonist; survey with the engine off and no power (the console stops at a low battery so the truck can still start) | Intellectual colonists get field work; the survey needs fuel, power or a camp |
| **The find** (a lump appears, the overlay) | See the lump on the deep-ore overlay while the truck stands deployed | See it after driving off, unless a scanner building is on the map (the game's rule) | Mark the site, then bring the drill rig: the truck is the first vehicle of the chain |
| **Diminishing returns** | Keep surveying the same map, more slowly each time | Mint a map's worth of ore by parking the truck at home | After two or three finds a map is "worked out"; the player moves on |
| **The long-range mode** | Tune to one ore and find a lump on another world tile (the game's quest) | Anything on the current map while tuned long-range | The find costs travel: a caravan or the vehicle family goes out to drill it |
| **The powered array on shore power** | Survey at 3x speed beside a powered net (the generator set or a base grid) | Run the fast mode unplugged without the battery node | The fast survey is a camp operation: generator set, survey truck, drill rig together |
| **The powered array on the battery** | Run the fast mode in the field for about 4 hours, then wait for the recharge | Run it continuously: at 30% the array stops ("Battery low: the array has stopped") | About 1.5x standard speed in the field for fuel; the truck is semi-independent of the generator set |
| **Auto-idle** | Leave the deployed truck to recharge itself (the inspect line and Dashboard say "engine idling to recharge") | Nothing: it is automatic with the battery node. A dry tank is the risk | Fuel planning: a long field survey far from home can leave the truck stranded |
| **The two sensor nodes** | Favour heavy lumps (steel, deepchem) or favour uranium | Fit both at once | The player tunes the truck to what the colony is short of; the probe costs uranium up front |
| **The hydrogeology node** (Dubs Bad Hygiene) | See the groundwater grid for a time and have the richest well cell nearby named | Make water: wells read a fixed grid and are never drilled | A dry-biome camp is sited where a well will give the most |
| **Finding oil** (Rimefeller) | Place a deep oil well in the field, beside the deployed truck | Place one without the truck or a scanner building | Oil prospecting leaves the base |
| **Plug into the generator set** | Survey with the engine off, on the camp's power, in any mode | Nothing | The camp has a reason to exist: the set powers the truck and the rig |
| **Work lights** | Survey through the night (a toggle, an electrical load) | Nothing | The truck is lit at night; the load is a few amps on the alternator or battery |

## The honest mechanics

### What the game does

- Both scanners derive from `CompScanner`; they run only while spawned, powered, unroofed, unforbidden and player-owned. The odds of a find
  are a mean-time-between-finds roll every 59 ticks scaled by the operator's `scanSpeedStat`, plus a guaranteed find after
  `scanFindGuaranteedDays` of work (from the code, all.cs:566884-566950).
- The ground-penetrating scanner (`CompDeepScanner`) needs a biome with bedrock. On a find it picks a random non-edge cell, an ore
  weighted by `deepCommonality`, and writes an irregular lump of `deepLumpSizeRange` cells, each holding `deepCountPerCell`, into the
  `DeepResourceGrid` (from the code, all.cs:566993, 567027-567069). **Deep ore is created at the find, not at map generation**: only the
  scanner's find (all.cs:567040) and the drill's take (all.cs:534070) write the grid, which stores an ore and 0-65535 per cell
  (all.cs:70394-70455).
- The deep-ore overlay shows only when a colony building has a powered `CompDeepScanner` (from the code, all.cs:70496-70507, 567018). A
  vehicle does not count.
- The long-range scanner is tunable to one ore (default gold) from the PreciousLump list; a find starts the `LongRangeMineralScannerLump`
  quest, which picks a nearby site tile; it reveals nothing per tile (from the code, all.cs:567100-567161; `Script_LongRangeMineralScannerLump.xml`).
- Defs (query-defs, 9 Nov 2026): the ground-penetrating scanner draws 700 W, `scanFindMtbDays` 3, guaranteed 6 days, `ResearchSpeed`; the
  long-range scanner 700 W, 4 days, guaranteed 8.
- Deep ore, as commonality / per drill portion / lump cells (from the code, all.cs:45088-45094; `deepCountPerCell` default 300): steel
  4/45/20-30, plasteel 1/10/2-10, uranium 1/15/4-10, silver 0.5/70/2-10, gold 0.5/8/1-4, jade 0.5/15/1-4. Vanilla Chemfuel Expanded's
  `VCHE_Deepchem` is a deep resource at 2/35/20-40 (query-defs), so the survey finds it.
- World tiles hold elevation, hilliness, temperature, rainfall, swampiness, feature, pollution and mutators (all.cs:776667-776759); the
  `MineralRich` mutator picks one ore per tile (all.cs:419335-419430); VEF's `TileMutatorExtension.deepOresMultiplier` scales
  `deepCountPerCell` on finds (`.local/decompiled/VEF-1.7/VEF.Maps/TileMutatorExtension.cs:42`). **A find on a mineral-rich tile is bigger
  by the game's own rule; the truck adds nothing to it.**

### What we reuse and what we must add

| Piece | Reuse or add | Why |
| --- | --- | --- |
| The find routine | **Add: a mirror comp** on the vehicle that runs the same roll (59-tick MTB roll scaled by the operator's `ResearchSpeed`, guaranteed days) and writes the lump through the same grid calls | The scanners' code casts their owner to a colony building (all.cs:566884-566950), so a vehicle cannot carry `CompScanner` as is |
| The lump | Reuse: the game's `DeepResourceGrid`, ore weights, lump sizes and `deepCountPerCell` | Vanilla precedence: the truck never finds more per lump than a built scanner |
| The deep-ore overlay | **Add: one Harmony postfix** on the overlay check (all.cs:70496-70507) that also returns true while one of our survey trucks is deployed on the map | Without it the player cannot see what the truck found; a postfix, never a prefix |
| The long-range find | Reuse: start the `LongRangeMineralScannerLump` quest as the game does (all.cs:567100-567161), tunable from the same list | The quest already picks the site and makes the letter |
| Rimefeller's deep well | **Add: one Harmony postfix** on `PlaceWorker_DeepWellHead.HasScanner` (`.local/decompiled/Rimefeller/Rimefeller/PlaceWorker_DeepWellHead.cs`), true while a truck is deployed; in Rimefeller's `IfModActive` folder and assembly | Oil lives in pre-seeded `OilGrid`s (`MapComponent_Rimefeller.cs:52-58, 373-395`); the truck only unlocks placing the well in the field |
| Deploy | Reuse: the tanker's deploy comp on Vehicle Framework's move and draft hooks | One comp for the family |
| Power | Reuse: the electrical system's loads, battery charge, alternator and shore-power check; **nothing new** | Owner, 9 Nov 2026 |

The per-map find count is saved in a `MapComponent` under a stable Scribe key; it counts only our truck's finds. The game's own scanner
buildings are not counted and not slowed: the game already lets a player build several, and the truck's rule is only that it never
becomes a cheaper second scanner.

### Diminishing returns

Each find by a survey truck on a map multiplies that map's time-to-find for the next survey. **Authored curve,** starting from the game's
figures (3 days MTB, 6 guaranteed, from the Def); the long-range mode is not on the curve, because travel is its cost.

| Find on this map | MTB multiplier | Standard MTB (days) | Guaranteed (days) | Fast mode MTB (days) |
| --- | --- | --- | --- | --- |
| 1st | 1x | 3 | 6 | 1 |
| 2nd | 1.5x | 4.5 | 9 | 1.5 |
| 3rd | 2.25x | 6.75 | 13.5 | 2.25 |
| 4th | 3.4x | 10.1 | 20 | 3.4 |
| 5th | 5.1x | 15.2 | 30 | 5.1 |
| 6th and after | 8x (cap) | 24 | 48 | 8 |

**The minting risk, plainly.** A find writes 4 to 30 cells (40 for deepchem) at 300 each into a grid that held nothing: a steel find is
6,000 to 9,000 steel that did not exist. The game accepts this at one find per 3 days per 700 W scanner. The truck at 3x speed with no
curve would be a 1-day mint anywhere; with the curve, the first three finds on a map take 12.25 standard days together and the fourth
alone takes ten, so a home map is worked out in a season and the real speed sits in the long-range mode, which costs a journey. **If
the owner wants the base mode weaker still, raise the multiplier to 2x; if the curve feels punitive in play, cap at 4x.**

### Power and the battery

All figures authored; the electrical model is the one in `Systems_Electrical.xml` (charge is a fraction of `batteryAmpHours`; the
alternator gives nothing below 1,200 rotor rpm, half at 1,800, 70% at 2,400). The truck is a 24 V vehicle with the heavy set's electrics:
**100 Ah starting battery, 100 A alternator** (as `PhobosVehicleLoadout_LandHeavy` sets them, `Loadouts_Default.xml:352-353`).

| Mode | Load | Supply | Arithmetic | Result |
| --- | --- | --- | --- | --- |
| Standard, engine idling | Console and sensors 30 A (24 V x 30 A = 720 W, matching the game's 700 W scanner) | Alternator at idle, about 50 A (700 rpm x 2.5 = 1,750 rotor rpm, about half output) | 50 - 30 = 20 A spare | Runs indefinitely on fuel: 8 chemfuel a day at idle |
| Standard, engine off, no battery node | 30 A | Starting battery 100 Ah | 100 Ah x 0.7 (full to the 30% low stage) / 30 A = 2.3 h | **The console stops at 30%** so the truck can still start; the game's flat-battery refusal is never reached by surveying |
| Standard, on `SurveyBattery` | 30 A | 600 Ah | 600 x 0.7 / 30 = 14 h | A quiet night's survey without fuel |
| Fast, shore power | Array 150 A (3,600 W) | The net beside the truck | 3,600 W is drawn from the net as a load (owned by the generator set record) | 3x standard speed, no fuel |
| Fast, on `SurveyBattery`, engine off | 150 A | 600 Ah | 600 x 0.7 / 150 = 2.8 h from full; 600 x 0.5 / 150 = 2 h from the 80% recharge level | Stops at 30% |
| Fast, on `SurveyBattery`, auto-idle running | 150 A | Alternator at high idle (1,200 rpm, about 75 A) plus the battery | Net drain 150 - 75 = 75 A; 600 x 0.5 / 75 = 4 h | **4 hours fast, then the array stops and the engine recharges** |
| Auto-idle recharge | None (the array is off) | 75 A at high idle | 600 x 0.5 / 75 = 4 h from 30% to 80% (the charging taper starts at 80%) | 8 chemfuel a day at high idle: about 1.3 chemfuel per cycle |

So in the field the fast mode runs 4 hours in every 8: twelve fast hours a day, equal to 36 standard hours, **about 1.5x the standard
speed for 8 chemfuel a day**, against 3x on shore power. Without the battery node the fast mode refuses unplugged ("Nothing to power the
array: plug in, or fit the large battery"). Auto-idle starts the engine when charge falls to the existing 30% low stage and stops it at
80%; the inspect line says "engine idling to recharge the battery" and the Dashboard shows the charge as it does today. Fuel is the risk:
a 120-unit tank is fifteen days of standard surveying, or about 90 auto-idle cycles, so a truck will not run dry from one survey, but a
truck left deployed through a season with a leaking tank will. **Open at build:** whether the existing electrical balance treats shore
power as supply for the loads (the family rule wants it to) or only trickle-charges at `shoreChargeAmps` 10 with the engine off
(`Systems_Electrical.xml:36, 230-251`); and how a node raises `batteryAmpHours` (agent lean: a `Phobos_BatteryCapacity` stat set by the
node, read by the electrical system where present).

### Sources and simplifications

The game's figures are from the code and Defs as cited. The 1.5x curve, the 150 A array, the 600 Ah battery, the 30 A console, idle and
high-idle currents and fuel, the 3x fast speed, the sensor-node weights and every cost are authored. The alternator curve and the 24 V
convention are the electrical system's own (secondary, in its Def). The uranium in the cost stands in for sealed caesium-137 and
americium-beryllium or californium-252 sources and for gamma-ray spectrometry of potassium, uranium and thorium (secondary, below); the
game has no caesium, so uranium is the honest item. The truck's survey is one abstract instrument, as the game's is: it does not model
seismic reflection, depth of investigation or borehole logging. Deep ore is the game's grid; the truck never writes a second one.

### Argued against or later

| Idea | Verdict | Why |
| --- | --- | --- |
| Solar panels on the body | **Not chosen** (owner, 9 Nov 2026); later if asked | A panel's watts against a 3,600 W array would be decoration; the generator set is the camp's power |
| Unattended (no-colonist) operation | **Not chosen** (owner, 9 Nov 2026) | The game's scanners need an operator's `ResearchSpeed`; a crewless survey would be a free scanner on wheels |
| Reusing `CompScanner` on the vehicle | Rejected | Its code casts the owner to a colony building (all.cs:566884-566950); a mirror comp is smaller than patching the cast |
| A second resource grid of ours (groundwater, gas) | Rejected | Dubs Bad Hygiene seeds its own grids from the world seed and tile (`MapComponent_Hygiene.cs:89-93, 381-390, 661-789`); Helixien gas is switched off on the owner's list; a grid of ours would be a second truth |
| Helixien gas hooks | **Out** (owner, 9 Nov 2026) | The mod is subscribed but off |
| Duplicating Local Mineral Scanner (Workshop 3798782281) | Avoided | That mod is a pawn-operated 2x2 building that reveals undiscovered deposits on the current map, tunable to one mineral, two pawns scan faster. Ours adds mobility, the long-range quest, the powered modes, the family and the curve; it must not be a cheaper copy of a building the owner already runs |
| The long-range mode on the curve | Rejected | Travel is its cost; slowing it would make the family pointless |

## The truck

| Item | Value |
| --- | --- |
| Footprint, drawSize | **(2, 7), (3, 8)**. Two wide: a Unimog-class truck is a light vehicle, two thirds the width of the tanker's PLS-class 3x10 (that scale was set after the owner's first look, 9 Nov 2026); seven long because the boom folds along the deck and overhangs the rear, where VVE's pickups and the old 2x4 sketch stop at the tray. Agent's reasoning |
| Empty mass and power | About 4,500 kg and 130 kW: a Unimog U4000-class 4x4 with a survey body (secondary, approximate; the owner's figure). Vehicle Framework Mass 375 (kerb mass is Mass x 12 kg, the systems service's authored rule) |
| Drivetrain Def | **Agent lean: a new `PhobosDrivetrain_MediumDiesel`**: 130 kW at 2,200 rpm (secondary, Unimog OM 904 LA class), idle 700, 4,500 kg, four-wheel drive (`drivenShare` 1), wheel radius 0.55 m (365/80 R20, our arithmetic), final drive 6.0 for portal axles, gears and drag area 3.5 authored. Alternative: `PhobosDrivetrain_HeavyDiesel`, whose 30 kW/t matches but whose 0.38 m wheels and highway drag do not |
| Fuel | Chemfuel. Driving 20 a day; deployed in standard mode 8 a day (engine idling for the alternator); auto-idle 8 a day while charging, nothing between cycles; capacity 120. On shore power or the battery, nothing |
| Crew | Driver; one surveyor at the console while deployed (a job, not a seat; Intellectual, `ResearchSpeed`). Nobody is needed for auto-idle |
| Research | The game's ground-penetrating scanner research. The long-range node needs the long-range mineral scanner research; the powered array needs Fabrication as well (agent lean) |
| Cost (authored) | 1 `PhobosVehicles_EngineAssembly`, 1 `PhobosVehicles_Transmission`, 1 `PhobosVehicles_DrivetrainKit`, 1 `PhobosVehicles_ElectricalKit`, 220 steel, 8 components, 10 plasteel, **10 uranium** (the recorder's calibration and density source). Substitutes accepted where present: RimThunder's **medium** diesel engine (4,500 kg sits in its 4-15 t medium class) for the engine assembly; Vanilla Vehicles Expanded's car parts are **not** accepted (the truck is under its 6 t limit by mass, but a car transmission and wheels do not fit a portal-axle 4x4; agent lean, owner to confirm). The part items set what a repair costs (owner, 9 Nov 2026) |
| Systems loadout | Electrical (24 V: 100 Ah, 100 A alternator, 0.3 Ah starter), Cooling, Lubrication, Fuel, Service, Running gear, Brakes, Transmission, Cabin. Loads: headlights 6 A (the default); **console and sensors 30 A** while deployed and surveying; **survey array 150 A** (toggle; available with `PoweredArray`); **work lights 6 A** (toggle, at night while deployed; available with `WorkLights`) |
| Tie-ins by `IfModActive` folder | Dubs Bad Hygiene: the `Hydrogeology` node. Rimefeller: the deep-well postfix. Vanilla Chemfuel Expanded: nothing to add, deepchem is already in the game's deep list. Each folder loads only when its mod is active; a player with none sees a complete truck |
| Prior art | Local Mineral Scanner (Workshop 3798782281), a building, on the owner's list; nothing on Vehicle Framework surveys |

## Upgrade tree

Vehicle Framework nodes (`UpgradeNode`: `work`, `ingredients`, `researchPrerequisites`, `prerequisiteNodes`, `disableIfUpgradeNodesEnabled`,
`graphicOverlays`, `refundFraction` 0.5; from the code, `Vehicles.decompiled.cs:34815-34866`). `StatUpgrade` sets or adds stats
(`:33992-34218`); `CompUpgrade` activates a comp, creating it if missing (`:33709-33785`). **No node changes the parts list** (principle 8).
Every cost is authored.

| Node key | Figure it moves | Cost | Research | Prerequisite or exclusive with | Look |
| --- | --- | --- | --- | --- | --- |
| `GeophoneArray` | Deep find weights: steel and deepchem x1.5 (the heavy lumps); `Phobos_ScanRate` +0.1 | 60 steel, 3 components | Ground-penetrating scanner | Exclusive with `RadiometricProbe` | A line of geophone spikes along the boom |
| `RadiometricProbe` | Deep find weights: uranium x3; `Phobos_ScanRate` +0.1 | 40 steel, 30 plasteel, 3 components, **40 uranium** | Ground-penetrating scanner | Exclusive with `GeophoneArray` | A shielded probe housing with a hazard band at the boom's end |
| `LongRangeArray` | Adds the long-range job (`CompUpgrade`); tunable ore; 4 days MTB, 8 guaranteed, off the curve | 120 steel, 6 components, 20 plasteel | Long-range mineral scanner | None | A dish on a mast above the recorder cabin |
| `PoweredArray` | Adds the fast mode (`CompUpgrade`): 3x survey speed while the 150 A array is powered | 200 steel, 40 plasteel, 10 components | Fabrication and long-range mineral scanner | None (shore power runs it; `SurveyBattery` runs it unplugged) | A big transducer array and a cable tray along the deck |
| `SurveyBattery` | Battery capacity 100 to 600 Ah (`Phobos_BatteryCapacity`, agent lean); enables auto-idle | 120 steel, 8 components | Batteries | None | A battery box behind the cab with a vent louvre |
| `WorkLights` | Adds the work-lights load (6 A) and lets the survey run at night | 30 steel, 2 components | None | None | Two lamps on the cabin roof, a lit overlay at night |
| `Hydrogeology` (DBH folder) | Adds the groundwater report (`CompUpgrade`): shows the groundwater grid for a day after a survey and names the richest well cell within 15 cells; `Phobos_ScanRate` -0.05 (a heavier boom) | 80 steel, 4 components | Ground-penetrating scanner | None | A resistivity reel on the boom |

Not proposed: an engine-class node (RimThunder's `IfModActive` patch adds those to every tree of ours, by the utility vehicles record).

## Parts

Cells are the vehicle's own facing north: x 0-1, z from -3 to 3, the cab forward at high z. Health, depth and cells authored. Appended only,
never reordered (saves hold part health by position). Roles for the systems service in brackets.

| Part | Health | Depth | Cells | Feeds | Job |
| --- | --- | --- | --- | --- | --- |
| Chassis | 130 | External | all, weight 3 | BodyIntegrity | Takes the hits that reach nothing else |
| Cab | 50 | External | (0, 3) (1, 3) | | Crew |
| Engine | 55 | Internal, priority | (0, 3) (1, 3) | MoveSpeed | Under the cab; a dead engine stops the truck and the alternator |
| Transmission | 40 | Internal | (0, 2) (1, 2) | MoveSpeed | |
| Front axle and wheels | 35 | External | (0, 2) (1, 2) | MoveSpeed | |
| Rear axle and wheels | 35 | External | (0, -2) (1, -2) | MoveSpeed | |
| Fuel tank | 35 | Internal | (0, 1) | | Vehicle Framework's fuel-leak reactor |
| Radiator | 30 | External | (0, 3) (1, 3) | | The systems service's cooling |
| Alternator [Alternator] | 30 | Internal | (1, 2) | | Standard mode's supply; auto-idle's charger |
| Battery box [Battery] | 35 | Internal | (0, 1) (1, 1) | | The starting battery; the `SurveyBattery` node fills the same box (the node sets the label and the capacity, the part never changes) |
| Recorder and console | 35 | Internal | (0, 0) | Phobos_ScanRate, Phobos_RigControl | Below the control floor the survey will not run |
| Wiring | 25 | Internal | (1, 0) (1, 1) | Phobos_RigControl | |
| Power distribution | 30 | Internal | (0, -1) | Phobos_RigControl | Feeds the console; in fast mode its efficiency caps the array's speed |
| Hydraulic pump | 30 | Internal | (1, -1) | Phobos_RigStability | Raises the boom and the outriggers |
| Outriggers | 45 | External | (0, -1) (1, -1) (0, -3) (1, -3) | Phobos_RigStability | Below the floor the truck will not set up |
| Sensor boom | 50 | External | (0, -1) (1, -1) (0, -2) (1, -2) (0, -3) (1, -3) | Phobos_ScanRate | Folded along the deck, raised when deployed (overlay) |
| Sensor head | 30 | External | (0, -3) (1, -3) | Phobos_ScanRate | The geophones or the probe: the fitted node sets the label |
| Lights | 15 | External | (0, 3) (1, 3) (0, -3) (1, -3) | | Headlights and work lights; a load at night |

**Floors (authored, as the drill rig's):** deploying needs `Phobos_RigStability` and `Phobos_RigControl` at or above 0.5; surveying needs
control at or above 0.35. The refusal names the part ("Cannot set up: the outriggers are too damaged"; "The survey has stopped: the
wiring is too damaged"). `Phobos_RigControl` already exists (`Tanks.xml:50`); `Phobos_ScanRate` is design-only and would be defined with
the truck. Reactors: the fuel-leak reactor on the fuel tank. The systems loadout adds the starter beside the engine.

## Art request

Pilot picture first: **the bare body, north**, inspected at native size before any facing set, under the paid-generation rules. The body
is drawn empty: a cab-over 4x4 light truck, a boxy recorder cabin behind the cab, a flat deck with a folded boom lying along it to the
rear, no cargo, crew, damage or text. Canvas 3 x 8 cells. Overlay sets, each with north, east and south on the body's canvas:

**Motion (9 Nov 2026; [the animation research](vehicle-animation-research.md)):** the boom is its own sprite, drawn stowed on the body's
canvas with the body drawn without it, hinged at its root (the bracket behind the cabin), and **swings out flat to the clearer side** over two seconds when the truck sets up (owner's choice; built in Framework 0.51.0; a rotation from straight above cannot lift it);
the long-range dish is its own sprite, turned slowly by code while long range is on; the powered array's look is a plain glow sprite for a pulsing shader;
outriggers down and the lamps lit stay two-state overlays. Geophones and the probe are static. No frame sheets.

| Overlay set | Shown when |
| --- | --- |
| Boom raised and outriggers down | Deployed |
| Geophone spikes along the boom | `GeophoneArray` |
| Shielded probe housing with a hazard band | `RadiometricProbe` |
| Dish on a mast over the cabin | `LongRangeArray` |
| Transducer array and cable tray on the deck | `PoweredArray` |
| Battery box with a louvre behind the cab | `SurveyBattery` |
| Lamps on the cabin roof, and a lit version | `WorkLights`; lit at night while deployed |
| Resistivity reel on the boom | `Hydrogeology` |
| Damage: bent boom, lost outrigger, scorched cabin | `ComponentRequirement` on the boom, outriggers and recorder |

Icon: the truck from the north with the boom raised, on the house palette. Body colours for the Def are chosen at the pilot, repaintable.
- **Size it first** (owner's rule, 9 Nov 2026): before the first picture, measure the planned footprint against the biggest comparable light truck on the owner's list (the HUMVEE mod's trucks and GTek's MaxxPro at 2 wide), and generate at 1.5 times the drawn size ([the rules](paid-image-generation-rules.md)); the tanker had to grow from 2 x 6 to 3 x 10 after its first look.

## Open questions for the owner

| Question | Agent lean |
| --- | --- |
| Drivetrain: a new `PhobosDrivetrain_MediumDiesel`, or reuse `PhobosDrivetrain_HeavyDiesel`? | New Def; the heavy one's wheels and drag are a highway truck's |
| The curve: 1.5x per find capped at 8x, or 2x, or a 4x cap? | **Decided by the owner, 9 Nov 2026: 1.5x, cap 8x;** tune in play |
| Fast mode 3x and 150 A, battery 600 Ah: accept, or a bigger battery for more field speed? | Accept; the field is meant to be slower than the camp |
| Should the curve count the game's scanner buildings' finds too (a patch on their find)? | No: the truck is the only thing slowed; the game's own economy stays untouched |
| Uranium: 10 in the base and 40 in the probe, or uranium only in the probe? | **Decided by the owner, 9 Nov 2026: both.** The recorder's source is the grounding; 50 against a 1,200 minimum find is 4% |
| Accept VVE's car parts as repair substitutes for this truck? | No: car wheels and a car transmission do not fit a portal-axle 4x4; RimThunder's medium engine yes |
| The hydrogeology report: 15 cells and a day's overlay, or something else? | 15 cells, one day; it reveals, never makes water |
| Auto-idle thresholds: start at 30%, stop at 80%? | Yes: they are the electrical system's existing low stage and charging taper |
| Where does the 3,600 W shore load come off the net? | In the generator set record: shore power debits the net as a real load for every vehicle of ours |
| Research gate for `PoweredArray`: Fabrication plus long-range, or long-range alone? | Both; it is the costly overhaul |

## Who chose what

| Decision | Owner (9 Nov 2026) | Agent proposal |
| --- | --- | --- |
| One record per vehicle, research only | Yes | This record |
| No Helixien gas hooks; deepchem in | Yes | Deepchem needs no code: it is on the game's deep list |
| Both scanner jobs by upgrade | Yes | Base deep find; `LongRangeArray` node starts the quest |
| Two ways of running through the existing electrical system | Yes | 30 A console; 150 A array; shore power or `SurveyBattery` |
| Recharge alone in the field: auto-idle only | Yes; solar and unattended operation offered and not chosen | 30% to 80% at high idle, 8 chemfuel a day |
| Diminishing returns per map | Yes, and the 1.5x per find with its 8x cap | Long-range off the curve |
| Uranium in the cost | Yes, and the amounts: 10 base, 40 in `RadiometricProbe` | The arithmetic above |
| Camp power is the family rule | Yes, in the generator set record | One row here, the link above |
| Our part items always exist; substitutes where they make sense | Yes (principle 9 reversed) | RimThunder medium engine accepted; VVE car parts not |
| Footprint, mass, drivetrain, curve figures, node costs, part table, art list | Open | All agent-authored above |

## Sources

- **Decompiles (read, never copied; summarised in our own words):** the game's full decompile `.local/decompiled/full/all.cs`
  (scanners, deep resource grid, overlay, quest start, deep ore Defs, world tiles, mutators); Vehicle Framework 1.6
  `.local/decompiled/VehicleFramework-1.6/Vehicles.decompiled.cs` (upgrade types and nodes); Dubs Bad Hygiene 1.7
  `.local/decompiled/DBH-1.7/DubsBadHygiene/MapComponent_Hygiene.cs`, `CompWaterInlet.cs`, `PlaceWorker_WaterGrid.cs`,
  `PlaceWorker_DeepWaterGrid.cs`; Rimefeller `.local/decompiled/Rimefeller/Rimefeller/PlaceWorker_DeepWellHead.cs`,
  `PlaceWorker_WellHead.cs`, `MapComponent_Rimefeller.cs`; Vanilla Expanded Framework 1.7
  `.local/decompiled/VEF-1.7/VEF.Maps/TileMutatorExtension.cs`. Defs through `scripts/query-defs.py`, 9 Nov 2026.
- **Mods by name and author:** Vehicle Framework (SmashPhil); Vanilla Vehicles Expanded, Vanilla Chemfuel Expanded and Vanilla Expanded
  Framework (Oskar Potocki and the Vanilla Expanded team); Dubs Bad Hygiene and Rimefeller (Dubwise); RimThunder - Core (the RimThunder
  team); Local Mineral Scanner (Workshop 3798782281; author not recorded here).
- **Our records:** [utility vehicles](utility-vehicles-design.md), [vehicle health, slice 3](vehicle-health-consequences.md),
  [electric vehicles](vehicle-electric-design.md), [the generator set](vehicle-generator-set.md), `Systems_Electrical.xml`,
  `Loadouts_Default.xml`, `Drivetrains.xml`, `CompPhobosVehicleSystems.cs` (`ShorePowered()`).
- **Logging-tool grounding (secondary; no link verified here):** sealed caesium-137 sources for density logging and americium-beryllium or
  californium-252 neutron sources for porosity logging are described in Ellis and Singer, *Well Logging for Earth Scientists* (Springer,
  2nd ed.) and in the IAEA's guidance on radiation sources in well logging; potassium, uranium and thorium mapping by gamma-ray spectrometry
  in IAEA TECDOC-1363, *Guidelines for radioelement mapping using gamma ray spectrometry data* (2003). These ground the uranium in the cost
  only; the game's instrument models none of it.
- **Vehicle grounding (secondary, approximate):** Mercedes-Benz Unimog U4000 class, about 130 kW (OM 904 LA) and a 4.5 to 5 t chassis
  mass; the 4,500 kg is the owner's round figure.
