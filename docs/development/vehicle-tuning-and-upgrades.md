# Vehicle tuning and upgrades: what it would do to the player (9 Nov 2026)

**Status: a proposal for the owner's review. Nothing here is built.** The owner's direction (9 Nov 2026): our framework of parts and
supplies should become "one of possible tuning and full of upgrades as well". Everything else below is the agent's proposal. Findings
marked "from the code" are read from our decompile of Vehicle Framework 1.6 and summarised in our own words.

## Read this first

- **Six questions need the owner's answers** (at the end). The one that shapes everything: **should this work on every vehicle from any
  mod, or only on our own?** The agent recommends every vehicle.
- **Agent's recommendation, in order:** a small set of **fittings** first, then **engine tunes**. Leave part grades out: fittings do the same
  job with less to manage.
- **Figures in the examples below are placeholders**, there to show the kind of trade-off. Each would be authored and labelled when built.
- **It builds on untested work.** Part conditions and destroyed parts have not been confirmed in game, so nothing here should start before they are.

## What exists today

| Thing | What it is | Limit |
| --- | --- | --- |
| Vehicle Framework's upgrade tree | Per-vehicle nodes in XML (`UpgradeNode`), which we use on our own vehicles (the tanker) | From the code: a tree belongs to one vehicle's Def, so another author's vehicle only has one if someone writes it |
| "Tuning" in Vehicle Framework's window | A player-set figure per vehicle **type** (battery size, empty mass) | A settings screen, not something done in play with a mechanic; **a different thing from the tuning proposed here** |
| Our parts and supplies | Roles, parts, what a replacement costs (Framework 0.36.0) | Every part is the same as the one it replaced |
| The systems' figures | Power, heat, wear, grip, charge, in loadouts and Defs | Fixed per vehicle type |

Part lists stay fixed: from the code, Vehicle Framework saves parts by position, so nothing here adds or removes a part.

## What each behaviour would do to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Fittings** | Bolt an improvement onto one part of one vehicle: any vehicle, from any mod | Stack them: one fitting per part. A fitting is lost if its part is destroyed | Vehicles stop being interchangeable: "the truck with the oil cooler" is worth protecting, and fittings become a thing to make and carry |
| **Engine tunes** | Have a skilled mechanic set an engine for economy, balance or power, per vehicle | Have power without a price: more power brings more heat, wear and fuel | Choosing a tune for a vehicle's job, and re-tuning it for a different one |
| **Gearing** (a kind of tune) | Trade top speed for pulling power | Have both | A heavy hauler and a fast scout can be the same vehicle set up differently |
| **Part grades** (recommended out) | Choose a cheap or a premium replacement part | Nothing | Another choice at every repair, overlapping fittings |

## How it would work

### Fittings

A `PhobosVehicleFittingDef` in XML names the role it goes on, the item it uses and what it does, using the **same effects the systems already
have** (a factor on a stat, a change to a rate), so there is no second engine and other modders can add their own. Fitting one is the job that
already fits parts: supplies hauled to the cargo, a mechanic, a button that says what it did. It is saved by the part's key, as a condition is.

Examples of the kind of thing (real-world parts, gameplay figures to be authored):

| Fitting | Goes on | Trade-off |
| --- | --- | --- |
| Oil cooler | Engine | Oil runs cooler, one more thing to hit |
| Uprated radiator | Radiator | Cools better, costs more to replace |
| Heavy-duty alternator | Alternator | More charge, more load on the engine |
| Run-flat tyres | Wheels | Keep moving when damaged, slower |
| Armoured fuel tank | Fuel tank | Harder to hole, heavier |
| Turbocharger | Engine | More power, more heat, needs a tune |

### Engine tunes

A setting saved per vehicle (economy, balance, power; agent's suggestion is three rather than a slider), applied by a mechanic as a job
of Construction work. It changes figures the drivetrain model already reads: power, fuel, heat and wear. Gas turbines would use their
existing ratings instead. Gearing changes the final drive ratio the model already has.

**Skill:** a tune could need a minimum skill, so a good mechanic is worth having. No random failure is proposed: a bad roll on a job the
player cannot see into would feel arbitrary.

### Where the player does it

Agent's suggestion: one new section on the Dashboard tab rather than a new tab, listing each part's fitting and the engine's tune, each row saying
what it needs and why it cannot be done yet. The vehicle tabs are already crowded.

### Our own vehicles' upgrade trees

They stay. A tree is what makes a tanker a tanker; a fitting is what makes one tanker better than another. **A fitting must not duplicate a
node on the same vehicle.**

## Cost and risk

- **Balance across every vehicle from every mod** is the main risk: a fitting tuned for a truck may be silly on a tank or a helicopter.
  Limits by mass and by turbine or not, as replacement parts have, would be the first answer.
- **Combat Extended:** armour-like fittings must go through its armour rules; the first set should avoid armour figures.
- **Performance:** fittings and tunes change only when a mechanic changes them, and go through the stat factors already cached, so nothing runs per tick.
- **Saves:** additive, by part key; the uninstall sweep clears them.
- **Vehicle Framework 1.7** could add its own equivalent. Its development branch showed none when read on 8 Nov 2026.

## Questions for the owner

| # | Question | Agent's recommendation |
| --- | --- | --- |
| 1 | Should fittings and tunes work on every vehicle from any mod, or only ours? | Every vehicle: it is what the framework gives that upgrade trees cannot, and a setting can narrow it |
| 2 | Is "fittings, then tunes, no part grades" the right shape? | Yes |
| 3 | Should a fitting be lost when its part is destroyed? | Yes: it makes protecting a vehicle matter |
| 4 | Tunes: three named settings, or a slider? | Three named settings, easier to understand and to balance |
| 5 | Should a tune need a minimum skill? | Yes, with no random failure |
| 6 | Should fittings be available to AI factions' vehicles? | No: they run the light model |

## Who chose what

- **Owner (9 Nov 2026):** that the framework grows into tuning and upgrades.
- **Agent proposals, for the owner to revise:** everything else here.

## Sources

- SmashPhil, Vehicle Framework 1.6 (`.local/decompiled/VehicleFramework-1.6/`) and its `develop` branch, MIT; read for research, not copied.
- Our records: [vehicle health, slice 3](vehicle-health-consequences.md), [utility vehicles](utility-vehicles-design.md).
