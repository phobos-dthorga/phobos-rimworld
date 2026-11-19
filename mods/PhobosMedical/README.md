# D'thorga Medical

Burns and smoke that matter, doctors who dose and operate on their own, hospital and field equipment to treat it all, and a safe way
to add More Injuries to a colony you are already playing. Built to sit beside **More Injuries** and **Combat Extended**; neither is
required.

One of House D'thorga's mods (also spelt dthorga). **How to play it is in the player guide's
[medical chapter](../../docs/player-guide/medical.md)**; this README holds the gotchas, the settings and the links for modders.

Version 0.19.0, built 20 November 2026 for House D'thorga. **Partly tested in game:** the owner has tested radiation control on
D'thorga Medical (11 November 2026), and burns, dosing, the oxygen concentrator, the burn unit bed and the save bridge on the five
mods it replaced (3 to 6 November 2026). Everything else is not yet tested in game, nor is the merged mod as a whole.

## Read this first (gotchas)

- **Needs Harmony, D'thorga Framework 0.184.0 or later and Vanilla Expanded Framework, loaded before it.** The About file asks for
  that order, so RimSort and the game's sorter get it right. **RimWorld 1.7 only.**
- **Vanilla Gravship Expanded is strongly recommended** (it needs the Odyssey expansion). The bedside oxygen stand, the oxygen
  trolley, the field oxygen kit, the hyperbaric chamber, the electrolyser and the liquid oxygen store use its oxygen canisters and
  pipes, and exist only with it. Without it a letter says so once, and the settings window says which pieces are off.
- **Optional, used when present:** More Injuries, Combat Extended, Search and Rescue, VFE Medical, Dubs Bad Hygiene, Rimatomics
  (radiation control), Useful Marks (triage tags) and Hospitality (guests). None is needed.
- **Moving from the five old mods** (D'thorga Burns and Smoke, Emergency Dosing, Triage, Medical Equipment and Medical Save Bridge,
  merged into this one on 7 November 2026, [why](../../docs/development/mod-consolidation.md)):
  1. **Switch all five off in RimSort and this one on, in the same change**, then load your colony. The About file lists the five as
     incompatible, so RimSort warns if one is left on, and the load line logs an error naming any still active: running one beside
     this loads the same part twice.
  2. The game shows its usual warning that the mod list changed: load anyway. Every condition, building, item, job and saved record
     kept its name, so the colony carries over. Switching does not ask about converting the save: this mod adds no body parts.
  3. **Your settings carry over once**, from the five old settings files, the first time the game starts with this mod. There is one
     display mode for every part now.
- **Safe to add to a running colony.** A colony that had already done the research one of this mod's projects replaced got that
  project free, once.
- **Taking it out of a colony:** press **Remove every condition this mod adds, from every pawn** in its settings, deconstruct its
  machines and burn unit beds, take its items out of the colony (sell, destroy or use them up), save, then switch the mod off.
  Buildings and items left behind are dropped by the game with an error line. Operations triage queued stay as ordinary bills.
  The next load prints a harmless line for each missing map or game component, and one per research project; the save after that is
  clean.
- **Taking Vanilla Gravship Expanded out later** removes those six oxygen pieces the same way; patients on them lose the treatment
  within the hour, cleanly.
