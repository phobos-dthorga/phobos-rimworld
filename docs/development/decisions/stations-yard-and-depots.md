# Decisions: stations, yard and depots

**Status (23 Nov 2026):** living; harvested from the records' "Who chose what", owner's decisions and answered questions.
One row a decision. How it works now: [the system page](../systems/stations-yard-and-depots.md). Dates are 2026. **Who**:
owner; owner's card (a decision card answered); agent default or agent choice (the owner may revise either). A later row that
overturns an earlier one says so.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 8 Nov | A garage built on the outposts system | owner | Many more vehicles to handle | [st], Garage: Decisions and who made them |
| 8 Nov | Garage first version: parking and slow repairs | owner | Spares held at chosen places | [st], Garage: Decisions and who made them |
| 8 Nov | Also a fleet board, parked warning, depot bays | owner | Ways to handle a large fleet | [st], Garage: Decisions and who made them |
| 8 Nov | Standing crews and convoys too (overturned 9 Nov) | owner | Crew up in one button | [st], Garage: Decisions and who made them |
| 8 Nov | Garage needs VE Framework and VF, not VOE itself | agent default | The outposts engine is in VEF | [st], Garage: Decisions and who made them |
| 8 Nov | Garage code a Framework tie-in; Def in Vehicles | agent default | Code and balance apart | [st], Garage: Decisions and who made them |
| 8 Nov | A garage parks a whole convoy at a time | agent default | From the code | [st], Garage: Decisions and who made them |
| 8 Nov | Garage bays, repair rate, costs authored in XML | agent default | For the owner to tune | [st], Garage: Decisions and who made them |
| 8 Nov | Patch VEF's convoy fault, with a setting to undo | owner | Possible loss of colony data | [st], Garage: Decisions and who made them |
| 8 Nov | Garage holds vehicles itself, never by AddPawn | agent choice | Outposts refuse vehicles | [st], Hazards, from the code |
| 8 Nov | A garage must be staffed; no pack-up with vehicles | agent choice | An empty outpost deletes itself | [st], The garage |
| 8 Nov | Last person gone: vehicles to a VF stash, a letter | agent choice | Never deleted silently | [st], The garage |
| 8 Nov | Repairs by stationed skill, VF's HealComponent | agent choice | VF's part health the one authority | [st], The garage |
| 8 Nov | Repairs paid in steel and components from stores | agent choice | Stocking a garage is a job | [st], The garage |
| 8 Nov | Parked vehicles frozen, not ticked | agent choice | Costs nothing per tick | [st], The garage |
| 8 Nov | Aircraft and what cannot caravan refused | agent choice | They cannot form a caravan | [st], The garage |
| 8 Nov | Garage stores spent one stack at a time | agent choice | TakeItems fails across stacks | [st], As built (Framework 0.31.0, Vehicles 0.11.0) |
| 8 Nov | No "add a person" button for a convoy at a garage | agent choice | It lists vehicles and fails | [st], As built (Framework 0.31.0, Vehicles 0.11.0) |
| 8 Nov | Fleet board: fuel, worst condition, service, crew | agent choice | Fleet state from one window | [st], Fleet tools |
| 8 Nov | Parked warning: a letter before VF deletes a stash | agent choice | A visible deadline | [st], Fleet tools |
| 9 Nov | Our own Architect build category | owner | Things lost in shared tabs | [st], Who chose what |
| 9 Nov | Named "motor pool", order 23, beside VF's tab | agent choice | One line each to change | [st], Who chose what |
| 9 Nov | The bulk tanker's blueprint moves into it | agent choice | Others' vehicles stay put | [st], How it works |
| 9 Nov | Bills already on a machining table keep working | agent choice | Game reads the list at Add Bill | [st], How it works |
| 9 Nov | A rig off, unpowered or broken does not count | agent choice | It must have power | [st], How it works |
| 9 Nov | Bench and rig art: stand-ins, then owner's picks | agent choice, owner | OpenAI image model | [st], Who chose what |
| 9 Nov | Depot bays: Go to bay, self-refuelling, faster work | owner | Vehicles come home | [st], Depot bays: Who chose what |
| 9 Nov | A vehicle goes to its bay only when told | owner | Auto-return not chosen | [st], Depot bays: How it works |
| 9 Nov | Crews and convoys left to Crew Presets (overturns 8 Nov) | owner | "Responsible for this, not us" | [st], Depot bays: Status |
| 9 Nov | A parts shelf; 2 x 1 bay, outlined pad; one vehicle | agent choice | From "holds its spare parts" | [st], Depot bays: Who chose what |
| 9 Nov | Pad 3 by 10 (overturned 11 Nov) | agent choice | Covers our largest truck | [st], Depot bays: Who chose what |
| 9 Nov | Parking direction not controlled (overturned 11 Nov) | agent choice | Nothing depends on it | [st], Depot bays: How it works |
| 9 Nov | Pump 30 an hour, tank 200; work at 70 % | agent choice | Authored | [st], Depot bays: Who chose what |
| 9 Nov | Bay 100 W, 80 steel and 3 components | agent choice | Authored | [st], Depot bays: Who chose what |
| 9 Nov | Bay pumps chemfuel only, to its own vehicle | agent choice | Others told so on the inspect pane | [st], Depot bays: How it works |
| 9 Nov | Faster work on our jobs only, not VF's free repair | agent choice | VF's repair untouched | [st], Depot bays: How it works |
| 9 Nov | The bay holds the assignment, not the vehicle | agent choice | Safe to add and remove | [st], Depot bays: Cost and risk |
| 10 Nov | Diagnose at the rig, a report letter (S1) | owner | "#1 through #4" | [cl], The owner's answers, and what was built |
| 10 Nov | Diagnose 1,500 ticks; Plain mode in words | agent choice | The rig earns its power | [cl], The owner's answers, and what was built |
| 10 Nov | A part fitted at the rig: 60 % for 0.7 work (S2) | owner | Home is where engines are swapped | [cl], The owner's answers, and what was built |
| 10 Nov | The hoist stays optional; field fitting at 25 % | owner | Requiring it strands vehicles | [cl], The owner's answers, and what was built |
| 10 Nov | Consumables as kits made at the parts bench (S4) | owner | The bench used every week | [cl], The owner's answers, and what was built |
| 10 Nov | Old saves keep raw materials; new colonies use kits | agent choice | Nothing due held up | [cl], The owner's answers, and what was built |
| 10 Nov | The depot bay's shelf takes the four kits | agent choice | Parts stocked where used | [cl], The owner's answers, and what was built |
| 10 Nov | Keep one parts bench, never split (S5) | agent choice | No decision would change | [cl], S5. The parts bench: keep it one bench |
| 10 Nov | Stations differ by job: make, measure, keep | agent choice | Bench, rig, bay | [cl], S5. The parts bench: keep it one bench |
| 10 Nov | No inspection pit (overturned 10 Nov) | agent choice | Would duplicate the bay's bonus | [cl], Stations argued against |
| 10 Nov | A pit as a bench production bonus (overturns 10 Nov) | owner | As Dead Man's Switch does | [cl], The owner's answers, and what was built |
| 10 Nov | Pit +15 % work, one counts; 60 steel, a component | agent choice | Tool cabinet for scale | [cl], The owner's answers, and what was built |
| 10 Nov | Pit picture: attempt 1 | owner | "More flat and top-down" | [cl], The owner's answers, and what was built |
| 10 Nov | No wash bay or paint shop | agent choice | Medical's; VF's patterns | [cl], Stations argued against |
| 11 Nov | Bay pad 3 by 6, nose to the bay (overturns 9 Nov) | owner, agent choice | Pad "brought back a little" | [st], Depot bays: Who chose what |
| 11 Nov | A fuel tank and a pump as parts in the bay's cost | owner | Approved with the parts plan | [st], Depot bays: Who chose what |
| 17 Nov | Heavy overhauls wait for the rig (S3) | owner | "Proceed with your recommendations" | [cl], S3. Serious work waits for the rig |
| 17 Nov | One setting covers tunes and heavy overhauls | agent choice | Off: worked anywhere again | [cl], S3. Serious work waits for the rig |
| 17 Nov | Hoppers: vehicles in, belts out; chunks, no new item | owner | "Aggregate" meant chunks | [uh], The owner's directions (17 Nov 2026) |
| 17 Nov | No parts bins at the depot and dyno bays | owner | Not chosen | [uh], The owner's directions (17 Nov 2026) |
| 17 Nov | The Talus tips into storage (UH1) | owner | Everything else rests on it | [uh], Built (17 Nov 2026) |
| 17 Nov | What full storage refuses stays aboard | owner's card | A mixed load still comes off | [uh], Built (17 Nov 2026) |
| 17 Nov | Tip only into storage of the truck's own side | agent choice | Game's own IsValidStorageFor | [uh], Built (17 Nov 2026) |
| 17 Nov | Bunker shows goods on its cells | owner's card | Universal, and no code | [uh], Questions for the owner |
| 17 Nov | Bunker 3 by 4 at 8 a cell; chunks ticked first | owner's card | One base Talus load, 96 | [uh], Built (17 Nov 2026) |
| 17 Nov | Bunker on tipper research, the motor pool tab | owner's card | As the agent recommended | [uh], Built (17 Nov 2026) |
| 17 Nov | Bunker 120 steel, 3,000 work; priority Normal | agent choice | Authored | [uh], Built (17 Nov 2026) |
| 17 Nov | Bunker has hopper marks, not the grinder's | agent choice | The grinder reads one cell | [uh], Built (17 Nov 2026) |
| 17 Nov | Bunker art: stand-in, then OpenAI attempt 1 | owner | The owner's pick | [uh], Built (17 Nov 2026) |
| 17 Nov | Load-out switch on every storage, off (UH12) | owner's card | The grab never empties stockpiles | [uh], Built (17 Nov 2026) |
| 17 Nov | Load-out in the Framework's vehicle part | agent choice | A dead button without VF | [uh], Where it would live |
| 17 Nov | Load-out and chute serve only their own faction | agent default | Raiders never loaded | [uh], Other factions' vehicles |
| 17 Nov | Build the loading chute (UH13) | owner's card | Second round | [uh], Built (17 Nov 2026) |
| 17 Nov | Chute: one cell, 4 stacks, empty filter, Low | agent default | Haulers never fill it unasked | [uh], Built (17 Nov 2026) |
| 17 Nov | Chute: tipper research, 30 steel; not a hopper | agent default | Belts never take from it | [uh], Built (17 Nov 2026) |
| 17 Nov | Belt's end by a parked Talus: a test row (UH11) | owner's card | May already load it | [uh], Questions for the owner |
| 17 Nov | Tanker's cans into storage beside it (UH5) | owner's card | Built though ranked low | [uh], Built (17 Nov 2026) |
| 17 Nov | Drill rig output into storage within 3 cells (UH3) | owner's card, agent default | A rig site as a mine head | [uh], Built (17 Nov 2026) |
| 17 Nov | Fuel by belt a guide note only (UH9) | owner's card | A belt never reaches a vehicle | [uh], Built (17 Nov 2026) |
| 17 Nov | No tipping straight onto belts | agent choice | 96 belt cells; error storms | [uh], Argued against |
| 20 Nov | Vehicles taken to a dyno bay for repair unasked | owner | Broken vehicles littered everywhere | [bt], Who chose what |
| 20 Nov | Bay trips need no other mod | owner | Works on any mod list | [bt], Who chose what |
| 20 Nov | What sends one: three levels, "broken or bay-only" | owner's card | Routine work stays in place | [bt], What sends a vehicle [O] |
| 20 Nov | Every land vehicle; a Fleet board switch to keep out | owner's card | One off switch each | [bt], Which vehicles [O] |
| 20 Nov | Wrecks towed, or pushed by hand when light | owner's card | "Tow, or push light ones by hand" | [bt], A vehicle that cannot drive (batches 3 and 4) |
| 20 Nov | Afterwards: depot bay, then a vehicle park, nearby | owner's card | Our own park zone | [bt], Who chose what |
| 20 Nov | Only work that can be paid for sends a vehicle | agent choice | Wrecks never fill the bay | [bt], What sends a vehicle [O] |
| 20 Nov | Left alone: crewed, running, towing, burning, raids | agent choice | Never disturb a vehicle in use | [bt], Which vehicles [O] |
| 20 Nov | No aircraft, boats or trailers on bay trips | agent choice | None drives across a map | [bt], Read this first |
| 20 Nov | Only a driver we called starts a trip | agent choice | Boarding means the player's plans | [bt], The trip |
| 20 Nov | A vehicle the player parked at the bay stays | agent choice | Only ours leaves by itself | [bt], The trip |
| 20 Nov | Place given up after a day with nothing done | agent choice | The bay never held idle | [bt], The trip |
| 20 Nov | A stalled drive retried 3 times, then a day's wait | agent choice | Authored | [bt], The trip |
| 20 Nov | A look every 1,000 ticks, staggered by ID | agent choice | Nothing runs each tick | [bt], Cost to run |
| 20 Nov | Four batches: drive, park, tow, push | agent choice | Each built in 0.201.0 | [bt], Batches |
| 20 Nov | Park fills from its south-west corner, a cell clear | agent choice | Nothing door to door | [bt], The vehicle park (batch 2) |
| 20 Nov | Park icon drawn in code | agent choice | A sign is flat shapes | [bt], The vehicle park (batch 2) |
| 20 Nov | Tow by the nearest winch truck within the tow ratio | agent choice | Over-heavy tows the player's own | [bt], The recovery tow |
| 20 Nov | The tow driver gets out; the hitch's job couples | agent choice | Whoever takes it | [bt], The recovery tow |
| 20 Nov | Short of reach, winched in along a found way | agent choice | A labelled simplification | [bt], The recovery tow |
| 20 Nov | Tow stages limited; a failed tow retried in a day | agent choice | Never stuck for ever | [bt], The recovery tow |
| 20 Nov | Push: 25 kgf a person, six people at most | agent choice | A ballpark, unverified | [bt], Pushing by hand |
| 20 Nov | Pushing is hauling work, 90 ticks a cell | agent choice | Hunger and sleep come first | [bt], Pushing by hand |
| 20 Nov | A risky behaviour has a setting, on unless high | owner | "Unless they are a HIGH" | [bt], Risk and settings |
| 20 Nov | Bay trips moderate, tow and push low: all on | agent choice | Risk grades for the owner | [bt], Risk and settings |
| 20 Nov | Design depots, step-wise | owner | After the emergent-play EM4 | [dp], Who chose what |
| 20 Nov | No random rolls; every pressure has a cause | owner | Seen coming by the player | [dp], Read this first |
| 20 Nov | Our own depot place, not VF's stash, no clock | owner | The agent's lean | [dp], The owner's answers (20 Nov 2026) |
| 20 Nov | Bare caches of crates too (DP1(b)) | owner | Against the agent's lean | [dp], The owner's answers (20 Nov 2026) |
| 20 Nov | The aircraft's depot stop in batch 1 | owner | The agent had it second | [dp], The owner's answers (20 Nov 2026) |
| 20 Nov | Four pressures with a cause (wars overturned 20 Nov) | owner | All the agent offered | [dp], The owner's answers (20 Nov 2026) |
| 20 Nov | Swapping cargo is enough; the line names fuel | owner | The agent's lean | [dp], The owner's answers (20 Nov 2026) |
| 20 Nov | Proceed with both batches | owner | "Proceed with the two batches" | [dp], Batch 1 as built (Framework 0.199.0) |
| 20 Nov | Settings by risk made a shared rule | owner | Kit 1.63.0 | [dp], Status |
| 20 Nov | Nobody stays with a depot | agent choice | A staffed one is the garage | [dp], The design |
| 20 Nov | Depots not ticked; time worked out on a visit | agent choice | Costs nothing to run | [dp], Choices inside the design |
| 20 Nov | Fuel never made or lost in a transfer | agent choice | Honest quantities | [dp], Choices inside the design |
| 20 Nov | No depot laid on enemy ground | agent choice | As aircraft never land there | [dp], Choices inside the design |
| 20 Nov | No depots for others; no automatic resupply | agent choice | Light tier; EM15 held | [dp], What is not proposed |
| 20 Nov | Depot: the lost-convoy place with its own Def | agent choice | Holds items and vehicles | [dp], Batch 1 as built (Framework 0.199.0) |
| 20 Nov | No depot if the convoy would keep no vehicle | agent choice | VF's convoy is made of vehicles | [dp], Batch 1 as built (Framework 0.199.0) |
| 20 Nov | Refuel emptiest first; a standing convoy pays no time | agent choice | Authored | [dp], Batch 1 as built (Framework 0.199.0) |
| 20 Nov | Aircraft stop below half a tank, within 1.5 tiles | agent choice | Authored | [dp], Batch 1 as built (Framework 0.199.0) |
| 20 Nov | Depot vehicles taken back only by a visit | agent choice | Not at the world map | [dp], Batch 1 as built (Framework 0.199.0) |
| 20 Nov | Weather low, on; stale fuel moderate, on | agent choice | A slow, foreseen loss | [dp], Batch 2 as built (Framework 0.200.0) |
| 20 Nov | Hostile neighbours high, off | agent choice | Losing all at once | [dp], Batch 2 as built (Framework 0.200.0) |
| 20 Nov | Wars folded into neighbours (overturns 20 Nov) | agent choice | RimPacts names no tile owner | [dp], Batch 2 as built (Framework 0.200.0) |
| 20 Nov | Weather 0.3 % a day; fuel stale after 90 days | agent choice | Authored, in DepotPressureRule | [dp], Batch 2 as built (Framework 0.200.0) |
| 20 Nov | Neighbours within 6 tiles find it in 4 days a tile | agent choice | Authored; said on its line | [dp], Batch 2 as built (Framework 0.200.0) |
| 20 Nov | A service yard: servicing and parking in one | owner | Fewer trips with dozens of vehicles | [sy], Who chose what |
| 20 Nov | The yard refuels too | owner | "Refilling fuel too" | [sy], Who chose what |
| 20 Nov | A size cap that grows with its equipment | owner | "Cannot ... start cheesing" | [sy], The zone and its size [O] |
| 20 Nov | One vehicle at a time per dyno bay, no moving | owner's card | A labelled simplification | [sy], Read this first |
| 20 Nov | A new fuel pump building | owner's card | The one new picture | [sy], The fuel pump [O] |
| 20 Nov | The Fleet board runs the yard | owner | "A practical part of this" | [sy], The Fleet board [O] |
| 20 Nov | Yard built after the bay trips test (overturned 21 Nov) | owner | Test first | [sy], Who chose what |
| 20 Nov | Yard figures as proposed, tuned after play | owner's card | Tuned after play | [sy], The owner's answers (cards, 20 Nov 2026) |
| 20 Nov | Stays in the yard only without a depot bay | owner's card | One with a bay goes home | [sy], The owner's answers (cards, 20 Nov 2026) |
| 20 Nov | Charging as a building upgrade in the core | owner's card | "As an upgrade, if possible?" | [sy], The owner's answers (cards, 20 Nov 2026) |
| 20 Nov | Named Service yard | owner's card | No clash in 370 mods | [sy], The owner's answers (cards, 20 Nov 2026) |
| 20 Nov | The vehicle park stays unlimited; the yard a new zone | agent choice | Parking alone gives no cheat | [sy], Read this first |
| 20 Nov | A yard needs a dyno bay | agent choice | Not a yard without one | [sy], Read this first |
| 20 Nov | Allowance: dyno bay 80, pump 40, pit 40 cells | agent choice | A HEMTT stands on 3 x 10 | [sy], The zone and its size [O] |
| 20 Nov | Work at 80 % in a yard; a bay's own pad 70 % | agent choice | Bays keep their worth | [sy], What the yard gives |
| 20 Nov | A yard too big after a loss keeps its cells | agent choice | Never shrunk for the player | [sy], The zone and its size [O] |
| 20 Nov | Dyno line: longest waiting first; Move up first | agent choice | The player's word overrides | [sy], Authored figures [A] |
| 20 Nov | Room made only when a vehicle needs the yard | agent choice | Nothing moved for other reasons | [sy], Bay trips and the yard |
| 20 Nov | Building upgrades as XML Defs, one comp | agent choice | Other modders can add them | [sy], Building upgrades, in the Framework core [O] |
| 20 Nov | First upgrades: cable, roller set, faster pump | agent choice | One for each yard building | [sy], Building upgrades, in the Framework core [O] |
| 20 Nov | Fleet board: yard group, queues, send and take out | agent choice | Presentation; one call each | [sy], The Fleet board [O] |
| 20 Nov | A yard serves only the player's vehicles | agent choice | Costs nothing in a raid | [sy], Other factions' vehicles |
| 21 Nov | Build the yard now, all three upgrades (overturns 20 Nov) | owner | With a cable overlay picture | [sy], Status |
| 21 Nov | Pump and cable art before the first test | owner | Real art before a first test | [sy], Status |
| 21 Nov | Pump filled by haulers only; a pipe later | agent choice | The depot bay has no pipe | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | Equipment counts for the oldest yard only | agent choice | One dyno never pays for two | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | "In the yard" is the vehicle's centre cell | agent choice | As the vehicle park reads it | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | The dyno line looked at every 250 ticks | agent choice | Jobs read the last look | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | Room made after an hour idle; kept-out stay | agent choice | Not the moment work ends | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | Send to yard parks it and ends the trip | agent choice | As if the player parked it | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | Cable at VF's charge rate, 500 W a unit | agent choice | VF's own charging draw | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | Upgrades' costs; taking one off returns half | agent choice | Authored | [sy], As built, where it differs (21 Nov 2026) |
| 21 Nov | Pump 1 x 2, tank 200, 80 W, Vehicle depots research | agent choice | Its parts bill as the bay's | [sy], As built, where it differs (21 Nov 2026) |
| 24 Nov | Send to yard and Take out of yard go whatever the bay-trip setting or the vehicle's switch; only automatic trips obey them | owner's card | Both orders were undone within a few seconds with trips off or the vehicle kept out | Framework 0.276.4 changelog |
| 24 Nov | Ticking the vehicle's switch mid-way leaves an ordered trip going; older saves' trips count as automatic | agent choice | The order is the player's word; nothing in flight changes on load | Framework 0.276.4 changelog |

[st]: ../archive/vehicle-stations.md
[sy]: ../archive/vehicle-service-yard.md
[bt]: ../archive/vehicle-bay-trips.md
[dp]: ../archive/vehicle-depots.md
[uh]: ../universal-hoppers.md
[cl]: ../vehicle-crosslinks.md
