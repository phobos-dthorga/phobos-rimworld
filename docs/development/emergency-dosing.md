# Emergency dosing: research and decisions (5 Nov 2026)

**Since 7 November 2026 this mod is a part of [D'thorga Medical](../../mods/PhobosMedical/README.md)** ([why](mod-consolidation.md)); paths below are updated, the rest is as recorded.

The record behind [D'thorga Emergency Dosing](../../mods/PhobosMedical/README.md). **Partly tested in game:** the owner reports
doctors' dosing working on 0.3.0 (5 Nov 2026); self-medication and the bleeding hold are not yet tested in game.

## The problem

The owner found that emergency drugs (painkillers, anaesthetics and the like) are never given to a patient automatically, even
with Search and Rescue coordinating doctors, and asked for that and any other shortcoming in the area to be fixed. The owner's
rule for the design (5 Nov 2026): **match on what an item does, never on a named drug**, so any mod's drugs qualify, provided it
stays performant.

## The chain, as read from the code (5 Nov 2026)

Read from More Injuries 1.6.0.2's shipped source and 1.6 assembly (it ships no 1.7 build; the game falls back to its 1.6
folder), Search and Rescue's assembly of 3 Nov (decompiled into `.local`), Medicines+, Smart Medicine, MSE2 and VFE Medical, and
Core 1.7.4998; the save PG19 of 5 Nov 00:42.

- **More Injuries' drugs** (`1.6/Defs/ThingDefs/Medicine/Medicine_Drugs.xml`): morphine (MorphineBuildup 0.35: pain x0.2,
  consciousness -0.4, breathing -0.2 at that stage; a linked Vasodilation condition raises bleeding from its own code), ketamine
  and chloroform (the game's Anesthetic 1.2 plus a buildup 0.35), epinephrine (AdrenalineRush 0.75, whose benefit against
  hypovolaemic shock lives in More Injuries' code). All are `drugCategory` Medical with a vanilla `IngestionOutcomeDoer_GiveHediff`
  list, so the game's own ingestion applies them; the More Injuries job path adds a goodwill penalty and a social thought for an
  unneeded sedation and a doctor-skill curve for chloroform, which the vanilla path does not.
- **Nothing gives a painkiller or sedative automatically.** More Injuries' automatic work givers (`WorkGiver_MoreInjuriesTreatmentBase`
  subclasses, patient in bed) cover defibrillator, airway, CPR, saline, blood bag and splint. Its drugs are right-click options
  for a **drafted** doctor only (`DrugWorker`: `if (!selectedPawn.Drafted) return;`), with no condition on the patient's pain.
  Its "Provide first aid" loop uses epinephrine, never the pain drugs. Search and Rescue's doctors must be **undrafted**
  (`WorkerEligibility.WorkerControlledByScheduler` returns `!worker.Drafted`); its `MedicalIntervention` enum has no pain,
  sedation or drug entry; the one drug it plans is epinephrine during cardiac arrest, through More Injuries' `UseEpinephrine`
  job. So a Search and Rescue doctor can never reach a morphine dose. Smart Medicine stocks drugs in inventories but never
  gives one to another pawn. Medicines+'s DLL only adds its drugs to drug policies. MSE2 and VFE Medical have nothing here.
- **The game's own routes:** the generated `Administer_<drug>` bills (`RecipeDefGenerator.DrugAdministerDefs`, patient in bed,
  manual); drug policies, which the pawn follows itself and which have no pain setting (`DrugPolicyEntry`); the anaesthetic
  the game gives at the start of a surgery. Vanilla has no painkiller drug; pain is reduced only by highs, the painstopper, the
  anaesthetic and DLC effects.