- **Adding More Injuries (or another mod that adds body parts) to a running colony asks first**: **Convert now**, **Not now** or
  **Never for this save**. **Back up the save first.** **If you choose Not now, don't save that colony**: whatever went wrong on that
  load (wounds on the wrong parts, lost limbs back, the dead standing up) is kept when you save. The steps are in
  [the guide](../../docs/player-guide/medical.md#adding-more-injuries-to-an-old-colony).
- **Decide before a colony:**
  - **People in pain fetch their own painkillers** (on by default), by their drug policies. **Check your drug policies first**:
    addiction and overdose are real.
  - **Tox gas is slow-acting** by default: the harm comes from about 4 hours later, over two or three days. A setting makes it act
    at once.
  - **How far the doctors may operate without orders**: to save life, by default, for colonists, slaves and prisoners; never for
    guests. Each person also has a button.
  - **How much to show** is Plain by default: no percentages or hours. Operator or Engineer show the figures.
- **What bites in play:**
  - **Carbon monoxide ignores gas masks** and has no smell. Move patients out of the burning room: a patient on oxygen still breathes
    the room's air.
  - **Fires in the open are hot**: heatstroke and burns from the air beside a blaze. With Atmosphere Overhaul or Celsius, this fire
    heat stands down by itself.
  - **Oxygen canisters, and kits made from them, explode when they burn** (Vanilla Gravship Expanded's rule), and a badly damaged
    liquid oxygen store bursts into a fireball.
  - **No surgery in the hyperbaric chamber**: move the patient to a hospital bed.
  - **With Search and Rescue, give your medics kits to carry**: its restocking never fetches ours, and its responders need Doctor
    work switched on to use them.
  - **Care on the ground gives way to the carry** (0.18.0, not yet tested in game): our doctors never hold a downed person against
    whoever comes to carry them to bed, and a dose against a disease waits for the bed. Needs D'thorga Framework 0.126.0.

## The parts

Each part has a section in the settings window and a section in the [player guide](../../docs/player-guide/medical.md).

| Part | What it does | In the guide |
| --- | --- | --- |
| Burns and smoke | Burns add up to a burned body fraction; burn shock past about a sixth of the body, which kills within days untreated; airway burns that swell shut; limbs that tighten under deep burns; smoke that scars the lungs; carbon monoxide from fires in closed rooms; fire heat in the open and in large rooms | [Burns and smoke](../../docs/player-guide/medical.md#burns-and-smoke), [conditions](../../docs/player-guide/medical.md#conditions-and-what-treats-them), [carbon monoxide](../../docs/player-guide/medical.md#carbon-monoxide) |
| Caustic gas and acid | Tox gas (Biotech) injures eyes, throat, lungs and skin, and can weaken immunity; acid keeps burning until a safety shower washes it off | [Tox gas and acid](../../docs/player-guide/medical.md#tox-gas-and-acid) |
| Emergency dosing | Doctors give painkillers, sedatives and disease-fighting drugs on their own, chosen by what each drug does; people in pain fetch their own by drug policy | [Doctors who dose](../../docs/player-guide/medical.md#doctors-who-dose) |
| Triage | A tag on every patient, the operation a killing condition needs queued by itself, and the worst case first | [Triage](../../docs/player-guide/medical.md#triage) |
| Equipment | Oxygen at the bedside, in the field and in a pressure chamber; ventilator, nebuliser, anaesthesia machine, steriliser, burn unit bed, safety shower, gas alarm, field kits; making and keeping oxygen | [Equipment](../../docs/player-guide/medical.md#equipment) |
| Radiation control | With Rimatomics: a line colonists suit up at, and potassium iodide before going in | [Radiation control](../../docs/player-guide/medical.md#radiation-control-with-rimatomics) |
| Save bridge | Converts a colony's wounds, lost limbs and bionics when More Injuries is added to it, so nothing lands on the wrong part | [Adding More Injuries](../../docs/player-guide/medical.md#adding-more-injuries-to-an-old-colony) |

## Settings

One entry, **D'thorga Medical**, in the game's mod settings, with a page per part down the left (0.19.0; it was one long scroll).
**Type in the search box to find a setting on any page**, tick **Changed only** to see just what you have changed (each marked with a
dot), and use **Reset this page to defaults** at the bottom of a page. Read from the source and the English text files, 20 November 2026.

| Setting, as the window shows it | What it does | Default |
| --- | --- | --- |
| ***General*** | | |
| **How much to show** (Plain, Operator, Engineer) | How many figures this mod's text shows. Plain shows no percentages and no hours | Plain |
| *Medical equipment* | No settings: a line says whether Vanilla Gravship Expanded is active and which pieces are off without it | |
| ***Burns and smoke*** | | |
| Burned body fraction | Adds every burn on the skin into one condition | On |
| Burn shock | Large burns can kill within days unless treated | On |
| Airway burns | Burns about the head and neck swell over hours | On |
| Deeply burned limbs tighten | A deeply burned arm or leg grows a crust that cuts off its blood until an escharotomy | On |
| Acid keeps burning until it is washed off | Acid on the skin deepens the burns until rinsed at a safety shower | On |
| Smoke inhalation | Breathing smoke damages the lungs for days | On |
| Carbon monoxide | Fires in closed rooms make carbon monoxide | On |
| Also apply this mod's smoke inhalation with Combat Extended | Shown only with Combat Extended. Off: Combat Extended's own condition is the one that applies | Off |
| Coughing | People with smoke, burned or stung airways cough now and then. Stays quiet by itself while another mod gives coughs, and names it | On |
| Fires heat the air around them in the open | Each fire in the open heats the cells around it | On |
| Fires heat the air around them in large rooms | The same in a room larger than 30 cells | On |
| People and animals move away from heat that would burn them | Anyone not drafted sprints to cooler air; firefighters stay | On |
| ***Caustic gas*** | | |
| Tox gas does lasting harm | Tox gas is caustic; the settings below appear while it is on | On |
| Tox gas acts fast | On: the harm comes at once, closer to the game's feel. Off: it comes hours later | Off |
| Tox gas: injures the eyes / can swell the throat / injures the lungs / burns the skin | Each kind of harm on its own | On |
| Tox gas: a heavy dose weakens immunity | Only when tox gas is slow-acting | On |
| Tox gas: badly injured lungs can stay irritable | Irritable airways after severe lung injury | On |
| Tox gas: a badly burned eye scars | A scarred cornea that dims the eye for good | On |
| Tox gas: plants wilt | Plants and crops in dense gas die if it lingers | On |
| Tox gas: strip and rinse | Gassed clothes keep burning until stripped at a shower | Off |
| ***Emergency dosing*** | | |
| Doctors give medication on their own | Painkillers, sedatives in extreme pain | On |
| Dose patients whose medical care is at least | Patients set below this care level are not dosed; no medicine is never dosed | Herbal medicine or worse |
| Use recreational drugs for pain | As a last resort, when nothing medical can be given | Off |
| Give medical drugs to patients who refuse drugs | Teetotalers and believers get medical drugs anyway | Off |
| Doctors give drugs that help fight disease | Only to a patient losing the race against a deadly disease | On |
| Patients in extreme pain are harder to tend | Tends come out up to a quarter worse | On |
| People in pain fetch their own painkillers | By their drug policy | On |
| Guests may take painkillers from your stock | Medical painkillers only | Off |
| ***Triage*** | | |
| Doctors queue operations on their own | Off: only the tags and the order of work remain | On |
| How far the doctors may go without orders, by default: Your colonists / Your slaves / Your prisoners | For anyone not set with the button on them | To save life |
| ... Guests | The same, for people of other factions staying with you | Never without orders |
| Highest chance of death on the table the doctors may accept alone | Riskier operations wait for your order | 5% |
| May use rare tools | Operations that use a costly instrument | On |
| May operate where it would wrong another faction | Off: such operations wait for your order | Off |
| Doctors take the worst case first | Off: the nearest patient, the game's order | On |
| Tell me when an operation is queued, or when no one can do one | Messages, naming what is missing | On |
| ***Colonies saved before More Injuries*** | Counts the saves you chose never to convert; **Ask about them again** forgets that choice | |
| ***Taking this mod out of a colony*** | **Remove every condition this mod adds, from every pawn**: a button, for use before removing the mod | |

The sounds follow D'thorga Framework's **D'thorga sounds volume** setting.

## For modders

Each part's Def extensions, rules Defs and figures are in its design record, under "For modders" where the README used to carry
them:

- [Burns and smoke](../../docs/development/burns-and-smoke.md#for-modders-moved-from-the-readme-17-nov-2026) and
  [caustic gas](../../docs/development/caustic-gas.md)
- [Emergency dosing](../../docs/development/emergency-dosing.md#for-modders-moved-from-the-readme-17-nov-2026)
- [Triage](../../docs/development/triage.md#for-modders-moved-from-the-readme-17-nov-2026)
- [Medical equipment](../../docs/development/medical-equipment.md#for-modders-moved-from-the-readme-17-nov-2026) and
  [the second set](../../docs/development/medical-equipment-2.md)
- [The save bridge](../../docs/development/medical-save-bridge.md#for-modders-moved-from-the-readme-17-nov-2026)
- [Radiation control](../../docs/development/radiation-control-line.md#for-modders-moved-from-the-readme-17-nov-2026)
- The health layer every part shares (who the doctors' patients are, how dangerous a condition is, what a pawn is breathing, the
  per-person buttons): [D'thorga Framework's record](../../docs/development/phobos-framework.md#health-layer-030-6-nov-2026)

## Checks done

What was checked for each version, and what the owner has tested, is in the [changelog](CHANGELOG.md). In short: every version builds
for RimWorld 1.7 and passes the repository's gate (the game-free rules' unit tests, the Defs, the text keys and the layout checks).
**Tested in game by the owner:** the save bridge's first load (3 November 2026), burn numbers and progression and the More Injuries
tie-ins (5 November), doctors' dosing (5 November), breathing on the oxygen concentrator (5 November), the burn unit bed (6 November),
all on the five former mods, and radiation control on D'thorga Medical (11 November). **Not yet tested in game:** everything else,
including triage, the ventilator and the second set of equipment, tox gas and acid, self-medication, the field responders' use of
the kits, and D'thorga Medical as one mod.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
