# Outstanding work (from 7 Nov 2026)

Everything still open across the D'thorga mods, as the owner decided it on 7 November 2026, when they asked for every open item as a
question. **Keep this file current:** every change that starts, finishes or defers an item updates it in the same commit. In-game
testing is the owner's and is not listed here; each mod's changelog says what is tested.

**Status key:** open, in progress, done (with the version), deferred (with the reason).

## This round, in order

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| A1 | This checklist | docs | Wanted | Done |
| A2 | Stale records after the merge (Medical Equipment's "next", the art backlog's pre-merge previews, Triage's open points) | docs | Fix | Done |
| A3 | Water Tap draws through the Framework's shared water reader | Water Tap 0.2.2 | Do it | Done |
| A4 | Read Ludeon's 1.7 Modder Primer | docs | Do it | Done: [modding reference](rimworld-modding-reference.md#building-c-mods) |
| B1 | Immunity race: dose when losing by under **two days** (was one) | Medical (dosing) | Two days | Done: Medical 0.7.0 |
| B2 | Doctors give pain relief from **40%** pain (was 60%) | Medical (dosing) | From 40% | Done: Medical 0.7.0 |
| B3 | Masochists skip self-medication | Medical (dosing) | Yes | Done: Medical 0.7.0 |
| C1 | A colour-coded icon for every triage tag, in the game's row of small status icons by a pawn's name | Medical (triage) | Every tag: red immediate, yellow urgent, green delayed, white minor, black expectant | Done: Medical 0.7.0 (Useful Marks row, or beside the name without it) |
| C2 | "How far the doctors may go" as a dropdown on the health tab, beside medical care (the button stays) | Medical (triage) | Add it | Done: Medical 0.7.0 (a button beside the medicine setting) |
| C3 | Triage queues fracture repairs under "save life or a limb" | Medical (triage) | Triage queues them | Done: no change needed, triage already queued them |
| C4 | More Injuries' brain-damage therapy and custom-worker recipes found by triage | Medical (triage) | With C1 to C3 | Done: Medical 0.7.0 |
| D1 | Respect More Injuries' own fire-inhalation setting | Medical (burns) | Respect it | Done: Medical 0.7.0 (its setting decides; ours stands down while a pawn burns) |
| D2 | Fire heat indoors: hot cells near a fire in a large room | Medical (burns) | Yes | Done: Medical 0.7.0 (its own setting) |
| D3 | Pawns and animals flee cells hot enough to burn | Medical (burns) | Yes | Done: Medical 0.7.0 (its own setting; firefighters stay) |
| D4 | Supplied oxygen treats More Injuries' hypoxia | Medical (equipment) | Yes | Done: Medical 0.7.0 (faster healing; the chamber twice as fast) |
| E | Burns phase 2: eschar, escharotomy, ischaemia handed to More Injuries' gangrene, a fluid bill using its saline bag | Medical (burns) | This round, first build | Done: Medical 0.7.0 (fluids bill with More Injuries only; our kit comes in G) |
| F | Second equipment set, in the owner's order: ventilator (intubation a quick doctor job), safety shower and eyewash, carbon monoxide and smoke alarm (wakes sleepers; separate from the hydrogen detector, Ostranauts-like), cylinder trolley, nebuliser, anaesthesia machine, needle decompression kit, steriliser | Medical (equipment) | This round, second build | Done: Medical 0.7.0, all eight with placeholder art |
| G | Fussing (lower tend quality in extreme pain) and a fluids kit for burn shock | Medical | This round, third build | Done: Medical 0.7.0 (fussing setting on; fluids kit beside the saline bill) |
| H | Caustic (tox) gas model: slow-acting by default, fast-acting setting | Medical (burns) | This round, after the builds | Done: Medical 0.7.0 (slow-acting default; eye scarring, plants and clothing still open) |
| I1 | Workstation environmental effects: a design record | docs | Record now, nothing built | Done: [record](workstation-environmental-effects.md) |
| I2 | Vaccines and prevention for healthy pawns: a design record | docs | Record now, nothing built | Done: [record](vaccines-and-prevention.md) |
| I3 | Harrier: upgrade costs and CE armour against the Hind, and what a shot-down Harrier leaves; proposals | Patches | Check both | Done: findings and proposals in [vehicle patches](vehicle-patches.md); one change proposed (a cheaper CE armour upgrade), for the owner |
| J | Bug reports finished for the owner to post: RimPacts, Harrier (two), More Injuries and MSE2 (two), IRBM (after the owner's test confirms the cause) | docs | Finish all | Done except IRBM: RimPacts and Harrier (two) in [vehicle patches](vehicle-patches.md), More Injuries and MSE2 in [the save bridge](medical-save-bridge.md); IRBM held for the owner's test |

## The owner's reports of 7 November 2026 (evening)

| # | Report | Finding | Status |
| --- | --- | --- | --- |
| R1 | Machining bays read only the first stack in a deep factory hopper | The scan reads every stack; a bill saved before a recipe change kept old owners, and an item it could not take threw and ended the scan. Two bays also waited on ingredients no input hopper held ([record](vfe-factory-conveyors.md)) | Done: Factory Additions 0.11.4 rebuilds stale bills, guards the scan; README states the input-cell rule. Not yet tested in game |
| R2 | The Harrier cannot fly sorties while a RimPacts client state attacks | RimPacts' spectate landing gate empties the arrival list until the battle is joined; the jet rejected the click in silence ([record](vehicle-patches.md)) | Done: Patches 0.11.0 shows the reason; a setting lets sorties fly over an unjoined battle. The owner should try Join first. Not yet tested in game |
| R3 | A far more detailed Harrier parts list, upgrades following, existing jets updated | Vehicle Framework saves parts by position; parts may only be appended, and code must add them to saved jets ([record](vehicle-patches.md)) | Done: Patches 0.11.0, fourteen parts, flight-control rule, saved jets gain the parts on load. Not yet tested in game |
| R5 | After a nuke, an ally's RimPacts assault "never eventuated" and the battle was lost; about twelve towns were lost later | RimPacts trims the assault to the defenders left standing, about two after the strike ([record](missile-patches.md)) | Done: Patches 0.11.1 (0.11.0's version did not work: RimPacts capped the squad before our trim). The towns: RimPacts' world war quest deadline (3 days from accepting) passed before the battle; its daily check failed the war and moved control of 11 towns. RimPacts' rule, recorded in the README; Patches 0.12.0 gives the quest the game's own countdown (owner's request); Patches 0.13.0 stops Visit Settlements closing a battle map under the owner's force (bug report drafted in the missile record) |
| R4 | (Found in the log) 7,662 errors from the IRBM strike job after loading another game | The job named a closed map in a log line, and the Framework's queue could not empty itself | Done: Framework 0.8.1 and Patches 0.11.0 |

