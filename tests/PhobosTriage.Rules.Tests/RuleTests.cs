using Phobos.Triage.Rules;
using Xunit;

namespace Phobos.Triage.Rules.Tests;

public class AuthorityRuleTests
{
    [Fact]
    public void EachLevelAllowsExactlyItsClasses()
    {
        Assert.False(AuthorityRule.Allows(TriageAuthority.None, ConditionClass.Lethal));
        Assert.True(AuthorityRule.Allows(TriageAuthority.LifeSaving, ConditionClass.Lethal));
        Assert.False(AuthorityRule.Allows(TriageAuthority.LifeSaving, ConditionClass.Crippling));
        Assert.True(AuthorityRule.Allows(TriageAuthority.LifeSavingAndCrippling, ConditionClass.Crippling));
        Assert.False(AuthorityRule.Allows(TriageAuthority.LifeSavingAndCrippling, ConditionClass.Other));
        Assert.True(AuthorityRule.Allows(TriageAuthority.AnyRepair, ConditionClass.Other));
    }
}

public class ConditionClassRuleTests
{
    static readonly ClassThresholds T = new(0.3f, 0.5f, -0.5f);

    static ConditionReading Reading(bool threatening = false, float mtb = -1f, float lethal = -1f, float severity = 0.5f, float perDay = 0f,
        bool isBad = true, bool clears = false, float vitalOffset = 0f, float vitalLevel = 1f, float cap = 1f, float capOffset = 0f, float partEff = 0f) =>
        new(threatening, mtb, lethal, severity, perDay, isBad, clears, vitalOffset, vitalLevel, cap, capOffset, partEff);

    [Fact]
    public void TheGamesOwnFlagsAreLethal()
    {
        // A collapsed lung: every stage life-threatening. Burn shock, severe: a death clock.
        Assert.Equal(ConditionClass.Lethal, ConditionClassRule.Classify(Reading(threatening: true), T));
        Assert.Equal(ConditionClass.Lethal, ConditionClassRule.Classify(Reading(mtb: 4f), T));
    }

    [Fact]
    public void AClimbingSeverityTowardLethalIsLethalAFallingOneIsNot()
    {
        Assert.Equal(ConditionClass.Lethal, ConditionClassRule.Classify(Reading(lethal: 1f, perDay: 0.5f), T));
        Assert.Equal(ConditionClass.Other, ConditionClassRule.Classify(Reading(lethal: 1f, perDay: -0.5f, clears: true), T));
    }

    [Fact]
    public void ACapNeverKillsButAnOffsetAtTheFloorDoes()
    {
        // The game's anaesthetic caps consciousness at 0.01: crippling at most, never lethal.
        Assert.Equal(ConditionClass.Crippling, ConditionClassRule.Classify(Reading(isBad: false, cap: 0.01f), T));
        // Cerebral hypoxia pushing consciousness down with the patient at 0.2: lethal.
        Assert.Equal(ConditionClass.Lethal, ConditionClassRule.Classify(Reading(vitalOffset: -0.5f, vitalLevel: 0.2f), T));
        // The same offset with the patient still at 0.8: not lethal, and under the crippling line it cripples.
        Assert.Equal(ConditionClass.Crippling, ConditionClassRule.Classify(Reading(vitalOffset: -0.5f, vitalLevel: 0.8f, capOffset: -0.5f), T));
    }

    [Fact]
    public void AFractureCripplesAConcussionClears()
    {
        Assert.Equal(ConditionClass.Crippling, ConditionClassRule.Classify(Reading(partEff: -1f), T));
        Assert.Equal(ConditionClass.Other, ConditionClassRule.Classify(Reading(perDay: -0.5f, clears: true, cap: 0.3f), T));
        Assert.Equal(ConditionClass.Other, ConditionClassRule.Classify(Reading(partEff: -0.2f), T));
    }
}

public class OperationChoiceRuleTests
{
    static readonly OperationLimits Limits = new(0.05f, false, true);

    static OperationCandidate Candidate(bool research = true, bool doctor = true, float best = 0.9f, float factor = 1f, float death = 0.025f,
        bool items = true, float cost = 100f, bool care = true, bool violation = false, bool rare = false) =>
        new(research, doctor, best, factor, death, items, cost, care, violation, rare);

    [Fact]
    public void SuccessIsTheRecipeFactorTimesTheDoctorClampedAndDeathFollowsFailure()
    {
        Assert.Equal(0.9f, OperationChoiceRule.ExpectedSuccess(Candidate()), 4);
        Assert.Equal(0.98f, OperationChoiceRule.ExpectedSuccess(Candidate(best: 1.2f, factor: 1.35f)), 4);
        Assert.Equal(0.1f * 0.025f, OperationChoiceRule.DeathChance(Candidate()), 5);
    }

