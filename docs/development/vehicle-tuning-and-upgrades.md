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

## Sources

- SmashPhil, Vehicle Framework 1.6 (`.local/decompiled/VehicleFramework-1.6/`) and its `develop` branch, MIT; read for research, not copied.
- Our records: [vehicle health, slice 3](vehicle-health-consequences.md), [utility vehicles](utility-vehicles-design.md).