## Vehicles (from 8 Nov 2026)

Added 9 Nov 2026: the vehicle work of 8 and 9 November did not update this file as it went. Records:
[vehicle systems](vehicle-systems-design.md), [utility vehicles](utility-vehicles-design.md), and the dated records they link.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| V1 | Vehicle systems: battery, coolant, oil, wear and services, tyres, brakes, drivetrains, gas turbines, power and lift, steam, Combat Extended corrections, the world map and the cab, the Dashboard and Systems tabs, booster packs, world-map ground, cab moods, RimHUD | Framework 0.9.0 to 0.18.0, Vehicles 0.1.0 to 0.7.0 | Approved in parts | Done; not yet tested in game |
| V2 | What a service uses for oil and coolant (chemfuel stands in; with Dubs Bad Hygiene coolant could be its water) | Framework | Unsure (9 Nov 2026) | Open |
| V3 | The utility vehicles: a tanker first, then survey truck, drill rig, tipper hauler, generator set | Vehicles | Yes, after the framework | Open: waits for vehicle body art (V4) |
| V4 | Vehicle body art through OpenAI's image API (an engine in `scripts/art-run.py`); icons on the owner's local model | art | OpenAI for bodies, local for icons (9 Nov 2026); **the stand-in icons are good enough: replace them only when little else is left** (9 Nov 2026) | Open: bodies wait for the owner's API key; icons low priority |
| V5 | Systems for electric vehicles, with an optional patch making Vanilla Vehicles Expanded's Lightning electric to test them; our own diesel-electric utility vehicles later (V3) | Framework, Vehicles | Framework and a test vehicle (9 Nov 2026) | Done: Framework 0.20.0, Vehicles 0.9.0 ([record](vehicle-electric-design.md)); a night heater for electric vehicles left for later |
| V6 | The M1A2's APU upgrade node; "drop the stores" for a jet too heavy to land vertically | Vehicles, Patches | Proposed | Deferred: the APU needs a patch to RimThunder's upgrade trees |
| V7 | Engine sound pitched by engine speed | Framework | Later | Deferred: only if Vehicle Framework exposes the sustainer |
| V8 | Kerb mass of 12 kg per Vehicle Framework mass unit for retrofitted vehicles, checked for outliers (dirt bikes, tanks) | Framework, Vehicles | Authored masses for Vanilla Vehicles Expanded; tunable (9 Nov 2026) | Done: Framework 0.19.0, Vehicles 0.8.0 ([record](vehicle-kerb-mass-check.md)) |
| V9 | When Vehicle Framework ships for 1.7: rebuild, and re-check the tab hook's order, the caravan patches' method names, and the part icons and colours the Systems tab leaves out | Framework | Expected | Waiting on Vehicle Framework |
| V10 | The boats, the Warbird and the Smuggler keep one engine | Vehicles | Agreed to leave out | Deferred |
| V14 | Bleed air: the Harrier's puffer jets in a hover (more in wind) and cockpit heat raise the engine's work | Framework | Yes, if it stays cheap (9 Nov 2026) | Done: Framework 0.24.0 |
| V13 | The Dashboard and Systems tabs on the world map: vehicle caravans and aircraft in flight, with a vehicle list | Framework | Yes (9 Nov 2026) | Done: Framework 0.23.0 |
| V12 | Cab heaters for aircraft, boats and steam cabs (a gap found from the owner's Harrier); automatic climate control by era with a blanket setting | Framework, Vehicles | Climate control with a main-menu option (9 Nov 2026) | Done: Framework 0.21.2 and 0.22.0, Vehicles 0.9.1 and 0.10.0 |
| V11 | The tabs after the owner's first look (9 Nov 2026): wear gauges show what is left, no repeated part names, no empty sections | Framework | Show what is left | Done: Framework 0.19.0 |

## Vehicle health (from 9 Nov 2026)

The owner's primary aim for the vehicles: pawn-like health and injury detail for player vehicles, diffuse for NPCs, performance
first ([record](vehicle-health-design.md)). Slices in order; each needs the one before.

| # | Item | Mod | Owner's decision | Status |
| --- | --- | --- | --- | --- |
| H0 | Measurement (a dev timing readout) and the NPC lite tier, with the setting to run the full model for everyone | Framework | Lite by default, setting (9 Nov 2026) | Done: Framework 0.25.0 |
| H1 | The Health tab replacing Vehicle Framework's: VF's stats with impactor tooltips, parts grouped pawn-style with the stages and refusals under them, a crew row, hover highlight, on the world map too; a setting keeps VF's tab | Framework | First deliverable (9 Nov 2026) | Done: Framework 0.26.0 |
| H2 | Conditions proper: per-part severities with stages, from hits, stages and wear; resolved by repair, fading or a service | Framework | Architecture chosen (9 Nov 2026); Def shape waits for a read of Vehicle Framework's develop branch | Open |
| H3 | Consequences and repair depth: leaks with the danger clock, destroyed parts needing supplies (setting, on), field fixes, permanent conditions; wreck-on-death as a separate question | Framework, Vehicles | Supplies for destroyed parts and serious conditions (9 Nov 2026) | Open |
| H4 | The performance table in the record, filled from the owner's runs (Dubs Performance Analyzer, the 1.7 Frame Profiler, our readout) | docs | | Open: the owner runs the protocol |

## Caustic gas, the rest (9 Nov 2026)

| # | Item | Owner's decision | Status |
| --- | --- | --- | --- |
| C5 | Lasting eye damage after a severe caustic eye injury | Our own corneal scarring | Done: Medical 0.8.0 |
| C6 | Plants and corpses in dense gas | Plants wilt, corpses unaffected | Done: Medical 0.8.0 |
| C7 | Supplied oxygen and chemical lung injury | Oxygen eases and speeds recovery | Done: Medical 0.8.0 |
| C8 | Contaminated clothing | The shower is enough by default; extra decontamination with Dubs Bad Hygiene; strip and rinse as a main-menu setting | Done: Medical 0.8.0 |

## Ideas, not decided (9 Nov 2026)

Agent proposals made after the electric vehicles and caustic gas builds, kept here at the owner's request so they are not lost. None is
decided or started; each needs the owner's go.

| # | Idea | Mod | Why | Status |
| --- | --- | --- | --- | --- |
| I1 | A diesel-electric utility vehicle that powers a base: parked, its generator feeds the power grid (a mobile generator set) | Vehicles, Framework | The owner wants diesel-electric utility vehicles with mechanics of their own (V3); the engine model, traction motor heat and the electrical system already exist | Idea |
| I2 | A small battery buffer on a diesel-electric (a hybrid), so it can creep quietly a short way on electricity alone | Framework | Builds on the traction battery of the electric vehicles (V5) | Idea |
| I3 | Choose a vehicle's class (light, medium, heavy) by its empty mass instead of Vehicle Framework's Mass | Framework | Loadouts still use VF's Mass, which is not comparable between mods; the empty masses (V8) would sort Vanilla Vehicles Expanded's tanks into the heavy class | Idea |
| I4 | A field decontamination kit for caravans: strip and rinse away from any shower | Medical | Strip and rinse (C8) needs a shower; there is none in the wild | Idea |
| I5 | A night heater for electric vehicles, drawing a share of the traction battery an hour | Framework | Left out of V5: the night heater's fuel is counted in chemfuel units | Idea |

## Housekeeping (9 Nov 2026)

| # | Item | Status |
| --- | --- | --- |
| K1 | `python scripts/cleanup.py --apply`: 297 old install backups, about 400 test repositories, 220 git watcher daemons | Open: the owner runs it |
| K2 | Another session's uncommitted edits in the tree since 8 Nov 2026 (`docs/development/README.md`, `project-direction.md`, the new `rimworld-1.7-readiness.md`) | Done: the owner confirmed every session finished (9 Nov 2026); committed as that session's dated records |

## Analysis tooling (from 7 Nov 2026)

Built in the shared [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit) repository and consumed here; the order and
reasons are in [analysis tooling](analysis-tooling.md). Each tool's first change here adds its rule to `AGENTS.md`.

| # | Item | Language | Status |
| --- | --- | --- | --- |
| T0 | Create phobos-modkit (public, MIT) with its documents | docs | Done: documents only |
| T1 | Log triage, with the RimWorld adapter | Python | Done: phobos-modkit 0.1.1, `scripts/triage-log.py`, AGENTS.md rule |
| T2 | Save index and query, working slice | Rust | Done: phobos-modkit 0.2.2, `scripts/query-save.py`, AGENTS.md rule |
| T2b | Save index: map grids (terrain, roofs, gas) and comparing two saves by load ID | Rust | Done: phobos-modkit 0.6.0, `grid` and `diff` in `scripts/query-save.py` |
| T3 | Mod and Def index (SQLite in the kit's cache) | Python | Done: phobos-modkit 0.4.0, `scripts/query-defs.py`, AGENTS.md rule |
| T4 | Assembly metadata reader | C# | Open |
| T5 | Workshop research | Python | Done: phobos-modkit 0.5.0, `scripts/workshop-research.py`, AGENTS.md rule |
| T6 | StartupImpact sessions | Python | Open |
| T7 | Version readiness scan | Python | Open |
| T8 | Release tooling | Python | Open |
| T9 | Shared report model and derived-data cache | Python | Done: phobos-modkit 0.3.0 |
| T10 | Game adapter contract (generic log and save commands) | Python | Waits for a second game adapter |
| T11 | Resolved-Defs dump: a D'thorga Framework dev action the kit indexes, so Defs show with patches applied | C# and Python | Proposal: needs the owner's go-ahead |

## Deferred

| Item | Reason |
| --- | --- |
| Hydrogen as a room gas, and the four vents (extractor fan, passive vent, fume hood, hydrogen detector) | The owner left it out of this round (7 Nov 2026); [ventilation and hydrogen](ventilation-and-hydrogen.md) |
| Radio Framework: the three defaults of 19 Oct, licence tiers, Phase 2, the public name | "Later, when I have a better idea of everything" (owner, 7 Nov 2026) |
| D'thorga treatments for IRBM's other strategic warheads | After the owner has tested the nuke |
| Generated art (the store's frost, the electrolyser, the chamber redraw, a whip, the radio base station, the new equipment) | Only on the owner's "go"; new pieces ship with scripted placeholders |
| A base class for interval map components in the Framework | Would change the timing code of parts the owner has tested; waits until one is changed for its own sake |
| Workshop publication tooling | At the first public beta, per AGENTS.md |
