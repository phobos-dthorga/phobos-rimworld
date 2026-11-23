# Decisions: the health layer and care

**Status (23 Nov 2026):** living; harvested from the records' "Who chose what", owner's decisions and answered questions.
One row a decision. How it works now: [the system page](../systems/health-layer.md). Dates are 2026. **Who**:
owner; owner's card (a decision card answered); agent default or agent choice (the owner may revise either). A later row that
overturns an earlier one says so.

| Date | Decision | Who | Why | Source |
| --- | --- | --- | --- | --- |
| 5 Nov | Treatment a condition its machine refreshes; it lapses alone | agent choice | Nothing left behind for long | [me], Decisions |
| 5 Nov | Dosing's patient list: a per-map cache with a tick stamp, never saved | agent choice | Adding or removing touches no save | [ed], Decisions |
| 6 Nov | Health layer in Framework; Dosing, Equipment adopt it at once | owner | Share, though mods change | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | Dosing takes its patient, care and danger tests and cache from the Framework | owner | The same answers as Triage | [ed], Decisions |
| 6 Nov | A condition's danger read from its effects, never its name | owner | Standing rule | [tr], Decisions |
| 6 Nov | Care choices: a button on each person, plus defaults by group | owner | Per-person control | [tr], Decisions |
| 6 Nov | Defaults: life-saving only; colonists, slaves, prisoners on; guests off | owner | Owner's choices | [tr], Decisions |
| 6 Nov | Patients: who doctors look after, group, care level | agent choice | One answer for every mod | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | Hours to death: soonest of clock mean, climb, bleed-out | agent choice | Mean labelled an expectation | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | Danger rule game-free, behind ConditionDanger | agent choice | Kept apart from the game | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | Doctors: the game's "doctor could come", best surgeon | agent choice | The game's own test | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | Dosing's per-map patient cache moves in as MapPatientCache | agent choice | Already in use | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | MedicalTreatment and lapsing comp move in from Equipment | agent choice | Shared by any mod | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | Linger on the comp's properties, 600 ticks as before | agent choice | Authored; same value | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | CompPhobosPatient: any mod's choices, a button each | agent choice | Registered by each mod | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 6 Nov | Patient comp on humanlikes by startup sweep, not patch | agent choice | A patch misses HAR races | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 7 Nov | Patients needing oxygen go to a chamber first; no surgery in a chamber | owner | Oxygen first | [me], Decisions |
| 7 Nov | SurgeryBars: the mod stopping surgery says why, in-world | agent choice | Asked by Triage's session | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 7 Nov | Queuing mod reads SurgeryBarred; no mod reference | agent choice | No dependency either way | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 7 Nov | A barred operation is held, never withdrawn, and counts as feasible | agent choice | Tag stays by the clock | [tr], Decisions |
| 7 Nov | IsFleshPerson, IsLyingDown, IsLyingPatient shared | agent choice | A vehicle seen as patient | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 7 Nov | Mods add own need and care checks; Dosing adopts later | agent choice | Shared base | [pf], Health layer (0.3.0, 6 Nov 2026) |
| 7 Nov | Care choices also on the health tab; person's button stays | owner | Beside the medicine setting | [tr], Open points and upstream |
| 7 Nov | The care comp ignores a corpse that carries it (0.4.1) | agent choice | Corpse threw a cast error | [pf], Results from play |
| 8 Nov | Lasting damage by peak: Framework's LeavesLasting | agent choice | Scarred cornea first | [cg], Built 8 Nov 2026 (D'thorga Medical 0.8.0) |
| 17 Nov | On the ground, our doctors hold on our own reservation layer | agent choice | Our holds blocked SAR's carriers | [sd], Questions for the owner |
| 17 Nov | It keeps our doctors apart, never a carrier; bed: game's | agent choice | Carriers still lift them | [sd], Questions for the owner |
| 17 Nov | A dose against disease waits for the bed (FieldCareRule) | agent choice | Bed care for disease | [sd], Questions for the owner |
| 22 Nov | Strike dose via RadiationDoses; else Rimatomics | agent choice | The strike graded in grays | [rm], As built (22 Nov 2026) |

[pf]: ../archive/phobos-framework.md
[tr]: ../archive/triage.md
[ed]: ../archive/emergency-dosing.md
[me]: ../archive/medical-equipment.md
[rm]: ../radiation-model.md
[sd]: ../soft-dependency-crosslinks.md
[cg]: ../archive/caustic-gas.md
