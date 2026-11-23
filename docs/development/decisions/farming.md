# Decisions: Farming

**Status (24 Nov 2026):** living; harvested from the farming records' owner's answers, decisions, proposals and fixes. One row a
decision. How it works now: [the system page](../systems/farming.md). Dates are 2026. **Who**: owner; owner's card (a decision
card answered); agent default or agent choice (the owner may revise either). A later row that overturns an earlier one says so. A
dash in Why means the record gives no reason. Rows for soil rules, farms on other tiles and other designs not yet built are kept,
for when they are.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 19 Nov | Farms on other tiles: background ledger, player may observe | owner | Realism with good performance | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Soil rules as settings; a harsher mode, off by default | owner | Choice; owner likes soil chemistry | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Farming is a new mod, D'thorga Farming | owner | Audience and content differ from Vehicles | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | In depth: tillage, seeding, harvest, grain, spraying, fertiliser | owner | The core farm machines | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Hay, irrigation, forestry get short entries only | owner | "Largely offered by other mods already" | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Learn from other mods as the mod expands | owner | — | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Study Dubs Bad Hygiene's soil moisture as the model | owner | Its realism-to-play balance is "quite good" | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Machine sowing uses up seed | owner | Fair cost of industrial-scale planting | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Hand sowing free; switch "hands use seed too", off | owner's card | — | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Implements may be 3x3 to 3x5, under the 3x7 floor | owner's card | To look right behind a tractor | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Three tractor sizes from the first tractor build | owner's card | Tractor choice is real from the start | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Seed both ways: kept back, and generic from traders | owner's card | An FS25 mod the owner loved | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Machines' names left for later (overturned 20 Nov) | owner's card | Proposals stand until then | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Harvest fires: a switch, on in Realistic preset only | owner's card | — | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | First batch is the base and the harvest; tillage second | owner's card | — | [fm], The owner's answers (19 Nov 2026) |
| 19 Nov | Balance realism and play, Farming Simulator 25 the model | owner | Realism is not the end goal | [fm], The guiding principle (owner, 19 Nov 2026) |
| 19 Nov | Shouldn't deny or negate player choices | owner | Correcting the agent's first draft | [fm], The guiding principle (owner, 19 Nov 2026) |
| 19 Nov | Hard dependencies: D'thorga Framework and Vehicle Framework | agent choice | — | [fm], The mod |
| 19 Nov | Generic code (run, draught, rows, bulk hold) to Framework | agent choice | The rules require it | [fm], The mod |
| 19 Nov | Other mods only as soft tie-ins, on when active | agent choice | — | [fm], The mod |
| 19 Nov | Paddock is a growing zone with a mark (AG0.1) | agent choice | Other mods still see a growing zone | [fm], Paddocks |
| 19 Nov | Hands skip paddocks: allowSow off, one harvest-scan postfix | agent choice | Hand scans walk every cell | [fm], Paddocks |
| 19 Nov | "Hands may help" switch on each paddock | agent choice | For players who want both | [fm], Paddocks |
| 19 Nov | Driver's skill counts as the sower's; sowing rules apply | agent choice | — | [fm], Paddocks |
| 19 Nov | One order per paddock; machine drives its own rows (AG0.2) | agent choice | Farm work as a job with an end | [fm], Field work |
| 19 Nov | Row planner a game-free Framework rule, with tests | agent choice | — | [fm], Field work |
| 19 Nov | Implement acts only on cell change, on its own strip (AG0.3) | agent choice | Few cells per move, never a scan | [fm], Field work |
| 19 Nov | Machines never destroy an unripe crop | agent choice | The autofarmer's fault | [fm], Field work |
| 19 Nov | Every stop says why | agent choice | — | [fm], Field work |
| 19 Nov | New engine state, field work (AG0.4) (overturned 19 Nov, combine) | agent choice | Driving and working at once | [fm], Field work |
| 19 Nov | Draught from Iowa State per width, scaled for play | agent default | ASABE D497 could not be read | [fm], Field work |
| 19 Nov | Power shown as a plain statement; maths in Engineer mode | agent choice | — | [fm], Field work |
| 19 Nov | Work rate: speed x width x efficiency, also off-map | agent choice | Standard field-capacity relation | [fm], Field work |
| 19 Nov | Plough or direct-drill is the tillage decision | agent choice | Real passes differ widely in fuel | [fm], Tillage and seeding (AG1) |
| 19 Nov | Wet ground stops work through existing grip rules | agent default | No new rule needed | [fm], Tillage and seeding (AG1) |
| 19 Nov | Crop no front suits: harvested at reduced rate (overturned 19 Nov) | agent choice | — | [fm], Harvest and grain handling (AG2) |
| 19 Nov | Fronts chosen by crop traits, never by name | agent choice | — | [fm], Harvest and grain handling (AG2) |
| 19 Nov | We make dual crops' second product ourselves | agent choice | VEF's patch skips a vehicle | [fm], Harvest and grain handling (AG2) |
| 19 Nov | With Dubs Bad Hygiene, spreader and sprayer write into its soil | agent choice | One soil model, never two | [fm], Spraying and fertiliser (AG3) |
| 19 Nov | Blight: early spraying saves crop, late only stops spread | agent default | — | [fm], Spraying and fertiliser (AG3) |
| 19 Nov | Ship plain fertiliser, lime, spray; real routes a tie-in | agent default | Chemistry is a tie-in | [fm], Spraying and fertiliser (AG3) |
| 19 Nov | Crop-duster waits on our flight being tested | agent choice | It reuses circling and water drop | [fm], Spraying and fertiliser (AG3) |
| 19 Nov | Drill carries seed in bulk; empty stops; haulers refill | owner | — | [fm], Seed (owner's decisions) |
| 19 Nov | Farm machines parked at farming settlements; none in raids | agent choice | Standing rule on other factions | [fm], Other factions' machines |
| 19 Nov | Vehicle budget 0.05 ms a tick stands; measure with perf.py | agent choice | Measured, not assumed | [fm], Cost to run |
| 19 Nov | A machine harvest yields what a hand harvest would | agent choice | Machines add cells, not yield | [fm], Economy |
| 19 Nov | Safe to add mid-game; removal leaves plain growing zones | agent choice | Mark saved in a map component | [fm], Saves |
| 19 Nov | Bodies drawn bare, an overlay per upgrade, no stand-in art | owner | The art rules | [fm], Art and sound |
| 19 Nov | Real recordings for real machines | owner | Decided for sounds that day | [fm], Art and sound |
| 19 Nov | Build order: base and harvest; tillage and seed; spraying | owner | — | [fm], The order to build (owner, 19 Nov 2026) |
| 19 Nov | Each batch has its own art before its first test | owner | The owner's preference | [fm], The order to build (owner, 19 Nov 2026) |
| 19 Nov | Any vehicle with our hitch tows the chaser bin | owner | Agent had leaned to moving a tractor up | [b1], The owner's answers (19 Nov 2026) |
| 19 Nov | Field and gathering runs share one base, VehicleRun, now | owner | Risk accepted: touches untested code | [b1], Where each piece goes |
| 19 Nov | Gathering run's behaviour and saved keys kept unchanged | agent choice | — | [b1], Where each piece goes |
| 19 Nov | Run the combine's pilot picture now | owner | — | [b1], The owner's answers (19 Nov 2026) |
| 19 Nov | Paddock mark in a map component keyed by zone ID | agent choice | Zone ID is stable across saves | [b1], Paddocks (AG0.1) |
| 19 Nov | Patch zone gizmos: our toggles replace "allow sowing" | agent choice | The game's toggle can be flipped back | [b1], Paddocks (AG0.1) |
| 19 Nov | Batch one: hands sow (hands may help), machines harvest | agent choice | No seed drill in batch one | [b1], Paddocks (AG0.1) |
| 19 Nov | Run started by a gizmo; driver already aboard; no new job | agent choice | As the gathering run is | [b1], Harvest this paddock (AG0.2) |
| 19 Nov | Rows alternate; headland ring left for a last pass | agent default | — | [b1], Harvest this paddock (AG0.2) |
| 19 Nov | Keep the failure roll for a humanlike driver | agent choice | Driver's skill matters as by hand | [b1], The harvest steps (AG2.2) |
| 19 Nov | Vehicle passed as harvester to PlantCollected (Chop) | agent choice | Every active patch tolerates it | [b1], The harvest steps (AG2.2) |
| 19 Nov | Produce tallied per row, never into VF cargo | agent choice | Cargo recalculates stats each time | [b1], The harvest steps (AG2.2) |
| 19 Nov | Driver earns a share of hand-harvest Plants XP | agent default | A driver still learns, slower | [b1], The harvest steps (AG2.2) |
| 19 Nov | Grain front fits cereals incl. wheat, barley, oats (overturned 21 Nov) | agent choice | Agent's reading of crops | [b1], Fronts (AG2.3) |
| 19 Nov | XML extension overrides a misread crop's front | agent choice | Odd crops of other mods | [b1], Fronts (AG2.3) |
| 19 Nov | Crop no front suits is refused (overturns 19 Nov) | agent default | — | [b1], Fronts (AG2.3) |
| 19 Nov | Front strips 5 and 7 cells (overturned 20 Nov) | agent default | — | [b1], Fronts (AG2.3) |
| 19 Nov | Bulk grain hold is new, not a tank; capacity by mass | agent choice | Tanks are built for fluids | [b1], The bulk grain hold (AG2.5) |
| 19 Nov | Bins and silo may mix products; combine tank one at a time | agent default | — | [b1], The bulk grain hold (AG2.5) |
| 19 Nov | One weighted freshness per product; powered silo slows rot | agent default | As the tanker keeps fluid quality | [b1], The bulk grain hold (AG2.5) |
| 19 Nov | Combine 4x9, tank 9,000 kg; chaser bin 3x7, 12,000 kg | agent default | — | [b1], The machines (AG2) |
| 19 Nov | Field bin 3x3, 25,000 kg; silo 4x4, 60,000 kg | agent default | — | [b1], The machines (AG2) |
| 19 Nov | Threshing as shaft loads; no new engine state (overturns 19 Nov) | agent choice | Ledger already charges while driving | [b1], The machines (AG2) |
| 19 Nov | Research: Mechanised harvesting; Grain storage for the silo | agent choice | — | [b1], The machines (AG2) |
| 19 Nov | Combine nodes: fronts, tank ext., chopper, filter, monitor | agent choice | Every node moves an outcome | [b1], Upgrade trees (agent proposals) |
| 19 Nov | Chaser bin fast auger and flotation tyres; silo aeration fan | agent choice | Every node moves an outcome | [b1], Upgrade trees (agent proposals) |
| 19 Nov | "Hands may help on new paddocks" off | agent default | — | [b1], Settings (batch one) |
| 19 Nov | "A combine driver can fail a harvest" on | agent default | — | [b1], Settings (batch one) |
| 19 Nov | "Combines leave the headland for a last pass" on | agent default | — | [b1], Settings (batch one) |
| 19 Nov | Harvest fires setting off until the soil presets exist | agent default | On in Realistic only | [b1], Settings (batch one) |
| 19 Nov | "Grain keeps longer in an aerated silo" on | agent default | — | [b1], Settings (batch one) |
| 19 Nov | Five seen-in-play note keys for paddocks and harvest | agent choice | Each behaviour notes itself | [b1], Seen-in-play notes (batch one) |
| 19 Nov | Bins age spoilage from elapsed time, not per tick (overturned 21 Nov) | agent default | Nothing per tick | [b1], Cost to run |
| 19 Nov | Combine body one pilot picture first, dry run, owner's go | agent choice | First paid picture of the mod | [b1], Art and sound plan |
| 19 Nov | Three tractor sizes at once | owner | — | [b2], Read this first |
| 19 Nov | Three presets: Off, Field care, Realistic | agent choice, owner | Configurable, as the owner asked | [fs], Read this first |
| 19 Nov | Realistic is punishing on purpose, off by default | owner | "For players like me" | [fs], The owner's decisions (19 Nov 2026) |
| 19 Nov | Moisture is a bonus only, in every preset | owner | Weather is outside the player's control | [fs], The owner's decisions (19 Nov 2026) |
| 19 Nov | Default preset: Field care | owner's card | — | [fs], The owner's decisions (19 Nov 2026) |
| 19 Nov | Rules apply to paddocks; all-zones option, off | owner's card | — | [fs], The owner's decisions (19 Nov 2026) |
| 19 Nov | A first-run wizard helps the player start (SL8) | owner's card | — | [fs], The owner's decisions (19 Nov 2026) |
| 19 Nov | Realistic yield may fall to about a third | owner's card | Stand-in for systemic failures | [fs], The owner's decisions (19 Nov 2026) |
| 19 Nov | Soil test: hand kit first, then a tractor-mounted sampler | owner's card | — | [fs], The owner's decisions (19 Nov 2026) |
| 19 Nov | Presets are buttons, not a slider | owner | The owner's standing preference | [fs], The presets |
| 19 Nov | A switch for each rule; a change shows "Custom" (SL1) | agent choice | — | [fs], The presets |
| 19 Nov | A switch that needs another is greyed out, says why | agent choice | Acidity acts through nutrients | [fs], The presets |
| 19 Nov | Moisture in DBH's shape: capped bonus, lost faster in heat | agent choice | DBH never punishes | [fs], Moisture: Dubs Bad Hygiene's way, studied |
| 19 Nov | Without DBH, the same rule per paddock in our ledger | agent choice | Cheaper than a cell sweep | [fs], Moisture: Dubs Bad Hygiene's way, studied |
| 19 Nov | Field care: visible states; steps add yield to a ceiling | agent choice | FS25's model; never punishes | [fs], Field care (SL3) |
| 19 Nov | Field care figures authored, set at first build | agent default | No official FS25 figures found | [fs], Field care (SL3) |
| 19 Nov | Realistic: harvests remove nutrients; yield below vanilla | owner | Written so it is not softened again | [fs], Realistic (SL4) |
| 19 Nov | Scarcest nutrient sets yield; legumes named per crop in XML | agent choice | Never worked out from a name | [fs], Realistic (SL4) |
| 19 Nov | Keep the real pH scale and its thresholds | agent default | FS25's scale is not calcium chloride | [fs], Realistic (SL4) |
| 19 Nov | Fail by linked causes, not a floor | owner | As the engine simulation does | [fs], Failure by chains, not by a floor (owner's direction, SL9) |
| 19 Nov | Each link its own XML rule | agent choice | Others can retune links | [fs], Failure by chains, not by a floor (owner's direction, SL9) |
| 19 Nov | Soil figures per paddock, changed at events, one-step equations | agent choice | Lets a farm run off the map | [fs], The equations |
| 19 Nov | Yield changed by one postfix on Plant.YieldNow | agent choice | Every harvest sees the same figure | [fs], The equations |
| 19 Nov | Yield factor: a third to 1.3 Realistic; 1.0 to ceiling care | agent default | — | [fs], The equations |
| 19 Nov | Plain N, P, K, lime, weed killer; DBH biosolids, compost | agent default | — | [fs], Fertiliser and the chemistry (SL6) |
| 19 Nov | Changing preset mid-game keeps unused figures | agent choice | Changing back loses nothing | [fs], Saves |
| 19 Nov | Paddock starts at neutral soil; safe to add mid-game | agent choice | — | [fs], Saves |
| 19 Nov | Run in background by ledgers; player may observe | owner | Realism with good performance | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | The same soil rules run in the ledger | owner | — | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | Our own world-map site, not a VE outpost | owner's card | No new hard dependency | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | Deliveries are real convoys from the start | owner's card | — | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | Raids rare, opening the map, behind a setting that starts off | owner's card | — | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | Anyone may be crew: colonists, animals, mechs, prisoners | owner's card | — | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | Farm goods count in full toward colony wealth | owner's card | No hiding wealth from the storyteller | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | One farm open at a time, a setting the player may raise | owner's card | 127-map limit; map cost | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | Player's own colonists may work at an open farm | owner's card | — | [rf], The owner's direction (19 Nov 2026) |
| 19 Nov | Ledger is the truth; the map a view with write-back | agent choice | — | [rf], Read this first |
| 19 Nov | A map kept asleep is withdrawn | agent choice | Other mods reach it; timers go stale | [rf], Read this first |
| 19 Nov | Founded on a real visit; needs a paddock and a grower | agent default | — | [rf], The farmstead |
| 19 Nov | Ledger steps daily, staggered; frost and season from start | agent choice | Else crops die when the map opens | [rf], The ledger step |
| 19 Nov | A full stored map only as a fallback | agent choice | Save size; breaks on mod changes | [rf], Observing |
| 19 Nov | Shared rules for both views; learned growth ratio per paddock | agent choice | Two views must never drift | [rf], Keeping the two views the same |
| 19 Nov | Crew held by the farmstead, not passed to the world | agent choice | The game's cleaner deletes pawns | [rf], Crew, machines and deliveries |
| 19 Nov | Closed farm not a target; incidents blocked at last step | agent choice | As RimExodus and the pause mod do | [rf], Raids and incidents |
| 19 Nov | Remote-farm build follows the machine batches | agent choice | Answers make it larger | [rf], What the owner's answers add to the first build |
| 20 Nov | Couplings enforced; a cheap adapter fixes a mismatch | owner's card | Farms use many towing types | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | Three-point linkage in batch two; plough, cultivator carried | owner's card | — | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | Seed drill is trailed, not carried | owner's card | — | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | Existing trucks and trailers count as universal | owner's card | No save's coupled pair broken | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | Mounted implement stays its own vehicle, carried | owner | Risk accepted; agent advised otherwise | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | Farming reaches main as one big commit (overturned 20 Nov) | owner | Once its build is whole | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | Tractors' art after batch one's art | owner | Fronts, bins and icons first | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | Couplings in the Framework core, a Def per coupling | agent choice | Other authors can name one | [fm], Coupling types (owner, 20 Nov 2026) |
| 20 Nov | No change needed for crop economy; watch in play | agent choice | Bounded as vanilla is | [fm], The check (AG6, 20 Nov 2026, agent's) |
| 20 Nov | Combine value 3,000 (was 4,200); chaser bin 1,300 | agent default | Selling must not beat building | [fm], The check (AG6, 20 Nov 2026, agent's) |
| 20 Nov | Names: Reaper, Garner, Thane, Sillion, Tillman, Coulter (overturns 19 Nov) | owner | Agent's names too plain | [fm], Art and sound |
| 20 Nov | Thane numbers by kW: T60, T130, T300 (overturned 20 Nov) | owner | — | [fm], Art and sound |
| 20 Nov | Headland sprayer, Broadcast spreader: still proposals | agent choice | — | [fm], Art and sound |
| 20 Nov | Commit as built; go on to batch two before a test (overturns 20 Nov) | owner | — | [b1], Status |
| 20 Nov | Front strips 6 and 8 cells, centred (overturns 19 Nov) | owner's card | Even widths centre on the 4-cell body | [b1], Fronts (AG2.3) |
| 20 Nov | Built: two fronts, tank ext., chopper; bin fast auger | agent choice | Others need a hook first | [b1], Upgrade trees (agent proposals) |
| 20 Nov | First test after batch two, a whole till-sow-harvest loop | owner's card | — | [b1], The owner's answers (19 Nov 2026) |
| 20 Nov | A question card for each paid art run, with dry-run ceiling | owner's card | Before anything is spent | [b1], The owner's answers (19 Nov 2026) |
| 20 Nov | Standard front: keep attempts 2 and 1; wide front attempt 2 | owner's card | — | [b1], The owner's answers (19 Nov 2026) |
| 20 Nov | Chaser bin body: attempt 3 | owner's card | — | [b1], The owner's answers (19 Nov 2026) |
| 20 Nov | Field machines' tanks are never a pour destination | agent choice | A combine unloaded into a combine | [b1], The fault-read (20 Nov 2026) |
| 20 Nov | Holds stay in real kilograms | owner | Industrial-scale crops to come | [b1], The fault-read (20 Nov 2026) |
| 20 Nov | Linkage built as a rigid hitch | agent choice | Least new ground in Vehicle Framework | [b2], Read this first |
| 20 Nov | Lowered on a run's row, lifted otherwise; Lower/Lift button | agent choice, owner | Owner added the button | [b2], The linkage (Framework) |
| 20 Nov | Tractor carries an implement and tows a cart at once | owner | Chosen over agent's one at a time | [b2], The linkage (Framework) |
| 20 Nov | Built as a chain of tows, at most three behind | agent choice | Not a second hitch on the tractor | [b2], The linkage (Framework) |
| 20 Nov | Field run finds its worker on the tractor, then the implement | agent default | — | [b2], The field run with an implement behind (Framework) |
| 20 Nov | Implement works the line under its rear edge (overturned 21 Nov) | agent default | — | [b2], The field run with an implement behind (Framework) |
| 20 Nov | Keep draught as authored (cell 1.5 m, medium soil) | owner | — | [b2], The implements (Farming) |
| 20 Nov | Width by a wing-extension node, not a second model | owner | — | [b2], The implements (Farming) |
| 20 Nov | Wings: plough 1 to 2, cultivator 4 to 7, drill 4 to 7 cells | agent default | — | [b2], The implements (Farming) |
| 20 Nov | Plough 1 cell wide | owner | Match the three-furrow picture | [b2], The implements (Farming) |
| 20 Nov | A plough sized for the T400 comes later | owner | Needs its own picture | [b2], The implements (Farming) |
| 20 Nov | Plough lays VPE's tilled soil (not built; 21 Nov) | agent choice | — | [b2], The implements (Farming) |
| 20 Nov | Ploughed and cultivated marks saved; no effect until soil rules | agent default | — | [b2], The implements (Farming) |
| 20 Nov | Seed in a bulk hold | agent choice, owner | — | [b2], Seed (owner's decisions of 19 Nov 2026; the agent's way, kept by the owner on 20 Nov 2026) |
| 20 Nov | Own grain; 10 % lost | agent choice, owner | — | [b2], Seed (owner's decisions of 19 Nov 2026; the agent's way, kept by the owner on 20 Nov 2026) |
| 20 Nov | Bought seed 3x, bulk traders | owner's card | — | [b2], Seed (owner's decisions of 19 Nov 2026; the agent's way, kept by the owner on 20 Nov 2026) |
| 20 Nov | Cell takes 3 % of harvest | agent default | — | [b2], Seed (owner's decisions of 19 Nov 2026; the agent's way, kept by the owner on 20 Nov 2026) |
| 20 Nov | Hands fetch 20 cells' seed | agent default | — | [b2], Seed (owner's decisions of 19 Nov 2026; the agent's way, kept by the owner on 20 Nov 2026) |
| 20 Nov | Crop class: harvest is seed? | agent choice | — | [b2], Seed (owner's decisions of 19 Nov 2026; the agent's way, kept by the owner on 20 Nov 2026) |
| 20 Nov | Patch frame hay; can be off | owner | — | [b2], The Agricultural Frame's hay (owner, 20 Nov 2026; built in Farming, the release after 0.6.0) |
| 20 Nov | Fill its list; no Harmony | agent choice | — | [b2], The Agricultural Frame's hay (owner, 20 Nov 2026; built in Farming, the release after 0.6.0) |
| 20 Nov | Which classes leave straw | agent choice | — | [b2], The Agricultural Frame's hay (owner, 20 Nov 2026; built in Farming, the release after 0.6.0) |
| 20 Nov | Large tractor is the T400, articulated (overturns 20 Nov T300) | owner | Real ones of this size bend | [b2], Tractors (Farming) |
| 20 Nov | T400 bends on our own hitch, swing about 40 degrees | owner | Agent had said drawn rigid | [b2], Tractors (Farming) |
| 20 Nov | T400 rear axle weight counts as driven grip | agent default | Real ones drive all four wheels | [b2], Tractors (Farming) |
| 20 Nov | Two T130 models under one Architect button | owner | Owner kept both pilot attempts | [b2], Tractors (Farming) |
| 20 Nov | Tractors 60/130/400 kW; 4, 8 (7 RC), 22 t | agent default | Real tractor ranges | [b2], Tractors (Farming) |
| 20 Nov | "Hands use seed too" default off | owner | — | [b2], Settings (batch two, proposed) |
| 20 Nov | T60, T130, T400 and cultivator keep both pilot attempts | owner | — | [b2], Art |
| 20 Nov | Per-model differences (weight, grip, fuel) | agent default | Read from each picture | [b2], Art |
| 20 Nov | Sillion plough is reversible; attempts are its two sides | owner's card | "Make it rotatable" | [b2], Art |
| 20 Nov | Research: Tillage and sowing; Articulated tractors | owner | — | [b2], Research, sounds, overlays and the install (owner's decisions, 20 Nov 2026) |
| 20 Nov | Tractor sounds from real recordings | owner | — | [b2], Research, sounds, overlays and the install (owner's decisions, 20 Nov 2026) |
| 20 Nov | Overlays, seed sack: next paid run, cap first | owner | — | [b2], Research, sounds, overlays and the install (owner's decisions, 20 Nov 2026) |
| 20 Nov | One version and install once art is real | owner | — | [b2], Research, sounds, overlays and the install (owner's decisions, 20 Nov 2026) |
| 20 Nov | Tractor nodes: liquid ballast, front weights, duals, fuel tank | owner's card | — | [b2], Upgrade trees (owner's picks, 20 Nov 2026) |
| 20 Nov | Implement nodes: wing extensions; drill hopper extension | owner's card | — | [b2], Upgrade trees (owner's picks, 20 Nov 2026) |
| 20 Nov | Lifted look by script from own picture, no new art | owner's card | — | [b2], Upgrade trees (owner's picks, 20 Nov 2026) |
| 20 Nov | Build order: linkage, workers, seed, then Defs | agent choice | Defs wait for pictures | [b2], The order to build (agent's) |
| 20 Nov | T400 voice from medium recordings, lowered and layered | owner | No large articulated recording found | [tr], The owner's decisions (20 Nov 2026) |
| 20 Nov | Licences CC0 and plain CC BY; no share-alike, no requests | owner | — | [tr], The owner's decisions (20 Nov 2026) |
| 20 Nov | Owner downloads Freesound originals; agents never sign in | owner | Freesound needs an account | [tr], The owner's decisions (20 Nov 2026) |
| 20 Nov | T60: Massey start, close idle, Valmet working | owner | — | [tr], Status |
| 20 Nov | T130: modern idle, Renault working, Case CX90 stop | owner | — | [tr], Status |
| 20 Nov | T130 and T400 starts to be generated | owner | No medium recording has one | [tr], Status |
| 21 Nov | Fix all 66 findings now | owner's card | — | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Ground loading takes crops and seed only | owner's card | Silo took steel and silver (B3) | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | A store warns before it is taken down full | owner's card | Deconstructing loses its grain | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Flour cereals in no class (overturns 19 Nov) | owner's card | Owner will add own crops | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | A drill still sows flour cereals from bought seed | agent choice | Seed grain sows any crop | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Wish: an FS25-style side-industry (not designed) | owner | — | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Work the line behind the body (overturns 20 Nov) | agent choice | No sowing under a vehicle | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Strip worked on the hitch's own move (R16) | agent choice | Hitch moves it after the tractor | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Paddock mark follows its zone; kept a quadrum | agent choice | Gravship paddocks lost it | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Tilled soil stays unbuilt; status says so | agent choice | No code laid it | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Field machines path through plants at no cost | agent choice | Paths left the row (R3) | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Drive a row only between its ends with work | agent choice | Short sweep; leftovers named | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Pour and aired-silo seen notes struck | agent choice | No test row asks for them | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Permanent coupling joins only a hitch naming it | agent choice | Rear halves got stuck | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Articulated: no winch, no recovery tow | agent choice | Its hitch is its pivot | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Plough body picture at three tenths alpha | agent choice | CE threw on an empty picture | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Soft ground grip 0.12 T60 F, 0.06 T130 RC | agent default | Text promised it (D7) | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | D11, D12, D13 and the silo question left open | agent choice | Each the owner's call | [fr], As fixed (Framework 0.202.0 and Farming 0.7.1) |
| 21 Nov | Threshing load only on the vehicle's own worker | agent choice | Tractors lost 40 % (D3) | [fr], Read this first |
| 21 Nov | Holds age at every rare tick (overturns 19 Nov) | agent choice | Aged only when looked at (B1) | [fr], Read this first |
| 23 Nov | Rain soaks open fields: build it now, on its own | owner's card | — | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | Rain-soak setting off by default | owner's card | Past performance hits | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | Dubs Bad Hygiene preferred; our code otherwise | owner's card | — | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | The setting lives in D'thorga Framework | owner's card | — | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | Build the fallback without DBH now | owner's card | — | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | Measure with perf.py in owner's game before shipping | owner's card | — | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | Rain lifts DBH wetness to our map figure | agent choice, owner | Owner's Option A | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | Fallback: fertility 1 to 1.8 when soaked | agent default | DBH's own default strength | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | Off takes the patches out, no restart | agent choice | Owner's worry: performance | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |
| 23 Nov | DBH rain irrigation off: nothing happens | agent choice | Player turned it off in Dubs | [fs], Rain that soaks the fields (SL10, built 23 Nov 2026) |

[fm]: ../farming-machines.md
[b1]: ../farming-batch-one.md
[b2]: ../farming-batch-two.md
[fs]: ../farming-soil.md
[rf]: ../farming-remote-farms.md
[tr]: ../farming-tractor-recordings.md
[fr]: ../fault-read-2026-11-21.md
