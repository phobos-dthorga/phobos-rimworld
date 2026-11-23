# World elevation: slopes, descents and engine braking, altitude, and radio line of sight (18 Nov 2026)

**Status (23 Nov 2026):** Descents are built in D'thorga Framework 0.251.0 ([descents as built](#descents-as-built-framework-02510-23-nov-2026)):
both ways, brakes that heat and fade with their own setting, and four retarders; not yet tested in game, rows 416 to 419. A research record. Built from it: the fixes for gaps D3 to D5 (D'thorga Framework 0.131.0, not yet tested in game), and D2 corrected in its record. Altitude is built too (Framework 0.132.0, in [thin air, turbines and aircraft fuel](vehicle-altitude-and-turbines.md), not yet tested in game). Not built: descents and engine braking (held), anything for radio, and D1. A tile is about 30 km across (owner, 18 Nov 2026).

The owner asked how slopes could be simulated in a game like RimWorld, after engine braking came up beside the
[engine loads](archive/vehicle-engine-loads.md) work, and asked for research and a record before anything concrete is decided. They added
that the answer will help set the direction of the radio mod. This record is the research: what the game holds, what a real world
of the owner's looks like when measured, what real trucks do on a descent, and what the same data would give a radio phase.

**Read this first:**

- **Every surface world tile has a real elevation in metres**, from about -500 to 5,000, sea level at 0. Vanilla never uses it for
  movement, and neither does Vehicle Framework.
- **Hilliness is a separate thing.** It comes from its own noise and does not follow elevation: on the owner's world every hilliness
  class has the same median elevation, about 500 m. Elevation is "how high is this place"; hilliness is "how rough is the ground here".
- **Height changes gently between neighbouring tiles**: 22 m at the median and 174 m at the 99th percentile on the owner's world. As a
  road grade that is small at any believable tile size. **Real descents come from hilliness, not from elevation.**
- **Our model has slopes already, and they are always uphill.** A hilly tile is one long climb in both directions. Nothing ever
  descends, so nothing ever brakes.
- **At the game's speeds a loaded truck on a steep road is at the real-world limit of its brakes.** By the same sum a Wyoming study
  used on real passes, our heavier trucks on an 8 % road descent need an engine brake or a lower speed.
- **Soft ground brakes by itself.** On soil, rolling resistance swallows a 5 % descent. Descents matter on roads and hard ground.
- **A tile is about 30 km across** (owner, 18 Nov 2026). The game does not say; three anchors give 6.6 to 9 km, 29 km and 31.5 km,
  and the owner took the two that agree. Radio depends on this more than vehicles do.
- **Elevation's best use for vehicles may be altitude, not slope**: thinner air takes engine power, rotor lift and cooling.
- **For radio, elevation is the terrain.** Line of sight, hilltop sites and what stands in the way can all be read from it cheaply.

How to read the citations: `file:line` marked † was read by a survey agent on 18 Nov 2026 and not re-read by this record's author.
Game lines are from the 1.7 decompile (`full/all.cs`), Vehicle Framework's from its 1.7 decompile; both are kept in `.local/` and
never committed. The measurements were made by the author with a scratch script over the save tool's index.

## What a world tile holds

| Field | What it is | Where |
| --- | --- | --- |
| `elevation` | Metres above sea level, a float; water is 0 or below. The game labels it "m" in the terrain tab | `Tile.elevation`, `all.cs:776671`†; `SurfaceTile.WaterCovered`, `:776585`† |
| `hilliness` | Flat, small hills, large hills, mountainous, impassable | `all.cs:776631-776639`† |
| temperature, rainfall, swampiness, pollution | Climate, with elevation's effect on temperature already included | `all.cs:776675-776683`† |
| roads and rivers | Links to neighbouring tiles, stored on both tiles | `SurfaceTile`, `all.cs:776579-776581`† |

- **Reading it:** `Find.WorldGrid[tile].elevation` and `.hilliness`. Plain public fields; nothing patches or hides them.
- **Only the surface layer has it.** In orbit a tile's "elevation" is the layer's radius, not metres, so guard with the surface layer.
- **Saves round it** to whole metres and lift land to at least 1 m (`all.cs:516253`†). A difference under a metre is noise.
- **A local map has no height at all.** The elevation grid used while a map is generated only decides where rock goes, and is thrown
  away when generation ends (`all.cs:85469-85484`†). **Slopes can only ever be a world-map thing.**

### How the game makes them

- **Elevation** is blended noise, cubed, then stretched to -500 to 5,000 m (`WorldGenStep_Terrain`, `all.cs:781270-781303`†). Cubing
  keeps most land low and makes high ground steeper.
- **Hilliness** comes from a separate "mountain lines" noise plus hill patches (`all.cs:781364-781443`†). Elevation enters only to
  make every ocean tile flat.
- **Temperature falls 8.4 °C for each kilometre** above 250 m (`all.cs:781494-781502`†). Real lapse rates are 6.5 to 9.8 °C a km,
  which supports reading the game's metres as real metres (our inference).
- **Rivers use an "implied elevation"**: the tile's elevation plus 15, 250, 500 or 1,000 m for small hills up to impassable
  (`WorldGenStep_Rivers.GetImpliedElevation`, `all.cs:780880-780900`†). This is the game's only model of how high a tile's own
  peaks stand above its floor, and a ready-made figure for anything that wants "the top of this tile".

## Measured on the owner's world

From the newest save's index, read-only. The world is a full globe of 590,492 tiles, 63 % of it land.

| Measure | Value |
| --- | --- |
| Land elevation | median 503 m; a tenth below 91 m, a tenth above 1,481 m; highest 3,968 m |
| Land by hilliness | flat 69 %, small hills 8 %, large hills 8 %, mountainous 12 %, impassable 4 % |
| Median elevation of each hilliness class | flat 497 m, small hills 538, large hills 539, mountainous 496, impassable 505 |
| Height difference between neighbouring passable land tiles | median 22 m; 68 m at the 90th percentile; 174 m at the 99th; steepest 1,103 m |
| The same, split by the rougher tile's hilliness | the same in every class: median 22 or 23 m |
| Tiles round the equator | about 1,271 |
| A steady climb, following the steepest way up | typically 6 tiles gaining 204 m; one in ten runs 17 tiles and gains 772 m; the longest found gained 2,561 m over 36 tiles |

**What it shows:**

- The measurement confirms the code: hilliness and elevation are independent. A mountainous tile is no higher than a flat one.
- Elevation is a broad, smooth landscape of plateaus and basins tens of tiles across. Hilliness is local roughness laid over it.
- The net slope between two tiles is small. As a grade, by what a tile is taken to be across:

| A tile is | Median | 90th percentile | 99th percentile | Steepest pair |
| --- | --- | --- | --- | --- |
| 9 km | 0.24 % | 0.76 % | 1.9 % | 12 % |
| 30 km | 0.07 % | 0.23 % | 0.6 % | 3.7 % |

Real roads agree with this picture: over tens of kilometres a road's net grade is small, and the steep parts are local climbs and
descents within the country it crosses. In the game's terms, **the local steepness is hilliness, and elevation is the long haul.**

## How big a tile is

The game never says, anywhere, in code or text. It commits only to metres for elevation. Three anchors:

| Anchor | Reasoning | A tile is |
| --- | --- | --- |
| A walker at 5 km/h | 3,300 ticks a tile is 1.32 game hours ([map scale research](map-scale-research.md), the radio work's starting point) | 6.6 to 9 km |
| Our vehicles at their own speed | 340 cells a tile at 1 m a cell; a vehicle's speed in cells a second is its speed in metres a second in our drivetrain, and the day's clock runs 86.4 times faster than the action's, so every vehicle covers the same distance per tile whatever its speed | 29.4 km |
| An Earth-sized planet | 40,075 km round, over 1,271 tiles. Atmosphere Overhaul assumes this throughout (Earth's radius, 6,371 km, in its tile cache†) | 31.5 km |

- The second and third agree within 7 %, from unrelated starting points.
- The first disagrees by a factor of four. That is the game's two clocks: people on a map move far faster than a 5 km/h walk if a
  cell is a metre.
- **Decided (owner, 18 Nov 2026): about 30 km**, the agent's recommendation, where the second and third anchors meet. Nothing reads
  it in code yet. It sets the vehicles' net slope if descents are built, real aircraft ranges in tiles (the V-22's 741 km on one tank
  is about 25 tiles, our arithmetic), and radio's reach: at 30 km a tile, VHF from mast to mast barely leaves its own tile.

## What reads elevation and hilliness today

| Reader | Elevation | Hilliness | Direction of travel |
| --- | --- | --- | --- |
| Vanilla caravans | Never | A fixed extra difficulty per class | None: a move costs the difficulty of the tile entered (`all.cs:790253-790295`†) |
| Vehicle Framework | Never | The same, or a vehicle's own figures; **0 on a road** by default (VF `:89428-89432`†) | None: a move costs the tile being left (VF `:95501-95587`†) |
| Our drivetrain | Never | A grade: 0, 2, 5 or 8 % (`Rules/WorldRules.cs:66-72`, authored) | None: always a climb, read from the tile being left (`Systems/Powertrain.cs:410-415`†, `:680-708`†) |
| Vanilla rivers | Yes, with the implied height | Yes | Downhill |
| Vanilla local maps | No | Scales how much rock a map gets | n/a |

Our own model, in full:

- On the world map a moving caravan is sampled at its design speed on a grade set by the tile's hilliness alone.
- **The grade can never be negative.** There is no test for a negative grade anywhere.
- Rails still receive the hill grade; boats ignore it (a propeller's load ignores the road); aircraft never reach it†.

### What the model would do with a descent today

Traced through `Rules/DrivetrainModel.cs`†, were it handed a negative grade:

- The force at the wheels goes negative, the power needed is clamped to zero, and the engine does no work.
- The gearbox picks the **highest** gear that fits, the opposite of holding a truck back in a low one.
- The engine burns friction fuel at that speed. There is no fuel cut-off on the overrun.
- An electric drive regenerates nothing; a diesel-electric has no dynamic braking.
- The speed cap stays at 1: a descent can never make a caravan faster.
- **Nothing absorbs the energy.** There is no braking term, no brake temperature and no fade anywhere in our code.

### Gaps found on the way

Read from code by the survey, not confirmed in play. **D2 to D5 were put right in Framework 0.131.0** at the owner's word (18 Nov 2026):
a road keeps half the country's grade and a railway a quarter (authored, in the rules Def), a leg reads the tile it is driving into, and
the cross-links record no longer promises a descent term. D1 stays open: it would change how fast caravans wear their brakes.

| # | Gap | Where |
| --- | --- | --- |
| D1 | **On the world map nothing wears per stop.** Vehicle Framework's caravan pather raises no start or stop events, so a caravan's brakes, clutch and air take only the hourly wear | VF `:94973-95727`†; `Systems_Chassis.xml:409-429`† |
| D2 | **The heavy-duty brakes' "descent term" was never built.** The cross-links record gives the fitting a descent figure, but no descent parameter exists for it to act on | `Fittings.xml:292-295`†; [cross-links](vehicle-crosslinks.md), row F3 |
| D3 | Rails get the hill grade. A railway is graded far flatter than the country it crosses | `Powertrain.cs:703-707`† |
| D4 | Vehicle Framework counts hills as 0 on a road; our grade does not. A road through mountains costs Vehicle Framework nothing extra and our model the full 8 % | VF `:89428-89432`† |
| D5 | The tile read during a leg is the one being left, so a caravan climbs the hill it has just come off | VF `:95296`†, `Powertrain.cs:410-415`† |

## Descents and engine braking

### What real trucks do

From sources read in full, listed under [sources](#sources).

- **A compression release brake ("Jake brake") turns the engine into an air compressor.** It opens the exhaust valves at the top of
  the compression stroke, so the work of compressing the air is thrown away, not returned. "No fuel is consumed while the engine
  brake is operating" (Heavy Duty Trucking, 2023).
- **Its power rises with engine speed.** "At speeds around 1,200 rpm, the engine brake delivers about half the retarding power it's
  capable of", and "the best performance is achieved at about 10% above the engine's rated rpm" (the same article, citing Jacobs).
  So the driver picks a low gear and lets the engine rev.
- **It is about as strong as the engine.** Measured on a real truck: a 2013 Cummins ISX15 with a Jacobs brake absorbed
  **502 hp at full setting, 238 hp at half and 63 hp with the brake off** (plain engine friction), at 1,800 rpm (Wyoming Department
  of Transportation, 2018).
- **An exhaust brake gives about half that.** "Exhaust retarders are known to offer only about half the retarding power of engine
  brakes but produce less noise, are easy to operate and are relatively inexpensive" (the Wyoming report). They suit light and
  medium diesels; engines of 10 litres and more need compression brakes (FleetOwner, 2002).
- **Service brakes are limited by heat.** The United States' Grade Severity Rating System holds brakes to **500 °F**, "based on the
  fade temperatures of brake linings", and leaves room for one emergency stop at the bottom (the Wyoming report). Its brake model is
  Newton's cooling: the brakes head toward a steady temperature set by the power going into them, at a rate set by the air moving
  over them.
- **The safe speed depends on weight, grade and length.** For an 80,000 lb truck with no retarder, the updated model gives
  **28 mph on a 7 % grade 5 miles long**, 37 mph on 6 % for 6 miles and 61 mph on 5 % for 7 miles. With the engine brake on full,
  the same truck may run at the speed limit on every one.
- **Engine brakes were invented for exactly this.** Clessie Cummins developed the first in 1931 "after a near crash on a steep
  descent of the Cajon Pass" (the Wyoming report).

### What it would take at the game's speeds

Power that must be absorbed to hold speed on a descent, worked by hand from the drivetrain's own road-load formula. Loads marked
illustrative are the author's.

| Vehicle | On a road, 5 % | On a road, 8 % | On soil, 5 % | On soil, 8 % |
| --- | --- | --- | --- | --- |
| Ullage tanker, empty (9 t, 43 km/h) | 38 kW | 70 kW | none | 29 kW |
| Ullage tanker with 8 t aboard (illustrative) | 74 kW | 134 kW | none | 58 kW |
| Talus tipper, full (12.4 t, 40 km/h) | 49 kW | 89 kW | none | 38 kW |
| Kelly drill rig (20 t, 36 km/h) | 73 kW | 132 kW | none | 57 kW |
| Tanker towing a full flatbed (24.8 t) | 108 kW | 196 kW | none | 85 kW |

What can absorb it, for the tanker's 224 kW engine at 2,000 rpm:

| Absorber | Power | Standing |
| --- | --- | --- |
| The engine's own friction | 21 kW | From the model's friction figures (authored). The Wyoming truck's 63 hp at 1,800 rpm is the real counterpart |
| An exhaust brake | about 75 kW | Authored: half a compression brake, from the Wyoming report's "about half" |
| A compression brake | about 150 kW | Authored: the engine's rated power at its rated speed, from the Wyoming measurement of 502 hp on a 450 to 500 hp class engine, scaled with speed |
| The service brakes | The rest | Limited by heat |

**Reading the two tables together:**

- **On soil a 5 % descent needs no braking at all**: rolling resistance takes it. Only an 8 % descent rolls, and gently. Descents are
  a matter for roads and hard ground, which is also where Vehicle Framework says hills cost nothing.
- **An empty truck is comfortable anywhere.** Its engine and brakes share 70 kW easily.
- **A loaded truck on an 8 % road is at the edge.** The tanker with 8 t aboard puts about 113 kW into its brakes with no retarder,
  9.4 MJ for every kilometre. The Wyoming truck at its safe speed (28 mph on 7 %) puts roughly 220 kW into ten brakes for 10.7
  minutes: gravity's 312 kW, less the engine's measured 47 kW and our own estimate of rolling and air. Per brake the two are close,
  so a few kilometres of 8 % is about what our loaded tanker's brakes could take (our arithmetic, not a model).
- **An exhaust brake changes the answer**: with it, the loaded tanker's service brakes carry about 38 kW, not 113.
- **A trailer is the hard case**: 196 kW on 8 %, more than a compression brake holds at that speed. Slow down or wear out.

### Three ways to have a descent

| Way | What it is | For | Against |
| --- | --- | --- | --- |
| **Hills go up and down** | A hilly tile is half climb and half descent at its grade | Needs no new data; keeps the average effort near today's; matches what hilliness is | The split and the length of each run are authored |
| **Net slope from elevation** | The grade of a leg is the height difference between its two tiles over the tile's width | Real data; routes become one-way (down to the coast loaded, home empty) | Measured, it is under 1 % almost everywhere, so by itself it does almost nothing; needs the tile's size settled |
| **Both** | The net slope, with hilliness as roughness on top | Uses each for what it is | The most to explain to a player |

The author's reading, not a decision: the first way does the work, and the second adds a little truth at almost no cost once a
tile's size is settled.

### What would have to change

In our code only; Vehicle Framework needs no patch.

1. **A direction.** The sample must know the leg (from, to, how far along), not one tile. Vehicle Framework's caravan pather holds
   all three (`NextTile`, the caravan's tile, and the cost left over the cost total†). A caravan reversing mid-leg swaps them and
   needs care†.
2. **A signed grade rule** in `Rules/WorldRules.cs`, with tests for negative grades.
3. **The drivetrain's overrun**: when the road pushes, choose a low gear, let the engine's friction and any retarder absorb what
   they can at that speed, and report what is left for the service brakes. Decide whether fuel is cut on the overrun.
4. **Brakes that take the rest**: a descent term in brake wear, and, if wanted, a brake temperature with fade as a new quantity.
5. **Fittings**: the exhaust brake of the cross-links record (F6), and a real figure for the heavy-duty brakes (F3, gap D2).
6. **Electric and diesel-electric**: regeneration into the pack, and dynamic braking into resistors, if they are to differ.
7. **Exclusions**: aircraft are out already; boats and rails need saying.

### The tie to engine loads

- **On a descent every load on the engine is free braking.** The alternator, the fan and the air conditioner all take shaft power,
  and on the overrun gravity pays for it. A trade article puts it in a driver's terms: an engine brake's effect "increases by 35 to
  40 retarding hp. when the fan kicks in" (FleetOwner, 1997).
- So the rule is one line in the [engine-load ledger](archive/vehicle-engine-loads.md): when the road pushes the engine, the loads are paid
  by gravity before fuel. A battery charged on the way down a pass is the same rule seen from the electrical side.
- With the fuel model on, a descent already hands fuel back against Vehicle Framework's flat rate†. A fuel cut-off would make a
  descent cost nothing, which is what the source says of a real engine brake.

## Altitude: elevation's other use

Slope is not the only thing elevation gives. **The air thins with height**, by the standard atmosphere (standard physics, worked
by the author, not read from a source for this record):

| Elevation | Air density against sea level | Land on the owner's world above it |
| --- | --- | --- |
| 500 m | 0.95 | half |
| 1,000 m | 0.91 | about a quarter |
| 1,500 m | 0.86 | a tenth |
| 2,500 m | 0.78 | about a hundredth |
| 4,000 m | 0.67 | none: the highest tile is 3,968 m |

What follows from thinner air, each a candidate, none designed:

- **A naturally aspirated engine loses power roughly with density**; a turbocharged one holds its power higher up. The turbocharger
  fitting would gain a second purpose.
- **Rotors and thrust lift less.** A helicopter's sling already loses lift with heat, on the stated ground that "air density falls
  with temperature" (`Rules_VehicleSystems.xml`, the sling figures). Altitude is the other half of the same physics.
- **Cooling is weaker**, for radiators and for brakes on a descent, and water boils lower.
- **A turbine makes less power**, the same way.

This fits the step back's "lost power as power": altitude would be one more factor on what the engine can give. It needs no
decision about a tile's size.

## What elevation gives radio

The radio framework is on the back burner; this section is research for when the owner picks it up, and changes nothing in it. Its
design asks, as an open question, "What real distance does one world tile represent? Every HF and VHF range depends on it"
([radio framework design](radio-framework-design.md)). This record's findings bear on that question and on a world-map phase.

### Line of sight between tiles

- **The data is there**: the height of every tile, and with the implied elevation, how far its peaks stand above its floor.
- **The game has no helper for a line between two tiles**, but one is cheap (our proposal): step along the great circle between the
  two tile centres, and at each step move to whichever neighbour is nearest the line. A path 100 tiles long costs about a thousand
  small lookups, far under a millisecond, and can be kept per pair of stations.
- **The radio horizon** over a smooth planet, with the usual allowance for the air bending the wave (a planet four-thirds the size),
  is about 4.12 times the sum of the square roots of the two antenna heights in metres, in kilometres. This is the textbook figure,
  not read from a source for this record; it stays **unverified** until one is recorded in
  [radio reference checks](radio-reference-checks.md).

| Antennas | Horizon | In tiles of 30 km | In tiles of 9 km |
| --- | --- | --- | --- |
| Two hand-helds, 2 m | 12 km | 0.4 | 1.3 |
| Two 10 m masts | 26 km | 0.9 | 2.9 |
| A 10 m mast and a station 250 m up | 78 km | 2.6 | 8.7 |
| A 10 m mast and a station 500 m up | 105 km | 3.5 | 11.7 |
| Two stations 500 m up | 184 km | 6.1 | 20.5 |
| Two stations 1,000 m up | 261 km | 8.7 | 29.0 |

**What this means for play:**

- **Height is range.** On the owner's world neighbouring tiles differ by 22 m at the median, but a mountainous tile's peaks stand
  500 m above its floor by the game's own figure. A station on a peak sees several tiles; one in a valley sees its own.
- **The hilltop repeater becomes the natural VHF and UHF mechanic**: a small station on high ground that joins valleys which cannot
  hear each other. It is what real operators build, for the same reason.
- **Terrain in the way matters as much as height.** The profile between two stations gives the highest ground between them, and so
  whether the path is clear, grazing or blocked. A blocked path loses signal by diffraction over the ridge; the sums for that are a
  later record.
- **HF does not care.** Skywave leaves the ground, so elevation barely touches it. Elevation is a VHF and UHF matter.

### What the tile's size does to radio

- At **30 km a tile**, VHF from mast to mast barely leaves its own tile. Neighbours are reached from high ground; anything further
  is HF or a chain of repeaters. This is close to real life.
- At **9 km a tile**, the same VHF station reaches three tiles on flat ground and a dozen from a hill. Local radio covers a region.
- The choice is between realism at an Earth-like scale and reach at the walker's scale. It is the owner's, and it is the same
  number the vehicles' net slope would use.

### Living with other mods

- **Read elevation when it is needed, never copy it for good.** DMS Expedition rewrites a tile's elevation and hilliness when an
  impact crater forms, during play, and again at each load†.
- **Atmosphere Overhaul assumes an Earth-sized planet**† and forces new worlds to full globes†. A 30 km tile would agree with its
  weather; its pressure and humidity could later feed radio effects of their own.
- **The number of tiles can change at load**: Atmosphere Overhaul expands older partial worlds to full globes†. Anything kept per
  tile must be sized after the world has finished loading.
- **Water tiles read as -500 m.** Treat them as sea level, as Atmosphere Overhaul does†.

## How other mods use elevation

No mod on the owner's list uses elevation for movement.

| Mod | What it does with it |
| --- | --- |
| Atmosphere Overhaul | Reads it once into its climate cache: surface friction, albedo, a lapse rate of 6.5 °C a km, pressure with height†. It keeps east and north gradients, used only for surface friction† |
| DMS Expedition | **Writes** it: a crater lowers a bowl and raises a rim, then sets hilliness from the new height using the river figures (15, 250, 500, 1,000 m) the other way round†, so most rims become impassable |
| Rails and Roads of the Rim | Reads it for the cost of **building** a road, above a threshold the player sets† |
| Vehicle Framework | Never reads it† |

## What each candidate lets, stops and changes

AGENTS.md's test, for the behaviours this record raises. None is designed; this is to judge whether each is worth designing.

| Behaviour | Lets the player | Stops the player | Changes how they play |
| --- | --- | --- | --- |
| Descents that heat and wear brakes | Bring a heavy load down a pass by choosing gear, speed and fittings | Running a loaded truck and trailer down a mountain road at full speed for nothing | Routes, loads and the exhaust brake become choices; the heavy-duty brakes earn their name |
| Engine braking and the exhaust brake | Hold a load on the engine and save the pads | Nothing | A reason to fit one, and to carry less on a mountain run |
| Altitude takes engine power and lift | Understand why a truck labours and a helicopter lifts less in high country | Treating a 3,000 m plateau like the coast | The turbocharger and the choice of base site matter |
| Radio line of sight from elevation | Put a station on high ground and reach further; see why two bases cannot hear each other | Reaching everywhere from a valley floor on VHF | Siting becomes the heart of local radio; repeaters have a purpose |

**Candidly:** descents are the weakest of the four for the effort. They need a direction, an overrun in the drivetrain, a brake heat
model and new fittings, to change something a player meets only with a loaded truck on a mountain road. Altitude is cheaper and
touches every vehicle and aircraft. Radio line of sight is the one that would shape a whole mod.

## Other factions' vehicles

- **In a raid on us and parked at a base:** none of this applies. Slopes exist only on the world map, and other factions' vehicles
  run the light model, which has no drivetrain.
- **Captured:** it descends like any of ours from then on.
- **Altitude** would be a property of the tile, so it would apply to a raider's vehicle only if the light model were given it. The
  standing decision keeps the light model as it is.

## Descents as built (Framework 0.251.0, 23 Nov 2026)

Built at the owner's word ("Let's proceed with #1 through #5", then the three cards of 23 Nov 2026). Not yet tested in game.

| Part | What it does | Where |
| --- | --- | --- |
| The grade of a leg | A hilly tile climbs at its grade for the first half of a leg and descends for the second (authored: half and half); the net slope from the tile left to the tile entered, over 30 km, lies on top throughout, held within 8 %. Vehicle Framework's own leg progress (`nextTileCostLeft` against `nextTileCostTotal`) says which half | `DescentRule.LegGrade`, `Powertrain.SampleGround` |
| The overrun | When the road pushes the vehicle downhill, the drivetrain picks the lowest gear the engine can spin in within its rev limit; the driveline's losses, the engine's friction, every load it drives (the ledger) and any retarder hold back what they can; an electric drive regenerates, up to 70 % of its rated power (authored); a diesel-electric brakes into its resistors, up to its rated power (authored); the service brakes take the rest | `DrivetrainModel.Overrun` |
| Fuel | Cut off on a piston engine's overrun, as modern engines do; a turbine keeps its idle; the loads are paid by gravity | `Powertrain.FuelKwNow` |
| Compression brake | Every diesel of 250 kW or more has one (authored: engines of 10 litres and more need one, FleetOwner, 2002), holding its rated power at rated speed, in proportion below and up to a tenth more above (the Wyoming measurement; Jacobs via Heavy Duty Trucking). A drivetrain may say otherwise (`compressionBrakeShare`) | `DescentRule.RetarderKw`, `EngineXml.compressionBrakeShare` |
| Exhaust brake | A fitting for diesel engines, half the engine's rated power ("about half", the Wyoming report); an engine with a compression brake gains nothing from it. Its picture is the turbocharger's until its own is drawn | `Fittings.xml` (`PhobosFitting_ExhaustBrake`), `FittingDefs.diesel` |
| Brake heat | A new reading, brake temperature: the brakes warm by the power they take over a heat capacity of 6.4 kJ per degree per tonne (authored from a heavy truck's brake iron), and cool toward the air at 14 an hour on the move (a time constant of about four minutes, from the Grade Severity Rating System's Newton cooling), two-fifths of that standing | `Systems_Chassis.xml`, `DescentRule.HeatPerHour`, `Input_BrakeHeat` |
| Fade | Past 260 °C (the system's 500 °F) the brakes hold less, up to 70 % less by 450 °C (authored); the drivetrain slows the vehicle until the engine and what they still hold suffice. Messages at "fading" and "faded" | `DescentRule.FadeLoss`, `DrivetrainModel.Overrun` |
| Pad wear | A descent wears the pads by the energy they take, three times as fast once fading (authored); the per-wheel brakes (Framework 0.250.0) share it, the front axle more | `Systems_Chassis.xml` (`descentWear`) |
| Heavy-duty brakes | Gap D2 closed: they warm a quarter more slowly, cool 30 % faster and wear 40 % less on a descent (authored) | `Fittings.xml` |
| The setting | **Brakes heat and fade on descents**, D'thorga Framework's vehicle settings, on by default (risk graded moderate: a slower trip and faster pad wear, never a crash). Off, descents still wear the pads but never heat them | `VehicleSystemsSettings.brakeFade` |

**What it lets, stops and changes:** it lets the player bring a heavy load down a pass by choosing the vehicle, the load and an exhaust
brake; it stops a laden truck and trailer running down a mountain road at full speed for nothing; and it makes routes, loads and the
exhaust brake choices. **What outside the vehicle notices:** the caravan's speed on the world map, its fuel, the pads (a service sooner),
and messages when the brakes fade. **Other factions' vehicles:** none of it; descents are on the world map only, where their vehicles run
the light model.

**Limits:** the half-and-half split and the leg's length are authored, not the real shape of each tile; local maps stay level; boats and
aircraft are left out; the exhaust brake's picture is borrowed.

## What is left to decide

The owner's answers of 18 Nov 2026: **altitude is one of the next few slices** ("I really do love this idea"); **descents stay held**
until after the engine-load ledger; **the four safe gaps are fixed** (above). The rest is undecided. In the order they would be needed:

1. ~~How big a tile is~~: **decided, about 30 km** (owner, 18 Nov 2026; [above](#how-big-a-tile-is)).
2. ~~Whether descents are wanted at all~~: **decided, both ways** (owner, 23 Nov 2026, as one of five engine-load follow-ups: "Let's
   proceed with #1 through #5", then the card's answer "Both"): hills go up and down at their grade, with the net slope from elevation
   between tiles on top.
3. ~~How far descents go~~: **decided, a brake temperature with fade** (owner, 23 Nov 2026; the agent's recommendation), with its own
   setting, on by default. **What else holds a vehicle back** (owner, 23 Nov 2026, all four offered): the engine's friction and loads
   with the fuel cut off on the overrun; an exhaust brake fitting for diesels; a compression brake built into heavy diesels; and
   regeneration into an electric vehicle's pack (a diesel-electric braking into resistors).
4. **Whether altitude should take engine power and lift.**
5. **Whether the gaps D1 to D5 should be fixed** by themselves, whatever is decided about descents.
6. **For radio**: nothing now. This record is there for the day it comes off the back burner.

## Sources

Findings are the sources'; the worked figures, the three ways and every reading are ours.

| Source | What it supports | How it was read |
| --- | --- | --- |
| University of Wyoming for the Wyoming Department of Transportation, *Updating and Implementing the Grade Severity Rating System (GSRS) for Wyoming Mountain Passes*, volume 1, report WY-1901F, December 2018 ([report](https://rosap.ntl.bts.gov/view/dot/42490/dot_42490_DS1.pdf)) | The measured engine brake powers; the 500 °F brake limit and its basis; safe speeds by grade and length; exhaust brakes at about half; the history | From the report's text. Its equations are images and were not read; the model is described here in the report's words |
| Jim Park, "How Truck Drivers Can Effectively Use an Engine Brake", Heavy Duty Trucking, 24 July 2023 ([article](https://www.truckinginfo.com/articles/engine-brakes-101-part-2-driving-for-peak-performance)), and its companion "What Trucking Fleets Need to Know About Engine Brakes", 21 July 2023 | How power varies with engine speed; no fuel while braking; how the brake works | From the pages; trade press, citing Jacobs and Cummins |
| Sean Kilcarr, "what's new in: Engine retarders", FleetOwner, 1 Sept 2002 ([article](https://www.fleetowner.com/news/article/21679565/whats-new-in-engine-retarders)) | Which engines take which brake; compression brakes at twice an exhaust brake | From the page; trade press |
| Stewart Siegel, "Smart Accessories", FleetOwner, 1 Nov 1997 | The fan adding to an engine brake's effect | As read for the [engine loads](archive/vehicle-engine-loads.md) record |
| Wikipedia, "Compression release engine brake" and "Brake fade" | "The power of this type can be around the same as the engine power" (citing Caterpillar); fade on long descents | From the pages; secondary |
| RimWorld 1.7 and Vehicle Framework 1.7, decompiled | Everything about tiles, generation and movement | By survey agents; lines marked † |
| The owner's save | The measurements | By the author, through the save tool's index |

**Unverified: seen only as search summaries, or from memory. No Def may carry these.**

- A Cummins X15's engine brake at 600 hp at 2,100 rpm and 450 hp at 1,500 rpm (maker's press material; the sell sheet's text gave
  only "Braking horsepower levels to 600 HP").
- The Grade Severity Rating System's equations and constants, which the author recalls but could not read.
- Safe-speed tables for Eisenhower and Vail passes, and brake smoke at 850 °F (a crash-analysis firm's page, partly read).
- Hydraulic retarders at about 600 kW with the engine brake (Voith and Volvo material).
- The radio horizon formula and the standard atmosphere, both textbook figures not read from a source for this record.
