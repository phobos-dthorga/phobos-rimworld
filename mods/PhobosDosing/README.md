# D'thorga Emergency Dosing

Doctors give painkillers and sedatives on their own, from whatever mod made them.

Version 0.1.0, built 5 November 2026 for House D'thorga. **Not yet tested in game.** Everything here is read from the game's and
the mods' code.

## Read this first (gotchas)

- **Needs D'thorga Framework**, loaded before it. No Harmony. Nothing is saved: **safe to add or remove at any time**.
- **Who gets dosed:** a colonist, prisoner or guest who is **down or in bed**, in **severe pain** (the game's 60% band), under
  medical care of at least the level in the settings (default: herbal medicine or better; "no medicine" is never dosed), and
  not reserved by another doctor. A pawn up and working in pain is left alone: a painkiller could drop them where they stand.
- **What is given:** the painkiller with the strongest relief net of its side effects, from the doctor's inventory first, then
  the nearest unforbidden stack. **Past the pain-shock band (80%) a sedative is given when nothing lighter can be.**
- **What is never given:** a second dose while the first still works; anything during an overdose; a dose that would reach a
  dangerous stage; a dose whose offsets would leave consciousness or breathing under 15%; **recreational drugs unless you turn
  the setting on**; drugs to a patient who refuses them on principle, unless you turn that setting on.
- **Any mod's drugs count.** The mod never looks for a drug by name: it reads what each ingestible does from its definitions.
  A mod can keep an item out with a one-line extension (below). **An effect a mod applies from its own code, not its XML,
  is invisible**: More Injuries' epinephrine, for instance, which More Injuries and Search and Rescue already give on their own.
- **Search and Rescue and More Injuries are not changed.** Their doctors keep their patients; this mod waits for a patient
  nobody has reserved. In the Work tab the job sits under Doctor between tending and feeding patients.
- **Stock is your job.** The colony needs the drugs made or bought; a doctor who carries them (Combat Extended loadouts, Smart
  Medicine's stock-up) is fastest.
- **Mechs do not dose** in this version.

## What it does

| Need | Who | Given | Not given when |
| --- | --- | --- | --- |
| pain relief | resting, pain at or past 60% | the painkiller with the best score | last dose still at a quarter or more; overdose at 30% or more; a dangerous stage would be reached; consciousness or breathing would fall under 15% |
| sedation | resting, pain at or past 80%, no painkiller can be given | the sedative with the best score | the same |

Score = pain relief minus half the consciousness cost (not for sedatives) minus half the breathing cost minus the bleed increase
(a quarter of it when the patient is not bleeding) minus half the addictiveness. Every figure is an authored starting value in
the rules Def.

Worked examples from the Defs, not from play: More Injuries' morphine autoinjector gives relief 0.8 at a consciousness cost of
0.4 and breathing 0.2, score 0.5; Medicines+ thornweed serum gives relief 0.4 with no capacity cost, score 0.4; ketamine and
chloroform knock the patient out (relief 1, sedatives). So morphine is preferred when in reach, thornweed when it is not, and
ketamine only at pain shock with neither in reach.

A right-click on a patient says why no dose can be given: nothing in reach, the last dose still working, a dangerous dose, and
so on.

## Settings

Doctors give pain relief on their own (on); dose patients whose medical care is at least (herbal or better); use recreational
drugs for pain (off); give medical drugs to patients who refuse drugs (off); the display mode.

## For modders

Keep an ingestible out of automatic dosing:

```xml
<modExtensions>
  <li Class="Phobos.Dosing.Defs.PhobosDosingExtension">
    <neverAutoDose>true</neverAutoDose>
  </li>
</modExtensions>
```

An ingestible is indexed when an IngestionOutcomeDoer_GiveHediff of its gives a condition whose stage at the dose relieves pain
(painFactor under 1 or a negative painOffset). Consciousness and breathing offsets, consciousness setMax, totalBleedFactor and
the drug comp's addictiveness and category are read from the same Defs. Balance lives in
`Common/Defs/Phobos/Dosing/Rules.xml` (one `PhobosDosingRulesDef`, first loaded wins).

## Checks done (5 November 2026)

- Built against the RimWorld 1.7 and 1.6 reference assemblies. The game-free rules (effect from stages, need bands, safety,
  choice) pass unit tests.
- Read from code: More Injuries, Search and Rescue, Medicines+, Smart Medicine, MSE2 and VFE Medical give no painkiller or
  sedative automatically; More Injuries' drugs are a drafted doctor's right-click option, and Search and Rescue's doctors are
  undrafted by rule (see the design record).
- **Not checked:** anything in game. The job's toils, the patient cache, the work giver's priority and the index's reading of
  the owner's mods are all untested.
