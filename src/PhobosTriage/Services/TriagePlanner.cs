using System.Collections.Generic;
using System.Linq;
using Phobos.Framework.Health;
using Phobos.Triage.Defs;
using Phobos.Triage.Rules;
using RimWorld;
using Verse;

namespace Phobos.Triage.Services;

/// <summary>One patient as the last pass read them. Derived, never saved.</summary>
public sealed class TriageEntry
{
    public Pawn Pawn = null!;
    public TriageCategory Category;
    public float Score;
    public float HoursToDeath = float.PositiveInfinity;
    public ConditionClass WorstClass;
    /// <summary>The condition that set the tag, or null for a patient with nothing classed.</summary>
    public Hediff? Worst;
    /// <summary>The bill that will fix the worst condition, ours or the player's.</summary>
    public RecipeDef? Queued;
    /// <summary>Why nothing is queued for the worst condition, when it needed something.</summary>
    public Refusal Refusal;
    /// <summary>The worst condition is lethal and field care cannot hold it.</summary>
    public bool NeedsOperation;
    /// <summary>A bill for the worst condition is on the patient, ours or the player's.</summary>
    public bool Feasible;
}

/// <summary>
/// The one service that decides: reads every patient the doctors look after, classes their conditions by effect, queues the
/// operation the rules choose through the game's own bill, withdraws the mod's own bills once a condition is gone, and tags
/// each patient for the doctors' order of work. The map component calls it on the rules Def's interval; nothing else mutates.
/// </summary>
public static class TriagePlanner
{
    static readonly List<OperationCandidate> candidates = new();
    static readonly List<RecipeDef> candidateRecipes = new();
    static readonly List<BodyPartRecord?> candidateParts = new();
    static readonly Dictionary<RecipeDef, bool> ingredientsPresent = new();
    static readonly Dictionary<Hediff, ConditionClass> classes = new();
    static readonly List<Bill> ownBills = new();
    /// <summary>Patients and conditions already told about: one "no one can operate" message each. Derived; a reload may repeat one.</summary>
    static readonly HashSet<(int, HediffDef)> told = new();

    public static void Run(Map map, List<TriageEntry> entries, List<Thing> patientsWithBills)
    {
        entries.Clear();
        patientsWithBills.Clear();
        ingredientsPresent.Clear();
        TriageSettings settings = TriageMod.Settings;
        PhobosTriageRulesDef rules = PhobosTriageRulesDef.Current;
        TriageBillLedger? ledger = TriageBillLedger.Current;
        ledger?.Prune();
        var limits = new OperationLimits(settings.maxDeathOnFailureChance, settings.allowViolations, settings.allowRareTools);
        ClassThresholds thresholds = rules.Thresholds;
        TriageBands bands = rules.Bands;
        IReadOnlyList<Pawn> pawns = map.mapPawns.AllPawnsSpawned;
        for (int i = 0; i < pawns.Count; i++)
        {
            Pawn pawn = pawns[i];
            if (pawn.BillStack != null && pawn.InBed() && pawn.BillStack.AnyShouldDoNow) patientsWithBills.Add(pawn);
            if (Patients.GroupOf(pawn) == null) continue;
            TriageEntry entry = Assess(pawn, map, settings, rules, ledger, limits, thresholds, bands);
            entries.Add(entry);
        }
    }

