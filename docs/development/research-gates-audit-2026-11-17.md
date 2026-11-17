# Research gates audit (17 Nov 2026)

**Status (17 Nov 2026):** built the same day at the owner's picks (Vehicles 0.61.0, Framework 0.122.1, not yet tested in game): R1 to R4, R6 and R8 at the proposed costs; R5 and R7 not chosen; R9 as written. The report tool `scripts/research-gates.py` is new with it.

The owner, 17 Nov 2026, before testing Framework 0.122.0 and Vehicles 0.60.0: "I wish to do an audit for where we could benefit from
gating anything behind either pre-existing or new research." This lists what is gated today, what is not, and proposals R1 to R9 for the
owner to choose from. The tree itself is in [research-tree.md](research-tree.md).

## Read this first

- **The tree is sound where it exists**: 12 vehicle projects, 6 medical and 9 submarine ones, and every truck has its own project.
- **The biggest gap is the trailers.** All five (flatbed, both bowsers, Billy, the shower trailer) open with **Vehicle parts** (1,200), the
  same project as the parts bench, so a field kitchen that cooks meals ten at a time arrives with the first wrench.
- **Most truck upgrades are gated in name only.** Of the 28 upgrade nodes in the five trucks' own trees, 9 have no research and 13 name research
  the truck itself already needed, so 22 of 28 open the moment the truck is built. Only 6 ask for anything more (other mods' tie-in nodes,
  added by patch, are all really gated and are not counted here).
- **Four small items have no gate and no recorded reason** (R6, R7).
- **How it was read:** `python scripts/research-gates.py` (every buildable, craftable, upgrade and fitting with its research; `--ungated`,
  `--weak`, `--project`). An offline reading of our own XML, not an in-game test.

## What is gated today

| Group | Count | How it is gated |
| --- | --- | --- |
| Trucks (tipper, tanker, generator set, drill rig, survey truck, seabed crawler) | 6 | Each its own project, 1,500 to 3,600 |
| **Trailers** (flatbed, 2 bowsers, Billy, shower trailer) | 5 | **All on Vehicle parts (1,200)** |
| Stations (parts bench, inspection pit, dyno bay, depot bay) | 4 | Vehicle parts, Vehicle tuning, Vehicle depot |
| Vehicle parts, kits and their recipes | 9 | Vehicle parts, Turbines, Booster packs; the jump-start kit through its bench (left off on purpose) |
| Fittings | 16 | Vehicle tuning (9), Vehicle electrics (4), Turbines (2); **the litter kit has none** |
| Tunes | 6 | Through the dyno bay (Vehicle tuning); left off on purpose |
| Seabed Crawler upgrades and parts | 16 | Eight projects on its own tab: fully gated |
| Truck upgrades | 28 | 9 none, 13 in name only, 6 real (below) |
| Trailer upgrades | 14 | **All none**: they open with the trailer |
| Medical buildings, items and operations | 22 | Six medical projects, or the game's and other mods' research; four left off on purpose |

