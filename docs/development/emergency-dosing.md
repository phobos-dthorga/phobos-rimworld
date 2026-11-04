# Emergency dosing: research and decisions (5 Nov 2026)

The record behind [D'thorga Emergency Dosing](../../mods/PhobosDosing/README.md). **Not yet tested in game.**

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
  Framework, so it builds on GitHub, and the planned medical equipment mod (VEF-bound) stays separate.
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
   dose while it lasts. Dev action "Dosing: log the decision" shows the ranked table.
3. Pain past 80% with no morphine or thornweed on the map: ketamine or chloroform is given; with morphine present, morphine. With
   the recreational setting on and no medical drug or sedative on the map, a recreational one; with any medical option, never.
4. A "no medicine" patient, and a teetotaler with the override off, get nothing; with a doctor selected, a right-click on the
   patient says why.
5. Search and Rescue treating a bleeding patient: our doctor does not interrupt; once tending is done and pain remains, the
   dose follows.
6. Save and reload: no new saved state, no errors. Switch the mod off and load: no missing-class line.
7. Report the load line, any error naming `Phobos.Dosing`, and whether the picks feel right.

## Results from play

- **5 Nov 2026, 0.1.0 loaded in the owner's game** (read from Player.log; not a declared test, and it says nothing about a dose being
  given). No error naming the mod. Its load line listed 40 items, which exposed the epinephrine, tier and load-line faults
  fixed in 0.1.1. The owner's setting "use recreational drugs for pain" was on in the run of 02:06 and off in the run of 02:04.

## Open points and upstream

- Whether to hold a painkiller back while the patient is bleeding hard. More Injuries warns that morphine is hazardous in
  uncontrolled bleeding and its bleed increase is invisible to the index. Priority puts tending first for one doctor, but a second
  doctor can dose a bleeding patient the first has not reached. For the owner.
- Whether the "resting only" rule is too strict: a colonist walking in severe pain gets nothing until they lie down.
- Stronger pain could prefer stronger drugs (the score ignores the pain level by design); revisit after play.
- A dressing or fluids kit for burns and an oxygen treatment belong to the planned medical equipment mod
  (`art-automation-and-medical-equipment.md`), not here.
- The two upstream faults above, for the owner to post.