- **The pieces the mod reuses:** `IngestibleProperties.outcomeDoers` and `IngestionOutcomeDoer_GiveHediff` (hediffDef,
  severity, else the condition's `initialSeverity`); `HediffStage.painFactor`, `painOffset`, `totalBleedFactor`, `capMods`
  (`PawnCapacityModifier.offset`, `setMax`), `lifeThreatening`, `deathMtbDays`; `HediffDef.lethalSeverity` and
  `StageAtSeverity`; `CompProperties_Drug.addictiveness`; `ThingDef.IsNonMedicalDrug`; `HediffSet.PainTotal` and
  `BleedRateTotal`; `Pawn.CanTakeDrug` (teetotalers and precepts); `HediffDefOf.DrugOverdose`; the game's feed-a-patient toils
  (`JobDriver_FoodFeedPatient`: `Toils_Misc.TakeItemFromInventoryToCarrier`, `Toils_Ingest.PickupIngestible`,
  `ChewIngestible(patient, ...)`, `FinalizeIngest(patient, ...)`), which apply the item's outcome doers to the patient; and
  `Hediff.TryMergeWith`, which adds a second dose's severity to the first, so doses stack.
- **The game kills at zero consciousness or breathing.** A morphine-like offset of -0.4 on a patient at 0.35 consciousness is a
  death, which is why the safety rule keeps offsets above a floor. A ceiling (`setMax`) never reaches zero and is not a cost.
- **The owner's colony (PG19):** every More Injuries research project is done; eight colonists carry epinephrine and three
  ketamine through the Combat Extended loadouts "Doctor #1" and "Military Doctor #1"; morphine stock is one unit on the whole
  map and no production bill exists for any of the four drugs; no More Injuries settings file exists, so it runs at defaults;
  all five drug policies list the four drugs, none scheduled.

## Decisions

- **A new mod, D'thorga Emergency Dosing** (agent choice): depends on D'thorga Framework only, no Harmony, no Vanilla Expanded
  Framework, so it builds on GitHub, and the medical equipment mod (VEF-bound, now D'thorga Medical Equipment) stays separate.
- **Shared with Triage** (owner's direction, 6 Nov 2026; 0.3.1): who counts as a patient, the care-level test, the severity-rate
  sum, the danger reader and the patient cache now come from D'thorga Framework's health layer, with the same answers.
- **Effects, not names** (owner's rule). `DoseIndex` walks every item `ThingDef` with an `IngestionOutcomeDoer_GiveHediff`
  once, on first use after the Defs load, reads the outcome condition's stage at the dose and the drug comp, and keeps those
  that relieve pain. An item can opt out with `PhobosDosingExtension.neverAutoDose`. Cost: one pass over Defs at load.
- **A sedative is a consciousness ceiling, not an offset** (agent choice, found by the unit tests): More Injuries' morphine
  takes 0.4 of consciousness at its dose, and a rule that read a large offset as sedation would have held morphine back for
  pain shock. The offset is a cost in the score and a floor in the safety rule; only a ceiling at or below 0.1 sedates.
- **Relief must be real** (agent choice, 0.1.1): an item is indexed only if it takes away at least 30% of the pain and scores above
  zero. Found from the owner's load line: 0.1.0 indexed 40 items, among them epinephrine (relief 0.23), which doctors carry for
  cardiac arrest and would have spent on pain, with morphine stock at one. The cut is by effect, so it needs no names. Thornweed
  serum (0.40) and morphine (0.80) stay; epinephrine, beer, cocktails and smelling salts go.
- **Tiers** (agent choice, 0.1.1): medical painkillers, then a sedative when sedation is wanted, then recreational painkillers. A
  score alone puts go-juice (0.89) and heroin (0.80) above morphine (0.50), since they cost no consciousness.
- **No stacking** (agent choice, 0.1.1): a medical painkiller still at a quarter of a dose blocks every other painkiller. A sedative is
  checked against its own conditions only, so it may follow in extreme pain.
- **Two needs** (agent choice): pain relief from 0.6, authored, inside the game's "severe" label (0.4 to 0.8,
  `HealthCardUtility.GetPainLabel`), and sedation from 0.8, where the "extreme" label starts, only when no medical painkiller can
  be given. The patient must be resting (down or in bed): a painkiller's consciousness cost could
  drop a working pawn where they stand.
- **Choice by score within a tier** (agent choice): relief minus weighted consciousness cost (not for sedatives), breathing cost, bleed
  increase (a quarter when the patient is not bleeding) and addictiveness; painkillers before sedatives; the score ignores how
  bad the pain is, so the pick is the same every time. Morphine 0.5, thornweed serum 0.4, from the Defs.
- **Rules first, reach last** (agent choice, 0.1.1): a cheap presence test and the safety rule run before any path search, so a
  refused item costs no reach check. The patient list and the index are both held in memory only; nothing goes to disk.
- **Safety** (agent choice): no second dose while the first is still at a quarter or more; none at drug overdose 0.3 or more;
  none that reaches a stage of its own condition that is life-threatening, carries a death clock or passes `lethalSeverity`;
  consciousness and breathing must stay at 0.15 or more after the dose's offsets; "no medicine" patients never; the game's
  willingness check unless the setting overrides it for medical drugs; recreational drugs only by setting.
- **Not changing Search and Rescue or More Injuries** (agent choice): a patient reserved by another pawn is skipped, so their
  doctors keep their patients. The work giver sits under Doctor at 85, between tending (100, 90) and feeding (80).
- **Circulatory support stays with More Injuries and Search and Rescue** (agent choice): epinephrine's main benefit is in More
  Injuries' code, not in a stage, and its visible pain relief is under the 30% cut.
- **Nothing is saved** (agent choice): the patient list is a static per-map cache with a tick stamp, so adding or removing the
  mod touches no save.
- **The vanilla ingestion path** (agent choice): the patient takes the item through the game's toils, which applies the item's
  outcomes as its mod wrote them for self-use. More Injuries' goodwill penalty and social thought for an unneeded sedation do
  not apply, which is right: the rule only sedates at pain shock. Its chloroform doctor-skill curve does not apply either; the
  fixed 0.35 self-use dose does.

## Fussing (D'thorga Medical 0.7.0, 7 Nov 2026)

Owner's go (released 7 Nov 2026); not yet tested in game. More Injuries' "resisting treatment" text is flavour only, so nothing in the
owner's list already does this.

- **What**: an awake patient in extreme pain (from 0.8, the game's label and our sedation threshold) is tended worse: the base tend
  quality is multiplied by 0.9 at the threshold, falling in a straight line to 0.75 at full pain (authored). Asleep, unconscious or
  anaesthetised patients don't fuss. The pain relief doctors give from 0.4 keeps most patients below it, so the two parts reinforce
  each other.
- **How**: the dosing part's first Harmony patch, a postfix on `TendUtility.CalculateBaseTendQuality(Pawn, Pawn, float, float)`, the
  overload every tend goes through (the ThingDef overload calls it; read from the 1.7 decompile). The game has no extension point for
  the patient's state in tend quality. It multiplies the result and never skips the original. A setting, on by default.

## Drugs for fighting disease, and the bleeding hold (0.3.0, 5 Nov 2026)

Both approved by the owner on 5 November 2026. **Not yet tested in game.**

Read from the 1.7.4998 assembly and the active mods' Defs:

- **The race:** `HediffComp_Immunizable` and each `HediffComp_SeverityModifierBase` move a disease's severity through `SeverityChangePerDay` (tending included, through `HediffComp_TendDuration`). `ImmunityRecord.ImmunityChangePerTick` moves immunity, scaled by the `ImmunityGainSpeed` stat. `HediffDef.PossibleToDevelopImmunityNaturally` tells a disease from a condition that only has a timer.
- **What qualifies in the owner's list**, by a positive `ImmunityGainSpeed` on the stage at the dose (computed offline from the XML): Medicines+ Tyrox (+0.5) and penicillin (+0.2), both medical. The game's load line confirms those two. Vanilla Races Expanded's antibiotics (+0.1) were predicted here and are not indexed: they give their condition through that mod's own `IngestionOutcomeDoer_GiveHediff_Discriminating`, and carry a tolerance through a second custom doer, so the index does not read them. Left out for now as a limit, since their tolerance is that mod's own rule. Yuenyeung (+0.15) and rose tea (+0.02) are social drinks and do not count.
- **Traps found by the scan:** Biological Warfare's malaria and sleeping sickness pills give the disease. Luciferium and red-flicker raise blood filtration, which feeds immunity, and are highly addictive. Penoxycyline and the vaccines prevent a disease and do not help someone who has it.

Decisions:

- **Doctors only** (owner's choice): a pawn cannot judge a race between an illness and their immunity.
- **By effect** (owner's rule): a positive immunity stat on a medical drug, at least 0.1 (authored), its costs below its gain, and none of its outcomes a disease.
- **Only when losing** (agent recommendation, approved): the disease can kill, and immunity would arrive too late or within a day of the lethal mark (authored margin). It reads current rates, so better care can turn the answer round. A patient already winning gets nothing.
- **Pain first** (agent choice): a patient with both needs gets pain relief first. If that is held back or nothing is in reach, the disease drug still comes.
- **Any disease drug still working blocks the next** (agent choice): the same rule as medical painkillers.
- **The bleeding hold** (agent recommendation, approved): at 0.3 a day or more (authored; the health tab shows it as 30%), a painkiller waits. It exists because More Injuries' morphine raises bleeding through a linked condition the index cannot see, and one doctor's priority does not stop a second doctor reaching the patient first. A sedative is exempt: a patient past the pain-shock band still needs relief, and its anaesthetic has no such link. A disease drug is not a painkiller. It applies to self-medication too.

Test steps (owner runs them):

1. The load line lists the drugs for fighting disease: penicillin and Tyrox on the owner's list, as seen in play. Not tea, not malaria pills.
2. A patient in bed with a disease they are losing to (dev: add plague at high severity), Tyrox in reach: a doctor gives it. The dev action "Dosing: log the decision for selected pawns as patients" shows losing yes and need Immunity. At low severity with immunity well ahead: no dose.
3. A patient at 60% pain or more bleeding at 90% a day, with morphine in reach: no morphine, and with a doctor selected a right-click says the bleeding must be under control first. Tend the bleeding: the morphine follows. At 80% pain with ketamine in reach: ketamine comes at once.

## Self-medication (0.2.0, 5 Nov 2026)

The owner's idea: someone above a pain threshold would seek out a painkiller on their own. It answers the earlier open point that a colonist
walking in severe pain got nothing. **Not yet tested in game.**

Read from the 1.7.4998 assembly and Core Defs:

- **Nothing in the game takes a drug because of pain.** The only behaviour reading `PainTotal` is in-bed meditation (`JobGiver_Meditate`).
  Pain's mood bands (`ThoughtWorker_Pain`) change at 0.15, 0.4 and 0.8; pain shock defaults to 0.8. So this is new behaviour, not a repair.
- **The game's own self-dosing** (`JobGiver_TakeDrugsForDrugPolicy`, `JobGiver_SatisfyChemicalNeed`, `JoyGiver_TakeDrug`) finds the drug itself,
  in the pawn's inventory then with `GenClosest.ClosestThingReachable`, and returns `DrugAIUtility.IngestAndTakeToInventoryJob(thing, pawn, 1)`. The stock
  `JobDriver_Ingest` does the rest. A `DrugDesire` degree above 0 skips the policy's joy and addiction lines; `IsTeetotaler` and `CanTakeDrug` refuse.
- **`DrugPolicy`'s indexer is for drugs only** and `CurrentPolicy` can be null, so the code asks only about drugs. **Guests have no policy at all:**
  `PawnComponentsUtility` creates `Pawn_DrugPolicyTracker` only for the player's own faction. `IngestAndTakeToInventoryJob` copes with that.
- **Hook:** `Humanlike.xml`'s `Humanlike_PreMain`, a `ThinkNode_SubtreesByTag`; Smart Medicine adds its stock-up giver the same way at another tag.

Decisions:

- **The drug policy is the gate** (owner's choice): an item qualifies when its line allows joy or a schedule. No new UI, and the player keeps control.
  The scheduled line's frequency and mood gates are not applied, because pain is the reason here rather than the schedule (agent choice).
- **The threshold** (owner's choice): 40%. Chemical interest 30% and fascination 20% (authored).
- **Who** (owner's choice): free colonists by default; a setting adds guests, for Hospitality players. Adults only, and not slaves or prisoners (agent choice).
- **Guests, who have no policy to gate by** (agent choice, found while building, for the owner to overrule): medical painkillers only, never recreational. If a mod gives
  a guest a policy, that policy applies instead; whether Hospitality does has not been checked.
- **The trait bypass** (agent choice, by vanilla precedence): chemical interest and fascination ignore the policy, as in the game's joy givers.
- **Left to the doctors** (agent choice): anyone in bed or under `HealthAIUtility.ShouldSeekMedicalRest`, which includes a tended wound still healing.
- **The same safety and choice as doctors** (agent choice): `Evaluate` and `DoseChoiceRule.Choose`, with need always pain relief, so sedatives are out and medical
  drugs come before recreational. Unlike the doctor path, a recreational painkiller still working also blocks the next, or highs would stack.
- **A higher floor on one's feet** (agent choice): consciousness and breathing must stay at 0.4 or more after a self-taken dose, against 0.15 for a doctor's. The game drops
  a pawn below 0.3 consciousness (`PawnCapacitiesHandler.CanBeAwake`), and more of the doctors' patients are already down. Morphine's cost of 0.4 means it needs 0.8 consciousness.
- **The game's ingest job** (agent choice): no new JobDef, so the item's effects, the chew spot and the policy's take record behave as usual.
- **Cost** (agent choice): the giver runs when a humanlike chooses a job, not per tick. Who-and-how-much-pain are checked first; the index and any path search
  only follow for a pawn who passed them.

Test steps (owner runs them):

1. The load line says self-medication is on and whether guests may. No think-tree error at load.
2. A colonist up and about at 40% pain or more, thornweed serum in a stockpile, allowed for joy in their policy: they go and take it. Joy tick off and no
   chemical trait: nothing; the dev action "Dosing: log self-medication for selected pawns" says `PolicyForbids`.
3. With morphine allowed too: morphine is chosen. With only a recreational drug allowed: that one.
4. Chemical interest at 32% pain takes one. A teetotaler at 70% does not. A pawn under a working dose does not. Nobody falls unconscious from a dose they took themself.
5. In a hospital bed: left to the doctors. Drafted: nothing until undrafted.
6. Guests setting off: a guest in pain takes nothing. On: they take a medical painkiller from your stock, never a recreational one.

## Upstream faults seen in the owner's log of 5 Nov 2026 (for the owner to report)

Both are other authors' interplay, not ours; drafts the owner can post.

- **Search and Rescue, Paramedic mechs get More Injuries jobs.** `WorkGiver_MechFieldRescueDoctor.NonScanJob` issues
  `JobDriver_UseSalineBag` (and other `JobDriver_UseMedicalDevice` jobs) to Paramedic mechs through
  `Compatibility.TryCreateMoreInjuriesDispatcherJob`. More Injuries' `ExtendedJobParameters.Create` needs the worker's
  `MoreInjuryComp`, which only Human and Humanoid Alien Races flesh races carry, so each attempt logs "[MoreInjuries] ERROR:
  JobDriver_UseMedicalDevice.cs::L347: Failed to get MoreInjuryComp from Mech_Paramedic..." (50 times in one session, 5 Nov
  2026, RimWorld 1.7.4998, Search and Rescue of 3 Nov, More Injuries 1.6.0.2). Suggested fix: skip More Injuries device jobs
  for workers without the comp, or let the mech use the vanilla tend only.
- **More Injuries, tourniquet on a missing part.** `JobDriver_UseTourniquet.ApplyDevice(Pawn patient, Thing device, string
  bodyPartKey)` adds the tourniquet condition to a part that no longer exists (here a shoulder, plausibly burned off), and the
  game logs "Tried to add health diff to missing part BodyPartRecord(Shoulder ...)" (7 times, same session). Suggested fix:
  re-check `hediffSet.PartIsMissing(part)` at apply time, since the part can be lost between planning and applying.

## Test protocol (owner runs it)

1. Launch: the load line lists **medical pain relief** and **recreational pain relief** apart. Expected medical on the owner's list
   (computed offline from the active mods' XML, not from the game): morphine, thornweed serum, ketamine and chloroform as
   sedatives. **Not** epinephrine, luciferium or smelling salts. Recreational (go-juice, psycho, heroin, ambrandy, opium, yayo,
   upper, flake and similar) is marked never given while the setting is off. No error naming `Phobos.Dosing`. Dev action "Dosing:
   log the pain-relief index" shows each item's figures.
2. Dev "Apply damage" on a colonist until pain is at 60% or more, lay them in a hospital bed: a doctor with morphine in
   inventory, or with morphine in a stockpile, gives it within 250 ticks; the health tab shows the morphine high; no second
   dose while it lasts. Dev action "Dosing: log the decision for selected pawns as patients" shows the ranked table.
3. Pain past 80% with no morphine or thornweed on the map: ketamine or chloroform is given; with morphine present, morphine. With
   the recreational setting on and no medical drug or sedative on the map, a recreational one; with any medical option, never.
4. A "no medicine" patient, and a teetotaler with the override off, get nothing; with a doctor selected, a right-click on the
   patient says why.
5. Search and Rescue treating a bleeding patient: our doctor does not interrupt; once tending is done and pain remains, the
   dose follows.
6. Save and reload: no new saved state, no errors. Switch the mod off and load: no missing-class line.
7. Report the load line, any error naming `Phobos.Dosing`, and whether the picks feel right.

## Results from play

- **5 Nov 2026, 0.3.0, the owner's report: "It's working"** (owner's observation, one screenshot; the log shows 0.3.0, Burns and Smoke 0.4.2 and Framework 0.2.2 loaded with
  no error naming them). A dead patient, Boss, carries a ketamine buildup at its therapeutic dose, anaesthetic wearing off and penicillin with 17 hours left, beside burn shock
  (compensated) and burns (serious) from a laser. So penicillin was given about 7 hours before death, by the game's 24-hour duration at that dose (our arithmetic), though
  whether an earlier dose came first is not known. She had two extreme wound infections, one at 100% with immunity at 99.083%: the race was lost by under one point. Which
  infection or condition killed her is not shown. Not shown by this: self-medication, the bleeding hold, or who gave the ketamine. The load line lists penicillin and Tyrox as
  drugs for fighting disease, not the antibiotics first predicted from the XML; the section above now says why.
- **5 Nov 2026, 0.1.0 loaded in the owner's game** (read from Player.log; not a declared test, and it says nothing about a dose being
  given). No error naming the mod. Its load line listed 40 items, which exposed the epinephrine, tier and load-line faults
  fixed in 0.1.1. The owner's setting "use recreational drugs for pain" was on in the run of 02:06 and off in the run of 02:04.

## Open points and upstream

- Whether the one-day margin in the immunity race is too tight. One patient lost by under one point of immunity; a dose a day earlier adds about 13 points at penicillin's rate against
  wound infection (0.6441 a day times 0.2; our arithmetic). One case is not enough to change it. For the owner, after more play.
- Vaccines and other prevention for the healthy: a separate decision about giving doses to people who are not ill. For the owner.
- Telling bacterial from viral disease, so antibiotics help only against the first. It would mean labelling each disease by hand. Set aside for now.
- **Settled by the owner, 7 Nov 2026 (D'thorga Medical 0.7.0):** doctors give pain relief from 40% (was 60%), the immunity margin is
  two days (was one), and anyone for whom the game nullifies the pain thought (masochists, idealised pain) skips self-medication.
  Vaccines and prevention get a design record of their own. The points below are kept as they were raised.
- A patient in bed between 40% and 60% pain gets nothing: too high for comfort, below the doctors' band, and left alone by
  self-medication. Whether doctors should come down to 40% is for the owner, after play.
- Whether Masochists should skip self-medication: the game nullifies their pain thought, and this version does not exempt them.
- Stronger pain could prefer stronger drugs (the score ignores the pain level by design); revisit after play.
- A dressing or fluids kit for burns belongs to D'thorga Medical Equipment, which already gives oxygen and air-cell burn care;
  not here.
- The two upstream faults above, for the owner to post.
