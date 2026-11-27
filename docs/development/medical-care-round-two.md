# Medical care, round two: the gaps left after the care needs (27 Nov 2026)

**Status (27 Nov 2026):** slice 1, sharper dosing, is built in D'thorga Medical 0.33.0 ([as built](#as-built-medical-0330)), and
slice 2, who is covered, in 0.34.0 with Framework 0.351.0 ([as built](#as-built-medical-0340)), not yet tested in game (rows 691 to
695). Slices 3 and 5 are next, on the owner's "1, 2, 3, and 5, step-wise if necessary"; slice 4, diseases
that spread, waits. The owner answered all eleven question cards on 27 Nov 2026
([the answers](#the-owners-answers-27-nov-2026-by-card-with-every-option-offered)).

The owner, 27 Nov 2026, of the gaps and suggestions listed once the [care needs](medical-care-needs.md) were finished: "I wish for all
of these gaps and suggestions to be fulfilled", then "Go ahead and write the record with the question cards". How care works today:
[injuries and care](systems/medical-injuries-and-care.md). Every figure and class marked [A] is the agent's, authored, for the owner to
revise.

## Read this first

- **Five slices, not six.** The mods, the game's code and the owner's Defs were read before this was written, and four items on the
  list turned out to be built, or another session's, or far smaller than they looked ([what the reading found](#what-the-reading-found)).
- **Nothing is found by name**, as before: a disease's class, a drug's class and a mask are data on Defs, and another mod adds its own
  with a patch.
- **The riskiest slice is diseases that spread.** It changes how a colony plays, so it has its own setting and the agent grades it
  high: it would start off.
- **The owner's Defs file is a week old** (written 19 Nov 2026 with 364 active mods; 387 are active now). **Read the Defs again
  before building each slice.**
- None of this is medical advice. Real medicine is the model for what is plausible, never the source of a figure.

## As built: Medical 0.34.0

Slice 2, as designed below, with these differences and findings.

- **The game lets mechanoids do all doctor work but tend animals**: its own operate and tend givers say nothing, which means yes. So
  "everything their body can do" opened every work giver of ours, triage's operations included, and nothing needed a body check of
  its own. A mechanoid has no skills: where a kit asks a Medicine level, its race's fixed level counts (10 for most).
- **Triage now counts mechanoid surgeons** when it asks whether anyone can do an operation, through the Framework's shared doctor test.
- **Animals have their own switch**, on (agent default, after the risk grade: moderate). The card chose which animals; the switch lets a
  player with a large herd say no.
- **An animal is dosed for pain only when it is extreme**, as the card's design said, and the pain ladder aims under the same band.
- **Agent choice: no prevention, long-term care or self-medication for animals**, and burns, smoke and triage stay people only, as the
  design said.

## As built: Medical 0.33.0

Slice 1, as designed below, with these differences and findings.

- **The serum's target was read before building**, as the limits said it must be: `HealthUtility.TryGetWorstHealthCondition` takes a
  life-threatening condition first (visible, curable by an item, not yet immune, at its life-threatening stage or 80% of the way to
  lethal; of several, the one on the largest part), then blood loss about to kill, then others. Triage asks it, without changing
  anything, and queues the serum only when its answer is the condition killing the patient. So the risk named in the limits is closed.
- **The serum's operation is found by what its item does**: an "administer" operation whose one ingredient carries the game's
  `CompUseEffect_FixWorstHealthCondition`. It has no failure outcome, so a doctor being free is all it needs.
- **The letter is sent when the serum is queued**, not when it is given (agent choice: the game's own operation ends with its own
  message, and a letter before lets the player stop it). Once per patient and condition.
- **The pain ladder's threshold** is the point where the patient would be dosed at all: 40% for doctors, the person's own threshold for
  self-medication. Agent choice.
- **Classes are a Def of their own**, with a mod extension on each disease, patched only where the disease exists. Genie's antibiotics
  are the only drug classed; the "Medicine needed" alert does not yet name a class (agent choice: the alert reads by need, and a class
  would need its own line).

## What the reading found

| Item on the list | What was found (27 Nov 2026) | So |
| --- | --- | --- |
| Triage's three open points | **All three are built**, in D'thorga Medical 0.7.0: the needle decompression kit, the button beside the health tab's medicine choice, and the expectant tag shown as an icon (the owner's answers of 7 Nov 2026, [the triage record](archive/triage.md#open-points-and-upstream)). The agent's note saying they were open was three weeks stale | Nothing to build. Not yet tested in game, as the rest of triage |
| Medevac by radio | **The owner chose it on 27 Nov 2026 and another session is building it** ([radio friends, School of the Air, the medevac](radio-friends-school-medevac.md#the-flying-doctor-flies-a-medevac)): a convoy button, the Flying Doctor as its model | Left to that record. Nothing here |
| Evacuation to a vehicle | **Ours is built**: Take casualties aboard carries the downed to a parked aircraft's litters, in doctors' work and in Search and Rescue's field rescue work. What is missing is Search and Rescue's own planner ending a rescue at a vehicle: its list of places is fixed in its code | A request for its author, drafted in the [soft-dependency record](soft-dependency-crosslinks.md). Card 11 |
| Diseases that spread | **Vanilla Expanded Framework, which D'thorga Medical already requires, has a spreading comp** (`VEF.Hediffs.HediffCompProperties_Spreadable`): a radius, a chance, an interval, line of sight, spread within a caravan and in a chat, the game's own immunity factor, and **apparel or a stat that stops it**. No disease in the owner's game carries it | The model exists. This slice is mostly Defs, and masks come with it. Prevention in an outbreak (0.28.0) already treats a condition with this comp as an outbreak |
| Items only used, never swallowed | The one medical item is the **healer mech serum** (1,300 silver). **The game has an operation that gives it** (`AdministerMechSerumHealer`), and triage already queues operations | No new job: triage queues that operation, under a rule of its own. Card 3 |
| Hygiene that prevents | **Dubs Bad Hygiene already brings cholera and dysentery** from bad water and poor hygiene, and both can be caught only that way | The proposal narrows to gut worms and food poisoning, as the earlier record had it. Card 8 |
| Animals | Every part of care asks for a person (`Patients.IsPlayersPerson`: humanlike). Of 76 medical items that state their own category, 22 are marked for people only on their own Def; the rest inherit, unread | A real slice: who, and with what. Card 4 |
| Mechanoid doctors | Eight of our work givers say `canBeDoneByMechs` false | A switch per work giver, and a check that a mechanoid's body can do each job. Card 5 |

**The diseases in the owner's game**, read from the patched Defs: 16 a person can become immune to, and 7 more that an event brings
and immunity never ends.

| Class [A] | Diseases | Why |
| --- | --- | --- |
| Bacterial | Plague, wound infection, cholera, dysentery, pneumonia (ours, after smoke), animal plague, shambler plague | Ushanka's Biological Warfare classes plague and wound infection as bacteria in its own pathogen Defs; cholera is a bacterium; dysentery is taken as the bacterial kind; pneumonia after smoke injury is usually bacterial |
| Viral | Flu, animal flu, necroa, scaria infection, developing scaria | Biological Warfare classes flu, necroa and scaria as viruses |
| Protozoan | Malaria, sleeping sickness | Both are protozoa (*Plasmodium*, *Trypanosoma*); Biological Warfare calls them parasites |
| Worms | Gut worms, muscle parasites, exoworm infestation | Helminths, or their like from another world |
| Mechanites | Fibrous mechanites, sensory mechanites | Machines, not germs: no drug chemistry touches them |
| None | Abasia, blood rot, organ decay, infant illness | No cause the Defs name |

## The slices

| Slice | What | Size |
| --- | --- | --- |
| **1. Sharper dosing** | Pain strength; disease and drug classes; the healer mech serum through triage | Small: the dosing rules and the index |
| **2. Who is covered** | Animals; mechanoid doctors | Medium: the patient test is shared by every part |
| **3. Prevention of our own** | A vaccine; hygiene | Medium: new items, research and art (items are made the paid way) |
| **4. Diseases that spread, and masks** | The spreading comp on chosen diseases, masks that stop it, and what the colony is told | Medium in code, large in play |
| **5. Links** | The request to Search and Rescue's author | A draft to check and hand over |

**The owner's testing:** the whole is built before a first test, as the owner prefers; an install after slices 1 and 3 keeps the test
list to one sitting.

### 1. Sharper dosing

**Pain strength.** Today the best-scoring painkiller is given whatever the pain: 45% pain and 95% pain get the same drug. The proposal
is the mildest that does the job: among the items the safety rule allows, the one with the least cost whose relief brings pain under
the threshold; if none can, the strongest. This is the shape of the World Health Organization's analgesic ladder (mild medicines first,
strong ones as pain demands), named as the model, not as a source of figures. Game-free, in `DoseChoiceRule`.

**Disease and drug classes.** A disease's class is a `PhobosDiseaseClassDef` named on its HediffDef through an extension, shipped by us
for the diseases above and patchable by any mod. A drug for fighting disease may declare the classes it acts on
(`PhobosDosingExtension.actsOn`); **a drug that declares none acts on every class, as now**, so nothing changes until an item is
classed. Vanilla Races Expanded - Genie's antibiotics would be declared bacterial, in a tie-in folder, and stop being spent on flu.
The same classes carry the vaccines of slice 3. **Penoxycyline is untouched**: it prevents by the game's own `makeImmuneTo`, and is
not a drug for fighting disease. (No single real medicine covers malaria, sleeping sickness and plague as it does; doxycycline comes
nearest, used against plague and to prevent malaria.)

**The healer mech serum.** Triage would queue the game's own "administer healer mech serum" operation for someone dying of a condition
nothing else in the colony can treat: the patients it tags expectant today. The serum mends the condition the game judges worst
(`HealthUtility.TryGetWorstHealthCondition`; **its order is to be read before building**, since a serum spent on the wrong condition
is 1,300 silver gone).

### 2. Who is covered

**Animals.** The patient test gains a second kind of patient, and each care need says whether it takes it. Proposed [A]: cures,
drugs for fighting disease, protection from toxins and pain relief past the sedation band; never prevention, the long-term needs or
anything addictive. An item marked for people only is never given to an animal. Burns, smoke and triage stay people only: their
models were built on a person's body.

**Mechanoid doctors.** The owner chose everything a mechanoid's body can do, kits included (card 5). The work givers are opened to
mechanoids one at a time, each after reading that a mechanoid can do its job: it carries, it has no skill to learn, and some of our
kits ask a Medicine level, **so how a kit's level is met by a mechanoid is the first thing to settle in the slice** (the game gives a
mechanoid a fixed skill level for its work). A paramedic mechanoid already rescues and tends in the game.

### 3. Prevention of our own

**A vaccine.** As the [vaccines record](vaccines-and-prevention.md) proposed: a "primed immunity" condition, so that when the disease
strikes, immunity starts part-way up and the patient usually wins the race, but can still lose. One vaccine per class is the natural
fit with slice 1. Made at a drug lab behind one research; an item in the world, so its art is made the paid way.

**Hygiene.** Clean hands and clean water lower the chance of gut worms and food poisoning, with Dubs Bad Hygiene only. Small, and
partly covered already: see card 8.

### 4. Diseases that spread, and masks

The spreading comp is added to flu and to plague, **each behind its own setting, off by default** (the owner's words on card 9),
with its figures in our rules Def. A mask stops it through the comp's own
`statsPreventingSpreading`, read from the toxic environment resistance the protective-gear lines already use, so any mod's mask counts
by what it does. **Switching it needs a restart** [A]: the comp is on the Def, and Defs are fixed once the game has loaded.

What it brings with it, each to be designed in the slice: an outbreak alert that names who caught it from whom; isolation as an
ordinary bedroom assignment (no new zone); and prevention in an outbreak, built in 0.28.0, finally protecting against the bout under
way and not only the next one.

**Performance** is the first thing to measure: the comp looks at every pawn within its radius at each interval, for each sick pawn.
On the owner's colony that is recorded before and after (`scripts/perf.py`, with the owner's go).

### 5. Links

The request to Search and Rescue's author is checked against its newest public source, as the shared rule on reports asks, and handed
to the owner to post. Nothing is patched: its author is active.

## The four questions

| Behaviour | Lets the player | Stops the player | Changes how they play | Who else notices |
| --- | --- | --- | --- | --- |
| Pain strength | Keep strong medicines for strong pain | Nothing | Mild stock is used first, so the strong stock lasts | The stockpile; the patient's consciousness |
| Disease classes | Stock the right drug for the disease | Treating a virus with an antibiotic | A second shelf to think about; an unclassed drug stays a broad one | The "Medicine needed" alert names the class |
| Healer serum by triage | Have a dying colonist saved while away from the screen | Nothing: the bill can be removed | A serum in stock is a life held in reserve | A letter when one is spent |
| Animals | Trust the barn to the doctors | Nothing | Medicine stock is shared with animals | The animal's master; the stockpile |
| Mechanoid doctors | Run a clinic with mechanoids | Nothing | A mechanitor colony gets automatic care | The mechanitor's bandwidth, already spent |
| A vaccine | Protect a colony before a disease arrives | Nothing | A research and a yearly job | Raiders and visitors are not protected |
| Hygiene | Lower two diseases by keeping clean | Nothing | Another reason to build washrooms | The cook; food poisoning's own thought |
| Diseases that spread | See an epidemic, and fight it | Ignoring one sick colonist | Isolation, masks and prevention start to matter | The whole colony; visitors; a caravan; an alert |
| Masks | Send a nurse in safely | Nothing | Masks have a use beyond toxins | Whoever is nursed |

## Risk grades (agent proposals, for the owner to confirm)

| Behaviour | What can be lost | Grade | Starts |
| --- | --- | --- | --- |
| Pain strength | A patient left in more pain than the strongest drug would leave them | Low | On |
| Disease classes | A patient with no drug of the right class goes without, where today they would get the wrong one (which does nothing real, but did count) | Moderate | On |
| Healer serum by triage | A 1,300-silver item, at once | High | Off |
| Animals | Medicine stock run down by a large herd | Moderate | On, for the animals card 4 names |
| Mechanoid doctors | A kit used by a mechanoid where a skilled doctor would have done better, if a kit's quality follows skill | Low | On |
| A vaccine | Nothing: it only helps | Low | On |
| Hygiene | Nothing: it only helps | Low | On |
| Flu that spreads | Several colonists in bed at once; flu seldom kills | High | Off, its own setting |
| Plague that spreads | Several colonists ill at once, and the deaths that follow | High | Off, its own setting |

## The owner's answers (27 Nov 2026, by card), with every option offered

Put by card in the session, in three rounds. The chosen option is first and marked; the others are kept as they were offered, so a
later change of mind starts from the same list. The owner chose the agent's lean on nine cards, and their own answer on cards 5 and 9.

### 1. Which painkiller for how much pain

- **Chosen: the mildest that does the job.** The item with the least cost that brings pain under the threshold; the strongest
  when none can.
- Strong pain prefers strong drugs, mild pain as now: only pain past the sedation band changes the choice.
- Leave it: the best score whatever the pain.

### 2. A disease nobody has classed

- **Chosen: any drug counts for it.** As now, so a new mod's disease is never left untreated because we had not read it.
- Only drugs with no class count: a classed antibiotic is never spent on an unknown disease.
- Nothing by rule: the player gives the drug by hand until the disease is classed.

### 3. The healer mech serum

- **Chosen: only for someone dying whom nothing else can save, and off until turned on.** Triage queues the game's own
  operation for a patient it tags expectant; a letter says it was spent.
- The same, on by default.
- Never by rule: it stays the player's hand, and the record says why.

### 4. Which animals get care by rule

- **Chosen: bonded animals and those with a master.** The ones the colony would grieve.
- Every tame animal.
- Animals worth more than a mark the player sets.
- None: care stays for people.

### 5. Mechanoid doctors

- **Chosen: everything a mechanoid's body can do, kits included** (not the agent's lean).
- Doses and bedside devices (the agent's lean): the jobs that carry a thing to a patient; the kits that ask a Medicine level stay
  with people.
- Leave it: our work stays with people.

### 6. How many vaccines

- **Chosen: one per class of disease.** Bacterial, viral, protozoan and worms; none for mechanites, which are machines.
- One per disease.
- One broad vaccine.

### 7. How much a vaccine helps

- **Chosen: immunity starts a third of the way up.** Most win the race, the old and weak can still lose.
- Half way up.
- Full immunity, as penoxycyline gives.

### 8. Hygiene that prevents

- **Chosen: as proposed, kept small.** With Dubs Bad Hygiene, washing lowers the chance of gut worms and food poisoning.
- Skip it: Dubs Bad Hygiene already brings cholera and dysentery from bad water and poor hygiene, and that is enough.
- Wider: clean hands also lower the chance of a tended wound becoming infected.

### 9. Which diseases spread

- **Chosen, in the owner's own words: "Flu and plague, but keep each gated behind a setting that's off by default."** So two
  settings, one for each disease, each off.
- Flu only (the agent's lean): the mildest, to learn how the model plays before anything deadly spreads.
- Flu and plague (as one choice, before the owner's own answer).
- Every bacterial and viral disease caught by breath or touch (not wound infection, cholera or dysentery).

### 10. Whether spreading starts on

- **Chosen: off until turned on.** The shared rule for a high risk: it can put several colonists in bed at once.
- On by default.

### 11. The request to Search and Rescue's author

- **Chosen: check the draft against its newest source and hand it to the owner to post.**
- Leave it: Take casualties aboard is enough.

## Agent defaults (for the owner to revise)

- **Classes are ours to ship** for the diseases above, in D'thorga Medical's Defs and its tie-in folders; a mod's own diseases are
  classed by that mod, or by a patch.
- **Only Genie's antibiotics are classed at first** (bacterial). Every other drug for fighting disease stays broad until someone
  reads what it claims to be.
- **Animals never get** prevention, the long-term needs, anything addictive, or an item marked for people only.
- **A vaccine lasts a year** and is made at a drug lab behind one research.
- **A mask counts** when it gives 0.5 toxic environment resistance or more: the game's face mask and above.
- **Spreading's figures** start gentle: three cells, line of sight, a low chance at a long interval, to be set from play.
- **Switching spreading needs a restart.**

## Limits known before building

- **The healer serum chooses its own target**: the game's worst condition, which may not be the one killing the patient. To be read
  before building; if it cannot be relied on, the slice says so and the serum stays the player's hand.
- **The spreading comp is another author's code** (Vanilla Expanded Framework, CC BY-NC-ND 4.0 with a rider that permits depending on
  it). We set its figures in Defs and never copy or adapt it; a change in it changes our epidemic.
- **Animals' medicine is mostly unread**: which items the game lets an animal swallow is on parent Defs the patched file does not
  merge. Read in the game, with the Framework's "write patched Defs" and the dose index's dev actions.
- **Not generic across games yet**: this is RimWorld's health model. A next game would need its own readers; the game-free choice,
  class and ladder rules would carry over.

## Sources

The owner's patched Defs file (written 19 Nov 2026 by game 1.7.5013 with 364 active mods), read with throwaway scripts on 27 Nov 2026
for every disease, every spreading comp and every operation that gives a usable item. Vanilla Expanded Framework's spreading comp, read
from a decompile kept locally and summarised in our own words (the Vanilla Expanded team's work). Ushanka's Biological Warfare's
pathogen classes, from its Defs. The game's own code through `query-code.py`. D'thorga Medical's code and records as of commit
63047d10. The World Health Organization's analgesic ladder is named as the model for pain strength; no figure is taken from it.