**Truck upgrades, one by one** ("in name only" means the truck's own project already required that research):

| Truck | No research | In name only | Really gated |
| --- | --- | --- | --- |
| Tipper | greedy boards | bulk body, rock body, loading grab (Machining) | none |
| Tanker | all seven: general, insulated and high-volume tanks, two and three compartments, hose reel, electric pump | none | the gas vessels (Fabrication, by patch) |
| Generator set | none | bigger alternator, cable reel, heat recovery, heavy fan, service lead (Electricity) | battery buffer (Batteries) |
| Drill rig | none | diamond bit, RC hammer, tall mast (Deep drilling) | electric power pack and work lights (Electricity), the well kit and deepchem head (other mods' research) |
| Survey truck | work lights | geophone array, radiometric probe (Ground-penetrating scanner) | long-range and powered arrays (Long-range mineral scanner, Fabrication), survey battery (Batteries) |

## Proposals

Costs are authored, in the tree's own bands (Machining 1,000; our trucks 1,500 to 3,600). Every project keeps a hard requirement. An
existing colony is granted a new project once if it already had what the project replaces (the tree's own rule), so nothing built is lost.

| # | Proposal | Lets the player | Stops the player | Changes how they play |
| --- | --- | --- | --- | --- |
| **R1** | **Towing and trailers** (new, 1,000; needs Vehicle parts): the flatbed and both bowsers; the water bowser also needs Dubs' Plumbing | See trailers as a step after their first trucks | Building trailers with the first wrench | Towing becomes a small choice, not a free extra |
| **R2** | **Field kitchen** (new, 2,000; needs R1 and Electricity): Billy, and with it caravan cooking | Research one clear thing for expeditions | Mass catering and caravan cooking straight after Vehicle parts | Billy becomes a goal for a colony that travels |
| **R3** | **Field ablutions** (new, 1,500; needs R1, and Dubs' Plumbing and Septic tanks): the shower trailer | The same for hygiene away from home | Hot showers in the field before plumbing is understood at home | Follows Dubs' own order: plumbing first |
| **R4** | **Field catering at scale** (new, 1,500; needs R2): Billy's second range and big kettle (meals twenty at a time) | Grow the kitchen with the colony | Doubling Billy's output for 100 steel | The kitchen's ceiling is earned in two steps |
| **R5** | **One second-tier project per truck** (four new, 1,200 to 2,000 each): Tipper bodies (bulk and rock body, loading grab); Tanker compartments (two and three compartments, high-volume tank, electric pump); Generator uprating (bigger alternator, heat recovery, heavy fan); Drilling tools (diamond bit, RC hammer, tall mast) | Aim research at the truck they use most | Fitting every upgrade the hour the truck is built | Each truck has an early and a late form |
| **R6** | **Litter kit** on Vehicle tuning (existing), like the other fittings | Nothing new | Fitting it with no research at all | Brings the one stray fitting into line |
| **R7** | **Radiation control point** (Medical, with Rimatomics) on Rimatomics' own first research | Nothing new | Building it before any reactor work | Follows the mod it serves |
| **R8** | **Billy's water gear** (wash-up basin, bigger water tank) also needs Dubs' Plumbing | Nothing new | Plumbed fittings before plumbing | Follows Dubs' own order |
| **R9** | **Leave the trailer upgrades free** (lagged tank, second hose, greedy boards, third shower, dining annex, insulated tent): each costs materials and work, and gating them would not be proportional | Fit them as soon as they can afford them | Nothing | No change |

**Agent's recommendation:** R1, R2, R3, R6, R7 and R8, and R9 as written. **R4 and R5 are closer calls**: they add five projects for
upgrades that already cost materials and work. If the owner wants deeper trees, R5 is where the depth is; the survey truck is left out
of it because it is the best gated already (three of its six upgrades, and its groundwater tie-in). A lighter form of R5 is one shared project, "Heavy truck upgrades"
(2,000), for all four trucks' late nodes.

## Left as they are, with reasons

- **Safety shower, escharotomy, tracheotomy, the jump-start kit, the six tunes, the gas alarm, the electrolyser, the liquid oxygen
  store and the hyperbaric chamber:** left off the tree on purpose on 10 Nov 2026 (life-saving first aid, or a gate that already fits).
- **Giving fluids for burn shock:** has no research of its own, but needs the fluids kit, which Basic clinical kit gates.
- **The kettle's recipes:** exist only at Billy's kettle, so Billy's gate is theirs.
- **Factory Additions, Patches and the radio:** outside the tree by the owner's choice of 10 Nov 2026. Their items carry the research of
  the mods they extend (checked: the bomb cases and fills, the fabrication bay, the hopper).
- **The Seabed Crawler:** nothing to add; every upgrade and part has its own project.
- **Behaviours:** the cargo sling (the cargo hook fitting, Turbines), countermeasures (Turbines) and depot bays (Vehicle depot) are
  gated through their fitting or building. Hitching needs no research of its own: the trailer is the gate, which R1 strengthens.

## Soft tie-ins worth having with the new projects

- **R1:** Vanilla Vehicles Expanded's basic vehicles research, as the trucks have.
- **R3:** Dubs Central Heating's heating research (the trailer's water heater is a boiler), counted only while that mod is on.
- **R2:** none found that earns it: Vanilla Cooking Expanded's research is about canning and cheese, not field kitchens.

## Who chose what

- **Owner (17 Nov 2026):** that the audit be done before testing; then R1, R2, R3, R4, R6 and R8 at the proposed costs, the truck upgrades
  left as they are (not R5), and not R7.
- **Agent:** the reading, every proposal, cost and recommendation. All are the owner's to choose from and revise.
