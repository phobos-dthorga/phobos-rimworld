# Triage: research and decisions (6 November 2026)

**Since 7 November 2026 this mod is a part of [D'thorga Medical](../../mods/PhobosMedical/README.md)** ([why](mod-consolidation.md)); paths below are updated, the rest is as recorded.

D'thorga Triage (`mods/PhobosMedical`, `src/PhobosMedical/Triage`): doctors queue the life-saving operations a patient needs, and take
the worst case first. This record holds what was read, who chose what, and the test protocol. The mod is **not yet tested in
game**.

## The problem

The owner (6 November 2026) felt Search and Rescue's stabilisation "only stems bleeding" while More Injuries, Burns and Smoke,
Emergency Dosing and Medical Equipment had added many more ways to die, and asked for doctors who take over life-saving
surgery on their own when given the agency, with settings in game and in the main menu, and a triage score deciding which bills
are done first.

## What the code says (read 6 November 2026)

Read: Search and Rescue 0.1.0-alpha.24 (decompile of 3 November, alpha.23, plus the Workshop changelog since), More Injuries
1.6.0.2 (published source; its 1.6 folder loads on 1.7), the game 1.7.4998 and the 1.6.4871 reference.

- **Search and Rescue covers far more than bleeding.** With More Injuries it orders tourniquets, hemostatic agents, bandages,
  CPR, airway suction, the defibrillator, epinephrine, saline and blood, each scored per doctor and patient and matched for
  the best overall outcome. Its emergency test reads bleeding, `lifeThreatening` stages, infections and lethal severity; it
  never reads `deathMtbDays`. **It never adds a surgery bill**: it reads every recipe's `removesHediff` only to raise the
  evacuation priority of a patient who needs surgery. Its treatment list is an internal switch with no Def, extension or API.
- **More Injuries never queues surgery either**, apart from blood-farm extraction on prisoners. Its automatic jobs (CPR, airway,
  defibrillator, saline, blood, splint) need the patient in bed. Its killers that only surgery removes: lung collapse (every
  stage life-threatening, cardiac arrest from severity 0.2; thoracotomy at Medicine 6 or video-assisted surgery at 8 with a
  thoracoscope, which may break), haemorrhagic stroke (tendable, or surgery at 2, 5 or 8), wet gangrene (lethal). Cripplers:
  fracture (its own in-bed splint job; osteosynthesis at 5), spinal paralysis (12), hearing loss (12). Our airway burn is
  surgical only (tracheotomy at 6). Our burn shock, pneumonia and lung injury carry `lifeThreatening` stages and are tendable,
  so Search and Rescue tends them; carbon monoxide needs oxygen, which Medical Equipment gives in a linked bed.
- **Vanilla doctors take the nearest patient and bills in list order.** `WorkGiver_DoBill` (Doctor, priority 70) has no
  urgency. A bill is `HealthCardUtility.CreateSurgeryBill(pawn, recipe, part, null, sendMessages: false)`: it adds the bill,
  then sets the part (that order matters). `Bill.loadID` is private but bills are `ILoadReferenceable`, deep-saved in the
  pawn's health tracker, so a game component can hold them by reference. `BillStack.Reorder(bill, -index)` moves a bill to
  the top; `AddBill` has no cap (the UI's `MaxCount` is 15). The game removes uncompletable medical bills only in
  `WorkGiver_DoBill.JobOnThing`, never from the health tracker, so a mod that adds bills must withdraw its own. A patient is
  carried to a bed by the game's take-to-bed giver when any bill should be done and a doctor could come; a guest only when
  downed. The outcome quality is the recipe's factor times the surgeon's success stat, bed, room and medicine, clamped at 0.98;
  death is rolled only after a failed outcome, at the recipe's death chance.
- **A prioritised scanner is honoured by the job giver** (1.7): with `Prioritized` true and a global list, the target with the
  highest `GetPriority` wins, distance breaking ties; work givers run in priority order within the work type. The 1.6 body
  could not be read (the local decompile is partial); the API is identical.
- **A comp on a pawn is safe to remove**: comp fields are written as plain child elements of the pawn's node with no class
  name, so with the mod gone they are ignored without a line. `Pawn.GetGizmos` yields comp gizmos for every selected pawn,
  raiders included, so the comp gates its own gizmo. An XML patch on `race/intelligence` reaches only Defs that spell it out,
  which is why More Injuries carries a separate Humanoid Alien Races patch; a startup sweep reaches every humanlike race.
- **A cap never kills.** The game's anaesthetic and every sedative cap consciousness with `setMax`; the game kills at zero and
  a cap stays above it. Offsets can take a capacity to zero (Dosing's record: morphine's -0.4 on a 0.35 consciousness).

## Decisions

- **A new mod with no Harmony** (agent choice): every hook is a Def, the framework's patient comp, a game component for the
  ledger and a map component for the pass.
- **Life-saving only by default; a per-person button plus defaults by group; colonists, slaves and captured prisoners on,
  guests off** (owner's choices, 6 November 2026).
- **Drug dependencies left out** (owner's rule, 6 Nov 2026, after the first start on 0.1.0 named psychite dependency in "no one
  can operate" messages): a condition the game marks as an addiction (`IsAddiction`), a gene-driven chemical dependency
  (`Hediff_ChemicalDependency`) or one that creates a chemical need (`chemicalNeed`) is skipped by the whole pass. No installed
  recipe removes one (checked 6 Nov 2026); those messages came from 0.1.0's fault of messaging conditions no operation removes,
  which 0.1.1 also fixes. Rules Def flag `excludeChemicalDependencies`, on.
- **Surgery holds** (0.2.0; the owner's thread with D'thorga Medical Equipment, 7 Nov 2026; agent choices on the details): the
  Framework's `Patients.SurgeryBarred(pawn, out reason)` (0.4.0) reports a hold another mod registered, with an in-world reason
  that mod translated. While held, the planner queues nothing new and withdraws nothing for the hold (stale bills still go when
  the condition is gone or the authority changes), the work giver's list leaves the patient out, and the inspect line and one
  message per patient and killing condition carry the reason. A held operation counts as feasible, so the tag is immediate or
  urgent by the clock, not expectant. Triage holds no reference to the mod that owns the hold.
- **Any repair** (0.1.1): with that authority, a condition classed other is also queued when an operation removes it; such
  conditions never set the tag.
- **Classed by effect, never by name** (owner's standing rule). Lethal: the game's own life-threatening flag on the current
  stage, a death clock, a severity climbing toward a lethal one, or a lasting bad condition (not clearing, `isBad`) with a
  negative offset on consciousness, breathing or blood pumping while that capacity sits at or under 0.3 (the level below which
  the game drops a pawn). A cap counts as crippling at most (agent rule, from the reading above). Crippling: a cap at or under
  0.5, or a part efficiency or capacity offset at or under -0.5, on a condition that does not clear itself (agent-authored
  thresholds).
- **Operations found through `removesHediff`** plus `PhobosTriageExtension.treatsHediffs` for custom workers and
  `neverAutoQueue` for recipes that must wait for the player (agent choice). Every remove-hediff worker seen (the game's, More
  Injuries', ours) keeps `removesHediff` set, so thoracotomy, video-assisted surgery, the stroke recipes, paralysis, hearing,
  fracture repair and the tracheotomy are all found; More Injuries' brain-damage therapy is not until an extension names it.
- **The choice**: highest expected success (recipe factor times the best qualifying doctor's success stat, clamped as the
  game clamps), then lowest ingredient cost (base counts times the cheapest allowed ingredient's market value, for ordering
  only). Refusals in the order of nearest-to-fixable, so the message names the one thing to change: a missing item, then
  skill, no doctor, research, risk, care level, violation, rare tool, no recipe (agent choice).
- **Risk is a setting, 5% by default** (agent default): the recipe's death chance times the chance the best doctor fails.
  More Injuries' thoracotomy at 2.5% with a 0.9 doctor is 0.25%; the tracheotomy at 2% likewise.
- **Rare tools**: an ingredient that is not medicine and is worth 300 silver or more (authored) marks a recipe as needing a rare
  tool; a setting, on by default, lets the doctors use them.
- **The ledger**: a game component holding the mod's bills by reference, pruned by stack membership before saving; only ledger
  bills are ever withdrawn (agent choice). Withdrawal when the condition is gone, the care level is "no care", or the person's
  authority no longer allows the class.
- **Lethal bills to the top** (rules flag, on): a killing condition's bill goes above the player's own queue. The owner may turn
  it off in the rules Def.
- **One pass per map every 250 ticks on a map component**, maps offset from one another (agent choice): bills must exist
  before a doctor is idle, because the game's own take-to-bed giver reads them; a cache rebuilt when a doctor asks would be
  too late. Emergency Dosing keeps the framework's ask-when-needed cache for its own patients.
- **The tag and the score** (agent-authored): immediate within 6 hours or a vital capacity at or under 0.3; urgent otherwise
  while dying; delayed for crippling; minor; expectant for a dying patient whose condition only an operation removes (it is not
  tendable and at least one indexed operation treats it) and nobody can do one. A dying condition no operation removes is never
  expectant and never messaged (0.1.1, after the documentation review found carbon monoxide tagged expectant). The score's time
  scale is the rules Def's `urgentHours` (24): a patient with that many hours left gets half the urgency of one with none. Bands of 100
  so the tag outranks the clock; within a band less time scores higher; expectant has no urgency term, so it never outranks a
  minor case. Hours to death take the soonest of a death clock's mean (an expectation, labelled so), the climb to a lethal
  severity at today's rate, and the game's bleed-out estimate.
- **The doctors' order of work** is a subclass of the game's own operate giver, prioritised by the score, at Doctor priority
  75 (between feeding patients at 80 and the game's operate giver at 70), not orderable by right-click, and skipping cheaply
  when no patient in a bed has a bill to do (agent choice). It lists every patient in a bed with a bill to do, tagged or not, so the player's own
  bills still get done, after the urgent ones.
- **Text stays in the world** (owner's rule): the tag, the condition's label, the operation's label and the reason; Plain mode
  shows no hours.
- **Shared code in the framework** (owner's direction, 6 November 2026): the patient grouping, the care-level test, the danger
  reader, the doctor roster, the per-map cache, the lapsing treatment and the per-person preference comp all live in D'thorga
  Framework 0.3.0, and Emergency Dosing 0.3.1 and Medical Equipment 0.2.2 use them.

## Test protocol (owner runs it; nothing counts as tested until the owner says so)

1. Load with Framework before Triage. Read the load lines: Framework's "patient comp on N humanlike races"; Triage's "operations on the doctors' own: colonists life-saving, slaves life-saving, prisoners life-saving,
   guests never, death chance accepted up to 5%", "N operations indexed", "doctors take the worst case first". No error naming
   `Phobos.Triage` or `Phobos.Framework.Health`.
2. Select a colonist: a button "Doctors may operate: to save life" with a menu of four. Set one to "never without orders".
3. Dev: on a colonist, add More Injuries' lung collapse (Add hediff) and set its severity to 0.5, with a Medicine 6 doctor
   free and 3 industrial medicine in stock. Within 250 ticks (or the dev action "Triage: run the planner now on this map"): a thoracotomy
   bill at the top of their operations, the message "the doctors will operate", the inspect line "Triage: immediate" (or
   urgent, by the clock), and a doctor carrying them to bed to operate. Take the doctor off Doctor work: on the next pass the
   message "no one here can do it: no doctor is skilled enough" (or "no doctor is free"); the tag reads expectant.
4. Two patients in beds, a stroke and a collapsed lung in shock, the stroke nearer the doctor: the doctor goes to the lung first.
   Switch "doctors take the worst case first" off: the nearest.
5. Set the patient's button to "never without orders": the mod's bill disappears on the next pass; a bill you added yourself
   stays. Set it back: the bill returns.
6. A prisoner with a collapsed lung is operated on by default; a Hospitality guest is not until the guest default is raised.
7. Save, reload: the button's choice is kept; the bills are still there and still withdrawn when the condition goes. Remove
   Triage: the save loads with one line about a missing game component and one per map about its map component; the bills it
   queued remain ordinary bills.
8. Dubs Performance Analyzer after a raid: `MapComponent_PhobosTriage` and `WorkGiver_PhobosTriageOperate`.
9. The moved helpers: Emergency Dosing's and Medical Equipment's load lines read as before; a doctor still doses a patient in
   pain; the oxygen concentrator (or the stand, with Vanilla Gravship Expanded) still treats; a burn unit bed still gives air-cell
   care; a save from before the change loads without a line about the treatment comp.
10. A colonist with carbon monoxide poisoning at the dying stage: tagged immediate or urgent, **no** bill and **no** "no one can
    operate" message.

## Results from play

Nothing yet.

## Open points and upstream

**The owner's answers, 7 November 2026** (tracked in [outstanding work](outstanding-work.md); none built yet):

- **Tags as icons:** every tag gets a colour-coded icon in the game's own row of small status icons by a pawn's name (red immediate,
  yellow urgent, green delayed, white minor, black expectant). This settles whether colonists show the expectant tag: they do.
- **Health-tab dropdown:** add it beside the game's medical care setting (a Harmony postfix on the health card); the button stays.
- **Fractures:** triage queues them under "save life or a limb", rather than leaving them to More Injuries' splint giver.
- **Needle decompression** for a collapsed lung under pressure is in the second equipment set, as a kit.
- **More Injuries' brain-damage therapy** and recipes with custom workers get the extension through a `Mods/MoreInjuries` patch.
