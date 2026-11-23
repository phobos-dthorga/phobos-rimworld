# Medical equipment: research and decisions (5 Nov 2026, updated 7 Nov 2026)

**Since 7 November 2026 this mod is a part of [D'thorga Medical](../../../mods/PhobosMedical/README.md)** ([why](../mod-consolidation.md)); paths below are updated, the rest is as recorded.

The record behind [D'thorga Medical Equipment](../../../mods/PhobosMedical/README.md). **The owner reports a patient's breathing improving on the concentrator (5 Nov 2026, on 0.1.0) and the burn unit bed working (6 Nov 2026, on 0.2.0); the bedside stand and everything added in 0.3.0 are not yet tested in game.** The wider plan's medical half, with the oxygen chain, is [at the end of this record](#the-first-designs-medical-half-4-to-5-nov-2026); its art half is in the [art pipeline](../art-pipeline.md).

How gases and air works now: [systems/gases-and-air.md](../systems/gases-and-air.md); this record keeps its history.

How the health layer works now: [systems/health-layer.md](../systems/health-layer.md); this record keeps its history.

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
| The electrolyser costs what real electrolysis costs, far more than Vanilla Gravship Expanded's harvester by its own balance; its place is vacuum and thin air. The figure lives in XML, for the owner to tune | Agent choice, 7 Nov 2026; **confirmed by the owner the same day** |
| Vanilla Gravship Expanded stays optional: it needs the Odyssey DLC, and a hard dependency would lock out players without it. It is strongly recommended: a one-time letter in the colony when it first loads without it, the settings window and the load line | Owner, 7 Nov 2026 (the letter is an approved exception to the in-world text rule) |
| One oxygen network, Vanilla Gravship Expanded's; no network of our own | Owner, 7 Nov 2026, confirming the agent's choice |
| Patients who need oxygen go to a free chamber first, and there is no surgery in a chamber | Owner, 7 Nov 2026 |
| With Dubs Bad Hygiene active, the electrolyser draws its water from the plumbing; without it, water stays assumed | Owner, 7 Nov 2026 |
| The electrolyser's hydrogen becomes a flammable room gas of ours, with vents to deal with it ([ventilation and hydrogen](../ventilation-and-hydrogen.md)) | Owner, 7 Nov 2026 |
| An unpowered liquid oxygen store stops filling; it still gives out what it holds, and boils off | Owner, 7 Nov 2026 |
| The store's frost waits for its art; the code follows | Owner, 7 Nov 2026 |
| The second set of equipment, all eight pieces ([medical equipment, second set](medical-equipment-2.md)) | Owner, 7 Nov 2026 |

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
15. **Chamber first (0.4.0):** with a ready chamber, down a pawn with carbon monoxide in the open: a doctor carries them to the
    chamber, not a hospital bed. Queue an operation for someone in the chamber: it waits, and the chamber's inspect line says so.
16. **Store and letter (0.4.0):** cut a filling store's power: it stops filling and says why. On a test install without Vanilla
    Gravship Expanded, the letter appears once, and not again on the next load.

## Results from play

| Date | Version | What the owner saw | What it means |
| --- | --- | --- | --- |
| 5 Nov 2026 | 0.1.0 | A patient's breathing improves on the concentrator | Oxygen's breathing offset works |
| 6 Nov 2026 | 0.2.0 | The burn unit bed looks good, and a burned patient gets "on air cells" with its effects listed | The bed, its art and its treatment work |
| 6 Nov 2026 | 0.2.0 | The oxygen machine beside the bed did not give the same patient oxygen | Most likely working as designed: the Health tab shows breathing at 100%, and no smoke condition is in view (the list scrolls, so that part is read from a screenshot). The fault was silence, fixed in 0.2.1 by naming the patient and the reason |

## Open points

Updated 7 Nov 2026, after D'thorga Medical 0.6.0; the open work is listed in [outstanding work](../outstanding-work.md).

- **Done in D'thorga Medical 0.7.0** (owner, 7 Nov 2026: supplied oxygen treats More Injuries' hypoxia; not yet tested in game):
  organ hypoxia heals 4 a day faster on oxygen and cerebral hypoxia 2 (authored; the brain's benefit is less certain), times the
  treatment's factor (the chamber 2). Permanent damage never heals. The rate sits on the Framework's `PhobosOxygenResponsiveExtension`
  and the treatment calls `PhobosBreathing.HealOnSuppliedOxygen`, so any mod's condition or treatment can join in XML.
- The store's look in game (the owner, 7 Nov 2026), then its frost once the art exists (art on the owner's go).
- **Done:** Triage reads the Framework's surgery bars and holds an operation for someone in the chamber (Triage 0.2.0); the chamber,
  the unpowered store, the electrolyser's water from Dubs Bad Hygiene and the one-time letter (0.4.0 to 0.5.0).
- **Next, as the owner chose on 7 Nov 2026:** the second set of equipment (after Burns phase 2). Hydrogen and the vents wait.

## For modders (moved from the README, 17 Nov 2026)

Moved again on 24 November 2026, with the README's slimming, to the living page
[D'thorga Medical for modders](../../modding/medical.md), section "Medical equipment".

## The first design's medical half (4 to 5 Nov 2026)

*Moved here on 17 Nov 2026 from `art-automation-and-medical-equipment.md` ("Art automation and medical equipment: design"); kept as written. Its opening and its art half went to the [art pipeline](../art-pipeline.md#the-first-designs-art-half-4-nov-2026-updated-5-nov-2026); its tables of where everything stood and of decisions, and its open questions, were split by row, the medical rows here; the table of where everything stands is as of 7 Nov 2026. Dropped because this record already says them: the paragraph "Pipes come free" (see What exists, above), the bullet on airway support (Decisions: no Questionable Ethics), the paragraph on treatment tiers for carbon monoxide (Figures, and the note under For modders), and the engineering note that it is its own mod (Decisions).*

### Where everything stands

| Work | State | Next step | Record |
| --- | --- | --- | --- |
| D'thorga Framework 0.3.0 | Draft. The owner reports the gas layer and supplied oxygen working (5 Nov 2026); the health layer (6 Nov) is not yet tested in game | The health layer, through Triage | [Framework](phobos-framework.md) |
| D'thorga Burns and Smoke 0.5.0 | Draft. The owner reports burn numbers, progression and the tie-ins working (5 Nov 2026, on 0.4.2); the tracheotomy and the fire heat in the open are not yet tested in game | The fire heat and the tracheotomy | [Burns and smoke](burns-and-smoke.md) |
| D'thorga Emergency Dosing 0.3.1 | Draft. The owner reports doctors' dosing working (5 Nov 2026, on 0.3.0); self-medication and the bleeding hold not yet tested in game | Self-medication | [Emergency dosing](emergency-dosing.md) |
| D'thorga Triage 0.1.2 | Draft, 7 Nov 2026 (0.1.2 adds the button's own icon); not yet tested in game | The owner's first test | [Triage](triage.md) |
| Native 1.7 builds of all ten mods | Built; the owner plays 1.7 and has tested several mods on it | The rest of the owner's tests | [Building](../building.md) |
| Burns and Smoke: eschar, escharotomy | Planned | The owner's go; their test passed the gate on 5 Nov 2026 | [Burns and smoke](burns-and-smoke.md) |
| D'thorga Medical Equipment 0.3.0 | Draft: concentrator, bedside stand, burn unit bed, and in 0.3.0 the hyperbaric chamber, field oxygen kit, manual resuscitator, silver burn dressings, electrolyser and liquid oxygen store, all built on Vanilla Gravship Expanded's oxygen network where they pipe oxygen. The owner reports breathing improving on the concentrator (on 0.1.0) and the burn unit bed working (on 0.2.0); the bedside stand is not yet tested in game | The stand and its pipe link | [Record](medical-equipment.md) |

### Decisions so far

| Decision | By |
| --- | --- |
| Existing effects first; add our own only where it earns more than inference, or nothing exists, or on the owner's call-out; keep a manual override | Owner (rule) |
| We may add our own medical equipment, for both hospital and field | Owner |
| The medical equipment mod may depend on Vanilla Expanded Framework, for its years of track record | Owner |
| Our own oxygen comes from three devices: a concentrator, an electrolyser and a frosted liquid oxygen store | Owner, 5 Nov 2026 (the owner chose electrolysis for the chemistry; the agent moved the frost from the concentrator to the liquid store, because a concentrator runs at room temperature) |
| Put medical equipment in its own mod, not in Burns and Smoke | Agent choice, built 5 Nov 2026 |
| Bedside oxygen first, automatic, with a concentrator too; no Questionable Ethics, and a ventilator of our own later | Owner, 5 Nov 2026 |

### Medical equipment

**Your mods already cover more than the first ideas assumed.** A scan of the Defs in every active mod on 4 Nov 2026 found:

| Need | Already there | How it works (read from its Defs) |
| --- | --- | --- |
| Oxygen supply | Vanilla Gravship Expanded | An oxygen pipe network, pumps, tanks and an oxygen canister item that explodes when burned. Research: Oxygen Network, on the gravship research path |
| Portable oxygen | Vanilla Gravship Expanded | Oxygen packs, consumed in vacuum |
| Emergency oxygen | The Dead Man's Switch - Expedition | An oxygen candle, a sodium chlorate device for sealed cabins |
| Ventilator | More Injuries: Organ Failure | A surgery that intubates the patient. **It cannot appear in the owner's game (read from its source, 5 Nov 2026):** it needs the hediff `QE_LifeSupport`, from Questionable Ethics' life support, and no installed mod defines that |
| IV stand | VFE Medical Module | A facility beside a hospital bed that adds immunity gain speed |

None of the oxygen above treats a patient yet, as far as these Defs show: it serves vacuum and sealed rooms.

**Under the owner's rule, reuse these where present:**

- **Medical oxygen** uses Vanilla Gravship Expanded's canister or pipe network when that mod is active, as an optional tie-in.
  Without it, we supply a simple oxygen item of our own, because nothing else would exist.

#### The oxygen chain (owner, 5 Nov 2026)

**Built in Medical Equipment 0.3.0 on Vanilla Gravship Expanded's oxygen network, not a second network used when it is absent** (agent choice, 7 Nov 2026, for the owner to revise; reasons in the [medical equipment record](medical-equipment.md)). The game values are there, each labelled.

| Device | Needs | Gives | The real thing |
| --- | --- | --- | --- |
| **Oxygen concentrator** (bedside, field) | Power | Oxygen for one patient, with no storage | Pressure-swing adsorption: zeolite beds hold back nitrogen from compressed air. WHO's [plant specification](https://wkc.who.int/resources/publications/i/item/technical-specifications-for-pressure-swing-adsorption(psa)-oxygen-plants) targets 93% ±3. Runs at room temperature, so **it does not frost** |
| **Electrolyser** (hospital) | Power, water | Purer oxygen to pipes or canisters; hydrogen is vented | 2 H2O gives 2 H2 + O2. Typical systems use [about 4.5 to 6 kWh per cubic metre of hydrogen](https://arts.units.it/retrieve/e2913fde-cecd-f688-e053-3705fe0a67e0/energies-14-05347-v2_compressed2.pdf) |
| **Liquid oxygen store** (hospital) | Power, piped oxygen | Dense storage, and **the frost** | [Boils at about -183 C](https://eg.airliquide.com/sites/al_eg/files/2022-10/al008b-liq.-o2.pdf); one litre of liquid gives [about 862 litres of gas](https://ehs.princeton.edu/laboratory-research/chemical-safety/cryogenic-liquids). A [strong oxidiser](https://cal-tool.com/wp-content/uploads/2014/07/MSDS-OXYGEN-LIQUID-LINDE.pdf), so it should be a real fire hazard in play |

**The power cost, our own arithmetic:** each cubic metre of oxygen comes with two of hydrogen, so 9 to 12 kWh per cubic metre of oxygen. A patient on 5 litres a minute uses 0.3 cubic metres an hour, so about **3 to 3.6 kW of cell power per patient**. That hunger is honest, and it is the electrolyser's gameplay cost against the concentrator.

**The frost is a real effect:** water vapour deposits as rime on the store's cold pipes and vaporiser. Following [DMS2's Pressure Chamber](https://steamcommunity.com/sharedfiles/filedetails/?id=3819799478), it shows while the store is working, as our own mote or fleck and our own art.

**Equipment nothing here provides (agent ideas, for review):**

| Where | Item | Purpose |
| --- | --- | --- |
| Field | Oxygen mask and regulator | Built: the field oxygen kit, Medical Equipment 0.3.0 |
| Field | Burn dressings | Built: silver burn dressings, a medicine, Medical Equipment 0.3.0 |
| Field | Manual resuscitator | Built: Medical Equipment 0.3.0, pumped by a doctor for breathing under half |
| Hospital | Bedside oxygen | Built: Medical Equipment 0.1.0 (a concentrator, and a stand on canisters or the pipe network) |
| Hospital | Hyperbaric chamber | Built: Medical Equipment 0.3.0, a bed for one, 1 by 3 |
| Hospital | Burn unit bed | Built: Medical Equipment 0.2.0. Air-cell care: faster healing, less pain, better tending |

**Engineering notes:**

- **Vanilla Expanded Framework is a required dependency of this mod** (owner, 4 Nov 2026). Its PipeSystem module can carry our own
  piped oxygen when Vanilla Gravship Expanded is absent, the same module that network already uses and the radio framework's
  feedline relies on. Declared in `modDependencies`; the installed assembly is referenced with `Private=false`, never copied
  (CC BY-NC-ND 4.0, [third-party notices](../../../THIRD_PARTY_NOTICES.md)). D'thorga Framework and Burns and Smoke stay free of it,
  so a mod using only those needs nothing extra (agent assumption, for the owner to correct).
- **Its own treatment jobs:** More Injuries' suction and CPR target lists are hard-coded, so new devices cannot join them through XML.
- **Oxygen is a treatment you supply, not a gas in the room**, which keeps it clear of an air model.

### Open questions

| Question | For |
| --- | --- |
| ~~Is the electrolyser's water a Dubs Bad Hygiene pipe tie-in or a water item?~~ Settled 7 Nov 2026: with Dubs Bad Hygiene it draws from the plumbing; without it, water stays assumed ([medical equipment record](medical-equipment.md)) | Owner |
| ~~Should vented hydrogen be a room gas of ours, flammable in a closed room?~~ Settled 7 Nov 2026: yes, the owner's call; not built ([ventilation and hydrogen](../ventilation-and-hydrogen.md)) | Owner |
| ~~Is the frost one overlay texture or several frames?~~ Settled 12 Nov 2026: neither; the frost is drawn in code from the store's outline (Medical 0.15.0) | Agent, when the store's art is picked |
