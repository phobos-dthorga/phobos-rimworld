# Burns and smoke: research and decisions (4 Nov 2026)

The record behind [D'thorga Burns and Smoke](../../mods/PhobosBurnsAndSmoke/README.md). **0.3.0 not yet tested in game**
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
- **Tend quality** (`TendUtility.CalculateBaseTendQuality`, 1.7 decompile): the doctor's Medical Tend Quality stat times the
  medicine's potency (none 0.3, herbal 0.6, industrial 1, glitterworld 1.6), plus the bed's offset, times 0.7 for self-tending,
  clamped to the medicine's quality ceiling (none and herbal 0.7, industrial 1, glitterworld 1.3). `HediffComp_TendDuration`
  keeps it in a public `tendQuality` while `IsTended`, and scales `severityPerDayTended` by it.
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
  head and shoulders came to 0 and the outside parts summed to 0.35. Fixed in 0.2.0.)
- **A destroyed part** gets a `Hediff_MissingPart` whose `lastInjury` is the injury Def that destroyed it (`Burn` for fire, hence
  the "burned off" label), and every part under it gets its own non-fresh record with the same `lastInjury`. The stump is fresh
  until tended or 90,000 ticks (1.5 days) old. A fresh stump on a non-solid part bleeds at the part's health times the
  `MissingBodyPart` Def's 0.12, times the part and race bleed factors, **whatever destroyed it**: a burn amputation bleeds, and the
  owner's screenshot shows it (Luise, right arm burned off, 90% a day). `Find.TickManager` is `Current.Game.tickManager` and throws before a game exists.

## Sources

