# Medical equipment: research and decisions (5 Nov 2026, updated 7 Nov 2026)

The record behind [D'thorga Medical Equipment](../../mods/PhobosMedicalEquipment/README.md). **The owner reports a patient's breathing improving on the concentrator (5 Nov 2026, on 0.1.0) and the burn unit bed working (6 Nov 2026, on 0.2.0); the bedside stand and everything added in 0.3.0 are not yet tested in game.** The wider plan, with the oxygen chain and the art, is in the [art automation and medical equipment design](art-automation-and-medical-equipment.md).

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
| The concentrator and the bedside stand use generated images the owner picked from the first batch (references only, no LoRA), shipped in 0.2.3 | Owner, 7 Nov 2026 |
| A machine or bed names an occupant it leaves alone, and says why | Agent choice, 6 Nov 2026, after the owner read a correct hold-back as a fault |
| Build the outstanding pieces: the hyperbaric chamber, the electrolyser, the liquid oxygen store, the field oxygen kit, burn dressings and the manual resuscitator, with placeholders where art is outstanding | Owner, 7 Nov 2026 |
| **Everything that stores or pipes oxygen joins Vanilla Gravship Expanded's network** and exists only with it, instead of a second network of our own when it is absent. A network of ours would need its own pipes and their art, and would duplicate one the owner has. This narrows the 5 Nov design, which had our own chain "used when Vanilla Gravship Expanded's is absent": for the owner to revise | Agent choice, 7 Nov 2026, under the owner's rule of using what exists |
| The chamber is a bed, 1 by 3: the game gives a bed one sleeping place per tile of width, so a 2-wide chamber would hold two | Agent choice, 7 Nov 2026 |
| The burn dressing is a medicine, not a burn-only treatment: the game's infection chance has no patient-side modifier (it is the tend's quality, the room and the wound, read from the 1.7 code), so a better tend is the honest route with no patch. Real silver dressings are used on other wounds too | Agent choice, 7 Nov 2026 |
| The field kit and the hand pump are one Doctor job, driven by a Def extension on the item, so other mods add devices in XML. It lives in this mod, not the framework, until a second mod needs it | Agent choice, 7 Nov 2026 |
| Devices are matched by what the patient's body shows (needing oxygen; breathing under half), never by a condition's name | Owner's rule (5 Nov 2026) |
| The treatments the VGE-only pieces give are defined with the core Defs, so a save loads cleanly without that mod | Agent choice, 7 Nov 2026 |
| The electrolyser costs what real electrolysis costs, far more than Vanilla Gravship Expanded's harvester by its own balance; its place is vacuum and thin air. The figure lives in XML, for the owner to tune | Agent choice, 7 Nov 2026, for the owner to confirm |

## Figures

