# Medical equipment, second set: design (7 Nov 2026)

A design record. **Built so far** (D'thorga Medical 0.7.0, not yet tested in game): the ventilator and its intubation kit, the safety
shower and eyewash; see Built below. The owner chose all eight proposals from the
[art backlog](art-backlog.md#proposed-medical-equipment-agent-ideas-6-nov-2026-for-the-owner) for D'thorga Medical Equipment (7 Nov
2026), in this order. The first set (oxygen, burn care, the field kit and the hand pump) is in the [medical equipment record](medical-equipment.md).

## Read this first

- **Order (owner):** ventilator; safety shower and eyewash; carbon monoxide and smoke alarm; a cylinder trolley; then the nebuliser,
  the anaesthesia machine, a needle decompression kit and the steriliser.
- **Rules that still apply:** effects matched from the body or a Def extension, never by name; the oxygen network is Vanilla Gravship
  Expanded's, optional; every new button has its own icon; every refusal says why; in-game text names no mod.
- **Each piece ships with a scripted placeholder** and an art request, so the logic never waits for a GPU night.
- **Figures from memory are marked unverified** until a source is recorded. Nothing below is medical advice: these are game tiers.

## The pieces

| Piece | Kind | What it would do in play (agent proposal) | The real thing, as a basis |
| --- | --- | --- | --- |
| **Ventilator** | Bedside machine, 1x1, power | Breathes for a patient whose breathing has failed, holding it at a floor, **but only through an open route**: a tracheostomy (Burns and Smoke's) or a doctor's intubation, a short job with a skill check. A shut upper airway is bypassed by the tube, which is the honest difference from the hand pump | Mechanical ventilation through an endotracheal or tracheostomy tube |
| **Safety shower and eyewash** | Building, 1x1, against a wall | A pawn with a chemical or acid burn, or caustic gas on the skin, is rinsed (a job, or by themselves when they can walk): the burn stops deepening and its pain eases. Uses water from Dubs Bad Hygiene when active, assumed otherwise | Long flushing with water; the usual figure is 15 minutes, from memory, unverified |
| **Carbon monoxide and smoke alarm** | Wall-mounted, 1x1, power | Warns when a room's carbon monoxide or smoke passes a threshold: a message naming the room and what to do, with Burns and Smoke's gases read through the framework | A household alarm |
| **Cylinder trolley** | Building, 1x1, minifiable, Vanilla Gravship Expanded only | A wheeled holder for oxygen canisters that serves the bed it stands beside like the bedside stand, and can be moved where it is needed | Medical cylinders on trolleys. Colour code to check: Australian medical oxygen, from memory black with a white shoulder, unverified |
| **Nebuliser** | Bedside machine, 1x1, power | Eases airway spasm after smoke with a drug from Emergency Dosing, given by machine | A compressor that turns a liquid drug into a mist |
| **Anaesthesia machine** | Facility beside a surgery bed, power | Safer surgery: a bonus to success through the game's own surgery factors, tied to Emergency Dosing's anaesthetics | Vaporisers, flow meters and a breathing circuit |
| **Needle decompression kit** | Item, used up | A field hold for a collapsed lung under pressure: buys time until D'thorga Triage's operation, through More Injuries' conditions when active | Needle thoracostomy |
| **Steriliser** | Building, 1x1 or 2x1, power | Lowers surgical infection in its room, through the game's own room stats. The weakest of the eight: it overlaps with cleanliness | An autoclave |

## Built

**Ventilator and intubation kit** (7 Nov 2026). Decisions, labelled:

- **Intubation is a quick doctor job with a kit** (owner: a doctor job). It runs through the device system the field kit and the hand
  pump use, as a consumed item with a new need, `Intubation`, and a skill floor on the device extension, `minMedicineSkill` 4 (agent
  choice), checked against the doctor; a doctor without it is told why. Doctors reach for a tube first when one is needed.
- **When a tube is needed** (agent choice): no tube yet, and either an upper airway is closing (a condition marked
  `PhobosBypassedByTubeExtension`, the airway burn, with breathing under the hand pump's threshold) or the patient lies at a working
  ventilator that needs one. It works at breathing zero: getting past a shut airway is its point.
- **What the tube does:** the `PhobosMedical_Intubated` condition, marked `PhobosOpenAirwayExtension` like the tracheostomy, stops
  talking and lasts two days (authored, past the airway burn's 48 hours); fitting it removes the bypassed airway condition, as the
  tracheotomy does.
- **The ventilator** (bedside facility, 200 W, one patient; authored) breathes through any open-airway condition. Its condition picks
  one of six stages, each adding a tenth of breathing, the fewest that reach the rules Def's floor of 0.6 (authored), from breathing
  measured with every breathing treatment left out (the oxygen condition's measuring switch is now shared). Nothing at zero: with a
  tube in, zero means no lungs left. It says who it treats, who needs a tube, and who breathes well enough.
- Placeholder art for both, drawn in code (`assets/phobos-medical/draw_placeholders.py`).

**Safety shower and eyewash** (7 Nov 2026). Decisions, labelled:

- **What it treats.** The game's acid burn is a one-off injury, so nothing would deepen and a rinse would have nothing to stop. Our
  inference from first aid (flush chemical burns with water for a long time): an acid burn now leaves **acid on the skin** for about
  three hours (authored), deepening the acid burns by 2 damage an hour at first (authored), spread over them through the burn's own
  damage kind (found from the Defs by its injury, never by name), ignoring armour since the acid is under it. Marked by
  `PhobosRinsedByWaterExtension` on `AcidBurn`; the caustic gas model can mark its own. A burns setting.
- **Water** (sourced, approximate): ANSI/ISEA Z358.1 asks a drench shower for 75.7 litres a minute and an eyewash for 1.5 litres a
  minute, each for 15 minutes, as reported by ISHN ([ISHN, "ANSI/ISEA Z358.1-2014 emergency eyewash and shower equipment"](https://www.ishn.com/articles/105549-ansiisea-z3581--2014-emergency-eyewash-and-shower-equipment), secondary source). So one rinse is 625 ticks and about 1,136 litres, or 22.7 when every acid burn is on an eye. With
  Dubs Bad Hygiene the Medical tie-in draws it from the plumbing through the Framework's reader (a failed draw marks the shower dry for
  an hour, with the electrolyser's reasons); without it, water is assumed. A core interface, `IPhobosWaterSource`, keeps the core free
  of the tie-in.
- **Who rinses** (agent choice): anyone who can walk, by a job giver inserted into the game's constant think tree for people after
  moving away from heat, to the nearest usable shower within 60 cells; patients lying down get Doctor work (priority 92): the doctor
  draws water at the shower, carries it over and rinses them. The shower is stood on. No research.
- **After a rinse**: cool water eases burn pain (pain factor 0.85) for about four hours (authored).

## Open questions

| Question | For |
| --- | --- |
| The ventilator's intubation: a job any doctor can do, or a surgery bill? **Answered: a doctor job (owner, 7 Nov 2026); built with a kit and Medicine 4** | Owner |
| Does the alarm also wake sleeping pawns in the room? | Owner |
| The anaesthesia machine and the nebuliser need Emergency Dosing to give a drug by machine. Built in Dosing (shared) or here? | Agent, when it starts |
| Which More Injuries condition is the collapsed lung, and what it already allows | Agent, from its Defs |
