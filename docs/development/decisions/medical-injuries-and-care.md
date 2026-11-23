# Decisions: injuries and care (D'thorga Medical)

**Status (23 Nov 2026):** living; harvested from the records' "Decisions", owner's answers and settled points. One row a
decision. How it works now: [the system page](../systems/medical-injuries-and-care.md). Dates are 2026. **Who**: owner;
owner's card (a decision card answered); agent default or agent choice (the owner may revise either). A later row that
overturns an earlier one says so. Decisions on the shared health layer are in [its log](health-layer.md), and on gases, carbon
monoxide's source and fire heat in [gases and air](gases-and-air.md); they are not repeated here.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 4 Nov | Burned fraction derived each pass from the burns, not accumulated per hit | agent choice | Sees tending, healing, CE's burns | [bs], Decisions |
| 4 Nov | One giver in OrganicStandard; humanlike only; early outs | agent choice | Cheap for everyone else | [bs], Decisions |
| 4 Nov | A part burned off is its whole subtree burned, fading over 15 days | agent choice | A lost part is wholly burned | [bs], Decisions |
| 4 Nov | Scars are not burns | agent choice | A healed pawn left in shock for life | [bs], Decisions |
| 4 Nov | Past 15 % burned the shock target is at least 0.1 | agent choice | The fluids job always shows | [bs], Decisions |
| 4 Nov | Area and depth are two numbers; area authored per burn kind | owner | A round small and deep, fire wide | [bs], Decisions |
| 4 Nov | Shock care scales with medicine; no burn ward needed | owner | Playable over exact | [bs], Decisions |
| 4 Nov | A Baux score of 110 stays terminal untreated | owner | Authored mark kept | [bs], Decisions |
| 4 Nov | Burn shock tendable; its tend stands for fluids | agent choice | The game's own tending | [bs], Decisions |
| 4 Nov | Death through stage clocks, not a kill in code | agent choice | Visible in XML and to Search and Rescue | [bs], Decisions |
| 4 Nov | A closed airway sets breathing to 0, no lethal severity | agent choice | Organ failure mods can step in | [bs], Decisions |
| 4 Nov | More Injuries' lung-burn worker removed (overturned 7 Nov) | owner | Avoid burning lungs twice | [bs], Decisions |
| 4 Nov | Tracheotomy: no research alone; Emergency Medicine with More Injuries | agent choice | No medical research to hang it on | [bs], Decisions |
| 5 Nov | Painkillers found by effect from the Defs; an opt-out extension | owner | Effects, not names | [ed], Decisions |
| 5 Nov | Dosing as a mod on the Framework only, no Harmony | agent choice | Builds on GitHub | [ed], Decisions |
| 5 Nov | A sedative is a consciousness ceiling at 0.1 or less, not an offset | agent choice | Morphine is not a sedative | [ed], Decisions |
| 5 Nov | Indexed only with 30 % relief and a score above zero | agent choice | Epinephrine was spent on pain | [ed], Decisions |
| 5 Nov | Tiers: medical, then sedative, then recreational | agent choice | A score alone put heroin first | [ed], Decisions |
| 5 Nov | No stacking: a quarter of a dose still working blocks the next | agent choice | Relief and costs stack | [ed], Decisions |
| 5 Nov | Pain relief from 0.6, sedation from 0.8; resting patients only (0.6 overturned 7 Nov) | agent choice | A working pawn could drop | [ed], Decisions |
| 5 Nov | Choice by score within a tier; the pain level ignored | agent choice | The same pick each time | [ed], Decisions |
| 5 Nov | Rules first, reach last | agent choice | No path search for a refused item | [ed], Decisions |
| 5 Nov | Safety: quarter dose, overdose 0.3, danger stage, floor 0.15 | agent choice | The game kills at zero | [ed], Decisions |
| 5 Nov | Patients others reserve are skipped (overturned 17 Nov: field care) | agent choice | Their doctors keep their patients | [ed], Decisions |
| 5 Nov | Circulatory support left to More Injuries and Search and Rescue | agent choice | Its benefit is in their code | [ed], Decisions |
| 5 Nov | The dose through the game's ingestion toils | agent choice | Items work as their mods wrote | [ed], Decisions |
| 5 Nov | Disease drugs from doctors only | owner | A pawn cannot judge the race | [ed], Drugs for fighting disease, and the bleeding hold (0.3.0, 5 Nov 2026) |
| 5 Nov | Disease drug: immunity gain 0.1 or more, no disease | owner | Effect | [ed], Drugs for fighting disease, and the bleeding hold (0.3.0, 5 Nov 2026) |
| 5 Nov | Only when losing, by one day (overturned 7 Nov) | owner | Else none | [ed], Drugs for fighting disease, and the bleeding hold (0.3.0, 5 Nov 2026) |
| 5 Nov | Pain first, then the disease drug | agent choice | Both needs | [ed], Drugs for fighting disease, and the bleeding hold (0.3.0, 5 Nov 2026) |
| 5 Nov | Bleeding hold at 30 % a day; sedatives exempt | owner | Unseen bleed | [ed], Drugs for fighting disease, and the bleeding hold (0.3.0, 5 Nov 2026) |
| 5 Nov | Self-medication gated by the drug policy | owner | The player keeps control | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Self-medication from 40 % pain; interest 30 %, fascination 20 % | owner | Authored offsets | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Free colonists self-medicate; guests by a setting | owner | Hospitality players | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Adults only; not slaves or prisoners | agent choice | Left to the doctors | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Guests, with no policy: medical painkillers only | agent choice | Nothing to gate by | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Chemical interest and fascination bypass the policy | agent choice | The game's own precedent | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Anyone in bed or resting is left to the doctors | agent choice | A healing wound included | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | The doctors' safety and choice; a recreational dose blocks the next | agent choice | Highs would stack | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Floor 0.4 after a dose on one's feet | agent choice | Below 0.3 the game drops a pawn | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | The game's own ingest job; no new job | agent choice | The item behaves as usual | [ed], Self-medication (0.2.0, 5 Nov 2026) |
| 5 Nov | Caustic gas: two models behind a setting, slow-acting by default | owner | Choice of feel | [cg], Decisions |
| 5 Nov | Caustic effects live in the burns and smoke part | owner | It owns the airway and lungs | [cg], Decisions |
| 5 Nov | Caustic gas a game design: every figure authored | owner | No real-agent data | [cg], Decisions |
| 5 Nov | Existing effects first: buildup, airway burn, lung injury, chemical burn | owner | Owner's rule | [cg], Decisions |
| 5 Nov | Own conditions only where none exists: dose, eye, immunity, airways | owner | Owner's rule | [cg], Decisions |
| 5 Nov | The game's buildup formula kept; our tracks read their own exposures | agent choice | A kidney should not save eyes | [cg], Decisions |
| 5 Nov | Irritable airways as the aftermath, not lung rot | agent choice | Lung rot too lethal | [cg], Decisions |
| 6 Nov | Triage as a mod with no Harmony | agent choice | Every hook a Def or component | [tr], Decisions |
| 6 Nov | Drug addictions and dependencies left out of triage | owner | Their treatment is no operation | [tr], Decisions |
| 6 Nov | "Any repair" also queues other conditions; they never set the tag | agent choice | Repairs without false alarms | [tr], Decisions |
| 6 Nov | Lethal and crippling thresholds 0.3, 0.5, -0.5; a cap crippling at most | agent choice | The game kills at zero | [tr], Decisions |
| 6 Nov | Operations by removesHediff plus an extension | agent choice | Every remove worker keeps it | [tr], Decisions |
| 6 Nov | Highest expected success, then cheapest; refusals nearest-first | agent choice | One thing to change | [tr], Decisions |
| 6 Nov | Risk a setting, 5 % by default | agent default | Thoracotomy well under it | [tr], Decisions |
| 6 Nov | Rare tool: non-medicine ingredient worth 300 or more; allowed | agent choice | Authored | [tr], Decisions |
| 6 Nov | A ledger; only triage's bills withdrawn | agent choice | The player's bills untouched | [tr], Decisions |
| 6 Nov | A lethal bill goes to the top | agent choice | A rules flag | [tr], Decisions |
| 6 Nov | One pass per map every 250 ticks, maps offset | agent choice | Bills before a doctor is idle | [tr], Decisions |
| 6 Nov | Tags: immediate 6 h or vital 0.3, urgent, delayed, minor, expectant | agent choice | Authored bands | [tr], Decisions |
| 6 Nov | A dying condition no operation removes is never expectant | agent choice | Carbon monoxide was tagged | [tr], Decisions |
| 6 Nov | Score bands of 100; less time higher; expectant no urgency | agent choice | The tag outranks the clock | [tr], Decisions |
| 6 Nov | Order of work: a prioritised operate giver at 75 | agent choice | Worst case first | [tr], Decisions |
| 6 Nov | Text stays in the world; Plain shows no hours | owner | Owner's rule | [tr], Decisions |
| 7 Nov | Respect More Injuries' fire inhalation setting (overturns 4 Nov) | owner | Its player's choice decides | [bs], Open points and upstream |
| 7 Nov | Our lung injury stands down while burning with its inhalation on | agent choice | Narrowest overlap | [bs], Open points and upstream |
| 7 Nov | Eschar at 60 % of a limb's health | owner | Stand-in for all round | [bs], Phase 2: eschar, escharotomy, ischaemia and fluids (7 November 2026) |
| 7 Nov | Eschar formed and released each pass | agent choice | Heals, softens | [bs], Phase 2: eschar, escharotomy, ischaemia and fluids (7 November 2026) |
| 7 Nov | Eschar 0.05, +1.5 a day; no blood at 0.7 | agent choice | Authored | [bs], Phase 2: eschar, escharotomy, ischaemia and fluids (7 November 2026) |
| 7 Nov | Escharotomy: Medicine 4, no research | agent choice | Simpler | [bs], Phase 2: eschar, escharotomy, ischaemia and fluids (7 November 2026) |
| 7 Nov | More Injuries' gangrene, else ours | owner | Never both | [bs], Phase 2: eschar, escharotomy, ischaemia and fluids (7 November 2026) |
| 7 Nov | Fluids bill with a saline bag, quality 1 | owner | Burn shock fluids | [bs], Phase 2: eschar, escharotomy, ischaemia and fluids (7 November 2026) |
| 7 Nov | Caustic tracks follow peaks, not current doses | agent choice | Fading doses stopped the lungs | [cg], As built (D'thorga Medical 0.7.0, 7 Nov 2026) |
| 7 Nov | Caustic skin damage a point at a time, ignoring armour | agent choice | Clothes counted | [cg], As built (D'thorga Medical 0.7.0, 7 Nov 2026) |
| 7 Nov | Chemical burn rinsable; a rinse clears the skin dose | agent choice | Shower decontaminates | [cg], As built (D'thorga Medical 0.7.0, 7 Nov 2026) |
| 7 Nov | Immunity reads the higher of the eye and lung peaks | agent choice | Either route harms | [cg], As built (D'thorga Medical 0.7.0, 7 Nov 2026) |
| 7 Nov | A master switch, the fast model, a toggle per track | agent choice | Each harm optional | [cg], As built (D'thorga Medical 0.7.0, 7 Nov 2026) |
| 7 Nov | Fussing: tend quality 0.9 at extreme pain, 0.75 at full | owner | Pain makes tending harder | [ed], Fussing (D'thorga Medical 0.7.0, 7 Nov 2026) |
| 7 Nov | Fussing by a postfix on base tend quality | agent choice | No hook for the patient's state | [ed], Fussing (D'thorga Medical 0.7.0, 7 Nov 2026) |
| 7 Nov | Doctors give pain relief from 40 % (overturns 5 Nov) | owner | Settled after play | [ed], Open points and upstream |
| 7 Nov | Immunity margin two days (overturns 5 Nov) | owner | One case lost by a point | [ed], Open points and upstream |
| 7 Nov | Pawns who welcome pain skip self-medication | owner | Masochists, idealised pain | [ed], Open points and upstream |
| 7 Nov | Triage tags as coloured icons by the name; expectant shown | owner | Field triage colours | [tr], Open points and upstream |
| 7 Nov | The tag in Useful Marks' row, else our own postfix | agent choice | The game draws no icons there | [tr], Open points and upstream |
| 7 Nov | Fractures queued under "save life or a limb" | owner | Rather than splints alone | [tr], Open points and upstream |
| 7 Nov | More Injuries' brain therapy found by extension; auto-queued | agent choice | Its worker names nothing | [tr], Open points and upstream |
| 8 Nov | A burned eye leaves our own scarred cornea, by how bad | owner | Lasting, partial harm | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 8 Nov | Plants in the gas wilt; corpses untouched | owner | Owner's answer | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 8 Nov | Plants scorched from density 0.3, 20 hit points an hour | agent choice | Authored | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 8 Nov | A shower is enough by default; strip and rinse a setting, off | owner | Owner's answer | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 8 Nov | Clothing residue rates 0.6, 0.3, 0.25 a day, clean under 0.05 | agent choice | Authored | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 8 Nov | Dubs Bad Hygiene's washing rinses as a safety shower | agent choice | Any wash counts | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 8 Nov | "Allow contaminated" off in new policies and stockpiles | agent choice | Soaked clothes stay off | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 9 Nov | No new eschar on a released limb | owner | Not undone | [bs], Phase 2: eschar, escharotomy, ischaemia and fluids (7 November 2026) |
| 9 Nov | A fresh exposure restarts the caustic clock | owner | A second gassing as serious | [cg], Open points and upstream |
| 9 Nov | "Fresh" means gas met after 4 hours in clear air | agent choice | Authored | [cg], Open points and upstream |
| 11 Nov | Coughing, never doubled with another mod's; it stands down | owner | No double sounds | [sc], Owner's answers (11 Nov 2026, going to bed) |
| 13 Nov | VFE Medical's drip stand gives burn-shock fluids | owner | Uses the hospital built | [sd], Questions for the owner |
| 13 Nov | The drip's fluids at quality 0.7, below the kit's 0.9 | agent choice | A hung kit stays better | [sd], Questions for the owner |

[bs]: ../archive/burns-and-smoke.md
[cg]: ../archive/caustic-gas.md
[tr]: ../archive/triage.md
[ed]: ../archive/emergency-dosing.md
[sd]: ../soft-dependency-crosslinks.md
[sc]: ../sound-catalogue.md
