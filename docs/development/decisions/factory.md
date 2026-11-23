# Decisions: Factory Additions

**Status (24 Nov 2026):** living; harvested from the factory records' decisions, owner's choices and agent choices. One row a
decision. How it works now: [the system page](../systems/factory.md). Dates are 2026. **Who**: owner; owner's card (a decision
card answered); agent default or agent choice (the owner may revise either); "agent choice*" where the record names no
chooser. A later row that overturns an earlier one says so. Water Tap's own decisions stay in its record; only the one that set it
apart from this mod is here.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 28 Oct | A human-meat neutroamine bill; stock bill excludes human meat | agent choice, owner | Player picks which meat goes where | [fa], Decisions |
| 28 Oct | No Ideology thought when the human-meat bill finishes | owner | Chemistry is not eating | [fa], Decisions |
| 28 Oct | One mod for every VFE Factory change, prefix `PhobosFactory_` | agent default | Owner to confirm the name | [fa], Decisions |
| 28 Oct | VFE Factory a hard dependency, beside Vanilla Expanded Framework | agent default | It exists only to change that mod | [fa], Decisions |
| 28 Oct | 6 neutroamine per 20 human meat; time and chemfuel unchanged | agent default | Fattier carcass; authored, tune in play | [fa], Decisions |
| 28 Oct | Declares 1.6 and 1.7, one XML set (overturned 8 Nov: 1.7 only) | agent default | Owner plays 1.7; XML serves both | [fa], Decisions |
| 28 Oct | VCHE tie-in through an IfModActive folder | agent default | Mirrors VFE Factory's own patch | [fa], Decisions |
| 28 Oct | No shared library code | agent default | XML on another mod's Defs only | [fa], Decisions |
| 28 Oct | Port drug lab recipes of 8 mods, leaves and refinery bills (0.3.0) | owner | Agent listed the candidates | [fa], Decisions |
| 28 Oct | Luciferium Expansion's 13 recipes not ported | owner | Not recorded | [fa], Decisions |
| 28 Oct | Penicillin from raw fungus; morphine from opium, cyanide from chemfuel declined | agent choice, owner | Penicillium is a mould | [fa], Decisions |
| 28 Oct | Fungus penicillin: 10 raw fungus a unit | agent choice* | Bulkier, not cheaper | [fa], Moved from the README (17 Nov 2026) |
| 28 Oct | Stock rule for ports: batch 4, inputs x4, ticks = lab work x4 x batch | agent default | VFE's own drug bills follow it | [fa], Decisions |
| 28 Oct | Infusions batch 8, reproduction pills batch 1, leaf neutroamine batch 5 | agent default | Lab x8 recipe; samples stack to 1 | [fa], Decisions |
| 28 Oct | maxOutputCount = min(75, stack limit minus batch) on every hopper bill | agent default | Pipe System loses the surplus | [fa], Decisions |
| 28 Oct | Vacsop's steel-or-vacstone as two bills | agent default | One ingredient is one thing | [fa], Decisions |
| 28 Oct | Glitterworld medicine gets five input cells | agent default | Four ingredients | [fa], Decisions |
| 28 Oct | Pipe tie-ins wrapped in PatchOperationFindMod by display name | agent default | As VFE Factory gates its own | [fa], Decisions |
| 28 Oct | Only the corpse fridge a hopper (0.2.0) | agent choice* | Others later | [fa], Moved from the README (17 Nov 2026) |
| 29 Oct | Every file in `Mods/<SourceMod>/` carries the folder's name (0.3.1) | agent choice | Game loads one file per path | [fa], Decisions |
| 29 Oct | A deep factory hopper, option C of three (0.4.0) | owner | A shared ordinary hopper clogged | [fa], Decisions |
| 29 Oct | Deep hopper inherits VFE's via a patch-added Name; 4 stacks | agent default | Keeps VFE's class, follows updates | [fa], Decisions |
| 29 Oct | Deep hopper 35 steel, 1 component, 800 work | agent default | Authored; owner to tune | [fa], Decisions |
| 29 Oct | Re-apply VFE's two chemfuel steps a CE clash skips, each guarded (0.3.2) | agent choice, owner | Does nothing once fixed upstream | [fa], Decisions |
| 29 Oct | Automate fabrication bench, bioferrite shaper, Rimatomics table; no smithies or DMS | owner | Agent listed the benches | [fa], Decisions |
| 29 Oct | A separate automated fabrication bay, not bills on the machining bay | owner | Overruled agent's machining-bay pick | [fa], Decisions |
| 29 Oct | Fix the lost-item bug on VFE's machining bay bills | agent choice, owner | Guns vanished at a full hopper | [fa], Decisions |
| 29 Oct | Generate bench bills in C# at load (the first assembly) | agent choice | Follow other mods' patches; recipe gate | [fa], Decisions |
| 29 Oct | Fab bay inherits the machining bay; 300 steel, 150 plasteel, 12+10 comps, 7000 | agent default | Spacer work; owner to tune | [fa], Decisions |
| 29 Oct | Fab bay needs Complex factories and Fabrication | agent default | Gated like the fabrication bench | [fa], Decisions |
| 29 Oct | Bills: VFE's rules, template research, normal quality, no skill | agent default | Same as VFE's machining bills | [fa], Decisions |
| 29 Oct | Skip buildings and stuff-plus-parts items | agent default | Wrong material | [fa], The fabrication bay and the lost-item fix (29 Oct 2026) |
| 29 Oct | Menu grouped by category then name; labels unique per machine | agent default | Menu filter matches by label | [fa], Decisions |
| 29 Oct | Lower bay limits by a marker (overturned 29 Oct) | agent choice | Safe output limit | [fa], The fabrication bay and the lost-item fix (29 Oct 2026) |
| 29 Oct | Extend the lost-item fix to every affected machine (0.5.1) | owner | 74 of 134 bills at risk | [fa], Decisions |
| 29 Oct | One guard at output (overturns 29 Oct) | agent choice | Limits miss | [fa], The output guard for every factory machine (0.5.1, 29 Oct 2026) |
| 29 Oct | Generated bills' limit min(75, stack limit) | agent choice | The guard makes it safe | [fa], Decisions |
| 29 Oct | Each patch class on its own | agent choice | One part off | [fa], The output guard for every factory machine (0.5.1, 29 Oct 2026) |
| 29 Oct | One generic rule for belts to pull from any container, cheap to run | owner | No patch per building | [fa], Decisions |
| 29 Oct | A belt switch per storage building | agent choice, owner | Flags drain shelves | [fc], Belts that take from any storage (0.6.0, 29 Oct 2026) |
| 29 Oct | Switch starts off | agent default | Nothing moves without consent | [fa], Decisions |
| 29 Oct | Storage already flagged as a hopper gets no switch (overturned 2 Nov) | agent default | It already feeds belts | [fa], Decisions |
| 29 Oct | Switched-on storage feeds machine input cells, not output cells | agent default | Output merges one stack only | [fa], Decisions |
| 29 Oct | Forbidden items taken from switched-on storage | agent default | As from a hopper | [fa], Decisions |
| 29 Oct | A belt scans switched-on storage every 10 ticks | agent default | VFE's own rhythm; cost | [fa], Decisions |
| 29 Oct | VFE's belt icon read from its Def at runtime | agent default | Nothing of VFE copied | [fa], Decisions |
| 29 Oct | Reference VFE Factory's assembly, never ship it | agent choice | No licence | [fc], Belts that take from any storage (0.6.0, 29 Oct 2026) |
| 29 Oct | Belts at the storage's sides must not take from it | owner | "not intended behaviour" in test | [fa], Decisions |
| 30 Oct | Switched-on storage feeds only the belt behind it; hoppers keep sides | agent choice | Belts may run past storage | [fa], Decisions |
| 30 Oct | No "allow colony animal corpses" storage filter | owner | Mod stays as it is, for release | [fa], Decisions |
| 30 Oct | Fix stuck belts and the stray slag | owner | Owner's go | [fc], Stuck belts and belts that obey storage filters (0.7.0, 30 Oct 2026) |
| 30 Oct | Wake stuck belts by VFE's reset | agent choice | Else a rebuild | [fc], Stuck belts and belts that obey storage filters (0.7.0, 30 Oct 2026) |
| 30 Oct | Log wakes, 10 a session | agent default | Find the cause | [fc], Stuck belts and belts that obey storage filters (0.7.0, 30 Oct 2026) |
| 30 Oct | Marked storage's filter obeyed | agent choice | Strays jammed lines | [fc], Stuck belts and belts that obey storage filters (0.7.0, 30 Oct 2026) |
| 30 Oct | VFE's hoppers keep no filter | agent choice | Lines keep working | [fc], Stuck belts and belts that obey storage filters (0.7.0, 30 Oct 2026) |
| 30 Oct | Switched-on storage obeys its filter | agent choice | PG6 save | [fc], Stuck belts and belts that obey storage filters (0.7.0, 30 Oct 2026) |
| 31 Oct | Medicine from synthread on the granulator | owner | Tundra: cotton only in summer | [fa], Medicine from synthread (0.8.0, 31 Oct 2026) |
| 31 Oct | 12 synthread for 12 cloth | agent choice | Masses match within 4 % | [fa], Medicine from synthread (0.8.0, 31 Oct 2026) |
| 31 Oct | Same research as the stock bill; no synthread gate | agent choice | Owning synthread is enough | [fa], Medicine from synthread (0.8.0, 31 Oct 2026) |
| 31 Oct | Priority 0, beside the stock medicine bill | agent choice | Sits beside it in the menu | [fa], Medicine from synthread (0.8.0, 31 Oct 2026) |
| 2 Nov | A cooked-soup bill on the conveyor oven | owner | Uncooked soup upset lines | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | Keep the soup bill anyway | owner | A stand-in for lines | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | Stock soup: one a batch | owner | 80 a batch was wrong | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | Soup fix guarded on the worker | agent choice | A VFE fix ends it | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | Soup yield 2x, not 16x | agent choice, owner | Owner agreed | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | A second bill beside the stock one | agent choice | Cans and pots need uncooked soup | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | A fixed batch of 5 | agent choice | Worker gives 4 | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | 6000 ticks a soup batch | agent choice | A trade, not an upgrade | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | Simple soup only | agent choice | VFE automates no other soup | [fa], Cooked soup on the conveyor oven (0.9.0, 2 Nov 2026) |
| 2 Nov | Factory bills draw DBH water, with an on/off setting | owner | Owner's request | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Dirty water poisons food; a setting | owner | Owner's words | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Conveyor ovens first, other realistic processes later | owner | Owner's request | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Water code in a DBH-only assembly | agent choice | Core names no mod | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Oven: no pipe comp (overturned in part 9 Nov) | agent choice | Comp deletes pipes | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Dirty water risks poison, never refuses | owner | Owner's choice | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Wet: soups, meals, baby food, coffee | agent choice | Kibble stays dry | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Litres 20, 40, 10, 2, 10; 25 % risk | agent choice | Authored | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | Both water settings start on | agent choice | Inspect text says why ovens stop | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | DBH lite mode counts as water not required | agent choice | No plumbing could supply it | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | A new poison cause, 101 | agent choice | Names the water | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | One poison roll a batch (overturned 3 Nov) | agent choice | As the game | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | A batch's chance averaged over its litres | agent choice | Late bad water counts less | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | A cancelled bill clears the tally | agent choice | Small understatement | [fa], Piped water and food poisoning (0.10.0, 2 Nov 2026) |
| 2 Nov | A belt switch on factory hoppers (0.11.0) | owner | Owner asked for the option | [fc], A belt switch on factory hoppers (0.11.0, 2 Nov 2026) |
| 2 Nov | The hopper switch stops belts only | agent choice | Allow taking covers colonists | [fc], A belt switch on factory hoppers (0.11.0, 2 Nov 2026) |
| 2 Nov | Same switch and label as on other storage | agent choice | On means belts may take | [fc], A belt switch on factory hoppers (0.11.0, 2 Nov 2026) |
| 2 Nov | Hoppers get it too, on (overturns 29 Oct) | agent choice | Any belt empties it | [fc], A belt switch on factory hoppers (0.11.0, 2 Nov 2026) |
| 2 Nov | Each hopper its own switch | agent choice | Where it stands | [fc], A belt switch on factory hoppers (0.11.0, 2 Nov 2026) |
| 2 Nov | Inspect line only when off | agent choice | Uncluttered | [fc], A belt switch on factory hoppers (0.11.0, 2 Nov 2026) |
| 3 Nov | An oversized batch out whole, in parts | agent choice | A cap loses items | [fa], Endless output from oversized batches (0.11.1, 3 Nov 2026) |
| 3 Nov | Never spawn a batch into a full cell | agent choice | Displaces items | [fa], Endless output from oversized batches (0.11.1, 3 Nov 2026) |
| 3 Nov | Remainder saved on the process | agent choice | Clean removal | [fa], Endless output from oversized batches (0.11.1, 3 Nov 2026) |
| 3 Nov | Old endless mince left alone | agent choice | The owner's to use | [fa], Endless output from oversized batches (0.11.1, 3 Nov 2026) |
| 3 Nov | Dirty water rolls per part (overturns 2 Nov) | agent choice | Simplification | [fa], Endless output from oversized batches (0.11.1, 3 Nov 2026) |
| 3 Nov | A Pipe System report drafted | agent choice | Agents never post | [fa], Bug report draft for Pipe System (for the owner to post; agents never post) |
| 4 Nov | The tap a separate mod, not in Factory Additions | agent choice | Needs only DBH, not VFE Factory | [wt], Decisions |
| 7 Nov | Stale bills rebuilt; two guards (0.11.4) | agent choice* | Errors ended scans | [fc], Bills that outlive their recipe (7 Nov 2026) |
| 10 Nov | A register of patches for others' faults | owner | Nothing temporary goes permanent | [tp], Who chose what |
| 10 Nov | Rows 8-12 (Factory) marked temporary, with removal conditions | agent default | Owner to overrule | [tp], Who chose what |
| 10 Nov | Report states for rows 8 to 12 | agent default | Only dire faults | [tp], Temporary: comes out when the author fixes it |
| 10 Nov | Belt storage access and bench bills left out of the register | agent default | Additions, not fixes | [tp], Left out on purpose |
| 17 Nov | Universal hoppers: chunks, no new material; active mods only | owner | "Aggregate" meant chunks | [uh], The owner's directions (17 Nov 2026) |
| 17 Nov | Hoppers into our own machines not explored | owner | Not chosen | [uh], The owner's directions (17 Nov 2026) |
| 17 Nov | Universality by marks on Defs, not a switch for every reader | agent choice | Readers read the Def | [uh], What "universal" can honestly mean |
| 17 Nov | UH6: hoppers feed DMS benches | owner | One line | [uh], Built (17 Nov 2026) |
| 17 Nov | UH1 with UH6 first, then UH2, then UH12 | agent choice, owner | Neither UH1 nor UH6 needs art | [uh], Questions for the owner |
| 17 Nov | UH6 lives in Factory Additions, loaded only with DMS | agent default | It already depends on VFE Factory | [uh], Where it would live |
| 17 Nov | DMS feed hopper carries our filter mark | agent choice | It holds chunks, corpses, food, wood | [uh], Built (17 Nov 2026) |
| 17 Nov | Bulk bunker carries the factory-hopper mark (Vehicles) | agent choice | Belts and machines read it | [uh], Built (17 Nov 2026) |
| 17 Nov | The loading chute is not a factory hopper | agent choice | Belts never take from it | [uh], Built (17 Nov 2026) |
| 17 Nov | Fuel by belt (UH9) a guide note only | owner's card | Already in VFE Factory | [uh], Built (17 Nov 2026) |
| 17 Nov | Belt's end beside a parked Talus becomes test row 175 (UH11) | owner's card | May already load it | [uh], Questions for the owner |
| 17 Nov | No tipping straight onto belts | agent choice | 96 belt cells; error storms | [uh], Argued against |
| 17 Nov | No sorting grate at the bunker | agent choice | VFE's filter belt already sorts | [uh], Argued against |
| 17 Nov | No hopper feeding ordinary workbenches | agent choice | The game has no hook | [uh], Argued against |
| 17 Nov | Crushed stone (UH16) researched, then parked | agent default | Nothing in the game asks for it | [uh], Crushed stone (UH16): research, 17 Nov 2026 |
| 17 Nov | README slimmed; how to play moved to the player guide | owner | "Slim them" | [fa], Moved from the README (17 Nov 2026) |

[fa]: ../archive/vfe-factory-additions.md
[fc]: ../archive/vfe-factory-conveyors.md
[uh]: ../universal-hoppers.md
[wt]: ../water-tap.md
[tp]: ../temporary-patches.md