    [Fact]
    public void TheLikeliestToSucceedWinsThenTheCheapest()
    {
        // Thoracotomy at a factor of 1 against video-assisted surgery at 1.35 with the same doctor: the latter wins.
        var candidates = new[] { Candidate(factor: 1f, cost: 50f), Candidate(factor: 1.35f, cost: 500f) };
        Assert.Equal(1, OperationChoiceRule.Choose(candidates, Limits, out Refusal nearest));
        Assert.Equal(Refusal.None, nearest);
        // The same success: the cheaper one.
        Assert.Equal(0, OperationChoiceRule.Choose(new[] { Candidate(cost: 50f), Candidate(cost: 500f) }, Limits, out _));
    }

    [Fact]
    public void TheNearestRefusalIsReportedAndRiskIsAgainstTheLimit()
    {
        var candidates = new[] { Candidate(research: false), Candidate(items: false), Candidate(best: -1f) };
        Assert.Equal(-1, OperationChoiceRule.Choose(candidates, Limits, out Refusal nearest));
        Assert.Equal(Refusal.Item, nearest);
        Assert.Equal(Refusal.TooRisky, OperationChoiceRule.RefusalOf(Candidate(best: 0.5f, death: 0.2f), Limits));
        Assert.Equal(Refusal.None, OperationChoiceRule.RefusalOf(Candidate(best: 0.5f, death: 0.2f), new OperationLimits(0.2f, false, true)));
        Assert.Equal(Refusal.Violation, OperationChoiceRule.RefusalOf(Candidate(violation: true), Limits));
        Assert.Equal(Refusal.RareTool, OperationChoiceRule.RefusalOf(Candidate(rare: true), new OperationLimits(0.05f, false, false)));
        Assert.Equal(Refusal.NoRecipe, OperationChoiceRule.Choose(new OperationCandidate[0], Limits, out Refusal none) < 0 ? none : Refusal.None);
    }
}

public class TriageRuleTests
{
    static readonly TriageBands B = new(6f, 24f, 0.3f);

    [Fact]
    public void DyingPatientsAreImmediateOrUrgentByTheClockOrTheVitals()
    {
        Assert.Equal(TriageCategory.Immediate, TriageRule.Categorise(new PatientReading(3f, ConditionClass.Lethal, 0.9f, true, true), B));
        Assert.Equal(TriageCategory.Urgent, TriageRule.Categorise(new PatientReading(20f, ConditionClass.Lethal, 0.9f, true, true), B));
        Assert.Equal(TriageCategory.Immediate, TriageRule.Categorise(new PatientReading(float.PositiveInfinity, ConditionClass.Lethal, 0.2f, false, false), B));
        Assert.Equal(TriageCategory.Urgent, TriageRule.Categorise(new PatientReading(float.PositiveInfinity, ConditionClass.Lethal, 0.9f, false, false), B));
    }

    [Fact]
    public void NeedingAnOperationNobodyCanDoIsExpectant()
    {
        Assert.Equal(TriageCategory.Expectant, TriageRule.Categorise(new PatientReading(3f, ConditionClass.Lethal, 0.9f, true, false), B));
        // Field care can hold it: not expectant even with no operation.
        Assert.Equal(TriageCategory.Immediate, TriageRule.Categorise(new PatientReading(3f, ConditionClass.Lethal, 0.9f, false, false), B));
    }

    [Fact]
    public void CrippledIsDelayedAndTheRestMinor()
    {
        Assert.Equal(TriageCategory.Delayed, TriageRule.Categorise(new PatientReading(float.PositiveInfinity, ConditionClass.Crippling, 1f, false, false), B));
        Assert.Equal(TriageCategory.Minor, TriageRule.Categorise(new PatientReading(float.PositiveInfinity, ConditionClass.Other, 1f, false, false), B));
    }

    [Fact]
    public void TheTagOutranksTheClockAndLessTimeScoresHigherWithinIt()
    {
        float immediateLate = TriageRule.Score(TriageCategory.Immediate, 5f);
        float immediateSoon = TriageRule.Score(TriageCategory.Immediate, 0.5f);
        float urgentSoon = TriageRule.Score(TriageCategory.Urgent, 0.1f);
        Assert.True(immediateSoon > immediateLate);
        Assert.True(immediateLate > urgentSoon);
        Assert.True(urgentSoon < 400f);
        Assert.Equal(100f, TriageRule.Score(TriageCategory.Minor, float.PositiveInfinity));
        Assert.True(TriageRule.Score(TriageCategory.Expectant, 1f) < TriageRule.Score(TriageCategory.Minor, float.PositiveInfinity));
    }
}
