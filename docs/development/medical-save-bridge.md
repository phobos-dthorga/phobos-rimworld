# Medical save bridge: research and decisions (3 Nov 2026)

The record behind [D'thorga Medical Save Bridge](../../mods/PhobosMedicalSaveBridge/README.md). **The first load was tested in game
by the owner on 3 November 2026** (results at the end); the save-and-reload step and 0.1.1's change are not. The
player guide is the mod's README; this holds the code readings, the evidence, the sources and who chose what. It started from
a handoff the owner brought from a claude.ai investigation of the PG17 colony's logs and saves; what that handoff marked as a
hypothesis was checked here before anything was built.

## The failure

The owner added More Injuries (Continued) by Th3Fr3d (Workshop 3348840185, version 1.6.0.2, GPL-3.0, source on
[GitHub](https://github.com/frederik-hoeft/rimworld-more-injuries)) to PG17, a RimWorld 1.7.4995 colony running Medical System
Expansion 2 by JonM0 (Workshop 2056706586, installed 4.13.1, source on [GitHub](https://github.com/JonM0/MedicalSystemExpansion2))
and Combat Extended. The UI vanished. Counts from the preserved log of that session (kept beside the owner's Player.log as
MI-MSE2-broken-session-2026-11-03.log), and from the next session with More Injuries off:

| Log pattern | With More Injuries | Without |
| --- | ---: | ---: |
| MSE2 "Reinstalling Hediffs with wrong type" | 339 | 0 |
| "Tried to add health diff to missing part" | 762 | 0 |
| More Injuries "Compatibility patch removed some hediff(s)" | 287 | 0 |
| More Injuries "died due incorrect hediff placement ... Fully resetting pawn" | 105 | 0 |
| "Root level exception in OnGUI" | 1,401 | 0 |
| "Exception drawing ThingOverlay" | 5,606 | 0 |

Pre-existing noise in both sessions, not this fault: 3 "Tried to register the same load ID twice ... /codedPawn", 28 "Exception
registering" from CE and MVCF turret verbs, 3 "Error recaching pawn traits", and CE exosuit ammo-backpack cross-reference errors.

## The chain, as read from the code

Everything below was read from decompiled assemblies on 3 November 2026 (RimWorld 1.7.4995's Assembly-CSharp, the Workshop
builds of both mods), kept in the ignored `.local/decompiled/`. **Verified** means the code says so; **inference** is ours.

1. **A body part is saved as its index in the body's part list** (verified, 1.7). `Scribe_BodyParts.Look` writes the body's defName
   and `part.Index`; `BodyPartRecord.Index` is `body.GetIndexOfPart(this)`; `BodyDef.CacheDataRecursive` fills that list depth-first
   from the core part. Loading goes through `ScribeExtractor.BodyPartFromNode`, which calls
   `BackCompatibility.GetBackCompatibleBodyPartIndex(body, index)` and then `body.GetPartAtIndex`. `Hediff.ExposeData`,
   `Bill_Medical.ExposeData`, `Scribe_Collections` and MSE2's `Scribe_LimbConfiguration` all use `Scribe_BodyParts`.
2. **More Injuries appends eight parts to the Human body** (verified, `Patches/Patch_BodyParts.xml`): Abdomen with SmallIntestine and
   LargeIntestine under the torso, FemoralArtery and PoplitealArtery under each leg, SpinalCord under the spine. All six part defs are
   More Injuries' own; every operation is a `PatchOperationAdd`, so no existing part moves. Its CE patch changes coverages only.
   So every old part after the abdomen's insertion point is off by three, after the left leg's arteries by five, after the right leg's
   by seven, and after the spinal cord by eight. The log's "MissingBodyPart left femoral artery ticksSinceCreation=25459429", a missing
   part far older than More Injuries' presence, is that shift seen from the other side.
3. **MSE2 re-installs hediffs whose class differs from their def's** (verified, `MSE2.BackCompatibility.HediffClassLoading`, a
   postfix on `HediffSet.ExposeData` in ResolvingCrossRefs mode). More Injuries' `Patch_Core.xml` makes every injury def's class
   `BetterInjury` and every missing-part def's `BetterMissingPart`, both subclasses of the vanilla ones. Old saves hold the vanilla
   class names, so MSE2 removes each such hediff and re-adds a fresh one through `HediffSet.AddDirect`, which refuses a part that is
   already missing and logs "Tried to add health diff to missing part". Every child of a lost limb trips it. This class loading is not
   in MSE2's GitHub repository; it is in the Workshop build.
4. **More Injuries wipes pawns on its first load** (verified, `MoreInjuryComp.PostExposeData` and `Patches/Patch_Pawn_ExposeData`).
   The comp finds no saved `jobParameters` the first time it meets a save and sets `FailedLoading`; the postfix on `Pawn.ExposeData`
   then, in PostLoadInit, removes every `Hediff_Injury` and `Hediff_MissingPart`, clears capacities, surgery bills and immunity, and
   runs `FixMisplacedBionicsModExtension.FixPawn`, which moves an added part whose part def is not in its recipe's
   `appliedOnFixedBodyParts` to a random allowed part, or removes it. The source comment says the target part "may be incorrect":
   the author knows about step 2.
5. **The wipe revives the dead** (verified in code, confirmed in the log). The same postfix has `if (__instance.Dead)` set the health
   state back to Mobile, logging "died due incorrect hediff placement". It cannot tell a pawn killed by the shift from one that was
   dead already, so 105 corpses in PG17, Red Kangaroo and Black Skunk among them, became living pawns with no mind state, needs
   or guest state. `Pawn.InAggroMentalState` reads `mindState` and `Hediff_Addiction.Need` reads `pawn.needs` once `Dead` is false,
   which is the `NullReferenceException` under the colonist bar and the wealth recount.
6. **The four named colonists were a red herring** (inference from the stacks). The handoff named Noxa, Caelae, Stakius and
   Damianus as "half-loaded". The ThingOverlay errors name them because their labels were being drawn; the exception comes from
   `PawnsFinder.AllMapsCaravansAndTravellingTransporters_Alive_FreeColonists`, which walks every pawn and met a revived corpse.
   In the clean session those four appear only in AllowTool's and the bed-reservation lines, together, as neighbours in a barracks.

## Decisions

- **Hook the game's own conversion points, not the mods** (agent choice). `BackCompatibility.GetBackCompatibleBodyPartIndex` and
  `BackCompatibility.GetBackCompatibleType` are what Ludeon's own version conversions use for exactly these two jobs; a postfix on
  each covers every saved part reference and every saved hediff in one place. Neither mod's assembly is referenced; More Injuries'
  comp is found by name. MSE2 is never patched.
- **Upgrade old wounds rather than skip MSE2's re-install** (agent choice; the handoff's option C2 over C1). Creating the hediff as its
  def's class from the start keeps severity, part, age and tending, and MSE2 then finds nothing to re-install, so the bridge is inert
  after one save. Skipping the re-install would have had to stay active for every later load until the last old wound healed, and a
  missing part never does.
- **Write the bleeding state a live wound carries** (agent choice). `BetterInjuryState.ExposeData` loads `overriddenBleedRate` with a
  default of 0, which would have made every carried-over wound stop bleeding; a live wound saves 1. The bridge appends
  `overriddenBleedRate` 1, `temporarilyTamponadedMultiplierBase` 1 and `reducedBleedRateTicksRemaining` 0 to the saved node before it
  loads, so an upgraded wound is byte-for-byte what More Injuries would have saved. These are save-file field names, not copied code.
- **Decide "new" from the save's mod list, generically** (agent choice). The gate is the set of running package IDs absent from the
  header's `modIds`, compared lower-cased as the game does. A part is new when its def's `modContentPack` is one of them. The
  bridge therefore also helps with any other part-adding mod added mid-save, and never touches a save that already lists the
  mod. A newly added mod inserting records of vanilla part defs would not be seen.
- **Upgrade a class whichever mod it belongs to** (agent choice, 0.1.1, from the first in-game load). 0.1.0 upgraded a saved hediff
  only when its def's class came from a newly added mod's assembly. More Injuries also re-classes the vanilla `BloodLoss` and
  `Hypothermia` defs to `Verse.HediffWithComps` (`Patch_BloodLoss.xml`, `Patch_Hypothermia.xml`) to carry its comps, so those fell
  through to MSE2, whose re-install calls `HediffMaker.MakeHediff(def, pawn, part)`: a fresh hediff at initial severity, the old
  one's severity and age gone (verified, `HediffClassLoading.ReinstallWrongClassed`). The rule is now: upgrade when the def's class
  is a subclass of the saved one with a parameterless constructor. `HediffWithComps.ExposeData` builds its comps from the def in
  LoadingVars, so an upgraded blood loss carries More Injuries' comps at their defaults (verified, 1.7).
- **Hold the wipe by clearing the flag, not by skipping More Injuries' method** (agent choice; the handoff's option B, first form).
  Clearing `FailedLoading` as it is set lets More Injuries' own postfix run and find nothing to do, so its author's later changes to that
  method still apply.
- **Report, never fix, misplaced added parts** (owner's handoff). After the load the bridge lists added parts off their recipes' parts,
  which is what `FixPawn` would have moved or removed; with the re-numbering right the list should be empty.
- **House identity** (agent default, owner to confirm): `PhobosDthorga.MedicalSaveBridge`, "D'thorga Medical Save Bridge", in this
  repository under `mods/` and `src/` like the other mods, instead of the handoff's suggested local folder and ID. Deployed through
  `scripts/deploy.py` like the rest.

## Test protocol (owner runs it)

**Never load, modify or overwrite PG17.rws.** Copy it to PG17-bridgetest.rws in the Saves folder and work on copies.

Mods: More Injuries on, the bridge after More Injuries and MSE2, everything else as it is. The handoff said to keep Organ Failure
and Medieval Solutions off; the owner had them on and it was fine (results below).
Deploy with the game closed: `python scripts/deploy.py --apply PhobosMedicalSaveBridge`.

1. Before switching anything on, open clean PG17 and screenshot the health tabs of at least three colonists with lost limbs or
   bionics, and of one with MSE2 modules.
2. Switch on More Injuries and the bridge, load PG17-bridgetest, and read the log. Targets, from the owner's Player.log:

| Log pattern | Target |
| --- | ---: |
| [D'thorga Medical Save Bridge] ACTIVE ... Human (8 new parts) | 1 line |
| [D'thorga Medical Save Bridge] Hediffs sitting on parts newer than the save: none | present |
| [MSE2] Reinstalling Hediffs with wrong type | 0 |
| Tried to add health diff to missing part | 0 |
| Compatibility patch removed some hediff(s) | 0 |
| died due incorrect hediff placement | 0 |
| Root level exception in OnGUI | 0 |
| Exception drawing ThingOverlay | 0 |
| The pre-existing noise above | unchanged |

3. Compare the health tabs with the screenshots: same parts missing, same prosthetics and bionics in the same slots, MSE2
   modules intact. Check Noxa, Caelae, Stakius and Damianus too, for completeness.
4. Damage a pawn in dev mode, save as PG17-bridgetest2, reload: the bridge logs INERT and none of the patterns above appear.
5. New colony with More Injuries, MSE2 and the bridge: injure pawns, amputate a finger and a limb in dev mode, save, reload.
   Expect INERT and none of the patterns.

A count in Git Bash:

```bash
L="/c/Users/phobo/AppData/LocalLow/Ludeon Studios/RimWorld by Ludeon Studios/Logs/Player.log"
for p in 'Medical Save Bridge' '\[MSE2\] Reinstalling Hediffs' 'Tried to add health diff to missing part' 'Compatibility patch removed some hediff' 'died due incorrect hediff' 'Root level exception in OnGUI' 'Exception drawing ThingOverlay'; do printf '%6s  %s\n' "$(grep -c "$p" "$L")" "$p"; done
```

## Results from play (3 Nov 2026, owner's first load)

The owner loaded a copy of the live colony (Autosave-11, written 42 minutes after PG17 without More Injuries) with More Injuries,
More Injuries: Organ Failure, Medieval Solutions for More Injuries and No More Lethal Damage Threshold all switched on in the same
step, the bridge 0.1.0 after them. Neither add-on adds body parts: the gate line named Human (8 new parts) only. The owner reported
the colony playing well from the start; More Injuries' own migration dialogs (1.5.2 research choice, 1.5.2.1 notes) appeared and
its surgeries listed. Counts from the owner's Player.log against the control run (Player-prev.log, PG17 without More Injuries):

| Log pattern | Bridge run | Control |
| --- | ---: | ---: |
| Bridge ACTIVE ... Human (8 new parts) | 2 lines (the header is read once by the load dialog's check and once by the load; harmless) | n/a |
| Body parts re-numbered on Human | 3,811 | n/a |
| Hediff classes upgraded | 1,687 injuries, 1,071 missing parts | n/a |
| More Injuries first-load wipes prevented | 517 | n/a |
| Hediffs sitting on parts newer than the save | none | n/a |
| Added parts off their recipes' parts | none | n/a |
| Tried to add health diff to missing part | 0 | 0 |
| Compatibility patch removed some hediff(s) | 0 | 0 |
| died due incorrect hediff placement | 0 | 0 |
| Root level exception in OnGUI | 0 | 0 |
| Exception drawing ThingOverlay | 0 | 0 |
| MSE2 Reinstalling Hediffs with wrong type | 87 (79 BloodLoss, 8 Hypothermia; see the 0.1.1 decision) | 0 |
| ERROR lines | 116 | 134 |

The error lists of the two runs match group for group (CE and MVCF turret verbs, exosuit ammo backpacks, Enhanced Beliefs rungs,
the Humvee upgrade tree). New in the bridge run and not ours: four NullReferenceExceptions from Allow Tool's party-hunt handler
reading its world settings while two mechs' paths failed during map initialisation, and Vanilla Memes Expanded's obelisk tracker
dictionary dropping 11 null keys against 1 (a different save; its keys are pawns that no longer exist). Harmony on 1.7 accepted
`object __instance` for More Injuries' comp: the wipe patch applied and counted. Still to run: step 4 (save, reload, INERT) and
step 5 (new colony).

## Open points and upstream

- Steps 2, 4 and 5 are faults every More Injuries user meets when adding it mid-save, MSE2 or not. Worth a GitHub issue on
  frederik-hoeft/rimworld-more-injuries once the bridge is tested: re-number parts with `GetBackCompatibleBodyPartIndex`
  instead of wiping, and never reset a pawn that was already dead. For MSE2: `ReinstallWrongClassed` should not go through
  `AddDirect` on parts under a missing one, and should carry the old hediff's severity and age instead of re-making it at initial
  severity. Both wait for the owner's go-ahead.
- More Injuries' add-ons (Organ Failure, Medieval Solutions) were not read. Neither adds parts, and the owner's load with both on
  was clean, but their own first-load behaviour is still unread.
