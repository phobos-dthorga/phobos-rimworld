# D'thorga Medical

Burns and smoke that matter, doctors who dose and operate on their own, hospital and field equipment to treat it all, and a safe
way to add More Injuries to a colony you are already playing. Built to sit beside **More Injuries** and **Combat Extended**;
neither is required.

One of House D'thorga's mods (also spelt dthorga). **How to play it is in the player guide's [medical
chapter](../../docs/player-guide/medical.md)**; this README holds the gotchas, the settings and the links.

Version 0.34.0, built 27 November 2026 for House D'thorga. **Partly tested in game:** the owner has tested radiation control on
D'thorga Medical (11 November 2026), burns, dosing, the oxygen concentrator, the burn unit bed and the save bridge on the five
mods it replaced (3 to 6 November 2026), and, on D'thorga Medical itself, doctors' work with many wounded, field care giving way to
the carry, and, with Rimatomics, radiation sickness running its real course and converting once. Everything else is not yet tested
in game, nor is the merged mod as a whole.

## Read this first (gotchas)

- **Partly tested in game.** Besides the items on the version line above, the owner has passed doctors' work with many wounded,
  field care giving way to the carry, and, with Rimatomics, radiation sickness running its real course and converting once, all on
  D'thorga Medical itself. The list, with dates and what passed only in part, ends the guide's [Medical
  chapter](../../docs/player-guide/medical.md#tested-in-game). **Everything else is not yet tested in game.**
- **Needs Harmony, D'thorga Framework 0.184.0 or later and Vanilla Expanded Framework, loaded before it.** Its About file asks for
  that order, so RimSort and the game's sorter get it right.
- **RimWorld 1.7 only.**
- **Vanilla Gravship Expanded is strongly recommended** (it needs the Odyssey expansion). The bedside oxygen stand, the oxygen
  trolley, the field oxygen kit, the hyperbaric chamber, the electrolyser and the liquid oxygen store use its oxygen canisters and
  pipes, and exist only with it. Without it a letter says so once, and the settings window says which pieces are off.
- **Optional, used when present:** More Injuries, Combat Extended, Search and Rescue, VFE Medical, Dubs Bad Hygiene, Rimatomics,
  Useful Marks and Hospitality. None is needed.
- **It replaces five older mods** (D'thorga Burns and Smoke, Emergency Dosing, Triage, Medical Equipment and Medical Save Bridge).
  **Switch all five off and this one on, in the same change**, then load your colony and accept the game's warning that the mod
  list changed. Your colony and your settings carry over; RimSort warns if one is left on, and running one beside this loads the
  same part twice.
- **Settings to decide before a colony:**
  - **People in pain fetch their own painkillers** (on), by their drug policies. **Check your drug policies first**: addiction and
    overdose are real.
  - **Tox gas is slow-acting** (the default): the harm comes from about 4 hours later, over two or three days. A setting makes it
    act at once.
  - **How far the doctors may operate without orders**: to save life, by default, for colonists, slaves and prisoners; never for
    guests. Each person also has a button.
  - **How much to show** is Plain: no percentages or hours. Operator or Engineer show the figures.
- **Safe to add to a running colony.** A colony that had already done the research one of this mod's projects replaced gets that
  project free, once.
- **Adding More Injuries (or another mod that adds body parts) to a running colony asks first**: **Convert now**, **Not now** or
  **Never for this save**. **Back up the save first**, and **if you choose Not now, don't save that colony**: whatever went wrong
  on that load (wounds on the wrong parts, lost limbs back, the dead standing up) is kept when you save.
- **With Rimatomics, our radiation model takes Rimatomics' radiation for people and converts anyone's existing radiation once.**
  Switching the model off, or removing this mod, does not bring Rimatomics' conditions back. The late effects that use the game's
  own carcinoma, cataract and sterilized stay when the mod is removed.
- **What bites in play:** **carbon monoxide ignores gas masks**, and a patient on oxygen still breathes the room's air, so move
  patients out of a burning room. **Fires in the open are hot**: heatstroke and burns beside a blaze (with Atmosphere Overhaul or
  Celsius this stands down by itself). **Oxygen canisters, and kits made from them, explode when they burn**, and a badly damaged
  liquid oxygen store bursts into a fireball. **No surgery in the hyperbaric chamber.** **With Search and Rescue, give your medics
  kits to carry**: its restocking never fetches ours, and its responders need Doctor work on to use them.
- **Taking it out of a colony:** press **Remove every condition this mod adds, from every pawn** in its settings, deconstruct its
  machines and burn unit beds, sell, destroy or use up its items, save, then switch the mod off. The next load prints a harmless
  line for each missing component and research project; the save after that is clean. Operations triage queued stay as ordinary
  bills. Taking Vanilla Gravship Expanded out later removes the six oxygen pieces the same way. Every mod's steps are in [Removing
  a mod](../../docs/player-guide/getting-started.md#removing-a-mod).

## What it adds

Each part has a page in the settings window and a section in the [player guide](../../docs/player-guide/medical.md).

- **Burns and smoke**: burns add up to a burned body fraction, with burn shock past about a sixth of the body, airway burns, limbs
  that tighten under deep burns, smoke-scarred lungs, carbon monoxide in closed rooms, and fire heat in the open and in large
  rooms.
- **Tox gas and acid**: tox gas injures eyes, throat, lungs and skin; acid keeps burning until a safety shower washes it off.
- **Emergency dosing**: doctors give painkillers, sedatives, disease-fighting drugs and cures on their own, chosen by what each
  item does; people in pain fetch their own by drug policy, and when a disease strikes the unprotected take a preventive. People
  taking in toxins take AntiTox, and someone caught in vacuum takes a vacsop they carry.
- **Long-term care, each off until you turn it on**: steadying someone close to a major break, easing an addiction being quit,
  healing old wounds, regrowing a missing part and refilling an artificial part's reservoir.
- **Protective-gear lines**: a toxic controlled area, and with Rimatomics a radiation one, where colonists take a preventive and
  put on gear at the edge.
- **Triage**: a tag on every patient, the operation a killing condition needs queued by itself, and the worst case first.
- **Equipment**: oxygen at the bedside, in the field and in a pressure chamber; ventilator, nebuliser, anaesthesia machine,
  steriliser, burn unit bed, safety shower, gas alarm, field kits; making and keeping oxygen.
- **Radiation**: radiation sickness in its real course, where care decides who lives; a doctor's estimate of a dose, or the exact
  dose after Rimatomics' detectors; late effects, each a setting; fallout dust; three medicines doctors give by themselves.
- **Radiation control**, with Rimatomics: a line colonists suit up at, and potassium iodide before going in.
- **The save bridge**: converts a colony's wounds, lost limbs and bionics when More Injuries is added, so nothing lands on the
  wrong part.

## Settings

One entry, **D'thorga Medical**, in **Options > Mod settings**, with a page per part down the left. **Type in the search box to
find a setting on any page**, tick **Changed only** to see just what you have changed (each marked with a dot), and use **Reset
this page to defaults** at the bottom of a page.

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
| Doctors care for bonded animals | Bonded animals and those with a master get cures, disease drugs, toxin protection and relief from extreme pain; never anything addictive | On |
| Who gives something that keeps toxins out, while toxins are building up | Nobody, doctors, or doctors and people taking their own: AntiTox while in polluted ground, tox gas or fallout | Doctors, and people take their own |
| Someone caught in vacuum takes a vacuum medicine they carry | With Odyssey: a vacsop they carry, taken at once in a hull breach | On |
| Who gives something that steadies someone close to a major break | Long-term care: clozapine when mood falls below the major break threshold | Nobody |
| Who gives something that eases an addiction being quit | Long-term care: for an addiction the drug policy no longer feeds; never luciferium | Nobody |
| Who gives something that heals old wounds | Long-term care: Hydra for a scar or chronic condition that hurts or weakens | Nobody |
| Who gives something that regrows a missing part | Long-term care: a regeneration serum for a missing natural part | Nobody |
| Who refills the reservoir an artificial part draws on | Long-term care: Neuroglue for The Dead Man's Switch's biogel | Nobody |
| Who gives a stimulant to someone who can barely breathe | Nobody, or doctors: breathing at 30% or less, as a bridge to oxygen or an operation | Doctors only |
| Who gives a cure that is in stock | Nobody, or doctors: a patient in bed with a condition something removes outright is given it | Doctors only |
| Who gives a preventive when a disease strikes | Nobody, doctors, or doctors and people taking their own | Doctors, and people take their own |
| Preventives always kept in stock | Prevention never takes one below this many of a kind: 0, 5, 10 or 20 | 5 |
| Alert when nothing in stock answers a need | Names anyone who needs something your colony can make and has none of | On |
| A medicine is rare, and kept for saving a life, from | A slider, 0 to 2,000 silver; at zero no medicine is kept back | 300 silver |
| People in pain fetch their own painkillers | By their drug policy | On |
| Slaves may take painkillers from your stock | By their own drug policy; never while rebelling or escaping | Off |
| Guests may take painkillers from your stock | Medical painkillers only | Off |
| ***Triage*** | | |
| Doctors queue operations on their own | Off: only the tags and the order of work remain | On |
| How far the doctors may go without orders, by default: Your colonists / Your slaves / Your prisoners | For anyone not set with the button on them | To save life |
| ... Guests | The same, for people of other factions staying with you | Never without orders |
| Highest chance of death on the table the doctors may accept alone | Riskier operations wait for your order | 5% |
| May use rare tools | Operations that use a costly instrument | On |
| May give a healer serum to the dying | For someone dying whom no operation here can save, a healer mech serum from stock, only if it would mend that very condition; a letter says when | Off |
| May operate where it would wrong another faction | Off: such operations wait for your order | Off |
| Doctors take the worst case first | Off: the nearest patient, the game's order | On |
| Tell me when an operation is queued, or when no one can do one | Messages, naming what is missing | On |
| ***Radiation*** | | |
| Radiation sickness runs its real course | Off: Dubs Rimatomics' own radiation sickness, as before | On |
| How harsh radiation is | Gentle, Moderate, Realistic, Harsh: the dose that kills half without care, and how far care moves it | Moderate |
| A doctor estimates a dose until it can be measured | Off: every dose shows exactly | On |
| What is worn shields a little | Heavy clothing and armour cut radiation from outside by their weight: plate armour about a sixth | On |
| Fallout dust on skin and clothes | Dust from fallout keeps dosing until washed off; breathed unmasked, a dose from inside and iodine to the thyroid | On |
| Dusty colonists strip and wash | A dusty colonist leaves dusty clothes at a safety shower and washes | Off |
| Cancer | Each gray taken over a life adds a chance of leukaemia (from about a month) or a carcinoma (from about two and a half game years) | On |
| Cataracts | From half a gray, a chance of cataracts in both eyes later, certain at 5 Gy | On |
| Sterility | A man sterile for a while from 0.15 Gy; for good from 3.5 to 6 Gy (a woman 2.5 to 6) | On |
| Loss of a pregnancy | Needs Biotech: a dose early in a pregnancy, or a large one later, may end it | On |
| Harm to a child before birth | Needs Biotech: weaker movement, or slower learning and thinking, by when in the pregnancy | On |
| A child's own cancer risk | Needs Biotech: a child is born carrying the dose it took before birth | On |
| ***Colonies saved before More Injuries*** | Counts the saves you chose never to convert; **Ask about them again** forgets that choice | |
| ***Taking this mod out of a colony*** | **Remove every condition this mod adds, from every pawn**: a button, for use before removing the mod | |

The sounds follow D'thorga Framework's **D'thorga sounds volume** setting.

## More

- [The player guide's Medical chapter](../../docs/player-guide/medical.md): what to build, what each condition needs, and what is
  tested in game.
- [The changelog](CHANGELOG.md): what changed in each version, with the five former mods' histories.
- [For modders](../../docs/modding/medical.md): each part's Def extensions, rules Defs and figures, and the checks done.

Community-created content. Portions of the materials used to create this content/mod are trademarks and/or copyrighted works of
Ludeon Studios Inc. All rights reserved by Ludeon. This content/mod is not official and is not endorsed by Ludeon.
