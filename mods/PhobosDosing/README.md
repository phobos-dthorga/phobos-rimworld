# D'thorga Emergency Dosing

Doctors give painkillers and sedatives on their own, from whatever mod made them.

Version 0.3.0, built 5 November 2026 for House D'thorga. **Not yet tested in game.** Everything here is read from the game's and
the mods' code.

## Read this first (gotchas)

- **Needs D'thorga Framework**, loaded before it. No Harmony. Nothing is saved: **safe to add or remove at any time**.
- **Who gets dosed:** a colonist, prisoner or guest past babyhood who is **down or in bed**, in **severe pain** (60% or more;
  the game labels pain severe from 40%), under medical care of at least the level in the settings (default: herbal medicine or
  better; "no medicine" is never dosed), and not reserved by another doctor. A pawn up and working in pain is left alone: a painkiller could drop them where they stand.
- **What is given:** the painkiller with the strongest relief net of its side effects, from the doctor's inventory first, then
  the nearest unforbidden stack. **In extreme pain (80% or more) a sedative is given when no medical painkiller can be.**
- **What is never given:** a second painkiller while a medical one still works; anything during an overdose; a dose that would
  reach a dangerous stage; a dose whose offsets would leave consciousness or breathing under 15%; **recreational drugs unless
  you turn the setting on, and then only as a last resort**; drugs to a patient who refuses them on principle, unless you turn
  that setting on.
- **A drug counts as a painkiller only if it takes away 30% of the pain or more.** That keeps out relief that is only a side
  effect, by effect and not by name: **your epinephrine stays for cardiac arrest** (about 23%), and beer, tea and the like stay
  out too.
- **Any mod's drugs count.** The mod never looks for a drug by name: it reads what each ingestible does from its definitions.
  A mod can keep an item out with a one-line extension (below). **An effect a mod applies from its own code, not its XML,
  is invisible**: More Injuries raises bleeding after morphine through a linked condition this mod cannot see.
- **Search and Rescue and More Injuries are not changed.** Their doctors keep their patients; this mod waits for a patient
  nobody has reserved. In the Work tab the job sits under Doctor between tending and feeding patients.
- **Stock is your job.** The colony needs the drugs made or bought; a doctor who carries them (Combat Extended loadouts, Smart
  Medicine's stock-up) is fastest.
- **Doctors also give drugs that help fight disease, and only doctors do.** A patient down or in bed with a disease that can kill them, when immunity would
  arrive too late or within a day of the lethal mark, gets a medical drug that adds at least 10% to immunity gain speed. **A patient already winning gets none, so
  your stock lasts.** Drinks such as tea never count, nor do drugs that give a disease.
- **A patient bleeding at 30% a day or more waits for a painkiller until that is under control**, on both the doctors' and the self-medication paths. A sedative in extreme
  pain and a drug for fighting disease do not wait. The bleeding has to be tended for this to clear.
- **Mechs do not dose** in this version.
- **People also fetch their own painkillers, and your drug policies decide what.** An adult colonist who is up and about at 40% pain or more
  goes and takes the best painkiller whose policy line allows it, for joy or on a schedule. **Addiction and overdose are real
  risks, so check your drug policies before enabling this on a colony.** The setting is on by default.
- **Chemical interest and fascination ignore the policy**, as they do for joy in the game, and reach for it sooner (30% and 20%).
  **Teetotalers never self-medicate**, and neither do children.
- **A pawn under medical rest is left to the doctors.** That means anyone in bed, or who should be, including anyone with a tended wound still
  healing. In bed, doctors only step in from 60% pain, so a patient between 40% and 60% gets nothing yet.
- **Guests have no drug policy in the game.** With the guests setting on, they take medical painkillers from your stock and never recreational ones,
  so your morphine can go to a visitor. It is off by default.
- **Nobody knocks themself out.** A pawn on their feet refuses a dose that would leave consciousness or breathing under 40%, so a drowsy colonist may
  pass over morphine, whose cost is high.
- **Sedatives are never taken by choice**, and nobody takes from another pawn's pockets.

## What it does

| Need | Who | Given | Not given when |
| --- | --- | --- | --- |
| pain relief | resting, pain at or past 60% | the medical painkiller with the best score | a medical painkiller still at a quarter of a dose or more; overdose at 30% or more; a dangerous stage would be reached; consciousness or breathing would fall under 15% |
| sedation | resting, pain at or past 80%, no medical painkiller can be given | the sedative with the best score | its own last dose still working; otherwise the same |
| last resort | setting on, nothing above can be given | the recreational painkiller with the best score | the same |

Score = pain relief minus half the consciousness cost (not for sedatives) minus half the breathing cost minus the bleed increase
(a quarter of it when the patient is not bleeding) minus half the addictiveness. Every figure is an authored starting value in
the rules Def.

Worked examples from the Defs, not from play: More Injuries' morphine autoinjector gives relief 0.8 at a consciousness cost of
0.4 and breathing 0.2, score 0.5; Medicines+ thornweed serum gives relief 0.4 with no capacity cost, score 0.4; ketamine and
chloroform knock the patient out (relief 1, sedatives). So morphine is preferred when in reach, thornweed when it is not, and
ketamine only at pain shock with neither in reach.

With a doctor selected, a right-click on a patient in need says why no dose can be given: the care setting, nothing in reach,
an earlier dose still working, a dangerous dose, and so on. Nothing shows for a pawn who needs no dose.

## Settings

Doctors give pain relief on their own (on); dose patients whose medical care is at least (herbal or better); use recreational
drugs for pain (off); give medical drugs to patients who refuse drugs (off); doctors give drugs that help fight disease (on); people in pain fetch their own painkillers (on); guests may
take painkillers from your stock (off, for mods such as Hospitality); the display mode. The recreational setting is about what doctors give: a
pawn fetching their own goes by their drug policy instead.

## For modders

Keep an ingestible out of automatic dosing:

```xml
<modExtensions>
  <li Class="Phobos.Dosing.Defs.PhobosDosingExtension">
    <neverAutoDose>true</neverAutoDose>
  </li>
</modExtensions>
```

An ingestible is indexed when an IngestionOutcomeDoer_GiveHediff of its gives a condition whose stage at the dose takes away at
least 30% of the pain (through painFactor and a negative painOffset) and its score is above zero. Consciousness and breathing offsets, consciousness setMax, totalBleedFactor and
the drug comp's addictiveness and category are read from the same Defs. Balance lives in
`Common/Defs/Phobos/Dosing/Rules.xml` (one `PhobosDosingRulesDef`, first loaded wins).

## Checks done (5 November 2026)

- Built against the RimWorld 1.7 and 1.6 reference assemblies. The game-free rules (effect from stages, need bands, safety,
  choice and tiers, self-medication's threshold and policy gate) pass 23 unit tests.
- 0.1.0 loaded in the owner's game with no error naming it (5 November 2026); its load line showed the faults 0.1.1 fixes. That
  is a load, not a test of dosing.
- Read from code: More Injuries, Search and Rescue, Medicines+, Smart Medicine, MSE2 and VFE Medical give no painkiller or
  sedative automatically; More Injuries' drugs are a drafted doctor's right-click option, and Search and Rescue's doctors are
  undrafted by rule (see the design record).
- **Not checked:** anything in game, self-medication included. Its think-tree hook, the ingest job and how often pawns reach for a drug are
  all untested.
- **Also not checked:** the rest of the mod in game. The job's toils, the patient cache, the work giver's priority and the index's reading of
  the owner's mods are all untested.