| Figure | Value | Source |
| --- | ---: | --- |
| Carbon monoxide half-life on oxygen | 75 min | Sourced, the middle of 70 to 80 |
| Breathing offset, capped at normal, and none for a patient already at or above normal | +0.2 | Authored |
| Breathing below which a patient needs oxygen | 0.8 | Authored |
| Concentrator power | 350 W | Authored, inside the real 300 to 600 W range from memory and unverified |
| Patients per machine | 2 | Authored |
| Canisters per patient per hour | 1 | Authored |
| Treatment linger after the last pass | 600 ticks, on the condition's comp since 0.2.2 | Authored |
| Burn unit bed: tend quality | +0.15 | Authored |
| Burn unit bed: wound healing and pain on air cells | x1.25 and x0.85 | Authored |
| Burn unit bed: power, cost, skill | 200 W; 120 steel, 30 cloth, 7 components; Construction 8; Hospital bed research | Authored |
| One oxygen network unit (one canister) | One patient-hour, about 0.3 cubic metres at 5 L/min | Authored, from the stand's rate |
| Carbon monoxide half-life, hyperbaric | 22.5 min | Sourced, the middle of 15 to 30 ([burns and smoke](burns-and-smoke.md#sources)) |
| Hyperbaric chamber: breathing, oxygen use, power | +0.3 up to normal; 6 canisters a patient-hour; 800 W | Authored. Real chambers use far more oxygen (from memory, unverified) |
| Field oxygen kit: duration | One in-game hour | Authored; a small medical cylinder of a few hundred litres at 5 L/min (from memory, unverified) |
| Hand ventilation: breathing, threshold | +0.3 up to normal, for breathing under 0.5 and above 0 | Authored |
| Electrolyser: power for one unit an hour | 3000 W | Our arithmetic: 9 to 12 kWh per cubic metre of oxygen from a published 4.5 to 6 kWh per cubic metre of hydrogen ([source](https://arts.units.it/retrieve/e2913fde-cecd-f688-e053-3705fe0a67e0/energies-14-05347-v2_compressed2.pdf)) |
| Liquid oxygen store: capacity | 2000 units in 2 by 2, four times the small gas tank | Our estimate: about 862 litres of gas per litre of liquid ([Princeton EHS](https://ehs.princeton.edu/laboratory-research/chemical-safety/cryogenic-liquids)) against a gas tank assumed at about 200 bar |
| Liquid oxygen store: cooler, boil-off | 300 W; 2% a day without power | Authored. Real vacuum-insulated stores lose well under 1% a day with no power at all (from memory, unverified): the cooler is a gameplay simplification |
| Silver burn dressing: potency, quality cap, value | 0.80, 0.85, 14 silver | Authored, between herbal (0.60) and industrial (1.00) medicine |

**Simplifications:** a concentrator gives about 93% oxygen, treated as the same as a canister. A bedside machine's mask is assumed, with no job to fit it. A patient on oxygen still takes in the room's carbon monoxide. The electrolyser's water is assumed, and its hydrogen vanishes. The field kit's regulator is thrown away with its cylinder. A chamber doesn't stop surgery and isn't preferred for patients who need it. The hand pump can't help breathing at zero, a sharper line than real life, where a partly blocked airway can sometimes still be ventilated.

## Test protocol (owner runs it)

1. **Load:** the load line names the concentrator and says whether the stand is on. No red lines.
2. **Placement:** build a concentrator touching a hospital bed. Lines show the link, and without power it says "Waiting for power".
3. **Treatment:** in dev mode give a patient carbon monoxide in that bed. Within a few seconds they get "on oxygen", and the machine names them.
4. **Clearance:** the poison should halve about every 1.25 in-game hours, against about 5.3 for a control patient on a plain bed.
5. **Stopping:** flick the machine off, or move the patient. "On oxygen" should fall away shortly after.
6. **The stand:** with Vanilla Gravship Expanded, research Oxygen Network, build the stand, and watch it take canisters. Lay an oxygen pipe to it and check that it refills. This pipe link is the riskiest untested piece.
7. **Save and reload:** the condition and the machines survive with no errors.
8. **Burn unit bed:** a pawn with an open burn in a powered bed gets "on air cells"; with no power the bed says it waits for power
   for its blower; an unburned occupant is named as needing none.
9. The dev action "Log a pawn's oxygen need" gives breathing, need and half-life. A save from 0.2.1 loads with no line about the
   treatment comp (0.2.2 moved it into the framework).
10. **Field kit (0.3.0, needs Vanilla Gravship Expanded):** make one at a machining table. Down a pawn with carbon monoxide outside
    a bed: a doctor fetches the kit and fits it, and the pawn is "on field oxygen" for about an in-game hour.
11. **Hand pump (0.3.0):** make a resuscitator. Give a downed pawn breathing under 50% (dev mode). A doctor pumps until breathing
    recovers, and keeps the resuscitator. Right-click a pawn whose airway is shut: the reason is given.
12. **Chamber (0.3.0):** build it, lay an oxygen pipe to it, put a poisoned patient in it: "under hyperbaric oxygen", with carbon
    monoxide halving about every 22 minutes.
13. **Electrolyser and store (0.3.0):** build both on one oxygen pipe with power: the store fills about one unit an hour. Cut the
    store's power: its inspect line says it boils off. Check the fireball only on a test colony.
14. **Burn dressings:** make some at a drug lab and watch a doctor use them on a tend.

## Results from play

| Date | Version | What the owner saw | What it means |
| --- | --- | --- | --- |
| 5 Nov 2026 | 0.1.0 | A patient's breathing improves on the concentrator | Oxygen's breathing offset works |
| 6 Nov 2026 | 0.2.0 | The burn unit bed looks good, and a burned patient gets "on air cells" with its effects listed | The bed, its art and its treatment work |
| 6 Nov 2026 | 0.2.0 | The oxygen machine beside the bed did not give the same patient oxygen | Most likely working as designed: the Health tab shows breathing at 100%, and no smoke condition is in view (the list scrolls, so that part is read from a screenshot). The fault was silence, fixed in 0.2.1 by naming the patient and the reason |

## Open points

- A ventilator of our own. Under the owner's rule it earns its place, because nothing else exists in the owner's game.
- More Injuries' hypoxia counts as needing oxygen, but only breathing eases it. Whether to help it more depends on its handlers.
- The store's frost while it works (its art is outstanding) and its look in game (the owner, 7 Nov 2026).
- Whether the electrolyser should draw water from a plumbing mod, and whether its hydrogen should be a gas of ours.
- Whether patients who need it should be sent to the chamber first.
- Whether the electrolyser's honest power figure suits play, or should match Vanilla Gravship Expanded's scale.
- A field needle decompression for a collapsed lung, buying time until D'thorga Triage's operation.
