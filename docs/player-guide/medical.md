# Medical

**D'thorga Medical** makes burns, smoke and gas matter, lets doctors dose and operate on their own, sorts patients by how
urgently they need help, and adds the hospital and field equipment to treat it all. It sits beside **More Injuries** and **Search
and Rescue**, which handle care where a colonist falls; it handles what happens in bed. [Back to the guide](README.md).

![A doctor checking a patient on a field bed inside a lit tent at night](../../mods/PhobosMedical/About/Preview.png)

## Read this first (gotchas)

- **Needs Harmony, D'thorga Framework and Vanilla Expanded Framework.** **Vanilla Gravship Expanded is strongly recommended**
  (it needs the Odyssey expansion): most of the oxygen kit uses its canisters and pipes, and a letter says once what is missing
  without it. More Injuries and Combat Extended are optional and used when present.
- **Partly tested in game**: see [the last section](#what-has-been-tested-in-game).
- **People in pain fetch their own painkillers by default. Check your drug policies first**: addiction and overdose are real.
- **Tox gas does lasting harm, and by default the harm comes about 4 hours later**, over two or three days. Keep gassed colonists
  in bed and watch them.
- **Carbon monoxide ignores gas masks** and has no smell. Only sealed headgear with its own air keeps it out. It lingers in a shut
  room for hours after a fire: **open a door or a vent**. Hang a smoke and carbon monoxide alarm in bedrooms and hospitals.
- **Fires in the open are hot**: standing beside a blaze can cause heatstroke and burns from the hot air. People not drafted move
  away from heat that would burn them; firefighters stay at the fire.
- **Doctors need Doctor work switched on**, and the skill each operation or kit asks for. When nobody can operate, a message names
  what is missing.
- **Switching from the five old mods** (Burns and Smoke, Emergency Dosing, Triage, Medical Equipment, Medical Save Bridge): all five
  off and this one on, in the same change. Your colony and settings carry over.
- **Taking it out**: press **Remove every condition this mod adds, from every pawn** in its settings, deconstruct its machines and
  burn unit beds, take its items out of the colony, save, then switch it off.

## From injury to recovery

```mermaid
flowchart TD
    Hurt["A colonist is hurt: wounds, burns, smoke, gas"] --> Field["Where they fell (More Injuries, Search and Rescue):<br/>bleeding stopped, CPR, airway, fluids, the carry to bed"]
    Field --> Bed["In a hospital bed"]
    Bed --> Dose["Doctors dose: painkiller from 40% pain,<br/>sedative at 80%, a disease-fighting drug when immunity is losing"]
    Bed --> Oxygen["Equipment: oxygen when breathing is under 80%,<br/>air-cell care on a burn unit bed, a ventilator through a tube"]
    Bed --> Triage["Triage: a tag on each patient; the operation<br/>a killing condition needs is queued; the worst case first"]
    Triage --> Surgery["A doctor operates"]
    Dose --> Recover["Rest, tending and time"]
    Oxygen --> Recover
    Surgery --> Recover
```

**A bed is where most of it happens.** Painkillers, oxygen, burn care and every operation need the patient down or in bed. **Stock
decides everything**: medicine, painkillers, oxygen and kits have to be made or bought, and a doctor carrying them is fastest.
**Field care and surgery are separate jobs**: Search and Rescue and More Injuries (other authors) treat the patient where they
fell and carry them to bed; D'thorga Medical takes over once they are there. **Our doctors never hold someone on the ground against
a carrier:** a painkiller, a hand pump or a kit given where they lie stops when someone comes to carry them, and goes on at the bed. A
dose against a disease waits for the bed. Not yet tested in game.

## Burns and smoke

**What to do for a badly burned colonist:** tend the burns **and the burn shock** with the best medicine you have, keep them in bed,
and get them out of the smoke.

- **Burned body fraction**: every burn adds up. Past about a sixth of the body (15%) comes **burn shock**, which kills within days
  untreated. Older patients, and those with an airway burn or carbon monoxide, fare worse.
- **Tend the shock itself.** It is a tendable condition; tending stands in for giving fluids, and the medicine's quality sets how
  much it helps. Fluids wear off after 12 hours, so **keep tending**.

| Burns tended and in bed, the shock tended with | A bad case settles at | The worst cases settle at |
| --- | --- | --- |
| No medicine | Shock | Shock, close to severe |
| Herbal medicine | Shock | Shock |
| Industrial medicine | Compensated | Shock |
| Glitterworld medicine | Compensated | Compensated |

*For a doctor of average tend quality. A poorer doctor gets less from the same medicine.*

- **Airway burns swell shut over hours** (worst at 8 hours, gone by 48). A doctor with Medicine 4 places a breathing tube with an
  **intubation kit** by themselves, or you order a **tracheotomy** (with More Injuries it needs its Emergency Medicine research).
  Oxygen does not open a closed airway.
- **Deeply burned limbs tighten.** An arm or leg burned to 60% of its health grows a **constricting eschar** that cuts off its blood
  over about half a day. **An escharotomy** (Medicine 4, any medicine) cuts it open; with triage the doctors queue it themselves.
  **Left alone, the limb is lost**: with More Injuries it can turn gangrenous; without it, it takes damage until it is lost, in about
  three days for a leg.
- **A limb burned off counts as all of its skin burned** while the stump is fresh, so losing an arm and a leg to fire goes straight
  to burn shock. A fresh stump also bleeds until it is tended. Old burn scars never count.
- **Smoke scars the lungs**: tending, rest and oxygen help them heal. Severe damage can turn to pneumonia.
- **A fluids kit** (made at a drug lab) is hung by doctors themselves on a patient in burn shock. With More Injuries you can also
  order a **saline bag** as a bill, the very best care.
- **With VFE Medical**, a patient in burn shock in a hospital bed with an **intravenous drip stand** beside it is kept on fluids
  without a kit. A hung fluids kit is still better care.
- **With More Injuries**, its own **fire inhalation** setting decides whether fire burns the lungs directly; this mod never damages
  them twice. Carbon monoxide and a closing airway feed More Injuries' hypoxia and brain damage.
- **With Combat Extended's smoke running**, its smoke inhalation is the one you see; this mod adds the lasting lung damage and the
  carbon monoxide on top.
- **People only**: animals burn as they always did.

**Hold Alt over a cell** to see its temperature near a fire. A full-size fire in the open adds about 600 degrees at the flame, 150
one cell away and nothing past five. In a large room, a fire also heats the cells around it; a room of 30 cells or fewer is heated
through by the game as before. **With Atmosphere Overhaul or Celsius active, this fire heat switches itself off**: those mods model
the air cell by cell.

### Conditions and what treats them

| Condition | Comes from | Treated by |
| --- | --- | --- |
| Burns (burned body fraction) | Every burn on the skin, and every part burned off while fresh | Tending the burns and stumps |
| Burn shock | 15% or more of the body burned | Tending the burns, bed rest, tending the shock with good medicine, fluids |
| Airway burn | Burning, or burns to the head and neck | A breathing tube or a tracheotomy; or waiting, if it never closes |
| Constricting eschar | An arm or leg burned to 60% of its health | An escharotomy; or the burns on that limb healing |
| Smoke-damaged lungs | Breathing smoke | Tending, rest and supplied oxygen |
| Pneumonia | Severe lung damage | Tending and immunity, like a wound infection |
| Carbon monoxide poisoning | A fire in a closed room | Clean air; oxygen clears it about four times faster |
| Acute smoke inhalation | Breathing smoke, when Combat Extended's smoke is not running | Clean air; it clears in a day or so |
| Caustic gas dose | Standing in tox gas | Time; a wash at a safety shower clears the skin's share |
| Caustic eye injury | Tox gas in the eyes | Tending speeds healing |
| Scarred cornea | A badly burned eye, once it heals | A replacement eye or a healer mech serum |
| Weakened immunity | A heavy tox gas dose | Time: about two weeks |
| Irritable airways | Severe lung injury from gas | Time: about two months; after a critical injury it can stay |

### Carbon monoxide

Fires in closed rooms make carbon monoxide: headache, then dizziness, confusion, collapse and death. **Clean air** clears it: it
halves about every 5 hours indoors, 3 in the open, and **about every 75 minutes on oxygen**. **Move patients out of the burning
room**: a patient on oxygen still breathes the room's air.

**Which smoke colonists breathe:** without Combat Extended, a burning room fills with the game's own smoke. With it, Combat
Extended's **smoke effects** setting decides: on, its smoke is what colonists breathe; off, the game's smoke takes over. Gas stays
where it is when a gravship takes off: it does not travel with the ship.

### Tox gas and acid

- **Tox gas (needs Biotech) is caustic.** Besides the game's own effects it can injure the eyes, swell the throat, injure the lungs,
  burn the skin, and after a heavy dose weaken immunity for about two weeks. **A gas mask protects the eyes and lungs, not the
  skin**; only a sealed suit protects everything. A badly burned eye can scar for good. Plants in dense gas wilt.
- **By default it is slow-acting**: it stings, seems to pass, and the harm comes on from about 4 hours later. A setting makes it
  act at once, closer to the game's own feel. Each kind of harm has its own setting.
- **Acid keeps burning until it is washed off.** Anyone who can walk runs to the nearest **safety shower** (within 60 cells) and
  rinses for a quarter of an hour; **doctors draw water and rinse patients who can't walk**. Washing also takes tox gas off the skin,
  and the cool water eases the pain for a few hours.
- **With Dubs Bad Hygiene, lay plumbing to the safety shower**: a rinse takes about 1,100 litres (23 for the eyes alone), and a dry
  shower says so. Its showers and baths work as safety showers too.
- **Strip and rinse** (a setting, off): gassed clothes keep burning until the colonist strips them at a shower. They are left
  forbidden and clear in about four days, or at once in a washing machine.

## Doctors who dose

Doctors give painkillers and sedatives on their own, from whatever mod made them. Each drug is judged by what it does, never by
its name.

| When | Given |
| --- | --- |
| Down or in bed, pain 40% or more | The best painkiller in reach, net of its side effects |
| Pain 80% or more, and no painkiller can be given | A sedative |
| A disease that can kill, and immunity would come too late | A medical drug that speeds immunity. **A patient already winning gets none**, so stock lasts |

- **Never given**: a second painkiller while one still works, anything during an overdose, a dose that would leave consciousness or
  breathing under 15%, recreational drugs (unless the setting allows them, and then last), drugs to someone who refuses them on
  principle.
- **A drug counts as a painkiller only if it takes away 30% of the pain or more**, so epinephrine stays for cardiac arrest, and
  beer and tea stay out.
- **A patient bleeding hard (30% a day or more) waits for a painkiller** until the bleeding is tended.
- **A patient in extreme pain flinches while being tended**, so the tend comes out up to a quarter worse. Pain relief prevents it.
- **Colonists up and about fetch their own painkiller** at 40% pain, by their drug policy. Teetotalers, children and masochists never
  do; anyone with chemical interest or fascination reaches for it sooner. Nobody takes a dose that would knock them out.
- **Guests have no drug policy.** With the guests setting on, they take medical painkillers from your stock, never recreational ones.
- **Mechanoid doctors give no drugs.** Search and Rescue's and More Injuries' doctors keep their own patients; dosing waits for a
  patient nobody has claimed.
- **With a doctor selected, right-click a patient** to see why no dose can be given.

## Triage

![Triage tags: red immediate, yellow urgent, green delayed, white minor, black expectant, and the button for how far doctors may go](images/triage-tags.png)

**Each patient's tag shows by their name on the map** (with Useful Marks, in its row of marks), coloured as in field triage, and
**a doctor goes to the worst case first**.

| Tag | Means |
| --- | --- |
| **Immediate** (red) | Dying within 6 hours, or a vital function at the floor |
| **Urgent** (yellow) | Dying, with more time |
| **Delayed** (green) | Crippled, not dying |
| **Minor** (white) | Nothing serious |
| **Expectant** (black) | Dying of something only an operation fixes, and nobody here can do it |

- **Doctors queue the operation a killing condition needs**, choosing the one most likely to succeed among those your colony can do
  now, with a death risk at the table no higher than the setting (5%). The bill goes to the top of the game's own operations list;
  **bills you added are never withdrawn**.
- **The button on each person**, and beside the medicine setting on the health tab, says **how far the doctors may go**: never
  without orders; to save life (the default for colonists, slaves and prisoners); to save life or a limb (fracture repairs come
  here); any repair. Guests start at never.
- **More Injuries' therapy for brain damage uses glitterworld medicine**, so someone set to any repair may have it queued. Set them
  lower if you would rather order it yourself.
- **When nobody can do an operation a patient needs, a message says what is missing**: skill, a tool, research or risk.
- **Oxygen, rest and time are not operations**: carbon monoxide or burn shock is tagged, but nothing is queued.
- **No surgery in a hyperbaric chamber**: move the patient to a hospital bed.
- Drug addictions are left alone: their treatment is the drug, your policy and time.

## Equipment

![Medical machines: oxygen concentrator, bedside oxygen stand, oxygen cylinder trolley, ventilator, nebuliser, anaesthesia machine, steriliser, safety shower, smoke and carbon monoxide alarm, burn unit bed, hyperbaric chamber, oxygen electrolyser and liquid oxygen store](images/medical-machines.png)

**Bedside machines go touching a hospital bed**, as a vitals monitor does; one can serve two beds it touches, and any bed that takes
a vitals monitor works. **Nobody has to be told**: a patient who needs oxygen gets it, and doctors use the kits as Doctor work.
Doctors carry a downed patient who needs oxygen to a free hyperbaric chamber first. **The research is on the D'thorga tab**; a
colony that had already done the research a project replaced was given that project free, once.

**What to build first:** an **oxygen concentrator** (only power) by your hospital beds, a **smoke and carbon monoxide alarm** in each
bedroom and hospital, a **burn unit bed** if fire is a threat, and a **safety shower** if you fight with acid or gas.

| Piece | Needs | Does | Research |
| --- | --- | --- | --- |
| Oxygen concentrator | 350 W | Oxygen for up to 2 patients in beds it touches | Respiratory care |
| Bedside oxygen stand | 1 oxygen canister per patient-hour, or an oxygen pipe | The same, without power | Piped oxygen |
| Oxygen cylinder trolley | Canisters (holds 40), or a pipe | **No bed needed**: oxygen for 1 patient lying beside it, even on the floor. Quick to move | Piped oxygen |
| Ventilator | 200 W | Breathes for a patient whose breathing failed, **through a breathing tube** only, up to a safe level, not normal. It can't help someone with no lungs left | Respiratory care |
| Nebuliser | 60 W, bronchodilator vials | Eases airways that tightened after smoke | Respiratory care |
| Anaesthesia machine | 150 W | **Operations in that bed 8% likelier to succeed** | Anaesthesia |
| Steriliser | Nothing | The room counts as cleaner: less infection, better surgery | Clinical equipment |
| Safety shower and eyewash | Water (plumbing with Dubs Bad Hygiene) | Washes off acid and gas; cool water eases burn pain | None |
| Smoke and carbon monoxide alarm | 5 W, hangs from the ceiling | **Warns you which room, and wakes anyone asleep there**. Switch it off to silence it | Electricity |
| Burn unit bed | 200 W | Better tending; **wounds heal 25% faster and pain falls 15%** for a burned patient | Burn care |
| Hyperbaric chamber | 800 W and oxygen | Stronger oxygen for one patient; carbon monoxide clears in about 22 minutes. No surgery inside | Hospital bed, Oxygen Network |
| Oxygen electrolyser | 3,000 W (and water with Dubs Bad Hygiene) | Makes 1 canister of oxygen an hour into an oxygen pipe. For vacuum and thin air, where Vanilla Gravship Expanded's harvester can't run | Oxygen Network |
| Liquid oxygen store | 300 W for its cooler | Holds 2,000 units; about 2% boils off a day without power. **A badly damaged store bursts into a fireball**. Frost gathers on it while it works | Oxygen Network |

**Oxygen**: a patient gets it when a lung or smoke condition needs it or their breathing is under 80%, the worst breathing first.
Breathing rises 20 points (30 in the chamber or with the hand pump), never above normal. With More Injuries, oxygen also helps
hypoxia heal, the chamber twice as fast again; damage that has turned permanent never heals.

![Medical kits: field oxygen kit, manual resuscitator, intubation kit, needle decompression kit, fluids kit, silver burn dressing and bronchodilator vial](images/medical-kits.png)

| Kit | Used by | For | Made at |
| --- | --- | --- | --- |
| Field oxygen kit | Doctors, by themselves | Oxygen for one patient lying down, for about an hour | Machining table, from an oxygen canister (piped oxygen) |
| Manual resuscitator | Doctors, by themselves | Pumps for breathing under 50%, as long as needed. Kept | Machining table (clinical equipment) |
| Intubation kit | A doctor with Medicine 4 | A breathing tube, for a swelling airway or a ventilator. Out after two days; the patient can't talk meanwhile | Machining table (clinical equipment) |
| Needle decompression kit | A doctor with Medicine 5 (More Injuries only) | Buys about a day for a collapsed lung. **The lung still needs its operation** | Machining table (More Injuries' emergency medicine) |
| Fluids kit | Doctors, by themselves | Burn shock | Drug lab (clinical equipment) |
| Silver burn dressing | Any tend | A medicine, used under normal medicine or better | Drug lab (clinical equipment) |
| Bronchodilator vial | The nebuliser | 4 from 1 neutroamine | Drug lab (bronchodilators) |

**Right-click a patient with a doctor selected** to give a device yourself, or to see why not: the airway is shut, nothing is in
reach, or they are already on oxygen. **Oxygen canisters, and kits made from them, explode when they burn** (Vanilla Gravship
Expanded's rule).

**Every machine and device has its own quiet sound**, such as the concentrator's hum and the alarm's soft tone. D'thorga Framework's
**D'thorga sounds volume** setting turns them down or off.

**With Search and Rescue, its field responders use these kits where a colonist fell**, as Field rescue work, if they also have
Doctor work switched on. The airway kits, the hand pump and the decompression kit come just before its own treatment; oxygen and
fluids after it. **Give your medics kits to carry:** Search and Rescue's restocking does not fetch them. To have casualties carried
to a parked helicopter instead of a far bed, switch on its **Take casualties aboard** (D'thorga Framework, in the
[vehicle care chapter](vehicle-care.md)). Not yet tested in game.

## Radiation control (with Rimatomics)

![The radiation control point and the tools to paint and clear a controlled area](images/radiation-control.png)

Suits on inside, off at the door, as a real plant runs it. **Only with Rimatomics active.**

1. Place a **radiation control point** (D'thorga tab, Medical, 10 steel) facing into a room or its doorway, or paint a **radiation controlled
   area** (zone tab) over open ground or several rooms.
2. **Keep suits and masks just outside the line**, on a shelf, not forbidden. Gear inside the area is never fetched.
3. **Keep potassium iodide just outside too.** A colonist going in who is not on iodide takes a tablet first ("Taking potassium
   iodide first"); it takes radiation sickness down for about five days. For fallout over the whole map, schedule iodide in a drug
   policy instead.
4. A colonist without **75% toxic environment resistance** stops at the line, puts on the fewest, nearest items that get them there
   ("Suiting up"), and takes them off on the way out. Rimatomics' suit and mask give 40% each, so it takes both. Gear they were
   already wearing stays on.

Drafted colonists, prisoners, guests and animals cross as they are. With no gear in reach, a colonist won't go in: a message says
so once, and they keep clear of the area for an hour and take up other work.

## Adding More Injuries to an old colony

More Injuries adds body parts, which scrambles every old wound, lost limb and bionic in a save written without it. D'thorga Medical
puts them back on the right parts. It does the same for any other mod that adds body parts.

1. **Back up the save** and load the copy.
2. **Switch on More Injuries and D'thorga Medical in the same step.**
3. Load. A dialog asks: **Convert now** (do this), **Not now**, or **Never for this save**. **If you choose Not now, don't save that
   colony**: whatever went wrong on that load is kept. A save you marked never can be asked about again from the mod settings.
4. Check a colonist with a lost limb or a bionic, then **save**. From then on it does nothing.

The log's lines start with [D'thorga Medical: save bridge]; after the load, the line about hediffs sitting on parts newer than the
save ends "none." when all went well.

## What has been tested in game

- **Tested by the owner:** burn numbers, how burn conditions progress, and the More Injuries tie-ins (5 November 2026); doctors
  dosing penicillin and ketamine (5 November 2026); a patient's breathing improving on the oxygen concentrator (5 November 2026); the
  burn unit bed and its air-cell care (6 November 2026); the save bridge's first load on a copy of a 330-mod colony (3 November
  2026); radiation control with Rimatomics (11 November 2026, "working VERY well").
- **Not yet tested in game:** everything else, including triage, the ventilator, tox gas, acid, the safety shower, the alarm, the
  oxygen network pieces, self-medication and the field responders' use of the kits.
- **The tests before 7 November were on the five separate mods**, before they became one.

The settings, and links for modders, are in [D'thorga Medical's README](../../mods/PhobosMedical/README.md); what changed in each
version is in its [changelog](../../mods/PhobosMedical/CHANGELOG.md).