    static TriageEntry Assess(Pawn pawn, Map map, TriageSettings settings, PhobosTriageRulesDef rules, TriageBillLedger? ledger, in OperationLimits limits,
        in ClassThresholds thresholds, in TriageBands bands)
    {
        var entry = new TriageEntry { Pawn = pawn };
        TriageAuthority authority = settings.enabled ? (TriageAuthority)PatientPreferences.Choice(pawn, TriagePreferences.Authority) : TriageAuthority.None;
        bool careOk = Patients.CareAllows(pawn, MedicalCareCategory.NoMeds, false);
        bool doctorExists = Doctors.AnyAvailableFor(pawn);
        float bleedHours = ConditionDanger.HoursUntilBloodLossDeath(pawn);
        classes.Clear();
        List<Hediff> hediffs = pawn.health.hediffSet.hediffs;
        for (int i = 0; i < hediffs.Count; i++)
        {
            Hediff hediff = hediffs[i];
            if (!hediff.Visible || !hediff.def.isBad) continue;
            ConditionReading reading = ConditionReader.Read(pawn, hediff);
            ConditionClass conditionClass = ConditionClassRule.Classify(reading, thresholds);
            IReadOnlyList<OperationInfo> operations = OperationIndex.For(hediff.def);
            // A condition that neither kills nor cripples matters only when "any repair" is allowed and an operation fixes it.
            if (conditionClass == ConditionClass.Other && (operations.Count == 0 || !AuthorityRule.Allows(authority, ConditionClass.Other))) continue;
            classes[hediff] = conditionClass;
            float hours = Framework.Rules.ConditionDangerRule.ExpectedHoursToDeath(reading.DeathMtbDays, reading.Severity, reading.LethalSeverity,
                reading.SeverityPerDay, hediff.BleedRate > 0f ? bleedHours : -1f);
            Bill_Medical? existing = FindBill(pawn, hediff);
            Refusal refusal = Refusal.None;
            // Only a condition some operation removes is ever queued or reported: oxygen, rest and time are not operations.
            if (existing == null && operations.Count > 0 && careOk && AuthorityRule.Allows(authority, conditionClass) && pawn.BillStack.Count < rules.maxBillsPerPatient)
            {
                existing = TryQueue(pawn, map, hediff, conditionClass, operations, doctorExists, limits, rules, settings, ledger, out refusal);
            }
            bool needsOperation = conditionClass == ConditionClass.Lethal && !hediff.def.tendable && operations.Count > 0;
            bool feasible = existing != null;
            if (conditionClass == ConditionClass.Other) continue;
            if (conditionClass > entry.WorstClass || (conditionClass == entry.WorstClass && hours < entry.HoursToDeath))
            {
                entry.WorstClass = conditionClass;
                entry.HoursToDeath = hours;
                entry.Worst = hediff;
                entry.Queued = existing?.recipe;
                entry.Refusal = refusal;
                entry.NeedsOperation = needsOperation;
                entry.Feasible = feasible;
            }
        }
        if (ledger != null) WithdrawStale(pawn, ledger, careOk, authority);
        var reading2 = new PatientReading(entry.HoursToDeath, entry.WorstClass, ConditionReader.LowestVital(pawn), entry.NeedsOperation, entry.Feasible);
        entry.Category = TriageRule.Categorise(reading2, bands);
        entry.Score = TriageRule.Score(entry.Category, entry.HoursToDeath, bands.UrgentHours);
        return entry;
    }

