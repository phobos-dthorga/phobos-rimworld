# Vehicle health, slice 3: what it does to the player (8 Nov 2026, built 9 Nov 2026)

**Status: built in Framework 0.36.0 and Vehicles 0.14.0, on the owner's go of 9 Nov 2026. Not yet tested in game, nor is slice 2 it builds
on.** The answers to its questions are in [Decisions](#decisions-9-nov-2026). Findings marked "from the code" are read from our decompile of Vehicle Framework
1.6 (the build the 1.7 game loads) and summarised in our own words; what this record says will happen in play is a proposal.

This record answers the project's rule that every new behaviour says, before it is built, what it lets the player do, what it stops
them doing and how it changes the way they play. The architecture is settled in [the vehicle health design](vehicle-health-design.md).

## Read this first

- **Built:** the danger clock, destroyed parts needing supplies, one serious condition needing a service, and field fixes. **Permanent
  damage is left out for now, and wrecks wait until Vehicle Framework 1.7 ships.**
- **The player sets the prices** in the mod settings: engines and transmissions at 100% of the whole part by default, everything else
  at 50% of its materials (agent default for the 50%).
- **Every vehicle from every mod is affected** by destroyed parts needing supplies, so it stays behind a setting (on by default, the
  owner's decision of 8 Nov 2026).
- **Other factions' vehicles are left out** of all of this: they run the light model, and a raider must never be stranded by it.

## What each behaviour does to the player

| Behaviour | What it lets them do | What it stops them doing | How it changes the way they play |
| --- | --- | --- | --- |
| **Danger clock** | See how long a damaged vehicle has before something worse happens ("boils over in about 2 hours") | Nothing | Damaged vehicles are triaged like patients: which can wait, which must stop now |
| **Destroyed parts need supplies** | Rebuild a part at 0% with a real replacement, from whichever mod provides it | Rebuild a destroyed part from nothing, anywhere, for free. Ordinary damage stays free | Spare parts become a stock worth making and carrying; losing a part in the field has a cost that outlasts the fight |
| **Serious conditions need a service** | Have a mechanic properly fix the worst faults | Clear them with an ordinary repair | The worst faults mean going back to a place with supplies, so overworking an engine has a lasting price |
| **Field fixes** | Slow a leak with a component or a spare, away from any base, so the vehicle can limp home | Nothing: the fix is optional. It never repairs the part | A convoy carries a small kit; a shot-up radiator becomes "drive gently for a few hours" rather than "abandon the truck" |
| **Permanent damage** (recommended out for now) | Nothing new | Have a badly damaged vehicle made good by ordinary repairs | Old vehicles become visibly worn and carry a history; rebuilding one is a project |
| **Wreck on death** (separate, not now) | Salvage something from a destroyed vehicle | Nothing | A lost vehicle stops being a total loss |

## How each would work

### Danger clock

A line under the condition on the Health tab: what happens next and roughly when, worked out from the rates already running (a leak
draining coolant toward the level where the engine overheats). Plain mode says "about 2 hours", Operator and Engineer the figures.
It is worked out only when the tab's rows are rebuilt, so it costs nothing while the tab is closed. The core's `ConditionDangerRule`
was written for patients and reads death rolls, so the projection is a new game-free rule beside it, with tests. **It is an estimate**:
the rates change if the vehicle stops or the weather turns, and the line should say "about".

### Destroyed parts need supplies

From the code: Vehicle Framework's repair job (`JobDriver_RepairVehicle`) heals the first damaged part by a rate, with no materials,
and a caravan repairs one part every 300 ticks. Two other mods already postfix that job's `WorkComplete`; nobody patches its work giver.

Proposed: a part at 0% is skipped by the repair job until its supplies are in the vehicle's cargo, the way a service already waits for
them, and using them brings the part back to a small share of health, from which ordinary free repair continues. The inspect text and
the tab name what it waits for. The caravan path does the same.

Supplies come **from providers by role**, as [the utility vehicles' table](utility-vehicles-design.md) already sets out (RimThunder
for engine, transmission, fuel tank and running gear; Vanilla Vehicles Expanded for electrical and cooling; our fallback parts when
neither is present), named in XML and resolved through `IfModActive` folders, never in C#. A part with no role (most of an imported
vehicle's armour and hull) costs steel and components in proportion to its health.

**The garage** (Framework 0.31.0) repairs from its own stores and does not yet know about this; it must apply the same rule, or
parking a vehicle becomes the way round it.

### Serious conditions need a service

The mechanism exists: a condition with `clearsOnRepair` off stays until a service listing it in `clearsConditions`. The proposal is to
convert one condition first, **the warped cylinder head at its "badly" stage**, because a head that is badly warped is not put right by
patching the engine. A system has one service today, and the cooling system's is the coolant top-up, so this needs either a second
service per system or a small engine-overhaul system of its own (agent's preference: the small system).

### Field fixes

A job a colonist does at the vehicle, using a component or a provider's spare from the cargo: the condition's stage drops by one, or
its rate halves, until the part is properly repaired. It never restores health, and a second fix adds nothing. It works in a caravan
at rest. It has its own button with its own icon, and the button says why it is unavailable.

### Permanent damage (recommended out for now)

A lasting efficiency loss after severe structural hits, until a rebuild service, using the core's `LastingRule`. **Agent's candid
view:** it adds a slow penalty the player mostly notices as a smaller number, on top of supplies, services and field fixes that already
make damage matter. Worth reconsidering after play shows whether damage matters enough.

### Wreck on death (not now)

From Vehicle Framework's development branch (read 8 Nov 2026): its `Destroy` now drops the game's normal leavings. Today nothing is
left when a vehicle dies. Better to see what 1.7 does before building on it.

## Cost and risk

- **Performance:** all of it runs on hits, repairs, jobs and the tab's rebuild. Nothing runs every tick, inside the budget of the
  [vehicle health design](vehicle-health-design.md) (0.05 ms a tick for the whole vehicle model). Still unmeasured: H4 is open.
- **Combat Extended** writes part health directly, so "destroyed" is read from the part's health at the sweep and the repair job, not
  from an event.
- **A vehicle stranded for want of a part** is the main risk. Field fixes, and the fact that only destroyed parts cost anything, are
  the answer; a part that stops the vehicle moving is the case to watch in play.
- **Vehicle Framework 1.7 may change the repair job.** Every touch of it stays in one class.

## Decisions (9 Nov 2026)

The owner: "proceed with your H3 proposal in full", so the agent's recommendations below stand, with these additions.

- **Cost:** the whole part for engines and transmissions by default, a share of the cost for everything else, **both configurable in the
  mod settings** (owner).
- **Our own framework of parts and supplies** (owner): our parts always exist and set the cost; other mods' are accepted as substitutes
  **only where it makes sense**. This reverses "parts come from providers".
- **The framework should grow into tuning and upgrades** (owner: "one of possible tuning and full of upgrades as well"). **Not built
  yet:** it needs its own record of what each lets the player do, stops and changes, before any of it is written.

### Calibration after the owner's test (9 Nov 2026, Framework 0.53.0)

The owner's screenshots of two wrecked raider vehicles matched the model to the percent, and showed the rules were too gentle: "the damage is not being interpreted by the
coolant and lubricant systems as harshly as it could be". The owner agreed every figure below. All are authored; not yet tested in game.

| Rule | Was | Now |
| --- | --- | --- |
| Engine role | an internal part carrying move speed, so six Vanilla Vehicles Expanded designs whose engine is marked External bound a suspension or a gearbox | a part carrying move speed and named engine comes first |
| Radiator hit, coolant lost | 1.5% per point (at most 45% on 30 points) | 90% of the share of health lost |
| Engine hit, coolant lost | 0.5% per point | 30% of the share lost |
| Oil pump hit, oil lost | 1% per point (at most 20%) | 25% of the share lost |
| Engine hit, oil lost | 0.4% per point | 60% of the share lost |
| Cracked sump | nothing went on leaking | a condition on the engine: 1%, 5% or 20% of the oil an hour; field-fixable |
| No oil pressure, under 15% | 2 points an hour, as low pressure | 20% of the engine's health an hour |
| Boiling over | 6 points an hour | 15% of the engine's health an hour |

**Lets the player:** read a shot-up vehicle's state from its gauges, and race a leak home. **Stops them:** driving a holed engine for a day. **Changes play:** after a fight, a patch kit and oil decide
whether a vehicle gets home. **Left as it was:** other factions' vehicles still do not evolve after a hit, so a raider's "pouring out" never pours (cost); the setting for the full model on every faction changes that.

### As built

| Piece | Where | Note |
| --- | --- | --- |
| Our parts | `Parts.xml` in the Framework | Engine assembly, turbine assembly, transmission, and the two kits moved from D'thorga Vehicles |
| What each role costs | `PartSupplies.xml`, `PhobosVehiclePartSupplyDef` | The first Def that fits wins; substitutes filtered by empty mass and by turbine or not (authored) |
| Free repair leaves destroyed parts alone | Two postfixes on `VehicleStatHandler` | It leaves them for the job that fits a new part, on the map and at a garage |
| Fitting a part, a serious service, a field fix | `VehicleRepairs`, one job (Construction) | Supplies are hauled to the cargo as a service's are; another mod's part is hauled when ours cannot be found |
| In a caravan | A button per vehicle, at a halt | Done at once: an authored simplification, as the caravan jump-start is |
| The garage | Skips destroyed parts and charges nothing for them | **Limit:** it cannot fit new parts, so the vehicle has to come home for that |

## Questions for the owner (answered)

| # | Question | Agent's recommendation |
| --- | --- | --- |
| 1 | How dear is a destroyed part: the provider's whole part (a RimThunder heavy engine is 4 heavy castings and 12 components), or a cheaper repair kit? | The whole part for engines and transmissions, because the owner wants provider parts to matter; a share of the part's cost for everything else |
| 2 | Should a destroyed part with no role (hull, armour plates) need supplies at all? | Yes, but only steel and components, in proportion to its health |
| 3 | The danger clock: only on the Health tab, or also an alert when a player's vehicle has under a set time left? | Tab first; add an alert only if play shows it is missed |
| 4 | Which conditions count as serious, beyond the badly warped head? | Only that one until play shows more are needed |
| 5 | Field fixes: one job for every condition, or only leaks? | Only leaks and shorts at first: a radiator and a battery are what a field fix can honestly do |
| 6 | Permanent damage: leave out as recommended? | Leave out for now |

## Who chose what

- **Owner (8 Nov 2026):** destroyed parts and serious conditions need supplies, behind a setting, on by default; parts come from
  providers by role; the architecture of the condition layer.
- **Agent proposals, for the owner to revise:** everything in the behaviour table's wording, the order, the warped head as the first
  serious condition, a small engine-overhaul system, and every recommendation above.

## Sources

- SmashPhil, Vehicle Framework 1.6 (`.local/decompiled/VehicleFramework-1.6/`) and its `develop` branch, MIT; read for research, not copied.
- Our records: [the vehicle health design](vehicle-health-design.md), [utility vehicles](utility-vehicles-design.md),
  [the garage](vehicle-garage-design.md).
