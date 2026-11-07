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
| R5 | After a nuke, an ally's RimPacts assault "never eventuated" and the battle was lost; about twelve towns were lost later | RimPacts trims the assault to the defenders left standing, about two after the strike ([record](missile-patches.md)) | Done: Patches 0.11.0, the assault faces the pre-strike garrison. The town losses are unexamined: they happened after the last save; needs the letter or a replay |
| R4 | (Found in the log) 7,662 errors from the IRBM strike job after loading another game | The job named a closed map in a log line, and the Framework's queue could not empty itself | Done: Framework 0.8.1 and Patches 0.11.0 |

## Analysis tooling (from 7 Nov 2026)

Built in the shared [phobos-modkit](https://github.com/phobos-dthorga/phobos-modkit) repository and consumed here; the order and
reasons are in [analysis tooling](analysis-tooling.md). Each tool's first change here adds its rule to `AGENTS.md`.

| # | Item | Language | Status |
| --- | --- | --- | --- |
| T0 | Create phobos-modkit (public, MIT) with its documents | docs | Done: documents only |
| T1 | Log triage, with the RimWorld adapter | Python | Open |
| T2 | Save index and query, working slice | Rust | Open |
| T3 | Mod and Def index | Python | Open |
| T4 | Assembly metadata reader | C# | Open |
| T5 | Workshop research | Python | Open |
| T6 | StartupImpact sessions | Python | Open |
| T7 | Version readiness scan | Python | Open |
| T8 | Release tooling | Python | Open |

## Deferred

| Item | Reason |
| --- | --- |
| Hydrogen as a room gas, and the four vents (extractor fan, passive vent, fume hood, hydrogen detector) | The owner left it out of this round (7 Nov 2026); [ventilation and hydrogen](ventilation-and-hydrogen.md) |
| Radio Framework: the three defaults of 19 Oct, licence tiers, Phase 2, the public name | "Later, when I have a better idea of everything" (owner, 7 Nov 2026) |
| D'thorga treatments for IRBM's other strategic warheads | After the owner has tested the nuke |
| Generated art (the store's frost, the electrolyser, the chamber redraw, a whip, the radio base station, the new equipment) | Only on the owner's "go"; new pieces ship with scripted placeholders |
| A base class for interval map components in the Framework | Would change the timing code of parts the owner has tested; waits until one is changed for its own sake |
| Workshop publication tooling | At the first public beta, per AGENTS.md |
