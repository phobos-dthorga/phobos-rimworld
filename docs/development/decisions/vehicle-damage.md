# Decisions: vehicle damage

**Status (23 Nov 2026):** living; harvested from the records' "Who chose what", owner's decisions and answered questions.
One row a decision. How it works now: [the system page](../systems/vehicle-damage.md). Dates are 2026. **Who**:
owner; owner's card (a decision card answered); agent default or agent choice (the owner may revise either). A later row that
overturns an earlier one says so.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 8 Nov | Pawn-like health for player vehicles; performance the one hard limit | owner | Detail "most extreme" | [he], Read this first |
| 8 Nov | A condition layer on VF's parts; the pawn hediff stack rejected | owner | VF's part health one authority | [he], Read this first |
| 8 Nov | Our Health tab replaces VF's, the first deliverable | owner | Show all damage, as a pawn's | [he], Read this first |
| 8 Nov | Other factions run a lite tier; full by setting (overturned 11 Nov) | owner | Diffuse for NPCs | [he], Read this first |
| 8 Nov | Destroyed parts and serious conditions need supplies; setting, on | owner | Ordinary damage stays free | [he], Who chose what |
| 8 Nov | Parts come from providers by role (overturned 9 Nov) | owner | Provider parts matter | [he], Who chose what |
| 8 Nov | No second engine: conditions in the systems' vocabulary | agent choice | One schema, one checker | [he], Read this first |
| 8 Nov | Whole model 0.05 ms a tick; Health tab 0.25 ms a pass | agent choice | 36 to 50 TPS, no headroom | [he], Performance |
| 8 Nov | A setting keeps VF's tab; VF's stat rows kept in ours | agent default | Owner to revise | [he], Who chose what |
| 8 Nov | Conditions clear at full health only; effects read their system | agent choice | Owner to revise | [he], Who chose what |
| 8 Nov | First conditions (radiator, battery, head), saved by part key | agent choice | Authored; VF saves by index | [he], Slice 2 as built |
| 9 Nov | Slice 3 "in full": danger clock, supplies, a service, field fixes | owner | "Proceed with your H3 proposal" | [he], Decisions |
| 9 Nov | Engines, gearboxes a whole part; others 50 %; both settings | owner, agent default | The 50 % the agent's | [he], Decisions |
| 9 Nov | Our own parts set the cost; others' where sensible (overturns 8 Nov) | owner | Our framework of parts | [he], Decisions |
| 9 Nov | Danger clock on the tab only; warped head the one serious condition | owner | Until play shows more | [he], Questions for the owner |
| 9 Nov | Field fixes for leaks and shorts only; permanent damage left out | owner | What a field fix can do | [he], Questions for the owner |
| 9 Nov | Supplies and services spare other factions | agent choice | Never strand a raider | [he], Read this first |
| 9 Nov | Free repair skips destroyed parts; a caravan fits at once | agent choice | Authored simplification | [he], As built |
| 9 Nov | Harsher leaks: radiator coolant 90 % of health lost; cracked sump | owner | "Not ... as harshly" | [he], Calibration after the owner's test |
| 9 Nov | Water and fuel pumps added; no fan, thermostat or heater part | agent choice | No design had them | [he], Parts audit |
| 10 Nov | Patch VF's undying vehicle fires ourselves | owner | "Please implement a fix" | [vf], Who chose what |
| 10 Nov | Fires out on despawn; surplus fires go each rare tick | agent choice | As the game for containers | [vf], The fix |
| 10 Nov | Fire out in an hour: hot, unharmed; left burning: boils over | owner | The agent's lean | [cl], The owner's answers |
| 10 Nov | Fire cooks the radiator, half its health an hour; biggest fire read | agent choice | Heat alone cannot meet it | [cl], Order to build |
| 11 Nov | Other factions always lite; full-model setting gone (overturns 8 Nov) | owner | Other-factions audit | [he], Read this first |
| 13 Nov | Our conditions may scale others' figures by non-skipping hooks | owner | Never skip their code | [ko], Read this first |
| 13 Nov | Cook-off now, on, only where CE's ammunition allows | owner | Against later and off | [cl], Questions for the owner |
| 13 Nov | Fire reaches tyres and battery, in the first batch | owner | A quick win | [cl], Questions for the owner |
| 16 Nov | Only parts doing a job count toward destruction | owner | "Critical ... still remain" | [he], Our parts and a vehicle's destruction |
| 16 Nov | Losing a part or its feed costs the vehicle that job | owner | "Lose hydraulic pressure" | [ko], Who chose what |
| 16 Nov | A powered turret without power is hand-cranked, never dead | owner | As the Bradley's manual | [ko], Questions for the owner |
| 16 Nov | Power and pressure act on the player's only; raiders by damage | owner | The lite tier stays | [ko], Questions for the owner |
| 16 Nov | The turret blown off, with cook-off | owner | Follows its setting | [ko], Questions for the owner |
| 16 Nov | AI crews bail out; the player's get an alert | owner | Ends the dead hull | [ko], Questions for the owner |
| 16 Nov | A weapon stops when its mount is under 10 % | owner | A repair, not a new part | [ko], Questions for the owner |
| 16 Nov | No random jams, cargo-hook release or crew morale | agent choice | Rolls nobody can act on | [ko], Proposals |
| 16 Nov | Seated crew already hurt by cab hits: nothing to build | agent choice | VF and CE both do it | [cl], 3. Combat damage |
| 16 Nov | Cook-off a rare-tick service, so any faction's vehicle | agent choice | The lite tier runs no stages | [cl], Built in Framework 0.113.0 |
| 17 Nov | Build the knock-out short list, steps 1 to 6 | owner | "#1 through #6" | [ko], As built |
| 17 Nov | One rules Def holds every knock-out figure; no settings | agent choice | Owner to revise | [ko], As built |
| 17 Nov | Mount: own turret part, else a ring (60) or pylons (40); 0.55 pace | agent choice | VF's own requirement | [ko], As built |
| 17 Nov | A lost track stops all speed; VF's priority flag not used | agent choice | VF averages priority parts | [ko], As built |
| 17 Nov | Turret drive 40 A, shed last; cranked traverse 0.2, aim 0.5 | agent choice | Authored | [ko], As built |
| 17 Nov | Sights with the aircraft batch; field re-fit to a quarter | owner | One aiming hook; limp home | [ko], The owner's answers on the open rows |
| 17 Nov | A re-fitted part stays out of free repair until replaced | agent choice | A component is not a track | [ko], The owner's answers on the open rows |
| 17 Nov | Smoke and heat in the cab wait for hotboxing's cab air | owner | Needs a cab-air model | [ko], The owner's answers on the open rows |
| 17 Nov | No turret drive on aircraft | owner | "Nobody hand-cranks a Hind" | [ko], Weapon mounts audit |
| 17 Nov | Drive by hints: coaxial, mount words, the crew who works it | agent choice | Seven riding guns hand-swung | [ko], Weapon mounts audit |
| 17 Nov | Combat damage: a holed tank, shorted wiring | owner's card | The whole group | [cl], Built in Framework 0.123.0 |
| 17 Nov | Holed tank only where VF gave no leak reactor | agent choice | Never charge twice | [cl], Built in Framework 0.123.0 |
| 18 Nov | Combat Extended first; VF the fallback | owner | "When we finally publish" | [ar], Who chose what |
| 18 Nov | Aircraft aloft hit and shoot as from the ground, first build | owner | Real height costs every bullet | [ar], In the air |
| 18 Nov | Armour by class, named by the threat stopped | agent choice | Sourced threat, authored mm | [ar], Read this first |
| 18 Nov | CE patch per vehicle and a gate check; blunt and VF figures authored | agent choice | No source gives MPa | [ar], How a Def carries it |
| 18 Nov | One outer part to a cell; no patch | agent choice | CE stacks outer parts | [ar], One outer part to a cell |
| 19 Nov | Every truck part Skin; five trucks only; wider target now | owner's card | No source: trucks stop bullets | [ar], Status |
| 19 Nov | Classes as Defs; CE's hit-box callback; our outer parts inside | agent choice | No Harmony; no stacking | [ar], Status |
| 19 Nov | Kit weight as cargo, real kg: engine always, cargo room Talus only | owner's card | Others have 30 to 60 kg | [ms], The owner's answers |
| 19 Nov | EMP hardening a kit of its own, not from a gun | owner's card | Against the agent's lean | [ms], Questions for the owner |
| 19 Nov | Protection first; other factions' kits later, setting off | owner's card | Player's side tested first | [ms], Questions for the owner |
| 19 Nov | A stranded crew stays while a gun works, any faction (not built) | owner's card | Beyond the agent's lean | [ko], M4 read |
| 19 Nov | Armoured trucks keep working near enemies: a switch, off | owner's card | — | [ms], The owner's answers |
| 19 Nov | Kits raise parts by Add only; none without a cost | agent choice | Health upgrades heal at load | [ms], Argued against |
| 19 Nov | Kit classes and weights: armoured cab Rifle, 1,000 kg | agent choice | 7.85 kg a m² per mm | [ms], The kits |
| 19 Nov | Hardening read per burst; VF's EMP step run under CE | agent choice | CE skipped EMP | [ms], The kits |
| 20 Nov | Our breakdowns may end in the Harrier's own crash; no floor yet | agent choice | Real outcome; to watch | [ko], Their 20 Nov 2026 updates |
| 21 Nov | Spall: a record; CE first, nothing without it; vehicles, not people | owner | "More difficult without CE" | [sp], Who chose what |
| 21 Nov | No armour layers of ours; spall by reactors, no Harmony | agent choice | CE resolves layers already | [sp], Read this first |
| 21 Nov | Spall graded moderate, on | owner's card | Agent: high, off | [sp], The owner's answers |
| 21 Nov | Every vehicle with Rifle-class plates or more, own setting | owner's card | — | [sp], The owner's answers |
| 21 Nov | Shock spall from heavy hits | owner's card | Agent: leave it out | [sp], The owner's answers |
| 21 Nov | Liner needs the cab, 200 kg; a part that wears; panels re-line | owner's card | Plates stay as they are | [sp], The owner's answers |
| 21 Nov | Shock spall a quarter of the blast; lined spall to a random cell | agent choice | CE: plates shrug blasts | [sp], As built |
| 21 Nov | A re-line waits until a quarter is gone | agent choice | Authored | [sp], What the fault-read changed |
| 21 Nov | A spent-liner alert and logbook entries | owner | The colony is told | [sp], What the fault-read changed |
| 22 Nov | Strikes on vehicles: research and record only | owner | "Strictly" | [nk], Who chose what |
| 22 Nov | A vehicle's own route through a strike, as one piece | owner's card | Ours alone; each part small | [nk], The owner's answers |
| 22 Nov | Graded with the strike itself, on | owner's card | Agent: high by wording | [nk], The owner's answers |
| 22 Nov | People aboard hurt; their dose Rimatomics', cut by the hull | owner's card | Until our own model | [nk], The owner's answers |
| 22 Nov | No reports to other authors | owner's card | — | [nk], The owner's answers |
| 22 Nov | Weight decides: wrecked at severity 1, battered from 0.02 | agent choice | Davidson's tank figures | [nk], As built |
| 22 Nov | Blasts set part health through a core hook; no reactor | agent choice | Not a round: no shock spall | [nk], As built |
| 22 Nov | Pulse only on a running engine, by its EMP rating | agent choice | Real tests | [nk], As built |
| 22 Nov | Overpressure from every large explosion argued against | agent choice | Too wide a change to CE | [nk], D. The bigger problem |
| 22 Nov | Glass a vehicle part with knock-on effects; record only | owner | "So much work with glass" | [gl], Who chose what |
| 22 Nov | One glass part per vehicle, inner, broken by our rule | agent choice | An outer part stacks in CE | [gl], Read this first |
| 22 Nov | Glass base, splinters, driver's view, canopy; real art first | owner's card | The agent's options | [gl], The owner's answers |
| 22 Nov | A real glass chain, the owner steering the stand-ins | owner's card | Agent: Glass+Lights' glass | [gl], The owner's answers |
| 22 Nov | Sandstone silica, limestone, potash (soda with VCE), film | owner's card | Works with no other mod | [gl], The glass chain: the owner's answers |
| 22 Nov | Glass melted in the game's electric smelter | owner's card | Agent: a furnace of ours | [gl], The glass chain: the owner's answers |
| 22 Nov | Chain art overnight (overturned 22 Nov) | owner's card | — | [gl], The glass chain: the owner's answers |
| 22 Nov | Chain items made paid (overturns 22 Nov) | owner | In-world art is paid | [gl], The glass chain: the design |
| 22 Nov | Chain yields authored: 7 sand, 2 alkali, 2 lime to a pane | agent choice | Real batch, ballpark | [gl], The glass chain: the design |
| 22 Nov | Fire on vehicles: research and record | owner | Strike record's NK20 | [fi], Who chose what |
| 22 Nov | Fix unseen fires on vehicles now | owner's card | A fault of ours | [fi], The owner's answers |
| 22 Nov | Wear by fire size, more parts burn, raiders' vehicles burn | owner's card | All three | [fi], The owner's answers |
| 22 Nov | Fireproof designs: ground fire wears soft parts only; setting | owner's card | Authors' flammability stays | [fi], The owner's answers |
| 22 Nov | Extinguishing under CE fixed; an extinguisher designed | owner's card | — | [fi], The owner's answers |
| 22 Nov | A suppression discharge harms no crew | agent choice | Halon safe at used levels | [fi], FR10(b): automatic suppression, an upgrade |
| 23 Nov | Extinguisher: three charges, refilled by a mechanic at the vehicle | owner's card | Not like the flares | [fi], The four questions |
| 23 Nov | Suppression: crew and engine nodes; others' armoured vehicles too | owner's card | As real vehicles; "Both?" | [fi], The four questions |
| 23 Nov | Fire zones with spread; the button sends the nearest colonist | owner | "Not too much of a ... concern" | [fi], The four questions |
| 23 Nov | Zone spread 0.4 h and 0.25 h; armoured from 0.8 sharp | agent choice | Authored | [fi], What was built |
| 23 Nov | Second fire round: leaks, crews out, tyres, hulls, glass | owner's card | "FR4 + FR11 + FR13 ..." | [fi], What was built in the second round |
| 23 Nov | Off: leak-fed fire, tyre bursts; on: crews out, tyres relight | agent default | By risk to people | [fi], What was built in the second round |
| 23 Nov | Burnt-out hull gives half salvage; no blackening | agent choice | Needs a draw patch or art | [fi], What was built in the second round |
| 23 Nov | Correct the fires record's refuel cause | owner's card | Untrue to VF's author | [vf], Who chose what |
| 23 Nov | Others' vehicles marked lined; the Bradley, not the Abrams | owner, agent choice | Abrams sources conflict | [sp], As built |
| 23 Nov | Glass 40 health, cracked under 70 %; blasts reach 2.5 radii | agent choice | Works without CE | [gl], The glass part, as built |
| 23 Nov | Splinters the game's cut, not More Injuries' | agent choice | No dependency | [gl], The glass part, as built |
| 23 Nov | Glass and splinter settings, both on | agent default | Low and moderate grades | [gl], The glass part, as built |
| 23 Nov | Cracked-glass overlays: one vehicle as a pilot first | owner's card | — | [gl], The glass chain: the design |
| 23 Nov | Cracks as marks over the drawn glass, not pasted panes | owner | "Reassess our entire approach" | [gl], The glass part, as built |
| 23 Nov | Both tabs read one service; numbered roles join their base | owner | "Genericize how these talk" | [he], The two tabs, one answer |
| 23 Nov | The Cherokee's two turboshafts stay its engines | owner | NASA, DTIC sources | [he], The two tabs, one answer |
| 23 Nov | A windscreen kind: open vehicles keep their glass part | owner | The open-top Humvee had full glass | [gl], The glass part, as built |
| 23 Nov | All seats exposed counts as open; aircraft left out | owner | Canopies cover aircraft seats | [gl], The glass part, as built |

[he]: ../archive/vehicle-health-design.md
[ko]: ../archive/vehicle-knocked-out-systems.md
[sp]: ../archive/vehicle-spall.md
[gl]: ../archive/vehicle-glass.md
[ar]: ../archive/vehicle-armour.md
[fi]: ../archive/vehicle-fire.md
[vf]: ../archive/vehicle-framework-fires.md
[nk]: ../nuclear-strike-effects.md
[ms]: ../vehicle-militarised-service-vehicles.md
[cl]: ../vehicle-crosslinks.md
