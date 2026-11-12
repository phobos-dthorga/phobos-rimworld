# Vehicle tuning and upgrades: design (9 Nov 2026)

**Status: fittings, tunes and enemy sets are built (D'thorga Framework 0.40.0), and part grades (0.41.0). Nothing here is tested in game
beyond a save loading with it installed.** The owner's direction (9 Nov 2026): our framework of parts and supplies should become "one of
possible tuning and full of upgrades as well". Findings marked "from the code" are read from our decompile of Vehicle Framework 1.6 and
summarised in our own words.

## Read this first

- **From Framework 0.43.0 a tune or a change of grade on a working part waits for a tuning rig** (owner, 9 Nov 2026; a setting, on by default;
  [the stations record](vehicle-stations.md)). What is below predates that.
- **It works on every vehicle from any mod** (owner), through the roles a loadout already binds, so your RimThunder and other vehicles take fittings
  without anyone writing an upgrade tree for them.
- **Three layers, each doing a different job** (owner): a **fitting** changes how one part performs, a **tune** changes how the whole engine is set
  up, a **grade** changes how tough and how reliable a part is.
- **Part grades hang on the job that replaces destroyed parts**, so check 1 of the [test pass](in-game-test-pass.md) comes first when testing
  them. The owner gave the go for them on 9 Nov 2026.
- **Every figure below is authored**, a gameplay choice to be tuned in play. Nothing is measured from a real machine.

## What exists today

| Thing | What it is | Limit |
| --- | --- | --- |
| Vehicle Framework's upgrade tree | Per-vehicle nodes in XML (`UpgradeNode`), which we use on our own vehicles (the tanker) | From the code: a tree belongs to one vehicle's Def, so another author's vehicle only has one if someone writes it |
| "Tuning" in Vehicle Framework's window | A player-set figure per vehicle **type** (battery size, empty mass) | A settings screen, not something done in play with a mechanic; **a different thing from the tunes here** |
| Our parts and supplies | Roles, parts, what a replacement costs (Framework 0.36.0) | Every part is the same as the one it replaced |
| The systems' figures | Power, heat, wear, grip, charge, in loadouts and Defs | Fixed per vehicle type, until now |

Part lists stay fixed: from the code, Vehicle Framework saves parts by position, so nothing here adds or removes a part.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Fittings** | Bolt an improvement onto one part of one vehicle, from any mod | Stack them: one fitting per part, so a turbocharger and an oil cooler compete for the same engine | Vehicles stop being interchangeable: "the truck with the turbo" is worth protecting |
| **A fitting is lost with its part** (owner) | Know exactly what a destroyed part costs | Keep an improvement through a part's destruction | Protecting a fitted vehicle matters; a shot-out engine costs its turbo as well |
| **A share per part** | Fit one engine of a twin, or one axle, and get a share of the benefit | Multiply a fitting's whole effect by fitting it to each of several parts | Fitting every axle is a real investment, and pays exactly the full figure |
| **Engine tune** | Have a mechanic set an engine to economy, balanced or power | Have power without a price: more power brings more heat and burns more fuel | Choosing a tune for a vehicle's job, and re-tuning for a different one |
| **Gearing** (a kind of tune) | Trade top speed for pulling power | Have both | A heavy hauler and a fast scout can be the same truck set up differently |
| **A minimum skill, no failure** (owner) | Rely on the result: a good enough mechanic always succeeds | Have a novice do the demanding jobs; they are refused and told why | A skilled mechanic is worth keeping, and worth bringing along |
| **Enemy sets** (owner) | Meet an enemy vehicle that is better than the standard one, and capture it with what it came with | Assume every enemy truck of a type is the same | Capturing a fitted vehicle is a real prize; high-tech factions feel different |
| **Part grades** (owner) | Fit a cheap salvaged part when short, or a heavy-duty one where it counts | Have the cheap part hold a fitting, or be as tough or as reliable as a standard one | A choice at every replacement: what can you afford to have fail? |
| **Refit to another grade** (owner) | Upgrade a working part without waiting for it to be destroyed | Do it for free: the old part is used up | Heavy-duty parts become a planned upgrade |

## How it works

### Fittings

A `PhobosVehicleFittingDef` in XML names the roles it goes on, what it costs in materials, the work and minimum Construction skill, and what it does.
So other modders can add their own, what it can change is the figures the framework already reads:

- **Vehicle stats** (`statFactors`), through the stat part every Vehicle Framework stat already has.
- **System parameters** (`parameterFactors`), by name: `radiatorRate`, `alternatorAmps`, `batteryAmpHours` and so on, through the one call every system reads them with.
- **The engine's rated power** (`powerFactor`), through the drivetrain model, and **the fuel it burns** (`fuelFactor`).

Fitting one is the job that already replaces parts: supplies hauled to the cargo, a mechanic, a message that says what it did. **No new items, so no new art for items:** a fitting is built
from materials at the vehicle (agent choice). Replacing a fitting uses up the old one; nothing comes back.

**A share per part (agent rule):** a vehicle with several parts of a role takes `factor^(1/parts)` from each fitted part, so fitting all of them gives
exactly the listed factor and a vehicle's number of axles does not change the sum. Game-free, in `FittingRule`, with tests.

The first set (authored; agent's, for the owner to revise):

| Fitting | Goes on | What it does | Materials | Skill |
| --- | --- | --- | --- | --- |
| Uprated radiator | Radiator | `radiatorRate` x1.3 | 30 steel, 2 components | 4 |
| Oil cooler | Engine, piston | `oilBurnPerHour` x0.6 | 20 steel, 2 components | 4 |
| Turbocharger | Engine, piston | Power x1.2, fuel used x1.1, `heatAtFullLoad` x1.25 | 40 steel, 4 components | 8 |
| Heavy-duty alternator | Alternator | `alternatorAmps` x1.5 | 20 steel, 3 components | 5 |
| Deep-cycle battery | Battery | `batteryAmpHours` x1.5 | 25 steel, 3 components | 4 |
| Heavy-duty springs | Wheels | Cargo capacity x1.15, speed x0.96 | 25 steel, per axle | 3 |
| Road tyres | Wheels, not tracked | Speed x1.05, `wearPerHour` x1.4 | 15 steel, 1 component, per axle | 3 |

**Set aside for now, and why:** run-flat tyres (no figure yet says "keeps moving when damaged"); an armoured fuel tank (armour must go through Combat Extended's rules; the first set avoids armour).

**A fitting must not duplicate a node on the same vehicle.** Upgrade trees stay: a tree is what makes a tanker a tanker; a fitting is what makes one tanker better than another.

### Tunes

A `PhobosVehicleTuneDef` belongs to an axis, **Engine** or **Gearing**, with three named settings each (owner: named, not a slider). One of each axis is in force per vehicle, saved per vehicle.

| Axis | Setting | Power | Fuel used | Heat | Final drive | Skill |
| --- | --- | --- | --- | --- | --- | --- |
| Engine | Economy | x0.9 | x0.92 | x0.9 | | 5 |
| Engine | Balanced (as built) | | | | | 3 |
| Engine | Power | x1.15 | x1.15 | x1.2 | | 8 |
| Gearing | Long | | x0.95 | | x0.85 | 5 |
| Gearing | Standard (as built) | | | | | 3 |
| Gearing | Short | | x1.05 | | x1.2 | 5 |

They change figures the drivetrain model already reads. Fuel follows them whether or not the player has fuel following the load: a tune's factor is applied to what Vehicle Framework already burned. Gearing's fuel figure applies only when fuel does not follow the load; when it does, the model works gearing out itself. **Limits:** tunes apply to piston engines; a gas turbine's power comes from its ratings and an electric motor has nothing to tune, so those vehicles show why not. Gearing needs a gearbox. Boats and aircraft are left out of tunes and the turbocharger: a propeller's speed and an aircraft's flight phase do not follow engine power, so they would only cost heat and fuel. Tunes also need the drivetrain model to be running in full. Because a vehicle's speed cap only ever lowers its top speed, **long gearing does not make a vehicle faster than its design speed**: it lowers the revs and the fuel at cruise. Short gearing pulls harder and tops out sooner.

**Skill (owner):** a job names the minimum Construction skill; a mechanic below it is refused, with the reason. No random failure.

### What they cost (Framework 0.91.0)

**The owner's aim (11 Nov 2026): "I want these upgrades and tunes to feel earned."** An audit found every fitting and tune quick and cheap
(a Power tune 0.8 hours, a turbocharger 1.2), so the costs above are superseded by these. All authored; hours are for a builder of normal
speed (2,500 work is an hour, a working day about 10 hours), for a 5-tonne vehicle at the Standard presets.

| | Materials | Hours (work) |
| --- | --- | --- |
| Uprated radiator | 75 steel, 5 components | 5 (12,500) |
| Oil cooler | 50 steel, 5 components | 4 (10,000) |
| Turbocharger | 100 steel, 10 components | 12 (30,000) |
| Heavy-duty alternator | 50 steel, 7 components | 4 (10,000) |
| Deep-cycle battery | 60 steel, 7 components | 2 (5,000) |
| Block heater | 40 steel, 5 components | 3 (7,500) |
| Heavy-duty springs, per wheel | 60 steel | 3 (7,500) |
| Road tyres, per wheel | 25 steel, 30 chemfuel | 2 (5,000) |
| Economy tune | 3 components | 8 (20,000) |
| Power tune | 5 components | 10 (25,000) |
| Back to the stock tune | nothing | 4 (10,000) |
| Tall or short final drive | 50 steel, 2 components | 10 (25,000) |
| Back to the stock final drive | nothing | 6 (15,000) |

Taking a fitting off is half its fitting time. Skills, research and part grades are unchanged.

- **Presets** (owner: a day or more by default, with presets; materials about 2.5 times, with presets): *Time to fit and tune* (Quick a
  quarter, Shorter half, Standard, Long double) and *Materials to fit and tune* (Light two fifths, about the old costs; Standard; Heavy half as
  much again), as menus of choices, not sliders.
- **By weight** (owner: scale by weight; a setting, on by default): materials grow as the square root of the vehicle's kerb mass against
  5 tonnes, and time as its fourth root, held between half and three times (`FitCostRule`, game-free and tested; the figures in
  `PhobosVehicleRulesDef`). A 600 kg buggy pays half the materials and about 0.6 of the time; a 20-tonne truck twice and 1.4 times; a
  50-tonne tank three times and 1.8 times. The kerb mass does not change with cargo, so a waiting job's cost never moves.
- **Agent choices, for the owner to revise:** road tyres take chemfuel (rubber, as the tyre set kit is made) instead of a component; a
  final drive change is a new gear set, so 50 steel and 2 components rather than 25 steel; the exponents and the clamp.
- A change of preset re-prices work already waiting.

### Our vehicles' upgrades, priced like tunes (Framework 0.106.0)

**The owner, 13 Nov 2026:** "customizations/upgrades for vehicles such as our own Busbar, greatly need to be increased in both resource
cost and that of invested time. For this, we can reference that of our tunes which I feel have a good balance." Built, not yet tested in
game.

- **Which trees:** our own seven (Ullage, the drill rig, Pintle, Busbar, the Seabed Crawler, Datum, Talus), each opted in with the
  marker `PhobosUpgradeCostsLikeTunes` on its upgrade tree. Every other author's tree keeps the costs its author wrote.
- **The rule:** a node's figures in its XML are its base. They are raised once to the fittings' scale (the step fittings took in 0.91.0:
  work five times, bulk materials two and a half; `upgradeWorkUplift` and `upgradeMaterialUplift` in `PhobosVehicleRulesDef`, authored),
  then by the vehicle's weight exactly as a fitting is (`FitCostRule.WeightFactor`, by its kerb mass against 5 tonnes, under the same
  *Scale by weight* setting) and by the same two presets, *Time to fit and tune* and *Materials to fit and tune*.
- **Bulk materials only:** steel, components, plasteel, uranium, chemfuel scale; a part counted in ones (an alternator from a parts
  bill, the Crawler's own parts) is never multiplied. Told apart by how many stack together (`upgradeBulkStackAbove`, 10).
- **Applied once at start-up, after the parts bills, and again whenever one of the cost settings changes** (`UpgradePricing`). Vehicle
  Framework reads a node's work and ingredients live (`Vehicles.decompiled.cs:2761`, `:35021`), so the new price shows at once.
- **An upgrade already under way** keeps the work it started with (Vehicle Framework saves the work left, `:34797`), and haulers bring
  the rest of the new materials.
- **Refunds stay fair.** Vehicle Framework refunds a share of a node's ingredients as they stand when the upgrade is taken off
  (`CompUpgradeTree.RefundIngredients`, `:21748`). Without a record, an upgrade bought before this version, or at a lighter preset, would
  refund more than it cost: a loop that makes steel from nothing. So each priced upgrade's price is remembered when it is finished
  (`GameComponent_PhobosUpgradePrices`, safe to add to a running game), and taking it off refunds from that; one bought before 0.106.0
  refunds from the old figures. The swap lasts only for the refund and is always put back (a finalizer).
- **Floors and caps:** none added. No node reaches more than about 12 hours at the Standard presets, the longest being the powered
  survey array, beside the turbocharger's 12. **The drill rig pays the most materials** (five times its listed figures, from its
  20 tonnes): its work lights node asks 200 steel where the work lamps fitting asks about 60 on the same rig, because that node already
  listed more steel than the fitting. If it reads high in play, lower the node's own figure in its XML rather than add a cap.
- **Research and parts bills:** research gating is unchanged (`PhobosEnforceUpgradeResearch`). The three nodes a parts bill fills
  (the Busbar's uprated alternator among them) keep their part, and only its bulk materials rise.

Hours are for a builder of normal speed (2,500 work an hour), at the Standard presets, with *Scale by weight* on:

| Vehicle (kerb) | Node | Hours (work) before | Hours (work) after | Materials before | Materials after |
| --- | --- | --- | --- | --- | --- |
| Ullage bulk tanker (9,000 kg) | LiquidTank | 1.6 (4,000) | 9.3 (23,166) | 120 steel | 403 steel |
| Ullage bulk tanker (9,000 kg) | InsulatedLiquidTank | 1.6 (4,000) | 9.3 (23,166) | 150 steel, 2 components | 504 steel, 7 components |
| Ullage bulk tanker (9,000 kg) | HighVolumeLiquidTank | 1.6 (4,000) | 9.3 (23,166) | 160 steel | 537 steel |
| Ullage bulk tanker (9,000 kg) | HoseReel | 0.6 (1,500) | 3.5 (8,687) | 30 steel, 1 components | 101 steel, 4 components |
| Ullage bulk tanker (9,000 kg) | TwoCompartments | 1 (2,500) | 5.8 (14,479) | 40 steel, 2 components | 135 steel, 7 components |
| Ullage bulk tanker (9,000 kg) | ThreeCompartments | 1 (2,500) | 5.8 (14,479) | 40 steel, 2 components | 135 steel, 7 components |
| drill rig (20,000 kg) | DiamondBit | 1 (2,500) | 7.1 (17,678) | 30 plasteel, 2 components | 150 plasteel, 10 components |
| drill rig (20,000 kg) | RcHammer | 1 (2,500) | 7.1 (17,678) | 60 steel, 2 components | 300 steel, 10 components |
| drill rig (20,000 kg) | TallMast | 1.2 (3,000) | 8.5 (21,213) | 120 steel, 4 components | 600 steel, 20 components |
| drill rig (20,000 kg) | ElectricPowerPack | 0.8 (2,000) | 5.7 (14,142) | 80 steel, 6 components | 400 steel, 30 components |
| drill rig (20,000 kg) | WorkLights | 0.4 (1,000) | 2.8 (7,071) | 40 steel, 1 components | 200 steel, 5 components |
| Pintle trailer (2,000 kg) | PhobosTrailerHighSides | 0.5 (1,200) | 1.9 (4,772) | 50 steel | 80 steel |
| Busbar generator set (6,000 kg) | BiggerAlternator | 1.2 (3,000) | 6.3 (15,700) | 50 steel, 2 components | 137 steel, 6 components |
| Busbar generator set (6,000 kg) | CableReel | 0.6 (1,500) | 3.1 (7,850) | 40 steel, 1 components | 110 steel, 3 components |
| Busbar generator set (6,000 kg) | BatteryBuffer | 0.8 (2,000) | 4.2 (10,466) | 6 components | 17 components |
| Datum survey truck (4,500 kg) | GeophoneArray | 1 (2,500) | 4.9 (12,175) | 60 steel, 3 components | 143 steel, 8 components |
| Datum survey truck (4,500 kg) | RadiometricProbe | 1.2 (3,000) | 5.8 (14,610) | 40 steel, 30 plasteel, 3 components, 40 uranium | 95 steel, 72 plasteel, 8 components, 95 uranium |
| Datum survey truck (4,500 kg) | LongRangeArray | 1.6 (4,000) | 7.8 (19,480) | 120 steel, 6 components, 20 plasteel | 285 steel, 15 components, 48 plasteel |
| Datum survey truck (4,500 kg) | PoweredArray | 2.4 (6,000) | 11.7 (29,220) | 200 steel, 40 plasteel, 10 components | 475 steel, 95 plasteel, 24 components |
| Datum survey truck (4,500 kg) | SurveyBattery | 1.2 (3,000) | 5.8 (14,610) | 100 steel, 4 components | 238 steel, 10 components |
| Datum survey truck (4,500 kg) | WorkLights | 0.4 (1,000) | 1.9 (4,870) | 30 steel, 2 components | 72 steel, 5 components |
| Talus tipper (10,000 kg) | HighSides | 0.6 (1,500) | 3.6 (8,919) | 60 steel | 213 steel |
| Talus tipper (10,000 kg) | RockBody | 1.2 (3,000) | 7.1 (17,838) | 120 steel, 4 components | 425 steel, 15 components |
| Talus tipper (10,000 kg) | BulkBody | 1.2 (3,000) | 7.1 (17,838) | 150 steel, 4 components | 531 steel, 15 components |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | BottomingWheels | 1 (2,500) | 4.9 (12,373) | its own crawler parts | unchanged (parts are counted in ones) |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | Scrubber | 0.8 (2,000) | 4 (9,898) | its own crawler parts | unchanged (parts are counted in ones) |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | WetLab | 1 (2,500) | 4.9 (12,373) | its own crawler parts | unchanged (parts are counted in ones) |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | DeepLab | 1.2 (3,000) | 5.9 (14,848) | its own crawler parts | unchanged (parts are counted in ones) |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | Hydrophones | 0.8 (2,000) | 4 (9,898) | its own crawler parts | unchanged (parts are counted in ones) |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | ActiveSonar | 0.8 (2,000) | 4 (9,898) | its own crawler parts | unchanged (parts are counted in ones) |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | SensorRefit | 1.6 (4,000) | 7.9 (19,797) | its own crawler parts | unchanged (parts are counted in ones) |
| Seabed Crawler (about 4,800 kg, from its Vehicle Framework mass) | LaboratoryRefit | 2 (5,000) | 9.9 (24,746) | its own crawler parts | unchanged (parts are counted in ones) |

The dev action **"Vehicle systems: list upgrade prices"** writes every node's base and present price to the log.

### Where the player does it

One new button on the Dashboard opens a window listing each part that can take a fitting, and the two tunes; each row says what it does, what it needs and why it cannot be done yet. Orders are queued, can be cancelled, and appear with the vehicle's other repairs in its inspect line.

### Enemy sets

A `PhobosVehicleFitSetDef` names a span of faction tech levels, a weight, some fittings and a tune. When another faction's vehicle first appears, there is a 25% chance (authored; the rules Def) of one set suited to its faction's tech level, and each fitting goes on every free part it suits. It costs nothing per tick. A setting turns it off.

**An honest limit, from our own code:** other factions' vehicles run the light model, in which the drivetrain does not run. So fittings that change **stats** (springs, tyres) act on an enemy vehicle at once; **engine power and tunes only matter once it is captured** and running the full model.

### Part grades (built in 0.41.0)

Three grades: **salvaged, standard, heavy-duty**. Saved by part key; a grade is chosen when a destroyed part is replaced, or by a refit of a working part (owner), which uses up the old one.

| Grade | Cost | Part health | Faults taken from hits | Holds a fitting |
| --- | --- | --- | --- | --- |
| Salvaged | x0.7 | x0.7 | x1.5 | No |
| Standard | | | | Yes |
| Heavy-duty | x1.75 | x1.3 | x0.6 | Yes |

**Agent choices, for the owner to revise:** a replacement keeps the grade the part had unless another is asked for; a refit keeps the part's fitting if the new grade can hold one; a refit costs what replacing the part with that grade costs, and leaves it at full health; a grade other than standard is costed in what the part is made of, and another mod's substitute part only ever stands in for a standard one; heavy-duty needs Machining and Construction 6.

The health goes through Vehicle Framework's own per-part health modifier (from the code: `VehicleComponent.AddHealthModifiers`), applied again on load. The fault figure multiplies the severity a hit gives a part's condition. Figures are authored.

## Cost and risk

- **Balance across every vehicle from every mod** is the main risk: a fitting tuned for a truck may be silly on a tank or a helicopter. Each fitting can be limited by mass, by engine kind and by tracked or wheeled.
- **Performance:** fittings and tunes change only when a mechanic changes them; a player's vehicle reads cached figures, so nothing new runs per tick.
- **Saves:** additive, saved by part key; the uninstall sweep clears them. Safe to add mid-game.
- **Vehicle Framework 1.7** could add its own equivalent. Its development branch showed none when read on 8 Nov 2026.

## Who chose what

- **Owner (9 Nov 2026):** that the framework grows into tuning and upgrades; on every vehicle from any mod; fittings, tunes **and part grades**; a fitting lost with its part; three named settings; a minimum skill with no failure; **enemy vehicles get authored sets**, by chance and tech level; grades change toughness and wear, are chosen at replacement or by refit, and wait for check 1 of the test pass.
- **Agent proposals, for the owner to revise:** every figure; the first set of fittings; materials rather than new items; one window from the Dashboard; the share per part; the 25% chance; gearing as a second tune axis; tracked and turbine limits.

- **Owner (11 Nov 2026):** fittings and tunes should feel earned; a day or more of work each by default, with presets; materials about 2.5
  times, with presets; bigger vehicles cost more, by weight.

## Sources

- SmashPhil, Vehicle Framework 1.6 (`.local/decompiled/VehicleFramework-1.6/`) and its `develop` branch, MIT; read for research, not copied.
- Our records: [vehicle health, slice 3](vehicle-health-consequences.md), [utility vehicles](utility-vehicles-design.md).
