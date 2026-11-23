# Decisions: engines and drivetrains

**Status (23 Nov 2026):** living; harvested from the records' "Who chose what", owner's decisions and answered questions.
One row a decision. How it works now: [the system page](../systems/engines-and-drivetrain.md). Dates are 2026. **Who**:
owner; owner's card (a decision card answered); agent default or agent choice (the owner may revise either). A later row that
overturns an earlier one says so.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 8 Nov | Rpm and a semi-realistic drivetrain, if cheap | owner | More realism | [dt], Who chose what |
| 8 Nov | Work back from speed (quasi-static); only ever cap speed | agent choice | Stable, cheap | [dt], Read this first |
| 8 Nov | Two clocks: action for speed, day for fuel and heat | agent choice | Speeds look right | [dt], Questions |
| 8 Nov | Nothing of the drivetrain is saved | agent choice | Derived state is never saved | [dt], Read this first |
| 8 Nov | Without grip, crawl at 15 %; never stop | owner | Stopping strands pathing | [dt], As built |
| 8 Nov | Fuel follows the model: off (overturned 18 Nov) | owner | Keep authors' balance | [dt], As built |
| 8 Nov | Kerb mass 12 kg per Mass unit, overridable | agent choice | A starting figure | [dt], Questions |
| 8 Nov | Plain mode: only "Engine labouring", "Wheels spinning" | agent choice | Detail is for Operator | [dt], Questions |
| 8 Nov | The cap: a first-priority postfix on path cost | agent choice | VVE brakes on it | [dt], As built |
| 8 Nov | Tracked vehicles found by a part key ending Track | agent choice | Two lack a Wheel tag | [dt], As built |
| 8 Nov | Turbines: Harrier, Cherokee, M1A2 | owner | Asked for by name | [tu], Who chose what |
| 8 Nov | Too heavy or hot to hover: a setting, runway by default | owner | "Offer configurable settings" | [tu], As built |
| 8 Nov | Harrier water from Dubs Bad Hygiene, else free | owner | Plumbing where it exists | [tu], As built |
| 8 Nov | Turbines keep the mods' fuel figures (overturned 18 Nov) | owner | As the drivetrain | [tu], As built |
| 8 Nov | A second engine role only in named loadouts | agent choice | Most twins already have two parts | [tu], As built |
| 8 Nov | A hot start costs hot-section life, not damage | agent choice | Warn before harm | [tu], Questions |
| 8 Nov | Flight phases read by name; no optional assemblies | agent choice | No references | [tu], As built |
| 8 Nov | Bleed air, provided performance holds | owner | Bleed heats a real cockpit | [tu], Bleed air |
| 8 Nov | Bleed raises demand, not cuts thrust | agent choice | NASA TM-104021 | [tu], Bleed air |
| 8 Nov | Many engines, rotor drives, pods, diesel-electric | owner | More vehicles | [pl], Who chose what |
| 8 Nov | Boats, the Warbird and the Smuggler left out | owner | Nothing more to model | [pl], Who chose what |
| 8 Nov | Lift-off check joins the hover setting; figures authored | agent choice | One question | [pl], Who chose what |
| 8 Nov | Real figures for military vehicles, the L-ATV first | owner | Realism where sources exist | [rf], Who chose what |
| 8 Nov | The L-ATV at 370 hp (276 kW), mid-range | owner | Oshkosh publishes none | [rf], Who chose what |
| 8 Nov | Final drives from each real top speed | agent choice | Few ratios are published | [rf], Who chose what |
| 8 Nov | A boiler for steam locomotives; water as the Harrier's | owner | They had no systems | [st], Who chose what |
| 8 Nov | Boiler explosions allowed, off, with a warning | owner | Real, but drastic | [st], Who chose what |
| 8 Nov | Water stops draw a tenth; a realistic setting | owner | Fits one water tower | [st], Who chose what |
| 8 Nov | Plug on the Tommy, not the Frisco; explosion size | agent choice | UK and US practice | [st], Who chose what |
| 8 Nov | Electric systems; the Lightning as test, by setting | owner | None in the list | [ev], Who chose what |
| 8 Nov | Battery fires a setting, off by default | agent default | As boiler explosions | [ev], Who chose what |
| 10 Nov | An engine sound that follows rpm and load | owner | Fixed loops | [sc], Read this first |
| 10 Nov | Accessories load the engine; a setting, on | owner | Cross-link 5 | [cl], The owner's answers |
| 12 Nov | An electric night heater | owner | Idea I5 | [ev], The night heater |
| 18 Nov | Fuel follows engine load, on (overturns 8 Nov) | owner | Over a smaller step | [lo], Decisions (1) |
| 18 Nov | Drag: one fixed figure per class | owner | Exception to no drag | [lo], Decisions (2) |
| 18 Nov | Work loads join the ledger, from real power | owner | More realistic | [lo], Decisions (3) |
| 18 Nov | Generator set keeps its fuel; real figure noted | owner | Real figure too dear to play | [lo], Decisions (4) |
| 18 Nov | Idle at a quarter of cruise accepted | owner | The model's result | [lo], Decisions (5) |
| 18 Nov | Air-brake compressor on the ledger | owner | 8 % of cruise fuel | [lo], Decisions (6) |
| 18 Nov | Alternator efficiency 0.55, falling with speed | owner | Remy's typical figure | [lo], Decisions (9) |
| 18 Nov | Step back first: one engine state, one fuel service | owner | Seams before loads | [lo], Decisions (10, 11) |
| 18 Nov | Every load at once | owner | Against a first slice | [lo], Decisions (13) |
| 18 Nov | Rated power is net | agent default | SAE J1349, reported | [lo], The rule |
| 18 Nov | Loads as a Def type, not a loadout list | agent choice | Any mod adds one | [lo], The ledger as it stands |
| 18 Nov | Turbine gaps and altitude in one slice | owner | "Test when more complete" | [at], The owner's decisions |
| 18 Nov | Aircraft fuel follows the engines, everywhere | owner | Against world flight only | [at], The owner's decisions |
| 18 Nov | One air figure for all engines; settings on | owner, agent default | One rule | [at], The owner's decisions |
| 18 Nov | Read the tank, not the frameworks' rules | agent choice | Never charge twice | [at], The owner's decisions |
| 18 Nov | Thin air spares other factions | agent default | No drivetrain there | [at], The owner's decisions |
| 20 Nov | A flight mod's rate carried by our rate factor | owner | Any upgrade | [at], 20 Nov updates |
| 23 Nov | Loads round two: alternator, electronics, idle-up | owner | "#1 through #4" | [lo], Second round |
| 23 Nov | Electronics standing; no idle-up on turbines | agent default | Keep the cruise rate | [lo], Second round |
| 23 Nov | Round three: sized AC, electronics by type, dim lamps | owner | "#1 through #5" | [lo], Third round |
| 23 Nov | Hills climb then descend; brake fade a setting, on | owner's card | Slope and roughness | [we], Descents as built |
| 23 Nov | Power by health, wear per engine, yaw on twin jets (widened 23 Nov) | owner's card | Four ideas | [pe], Read this first |
| 23 Nov | Damaged engine gives half to all; out at 20 % | agent default | Authored shape | [pe], Step 1 |
| 23 Nov | Lopsided thrust burns fuel (third drag exception) | owner's card | Trim is drag | [pe], Round two |
| 23 Nov | Engine-out ratings, start in turn, grit, side tanks | owner's card | The smaller ideas | [pe], Round two |
| 23 Nov | Engines lit 0.1 h each; not jets that spool | agent choice | Game-scaled | [pe], Step 9 |
| 23 Nov | Cross-feed automatic, or open, or shut | owner's card | Choice when holed | [pe], Step 11 |

[dt]: ../archive/vehicle-drivetrain-design.md
[tu]: ../archive/vehicle-turbine-design.md
[ev]: ../archive/vehicle-electric-design.md
[st]: ../archive/vehicle-steam-design.md
[lo]: ../archive/vehicle-engine-loads.md
[pl]: ../archive/vehicle-power-and-lift-design.md
[rf]: ../vehicle-real-figures-design.md
[at]: ../vehicle-altitude-and-turbines.md
[pe]: ../per-engine-realism.md
[we]: ../world-elevation-and-descents.md
[sc]: ../sound-catalogue.md
[cl]: ../vehicle-crosslinks.md
