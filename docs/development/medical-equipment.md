# Medical equipment: research and decisions (5 Nov 2026)

The record behind [D'thorga Medical Equipment](../../mods/PhobosMedicalEquipment/README.md). **The owner reports a patient's breathing improving on the concentrator (5 Nov 2026, on 0.1.0) and the burn unit bed working (6 Nov 2026, on 0.2.0); the bedside stand and the rest are not yet tested in game.** The wider plan, with the oxygen chain and the art, is in the [art automation and medical equipment design](art-automation-and-medical-equipment.md).

## The problem

The owner wants our own medical equipment, hospital and field. Carbon monoxide and smoke-damaged lungs from D'thorga Burns and Smoke had no treatment beyond clean air and time.

## What exists, read from the code (5 Nov 2026)

- **The game's vitals monitor** and **VFE Medical's IV stand** are one-tile `CompProperties_Facility` buildings with `mustBePlacedAdjacent`, listed in a bed's `CompProperties_AffectedByFacilities`. The IV stand is added by a patch naming `HospitalBed`. `CompFacility.LinkedBuildings` gives the beds, `CanBeActive` checks power, and a null `statOffsets` is safe.
- **Vanilla Gravship Expanded:** `VGE_OxygenCanister` is an item, explosive when burned. `VGE_OxygenNet` lists it in `linkToRefuelables`, and Vanilla Expanded Framework's `AutoLinkRefuelables` adds a pipe link to any refuelable building whose fuel filter accepts it.
- **Organ Failure's ventilation** tests for the hediff `QE_LifeSupport`, which no mod the owner has installed defines, so the surgery never appears in the owner's game.
- **Clearance:** carbon monoxide's half-life is about 320 minutes on room air and 70 to 80 on oxygen ([sources](burns-and-smoke.md#sources)).

## Decisions

| Decision | By |
| --- | --- |
| Automatic: a patient in a linked bed who needs oxygen gets it, with no job | Owner, drawing on the IV stand and vitals monitor |
| A concentrator as well as the stand, so the mod works without Vanilla Gravship Expanded | Owner |
| Oxygen clears carbon monoxide faster and helps breathing, but does not repair lungs | Owner |
| No Questionable Ethics: the owner feels it is done poorly and that we can do better. A ventilator of our own stays on the list | Owner |
| Vanilla Expanded Framework is a required dependency of this mod | Owner, 4 Nov 2026. This version uses it only through Vanilla Gravship Expanded's pipes; the electrolyser and liquid store will need it directly |
| Its own mod, named D'thorga Medical Equipment | Agent choice |
| Beds are found by accepting a vitals monitor, not by name, so modded hospital beds qualify | Agent choice, following the owner's rule for Emergency Dosing |
| A stand exists only with Vanilla Gravship Expanded, so it never sits waiting for an item that cannot exist | Agent choice |
| Oxygen is a condition on the patient that the machine keeps refreshing. It lapses by itself, so a destroyed machine or a removed mod leaves nothing behind for long | Agent choice |
| The shared pieces (`PhobosBreathing` and its two extensions) live in the framework, so Burns and Smoke needs no dependency on this mod | Agent choice |
| A burn unit bed built on the game's hospital bed, its infection benefit through tend quality with no patch; figures authored | Owner's go, agent's design, 5 Nov 2026 |
| The bedside stand uses a picked, generated image; the concentrator keeps its scripted placeholder for now | Owner, 5 Nov 2026 |
| A machine or bed names an occupant it leaves alone, and says why | Agent choice, 6 Nov 2026, after the owner read a correct hold-back as a fault |

## Figures

| Figure | Value | Source |
| --- | ---: | --- |
| Carbon monoxide half-life on oxygen | 75 min | Sourced, the middle of 70 to 80 |
| Breathing offset, capped at normal, and none for a patient already at or above normal | +0.2 | Authored |
| Breathing below which a patient needs oxygen | 0.8 | Authored |
| Concentrator power | 350 W | Authored, inside the real 300 to 600 W range from memory and unverified |
| Patients per machine | 2 | Authored |
| Canisters per patient per hour | 1 | Authored |

**Simplifications:** a concentrator gives about 93% oxygen, treated as the same as a canister. A mask is assumed, with no job to fit it. A patient on oxygen still takes in the room's carbon monoxide.

## Test protocol (owner runs it)

1. **Load:** the load line names the concentrator and says whether the stand is on. No red lines.
2. **Placement:** build a concentrator touching a hospital bed. Lines show the link, and without power it says "Waiting for power".
3. **Treatment:** in dev mode give a patient carbon monoxide in that bed. Within a few seconds they get "on oxygen", and the machine names them.
4. **Clearance:** the poison should halve about every 1.25 in-game hours, against about 5.3 for a control patient on a plain bed.
5. **Stopping:** flick the machine off, or move the patient. "On oxygen" should fall away shortly after.
6. **The stand:** with Vanilla Gravship Expanded, research Oxygen Network, build the stand, and watch it take canisters. Lay an oxygen pipe to it and check that it refills. This pipe link is the riskiest untested piece.
7. **Save and reload:** the condition and the machines survive with no errors.

## Results from play

| Date | Version | What the owner saw | What it means |
| --- | --- | --- | --- |
| 5 Nov 2026 | 0.1.0 | A patient's breathing improves on the concentrator | Oxygen's breathing offset works |
| 6 Nov 2026 | 0.2.0 | The burn unit bed looks good, and a burned patient gets "on air cells" with its effects listed | The bed, its art and its treatment work |
| 6 Nov 2026 | 0.2.0 | The oxygen machine beside the bed did not give the same patient oxygen | Most likely working as designed: the Health tab shows breathing at 100%, and no smoke condition is in view (the list scrolls, so that part is read from a screenshot). The fault was silence, fixed in 0.2.1 by naming the patient and the reason |

## Open points

- A field version: a mask and regulator a medic carries.
- A ventilator of our own. Under the owner's rule it earns its place, because nothing else exists in the owner's game.
- More Injuries' hypoxia counts as needing oxygen, but only breathing eases it. Whether to help it more depends on its handlers.
- The electrolyser, the liquid oxygen store with its frost, burn dressings, the resuscitator, the hyperbaric chamber and the burn unit bed.
