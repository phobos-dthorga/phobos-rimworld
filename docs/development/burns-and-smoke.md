# Burns and smoke: research and decisions (4 Nov 2026)

The record behind [D'thorga Burns and Smoke](../../mods/PhobosBurnsAndSmoke/README.md). **0.2.0 not yet tested in game**
(0.1.0 was seen in play once; see Results from play). It is the
first consumer of [D'thorga Framework](phobos-framework.md).

## The problem

The owner ran an incendiary raid on an enemy city and found burns and smoke "not up to par" with the rest of More Injuries. The
save PG18 (4 November 2026, 18:06) confirmed it: 17 enemy pawns carried 112 plain `Burn` injuries (median severity 9.8, the
worst 36, up to 15 burns on one pawn, totals over 100 hit points), 15 carried Combat Extended's `SmokeInhalation` (the highest
0.9, most near 0), and nothing else had happened to any of them: no shock, no airway injury, no hypoxia, no carbon monoxide, no
fluid loss, no lasting lung damage. The log had no fire-related errors.

## The chain, as read from the code (4 Nov 2026)

- **The game.** A fire attached to a pawn deals 2 to 3 `Flame` damage every 150 ticks to a coverage-weighted random outside part
  and to one worn item (`RimWorld.Fire.DoFireDamage`); `Burn` is a plain injury (`BurnBase`: pain 0.01875 per severity, infection
  chance 0.30, tendable, merges with an untended burn on the same part, never bleeds, scars). Ambient air 150 C over the safe
  maximum gives `Burn` damage every 420 ticks (`HediffGiver_Heat`). Heatstroke is the only systemic heat condition. Fires put
  nothing in the gas grid.
- **Combat Extended** doubles the interval between fire hits and scales them by body size, patches `BurnBase` (pain 0.025, its
  own infection comp), adds non-merging `BurnSecondary` burns from incendiary ammunition and lasers, and gives roofed fires
  black smoke with its own inhalation condition (see the framework record).
- **More Injuries 1.6.0.2** has one fire feature, `InhalationInjuryWorker`: when a burning pawn takes burn damage it adds plain
  `Burn` injuries to both lungs (random 0.05 to 1, scaled by flammability and toxic environment resistance), gated by its
  setting `EnableFireInhalation` (default on). It also checks for a `CE_WearingGasMask` hediff that Combat Extended does not
  define, so its mask exemption never fires. Its secondary-condition system is public XML (`HediffCompProperties_SecondaryCondition`
  with handlers, makers, modifiers and target evaluators; More Injuries: Organ Failure drives it from its own XML). Hypoxia is
  two injury Defs, `OrganHypoxia` and `BrainDamage_Hypoxia`. Choking triggers only on bleeding injuries. Its suction and CPR
  target lists are hard-coded arrays. Its comp sits only on Human and Humanoid Alien Races flesh races.
- **Hooks the mod uses, none of them Harmony:** `HediffGiverSetDef` and `HediffGiver.OnIntervalPassed` every 60 ticks per pawn
  (one `PatchOperationAdd` into `OrganicStandard`, as Atmosphere Overhaul does), `HediffGiver.OnHediffAdded` for every
  condition the game adds, stage fields `lifeThreatening`, `deathMtbDays` and `hediffGivers` (`HediffGiver_Random`, as the
  game's dementia uses), `HediffCompProperties_TendDuration.severityPerDayTended` (a tended condition loses severity at that rate
  times tend quality; infections use it), `HediffCompProperties_SeverityPerDay`, `Recipe_RemoveHediff` with `addsHediff`.
- **Search and Rescue** treats a tendable condition with a life-threatening stage as an emergency, and a life-threatening one
  that some `removesHediff` surgery can remove as an urgent surgical case. **Organ Failure** turns a death through breathing
  into its respiratory-failure chain.
- **Human skin share**, computed from Core's `Bodies_Humanlike.xml`. The engine keeps two figures per part
  (`BodyDef.CacheDataRecursive`, decompiled from 1.7.4998 on 4 Nov 2026): `coverageAbsWithChildren`, the part's coverage times
  its parent's including-children figure (the root is 1), and `coverageAbs`, that times one minus the sum of all its children's
  coverage, which is the hit-chance weight for the part alone. An outside part's skin is its `coverageAbsWithChildren` less its
  outside children's `coverageAbsWithChildren`, so an inside organ's coverage stays with the skin over it: torso 0.405, each leg 0.126 (plus foot 0.0085 and toes 0.005), each shoulder 0.028 plus arm 0.080,
  hand 0.008 and fingers 0.005, neck 0.015, head 0.028 plus jaw 0.009, nose 0.006, ears 0.008 and eyes 0.008; the sum is 1.
  It is a hit-chance model, not skin area; the rule of nines gives head 9%, each arm 9%, each leg 18%, torso 36%. Labelled
  approximate. (0.1.0 subtracted the outside children from `coverageAbs`, which already leaves every child out: torso, neck,
  head and shoulders came to 0 and the outside parts summed to 0.36. Fixed in 0.2.0.)
- **A destroyed part** gets a `Hediff_MissingPart` whose `lastInjury` is the injury Def that destroyed it (`Burn` for fire, hence
  the "burned off" label), and every part under it gets its own non-fresh record with the same `lastInjury`. The stump is fresh
  until tended or 90,000 ticks (1.5 days) old; only a fresh stump bleeds, at the injury's bleed rate, so a burn amputation never
  bleeds. `Find.TickManager` is `Current.Game.tickManager` and throws before a game exists.

## Sources

- Rule of nines and total body surface area; the Parkland formula, 4 mL per kg per percent burned over 24 hours
  ([StatPearls](https://www.statpearls.com/point-of-care/158070); [Wikipedia, Parkland formula](https://en.wikipedia.org/wiki/Parkland_formula)).
- The revised Baux score, age plus percent burned plus 17 with an inhalation injury (Osler and colleagues, 2010); cohorts report
  no deaths below about 36 and no survivors above about 110
  ([PMC9555034](https://pmc.ncbi.nlm.nih.gov/articles/PMC9555034); [PMC6199008](https://pmc.ncbi.nlm.nih.gov/articles/PMC6199008)).
- Inhalation injury: heat injury of the upper airway with delayed oedema, chemical injury of the lower airway, systemic carbon
  monoxide and cyanide; it complicates 10 to 20% of burns and raises mortality (Walker and colleagues, Critical Care 2015,
  [PMC4624587](https://pmc.ncbi.nlm.nih.gov/articles/PMC4624587); [PMC8794442](https://pmc.ncbi.nlm.nih.gov/articles/PMC8794442)).
- Carbon monoxide half-life about 320 minutes on room air, 70 to 80 minutes on oxygen (Weaver and colleagues, 2000, as cited in
  [UMEM educational pearls](https://umem.org/educational_pearls/2365) and the
  [NHS England review of hyperbaric oxygen](https://www.engage.england.nhs.uk/consultation/comm-pol-hyperbaric-oxygen-therapy/supporting_documents/1678evidencereviewhbotcopoisoning.pdf)).
- Circumferential full-thickness burns and escharotomy ([NCBI Bookshelf, NBK482120](https://www.ncbi.nlm.nih.gov/books/NBK482120/)).
- Burned patients lose thermoregulation; 40 to 80% of severe burns are hypothermic in the acute phase (Enfermeria Global 2021;
  [PMC9501704](https://pmc.ncbi.nlm.nih.gov/articles/PMC9501704)).
- Sepsis is the leading late killer, mostly within two weeks ([PMC7901709](https://www.ncbi.nlm.nih.gov/pmc/articles/PMC7901709/)).
  The game's own infection comp on burns already covers wound infection.

Every stage figure, rate and threshold in the mod is an authored starting value shaped by these; the sources give the shape,
not the numbers.

## Decisions

- **Derived, not accumulated** (agent choice). The burned fraction is recomputed every 300 ticks from the burns a pawn has,
  by `HediffGiver_PhobosBurns`, rather than accumulated per hit through `DamageDef.additionalHediffs`: accumulation cannot see
  tending, healing or Combat Extended's non-merging burns, and would double-count merges.
- **One giver, humanlike only, early-outs** (agent choice). The giver reaches every flesh race through `OrganicStandard` but
  returns at once for anyone without burns, fire, one of our conditions, or smoke; animals wait for a later version.
- **Smoke from the framework, carbon monoxide from its gas** (owner's rule). Breathed smoke is Combat Extended's inhalation
  severity while CE's smoke runs, otherwise the game's smoke through the mask; carbon monoxide comes from the framework's room
  gas, and is inferred from breathed smoke times enclosure only when that gas is switched off.
- **A part burned off is its whole subtree's skin burned** (agent choice, 0.2.0): area = the missing part's
  `coverageAbsWithChildren` times the destroying injury's `tbsaWeight`, parts under another missing part skipped. The stump
  counts whole while fresh, then fades linearly to nothing over `burnedOffHealDays` (15, authored: a grafted or granulating
  stump takes weeks) by the record's age; a tended stump counts as tended area. A late tend drops the weight at once, a
  labelled simplification. Burn amputations keep the game's rule and do not bleed.
- **The compensated stage always shows past the threshold** (agent choice, 0.2.0): the shock target is at least
  `shockThresholdFloor` (0.1, authored) once 15% is burned, so a young pawn below the Baux no-death mark still shows the
  fluids job; the death clocks stay Baux-driven.
- **Burn shock is tendable** (agent choice): tending it stands in for fluids, through the game's own `severityPerDayTended`.
- **Mortality through `deathMtbDays`** on stages, not a kill in code (agent choice): XML-visible, Search and Rescue sees the
  `lifeThreatening` stages.
- **A closed airway kills through Breathing `setMax 0`**, not a lethal severity, so Organ Failure can intercept it (agent choice).
- **More Injuries' lung-burn worker is removed** by a patch in the More Injuries folder (owner's choice, 4 Nov 2026). The planning
  pass preferred leaving it to More Injuries' own setting rather than cutting another author's feature; recorded as the
  alternative, and the README tells players which file to delete to keep both.
- **Tracheotomy gating** (agent choice): no research without More Injuries (the game has no medical research to hang it on);
  Emergency Medicine with it.

## Test protocol (owner runs it)

1. Load with Framework before Burns, both after Combat Extended, More Injuries and Organ Failure; read both load lines.
2. Set a colonist on fire with the dev tool, put it out after about ten seconds: burns, "burns, minor" or "serious", maybe
   "airway burn, hoarse"; the tooltip is words in Plain mode and figures in Operator or Engineer.
3. Dev "Apply damage", Flame 15, six times on torso and legs: burned fraction 30% or more, burn shock within one recompute,
   Search and Rescue treating it as an emergency. Tend the burns and the shock, bed rest, a day at speed 3: the shock falls; an
   untended control pawn's rises.
4. Roof a 5x5 wooden room, spawn three fires, stand a pawn in it: with CE, CE's smoke inhalation as before plus our lung damage
   and carbon monoxide; a gas mask stops the smoke effects, not the carbon monoxide. Without CE's smoke, the game's smoke and
   our acute condition instead.
5. Start an airway burn with the dev action, set its severity to 0.9: the pawn is dying of breathing; bill the tracheotomy;
   afterwards breathing recovers and a tracheostomy shows.
6. Save and reload: everything present, no errors. Press "Remove every burn condition", save, switch both mods off, load: only
   the expected missing-Def lines.
7. Report the load lines, any error naming `Phobos.BurnsAndSmoke`, and how the death clocks feel.

## Results from play

- **4 Nov 2026, 0.1.0, the owner's incendiary raid** (owner's observation, three screenshots; the mod loaded with no error
  naming it). Jansons, head burned off, dead: burns (minor). Reith, both arms, right leg and jaw burned off plus burns on head,
  neck, torso, shoulders, an eye and an ear, dead: no burns condition at all. Luise, alive, right arm burned off and burns on
  head, jaw, nose, neck, both shoulders, left arm, both legs and a foot, 15 conditions untended: burns (minor), no burn shock.
  Read against the code: the skin-share fault zeroed everything but arms, legs and feet, and burned-off parts counted
  nothing, so the worst-burned pawns showed the least; Reith's condition vanished when his limbs did. With the 0.2.0 rules
  Reith is about 45% burned with an inhalation injury and Luise about 55% with an airway burn: Baux scores near 90, the
  "major" or "critical" stage at once and life-threatening shock within half a day. On the medicine: burn shock in life is a
  matter of hours (plasma loss peaks around 8 to 12 hours; Parkland gives half the first day's fluid in the first 8), so a
  pawn an hour or two after the hit being compensated rather than collapsed is defensible; what was wrong was the size of the
  burn the mod saw. 0.2.0 is not yet tested in game.

## Open points and upstream

- Whether `Recipe_RemoveHediff` in 1.6 already applies `addsHediff`; the subclass adds it only when the removal succeeded.
- Whether to respect More Injuries' own `EnableFireInhalation` setting instead of removing its worker.
- Phase 2: constricting eschar on limbs burned past 60% of a part's health, an escharotomy surgery, ischaemic damage, a hand-off
  to More Injuries' gangrene, and a fluid-resuscitation bill that consumes its saline bag.
- Later: animals, an oxygen treatment that halves the carbon monoxide half-life, letters for the life-threatening stages.
- Upstream, for More Injuries' author: the `CE_WearingGasMask` check never matches Combat Extended's `WearingGasMask`.