    /// <summary>Queues the best feasible operation for a condition through the game's own bill, or says why none can be.</summary>
    static Bill_Medical? TryQueue(Pawn pawn, Map map, Hediff hediff, ConditionClass conditionClass, IReadOnlyList<OperationInfo> operations, bool doctorExists,
        in OperationLimits limits, PhobosTriageRulesDef rules, TriageSettings settings, TriageBillLedger? ledger, out Refusal refusal)
    {
        candidates.Clear();
        candidateRecipes.Clear();
        candidateParts.Clear();
        for (int i = 0; i < operations.Count; i++)
        {
            OperationInfo operation = operations[i];
            RecipeDef recipe = operation.Recipe;
            if (!TryPart(pawn, recipe, hediff, out BodyPartRecord? part)) continue;
            bool researchDone = recipe.AvailableNow;
            float best = doctorExists ? Doctors.BestSurgeonSuccess(map, recipe, pawn) : -1f;
            bool present = IngredientsPresent(recipe, map);
            bool careAllows = Patients.CareAllows(pawn, MedicalCareCategory.NoMeds, operation.NeedsMedicine);
            bool violation = recipe.Worker.IsViolationOnPawn(pawn, part, Faction.OfPlayer);
            candidates.Add(new OperationCandidate(researchDone, doctorExists, best, recipe.surgerySuccessChanceFactor, recipe.deathOnFailedSurgeryChance,
                present, operation.IngredientCost, careAllows, violation, operation.NeedsRareTool));
            candidateRecipes.Add(recipe);
            candidateParts.Add(part);
        }
        int chosen = OperationChoiceRule.Choose(candidates, limits, out refusal);
        if (chosen < 0)
        {
            if (conditionClass == ConditionClass.Lethal && settings.messages && told.Add((pawn.thingIDNumber, hediff.def)))
            {
                Messages.Message("PhobosTriage_MessageNoOne".Translate(pawn.LabelShort.Named("PATIENT"), hediff.LabelBase.Named("CONDITION"),
                    TriageText.RefusalText(refusal).Named("WHY")), pawn, MessageTypeDefOf.NegativeEvent);
            }
            return null;
        }
        RecipeDef chosenRecipe = candidateRecipes[chosen];
        Bill_Medical? bill = HealthCardUtility.CreateSurgeryBill(pawn, chosenRecipe, candidateParts[chosen], null, false);
        if (bill == null) return null;
        ledger?.Add(bill);
        if (conditionClass == ConditionClass.Lethal && rules.lethalBillsToTop)
        {
            int index = pawn.BillStack.IndexOf(bill);
            if (index > 0) pawn.BillStack.Reorder(bill, -index);
        }
        if (settings.messages)
        {
            Messages.Message("PhobosTriage_MessageQueued".Translate(pawn.LabelShort.Named("PATIENT"), chosenRecipe.LabelCap.Named("OPERATION"),
                hediff.LabelBase.Named("CONDITION")), pawn, MessageTypeDefOf.NeutralEvent);
        }
        return bill;
    }

    /// <summary>The part the recipe would be billed on for this condition, or false when the recipe does not apply right now.</summary>
    static bool TryPart(Pawn pawn, RecipeDef recipe, Hediff hediff, out BodyPartRecord? part)
    {
        part = null;
        if (!recipe.targetsBodyPart) return recipe.AvailableOnNow(pawn);
        if (hediff.Part == null) return false;
        foreach (BodyPartRecord candidate in recipe.Worker.GetPartsToApplyOn(pawn, recipe))
        {
            if (candidate == hediff.Part && recipe.AvailableOnNow(pawn, candidate))
            {
                part = candidate;
                return true;
            }
        }
        return false;
    }

    static bool IngredientsPresent(RecipeDef recipe, Map map)
    {
        if (ingredientsPresent.TryGetValue(recipe, out bool present)) return present;
        present = !recipe.PotentiallyMissingIngredients(null, map).Any();
        ingredientsPresent[recipe] = present;
        return present;
    }

    /// <summary>A bill already on the patient, ours or the player's, that treats this condition on this part.</summary>
    static Bill_Medical? FindBill(Pawn pawn, Hediff hediff)
    {
        List<Bill> bills = pawn.BillStack.Bills;
        for (int i = 0; i < bills.Count; i++)
        {
            if (bills[i] is Bill_Medical medical && OperationIndex.Treats(medical.recipe, hediff.def) && (medical.Part == null || medical.Part == hediff.Part)) return medical;
        }
        return null;
    }

    /// <summary>Withdraws our bills whose condition is gone, or that the player's care level or the pawn's authority no longer allows.</summary>
    static void WithdrawStale(Pawn pawn, TriageBillLedger ledger, bool careOk, TriageAuthority authority)
    {
        ledger.OnStack(pawn.BillStack, ownBills);
        for (int i = 0; i < ownBills.Count; i++)
        {
            if (ownBills[i] is not Bill_Medical bill) continue;
            Hediff? target = null;
            foreach (KeyValuePair<Hediff, ConditionClass> pair in classes)
            {
                if (OperationIndex.Treats(bill.recipe, pair.Key.def) && (bill.Part == null || bill.Part == pair.Key.Part))
                {
                    target = pair.Key;
                    break;
                }
            }
            if (target == null || !careOk || !AuthorityRule.Allows(authority, classes[target])) ledger.Withdraw(bill);
        }
    }
}