- Rule of nines and total body surface area; the Parkland formula, 4 mL per kg per percent burned over 24 hours
  ([StatPearls](https://www.statpearls.com/point-of-care/158070); [Wikipedia, Parkland formula](https://en.wikipedia.org/wiki/Parkland_formula)).
- The revised Baux score, age plus percent burned plus 17 with an inhalation injury (Osler and colleagues, 2010). Our marks of 36
  and 110 come from one single-centre cohort, 2014 to 2020, Al Khobar, which reports no deaths below 36 and no survivors at 110 or
  above, though its own table lists a death in the 30 to 39 band and 40% mortality at 100 to 109
  ([PMC9555034](https://pmc.ncbi.nlm.nih.gov/articles/PMC9555034)). A regional specialist burns service reports 50% mortality at a
  Baux score of 109.6 and predicted 100% at 160 (Roberts and colleagues, 2012, abstract only,
  [record](https://researchexperts.utmb.edu/en/publications/the-baux-score-is-dead-long-live-the-baux-score-a-27-year-retrosp/)).
  So 110 as "terminal" is harsher than burn-centre outcomes: an authored choice, defensible only because the Rim has no burn
  unit. The score's accuracy is supported by [PMC6199008](https://pmc.ncbi.nlm.nih.gov/articles/PMC6199008), which does not state
  these marks; it was wrongly cited for them until 0.2.1.
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
  `coverageAbsWithChildren` (a lost part is wholly burned, whatever kind of burn took it), parts under another missing part skipped. The stump
  counts whole while fresh, then fades linearly to nothing over `burnedOffHealDays` (15, authored: a grafted or granulating
  stump takes weeks) by the record's age; a tended stump counts as tended area. A late tend drops the weight at once, a
  labelled simplification. Burn amputations keep the game's rule and bleed until tended or 1.5 days old. A stump past 1.5 days
  counts as tended even if nobody tended it, because the game cannot tell the two apart (labelled simplification).
- **Scars are not burns** (agent choice, 0.2.1): `Load` and `HasBurn` skip permanent injuries. Healed skin neither leaks fluid nor
  loses heat, and counting scars would hold a healed pawn in burn shock for life.
- **The compensated stage always shows past the threshold** (agent choice, 0.2.0): the shock target is at least
  `shockThresholdFloor` (0.1, authored) once 15% is burned, so a young pawn below the Baux no-death mark still shows the
  fluids job; the death clocks stay Baux-driven.
- **Area and depth are two things** (owner's choice, 4 Nov 2026: an armour-piercing incendiary round is a small deep burn, a
  fire a wide shallow one, and one number cannot say both). Depth stays the game's severity. Area is authored per burn kind
  in `PhobosBurnLikeExtension`: fraction of the part per point of damage and the most one burn can cover. Fire, chemical and
  acid: 1/12 and 1, so ten seconds on fire (four vanilla hits) is about a fifth of the body. Electrical and Combat Extended's
  `BurnSecondary`: 1/40 and 0.3. The earlier rule read severity against the part's full health, which made a shallow burn
  across a leg a sliver and a 40-point burn the only way to a whole torso.
- **Treatment scales with medicine** (owner's choice, 4 Nov 2026: playable over exact, treatable without a burn ward,
  industrial medicine to a moderate degree over a long rest, glitterworld more). The fluids term of the treatment factor is
  the shock condition's own tend quality times 0.35, capped at the game's best (1.3); tended burns 0.2, bed rest 0.15, floor
  0.1. With full care: no medicine leaves 55% of the untreated target, herbal 44%, industrial 30%, glitterworld 20%. So a
  score of 110 or more lands at "shock" with industrial care (life-threatening, no clock) and "compensated" with glitterworld.
  The 110 mark stays "terminal" untreated, labelled authored (owner, 4 Nov 2026, by way of this decision).
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

- **4 Nov 2026, 0.1.0, the owner's incendiary raid** (owner's observation, three screenshots). Jansons, head burned off, dead:
  burns (minor). Reith, both arms, right leg and jaw burned off plus burns on head, neck, torso, shoulders, an eye and an ear,
  dead: no burns condition at all. Luise, alive, right arm burned off and burns on head, jaw, nose, neck, both shoulders, left
  arm, both legs and a foot, 15 conditions needing tending, bleeding 90% a day: burns (minor), no burn shock. Read against the
  code: the skin-share fault zeroed everything but arms, legs and feet, and burned-off parts counted nothing.
- **Those three pawns cannot be re-measured.** That run was not kept: in PG19 Reith and Luise are dead with no burns at all
  (our inference: the colony was reloaded from before the raid). From the screenshots alone, Reith's lost parts give at least
  33% under the 0.2.0 rules and Luise's at least 9%, plus partial burns of unknown severity; her one visible burn, 3 on a 20-health
  jaw, counts as 15% of the jaw. An earlier version of this entry said about 45% and 55% and Baux scores near 90. Those
  estimates assumed every burned part was wholly burned, and are withdrawn.
- **What the save PG19 shows (4 Nov 2026, 0.2.0, read from the file; not a declared test).** Three pawns died carrying the
  conditions: Tergel Leman, 36, airway burn, burned 39%, shock 0.76, exactly the rule's target for a score of 92; Virginie
  Arthuring, 48, airway burn, burned 45%, shock 0.21 and still rising toward 0.99; JoongHoon Alitis, burned 6%, no shock. Recomputing
  every human corpse from its injuries gave 17 of them at 15% or more under the 0.2.0 rules and none above 0.7% under the 0.1.0
  rules. Corpses keep burning, so those recomputed figures overstate what the living pawn had. No living pawn in the save has
  a burn or a burn scar, which is why the scar fault had not yet shown.
- **Time course, unverified.** A search found secondary sources saying capillary leak starts within minutes and runs for about 24
  hours; an "8 to 12 hour peak" given earlier in this entry had no source and is withdrawn. 0.3.0 is not yet tested in game.

## Open points and upstream

- Supporting gear for burn care without a burn ward (owner's wish, 4 Nov 2026): a candidate for the planned medical equipment
  mod (`art-automation-and-medical-equipment.md`), for instance a dressing or a fluids kit that raises the fluids quality, once that mod
  has its first piece.
- `Recipe_RemoveHediff` does not apply `addsHediff` on 1.7 (read from the 1.7 assembly, 5 Nov 2026), so the subclass is needed. The 1.6 reference assembly has no method bodies to read; the subclass checks before adding, so a difference there could not double it.
- Whether to respect More Injuries' own `EnableFireInhalation` setting instead of removing its worker.
- Phase 2: constricting eschar on limbs burned past 60% of a part's health, an escharotomy surgery, ischaemic damage, a hand-off
  to More Injuries' gangrene, and a fluid-resuscitation bill that consumes its saline bag.
- Later: animals, an oxygen treatment that halves the carbon monoxide half-life, letters for the life-threatening stages.
- Upstream, for More Injuries' author: the `CE_WearingGasMask` check never matches Combat Extended's `WearingGasMask`.
